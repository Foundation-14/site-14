using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Stealth;
using Content.Shared.Stealth.Components;
using Content.Shared._SCP.SCP268;

namespace Content.Server._SCP.SCP268;

/// <summary>
/// Handles SCP-268 "Cap of Neglect" hat effect.
/// </summary>
public sealed partial class SCP268System : EntitySystem
{
    [Dependency] private readonly SharedStealthSystem _stealth = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SCP268BlindfoldComponent, GotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<SCP268BlindfoldComponent, GotUnequippedEvent>(OnGotUnequipped);
    }

    private void OnGotEquipped(EntityUid uid, SCP268BlindfoldComponent component, GotEquippedEvent args)
    {
        if (!args.SlotFlags.HasFlag(SlotFlags.HEAD))
            return;

        component.Enabled = true;

        var wearer = args.EquipTarget;

        component.Wearer = wearer;

        var stealth = EnsureComp<StealthComponent>(wearer);

        _stealth.SetEnabled(wearer, true);
        _stealth.SetVisibility(wearer, -1f);

        EnsureComp<SCP268InteractionBlockerComponent>(wearer);
    }

    private void OnGotUnequipped(EntityUid uid, SCP268BlindfoldComponent component, GotUnequippedEvent args)
    {
        if (!component.Enabled)
            return;

        var wearer = component.Wearer;

        component.Wearer = EntityUid.Invalid;
        component.Enabled = false;

        if (wearer.IsValid() && HasComp<StealthComponent>(wearer))
        {
            _stealth.SetEnabled(wearer, false);
            RemComp<StealthComponent>(wearer);
        }

        if (wearer.IsValid() && HasComp<SCP268InteractionBlockerComponent>(wearer))
        {
            RemComp<SCP268InteractionBlockerComponent>(wearer);
        }
    }
}
