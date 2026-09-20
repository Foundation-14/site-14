using Content.Shared._SCP.SCP268;
using Content.Shared.Actions.Events;
using Content.Shared.Emoting;
using Content.Shared.Hands;
using Content.Shared.Throwing;
using Content.Shared.Interaction.Events;
using Robust.Shared.Containers;

namespace Content.Shared._SCP.SCP268;

/// <summary>
/// Handles <see cref="SCP268InteractionBlockerComponent"/>, which prevents various
/// kinds of interactions (but NOT movement) when attached to an entity.
/// Allows inventory interactions and hand switching, but blocks world interactions
/// and hotbar actions. Also prevents dropping items while wearing the blindfold.
/// </summary>
public sealed partial class SCP268InteractionBlockerSystem : EntitySystem
{
    [Dependency] private SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<SCP268InteractionBlockerComponent, InteractionAttemptEvent>(OnInteractionAttempt);
        SubscribeLocalEvent<SCP268InteractionBlockerComponent, UseAttemptEvent>(OnUseAttempt);
        SubscribeLocalEvent<SCP268InteractionBlockerComponent, ThrowAttemptEvent>(OnThrowAttempt);
        SubscribeLocalEvent<SCP268InteractionBlockerComponent, AttackAttemptEvent>(OnAttackAttempt);
        SubscribeLocalEvent<SCP268InteractionBlockerComponent, EmoteAttemptEvent>(OnEmoteAttempt);
        SubscribeLocalEvent<SCP268InteractionBlockerComponent, DropAttemptEvent>(OnDropAttempt);
        SubscribeLocalEvent<SCP268InteractionBlockerComponent, ActionAttemptEvent>(OnActionAttempt);
    }

    private void OnInteractionAttempt(Entity<SCP268InteractionBlockerComponent> ent, ref InteractionAttemptEvent args)
    {
        // Allow self-interactions (target null) e.g., hand swap
        if (args.Target == null)
            return;

        // Allow self-targeting
        if (args.Target == ent.Owner)
            return;

        // Allow interactions with entities in the user's container hierarchy
        // (items in hands, worn items, inventory items).
        // Block if the target is not contained (world-root entities like doors, other players, items on ground).
        if (_container.IsInSameOrParentContainer(ent.Owner, args.Target.Value))
        {
            // The target has a containing container → it's an inventory/worn item owned by the user
            if (_container.TryGetContainingContainer(args.Target.Value, out _))
                return;
        }

        // Block all other world interactions (doors, other people, etc.)
        args.Cancelled = true;
    }

    private void OnUseAttempt(Entity<SCP268InteractionBlockerComponent> ent, ref UseAttemptEvent args)
    {
        // Allow use of items currently held in the user's hand
        // The used item must be in the user's container hierarchy
        if (_container.IsInSameOrParentContainer(ent.Owner, args.Used))
        {
            if (_container.TryGetContainingContainer(args.Used, out _))
                return;
        }

        // Block use of items not in the user's hand (world use of held items)
        args.Cancel();
    }

    private void OnThrowAttempt(EntityUid uid, SCP268InteractionBlockerComponent component, ThrowAttemptEvent args)
    {
        args.Cancel();
    }

    private void OnAttackAttempt(EntityUid uid, SCP268InteractionBlockerComponent component, AttackAttemptEvent args)
    {
        args.Cancel();
    }

    private void OnEmoteAttempt(EntityUid uid, SCP268InteractionBlockerComponent component, EmoteAttemptEvent args)
    {
        args.Cancel();
    }

    private void OnDropAttempt(EntityUid uid, SCP268InteractionBlockerComponent component, DropAttemptEvent args)
    {
        args.Cancel();
    }

    private void OnActionAttempt(Entity<SCP268InteractionBlockerComponent> ent, ref ActionAttemptEvent args)
    {
        args.Cancelled = true;
    }
}
