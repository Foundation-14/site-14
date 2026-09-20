using Robust.Shared.Prototypes;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;

namespace Content.Shared._SCP.SCP330.Components;

[RegisterComponent]
public sealed partial class SCP330Component : Component
{
    /// <summary>
    /// Prototype of the candy to spawn.
    /// </summary>
    [DataField]
    public EntProtoId CandyPrototypeId = "SCP330Candy";

    /// <summary>
    /// Interval between bowl refill attempts.
    /// </summary>
    [DataField]
    public TimeSpan AutoRefillInterval = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Damage to apply when the candy limit is exceeded.
    /// </summary>
    [DataField]
    public DamageSpecifier DamageOnOverdose = new() { DamageDict = { { "Blunt", FixedPoint2.New(50) } } };

    /// <summary>
    /// Number of bleed stacks to apply when the candy limit is exceeded.
    /// Each stack is 0.5 bleed amount (matching Hemorrhage reagent effect).
    /// </summary>
    [DataField]
    public float BleedStacksOnOverdose = 3.5f;
}
