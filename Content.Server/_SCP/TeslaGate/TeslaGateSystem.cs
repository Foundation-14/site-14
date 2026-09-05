using Robust.Shared.Audio.Systems;
using Content.Server._SCP.TeslaGate.Components;
using Robust.Shared.Timing;
using Content.Server.DeviceLinking.Systems;
using Content.Shared.DeviceLinking.Events;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics;
using Content.Shared.Physics;
using Robust.Shared.Prototypes;
using Content.Shared.Beam.Components;
using Content.Shared.Audio;
using Content.Shared.Mobs.Components;
using Robust.Shared.Physics.Events;

namespace Content.Server._SCP.TeslaGate;

public sealed partial class TeslaGateSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private DeviceLinkSystem _signalSystem = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private FixtureSystem _fixture = default!;
    [Dependency] private SharedBroadphaseSystem _broadphase = default!;
    [Dependency] private SharedAmbientSoundSystem _ambientSound = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TeslaGateComponent, SignalReceivedEvent>(OnSignalReceived);
        SubscribeLocalEvent<TeslaGateComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<TeslaGateComponent, TeslaGateLightingEvent>(OnLighting);
        SubscribeLocalEvent<TeslaGateComponent, ComponentShutdown>(OnShutdown);

        SubscribeLocalEvent<TeslaGateSensorComponent, StartCollideEvent>(OnSensorCollide);
    }

    private void OnInit(EntityUid uid, TeslaGateComponent component, ComponentInit args)
    {
        _signalSystem.EnsureSinkPorts(uid, component.ToggleTeslaGatePort);
        UpdateAmbientSound(uid, component);
        UpdateSensors(uid, component);
    }

    private void OnShutdown(EntityUid uid, TeslaGateComponent component, ComponentShutdown args)
    {
        RemoveSensors(component);
    }

    private void OnSignalReceived(EntityUid uid, TeslaGateComponent component, ref SignalReceivedEvent args)
    {
        if (args.Port == component.ToggleTeslaGatePort)
        {
            ToggleTeslaGate(uid, component);
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<TeslaGateComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (component.IsTimeLighting && _timing.CurTime > component.TimeUtilLighting && component.ConnectTeslaGate != null)
            {
                CreateLightning(uid, component.ConnectTeslaGate.Value, component.ZapBeamEntityId);
                component.IsTimeLighting = false;
            }
        }
    }

    private void OnLighting(EntityUid uid, TeslaGateComponent component, ref TeslaGateLightingEvent args)
    {
        if (component.ConnectTeslaGate == null)
            return;

        if (component.SoundsBeforeLighting != null)
            _audio.PlayPvs(component.SoundsBeforeLighting, uid);

        component.IsTimeLighting = true;
        component.TimeUtilLighting = _timing.CurTime + component.LightingDuration;
    }

    private void OnSensorCollide(EntityUid uid, TeslaGateSensorComponent sensor, ref StartCollideEvent args)
    {
        var otherEntity = args.OtherEntity;

        if (!HasComp<MobStateComponent>(otherEntity))
            return;

        if (sensor.GateUid == null || !Exists(sensor.GateUid.Value))
            return;

        var gateUid = sensor.GateUid.Value;
        if (!TryComp<TeslaGateComponent>(gateUid, out var gateComp))
            return;

        if (!gateComp.IsActive)
            return;

        if (_timing.CurTime < gateComp.NextZapTime)
            return;

        TryTeslaGateLighting(gateUid, gateComp);
        gateComp.NextZapTime = _timing.CurTime + gateComp.CooldownDuration;
    }

    public bool TryTeslaGateLighting(EntityUid uid, TeslaGateComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (!component.IsActive)
            return false;

        if (!Exists(component.ConnectTeslaGate))
            component.ConnectTeslaGate = null;

        if (component.ConnectTeslaGate == null)
            return false;

        TeslaGateLightingEvent ev = new TeslaGateLightingEvent();
        RaiseLocalEvent(uid, ref ev);

        return true;
    }

    public bool TryConnectTeslaGate(EntityUid uid, EntityUid ent, TeslaGateComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        component.ConnectTeslaGate = ent;
        UpdateSensors(uid, component);
        return true;
    }

    public void ToggleTeslaGate(EntityUid uid, TeslaGateComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.IsActive = !component.IsActive;
        UpdateAmbientSound(uid, component);
        UpdateSensors(uid, component);
    }

    private void UpdateAmbientSound(EntityUid uid, TeslaGateComponent component)
    {
        if (TryComp<AmbientSoundComponent>(uid, out var ambient))
        {
            _ambientSound.SetAmbience(uid, component.IsActive, ambient);
        }
    }

    private void UpdateSensors(EntityUid uid, TeslaGateComponent component)
    {
        RemoveSensors(component);

        if (!component.IsActive || component.ConnectTeslaGate == null)
            return;

        var sourcePos = _transform.GetWorldPosition(uid);
        var targetPos = _transform.GetWorldPosition(component.ConnectTeslaGate.Value);
        var distanceVec = targetPos - sourcePos;
        var length = distanceVec.Length();
        if (length < 0.01f)
            return;

        var dir = distanceVec / length;
        var steps = (int)Math.Floor(length);
        var isInteger = Math.Abs(length - steps) < 0.001f;
        var maxIndex = isInteger ? steps - 1 : steps;

        for (int i = 1; i <= maxIndex; i++)
        {
            var posVec = sourcePos + dir * i;
            var coords = new MapCoordinates(posVec, Transform(uid).MapID);
            var sensor = Spawn(component.SensorPrototype, coords);
            if (TryComp<TeslaGateSensorComponent>(sensor, out var sensorComp))
            {
                sensorComp.GateUid = uid;
            }
            component.Sensors.Add(sensor);
        }
    }

    private void RemoveSensors(TeslaGateComponent component)
    {
        foreach (var sensor in component.Sensors)
        {
            if (Exists(sensor))
                QueueDel(sensor);
        }
        component.Sensors.Clear();
    }

    private void CreateLightning(EntityUid source, EntityUid target, EntProtoId proto)
    {
        if (Deleted(source) || Deleted(target))
            return;

        var sourceMapPos = _transform.GetMapCoordinates(source);
        var targetMapPos = _transform.GetMapCoordinates(target);
        if (sourceMapPos.MapId != targetMapPos.MapId)
            return;

        var distanceVec = targetMapPos.Position - sourceMapPos.Position;
        var distanceLength = distanceVec.Length();
        if (distanceLength < 0.01f)
            return;

        var dir = distanceVec / distanceLength;
        var angle = distanceVec.ToWorldAngle();
        var steps = (int)Math.Floor(distanceLength);
        var isInteger = Math.Abs(distanceLength - steps) < 0.001f;
        var maxIndex = isInteger ? steps - 1 : steps;

        bool soundPlayed = false;

        for (int i = 1; i <= maxIndex; i++)
        {
            var posVec = sourceMapPos.Position + dir * i;
            var coords = new MapCoordinates(posVec, sourceMapPos.MapId);
            var ent = Spawn(proto, coords);
            _transform.SetWorldRotation(ent, angle);

            if (!soundPlayed && TryComp<BeamComponent>(ent, out var beam) && beam.Sound != null)
            {
                _audio.PlayPvs(beam.Sound, ent);
                soundPlayed = true;
            }

            if (!TryComp<PhysicsComponent>(ent, out var physics))
            {
                physics = EnsureComp<PhysicsComponent>(ent);
                _physics.SetBodyType(ent, BodyType.Static, body: physics);
            }

            var shape = new PhysShapeCircle(0.4f);
            _fixture.TryCreateFixture(
                ent,
                shape,
                "lightning_hitbox",
                hard: false,
                collisionMask: (int)CollisionGroup.MobLayer,
                collisionLayer: (int)CollisionGroup.ItemMask,
                body: physics);

            _broadphase.RegenerateContacts((ent, physics));
        }
    }
}
