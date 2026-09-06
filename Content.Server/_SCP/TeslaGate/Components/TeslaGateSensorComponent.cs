namespace Content.Server._SCP.TeslaGate.Components;

[RegisterComponent]
public sealed partial class TeslaGateSensorComponent : Component
{
    [DataField]
    public EntityUid? GateUid;
}
