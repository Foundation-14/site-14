using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client._SCP.SCP268;

public sealed partial class SCP268VisionOverlay : Overlay
{
    private static readonly ProtoId<ShaderPrototype> Shader = "SCP268Effect";

    [Dependency] private IPrototypeManager _prototypeManager = default!;

    public override OverlaySpace Space => OverlaySpace.ScreenSpace;
    public override bool RequestScreenTexture => true;

    private readonly ShaderInstance _shader;
    private float _intensity;
    private float _currentIntensity;
    private const float FadeSpeed = 3.0f;

    public SCP268VisionOverlay()
    {
        IoCManager.InjectDependencies(this);
        _shader = _prototypeManager.Index(Shader).InstanceUnique();
        _intensity = 0f;
        _currentIntensity = 0f;
    }

    /// <summary>
    /// Sets target intensity (0 = hidden). Actual intensity fades via FrameUpdate.
    /// </summary>
    public void SetIntensity(float intensity)
    {
        _intensity = intensity;
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        float t = args.DeltaSeconds * FadeSpeed;
        if (t > 1f) t = 1f;
        _currentIntensity += (_intensity - _currentIntensity) * t;

        if (_intensity <= 0f && _currentIntensity <= 0.001f)
            OverlayManager.RemoveOverlay(this);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (_currentIntensity <= 0.001f || ScreenTexture == null)
            return;

        _shader.SetParameter("intensity", _currentIntensity);

        var screenHandle = args.ScreenHandle;
        screenHandle.UseShader(_shader);
        screenHandle.DrawTextureRectRegion(ScreenTexture, args.ViewportBounds);
        screenHandle.UseShader(null);
    }
}
