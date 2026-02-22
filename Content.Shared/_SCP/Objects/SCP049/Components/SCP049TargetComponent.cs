using Content.Shared.StatusIcon;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._SCP.SCP049.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class SCPTarget049Component : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<FactionIconPrototype> StatusIcon = "Scp049TargetIcon";
}
