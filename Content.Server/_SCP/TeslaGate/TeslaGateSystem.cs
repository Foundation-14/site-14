using Robust.Shared.Audio.Systems;
using Content.Server._SCP.TeslaGate.Components;
using Content.Server.Beam;
using Robust.Shared.Timing;
using Content.Server.DeviceLinking.Systems;
using Content.Shared.DeviceLinking.Events;

namespace Content.Server._SCP.TeslaGate;

public sealed partial class TeslaGateSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private BeamSystem _beam = default!;
    [Dependency] private DeviceLinkSystem _signalSystem = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TeslaGateComponent, SignalReceivedEvent>(OnSignalReceived);
        SubscribeLocalEvent<TeslaGateComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<TeslaGateComponent, TeslaGateLightingEvent>(OnLighting);
    }

    private void OnInit(EntityUid uid, TeslaGateComponent component, ComponentInit args)
    {
        _signalSystem.EnsureSinkPorts(uid, component.ToggleTeslaGatePort);
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
            if (component.IsActive && _timing.CurTime > component.TimeUtilRunning)
            {
                component.TimeUtilRunning = _timing.CurTime + component.RunningDuration;
                TryTeslaGateLighting(uid, component);
            }

            if (component.IsTimeLighting && _timing.CurTime > component.TimeUtilLighting && component.ConnectTeslaGate != null)
            {
                _beam.TryCreateBeam(uid, component.ConnectTeslaGate.Value, "TeslaGateLightning");
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

        return true;
    }

    public void ToggleTeslaGate(EntityUid uid, TeslaGateComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.IsActive = !component.IsActive;

        if (component.IsActive)
            component.TimeUtilRunning = _timing.CurTime + component.RunningDuration;
    }
}
