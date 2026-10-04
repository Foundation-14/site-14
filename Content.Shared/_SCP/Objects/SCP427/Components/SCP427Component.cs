using Content.Shared.Damage;
using Content.Shared.Polymorph;
using Robust.Shared.Prototypes;

namespace Content.Shared._SCP.SCP427.Components;

[RegisterComponent]
public sealed partial class SCP427Component : Component
{
    /// <summary>
    ///     The radius of the healing cone in front of the wearer (in tiles).
    /// </summary>
    [DataField]
    public float ConeRadius = 3f;

    /// <summary>
    ///     The angle of the cone in degrees (total spread).
    /// </summary>
    [DataField]
    public float ConeAngle = 90f;

    /// <summary>
    ///     Time between each healing tick in seconds.
    /// </summary>
    [DataField]
    public float HealInterval = 1f;

    /// <summary>
    ///     Damage to heal per tick for each damage group (negative values = healing).
    /// </summary>
    [DataField]
    public DamageSpecifier HealPerTick = new()
    {
        DamageDict = new()
        {
            // Brute
            { "Blunt", -1 },
            { "Slash", -1 },
            { "Piercing", -1 },
            // Burn
            { "Heat", -1 },
            { "Shock", -1 },
            { "Cold", -1 },
            { "Caustic", -1 },
            // Airloss
            { "Asphyxiation", -1 },
            { "Bloodloss", -1 },
            // Toxin
            { "Poison", -4.5f },
            { "Radiation", -4.5f },
            // Genetic
            { "Cellular", -9 },
        }
    };

    /// <summary>
    ///     Mutation stages that trigger at specific exposure time thresholds.
    /// </summary>
    [DataField]
    public List<SCP427MutationStage> MutationStages = new()
    {
        // Stage 0: 0+ seconds - passive healing (handled by heal tick)
        new()
        {
            TimeThreshold = TimeSpan.Zero,
            Message = "popup-scp427-stage1",
            ApplyEffects = ApplyStageEffects.HealOnly,
        },
        // Stage 1: ~60 seconds - "You feel rested and full of energy"
        new()
        {
            TimeThreshold = TimeSpan.FromSeconds(60),
            Message = "popup-scp427-stage2",
            ApplyEffects = ApplyStageEffects.Rested,
        },
        // Stage 2: ~180 seconds - muscle spasms, slight strength boost
        new()
        {
            TimeThreshold = TimeSpan.FromSeconds(180),
            Message = "popup-scp427-stage3",
            ApplyEffects = ApplyStageEffects.Spasms,
            MeleeDamageMultiplier = 1.5f,
        },
        // Stage 3: ~360 seconds - muscles bulging, slime puddle spawn
        new()
        {
            TimeThreshold = TimeSpan.FromSeconds(360),
            Message = "popup-scp427-stage4",
            ApplyEffects = ApplyStageEffects.Bulging,
            SpawnPuddle = "SCPPuddleSlime",
        },
        // Stage 4: ~900 seconds - critical deformation, legs removed, permanent crawling
        new()
        {
            TimeThreshold = TimeSpan.FromSeconds(900),
            Message = "popup-scp427-stage5",
            ApplyEffects = ApplyStageEffects.Deformed,
        },
        // Stage 5: ~1200 seconds - chance to transform into SCP427-1
        new()
        {
            TimeThreshold = TimeSpan.FromSeconds(1200),
            Message = "popup-scp427-stage6",
            ApplyEffects = ApplyStageEffects.Transform,
            TransformChancePerMinute = 0.05f,
            TransformPrototype = "SCP427Transform",
        },
    };
}

/// <summary>
///     Defines a mutation stage for SCP-427 exposure.
/// </summary>
[DataDefinition]
public sealed partial record SCP427MutationStage
{
    /// <summary>
    ///     The time threshold at which this stage activates.
    /// </summary>
    [DataField]
    public TimeSpan TimeThreshold;

    /// <summary>
    ///     The localization key for the message shown when this stage activates.
    /// </summary>
    [DataField]
    public string Message = string.Empty;

    /// <summary>
    ///     What effects to apply when this stage activates.
    /// </summary>
    [DataField]
    public ApplyStageEffects ApplyEffects = ApplyStageEffects.None;

    /// <summary>
    ///     Optional puddle prototype to spawn under the target when this stage activates.
    /// </summary>
    [DataField]
    public string? SpawnPuddle;

    /// <summary>
    ///     Initial chance per minute to transform (for Transform stage).
    ///     Increases after each failed roll by TransformChanceIncrease.
    /// </summary>
    [DataField]
    public float TransformChancePerMinute = 0f;

    /// <summary>
    ///     Amount added to the transform chance after each failed roll.
    /// </summary>
    [DataField]
    public float TransformChanceIncrease = 0.05f;

    /// <summary>
    ///     The polymorph prototype to use for transformation.
    /// </summary>
    [DataField]
    public ProtoId<PolymorphPrototype>? TransformPrototype;

    /// <summary>
    ///     Multiplier applied to the user's melee damage when this stage is active.
    /// </summary>
    [DataField]
    public float MeleeDamageMultiplier = 1f;
}

/// <summary>
///     Types of effects that can be applied at mutation stages.
/// </summary>
[Flags]
public enum ApplyStageEffects : byte
{
    None = 0,
    HealOnly = 1,
    Rested = 2,
    Spasms = 4,
    Bulging = 8,
    Deformed = 16,
    Transform = 32,
}
