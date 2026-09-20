using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._SCP.Objects.SCP.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SCP330CounterComponent : Component
{
    /// <summary>
    /// Number of candies taken.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int TakenCount;

    /// <summary>
    /// Component expiration time (15 minutes by default).
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan ExpiresAt = TimeSpan.FromMinutes(15);
}
