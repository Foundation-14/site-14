using System.Numerics;
using Content.Shared._SCP.Viewcone;
using Robust.Client.GameObjects;
using Robust.Shared.ComponentTrees;
using Robust.Shared.Physics;

namespace Content.Client._SCP.Viewcone.ComponentTree;

/// <summary>
///     Handles gathering sprites to modify alpha in the viewcone overlays
/// </summary>
public sealed partial class SCPViewconeOccludableTreeSystem : ComponentTreeSystem<SCPViewconeOccludableTreeComponent, SCPViewconeOccludableComponent>
{
    [Dependency] private SpriteSystem _sprite = default!;

    protected override bool DoFrameUpdate => true;
    protected override bool DoTickUpdate => false;
    protected override bool Recursive => false;

    protected override Box2 ExtractAabb(in ComponentTreeEntry<SCPViewconeOccludableComponent> entry, Vector2 pos, Angle rot)
    {
        return _sprite.CalculateBounds((entry.Uid, Comp<SpriteComponent>(entry.Uid)), pos, rot, default).CalcBoundingBox();
    }
}
