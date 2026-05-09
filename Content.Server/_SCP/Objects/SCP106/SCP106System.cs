using Content.Server._SCP.SpawnLabel;
using Content.Shared._SCP.SCP106.Components;
using Content.Shared._SCP.SCP106;
using Content.Shared.Actions;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Mind.Components;
using Content.Shared.Mind;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC.Systems;
using Content.Shared.Popups;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;
using Robust.Server.Player;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._SCP.SCP106;

public sealed class SCP106System : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffect = default!;
    [Dependency] private readonly SCP106TrapSystem _TrapSCP = default!;
    [Dependency] private readonly SpawnLabelSystem _spawnLabel = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    public List<EntityUid> Targets = new List<EntityUid>();
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SCP106Component, ComponentInit>(OnInit);
        SubscribeLocalEvent<SCP106Component, ComponentShutdown>(OnShut);
        SubscribeLocalEvent<SCP106Component, SCP106SelectTargetEvent>(OnAttackTarget);
        SubscribeLocalEvent<SCP106Component, SCP106TeleportEvent>(OnTeleportToPortal);
        SubscribeLocalEvent<SCP106Component, SCP106SpawnPortalEvent>(OnSpawnPortal);
        SubscribeLocalEvent<SCP106Component, SCP106SpawnTrapEvent>(OnSpawnTrap);
    }
    private void OnInit(EntityUid uid, SCP106Component component, ComponentInit args)
    {
        _actions.AddAction(uid, ref component.SCP106TeleportActionEntity, component.SCP106TeleportAction, uid);
        _actions.AddAction(uid, ref component.SCP106SelectTargetActionEntity, component.SCP106SelectTargetAction, uid);
        _actions.AddAction(uid, ref component.SCP106SpawnPortalActionEntity, component.SCP106SpawnPortalAction, uid);
        _actions.AddAction(uid, ref component.SCP106SpawnTrapActionEntity, component.SCP106SpawnTrapAction, uid);
    }

    private void OnShut(EntityUid uid, SCP106Component component, ComponentShutdown args)
    {
        _actions.RemoveAction(uid, component.SCP106TeleportActionEntity);
        _actions.RemoveAction(uid, component.SCP106SelectTargetActionEntity);
        _actions.RemoveAction(uid, component.SCP106SpawnPortalActionEntity);
        _actions.RemoveAction(uid, component.SCP106SpawnTrapActionEntity);
    }

    private void OnAttackTarget(EntityUid uid, SCP106Component component, SCP106SelectTargetEvent args)
    {
        if (args.Handled)
            return;

        if (args.Target == uid)
            return;

        if (_npcFaction.IsEntityFriendly(uid, args.Target))
            return;

        if (!TryComp<MobStateComponent>(args.Target, out var mobState))
            return;

        if (_mobState.IsDead(args.Target, mobState))
            return;

        if (Targets.Contains(args.Target))
            return;

        Targets.Add(args.Target);
        _stun.TryUpdateParalyzeDuration(args.Target, TimeSpan.FromSeconds(component.ParalyzeTime));
        _statusEffect.TryAddStatusEffect<StunnedComponent>(uid, "Stun", TimeSpan.FromSeconds(component.ParalyzeTime), true);

        if (component.SoundDamage != null)
            _audio.PlayPvs(component.SoundDamage, uid);

        _damageable.TryChangeDamage(args.Target, component.Damage);

        if (component.SoundEnterPD != null 
            && TryComp<MindContainerComponent>(args.Target, out var mind)
            && TryComp<MindComponent>(mind.Mind, out var mindComp) 
            && mindComp.UserId != null)
        {
            if (_playerManager.TryGetSessionById(mindComp.UserId.Value, out var session))
            {
                Filter playerFilter = Filter.Empty();
                playerFilter.AddPlayer(session);
                _audio.PlayGlobal(component.SoundEnterPD, playerFilter, false);
            }
        }

        Timer.Spawn(TimeSpan.FromSeconds(component.ParalyzeTime++),
        () =>
        {
            if (!EntityManager.EntityExists(uid))
                return;

            Targets.Remove(args.Target);
            _stun.TryUpdateParalyzeDuration(args.Target, TimeSpan.FromSeconds(component.ParalyzeTime * 2));
            _spawnLabel.TeleportToLabel(component.LabelKey, args.Target);
        });
    }

    private void OnSpawnTrap(EntityUid uid, SCP106Component component, ref SCP106SpawnTrapEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (args.Handled)
            return;

        if (component.Traps.Count >= component.TrapsLimit)
        {
            var firstPortal = component.Traps[0];
            component.Traps.RemoveAt(0);
            QueueDel(firstPortal);
        }

        if (component.SoundSpawnTrap != null)
            _audio.PlayPvs(component.SoundSpawnTrap, uid);

        var trap = Spawn(component.TrapId, Transform(uid).Coordinates);
        component.Traps.Add(trap);

        args.Handled = true;
    }

    private void OnTeleportToPortal(EntityUid uid, SCP106Component component, ref SCP106TeleportEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (args.Handled)
            return;

        if (component.Portal == null)
        {
            _popup.PopupEntity(Loc.GetString("Вы не поставили телепорт!"), uid, uid);
            return;
        }

        if (component.IsTeleported == true)
        {
            _popup.PopupEntity(Loc.GetString("Вы уже телепортируетесь!"), uid, uid);
            return;
        }

        args.Handled = true;

        if (TryComp<SCP106TrapComponent>(component.Portal, out var trap))
        {
            trap.IsExit = true;
            _TrapSCP.UpdateState(component.Portal.Value);
            Teleport(uid, Transform(component.Portal.Value).Coordinates, true, component);
        }
    }

    private void OnSpawnPortal(EntityUid uid, SCP106Component component, SCP106SpawnPortalEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (args.Handled)
            return;

        args.Handled = true;

        if (component.SoundSpawnPortal != null)
            _audio.PlayPvs(component.SoundSpawnPortal, uid);

        var portal = Spawn(component.PortalId, Transform(uid).Coordinates);

        if (component.Portal != null)
            QueueDel(component.Portal);

        component.Portal = portal;
    }

    public void Teleport(EntityUid uid, EntityCoordinates coord, bool updatePortal = false, SCP106Component? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.SoundTeleport != null)
            _audio.PlayPvs(component.SoundTeleport, uid);

        component.IsTeleported = true;
        UpdateState(uid, component);

        _statusEffect.TryAddStatusEffect<StunnedComponent>(uid, "Stun", component.TeleportDuration, true);
        Timer.Spawn(component.TeleportDuration,
        () =>
        {
            if (!EntityManager.EntityExists(uid))
                return;

            component.IsTeleported = false;
            UpdateState(uid, component);
            _transform.SetCoordinates(uid, coord);
            _transform.AttachToGridOrMap(uid);

            if (updatePortal)
            {
                if (TryComp<SCP106TrapComponent>(component.Portal, out var trap))
                {
                    trap.IsExit = false;
                    _TrapSCP.UpdateState(component.Portal.Value);
                }
            }
        });
    }

    private void UpdateState(EntityUid uid, SCP106Component? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.IsTeleported)
        {
            _appearance.SetData(uid, SCP106Visuals.State, false);
            _appearance.SetData(uid, SCP106Visuals.Teleported, true);
        }
        else
        {
            _appearance.SetData(uid, SCP106Visuals.State, true);
            _appearance.SetData(uid, SCP106Visuals.Teleported, false);
        }
    }
}
