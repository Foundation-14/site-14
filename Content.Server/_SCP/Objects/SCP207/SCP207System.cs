using Content.Shared._SCP.Objects.SCP207;
using Content.Shared._SCP.Objects.SCP207.Components;
using Content.Shared.Bed.Sleep;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._SCP.Objects.SCP207;

public sealed partial class SCP207System : SharedSCP207System
{
    private static readonly EntProtoId Drowsiness = "StatusEffectDrowsiness";

    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private StatusEffectsSystem _status = default!;
    [Dependency] private MovementModStatusSystem _movement = default!;
    [Dependency] private SharedStaminaSystem _stamina = default!;
    [Dependency] private SleepingSystem _sleep = default!;
    [Dependency] private DamageableSystem _damage = default!;
    [Dependency] private MobStateSystem _mob = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SCP207Component, StatusEffectAppliedEvent>(OnApplied);
    }

    private void OnApplied(Entity<SCP207Component> ent, ref StatusEffectAppliedEvent args)
    {
        ent.Comp.NextUpdate = _timing.CurTime + ent.Comp.UpdateInterval;
        _sleep.TryWaking(args.Target, force: true);
        _popup.PopupEntity(Loc.GetString("scp207-effect-start"), args.Target, args.Target);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<SCP207Component, StatusEffectComponent, MovementModStatusEffectComponent>();
        while (query.MoveNext(out var uid, out var scp, out var status, out var movement))
        {
            if (curTime < scp.NextUpdate)
                continue;

            scp.NextUpdate = curTime + scp.UpdateInterval;

            if (!status.Applied || status.AppliedTo is not { } target || _mob.IsDead(target))
                continue;

            var seconds = (float) scp.UpdateInterval.TotalSeconds;
            scp.Elapsed += seconds;
            // мне больно смотреть на это извините пожалуйста
            var speed = Math.Min(scp.MaximumSpeed, scp.InitialSpeed + scp.Elapsed / 60f * scp.SpeedGainPerMinute);
            if (!MathHelper.CloseTo(movement.WalkSpeedModifier, speed) || !MathHelper.CloseTo(movement.SprintSpeedModifier, speed))
                _movement.TryUpdateMovementStatus(target, (uid, movement), speed);

            if (TryComp<StaminaComponent>(target, out var stamina) && _stamina.GetStaminaDamage(target, stamina) > 0f)
                _stamina.TakeStaminaDamage(target, -scp.StaminaRecovery * seconds, stamina);

            _status.TryRemoveStatusEffect(target, Drowsiness);

            if (scp.Elapsed <= scp.DamageDelay)
                continue;

            if (!scp.Warned)
            {
                scp.Warned = true;
                _popup.PopupEntity(Loc.GetString("scp207-effect-damage"), target, target, PopupType.MediumCaution);
            }

            var severity = 1f + (scp.Elapsed - scp.DamageDelay) / Math.Max(1f, scp.DamageRampSeconds);
            _damage.TryChangeDamage(target, scp.Damage * (severity * seconds),
                ignoreResistances: true, interruptsDoAfters: false);
        }
    }
}
