using Content.Shared._SCP.Viewcone;

namespace Content.Client._SCP.Viewcone;

/// <summary>
///     Marks an entity which this client should always perceive, even if they have <see cref="SCPViewconeOccludableComponent"/>
/// </summary>
/// <remarks>
///     Used for dynamic situations where you should intuitively always show the occludable, like if you're pulling it.
/// </remarks>
[RegisterComponent]
public sealed partial class SCPViewconeClientNoOccludeComponent : Component;
