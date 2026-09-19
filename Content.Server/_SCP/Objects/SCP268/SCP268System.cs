using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Stealth;
using Content.Shared.Stealth.Components;
using Content.Shared.Tag;
using Content.Shared._SCP.SCP268;

namespace Content.Server._SCP.SCP268;

/// <summary>
/// Handles SCP-268 "Cap of Neglect" hat effect.
/// </summary>
public sealed partial class SCP268System : EntitySystem
{
    [Dependency] private readonly SharedStealthSystem _stealth = default!;
    [Dependency] private readonly TagSystem _tag = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SCP268Component, GotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<SCP268Component, GotUnequippedEvent>(OnGotUnequipped);
        SubscribeLocalEvent<SCP268Component, ComponentShutdown>(OnComponentShutdown);
    }

    private void OnGotEquipped(EntityUid uid, SCP268Component component, GotEquippedEvent args)
    {
        if (!args.SlotFlags.HasFlag(SlotFlags.HEAD))
            return;

        var wearer = args.EquipTarget;

        component.Wearer = wearer;

        var stealth = EnsureComp<StealthComponent>(wearer);

        _stealth.SetEnabled(wearer, true);
        _stealth.SetVisibility(wearer, -1f);

        EnsureComp<SCP268InteractionBlockerComponent>(wearer);

        _tag.RemoveTag(wearer, "FootstepSound");
    }

    private void OnGotUnequipped(EntityUid uid, SCP268Component component, GotUnequippedEvent args)
    {
        RemoveEffects(component);
        component.Wearer = EntityUid.Invalid;
    }

    private void OnComponentShutdown(EntityUid uid, SCP268Component component, ComponentShutdown args)
    {
        RemoveEffects(component);
    }

    private void RemoveEffects(SCP268Component component)
    {
        var wearer = component.Wearer;
        if (!wearer.IsValid())
            return;

        if (HasComp<StealthComponent>(wearer))
        {
            _stealth.SetEnabled(wearer, false);
            RemComp<StealthComponent>(wearer);
        }

        if (HasComp<SCP268InteractionBlockerComponent>(wearer))
        {
            RemComp<SCP268InteractionBlockerComponent>(wearer);
        }

        _tag.AddTag(wearer, "FootstepSound");
    }
}
