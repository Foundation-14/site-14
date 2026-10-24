using Content.Shared.Administration;
using Content.Shared.CCVar;
using Content.Server.Administration;
using Content.Server.Chat.Managers;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.Utility;

namespace Content.Server._SCP.Tickets.Commands;

[AdminCommand(AdminFlags.Host)]
public sealed partial class MassModifyTicketsCommand : IConsoleCommand
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private IEntityManager _entityManager = default!;
    [Dependency] private IChatManager _chat = default!;

    public string Command => "scp_massmodifytickets";
    public string Description => Loc.GetString("cmd-scp-massmodifytickets-desc");
    public string Help => Loc.GetString("cmd-scp-massmodifytickets-help", ("command", Command));

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteError(Loc.GetString("cmd-scp-massmodifytickets-error-args"));
            return;
        }

        if (!Parse.TryFloat(args[0], out var amount)
            || amount <= 0
            || !float.IsFinite(amount))
        {
            shell.WriteError(Loc.GetString("cmd-scp-massmodifytickets-error-amount"));
            return;
        }

        var ticketSys = _entityManager.System<TicketSystem>();

        var success = 0;
        var failed = 0;

        foreach (var session in _playerManager.Sessions)
        {
            try
            {
                if (!await ticketSys.AddTicketsAsync(session.UserId, amount))
                {
                    failed++;
                    continue;
                }

                success++;
            }
            catch
            {
                failed++;
            }
        }

        if (success > 0 && _cfg.GetCVar(CCVars.ScpTicketsMassGiveAnnounce))
        {
            _chat.DispatchServerAnnouncement(Loc.GetString("cmd-scp-massmodifytickets-announce", ("amount", amount)));
        }

        if (failed > 0)
            shell.WriteLine(Loc.GetString("cmd-scp-massmodifytickets-partial",
                ("success", success),
                ("failed", failed)));
        else
            shell.WriteLine(Loc.GetString("cmd-scp-massmodifytickets-ok",
                ("success", success)));
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length == 1
            ? CompletionResult.FromHint(Loc.GetString("cmd-scp-massmodifytickets-hint-amount"))
            : CompletionResult.Empty;
    }
}
