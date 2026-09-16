using Robust.Shared.GameObjects;

namespace Content.Shared._SCP.Guestbook.Events;

[ByRefEvent]
public record struct IdentityViewerOverrideEvent(EntityUid Target)
{
    public string? Override;
}

[ByRefEvent]
public record struct AssignVisibleAdjectiveEvent(string? AdjectiveLocId);
