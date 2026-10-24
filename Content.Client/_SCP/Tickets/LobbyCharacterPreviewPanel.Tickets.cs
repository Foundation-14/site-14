using Content.Client._SCP.Tickets;
using Robust.Client.UserInterface;
using System.Globalization;

namespace Content.Client.Lobby.UI;

public sealed partial class LobbyCharacterPreviewPanel : Control
{
    [Dependency] private ClientTicketManager _ticketManager = default!;

    protected override void EnteredTree()
    {
        base.EnteredTree();

        _ticketManager.TicketInfoUpdated += OnTicketInfoUpdated;
        UpdateTicketLabel();
    }

    protected override void ExitedTree()
    {
        base.ExitedTree();

        _ticketManager.TicketInfoUpdated -= OnTicketInfoUpdated;
    }

    private void OnTicketInfoUpdated()
    {
        UpdateTicketLabel();
    }

    private void UpdateTicketLabel()
    {
        TicketSummary.Text = Loc.GetString("scp-tickets-lobby-balance",
            ("balance", _ticketManager.Balance.ToString("0.00", CultureInfo.InvariantCulture)),
            ("modifier", _ticketManager.Modifier.ToString("0.00", CultureInfo.InvariantCulture)));
    }
}
