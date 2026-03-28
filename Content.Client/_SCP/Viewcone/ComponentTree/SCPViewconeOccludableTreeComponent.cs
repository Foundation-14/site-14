using Content.Shared._SCP.Viewcone;
using Robust.Shared.ComponentTrees;
using Robust.Shared.Physics;

namespace Content.Client._SCP.Viewcone.ComponentTree;

[RegisterComponent]
public sealed partial class SCPViewconeOccludableTreeComponent : Component, IComponentTreeComponent<SCPViewconeOccludableComponent>
{
    public DynamicTree<ComponentTreeEntry<SCPViewconeOccludableComponent>> Tree { get; set; }
}
