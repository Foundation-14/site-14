namespace Content.Server._SCP.Elevator.Components;

[RegisterComponent]
public sealed partial class ElevatorConnectorComponent : Component
{
    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? TeslaGate;

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? Elevator;

    [ViewVariables]
    public EntityUid? FirstElevator;

    [ViewVariables]
    public EntityUid? PendingDoor;

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? Door;
}
