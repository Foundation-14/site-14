using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Map;
using Robust.Shared.Audio;
using Content.Shared.Damage;

namespace Content.Shared._SCP.SCP173;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class SCP173Component : Component
{
    [DataField]
    public EntProtoId SCP173DashAction = "ActionSCP173Dash";

    [DataField, AutoNetworkedField]
    public EntityUid? SCP173DashActionEntity;

    [DataField]
    public EntProtoId SCP173BlindAction = "ActionSCP173Blind";

    [DataField, AutoNetworkedField]
    public EntityUid? SCP173BlindActionEntity;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan BlindDuration = TimeSpan.FromSeconds(5f);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan UpdateDuration = TimeSpan.FromSeconds(0.2f);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan TimeUtilUpdate = TimeSpan.Zero;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public EntityCoordinates? Point = default!;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public float Range = 10f;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public float KillRange = 1f;

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public SoundSpecifier? SoundSCP173Blind = default!;

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public SoundSpecifier? SoundDamage = default!;

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public SoundSpecifier? SoundStepNoises = default!;

    [DataField("damage")]
    public DamageSpecifier Damage = new()
    {
        DamageDict = new()
        {
            { "Blunt", 300 },
        }
    };
}
