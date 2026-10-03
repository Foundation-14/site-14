using Content.Shared._SCP.SCP427.Components;
using Content.Shared.Damage;
using Content.Shared.Stunnable;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Shared._SCP.SCP427.Systems;

public sealed class SCP427SharedSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SCP427ExposureComponent, StandUpAttemptEvent>(OnDeformedStandAttempt);
        SubscribeLocalEvent<MeleeWeaponComponent, MeleeHitEvent>(OnMeleeHit);
    }

    private void OnDeformedStandAttempt(Entity<SCP427ExposureComponent> ent, ref StandUpAttemptEvent args)
    {
        if (!ent.Comp.PermanentCrawl)
            return;

        args.Cancelled = true;
    }

    private void OnMeleeHit(EntityUid uid, MeleeWeaponComponent comp, ref MeleeHitEvent args)
    {
        if (!args.IsHit || args.HitEntities.Count == 0)
            return;

        if (!TryComp<SCP427ExposureComponent>(args.User, out var exposure) ||
            !exposure.SpasmsActive ||
            exposure.MeleeDamageMultiplier == 1f)
            return;

        args.ModifiersList.Add(new DamageModifierSet
        {
            Coefficients = new()
            {
                { "Blunt", exposure.MeleeDamageMultiplier },
                { "Slash", exposure.MeleeDamageMultiplier },
                { "Piercing", exposure.MeleeDamageMultiplier },
                { "Stamina", exposure.MeleeDamageMultiplier },
            }
        });
    }
}
