using System.Numerics;
using Content.Client.Viewport;
using Content.Client._SCP.Viewcone.ComponentTree;
using Content.Shared._SCP.Viewcone;
using Content.Shared.MouseRotator;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Client._SCP.Viewcone.Overlays;

/// <summary>
///     Queries the bounds for each viewport for all <see cref="SCPViewconeOccludableComponent"/>, then
///     sets their alpha before entities render in accordance with whether they should be in view or not
///
///     This alpha pass only works because of <see cref="SCPViewconeResetAlphaOverlay"/>, which resets in a later stage of rendering.
/// </summary>
public sealed class SCPViewconeSetAlphaOverlay : Overlay
{
    [Dependency] private IEntityManager _ent = default!;
    private readonly SCPViewconeOverlayManagementSystem _cone;
    private readonly SCPViewconeOccludableTreeSystem _tree;
    private readonly TransformSystem _xform;
    private readonly SpriteSystem _sprite;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowEntities;

    // slightly sus but cached from beforedraw to use in draw.
    private Entity<EyeComponent, SCPViewconeComponent>? _nextEye;
    private MapCoordinates? _eyePosition; // Added for ZEye support

    public SCPViewconeSetAlphaOverlay()
    {
        IoCManager.InjectDependencies(this);

        _cone = _ent.EntitySysManager.GetEntitySystem<SCPViewconeOverlayManagementSystem>();
        _tree = _ent.EntitySysManager.GetEntitySystem<SCPViewconeOccludableTreeSystem>();
        _xform  = _ent.EntitySysManager.GetEntitySystem<TransformSystem>();
        _sprite = _ent.EntitySysManager.GetEntitySystem<SpriteSystem>();
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        _nextEye = null;
        _eyePosition = null;

        if (args.Viewport.Eye == null)
            return false;

        // Handle ZEye (multi-level rendering)
        if (args.Viewport.Eye is ScalingViewport.ZEye zEye)
        {
            // Use the original entity and eye from the ZEye to get the viewcone components
            var originalEntity = zEye.OriginalEntity;
            if (originalEntity != null && _ent.TryGetComponent<EyeComponent>(originalEntity.Value, out var eyeComp) &&
                _ent.TryGetComponent<SCPViewconeComponent>(originalEntity.Value, out var viewconeComp))
            {
                _nextEye = (originalEntity.Value, eyeComp, viewconeComp);
                // For ZEye, the actual eye position is the ZEye's position (which includes offset)
                _eyePosition = new MapCoordinates(zEye.Position.Position + zEye.Offset, zEye.Position.MapId);
                return true;
            }
            return false;
        }

        // Original logic for regular eyes
        var enumerator = _ent.AllEntityQueryEnumerator<EyeComponent, SCPViewconeComponent>();
        while (enumerator.MoveNext(out var uid, out var eye, out var viewcone))
        {
            if (args.Viewport.Eye != eye.Eye)
                continue;

            _nextEye = (uid, eye, viewcone);
            break;
        }

        return _nextEye != null;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (_nextEye == null)
            return;

        var (ent, eye, cone) = _nextEye.Value;

        // Determine eye position
        Vector2 eyePos;
        if (_eyePosition.HasValue)
        {
            // Use the stored position from ZEye (already includes offset)
            eyePos = _eyePosition.Value.Position;
        }
        else
        {
            var eyeTransform = _ent.GetComponent<TransformComponent>(ent);
            eyePos = _xform.GetWorldPosition(eyeTransform);
        }

        var eyeRot = cone.ViewAngle - eye.Rotation; // subtract rotation cuz idk. the lerp adds it but this doesnt want it for some reason idk.

        // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        // !! Thank You Bhijn God (TYBG) for 95% of the rest of this methods code !!
        // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        var radConeAngle = MathHelper.DegreesToRadians(cone.ConeAngle);
        var radConeFeather = MathHelper.DegreesToRadians(cone.ConeFeather);

        _cone.CachedBaseAlphas.Clear();
        var occludables = _tree.QueryAabb(args.MapId, args.WorldBounds);
        foreach (var entry in occludables)
        {
            var (comp, xform) = entry;
            var uid = entry.Uid; // this uses component.Owner.. oh well

            // dynamic clientside disabling, for effects like pulled entities
            if (_ent.HasComponent<SCPViewconeClientNoOccludeComponent>(uid))
                continue;

            if (!_ent.TryGetComponent<SpriteComponent>(uid, out var sprite))
                continue;

            if (comp.Source == ent)
                continue;

            if (!comp.OccludeIfAnchored && xform.Anchored)
                continue;

            var entPos = _xform.GetWorldPosition(xform);

            var dist = entPos - eyePos;
            var distLength = dist.Length();
            var angleDist = Angle.ShortestDistance(dist.ToWorldAngle(), eyeRot);

            var baseAlpha = sprite.Color.A;
            var angleAlpha = (float) Math.Clamp((Math.Abs(angleDist.Theta) - (radConeAngle * 0.5f)) + (radConeFeather * 0.5f), 0f, radConeFeather) / radConeFeather;
            var distAlpha = Math.Clamp((distLength - cone.ConeIgnoreRadius) + (cone.ConeIgnoreFeather * 0.5f), 0f, cone.ConeIgnoreFeather) / cone.ConeIgnoreFeather;
            var targetAlpha = Math.Max(1f - angleAlpha, 1f - distAlpha);

            // save the results so we can use it in resetalpha overlay
            _cone.CachedBaseAlphas.Add(((uid, sprite), baseAlpha));

            // multiply by the base alpha of the sprite (sprites which were already invisible for other reasons should stay invisible)
            var alpha = (comp.Inverted ? 1f - targetAlpha : targetAlpha) * (comp.OverrideBaseAlpha ? 1f : baseAlpha);
            _sprite.SetColor((uid, sprite), sprite.Color.WithAlpha(alpha));
            _sprite.SetVisible((uid, sprite), alpha > 0f);
        }
    }
}