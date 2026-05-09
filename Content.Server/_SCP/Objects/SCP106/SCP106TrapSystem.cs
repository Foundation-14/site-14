using Content.Server._SCP.SpawnLabel;
using Content.Shared._SCP.SCP106.Components;
using Content.Shared._SCP.SCP106;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Doors.Components;
using Content.Shared.Mind.Components;
using Content.Shared.Mind;
using Content.Shared.NPC.Systems;
using Content.Shared.Stunnable;
using Robust.Server.Player;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Physics.Events;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using System.Linq;

namespace Content.Server._SCP.SCP106;

public sealed class SCP106TrapSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly SpawnLabelSystem _spawnLabel = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    public List<EntityUid> Targets = new List<EntityUid>();
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SCP106TrapComponent, StartCollideEvent>(OnEntityEnter);
    }

    private void OnEntityEnter(EntityUid uid, SCP106TrapComponent component, ref StartCollideEvent args)
    {
        var otherUid = args.OtherEntity;

        if (_npcFaction.IsFactionFriendly(component.FriendlyFaction, otherUid) || Targets.Contains(otherUid))
            return;

        HandleSound(uid, component.SoundTrap);

        if (TryComp<SCP106TransitionComponent>(uid, out var transition))
        {
            var random = _random.NextFloat();
            component.LabelKey = random <= transition.ExitShance ? transition.ExitKey : transition.EnterKey;
        }

        if (component.NeedStun)
        {
            StunAndTeleport(uid, otherUid, component);
        }
        else
        {
            Teleport(uid, otherUid, component);
        }
    }

    private void HandleSound(EntityUid uid, SoundSpecifier? sound = null)
    {
        if (sound != null)
        {
            _audio.PlayPvs(sound, uid);
        }
    }

    private void StunAndTeleport(EntityUid uid, EntityUid target, SCP106TrapComponent component)
    {
        _stun.TryUpdateParalyzeDuration(target, TimeSpan.FromSeconds(component.ParalyzeTime));

        HandlePersonalSound(target, component.SoundEnterPD);

        Timer.Spawn(TimeSpan.FromSeconds(component.ParalyzeTime++), () =>
        {
            if (!EntityManager.EntityExists(uid))
                return;

            Teleport(uid, target, component);
            _stun.TryUpdateParalyzeDuration(target, TimeSpan.FromSeconds(component.ParalyzeTime * 2));
        });
    }

    private void HandlePersonalSound(EntityUid target, SoundSpecifier? sound = null)
    {
        if (sound == null)
            return;

        if (TryComp<MindContainerComponent>(target, out var mind) 
            && TryComp<MindComponent>(mind.Mind, out var mindComp) 
            && mindComp.UserId != null)
        {
            if (_playerManager.TryGetSessionById(mindComp.UserId.Value, out var session))
            {
                var playerFilter = Filter.Empty().AddPlayer(session);
                _audio.PlayGlobal(sound, playerFilter, false);
            }
        }
    }

    public void Teleport(EntityUid uid, EntityUid target, SCP106TrapComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        Targets.Remove(target);

        if (!EntityManager.EntityExists(target))
            return;

        var ents = _lookup.GetEntitiesInRange(_transform.GetMapCoordinates(uid, Transform(uid)), 1f)
                        .Where(ent => ent == target)
                        .ToList();

        if (ents.Count > 0)
        {
            _spawnLabel.TeleportToLabel(component.LabelKey, target);

            if (component.NeedDamage)
                _damageable.TryChangeDamage(target, component.Damage);
        }
    }


    public void UpdateState(EntityUid uid, SCP106TrapComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.IsExit)
        {
            _appearance.SetData(uid, SCP106PortalVisuals.State, false);
            _appearance.SetData(uid, SCP106PortalVisuals.ExitState, true);
        }
        else
        {
            _appearance.SetData(uid, SCP106PortalVisuals.State, true);
            _appearance.SetData(uid, SCP106PortalVisuals.ExitState, false);
        }
    }
}
