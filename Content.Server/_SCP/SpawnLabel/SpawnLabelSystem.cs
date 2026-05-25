using Robust.Shared.Audio.Systems;
using Content.Server._SCP.SpawnLabel.Components;
using Robust.Shared.Random;

namespace Content.Server._SCP.SpawnLabel;

public sealed class SpawnLabelSystem : EntitySystem
{
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private IRobustRandom _random = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpawnLabelComponent, SpawnOnTheLabelEvent>(OnSpawnLabel);
    }

    public EntityUid? SpawnLabelByKey(string key)
    {
        var query = EntityQueryEnumerator<SpawnLabelComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (component.Key == key)
            {
                if (component.SoundsAfterSpawn != null)
                    _audio.PlayPvs(component.SoundsAfterSpawn, uid);

                return Spawn(component.EntityPrototype, Transform(uid).Coordinates);
            }
        }

        return null;
    }

    public void OnSpawnLabel(EntityUid uid, SpawnLabelComponent component, ref SpawnOnTheLabelEvent args)
    {
        if (component.SoundsAfterSpawn != null)
            _audio.PlayPvs(component.SoundsAfterSpawn, uid);

        Spawn(component.EntityPrototype, Transform(uid).Coordinates);
    }

    public bool TrySpawnLabel(EntityUid uid, SpawnLabelComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        SpawnOnTheLabelEvent ev = new SpawnOnTheLabelEvent(uid);
        RaiseLocalEvent(uid, ref ev);

        return true;
    }

    public void TeleportToLabel(string key, EntityUid target)
    {
        var matchingEntities = new List<EntityUid>();

        var query = EntityQueryEnumerator<SpawnLabelComponent>();
        while (query.MoveNext(out var entityUid, out var spawnLabelComponent))
        {
            if (spawnLabelComponent.Key == key)
            {
                matchingEntities.Add(entityUid);
            }
        }

        if (matchingEntities.Count == 0)
            return;

        var randomEntity = _random.Pick(matchingEntities);

        _transform.SetCoordinates(target, Transform(randomEntity).Coordinates);
        _transform.AttachToGridOrMap(target);
    }

    public bool TrySpawnByKeys(string key)
    {
        bool anySpawned = false;

        var query = EntityQueryEnumerator<SpawnLabelComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (component.Key == key)
            {
                SpawnOnTheLabelEvent ev = new SpawnOnTheLabelEvent(uid);
                RaiseLocalEvent(uid, ref ev);

                anySpawned = true;
            }
        }

        return anySpawned;
    }
}
