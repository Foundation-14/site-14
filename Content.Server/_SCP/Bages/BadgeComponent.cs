using Content.Shared.Inventory;

namespace Content.Server._SCP.Bage.Components;

[RegisterComponent]
public sealed partial class BadgeComponent : Component, IClothingSlots
{
    [DataField]
    public LocId RankLoc { get; set; }

    [DataField(required: true)]
    public LocId TypeLoc { get; set; }

    [DataField]
    public bool HasGeneratedId { get; set; }

    [DataField]
    public string IdPrefix { get; set; } = "D";

    [DataField]
    public int IdDigits { get; set; } = 4;

    [DataField]
    public string? GeneratedId { get; set; }

    public SlotFlags Slots => SlotFlags.NECK | SlotFlags.IDCARD;
}
