using Robust.Shared.Serialization;

namespace Content.Shared._SCP.Guestbook.BUI;

[Serializable, NetSerializable]
public sealed class GuestbookRequestAddMessage : BoundUserInterfaceMessage
{
    public NetEntity Target;

    public GuestbookRequestAddMessage(NetEntity target)
    {
        Target = target;
    }
}

[Serializable, NetSerializable]
public sealed class GuestbookAddMessage : BoundUserInterfaceMessage
{
    public NetEntity Target;
    public string GivenName;

    public GuestbookAddMessage(NetEntity target, string givenName)
    {
        Target = target;
        GivenName = givenName;
    }
}

[Serializable, NetSerializable]
public sealed class GuestbookRenameMessage : BoundUserInterfaceMessage
{
    public NetEntity Target;
    public string NewName;

    public GuestbookRenameMessage(NetEntity target, string newName)
    {
        Target = target;
        NewName = newName;
    }
}

[Serializable, NetSerializable]
public sealed class GuestbookDeleteMessage : BoundUserInterfaceMessage
{
    public NetEntity Target;

    public GuestbookDeleteMessage(NetEntity target)
    {
        Target = target;
    }
}
