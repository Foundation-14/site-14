using Content.Server.Storage.EntitySystems;
using Content.Shared._SCP.Objects.SCP458.Components;
using Content.Shared.Mind;
using Content.Shared.Storage;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._SCP.Objects.SCP458.Systems;

public sealed partial class SCP458System : EntitySystem
{
    [Dependency] private StorageSystem _storage = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private IPrototypeManager _protos = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        Subs.BuiEvents<SCP458Component>(StorageComponent.StorageUiKey.Key, subs =>
        {
            subs.Event<BoundUIClosedEvent>(OnBoundUIClosed);
        });

        SubscribeLocalEvent<SCP458Component, BoundUIOpenedEvent>(OnBoundUIOpen);
    }

    private void OnBoundUIOpen(Entity<SCP458Component> ent, ref BoundUIOpenedEvent args)
    {
        if (ent.Comp.SpawnedPizza.HasValue)
            return;

        if (!TryComp(ent.Owner, out StorageComponent? storage))
            return;

        var userId = GetNetUserId(args.Actor);
        if (!ent.Comp.PreferenceMemory.TryGetValue(userId, out var proto))
        {
            if (ent.Comp.PossiblePizzas.Count == 0)
                return;

            proto = ent.Comp.PossiblePizzas[_random.Next(ent.Comp.PossiblePizzas.Count)];
            ent.Comp.PreferenceMemory[userId] = proto;
        }

        if (!_protos.TryIndex<EntityPrototype>(proto, out var pizzaProto))
            return;

        var pizza = Spawn(pizzaProto.ID, Transform(ent.Owner).Coordinates);
        ent.Comp.SpawnedPizza = pizza;
        Dirty(ent);

        if (!_storage.Insert(ent.Owner, pizza, out _, user: args.Actor, storageComp: storage, playSound: false))
        {
            ent.Comp.SpawnedPizza = null;
            QueueDel(pizza);
        }
    }

    private void OnBoundUIClosed(Entity<SCP458Component> ent, ref BoundUIClosedEvent args)
    {
        if (!ent.Comp.SpawnedPizza.HasValue)
            return;

        if (_ui.IsUiOpen(ent.Owner, StorageComponent.StorageUiKey.Key))
            return;

        var pizza = ent.Comp.SpawnedPizza.Value;

        if (_container.TryGetContainingContainer(pizza, out var container) &&
            container.Owner == ent.Owner)
        {
            _container.Remove(pizza, container);
            QueueDel(pizza);
        }

        ent.Comp.SpawnedPizza = null;
        Dirty(ent);
    }

    private NetUserId GetNetUserId(EntityUid uid)
    {
        if (_mind.TryGetMind(uid, out var mindId, out var mind))
            return mind.UserId ?? default;

        return default;
    }
}
