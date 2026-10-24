using Content.Client._SCP.Tickets;

namespace Content.Client.IoC;

internal static class ScpClientContentIoC
{
    public static void Register(this IDependencyCollection collection)
    {
        collection.Register<ClientTicketManager>();
    }
}
