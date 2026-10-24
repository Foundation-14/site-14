using Content.Shared._SCP.Tickets;
using Robust.Client;
using Robust.Shared.Network;

namespace Content.Client._SCP.Tickets;

public sealed partial class ClientTicketManager
{
    [Dependency] private IClientNetManager _net = default!;
    [Dependency] private IBaseClient _baseClient = default!;

    public float Balance { get; private set; } = 0f;
    public float Modifier { get; private set; } = 1.0f;

    public event Action? TicketInfoUpdated;

    public void Initialize()
    {
        _net.RegisterNetMessage<MsgTicketInfo>(RxTicketInfo);
        _baseClient.RunLevelChanged += (sender, args) =>
        {
            if (args.NewLevel == ClientRunLevel.Initialize)
            {
                Reset();
            }
        };
    }

    public void Reset()
    {
        Balance = 0f;
        Modifier = 1.0f;
        TicketInfoUpdated?.Invoke();
    }

    private void RxTicketInfo(MsgTicketInfo msg)
    {
        Balance = msg.Balance;
        Modifier = msg.Modifier;
        TicketInfoUpdated?.Invoke();
    }
}
