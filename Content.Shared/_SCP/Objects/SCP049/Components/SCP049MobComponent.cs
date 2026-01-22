using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._SCP.SCP049.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SCPMob049Component : Component
{
    // Zombie
    [AutoNetworkedField, ViewVariables]
    public HashSet<EntityUid> Zombies = [];

    [DataField, AutoNetworkedField]
    public int MaxZombies = 5;

    [ViewVariables(VVAccess.ReadWrite), DataField("targetLockChance")]
    public float TargetLockChance = 0.35f;

    // Actions
    [DataField]
    public EntProtoId TargetLockAction = "ActionScp049TargetLock";

    [DataField]
    public EntProtoId StopLifeAction = "ActionScp049StopLife";

    [DataField]
    public EntProtoId RepeatedTreatmentAction = "ActionScp049RepeatedTreatment";

    // TimeSpan
    [DataField]
    public TimeSpan RepeatedTreatmentTime = TimeSpan.FromSeconds(25);

    [DataField]
    public TimeSpan RepeatedTreatmentZombieTime = TimeSpan.FromSeconds(10);
}

// Actions
public sealed partial class Scp049TargetLockAction : EntityTargetActionEvent;

public sealed partial class Scp049StopLifeAction : EntityTargetActionEvent;

public sealed partial class Scp049RepeatedTreatmentAction : EntityTargetActionEvent;

// Events
[Serializable, NetSerializable]
public sealed partial class ZombieResurrectionDoAfterEvent : SimpleDoAfterEvent;
