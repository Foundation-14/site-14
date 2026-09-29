using Content.Shared.Damage;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._SCP.Objects.SCP207.Components;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class SCP207Component : Component
{

    [DataField]
    public float Elapsed;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextUpdate;

    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);

    [DataField]
    public float InitialSpeed = 1.15f;

    [DataField]
    public float SpeedGainPerMinute = 0.05f;

    [DataField]
    public float MaximumSpeed = 1.75f;

    [DataField]
    public float DamageDelay = 180f;

    [DataField]
    public float DamageRampSeconds = 180f;

    [DataField]
    public float StaminaRecovery = 10f;

    [DataField]
    public DamageSpecifier Damage = new();

    [DataField]
    public bool Warned;
}
