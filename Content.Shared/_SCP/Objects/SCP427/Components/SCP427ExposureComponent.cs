using Robust.Shared.GameStates;

namespace Content.Shared._SCP.SCP427.Components;

/// <summary>
///     Component attached to entities exposed to SCP-427 to track accumulated exposure time.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SCP427ExposureComponent : Component
{
    /// <summary>
    ///     Total time this entity has been exposed to an open SCP-427.
    ///     Does not reset when leaving the area - accumulation only pauses.
    /// </summary>
    [AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan TotalExposureTime = TimeSpan.Zero;

    /// <summary>
    ///     Set of mutation stage indices that have already been applied.
    /// </summary>
    [AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public HashSet<int> AppliedStages = new();

    /// <summary>
    ///     Accumulator for healing ticks. Resets each time healing is applied.
    /// </summary>
    [AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public float HealAccumulator = 0f;

    /// <summary>
    ///     Current chance to transform (for Transform stage), increases after each failed roll.
    /// </summary>
    [AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public float CurrentTransformChance = 0f;

    /// <summary>
    ///     Timestamp of the last transformation chance roll (for Transform stage).
    /// </summary>
    [AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan? LastTransformRoll = null;

    /// <summary>
    ///     Legs were removed by the Deformed stage; entity can never stand up.
    /// </summary>
    [AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public bool PermanentCrawl = false;

    /// <summary>
    ///     True when the Spasms stage is active, boosting melee damage.
    /// </summary>
    [AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public bool SpasmsActive = false;

    /// <summary>
    ///     Multiplies all melee damage dealt by the entity while the Spasms stage is active.
    /// </summary>
    [AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public float MeleeDamageMultiplier = 1f;
}
