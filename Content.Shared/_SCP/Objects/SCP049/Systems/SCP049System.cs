using Content.Shared.IdentityManagement;
using Content.Shared.Humanoid;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared._SCP.SCP049;
using Content.Shared._SCP.SCP049.Components;
using Robust.Shared.GameObjects;

namespace Content.Shared._SCP.SCP049.SharedSystem;

public abstract partial class SharedSCP049System : EntitySystem
{
    [Dependency] protected MobStateSystem _mobSystem = default!;
    [Dependency] protected SharedPopupSystem _popupSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<SCPMob049Component, Scp049StopLifeAction>(OnStopLifeAction);
    }

    private void OnStopLifeAction(Entity<SCPMob049Component> ent, ref Scp049StopLifeAction args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        var target = args.Target;

        if (_mobSystem.IsDead(target))
        {
            ShowPopupClient("scp049-action-dead", target, ent, PopupType.MediumCaution);
            return;
        }

        bool hasTargetComp = HasComp<SCPTarget049Component>(target);
        bool hasZombieComp = HasComp<SCPZombie049Component>(target);

        if (!hasTargetComp && !hasZombieComp)
        {
            ShowPopupClient("scp049-action-not-require-treatment", target, ent, PopupType.MediumCaution);
            return;
        }

        RemComp<SCPTarget049Component>(target);

        if (ent.Comp.Zombies.Contains(target))
        {
            var zombie049Component = EnsureComp<SCPZombie049Component>(target);
            if (zombie049Component.ResurrectionCapacity <= 0)
            {
                ent.Comp.Zombies.Remove(target);
            }
        }

        _mobSystem.ChangeMobState(target, MobState.Dead);

        _popupSystem.PopupPredicted(
            Loc.GetString("scp049-touch-action-success",
                ("target", Identity.Name(target, EntityManager)),
                ("performer", Identity.Name(ent, EntityManager))
            ),
            target,
            ent,
            PopupType.LargeCaution
        );
    }

    private void ShowPopupClient(string message, EntityUid target, EntityUid source, PopupType popupType)
    {
        _popupSystem.PopupClient(
            Loc.GetString(message),
            target,
            source,
            popupType
        );
    }
}
