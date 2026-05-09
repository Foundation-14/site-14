using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._SCP.SpawnLabel.Components;

[RegisterComponent]
public sealed partial class ExitLabelComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan UpdateDuration = TimeSpan.FromSeconds(5f);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan TimeUtilUpdate = TimeSpan.Zero;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan ExitDuration = TimeSpan.FromSeconds(40f);

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public float RangeLookup = 4f;

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public Dictionary<EntityUid, TimeSpan> ExitEnts { get; set; } = new Dictionary<EntityUid, TimeSpan>();
}

