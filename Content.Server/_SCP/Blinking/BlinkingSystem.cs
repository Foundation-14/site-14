using Robust.Shared.Timing;
using Content.Server._SCP.Blinking.Components;
using Content.Shared._SCP.Viewcone;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Examine;
using Content.Shared.Actions;
using System.Linq;

namespace Content.Server._SCP.Blinking;

public sealed class BlinkingSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private ExamineSystemShared _examine = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BlinkingComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<BlinkingComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<BlinkingComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<BlinkingComponent, ToggleHoldBlinkActionEvent>(OnToggleHoldAction);
    }

    private void OnStartup(EntityUid uid, BlinkingComponent component, ComponentStartup args)
    {
        if (HasComp<Content.Shared.Eye.Blinding.Components.EyeClosingComponent>(uid))
            RemComp<Content.Shared.Eye.Blinding.Components.EyeClosingComponent>(uid);

        component.IsBlinking = false;
        component.IsBlinded = false;
        component.BlinkEndTime = TimeSpan.Zero;
        component.BlindedUntil = TimeSpan.Zero;
        component.StoredConeAngle = null;
        component.NextTriggerBlinkTime = TimeSpan.Zero;
    }

    private void OnShutdown(EntityUid uid, BlinkingComponent component, ComponentShutdown args)
    {
        RestoreConeAngle(uid, component);
    }

    private void OnMobStateChanged(EntityUid uid, BlinkingComponent component, MobStateChangedEvent args)
    {
        if (_mobState.IsDead(uid) || _mobState.IsCritical(uid))
        {
            component.HoldEyeClose = true;
            SetWideAngle(uid, true, component);
        }
        else
        {
            component.HoldEyeClose = false;
            RestoreConeAngle(uid, component);
        }
    }

    private void OnToggleHoldAction(EntityUid uid, BlinkingComponent component, ToggleHoldBlinkActionEvent args)
    {
        if (args.Handled)
            return;

        component.HoldEyeClose = !component.HoldEyeClose;
        if (component.HoldEyeClose)
        {
            SetWideAngle(uid, true, component);
        }
        else
        {
            RestoreConeAngle(uid, component);
        }
        args.Handled = true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var curTime = _timing.CurTime;

        var query = EntityQueryEnumerator<BlinkingComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (component.IsBlinded && curTime >= component.BlindedUntil)
            {
                component.IsBlinded = false;
                if (!component.HoldEyeClose && !component.IsBlinking)
                    RestoreConeAngle(uid, component);
            }

            if (component.IsBlinded || component.HoldEyeClose)
                continue;

            if (component.IsBlinking && curTime >= component.BlinkEndTime)
            {
                RestoreConeAngle(uid, component);
                component.IsBlinking = false;
            }

            if (component.IsBlinking)
                continue;

            if (IsTriggerInViewCone(uid, component) && curTime >= component.NextTriggerBlinkTime)
            {
                StartBlink(uid, component);
                component.NextTriggerBlinkTime = curTime + component.TimeBetweenTriggerBlinks;
            }
        }
    }

    private void StartBlink(EntityUid uid, BlinkingComponent component)
    {
        if (component.IsBlinking || component.IsBlinded)
            return;

        SetWideAngle(uid, true, component);
        component.IsBlinking = true;
        component.BlinkEndTime = _timing.CurTime + component.BlinkingDuration;

        var ev = new BlinkingEvent(uid);
        RaiseLocalEvent(uid, ref ev);
    }

    private void SetWideAngle(EntityUid uid, bool wide, BlinkingComponent component)
    {
        if (!TryComp<SCPViewconeComponent>(uid, out var viewcone))
            return;

        if (wide)
        {
            if (component.StoredConeAngle == null)
                component.StoredConeAngle = viewcone.ConeAngle;
            viewcone.IgnoreCone = true;
            viewcone.ConeAngle = 0.01f;
            Dirty(uid, viewcone);
        }
        else
        {
            RestoreConeAngle(uid, component);
        }
    }

    private void RestoreConeAngle(EntityUid uid, BlinkingComponent component)
    {
        if (!TryComp<SCPViewconeComponent>(uid, out var viewcone))
            return;

        viewcone.IgnoreCone = false;
        if (component.StoredConeAngle != null)
        {
            viewcone.ConeAngle = component.StoredConeAngle.Value;
            component.StoredConeAngle = null;
            Dirty(uid, viewcone);
        }
    }

    public bool IsTriggerVisible(EntityUid player, EntityUid trigger, BlinkingComponent? component = null)
    {
        if (!Resolve(player, ref component, false))
            return false;
        if (!HasComp<BlinkingTriggerComponent>(trigger))
            return false;
        return IsTriggerInViewCone(player, component, specificTrigger: trigger);
    }

    private bool IsTriggerInViewCone(EntityUid player, BlinkingComponent component, EntityUid? specificTrigger = null)
    {
        if (!TryComp<SCPViewconeComponent>(player, out var viewcone))
            return false;

        if (viewcone.IgnoreCone || viewcone.ConeAngle >= 360f)
        {
            var xform = Transform(player);
            var eyePos = _transform.GetWorldPosition(xform);
            float maxDist = component.TriggerRange;

            var triggers = _lookup.GetEntitiesInRange<BlinkingTriggerComponent>(
                _transform.GetMapCoordinates(player, xform), maxDist);

            foreach (var triggerEnt in triggers)
            {
                if (specificTrigger != null && triggerEnt.Owner != specificTrigger)
                    continue;
                if (triggerEnt.Owner == player)
                    continue;

                var trigXform = Transform(triggerEnt.Owner);
                var trigPos = _transform.GetWorldPosition(trigXform);
                if ((trigPos - eyePos).Length() > maxDist)
                    continue;

                var fromMap = _transform.ToMapCoordinates(xform.Coordinates);
                var toMap = _transform.ToMapCoordinates(trigXform.Coordinates);
                if (_examine.InRangeUnOccluded(fromMap, toMap, maxDist, null))
                    return true;
            }
            return false;
        }

        var xform2 = Transform(player);
        var eyePos2 = _transform.GetWorldPosition(xform2);
        var eyeRot = _transform.GetWorldRotation(player);
        float halfConeRad = MathHelper.DegreesToRadians(viewcone.ConeAngle) * 0.5f;
        float maxDist2 = component.TriggerRange;

        var triggers2 = _lookup.GetEntitiesInRange<BlinkingTriggerComponent>(
            _transform.GetMapCoordinates(player, xform2), maxDist2);

        foreach (var triggerEnt in triggers2)
        {
            if (specificTrigger != null && triggerEnt.Owner != specificTrigger)
                continue;
            if (triggerEnt.Owner == player)
                continue;

            var trigXform = Transform(triggerEnt.Owner);
            var trigPos = _transform.GetWorldPosition(trigXform);
            var direction = trigPos - eyePos2;
            float distance = direction.Length();
            if (distance > maxDist2)
                continue;

            var angleToTrigger = direction.ToWorldAngle();
            var angleDiff = Angle.ShortestDistance(angleToTrigger, eyeRot);
            if (Math.Abs(angleDiff.Theta) > halfConeRad)
                continue;

            var fromMap = _transform.ToMapCoordinates(xform2.Coordinates);
            var toMap = _transform.ToMapCoordinates(trigXform.Coordinates);
            if (!_examine.InRangeUnOccluded(fromMap, toMap, maxDist2, null))
                continue;

            return true;
        }
        return false;
    }

    public void Blind(EntityUid uid, TimeSpan duration, BlinkingComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.IsBlinded = true;
        component.BlindedUntil = _timing.CurTime + duration;
        SetWideAngle(uid, true, component);
    }

    public bool EyeIsClosed(EntityUid uid, BlinkingComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return false;

        return component.IsBlinking || component.HoldEyeClose || component.IsBlinded;
    }
}

public sealed partial class ToggleHoldBlinkActionEvent : InstantActionEvent;
