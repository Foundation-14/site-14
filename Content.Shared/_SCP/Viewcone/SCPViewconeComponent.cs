using System.Numerics;
using Robust.Shared.GameStates;

namespace Content.Shared._SCP.Viewcone;

/// <summary>
/// Base ViewconeComponent
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SCPViewconeComponent : Component
{
    [DataField, AutoNetworkedField]
    public float ConeAngle = 185f;

    [DataField, AutoNetworkedField]
    public float ConeFeather = 3f;

    [DataField, AutoNetworkedField]
    public float ConeIgnoreRadius = 0.65f;

    [DataField, AutoNetworkedField]
    public float ConeIgnoreFeather = 0.03f;

    [DataField, AutoNetworkedField]
    public bool IgnoreCone = false;

    // Clientside, used for lerping view angle
    // and keeping it consistent across all overlays
    public Angle ViewAngle;
    public Angle? DesiredViewAngle = null;
    public Angle LastMouseRotationAngle;
    public Vector2 LastWorldPos;
    public Angle LastWorldRotationAngle;
}
