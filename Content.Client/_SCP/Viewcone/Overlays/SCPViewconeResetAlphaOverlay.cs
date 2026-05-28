using Content.Shared._SCP.Viewcone;
using Content.Shared.Clothing.Components;
using Content.Shared.Item;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Graphics;

namespace Content.Client._SCP.Viewcone.Overlays;

/// <summary>
///     After <see cref="SCPViewconeSetAlphaOverlay"/> has run, resets the alpha of affected entities
///     back to normal.
/// </summary>
public sealed partial class SCPViewconeResetAlphaOverlay : Overlay
{
    [Dependency] private IEntityManager _ent = default!;
    private readonly SCPViewconeOverlayManagementSystem _cone;
    private readonly SpriteSystem _sprite;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public SCPViewconeResetAlphaOverlay()
    {
        IoCManager.InjectDependencies(this);

        _cone = _ent.EntitySysManager.GetEntitySystem<SCPViewconeOverlayManagementSystem>();
        _sprite = _ent.EntitySysManager.GetEntitySystem<SpriteSystem>();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        foreach (var (ent, baseAlpha) in _cone.CachedBaseAlphas)
        {
            _sprite.SetColor(ent!, ent.Comp.Color.WithAlpha(baseAlpha));
            _sprite.SetVisible(ent!, baseAlpha > 0f);
        }

        _cone.CachedBaseAlphas.Clear();
    }
}
