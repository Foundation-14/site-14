using Content.Server._SCP.Elevator.Components;
using Content.Server._SCP.TeslaGate.Components;
using Content.Shared._SCP.SCP106.Components;
using Content.Shared.Doors.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using System;

namespace Content.Server._SCP.Elevator;

public sealed class ElevatorConnectorSystem : EntitySystem
{
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private ElevatorSystem _elevatorSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ElevatorConnectorComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<ElevatorConnectorComponent, UseInHandEvent>(OnUseInHand);
    }

    private void OnUseInHand(EntityUid uid, ElevatorConnectorComponent component, UseInHandEvent args)
    {
        component.FirstElevator = null;
        component.PendingDoor = null;
        component.TeslaGate = null;
        _popup.PopupEntity(Loc.GetString("connector-reset"), args.User, args.User);
    }

    private void OnAfterInteract(EntityUid uid, ElevatorConnectorComponent component, AfterInteractEvent args)
    {
        if (args.Target is not { } target)
            return;

        if (TryComp<SCP106FemurBreakerComponent>(target, out var femurBreaker))
        {
            if (component.PendingDoor != null)
            {
                femurBreaker.BreakerDoor = component.PendingDoor;
                component.PendingDoor = null;
                _popup.PopupEntity(Loc.GetString("connector-femur-breaker"), args.User, args.User);
                return;
            }
        }

        if (TryComp<ElevatorComponent>(target, out _))
        {
            if (component.FirstElevator == null)
            {
                component.FirstElevator = target;
                _popup.PopupEntity(Loc.GetString("connector-first-elevator"), args.User, args.User);
            }
            else if (component.FirstElevator == target)
            {
                _popup.PopupEntity(Loc.GetString("connector-same-elevator"), args.User, args.User);
            }
            else
            {
                var groupId = $"ElevatorGroup_{Guid.NewGuid().ToString("N").Substring(0, 8)}";
                if (TryComp<ElevatorComponent>(component.FirstElevator.Value, out var firstComp))
                    firstComp.ElevatorGroupId = groupId;
                if (TryComp<ElevatorComponent>(target, out var secondComp))
                    secondComp.ElevatorGroupId = groupId;

                _elevatorSystem.TryLinkElevators(component.FirstElevator.Value, target);

                _popup.PopupEntity(Loc.GetString("connector-second-elevator"), args.User, args.User);
                component.FirstElevator = null;
            }
            return;
        }

        if (TryComp<DoorComponent>(target, out _))
        {
            if (component.FirstElevator != null)
            {
                if (TryComp<ElevatorComponent>(component.FirstElevator.Value, out var elev))
                {
                    elev.ElevatorDoor = target;
                    _popup.PopupEntity(Loc.GetString("connector-door-linked"), args.User, args.User);
                    component.FirstElevator = null;
                }
            }
            else
            {
                component.PendingDoor = target;
                _popup.PopupEntity(Loc.GetString("connector-door-pending"), args.User, args.User);
            }
            return;
        }

        if (TryComp<TeslaGateComponent>(target, out var teslaGate))
        {
            if (component.TeslaGate == null)
            {
                component.TeslaGate = target;
                _popup.PopupEntity(Loc.GetString("connector-tesla-first"), args.User, args.User);
                return;
            }
            else
            {
                teslaGate.ConnectTeslaGate = component.TeslaGate;
                component.TeslaGate = null;
                _popup.PopupEntity(Loc.GetString("connector-tesla-second"), args.User, args.User);
                return;
            }
        }

        if (component.PendingDoor != null && TryComp<ElevatorComponent>(target, out var pendingElev))
        {
            pendingElev.ElevatorDoor = component.PendingDoor;
            _popup.PopupEntity(Loc.GetString("connector-door-linked"), args.User, args.User);
            component.PendingDoor = null;
        }
    }
}
