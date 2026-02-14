using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._SCP.SCP049.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SCPZombie049Component : Component
{
    [AutoNetworkedField, ViewVariables]
    public EntityUid OwnerUid;

    [DataField, AutoNetworkedField]
    public int ResurrectionCapacity = 2;
}
