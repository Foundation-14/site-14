using System.Numerics;
using Content.Client.Eye;
using Content.Client.Viewport;
using Content.Shared._SCP.Viewcone;
using Content.Shared.MouseRotator;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client._SCP.Viewcone.Overlays;

/// <summary>
///     Renders the actual "cone" part of the viewcone, no alpha modulation
/// </summary>
public sealed class SCPViewconeConeOverlay : Overlay
{
    [Dependency] private IEntityManager _ent = default!;
    [Dependency] private IPrototypeManager _proto = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    public override bool RequestScreenTexture => true;

    public static ProtoId<ShaderPrototype> ShaderPrototype = "Viewcone";
    private readonly ShaderInstance _viewconeShader;

    private Entity<EyeComponent, SCPViewconeComponent, TransformComponent>? _eyeEntity;
    private float _coneAngle;
    private float _coneFeather;
    private float _coneIgnoreRadius;
    private float _coneIgnoreFeather;

    public SCPViewconeConeOverlay()
    {
        IoCManager.InjectDependencies(this);
        _viewconeShader = _proto.Index(ShaderPrototype).InstanceUnique();
        ZIndex = -6;
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        _eyeEntity = null;

        // Handle ZEye (multi-level rendering)
        if (args.Viewport.Eye is ScalingViewport.ZEye zEye)
        {
            var originalEntity = zEye.OriginalEntity;
            if (originalEntity != null &&
                _ent.TryGetComponent<EyeComponent>(originalEntity.Value, out var eyeComp) &&
                _ent.TryGetComponent<SCPViewconeComponent>(originalEntity.Value, out var viewconeComp) &&
                _ent.TryGetComponent<TransformComponent>(originalEntity.Value, out var xformComp))
            {
                _coneAngle = viewconeComp.ConeAngle;
                _coneFeather = viewconeComp.ConeFeather;
                _coneIgnoreRadius = (viewconeComp.ConeIgnoreRadius - viewconeComp.ConeIgnoreFeather) * 50f;
                _coneIgnoreFeather = Math.Max(viewconeComp.ConeIgnoreFeather * 200f, 8f);
                _eyeEntity = (originalEntity.Value, eyeComp, viewconeComp, xformComp);
                return true;
            }
            return false;
        }

        // Original logic for regular eyes
        var enumerator = _ent.AllEntityQueryEnumerator<LerpingEyeComponent, EyeComponent, SCPViewconeComponent, TransformComponent>();
        while (enumerator.MoveNext(out var uid, out var _, out var eye, out var viewcone, out var xform))
        {
            if (args.Viewport.Eye != eye.Eye)
                continue;

            _coneAngle = viewcone.ConeAngle;
            _coneFeather = viewcone.ConeFeather;
            _coneIgnoreRadius = (viewcone.ConeIgnoreRadius - viewcone.ConeIgnoreFeather) * 50f;
            _coneIgnoreFeather = Math.Max(viewcone.ConeIgnoreFeather * 200f, 8f);
            _eyeEntity = (uid, eye, viewcone, xform);
            break;
        }

        return _eyeEntity != null;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null || _eyeEntity == null)
            return;

        var worldHandle = args.WorldHandle;
        var viewport = args.WorldBounds;

        // Get the appropriate eye rotation and zoom
        Angle viewAngle;
        float zoom;
        if (args.Viewport.Eye is ScalingViewport.ZEye zEye)
        {
            // Use the original eye's rotation and zoom from the ZEye
            viewAngle = _eyeEntity.Value.Comp2.ViewAngle; // viewcone component's ViewAngle
            zoom = zEye.OriginalEye.Scale.X; // zoom from original eye
        }
        else
        {
            viewAngle = _eyeEntity.Value.Comp2.ViewAngle;
            zoom = _eyeEntity.Value.Comp1.Zoom.X;
        }

        _viewconeShader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        _viewconeShader.SetParameter("Zoom", zoom);
        _viewconeShader.SetParameter("ViewAngle", (float) viewAngle.Theta);
        _viewconeShader.SetParameter("ConeAngle", _coneAngle);
        _viewconeShader.SetParameter("ConeFeather", _coneFeather);
        _viewconeShader.SetParameter("ConeIgnoreRadius", _coneIgnoreRadius);
        _viewconeShader.SetParameter("ConeIgnoreFeather", _coneIgnoreFeather);

        worldHandle.UseShader(_viewconeShader);
        worldHandle.DrawRect(viewport, Color.White);
        worldHandle.UseShader(null);
        _eyeEntity = null;
    }
}
