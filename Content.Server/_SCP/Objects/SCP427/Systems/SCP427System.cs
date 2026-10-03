using Content.Server.Jittering;
using Content.Shared._SCP.SCP427.Components;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Popups;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Stunnable;
using Content.Server.Polymorph.Systems;
using Content.Shared.Mobs.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Numerics;
using System.Linq;

namespace Content.Server._SCP.SCP427.Systems;

public sealed partial class SCP427System : EntitySystem
{
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private BloodstreamSystem _bloodstreamSystem = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private BodySystem _body = default!;
    [Dependency] private PolymorphSystem _polymorph = default!;
    [Dependency] private JitteringSystem _jittering = default!;

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<SCP427Component>();
        while (query.MoveNext(out var amuletUid, out var amuletComp))
        {
            if (!TryComp<ItemToggleComponent>(amuletUid, out var itemToggle))
                continue;

            if (!itemToggle.Activated)
                continue;

            EntityUid? wearerUid = null;
            var wearerPos = _transform.GetWorldPosition(amuletUid);
            var forward = _transform.GetWorldRotation(amuletUid).ToWorldVec();

            var parentUid = Transform(amuletUid).ParentUid;
            if (parentUid.IsValid() && TryComp<MobStateComponent>(parentUid, out _))
            {
                wearerUid = parentUid;
                forward = _transform.GetWorldRotation(parentUid).ToWorldVec();
            }

            var entitiesInCone = _lookup.GetEntitiesInRange(amuletUid, amuletComp.ConeRadius);

            foreach (var targetUid in entitiesInCone)
            {
                if (targetUid == wearerUid)
                    continue;

                if (!TryComp<DamageableComponent>(targetUid, out _))
                    continue;

                if (!TryComp<MobStateComponent>(targetUid, out _))
                    continue;

                var targetPos = _transform.GetWorldPosition(targetUid);
                var toEntity = targetPos - wearerPos;
                var distance = toEntity.Length();

                if (distance < 0.01f)
                    continue;

                var toEntityNormalized = toEntity / distance;
                var dot = Vector2.Dot(forward, toEntityNormalized);
                var angle = (float)Math.Acos(Math.Clamp(dot, -1f, 1f)) * (180 / MathF.PI);
                var halfAngle = amuletComp.ConeAngle / 2f;

                if (angle > halfAngle)
                    continue;

                if (!TryComp<SCP427ExposureComponent>(targetUid, out var exposureComp))
                {
                    exposureComp = EnsureComp<SCP427ExposureComponent>(targetUid);
                }

                exposureComp.TotalExposureTime += TimeSpan.FromSeconds(frameTime);

                exposureComp.HealAccumulator += frameTime;
                if (exposureComp.HealAccumulator >= amuletComp.HealInterval)
                {
                    exposureComp.HealAccumulator -= amuletComp.HealInterval;
                    _damageable.TryChangeDamage(
                        targetUid,
                        amuletComp.HealPerTick,
                        ignoreResistances: true,
                        origin: amuletUid
                    );
                    Dirty(targetUid, exposureComp);

                    if (TryComp<BloodstreamComponent>(targetUid, out var bloodstream) &&
                        amuletComp.HealPerTick.DamageDict.TryGetValue("Bloodloss", out var bloodloss) &&
                        bloodloss < 0)
                    {
                        var amount = Math.Abs(bloodloss.Float());
                        _bloodstreamSystem.TryModifyBleedAmount((targetUid, bloodstream), -amount);
                        _bloodstreamSystem.TryModifyBloodLevel((targetUid, bloodstream), amount);
                    }
                }

                CheckMutationStages(targetUid, exposureComp, amuletComp);
            }
        }
    }

    private void CheckMutationStages(EntityUid entityUid, SCP427ExposureComponent exposureComp, SCP427Component amuletComp)
    {
        for (int i = 0; i < amuletComp.MutationStages.Count; i++)
        {
            if (exposureComp.AppliedStages.Contains(i))
                continue;

            var stage = amuletComp.MutationStages[i];

            if (exposureComp.TotalExposureTime < stage.TimeThreshold)
                continue;

            ApplyMutationStageEffects(entityUid, exposureComp, stage);
            exposureComp.AppliedStages.Add(i);
            Dirty(entityUid, exposureComp);
        }

        var lastStage = amuletComp.MutationStages[amuletComp.MutationStages.Count - 1];
        if (lastStage.ApplyEffects.HasFlag(ApplyStageEffects.Transform) &&
            exposureComp.AppliedStages.Contains(amuletComp.MutationStages.Count - 1))
        {
            var minutesElapsed = (float)exposureComp.TotalExposureTime.TotalMinutes;

            if (minutesElapsed >= 1)
            {
                var fullMinutes = (int)MathF.Floor(minutesElapsed);
                var lastRollMinute = exposureComp.LastTransformRoll.HasValue
                    ? (int)exposureComp.LastTransformRoll.Value.TotalMinutes
                    : -1;

                if (fullMinutes > lastRollMinute)
                {
                    if (exposureComp.CurrentTransformChance <= 0f)
                    {
                        exposureComp.CurrentTransformChance = lastStage.TransformChancePerMinute;
                    }

                    if (_random.Prob(exposureComp.CurrentTransformChance))
                    {
                        TransformEntity(entityUid, lastStage);
                    }
                    else
                    {
                        exposureComp.CurrentTransformChance = Math.Clamp(
                            exposureComp.CurrentTransformChance + lastStage.TransformChanceIncrease,
                            0f,
                            1f
                        );
                        Dirty(entityUid, exposureComp);
                    }

                    exposureComp.LastTransformRoll = exposureComp.TotalExposureTime;
                    Dirty(entityUid, exposureComp);
                }
            }
        }
    }

    private void ApplyMutationStageEffects(EntityUid entityUid, SCP427ExposureComponent exposureComp, SCP427MutationStage stage)
    {
        if (stage.ApplyEffects.HasFlag(ApplyStageEffects.HealOnly))
        {
            ShowStageMessage(entityUid, stage.Message);
            return;
        }

        if (stage.ApplyEffects.HasFlag(ApplyStageEffects.Rested))
        {
            ShowStageMessage(entityUid, stage.Message);
        }

        if (stage.ApplyEffects.HasFlag(ApplyStageEffects.Spasms))
        {
            ShowStageMessage(entityUid, stage.Message);
            exposureComp.SpasmsActive = true;
            exposureComp.MeleeDamageMultiplier = stage.MeleeDamageMultiplier;
            Dirty(entityUid, exposureComp);

            _jittering.DoJitter(entityUid, TimeSpan.FromSeconds(15), true, 2f, 8f);
        }

        if (stage.ApplyEffects.HasFlag(ApplyStageEffects.Bulging))
        {
            SpawnPuddleUnderEntity(entityUid, stage.SpawnPuddle ?? "PuddleSmear");
            ShowStageMessage(entityUid, stage.Message);
        }

        if (stage.ApplyEffects.HasFlag(ApplyStageEffects.Deformed))
        {
            ApplyDeformedStage(entityUid, exposureComp);
            ShowStageMessage(entityUid, stage.Message);
        }
    }

    private void ShowStageMessage(EntityUid entityUid, string messageKey)
    {
        _popup.PopupEntity(Loc.GetString(messageKey), entityUid, entityUid, PopupType.MediumCaution);
    }

    private void SpawnPuddleUnderEntity(EntityUid entityUid, string puddleProtoId)
    {
        Spawn(puddleProtoId, Transform(entityUid).Coordinates);
    }

    private void ApplyDeformedStage(EntityUid entityUid, SCP427ExposureComponent exposureComp)
    {
        if (TryComp<BodyComponent>(entityUid, out var body))
        {
            var parts = new HashSet<ProtoId<OrganCategoryPrototype>>
            {
                "LegLeft", "LegRight",
                "FootLeft", "FootRight",
            };

            foreach (var organ in _body.EnumerateOrgans<OrganComponent>((entityUid, body))
                        .Where(it => it.Comp1.Category is { } category && parts.Contains(category))
                        .ToList())
            {
                QueueDel(organ);
            }
        }

        EnsureComp<CrawlerComponent>(entityUid);
        _stun.TryCrawling(entityUid, time: null, autoStand: false, drop: true, force: true);

        exposureComp.PermanentCrawl = true;
        Dirty(entityUid, exposureComp);
    }

    private void TransformEntity(EntityUid entityUid, SCP427MutationStage stage)
    {
        var result = _polymorph.PolymorphEntity(entityUid, stage.TransformPrototype!.Value);

        if (result is { } child)
        {
            ShowStageMessage(child, stage.Message);
            QueueDel(entityUid);
        }
    }
}
