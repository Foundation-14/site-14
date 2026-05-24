using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._SCP.SCP106.Components;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class SCP106Component : Component
{
    [DataField]
    public EntProtoId SCP106TeleportAction = "ActionSCP106Teleport";

    [DataField, AutoNetworkedField]
    public EntityUid? SCP106TeleportActionEntity;

    [DataField]
    public EntProtoId SCP106SelectTargetAction = "ActionSCP106SelectTarget";

    [DataField, AutoNetworkedField]
    public EntityUid? SCP106SelectTargetActionEntity;

    [DataField]
    public EntProtoId SCP106SpawnPortalAction = "ActionSCP106SpawnPortal";

    [DataField, AutoNetworkedField]
    public EntityUid? SCP106SpawnPortalActionEntity;

    [DataField]
    public EntProtoId SCP106SpawnTrapAction = "ActionSCP106SpawnTrap";

    [DataField, AutoNetworkedField]
    public EntityUid? SCP106SpawnTrapActionEntity;

    [DataField, AutoNetworkedField]
    public EntityUid? Portal = null;

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public SoundSpecifier? SoundEnterPD = default!;

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public List<EntityUid> Traps = new List<EntityUid>();

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public bool IsTeleported = false;

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public int TrapsLimit = 3;

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public float ParalyzeTime = 3f;

    [DataField("portalId", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    [ViewVariables(VVAccess.ReadOnly)]
    public string PortalId = "SCP106Portal";

    [DataField("trapId", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    [ViewVariables(VVAccess.ReadOnly)]
    public string TrapId = "SCP106Trap";

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public string LabelKey = "SCP106MainRoom";

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public SoundSpecifier? SoundDamage = default!;

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public SoundSpecifier? SoundSpawnTrap = default!;

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public SoundSpecifier? SoundSpawnPortal = default!;

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public SoundSpecifier? SoundTeleport = default!;

    [DataField("damage")]
    public DamageSpecifier Damage = new()
    {
        DamageDict = new()
        {
            { "Slash", 20 },
            { "Cellular", 2 },
        }
    };

    [DataField("healDamage")]
    public DamageSpecifier HealDamage = new()
    {
        DamageDict = new()
        {
            { "Shock", -13 }
        }
    };

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan TeleportDuration = TimeSpan.FromSeconds(2f);

    [DataField, AutoNetworkedField]
    public string MainFixtureId = "fix1";

    [DataField, AutoNetworkedField]
    public string TriggerFixtureId = "fix2";

    [DataField, AutoNetworkedField]
    public string DoorLayer = "AirlockLayer";

    [DataField, AutoNetworkedField]
    public float DoorPhaseSlowdown = 0.4f;

    [AutoNetworkedField]
    public bool IsPhasingDoor = false;

    [AutoNetworkedField]
    public bool IsBlocked = false;

    #region Visualizer
    [DataField("state")]
    public string State = "running";

    [DataField("teleportedState")]
    public string TeleportedState = "teleportation";
    #endregion
}
