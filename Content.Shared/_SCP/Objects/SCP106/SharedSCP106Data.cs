using Content.Shared.Actions;
using Robust.Shared.Serialization;

namespace Content.Shared._SCP.SCP106;

public sealed partial class SCP106TeleportEvent : InstantActionEvent { }

public sealed partial class SCP106SelectTargetEvent : EntityTargetActionEvent { }

public sealed partial class SCP106SpawnPortalEvent : InstantActionEvent { }

public sealed partial class SCP106SpawnTrapEvent : InstantActionEvent { }

[NetSerializable, Serializable]
public enum SCP106Visuals : byte
{
    State,
    Teleported
}

[NetSerializable, Serializable]
public enum SCP106PortalVisuals : byte
{
    State,
    ExitState
}
