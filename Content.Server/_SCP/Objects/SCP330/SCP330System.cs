using System.Linq;
using Content.Shared.Examine;
using Content.Shared.Hands.Components;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.EntitySystems;
using Content.Shared._SCP.SCP330.Components;
using Content.Shared.Interaction;
using Content.Shared.Body;
using Content.Server.Popups;
using Robust.Shared.Player;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._SCP.SCP330;

public sealed partial class SCP330System : EntitySystem
{
    [Dependency] private IGameTiming _gameTiming = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private BinSystem _binSystem = default!;

    /// <summary>
    /// Stores pending interactors for each bowl as a queue of (user, timestamp).
    /// Used to determine who took a candy from the container - ties each pickup to
    /// a specific interaction in FIFO order, avoiding race conditions.
    /// </summary>
    private readonly Dictionary<EntityUid, Queue<(EntityUid User, TimeSpan Time)>> _pendingInteractors = new();

    /// <summary>
    /// Stores the time of the last bowl refill.
    /// </summary>
    private readonly Dictionary<EntityUid, TimeSpan> _lastRefill = new();

    private static readonly TimeSpan InteractorTtl = TimeSpan.FromSeconds(30);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SCP330Component, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<SCP330Component, EntRemovedFromContainerMessage>(OnCandyTaken);
        SubscribeLocalEvent<SCP330Component, InteractHandEvent>(OnInteractHand, before: new[] { typeof(BinSystem) });
        SubscribeLocalEvent<SCP330Component, ComponentShutdown>(OnCompShutdown);
    }

    private void OnCompShutdown(EntityUid uid, SCP330Component component, ComponentShutdown args)
    {
        _pendingInteractors.Remove(uid);
        _lastRefill.Remove(uid);
    }

    private void OnExamined(EntityUid uid, SCP330Component component, ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("scp330-examine-warning"));
    }

    private void OnInteractHand(EntityUid uid, SCP330Component component, InteractHandEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp(args.User, out HandsComponent? _))
        {
            args.Handled = true;
            return;
        }

        // Store the user and timestamp when they interact with the bowl
        // This allows us to identify who took a candy when it is removed
        if (TryComp(args.User, out ActorComponent? actor) && actor.PlayerSession.AttachedEntity.HasValue)
        {
            // Do not enqueue the user until candy removal succeeds.
            // OnInteractHand runs before BinSystem, so an empty bowl must not leave a stale entry.
            if (!TryComp(uid, out BinComponent? bin) || bin.Items.Count == 0)
                return;

            if (!_pendingInteractors.TryGetValue(uid, out var queue))
            {
                queue = new Queue<(EntityUid, TimeSpan)>();
                _pendingInteractors[uid] = queue;
            }

            queue.Enqueue((args.User, _gameTiming.CurTime));
        }
    }

    private void OnCandyTaken(EntityUid uid, SCP330Component component, EntRemovedFromContainerMessage args)
    {
        // Get the user who took the candy from our pending interactors queue
        EntityUid? player = null;
        if (_pendingInteractors.TryGetValue(uid, out var queue))
        {
            while (queue.Count > 0)
            {
                var (candidateUser, time) = queue.Dequeue();
                if (_gameTiming.CurTime - time <= InteractorTtl &&
                    TryComp(candidateUser, out ActorComponent? actor) &&
                    actor.PlayerSession.AttachedEntity.HasValue)
                {
                    player = actor.PlayerSession.AttachedEntity.Value;
                    break;
                }
            }

            if (queue.Count == 0)
                _pendingInteractors.Remove(uid);
        }

        if (player is not { } takenPlayer)
            return;

        // Reset the refill timer only after a candy is actually removed
        _lastRefill[uid] = _gameTiming.CurTime;

        if (!TryComp(takenPlayer, out HandsComponent? _))
            return;

        if (!TryComp(takenPlayer, out SCP330CounterComponent? counter))
        {
            counter = EnsureComp<SCP330CounterComponent>(takenPlayer);
            counter.TakenCount = 0;
            counter.ExpiresAt = _gameTiming.CurTime + TimeSpan.FromMinutes(15);
            Dirty(takenPlayer, counter);
        }

        counter.TakenCount++;
        Dirty(takenPlayer, counter);

        // Check if hands should be removed (3 or more candies taken)
        if (counter.TakenCount >= 3)
        {
            RemoveHands(takenPlayer);
            _popup.PopupEntity(Loc.GetString("scp330-hands-removed"), takenPlayer);
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Periodically refill the bowl.
        var refillQuery = EntityQueryEnumerator<SCP330Component>();
        while (refillQuery.MoveNext(out var uid, out var component))
        {
            // Check whether enough time has passed since the last refill.
            var now = _gameTiming.CurTime;
            if (_lastRefill.TryGetValue(uid, out var lastRefill) &&
                now - lastRefill < component.AutoRefillInterval)
            {
                continue;
            }

            RefillCandy(uid, component);
            _lastRefill[uid] = now;
        }

        // Check expiration for SCP330CounterComponent
        var counterQuery = EntityQueryEnumerator<SCP330CounterComponent>();
        var expiredUids = new List<EntityUid>();
        while (counterQuery.MoveNext(out var uid, out var counter))
        {
            if (_gameTiming.CurTime >= counter.ExpiresAt)
            {
                expiredUids.Add(uid);
            }
        }

        foreach (var uid in expiredUids)
        {
            RemComp<SCP330CounterComponent>(uid);
        }
    }

    private void RefillCandy(EntityUid uid, SCP330Component component)
    {
        if (!TryComp(uid, out BinComponent? bin))
            return;

        if (component.CandyPrototypeId.Id == null)
            return;

        int currentCount = bin.Items.Count;
        int neededCount = Math.Max(0, bin.MaxItems - currentCount);

        for (int i = 0; i < neededCount; i++)
        {
            EntityUid candy = EntityManager.SpawnEntity(component.CandyPrototypeId, Transform(uid).MapPosition);
            if (!_binSystem.TryInsertIntoBin(uid, candy, bin))
            {
                Del(candy);
                break;
            }
        }
    }

    private void RemoveHands(EntityUid player)
    {
        if (!_container.TryGetContainer(player, BodyComponent.ContainerID, out var organContainer))
            return;

        var handCategories = new HashSet<ProtoId<OrganCategoryPrototype>>() { "HandLeft", "HandRight" };

        foreach (var organUid in organContainer.ContainedEntities.ToArray())
        {
            if (!TryComp(organUid, out OrganComponent? organ) ||
                organ.Category is not { } category ||
                !handCategories.Contains(category))
                continue;

            _container.Remove(organUid, organContainer);
        }

        if (TryComp(player, out HandsComponent? hands))
            RemComp(player, hands);
    }
}
