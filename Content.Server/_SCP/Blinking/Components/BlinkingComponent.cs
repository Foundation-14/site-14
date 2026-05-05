using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._SCP.Blinking.Components;

[RegisterComponent]
public sealed partial class BlinkingComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan BlinkingDuration = TimeSpan.FromSeconds(0.25f);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan BlinkEndTime = TimeSpan.Zero;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public bool HoldEyeClose = false;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public float TriggerRange = 10f;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public float? StoredConeAngle = null;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public bool IsBlinking = false;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public bool IsBlinded = false;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan BlindedUntil = TimeSpan.Zero;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan TimeBetweenTriggerBlinks = TimeSpan.FromSeconds(5f);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan NextTriggerBlinkTime = TimeSpan.Zero;
}

[ByRefEvent]
public readonly record struct BlinkingEvent(EntityUid User);
