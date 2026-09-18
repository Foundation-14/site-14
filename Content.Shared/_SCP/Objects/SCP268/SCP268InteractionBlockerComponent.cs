using Robust.Shared.GameStates;

namespace Content.Shared._SCP.SCP268;

/// <summary>
/// Component that blocks interactions (but not movement) when attached to an entity.
/// Used by SCP-268 to prevent the wearer from interacting with the environment.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SCP268InteractionBlockerSystem))]
public sealed partial class SCP268InteractionBlockerComponent : Component
{
    /// <summary>
    /// Whether the interaction blocking is currently active.
    /// </summary>
    [DataField]
    public bool Enabled = true;
}
