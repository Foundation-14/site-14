using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

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
}
