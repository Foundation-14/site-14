using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Utility;

namespace Content.Server._SCP.Tickets.Commands;

[AdminCommand(AdminFlags.Host)]
public sealed partial class ModifyTicketsCommand : IConsoleCommand
{
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private IEntityManager _entityManager = default!;

    public string Command => "scp_modifytickets";
    public string Description => Loc.GetString("cmd-scp-modifytickets-desc");
    public string Help => Loc.GetString("cmd-scp-modifytickets-help", ("command", Command));

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 3)
        {
            shell.WriteError(Loc.GetString("cmd-scp-modifytickets-error-args"));
            return;
        }

        var userName = args[0];
        var op = args[1];

        if (!_playerManager.TryGetSessionByUsername(userName, out var session))
        {
            shell.WriteError(Loc.GetString("cmd-scp-modifytickets-error-session", ("user", userName)));
            return;
        }

        var userId = session.UserId;
        var ticketSys = _entityManager.System<TicketSystem>();

        try
        {
            switch (op)
            {
                case "add":
                case "subtract":
                {
                    if (!Parse.TryFloat(args[2], out var amount)
                        || amount <= 0
                        || !float.IsFinite(amount))
                    {
                        shell.WriteError(Loc.GetString("cmd-scp-modifytickets-error-amount"));
                        return;
                    }

                    if (op == "subtract")
                    {
                        if (!await ticketSys.TrySpendTicketsAsync(userId, amount))
                        {
                            shell.WriteError(Loc.GetString("cmd-scp-modifytickets-error-insufficient"));
                            return;
                        }
                    }
                    else
                    {
                        if (!float.IsFinite(amount))
                        {
                            shell.WriteError(Loc.GetString("cmd-scp-modifytickets-error-amount"));
                            return;
                        }

                        if (!await ticketSys.AddTicketsAsync(userId, amount))
                        {
                            shell.WriteError(Loc.GetString("cmd-scp-modifytickets-error-failed", ("user", userName)));
                            return;
                        }
                    }

                    shell.WriteLine(Loc.GetString("cmd-scp-modifytickets-ok-balance",
                        ("user", userName),
                        ("balance", await ticketSys.GetBalanceAsync(userId))));
                    break;
                }

                case "set":
                {
                    if (!Parse.TryFloat(args[2], out var amount)
                        || amount < 0
                        || !float.IsFinite(amount))
                    {
                        shell.WriteError(Loc.GetString("cmd-scp-modifytickets-error-amount"));
                        return;
                    }

                    await ticketSys.SetTicketBalanceAsync(userId, amount);
                    shell.WriteLine(Loc.GetString("cmd-scp-modifytickets-ok-balance",
                        ("user", userName),
                        ("balance", amount)));
                    break;
                }

                default:
                    shell.WriteError(Loc.GetString("cmd-scp-modifytickets-error-op"));
                    return;
            }
        }
        catch (Exception ex)
        {
            shell.WriteError(ex.ToString());
        }
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length switch
        {
            1 => CompletionResult.FromHintOptions(
                CompletionHelper.SessionNames(players: _playerManager),
                Loc.GetString("cmd-scp-modifytickets-hint-player")),
            2 => CompletionResult.FromHintOptions(
                ["add", "subtract", "set"],
                Loc.GetString("cmd-scp-modifytickets-hint-operation")),
            3 => CompletionResult.FromHint(Loc.GetString("cmd-scp-modifytickets-hint-amount")),
            _ => CompletionResult.Empty,
        };
    }
}
