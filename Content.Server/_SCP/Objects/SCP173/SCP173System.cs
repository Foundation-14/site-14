using Robust.Shared.Timing;
using Content.Shared._SCP.SCP173;
using Robust.Shared.Audio.Systems;
using Content.Server._SCP.Blinking.Components;
using Content.Shared.NPC.Systems;
using Content.Shared.Actions;
using Content.Shared.Interaction;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Content.Shared.Mind;
using Content.Server._SCP.Blinking;
using System.Linq;
using Content.Shared.Mobs.Systems;
using Content.Shared.Damage;
using Content.Shared.Eye;
using Content.Shared.Damage.Systems;
using Content.Shared.Mind.Components;
using Content.Shared.Examine;
using Content.Shared.Mobs.Components;
using Robust.Server.Player;
using Content.Shared.Damage.Components;

namespace Content.Server._SCP.SCP173;

public sealed partial class SCP173System : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;=
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private BlinkingSystem _blinking = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private ExamineSystemShared _examine = default!;
    [Dependency] private IPlayerManager _playerManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SCP173Component, ComponentInit>(OnInit);
        SubscribeLocalEvent<SCP173Component, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<SCP173Component, ComponentStartup>(OnStart);
        SubscribeLocalEvent<SCP173Component, SCP173PointSelectEvent>(OnPointSelect);
        SubscribeLocalEvent<SCP173Component, SCP173BlindEvent>(OnBlind);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<SCP173Component>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (_timing.CurTime > component.TimeUtilUpdate)
            {
                component.TimeUtilUpdate = _timing.CurTime + component.UpdateDuration;
                UpdateSCP173(uid, component);
            }
        }
    }

    private void OnInit(EntityUid uid, SCP173Component component, ComponentInit args)
    {
        _actions.AddAction(uid, ref component.SCP173DashActionEntity, component.SCP173DashAction, uid);
        _actions.AddAction(uid, ref component.SCP173BlindActionEntity, component.SCP173BlindAction, uid);
    }

    private void OnShutdown(EntityUid uid, SCP173Component component, ComponentShutdown args)
    {
        _actions.RemoveAction(uid, component.SCP173DashActionEntity);
        _actions.RemoveAction(uid, component.SCP173BlindActionEntity);
    }

    private void OnStart(EntityUid uid, SCP173Component component, ComponentStartup args)
    {
        component.TimeUtilUpdate = _timing.CurTime + component.UpdateDuration;
    }

    private void OnBlind(EntityUid uid, SCP173Component component, ref SCP173BlindEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (args.Handled)
            return;

        var ents = _lookup.GetEntitiesInRange<BlinkingComponent>(_transform.GetMapCoordinates(uid, Transform(uid)), component.Range);
        var validEntities = ents
            .Where(ent => _examine.InRangeUnOccluded(_transform.ToMapCoordinates(Transform(uid).Coordinates), _transform.ToMapCoordinates(Transform(ent).Coordinates), component.Range, null) && ent.Owner != uid).ToList();

        foreach (var ent in validEntities)
        {
            if (component.SoundSCP173Blind != null &&
                TryComp<MindContainerComponent>(ent, out var mindContainer) &&
                mindContainer.Mind != null &&
                TryComp<MindComponent>(mindContainer.Mind, out var mindComp) &&
                mindComp.UserId != null)
            {
                if (_playerManager.TryGetSessionById(mindComp.UserId.Value, out var session))
                {
                    Filter playerFilter = Filter.Empty();
                    playerFilter.AddPlayer(session);
                    _audio.PlayGlobal(component.SoundSCP173Blind, playerFilter, false);
                }
            }

            _blinking.Blind(ent, component.BlindDuration);
        }

        args.Handled = true;
    }

    private void OnPointSelect(EntityUid uid, SCP173Component component, ref SCP173PointSelectEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (args.Handled)
            return;

        var user = args.Performer;
        args.Handled = true;

        var origin = Transform(uid).MapPosition;
        var target = args.Target.ToMap(EntityManager, _transform);

        if (!_interaction.InRangeUnobstructed(origin, target, 0f, CollisionGroup.Impassable | CollisionGroup.AirlockLayer, uid => uid == user))
        {
            _popup.PopupEntity(Loc.GetString("scp173-range-unobstructed"), uid, uid);
            return;
        }

        _popup.PopupEntity(Loc.GetString("Вы выбрали точку перемещения!"), uid, uid, PopupType.LargeCaution);
        component.Point = args.Target;

        args.Handled = true;
    }

    private void UpdateSCP173(EntityUid uid, SCP173Component? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (_mobState.IsCritical(uid))
            return;

        if (!SeeTheStatue(uid, component))
        {
            Dash(uid, component.Point, component);
            KillNearest(uid, component);
        }
    }

    private void Dash(EntityUid uid, EntityCoordinates? point = null, SCP173Component? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (point == null)
            return;

        _transform.SetCoordinates(uid, point.Value);
        _transform.AttachToGridOrMap(uid);

        if (component.SoundStepNoises != null)
            _audio.PlayPvs(component.SoundStepNoises, uid);

        component.Point = null;
    }

    public bool SeeTheStatue(EntityUid uid, SCP173Component? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        var ents = _lookup.GetEntitiesInRange<BlinkingComponent>(_transform.GetMapCoordinates(uid, Transform(uid)), component.Range);
        var validEntities = ents
            .Where(ent => _examine.InRangeUnOccluded(_transform.ToMapCoordinates(Transform(uid).Coordinates), _transform.ToMapCoordinates(Transform(ent).Coordinates), component.Range, null) && ent.Owner != uid).ToList();

        foreach (var ent in validEntities)
        {
            if (!_blinking.EyeIsClosed(ent) && _blinking.IsTriggerVisible(ent, uid))
                return true;
        }
        return false;
    }

    public void KillNearest(EntityUid uid, SCP173Component? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var selfMapCoords = _transform.GetMapCoordinates(uid);
        var selfPosition = selfMapCoords.Position;
        var entitiesInRange = _lookup.GetEntitiesInRange(selfMapCoords, component.KillRange);

        EntityUid? closestEntity = null;
        float closestDistanceSquared = float.MaxValue;

        foreach (var entity in entitiesInRange)
        {
            if (entity == uid)
                continue;

            if (!TryComp<MobStateComponent>(entity, out var mobState))
                continue;

            if (_mobState.IsCritical(entity, mobState) || _mobState.IsDead(entity, mobState))
                continue;

            var otherMapCoords = _transform.GetMapCoordinates(entity);
            var otherPosition = otherMapCoords.Position;
            float distanceSquared = (selfPosition - otherPosition).LengthSquared();

            if (distanceSquared < closestDistanceSquared)
            {
                closestDistanceSquared = distanceSquared;
                closestEntity = entity;
            }
        }

        if (closestEntity == null)
            return;

        if (component.SoundDamage != null)
            _audio.PlayPvs(component.SoundDamage, uid);

        _damageable.TryChangeDamage(closestEntity.Value, component.Damage, true, false);
    }
}
