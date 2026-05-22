using Content.Shared.Damage;
using Robust.Shared.Audio;

namespace Content.Shared._SCP.SCP106.Components;

[RegisterComponent]
public sealed partial class SCP106TrapComponent : Component
{
    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public float ParalyzeTime = 3f;

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool NeedStun = true;

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool NeedDamage = true;

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public string FriendlyFaction = "HandOfTheSnake";

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public string LabelKey = "SCP106MainRoom";

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public bool IsExit = false;

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public SoundSpecifier? SoundTrap = new SoundCollectionSpecifier("SCP106Corrosion");

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public SoundSpecifier? SoundEnterPD = new SoundCollectionSpecifier("SCP106PD");

    [DataField("damage")]
    public DamageSpecifier Damage = new()
    {
        DamageDict = new()
        {
            { "Slash", 16 },
            { "Cellular", 5 },
        }
    };

    #region Visualizer
    [DataField("state")]
    public string State = "portal";

    [DataField("exitState")]
    public string ExitState = "teleportation_exit";
    #endregion
}
