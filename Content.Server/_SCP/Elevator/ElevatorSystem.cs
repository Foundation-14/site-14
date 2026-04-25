using Content.Server._SCP.Elevator.Components;
using Content.Server.DeviceLinking.Systems;
using Content.Server.Doors.Systems;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Doors.Components;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Timing;
using System.Linq;

namespace Content.Server._SCP.Elevator;

public sealed class ElevatorSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly DoorSystem _doorSystem = default!;
    [Dependency] private readonly DeviceLinkSystem _signalSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ElevatorComponent, SignalReceivedEvent>(OnSignalReceived);
        SubscribeLocalEvent<ElevatorComponent, ComponentInit>(OnInit);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ElevatorComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (component.ConnectElevator == null)
                continue;

            if (_timing.CurTime > component.TimeUntilStopTravel && component.IsTravel)
            {
                if (component.CalledFrom != null)
                {
                    OpenElevatorDoor(component.CalledFrom.Value);
                    component.CalledFrom = null;
                }
                else if (component.ConnectElevator != null)
                {
                    OpenElevatorDoor(component.ConnectElevator.Value);
                }

                component.IsTravel = false;
            }

            if (_timing.CurTime > component.TimeUntilCall && component.IsCall)
            {
                if (!TryComp<ElevatorComponent>(component.ConnectElevator, out var comp))
                    continue;

                component.IsActive = true;
                comp.IsActive = true;
                component.IsCall = false;
                component.IsWaitingToDepart = false;
                component.DepartureTime = null;
            }

            if (component.IsWaitingToDepart && component.DepartureTime != null && _timing.CurTime >= component.DepartureTime)
            {
                ExecuteDeparture(uid, component);
            }
        }
    }

    private void OnInit(EntityUid uid, ElevatorComponent component, ComponentInit args)
    {
        _signalSystem.EnsureSinkPorts(uid, component.CallElevatorPort, component.ReviewElevatorPort);
    }

    private void OnSignalReceived(EntityUid uid, ElevatorComponent component, ref SignalReceivedEvent args)
    {
        if (component.IsWaitingToDepart)
        {
            _popup.PopupEntity(Loc.GetString("elevator-already-moving"), uid, PopupType.MediumCaution);
            return;
        }

        if (args.Port == component.ReviewElevatorPort)
        {
            TryRunning(uid, component);
        }

        if (args.Port == component.CallElevatorPort)
        {
            if (component.ConnectElevator != null)
            {
                var cabinUid = component.ConnectElevator.Value;
                if (!TryComp<ElevatorComponent>(cabinUid, out var cabinComp))
                    return;

                cabinComp.CalledFrom = uid;

                TryRunning(cabinUid, cabinComp);
            }
        }
    }

    public bool TryRunning(EntityUid uid, ElevatorComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (component.IsWaitingToDepart)
        {
            _popup.PopupEntity(Loc.GetString("elevator-already-moving"), uid, PopupType.MediumCaution);
            return false;
        }

        if (!component.IsActive)
        {
            if (component.SoundElevatorNoActive != null)
                _audio.PlayPvs(component.SoundElevatorNoActive, uid);
            _popup.PopupEntity(Loc.GetString("elevator-no-active"), uid, PopupType.LargeCaution);
            return false;
        }

        if (component.ConnectElevator == null)
        {
            _popup.PopupEntity(Loc.GetString("elevator-no-second-elevator"), uid, PopupType.LargeCaution);
            return false;
        }

        if (component.ElevatorDoor == null)
        {
            if (component.SoundElevatorNoActive != null)
                _audio.PlayPvs(component.SoundElevatorNoActive, uid);
            _popup.PopupEntity(Loc.GetString("elevator-no-door"), uid, PopupType.LargeCaution);
            return false;
        }

        if (!TryComp<ElevatorComponent>(component.ConnectElevator, out var comp))
            return false;

        Running(uid, component);
        return true;
    }

    private bool TryCloseElevatorDoor(EntityUid uid, ElevatorComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (component.ElevatorDoor == null)
            return false;

        var door = component.ElevatorDoor.Value;
        if (!TryComp<DoorComponent>(door, out var doorComp))
            return false;

        if (doorComp.State == DoorState.Closed)
            return true;

        if (!_doorSystem.TryClose(door, doorComp))
            return false;

        if (TryComp<DoorBoltComponent>(door, out var bolts))
            _doorSystem.SetBoltsDown((door, bolts), true);

        return true;
    }

    private void OpenElevatorDoor(EntityUid uid, ElevatorComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.ElevatorDoor == null)
            return;

        var door = component.ElevatorDoor.Value;
        if (!TryComp<DoorComponent>(door, out var doorComp))
            return;

        if (doorComp.State != DoorState.Open)
            _doorSystem.TryOpen(door, doorComp);

        if (TryComp<DoorBoltComponent>(door, out var bolts))
            _doorSystem.SetBoltsDown((door, bolts), false);
    }

    private void Running(EntityUid uid, ElevatorComponent component)
    {
        if (component.ConnectElevator == null) return;
        if (component.ElevatorDoor == null) return;

        if (!TryComp<ElevatorComponent>(component.ConnectElevator, out var partnerComp))
            return;

        var originStation = component.CurrentFloor ?? component.ConnectElevator.Value;
        if (!TryComp<ElevatorComponent>(originStation, out var originComp))
            return;

        var destinationStation = component.CalledFrom ?? component.ConnectElevator.Value;
        if (!TryComp<ElevatorComponent>(destinationStation, out var destComp))
            return;

        if (!TryCloseElevatorDoor(uid, component) || !TryCloseElevatorDoor(originStation, originComp))
        {
            if (component.SoundElevatorNoActive != null)
                _audio.PlayPvs(component.SoundElevatorNoActive, uid);
            _popup.PopupEntity(Loc.GetString("elevator-door-blocked"), uid, PopupType.LargeCaution);
            return;
        }

        float totalMass = 0f;
        var entsDoor = _lookup.GetEntitiesInRange<PhysicsComponent>(
            _transform.GetMapCoordinates(component.ElevatorDoor.Value, Transform(component.ElevatorDoor.Value)),
            0.5f,
            LookupFlags.Dynamic | LookupFlags.Sundries | LookupFlags.Sensors).ToList();

        var ents = _lookup.GetEntitiesInRange<PhysicsComponent>(
            _transform.GetMapCoordinates(uid, Transform(uid)),
            component.Range,
            LookupFlags.Dynamic | LookupFlags.Sundries | LookupFlags.Sensors).ToList();

        var validEntities = ents
            .Where(ent => ent.Owner != uid && !entsDoor.Contains(ent))
            .ToList();

        foreach (var ent in validEntities)
        {
            if (TryComp<PhysicsComponent>(ent, out var phys))
                totalMass += phys.Mass;
        }

        if (totalMass > component.TransportedWeight)
        {
            if (component.SoundElevatorNoActive != null)
                _audio.PlayPvs(component.SoundElevatorNoActive, uid);
            _popup.PopupEntity(Loc.GetString("elevator-weight"), uid, PopupType.LargeCaution);
            return;
        }

        component.CalledFrom = destinationStation;

        component.IsWaitingToDepart = true;
        component.DepartureTime = _timing.CurTime + TimeSpan.FromSeconds(component.DoorCloseDelay);

        if (component.SoundElevator != null)
        {
            _audio.PlayPvs(component.SoundElevator, uid);
            _audio.PlayPvs(component.SoundElevator, destinationStation);
        }
    }

    private void ExecuteDeparture(EntityUid uid, ElevatorComponent component)
    {
        if (!component.IsWaitingToDepart) return;

        component.IsWaitingToDepart = false;
        component.DepartureTime = null;

        var destinationStation = component.CalledFrom;
        if (destinationStation == null) return;

        if (!TryComp<ElevatorComponent>(destinationStation.Value, out var destComp)) return;

        // Проверка, что двери всё ещё закрыты (как раньше) ...
        if (component.ElevatorDoor != null && TryComp<DoorComponent>(component.ElevatorDoor, out var door1) && door1.State != DoorState.Closed)
        {
            _popup.PopupEntity(Loc.GetString("elevator-door-opened-during-wait"), uid, PopupType.MediumCaution);
            OpenElevatorDoor(uid, component);
            if (component.CurrentFloor != null && TryComp<ElevatorComponent>(component.CurrentFloor, out var curComp))
                OpenElevatorDoor(component.CurrentFloor.Value, curComp);
            return;
        }
        if (destComp.ElevatorDoor != null && TryComp<DoorComponent>(destComp.ElevatorDoor, out var door2) && door2.State != DoorState.Closed)
        {
            _popup.PopupEntity(Loc.GetString("elevator-door-opened-during-wait"), uid, PopupType.MediumCaution);
            OpenElevatorDoor(uid, component);
            OpenElevatorDoor(destinationStation.Value, destComp);
            return;
        }

        var elevatorReceivingCoords = Transform(destinationStation.Value).Coordinates;

        var entsDoor = _lookup.GetEntitiesInRange<PhysicsComponent>(
            _transform.GetMapCoordinates(component.ElevatorDoor!.Value, Transform(component.ElevatorDoor.Value)),
            0.5f,
            LookupFlags.Dynamic | LookupFlags.Sundries | LookupFlags.Sensors).ToList();

        var ents = _lookup.GetEntitiesInRange<PhysicsComponent>(
            _transform.GetMapCoordinates(uid, Transform(uid)),
            component.Range,
            LookupFlags.Dynamic | LookupFlags.Sundries | LookupFlags.Sensors).ToList();

        var validEntities = ents
            .Where(ent => ent.Owner != uid && !entsDoor.Contains(ent))
            .ToList();

        foreach (var ent in entsDoor)
        {
            _transform.SetCoordinates(ent.Owner, elevatorReceivingCoords);
            _transform.AttachToGridOrMap(ent.Owner);
        }

        foreach (var ent in validEntities)
        {
            var entityCoords = Transform(ent.Owner).Coordinates;
            var offset = entityCoords.Position - Transform(uid).Coordinates.Position;
            var newCoords = elevatorReceivingCoords.Offset(offset);
            _transform.SetCoordinates(ent.Owner, newCoords);
            _transform.AttachToGridOrMap(ent.Owner);
        }

        component.CurrentFloor = destinationStation;

        component.IsActive = false;
        destComp.IsActive = false;

        component.IsTravel = true;
        component.TimeUntilStopTravel = _timing.CurTime + component.TravelDuration;
        component.IsCall = true;
        component.TimeUntilCall = _timing.CurTime + component.CallDuration + component.TravelDuration;
    }
}
