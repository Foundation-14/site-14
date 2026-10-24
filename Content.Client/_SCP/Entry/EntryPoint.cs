using Content.Client._SCP.Tickets;

namespace Content.Client.Entry
{
    public sealed partial class EntryPoint
    {
        [Dependency] private ClientTicketManager _ticketManager = default!;

        private void Initialize()
        {
            _ticketManager.Initialize();
        }
    }
}
