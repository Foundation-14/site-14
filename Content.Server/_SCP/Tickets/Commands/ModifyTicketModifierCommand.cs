using System.Threading;
using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Utility;

namespace Content.Server._SCP.Tickets.Commands;

[AdminCommand(AdminFlags.Host)]
public sealed partial class ModifyTicketModifierCommand : IConsoleCommand
{
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private IEntityManager _entityManager = default!;

    public string Command => "scp_modifyticketmodifier";
    public string Description => Loc.GetString("cmd-scp-modifyticketmodifier-desc");
    public string Help => Loc.GetString("cmd-scp-modifyticketmodifier-help", ("command", Command));

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        try
        {
            if (args.Length != 3)
            {
                shell.WriteError(Loc.GetString("cmd-scp-modifyticketmodifier-error-args"));
                return;
            }

            if (!_playerManager.TryGetSessionByUsername(args[0], out var session))
            {
                shell.WriteError(Loc.GetString("cmd-scp-modifyticketmodifier-error-session",
                    ("user", args[0])));
                return;
            }

            if (!Parse.TryFloat(args[2], out var value) || !float.IsFinite(value))
            {
                shell.WriteError(Loc.GetString("cmd-scp-modifyticketmodifier-error-value"));
                return;
            }

            var ticketSys = _entityManager.System<TicketSystem>();
            var userId = session.UserId;

            var bonus = await ticketSys.GetTicketBonusModifierAsync(userId, CancellationToken.None);
            var auto = await ticketSys.GetTicketAutoModifierAsync(userId, CancellationToken.None);

            float newBonus;
            switch (args[1])
            {
                case "add":
                    newBonus = bonus + value;
                    break;
                case "subtract":
                    newBonus = bonus - value;
                    break;
                case "set":
                    newBonus = value - auto;
                    break;
                default:
                    shell.WriteError(Loc.GetString("cmd-scp-modifyticketmodifier-error-op"));
                    return;
            }

            if (!await ticketSys.SetTicketBonusModifierAsync(userId, newBonus))
            {
                shell.WriteError(Loc.GetString("cmd-scp-modifyticketmodifier-error-db"));
                return;
            }

            var savedBonus = await ticketSys.GetTicketBonusModifierAsync(userId, CancellationToken.None);
            var savedAuto = await ticketSys.GetTicketAutoModifierAsync(userId, CancellationToken.None);
            var newEff = MathF.Round(savedAuto + savedBonus, 2);
            shell.WriteLine(Loc.GetString("cmd-scp-modifyticketmodifier-ok",
                ("user", session.Name),
                ("modifier", newEff)));
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
                Loc.GetString("cmd-scp-modifyticketmodifier-hint-player")),

            2 => CompletionResult.FromHintOptions(
                ["add", "subtract", "set"],
                Loc.GetString("cmd-scp-modifyticketmodifier-hint-operation")),

            3 => CompletionResult.FromHint(
                Loc.GetString("cmd-scp-modifyticketmodifier-hint-value")),

            _ => CompletionResult.Empty,
        };
    }
}
