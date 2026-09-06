using Content.Shared.Damage;
using Content.Shared.DeviceLinking;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._SCP.SCP106.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SCP106FemurBreakerComponent : Component
{
    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan NextTickUtilPrison;

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan DamageTick;

    [DataField("damageSound")]
    [ViewVariables(VVAccess.ReadOnly)]
    public SoundSpecifier? DamageSound = default!;

    [DataField]
    public string ActivateSCP106FemurBreakerPort = "SCP106FemurBreaker";

    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan WorkDuration = TimeSpan.FromSeconds(30);

    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan TimeWork = TimeSpan.Zero;

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public string SoundCryPath = "/Audio/_SCP/Effects/SCP106/cry.ogg";

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public SoundSpecifier? SoundNoActive = default!;

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? BreakerDoor;

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? Target;

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? TargetSCP;

    [AutoNetworkedField, ViewVariables]
    public HashSet<EntityUid> SCPTraps = [];

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public bool IsWork = false;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public bool IsActive = true;

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public bool IsTrappedVictim = false;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public float Range = 3f;

    [DataField("damage")]
    public DamageSpecifier Damage = new()
    {
        DamageDict = new()
        {
            { "Blunt", 4 },
            { "Slash", 4 },
            { "Piercing", 4 }
        }
    };
}

