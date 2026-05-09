using Content.Shared.GameTicking.Components;
using Content.Server.StationEvents.Events;
using Content.Server.SpawnLabelRule.Components;
using Robust.Shared.Timing;

namespace Content.Server._SCP.SpawnLabel;

public sealed class SpawnLabelRule : StationEventSystem<SpawnLabelRuleComponent>
{
    [Dependency] private readonly SpawnLabelSystem _spawnLabel = default!;
    public override void Initialize()
    {
        base.Initialize();
    }

    protected override void Added(EntityUid uid, SpawnLabelRuleComponent component, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        base.Added(uid, component, gameRule, args);
        _spawnLabel.TrySpawnByKeys(component.Key);
    }

    protected override void ActiveTick(EntityUid uid, SpawnLabelRuleComponent component, GameRuleComponent gameRule, float frameTime)
    {
        base.ActiveTick(uid, component, gameRule, frameTime);
    }



}
