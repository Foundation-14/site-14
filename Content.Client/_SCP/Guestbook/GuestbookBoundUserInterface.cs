using Content.Shared._SCP.Guestbook.BUI;
using Content.Shared._SCP.Guestbook.BUIKey;
using Content.Shared._SCP.Guestbook.BUIStates;
using Robust.Client.UserInterface;

namespace Content.Client._SCP.Guestbook;

public sealed class GuestbookBoundUserInterface : BoundUserInterface
{
    private GuestbookWindow? _window;
    private GuestbookNameInputWindow? _inputWindow;
    private bool _inputConfirmed;

    public GuestbookBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not GuestbookBuiState s)
            return;

        if (s.PendingTarget is { } pending)
        {
            _inputConfirmed = false;

            EnsureInputWindow();
            _inputWindow!.SetTarget(pending, s.PendingRealName ?? string.Empty);
            _inputWindow.OpenCentered();
            return;
        }

        if (_inputConfirmed)
        {
            _inputConfirmed = false;

            if (_window != null && _window.IsOpen)
                _window.UpdateState(s);

            return;
        }

        EnsureMainWindow();
        _window!.UpdateState(s);
        _window.OpenCentered();
    }

    private void EnsureMainWindow()
    {
        if (_window != null)
            return;

        _window = new GuestbookWindow();

        _window.OnRename += (target, name) =>
            SendMessage(new GuestbookRenameMessage(target, name));

        _window.OnDelete += target =>
            SendMessage(new GuestbookDeleteMessage(target));
    }

    private void EnsureInputWindow()
    {
        if (_inputWindow != null)
            return;

        _inputWindow = new GuestbookNameInputWindow();

        _inputWindow.OnConfirm += (target, name) =>
        {
            _inputConfirmed = true;
            SendMessage(new GuestbookAddMessage(target, name));
        };
    }
}
