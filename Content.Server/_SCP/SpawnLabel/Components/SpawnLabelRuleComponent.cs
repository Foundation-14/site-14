namespace Content.Server.SpawnLabelRule.Components;

[RegisterComponent]
public sealed partial class SpawnLabelRuleComponent : Component
{
    [DataField("key")]
    [ViewVariables(VVAccess.ReadOnly)]
    public string Key = string.Empty;
}
