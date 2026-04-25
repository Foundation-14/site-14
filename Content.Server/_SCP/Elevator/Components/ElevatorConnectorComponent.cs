namespace Content.Server._SCP.Elevator.Components;

[RegisterComponent]
public sealed partial class ElevatorConnectorComponent : Component
{
    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? Elevator;

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? Door;
}
