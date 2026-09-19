using Robust.Shared.GameStates;

namespace Content.Shared._SCP.SCP268;

/// <summary>
/// Component that blocks all interactions when attached to an entity (typically worn clothing).
/// Makes the wearer unable to interact with the world.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SCP268Component : Component
{
    /// <summary>
    /// The entity that is wearing this blindfold component (the wearer).
    /// Set when the blindfold is equipped onto a wearer, cleared when removed.
    /// </summary>
    [DataField]
    public EntityUid Wearer { get; set; }
}
