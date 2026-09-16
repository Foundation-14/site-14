using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared._SCP.Guestbook.BUIStates;

[Serializable, NetSerializable]
public sealed class GuestbookBuiState : BoundUserInterfaceState
{
    public string OwnerName;
    public List<GuestbookBuiEntry> Entries;

    public NetEntity? PendingTarget;
    public string? PendingRealName;

    public GuestbookBuiState(
        string ownerName,
        List<GuestbookBuiEntry> entries,
        NetEntity? pendingTarget = null,
        string? pendingRealName = null)
    {
        OwnerName = ownerName;
        Entries = entries;
        PendingTarget = pendingTarget;
        PendingRealName = pendingRealName;
    }
}

[Serializable, NetSerializable]
public struct GuestbookBuiEntry
{
    public NetEntity Target;
    public string GivenName;

    public GuestbookBuiEntry(NetEntity target, string givenName)
    {
        Target = target;
        GivenName = givenName;
    }
}
