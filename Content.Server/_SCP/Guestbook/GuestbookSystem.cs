using Content.Server.Popups;
using Content.Server.UserInterface;
using Content.Shared._SCP.Guestbook.BUI;
using Content.Shared._SCP.Guestbook.BUIKey;
using Content.Shared._SCP.Guestbook.BUIStates;
using Content.Shared._SCP.Guestbook.Components;
using Content.Shared._SCP.Guestbook.Events;
using Content.Shared.Guestbook;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Components;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server._SCP.Guestbook.System;

public sealed partial class GuestbookSystem : SharedGuestbookSystem
{
    [Dependency] private UserInterfaceSystem _ui = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private IRobustRandom _random = default!;

    private const int MaxNameLength = 20;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GuestbookComponent, GuestbookRequestAddMessage>(OnRequestAdd);
        SubscribeLocalEvent<GuestbookComponent, GuestbookAddMessage>(OnAdd);
        SubscribeLocalEvent<GuestbookComponent, GuestbookRenameMessage>(OnRename);
        SubscribeLocalEvent<GuestbookComponent, GuestbookDeleteMessage>(OnDelete);

        SubscribeLocalEvent<GuestbookComponent, GetVerbsEvent<InteractionVerb>>(OnGetVerbs);
        SubscribeLocalEvent<InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<GuestbookComponent, GetKnownNameEvent>(OnGetKnownName);
        SubscribeLocalEvent<GuestbookComponent, AssignVisibleAdjectiveEvent>(OnAssignAdjective);
        SubscribeLocalEvent<GuestbookComponent, ComponentStartup>(OnGuestbookStartup);
    }

    private void OnGuestbookStartup(EntityUid uid, GuestbookComponent comp, ComponentStartup args)
    {
        if (!comp.UseRandomAdjective)
            return;

        if (!HasComp<HumanoidProfileComponent>(uid))
            return;

        if (!ProtoMan.TryIndex(comp.AdjectiveDataset, out var dataset))
        {
            Log.Warning($"Не найден датасет прилагательных \"{comp.AdjectiveDataset}\" для {ToPrettyString(uid)}.");
            return;
        }

        if (dataset.Values.Count == 0)
            return;

        comp.VisibleAdjectiveLocId = _random.Pick(dataset.Values);
        Dirty(uid, comp);
    }

    public void OpenGuestbook(EntityUid uid, GuestbookComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return;

        if (!TryComp<ActorComponent>(uid, out _))
            return;

        UpdateUiState(uid, comp);
        _ui.OpenUi(uid, GuestbookUiKey.Key, uid);
    }

    private void UpdateUiState(
        EntityUid uid,
        GuestbookComponent comp,
        NetEntity? pendingTarget = null,
        string? pendingRealName = null)
    {
        var ownerName = MetaData(uid).EntityName;
        var entries = BuildEntries(comp);
        var state = new GuestbookBuiState(ownerName, entries, pendingTarget, pendingRealName);
        _ui.SetUiState(uid, GuestbookUiKey.Key, state);
    }

    private List<GuestbookBuiEntry> BuildEntries(GuestbookComponent comp)
    {
        var list = new List<GuestbookBuiEntry>(comp.Entries.Count);
        foreach (var (netTarget, entry) in comp.Entries)
            list.Add(new GuestbookBuiEntry(netTarget, entry.GivenName));
        return list;
    }

    private void OnRequestAdd(EntityUid uid, GuestbookComponent comp, GuestbookRequestAddMessage msg)
    {
        if (!TryGetEntity(msg.Target, out var targetUid) || targetUid == null)
            return;

        UpdateUiState(uid, comp, msg.Target, MetaData(targetUid.Value).EntityName);
        _ui.OpenUi(uid, GuestbookUiKey.Key, uid);
    }

    private void OnAdd(EntityUid uid, GuestbookComponent comp, GuestbookAddMessage msg)
    {
        if (!TryGetEntity(msg.Target, out var targetUid) || targetUid == null)
            return;

        TryAddGuest(uid, comp, targetUid.Value, msg.GivenName);
    }

    private void OnRename(EntityUid uid, GuestbookComponent comp, GuestbookRenameMessage msg)
    {
        RenameGuest(uid, comp, msg.Target, msg.NewName);
    }

    private void OnDelete(EntityUid uid, GuestbookComponent comp, GuestbookDeleteMessage msg)
    {
        RemoveGuest(uid, comp, msg.Target);
    }

    public bool TryAddGuest(EntityUid user, GuestbookComponent comp, EntityUid target, string rawName)
    {
        if (user == target)
        {
            _popup.PopupEntity(Loc.GetString("guestbook-self"), user, user);
            return false;
        }

        if (!ValidateName(rawName, out var givenName))
        {
            _popup.PopupEntity(Loc.GetString("guestbook-name-invalid"), user, user);
            return false;
        }

        if (!VisibilityChecks(user, target))
            return false;

        var netTarget = GetNetEntity(target);
        var realName = MetaData(target).EntityName;

        if (comp.Entries.TryGetValue(netTarget, out var existing))
            return RenameGuestInternal(user, comp, netTarget, realName, givenName, existing);

        comp.Entries[netTarget] = new GuestbookEntry(realName, givenName);
        Dirty(user, comp);

        _popup.PopupEntity(
            Loc.GetString("guestbook-add-success", ("name", givenName)),
            user, user);

        AfterMutation(user, comp);
        return true;
    }

    public bool RenameGuest(EntityUid user, GuestbookComponent comp, NetEntity target, string rawName)
    {
        if (!ValidateName(rawName, out var newName))
        {
            _popup.PopupEntity(Loc.GetString("guestbook-name-invalid"), user, user);
            return false;
        }

        if (!comp.Entries.TryGetValue(target, out var existing))
            return false;

        return RenameGuestInternal(user, comp, target, existing.RealName, newName, existing);
    }

    private bool RenameGuestInternal(
        EntityUid user,
        GuestbookComponent comp,
        NetEntity target,
        string realName,
        string newName,
        GuestbookEntry old)
    {
        var oldName = old.GivenName;
        comp.Entries[target] = new GuestbookEntry(realName, newName);
        Dirty(user, comp);

        _popup.PopupEntity(
            Loc.GetString("guestbook-rename-success",
                ("old", oldName), ("new", newName)),
            user, user);

        AfterMutation(user, comp);
        return true;
    }

    public bool RemoveGuest(EntityUid user, GuestbookComponent comp, NetEntity target)
    {
        if (!comp.Entries.TryGetValue(target, out var existing))
        {
            _popup.PopupEntity(Loc.GetString("guestbook-unknown"), user, user);
            return false;
        }

        comp.Entries.Remove(target);
        Dirty(user, comp);

        _popup.PopupEntity(
            Loc.GetString("guestbook-forget-success", ("name", existing.GivenName)),
            user, user);

        AfterMutation(user, comp);
        return true;
    }

    private void AfterMutation(EntityUid user, GuestbookComponent comp)
    {
        if (_ui.IsUiOpen(user, GuestbookUiKey.Key, user))
            UpdateUiState(user, comp);
    }

    private bool ValidateName(string raw, out string cleaned)
    {
        cleaned = raw.Trim();

        if (string.IsNullOrWhiteSpace(cleaned) || cleaned.Length > MaxNameLength)
            return false;

        foreach (var c in cleaned)
        {
            if (char.IsControl(c))
                return false;
        }

        return true;
    }

    private bool VisibilityChecks(EntityUid user, EntityUid target, bool silent = false)
    {
        if (Deleted(target))
        {
            if (!silent) _popup.PopupEntity(Loc.GetString("guestbook-target-gone"), user, user);
            return false;
        }

        if (!_interaction.InRangeAndAccessible(user, target, SharedInteractionSystem.InteractionRange))
        {
            if (!silent) _popup.PopupEntity(Loc.GetString("guestbook-cannot-see"), user, user);
            return false;
        }

        var faceName = Identity.Name(target, EntityManager);
        if (faceName == Loc.GetString("identity-unknown"))
        {
            if (!silent) _popup.PopupEntity(Loc.GetString("guestbook-face-hidden"), user, user);
            return false;
        }

        return true;
    }

    private void OnGetVerbs(EntityUid uid, GuestbookComponent comp, GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        if (uid == args.User)
        {
            args.Verbs.Add(new InteractionVerb
            {
                Text = Loc.GetString("guestbook-verb-open"),
                Act = () => OpenGuestbook(uid, comp),
            });
            return;
        }

        if (!TryComp<GuestbookComponent>(args.User, out var userComp))
            return;

        if (!VisibilityChecks(args.User, uid, silent: true))
            return;

        var netTarget = GetNetEntity(uid);
        var realName = MetaData(uid).EntityName;

        args.Verbs.Add(new InteractionVerb
        {
            Text = Loc.GetString("guestbook-verb-remember"),
            Act = () =>
            {
                UpdateUiState(args.User, userComp, netTarget, realName);
                _ui.OpenUi(args.User, GuestbookUiKey.Key, args.User);
            },
        });
    }

    private void OnInteractHand(InteractHandEvent args)
    {
        if (args.Handled)
            return;

        var user = args.User;
        var target = args.Target;

        if (user == target)
            return;

        if (!TryComp<GuestbookComponent>(user, out var userComp))
            return;

        if (!HasComp<HumanoidProfileComponent>(target))
            return;

        if (!HasComp<MobStateComponent>(target))
            return;

        if (!VisibilityChecks(user, target))
            return;

        var netTarget = GetNetEntity(target);
        var realName = MetaData(target).EntityName;

        UpdateUiState(user, userComp, netTarget, realName);
        _ui.OpenUi(user, GuestbookUiKey.Key, user);

        args.Handled = true;
    }

    private void OnGetKnownName(EntityUid uid, GuestbookComponent comp, GetKnownNameEvent args)
    {
        if (args.Viewer != uid)
            return;

        var netTarget = GetNetEntity(args.Target);

        if (comp.Entries.TryGetValue(netTarget, out var entry))
            args.KnownName = entry.GivenName;
    }

    private void OnAssignAdjective(EntityUid uid, GuestbookComponent comp, ref AssignVisibleAdjectiveEvent args)
    {
        if (comp.VisibleAdjectiveLocId == args.AdjectiveLocId)
            return;

        comp.VisibleAdjectiveLocId = args.AdjectiveLocId;
        Dirty(uid, comp);
    }
}
