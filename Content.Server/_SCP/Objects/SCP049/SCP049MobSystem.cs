using Content.Server.Actions;
using Content.Shared._SCP.SCP049;
using Content.Shared._SCP.SCP049.Components;
using Content.Shared._SCP.SCP049.SharedSystem;
using Content.Shared.Humanoid;
using Content.Shared.Popups;
using Content.Shared.Inventory;
using Content.Shared.Mobs.Components;
using Content.Shared.Zombies;
using Robust.Shared.Random;

namespace Content.Server._SCP.SCP049.Sysytems;

public sealed partial class SCP049System : SharedSCP049System
{
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SCPMob049Component, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<SCPMob049Component, ZombieResurrectionDoAfterEvent>(OnRepeatedTreatmentDoAfter);
        SubscribeLocalEvent<SCPMob049Component, Scp049TargetLockAction>(OnTargetLock);

        InitializeActions();
    }

    private void OnStartup(Entity<SCPMob049Component> ent, ref ComponentStartup args)
    {
        var comp = ent.Comp;
        
        _actions.AddAction(ent, comp.TargetLockAction);
        _actions.AddAction(ent, comp.StopLifeAction);
        _actions.AddAction(ent, comp.RepeatedTreatmentAction);

        Dirty(ent);
    }

    private void OnTargetLock(Entity<SCPMob049Component> ent, ref Scp049TargetLockAction args)
    {
        if (args.Handled)
            return;

        if (!IsValidTarget(ent, args.Target))
        {
            ShowPopup("scp049-action-not-require-treatment", ent, PopupType.LargeCaution);
            return;
        }

        if (HasComp<SCPTarget049Component>(args.Target))
            return;

        args.Handled = true;

        float finalChance = ent.Comp.TargetLockChance;

        if (TryComp<SCPPestilenceComponent>(args.Target, out var pestilence))
        {
            finalChance *= pestilence.InfectionChanceMultiplier;
            finalChance += pestilence.InfectionChanceBonus;
            finalChance = Math.Clamp(finalChance, 0f, 1f);
        }

        if (_random.Prob(finalChance))
        {
            EnsureComp<SCPTarget049Component>(args.Target);
            ShowPopup("scp049-targetlock-success", ent, PopupType.LargeCaution);
        }
        else
        {
            ShowPopup("scp049-targetlock-failed-chance", ent, PopupType.MediumCaution);
        }
    }

    private bool IsValidTarget(Entity<SCPMob049Component> ent, EntityUid target)
    {
        return target != ent.Owner 
            && _mobSystem.IsAlive(target) 
            && HasComp<HumanoidProfileComponent>(target) 
            && !HasComp<SCPTarget049Component>(target) 
            && !HasComp<SCPZombie049Component>(target);
    }

    private void OnRepeatedTreatmentDoAfter(Entity<SCPMob049Component> scpEntity, ref ZombieResurrectionDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || !args.Target.HasValue)
            return;

        if (!TryComp<MobStateComponent>(args.Target.Value, out var mobStateComponent))
            return;

        Dirty(scpEntity);
        
        TryRepeatedTreatment((args.Target.Value, mobStateComponent), scpEntity);
        
        args.Handled = true;
    }

    private void ShowPopup(string message, EntityUid source, PopupType popupType)
    {
        _popupSystem.PopupEntity(
            Loc.GetString(message),
            source,
            source,
            popupType
        );
    }
}
