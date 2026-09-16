using Content.Shared._SCP.Guestbook.Components;
using Content.Shared._SCP.Guestbook.Events;
using Content.Shared.Humanoid;
using Robust.Shared.Enums;

namespace Content.Shared.Guestbook;

public abstract class SharedGuestbookSystem : EntitySystem
{
    [Dependency] private HumanoidProfileSystem _humanoidProfile = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GuestbookComponent, IdentityViewerOverrideEvent>(OnIdentityViewerOverride);
    }

    private void OnIdentityViewerOverride(
        EntityUid uid,
        GuestbookComponent comp,
        ref IdentityViewerOverrideEvent args)
    {
        if (uid == args.Target)
            return;

        if (!HasComp<HumanoidProfileComponent>(args.Target))
            return;

        var netTarget = GetNetEntity(args.Target);
        if (comp.Entries.TryGetValue(netTarget, out var entry))
        {
            args.Override = entry.GivenName;
            return;
        }

        args.Override = GetGenericName(args.Target);
    }

    public string GetAgeString(int age, Gender gender)
    {
        var g = gender.ToString();
        return age switch
        {
            >= 70 => Loc.GetString("guestbook-age-geriatric",    ("gender", g)),
            >= 60 => Loc.GetString("guestbook-age-elderly",      ("gender", g)),
            >= 50 => Loc.GetString("guestbook-age-old",          ("gender", g)),
            >= 40 => Loc.GetString("guestbook-age-middle-aged"),
            >= 24 => string.Empty,
            >= 18 => Loc.GetString("guestbook-age-young",        ("gender", g)),
            _     => Loc.GetString("guestbook-age-puzzling",     ("gender", g)),
        };
    }

    public string GetGenericName(EntityUid uid, bool prefixed = false)
    {
        var meta = MetaData(uid);
        var finalName = meta.EntityName;

        if (TryComp<GuestbookComponent>(uid, out var gb)
            && HasComp<HumanoidProfileComponent>(uid))
        {
            // Всё через HumanoidProfileSystem — прямых обращений к полям нет.
            var gender = _humanoidProfile.GetGender(uid);
            var age = _humanoidProfile.GetAge(uid);

            var parts = new List<string>(3);
            var g = gender.ToString();

            if (!string.IsNullOrEmpty(gb.VisibleAdjectiveLocId))
                parts.Add(Loc.GetString(gb.VisibleAdjectiveLocId, ("gender", g)));

            var ageStr = GetAgeString(age, gender);
            if (!string.IsNullOrEmpty(ageStr))
                parts.Add(ageStr);

            parts.Add(GetGenderString(gender));

            finalName = string.Join(' ', parts);
        }

        return prefixed
            ? Loc.GetString("guestbook-generic-name-prefixed", ("name", finalName))
            : finalName;
    }

    protected virtual string GetGenderString(Gender gender)
    {
        return gender switch
        {
            Gender.Male   => Loc.GetString("guestbook-gender-man"),
            Gender.Female => Loc.GetString("guestbook-gender-woman"),
            _             => Loc.GetString("guestbook-gender-person"),
        };
    }
}
