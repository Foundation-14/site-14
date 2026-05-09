using Robust.Shared.Prototypes;
using Robust.Shared.Audio;

namespace Content.Server._SCP.SpawnLabel.Components;

[RegisterComponent]
public sealed partial class SpawnLabelComponent : Component
{
    [DataField("proto")]
    [ViewVariables(VVAccess.ReadOnly)]
    public EntProtoId? EntityPrototype = default!;

    [DataField("key")]
    [ViewVariables(VVAccess.ReadOnly)]
    public string Key = string.Empty;

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public SoundSpecifier? SoundsAfterSpawn = default!;
}

[ByRefEvent]
public readonly record struct SpawnOnTheLabelEvent(EntityUid Label);
