using Content.Server.DoAfter;
using Content.Server.Ghost.Roles.Components;
using Content.Server.NPC.HTN;
using Content.Server.Popups;
using Content.Server.Zombies;
using Content.Shared._SCP.SCP049;
using Content.Shared._SCP.SCP049.Components;
using Content.Shared._SCP.SCP049.SharedSystem;
using Content.Shared.Administration.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Traits.Assorted;
using Robust.Server.Player;

namespace Content.Server._SCP.SCP049.Sysytems;

public sealed partial class SCP049System
{
    [Dependency] private readonly MobStateSystem _mobSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly ZombieSystem _zombieSystem = default!;
    [Dependency] private readonly RejuvenateSystem _rejuvenate = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    private void InitializeActions()
    {
        SubscribeLocalEvent<SCPMob049Component, Scp049RepeatedTreatmentAction>(OnRepeatedTreatment);
    }

    private void OnRepeatedTreatment(Entity<SCPMob049Component> scpEntity, ref Scp049RepeatedTreatmentAction args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        var scpComp = scpEntity.Comp;
        var target = args.Target;

        if (!_mobSystem.IsDead(target))
        {
            ShowCantTreatmentPopup(target, scpEntity);
            return;
        }

        if (TryComp<SCPZombie049Component>(target, out var zombieComp))
        {
            if (zombieComp.ResurrectionCapacity == 0)
            {
                ShowCantTreatmentPopup(target, scpEntity);
                return;
            }
        }

        if (scpComp.Zombies.Count >= scpComp.MaxZombies)
        {
            ShowPopup("scp049-action-max-cap-zombies", target, scpEntity, PopupType.MediumCaution);
            return;
        }

        var treatmentTime = zombieComp != null
            ? scpComp.RepeatedTreatmentZombieTime
            : scpComp.RepeatedTreatmentTime;

        var doAfterTreatArgs = new DoAfterArgs(
            EntityManager,
            scpEntity,
            treatmentTime,
            new ZombieResurrectionDoAfterEvent(),
            target: target,
            eventTarget: scpEntity
        )
        {
            BreakOnMove = true,
            BreakOnDamage = true,
        };

        args.Handled = _doAfter.TryStartDoAfter(doAfterTreatArgs);
    }

    private bool TryRepeatedTreatment(Entity<MobStateComponent> zombieEntity, Entity<SCPMob049Component> scpEntity)
    {
        if (!HasComp<HumanoidAppearanceComponent>(zombieEntity))
            return false;

        return RepeatedTreatment(zombieEntity, scpEntity);
    }

    private bool RepeatedTreatment(Entity<MobStateComponent> zombieEntity, Entity<SCPMob049Component> scpEntity)
    {
        if (!_mobSystem.IsDead(zombieEntity))
            return false;

        var zombie049Component = EnsureComp<SCPZombie049Component>(zombieEntity.Owner);

        if (zombie049Component.ResurrectionCapacity > 0)
        {
            zombie049Component.ResurrectionCapacity--;
        }

        zombie049Component.OwnerUid = scpEntity;

        var zombieUid = zombieEntity.Owner;
        Dirty(zombieUid, zombie049Component);

        if (!scpEntity.Comp.Zombies.Contains(zombieUid))
        {
            scpEntity.Comp.Zombies.Add(zombieUid);
        }

        _zombieSystem.ZombifyEntity(zombieEntity);
        _rejuvenate.PerformRejuvenate(zombieEntity);
        _mobSystem.ChangeMobState(zombieEntity, MobState.Alive);

        EnsureComp<NonSpreaderZombieComponent>(zombieUid);
        RemComp<HTNComponent>(zombieUid);

        TryMakeGhostRole(zombieUid);

        return true;
    }

    private void TryMakeGhostRole(EntityUid zombieUid)
    {
        if (_player.TryGetSessionByEntity(zombieUid, out _))
            return;

        var ghostRoleComponent = EnsureComp<GhostRoleComponent>(zombieUid);

        ghostRoleComponent.RoleName = Loc.GetString("scp049-2-ghost-role-name");
        ghostRoleComponent.RoleDescription = Loc.GetString("scp049-2-ghost-role-description");
        ghostRoleComponent.RoleRules = Loc.GetString("scp049-2-ghost-role-rules");

        EnsureComp<GhostTakeoverAvailableComponent>(zombieUid);
    }

    private void ShowCantTreatmentPopup(EntityUid target, EntityUid scpEntity)
    {
        ShowPopup("scp049-action-cant-treatment", target, scpEntity, PopupType.MediumCaution);
    }

    private void ShowPopup(string message, EntityUid target, EntityUid source, PopupType popupType)
    {
        _popupSystem.PopupEntity(
            Loc.GetString(message),
            target,
            source,
            popupType
        );
    }
}
