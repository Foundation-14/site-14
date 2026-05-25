using Content.Server.Chat.Systems;
using Content.Server.DeviceLinking.Systems;
using Content.Server.Doors.Systems;
using Content.Server.Station.Systems;
using Content.Shared._SCP.SCP106.Components;
using Content.Shared.Audio;
using Content.Shared.Chat;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Damage.Components;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Doors.Components;
using Content.Shared.Humanoid;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Speech.Components;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using System.Linq;


namespace Content.Server._SCP.SCP106;

public sealed class SCP106FemurBreakerSystem : EntitySystem
{
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private DoorSystem _doorSystem = default!;
    [Dependency] private DeviceLinkSystem _signalSystem = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SCP106System _SCP = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private StatusEffectsSystem _statusEffect = default!;
    [Dependency] private StationSystem _stationSystem = default!;
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SCP106FemurBreakerComponent, SignalReceivedEvent>(OnSignalReceived);
        SubscribeLocalEvent<SCP106FemurBreakerComponent, ComponentInit>(OnInit);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var bird = EntityQueryEnumerator<SCP106FemurBreakerComponent>();
        while (bird.MoveNext(out var uid, out var breakerComp))
        {
            if (breakerComp.IsTrappedVictim)
            {
                VictimDamage(uid, breakerComp);
            }

            if (_timing.CurTime > breakerComp.TimeWork && breakerComp.IsWork)
            {
                breakerComp.IsWork = false;
                foreach (var trap in breakerComp.SCPTraps)
                {
                    QueueDel(trap);
                }
                breakerComp.SCPTraps.Clear();
            }
        }
    }
    private void OnInit(EntityUid uid, SCP106FemurBreakerComponent component, ComponentInit args)
    {
        _signalSystem.EnsureSinkPorts(uid, component.ActivateSCP106FemurBreakerPort);
    }

    private void OnSignalReceived(EntityUid uid, SCP106FemurBreakerComponent component, ref SignalReceivedEvent args)
    {
        if (args.Port == component.ActivateSCP106FemurBreakerPort)
        {
            TryRunning(uid, component);
        }
    }

    public bool TryRunning(EntityUid uid, SCP106FemurBreakerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (!component.IsActive)
        {
            if (component.SoundNoActive != null)
                _audio.PlayPvs(component.SoundNoActive, uid);

            _popup.PopupEntity(Loc.GetString("breaker-deactive"), uid, PopupType.LargeCaution);
            return false;
        }

        if (component.BreakerDoor == null)
        {
            if (component.SoundNoActive != null)
                _audio.PlayPvs(component.SoundNoActive, uid);

            _popup.PopupEntity(Loc.GetString("breaker-no-door"), uid, PopupType.LargeCaution);
            return false;
        }

        if (!TryComp<DoorComponent>(component.BreakerDoor, out var doorComp))
            return false;

        if (doorComp.State == DoorState.Open)
        {
            if (component.SoundNoActive != null)
                _audio.PlayPvs(component.SoundNoActive, uid);

            _popup.PopupEntity(Loc.GetString("breaker-door-not-closed"), uid, PopupType.LargeCaution);
            return false;
        }

        Running(uid, component);

        return true;
    }

    private void ClooseDoor(EntityUid uid, SCP106FemurBreakerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.BreakerDoor == null)
            return;

        var door = component.BreakerDoor.Value;

        if (!TryComp<DoorComponent>(door, out var doorComp))
            return;

        if (doorComp.State == DoorState.Open)
            _doorSystem.TryClose(door, doorComp);

        if (!TryComp<DoorBoltComponent>(door, out var bolts))
            return;

        _doorSystem.SetBoltsDown((door, bolts), true);
    }

    private void OpenDoor(EntityUid uid, SCP106FemurBreakerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.BreakerDoor == null)
            return;

        var door = component.BreakerDoor.Value;

        if (!TryComp<DoorComponent>(door, out var doorComp))
            return;

        if (doorComp.State != DoorState.Open)
            _doorSystem.TryOpen(door, doorComp);

        if (!TryComp<DoorBoltComponent>(door, out var bolts))
            return;

        _doorSystem.SetBoltsDown((door, bolts), false);
    }

    private void Running(EntityUid uid, SCP106FemurBreakerComponent component)
    {
        if (component.BreakerDoor == null)
            return;

        var ents = _lookup.GetEntitiesInRange<HumanoidProfileComponent>(
            _transform.GetMapCoordinates(uid, Transform(uid)), component.Range).ToList();

        if (ents.Count < 1)
        {
            if (component.SoundNoActive != null)
                _audio.PlayPvs(component.SoundNoActive, uid);

            _popup.PopupEntity(Loc.GetString("breaker-no-victim"), uid, PopupType.LargeCaution);
            return;
        }

        if (ents.Count > 1)
        {
            if (component.SoundNoActive != null)
                _audio.PlayPvs(component.SoundNoActive, uid);

            _popup.PopupEntity(Loc.GetString("breaker-more-than-one"), uid, PopupType.LargeCaution);
            return;
        }

        component.Target = ents[0];

        if (!_mobState.IsAlive(component.Target.Value))
        {
            if (component.SoundNoActive != null)
                _audio.PlayPvs(component.SoundNoActive, uid);

            _popup.PopupEntity(Loc.GetString("breaker-victim-dead"), uid, PopupType.LargeCaution);
            return;
        }

        var scpQuery = EntityQueryEnumerator<SCP106Component>();
        while (scpQuery.MoveNext(out var scpUid, out var scpComp))
        {
            component.TargetSCP = scpUid;
            _SCP.Teleport(scpUid, Transform(uid).Coordinates, false, scpComp);
            break;
        }

        var trapsQuery = EntityQueryEnumerator<SCP106TrapComponent>();
        while (trapsQuery.MoveNext(out var trapUid, out _))
        {
            if (HasComp<SCP106TransitionComponent>(trapUid))
                continue;

            if (!component.SCPTraps.Contains(trapUid))
            {
                component.SCPTraps.Add(trapUid);
            }
        }

        component.NextTickUtilPrison = _timing.CurTime + component.WorkDuration;
        component.IsTrappedVictim = true;

        ClooseDoor(uid, component);

        var msg = new GameGlobalSoundEvent(component.SoundCryPath, AudioParams.Default);
        var stationFilter = _stationSystem.GetInOwningStation(uid);
        stationFilter.AddPlayersByPvs(uid, entityManager: EntityManager);
        RaiseNetworkEvent(msg, stationFilter);

        if (component.TargetSCP == null)
            return;

        _statusEffect.TryAddStatusEffect<StunnedComponent>(component.TargetSCP.Value, "Stun", component.WorkDuration, true);
        _stun.TryUpdateParalyzeDuration(component.Target.Value, component.WorkDuration);

        component.TimeWork = _timing.CurTime + component.WorkDuration;
        component.IsWork = true;
    }

    private void VictimDamage(EntityUid uid, SCP106FemurBreakerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (_timing.CurTime <= component.DamageTick)
            return;

        if (_timing.CurTime >= component.NextTickUtilPrison)
        {
            component.IsTrappedVictim = false;
            return;
        }

        EntityUid? prisonerTarget = component.Target;

        if (!TryComp<DamageableComponent>(prisonerTarget, out var damageable))
            return;

        _damageable.TryChangeDamage((prisonerTarget.Value, damageable), component.Damage, false, false);

        if (TryComp<VocalComponent>(prisonerTarget, out var vocal))
        {
            EmoteSoundsPrototype? emotes = null;
            if (vocal.EmoteSounds is not null)
                _prototype.TryIndex(vocal.EmoteSounds.Value, out emotes);

            if (emotes is not null)
            {
                var random = new Random();
                var emoteId = random.Next(0, 5) < 1 ? "Crying" : "Scream";
                var emote = _prototype.Index<EmotePrototype>(emoteId);
                _chat.TryPlayEmoteSound(prisonerTarget.Value, emotes, emote);
            }
        }

        if (component.DamageSound != null)
            _audio.PlayPvs(component.DamageSound, uid);

        component.DamageTick = _timing.CurTime + TimeSpan.FromSeconds(1f);
    }
}
