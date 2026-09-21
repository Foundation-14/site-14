using Robust.Shared.GameStates;

namespace Content.Shared._SCP.SCP330.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SCP330CounterComponent : Component
{
    /// <summary>
    /// Number of candies taken.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int TakenCount;

    /// <summary>
    /// Component expiration time.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan ExpiresAt = TimeSpan.FromMinutes(60);
}
