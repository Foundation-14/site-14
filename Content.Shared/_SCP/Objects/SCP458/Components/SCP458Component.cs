using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared._SCP.Objects.SCP458.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SCP458Component : Component
{
    [AutoNetworkedField, ViewVariables]
    public EntityUid? SpawnedPizza = null;

    [DataField]
    public List<EntProtoId> PossiblePizzas = new();

    [DataField]
    public Dictionary<NetUserId, EntProtoId> PreferenceMemory = new();
}
