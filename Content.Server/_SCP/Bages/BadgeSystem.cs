using System;
using Content.Server._SCP.Bage.Components;
using Content.Shared.Examine;
using Content.Shared.Inventory;
using Robust.Shared.Random;

namespace Content.Server._SCP.Bage.Systems;

public sealed partial class BadgeSystem : EntitySystem
{
    [Dependency] private InventorySystem _inventorySystem = default!;
    [Dependency] private IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BadgeComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<InventoryComponent, ExaminedEvent>(OnExamine);
    }

    private void OnMapInit(Entity<BadgeComponent> ent, ref MapInitEvent args)
    {
        if (!ent.Comp.HasGeneratedId || ent.Comp.GeneratedId != null)
            return;

        var max = (int)Math.Pow(10, ent.Comp.IdDigits);
        var number = _random.Next(0, max);

        ent.Comp.GeneratedId = $"{ent.Comp.IdPrefix}-{number.ToString().PadLeft(ent.Comp.IdDigits, '0')}";
    }

    private void OnExamine(EntityUid ent, InventoryComponent comp, ExaminedEvent ev)
    {
        if (!_inventorySystem.TryGetInventoryEntity<BadgeComponent>((ent, comp), out var bEnt) || bEnt.Comp == null)
            return;

        var rank = Loc.GetString(bEnt.Comp.RankLoc);
        var type = Loc.GetString(bEnt.Comp.TypeLoc);

        if (!string.IsNullOrEmpty(bEnt.Comp.GeneratedId))
        {
            ev.PushMarkup(Loc.GetString("badge-component-rank-description-id",
                ("type", type),
                ("id", bEnt.Comp.GeneratedId)), 10);
        }
        else
        {
            ev.PushMarkup(Loc.GetString("badge-component-rank-description",
                ("type", type),
                ("rank", rank)), 10);
        }
    }
}
