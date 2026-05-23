using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Audio;
using Content.Shared.DeviceLinking;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server._SCP.TeslaGate.Components;

[RegisterComponent]
public sealed partial class TeslaGateComponent : Component
{
    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<SinkPortPrototype>))]
    public string ToggleTeslaGatePort = "ToggleTeslaGate";

    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan LightingDuration = TimeSpan.FromSeconds(1.8f);

    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan TimeUtilLighting = TimeSpan.Zero;

    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan RunningDuration = TimeSpan.FromSeconds(5.5f);

    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan TimeUtilRunning = TimeSpan.Zero;

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? ConnectTeslaGate = null;

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool IsActive = false;

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public bool IsTimeLighting = false;

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public SoundSpecifier? SoundsBeforeLighting = new SoundPathSpecifier("/Audio/_SCP/Effects/TeslaGate/tesla_gate.ogg");

}

[ByRefEvent]
public readonly record struct TeslaGateLightingEvent();
