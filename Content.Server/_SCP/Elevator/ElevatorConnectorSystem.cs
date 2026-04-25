using Content.Server._SCP.Elevator.Components;
using Content.Shared.Doors.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Interaction;
using Content.Shared.Popups;

namespace Content.Server._SCP.Elevator;

public sealed class ElevatorConnectorSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ElevatorConnectorComponent, AfterInteractEvent>(OnScannerAfterInteract);
        SubscribeLocalEvent<ElevatorConnectorComponent, UseInHandEvent>(OnAfterInteract);
    }

    private void OnAfterInteract(EntityUid uid, ElevatorConnectorComponent component, UseInHandEvent args)
    {
        component.Door = null;
        component.Elevator = null;

        _popup.PopupEntity(Loc.GetString("connector-reset"), args.User, args.User);
    }

    private void OnScannerAfterInteract(EntityUid uid, ElevatorConnectorComponent component, AfterInteractEvent args)
    {
        if (args.Target is not { } target)
            return;

        if (TryComp<DoorComponent>(args.Target, out _))
        {
            if (component.Door != null)
            {
                return;
            }
            else
            {
                component.Door = args.Target;
                _popup.PopupEntity(Loc.GetString("connector-first-door"), args.User, args.User);
                return;
            }
        }

        if (TryComp<ElevatorComponent>(args.Target, out var elevator))
        {
            if (component.Door != null)
            {
                elevator.ElevatorDoor = component.Door;
                component.Door = null;
                _popup.PopupEntity(Loc.GetString("connector-second-door"), args.User, args.User);
                return;
            }

            if (component.Elevator != null)
            {
                if (TryComp<ElevatorComponent>(component.Elevator.Value, out var elevator2))
                {
                    elevator.ConnectElevator = component.Elevator;
                    elevator2.ConnectElevator = args.Target;
                }
                component.Elevator = null;
                _popup.PopupEntity(Loc.GetString("connector-second-elevator"), args.User, args.User);
                return;
            }
            else
            {
                component.Elevator = args.Target;
                _popup.PopupEntity(Loc.GetString("connector-first-elevator"), args.User, args.User);
                return;
            }
        }
    }
}
