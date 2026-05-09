using Robust.Shared.Audio;

namespace Content.Shared._SCP.SCP106.Components;

[RegisterComponent]
public sealed partial class SCP106TransitionComponent : Component
{
    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public float ExitShance = 0.4f;

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public string ExitKey = "SCP106TransitionExitPlace";

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public string EnterKey = "SCP106TransitionEnterPlace";

}
