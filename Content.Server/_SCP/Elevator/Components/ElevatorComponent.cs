using Content.Shared.DeviceLinking;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server._SCP.Elevator.Components;

[RegisterComponent]
public sealed partial class ElevatorComponent : Component
{
    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<SinkPortPrototype>))]
    public string CallElevatorPort = "CallElevator";

    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<SinkPortPrototype>))]
    public string ReviewElevatorPort = "ReviewElevator";

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? ConnectElevator;

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? ElevatorDoor;

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public SoundSpecifier? SoundElevator = default!;

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public SoundSpecifier? SoundElevatorNoActive = default!;

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan TravelDuration = TimeSpan.FromSeconds(11);

    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan TimeUntilStopTravel = TimeSpan.Zero;

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan CallDuration = TimeSpan.FromSeconds(2);

    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan TimeUntilCall = TimeSpan.Zero;

    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan TravelTime = TimeSpan.Zero;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public bool IsCallPort = false;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public bool IsActive = true;

    [ViewVariables(VVAccess.ReadOnly)]
    public bool IsTravel = false;

    [ViewVariables(VVAccess.ReadOnly)]
    public bool IsCall = false;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public float Range = 2f;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public float TransportedWeight = 500;

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public float DoorCloseDelay = 2f;

    [ViewVariables(VVAccess.ReadOnly)]
    public bool IsWaitingToDepart = false;

    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan? DepartureTime = null;

    [DataField]
    public EntityUid? CurrentFloor;

    [DataField]
    public EntityUid? CalledFrom;
}
