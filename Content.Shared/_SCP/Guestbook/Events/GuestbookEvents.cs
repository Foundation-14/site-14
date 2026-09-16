using Robust.Shared.GameObjects;

namespace Content.Shared._SCP.Guestbook.Events;

public sealed class GetKnownNameEvent : EntityEventArgs
{
    public EntityUid Viewer { get; }

    public EntityUid Target { get; }

    public string? KnownName { get; set; }

    public GetKnownNameEvent(EntityUid viewer, EntityUid target)
    {
        Viewer = viewer;
        Target = target;
    }
}

[ByRefEvent]
public record struct GuestbookAddAttemptEvent(EntityUid Target);
