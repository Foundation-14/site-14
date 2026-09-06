using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Audio;
using Content.Shared.DeviceLinking;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Prototypes;

namespace Content.Server._SCP.TeslaGate.Components;

[RegisterComponent]
public sealed partial class TeslaGateComponent : Component
{
    [DataField]
    public string ToggleTeslaGatePort = "ToggleTeslaGate";

    [DataField]
    public TimeSpan LightingDuration = TimeSpan.FromSeconds(1.8f);

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan TimeUtilLighting = TimeSpan.Zero;

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public EntProtoId SensorPrototype = "TeslaGateSensor";

    [DataField]
    public float DetectionWidth = 1.5f;

    [DataField]
    public TimeSpan CooldownDuration = TimeSpan.FromSeconds(2f);

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan NextZapTime = TimeSpan.Zero;

    [DataField]
    public List<EntityUid> Sensors = new();

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
    public SoundSpecifier? SoundsBeforeLighting = new SoundPathSpecifier("/Audio/_SCP/Effects/TeslaGate/tesla_gate.ogg");

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public EntProtoId ZapBeamEntityId = "TeslaGateLightning";

}

[ByRefEvent]
public readonly record struct TeslaGateLightingEvent();
