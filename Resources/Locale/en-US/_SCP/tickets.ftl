# SCP Tickets

scp-tickets-lobby-balance = Tickets: { $balance } (modifier x{ $modifier })
scp-tickets-insufficient = Insufficient tickets. Required: { $cost }
scp-tickets-job-cost = Entry cost: { $cost } tickets

# Commands

cmd-scp-modifytickets-desc = Modify player ticket balance
cmd-scp-modifytickets-help = Usage:
    scp_modifytickets <player> add <amount>
    scp_modifytickets <player> subtract <amount>
    scp_modifytickets <player> set <amount>
cmd-scp-modifytickets-error-args = Invalid number of arguments. Expected: scp_modifytickets <player> add|subtract|set <amount>
cmd-scp-modifytickets-error-session = Player { $user } not found.
cmd-scp-modifytickets-error-amount = Invalid ticket amount.
cmd-scp-modifytickets-error-op = Unknown operation.
cmd-scp-modifytickets-error-insufficient = Insufficient tickets to subtract.
cmd-scp-modifytickets-error-failed = Failed to add tickets for { $user }: player record not found.
cmd-scp-modifytickets-error-overflow = Amount exceeds maximum balance size.
cmd-scp-modifytickets-ok-balance = Ticket balance for { $user } modified.

cmd-scp-modifyticketmodifier-desc = Modify player ticket bonus modifier
cmd-scp-modifyticketmodifier-help = Usage: scp_modifyticketmodifier <player> add|subtract|set <value>
cmd-scp-modifyticketmodifier-error-args = Invalid number of arguments. Expected: scp_modifyticketmodifier <player> add|subtract|set <value>
cmd-scp-modifyticketmodifier-error-session = Player { $user } not found.
cmd-scp-modifyticketmodifier-error-value = Invalid modifier value.
cmd-scp-modifyticketmodifier-error-op = Unknown operation.
cmd-scp-modifyticketmodifier-ok = Bonus modifier for { $user } set to { $modifier }.

cmd-scp-massmodifytickets-desc = Grant tickets to all connected players
cmd-scp-massmodifytickets-help = Usage: scp_massmodifytickets <amount>
cmd-scp-massmodifytickets-error-args = Invalid number of arguments.
cmd-scp-massmodifytickets-error-amount = Invalid ticket amount.
cmd-scp-massmodifytickets-ok = Tickets granted to { $success } players.
cmd-scp-massmodifytickets-partial = Tickets granted to { $success } players, failed for { $failed }.
cmd-scp-massmodifytickets-announce = { $amount } tickets have been granted to all players.

cmd-scp-modifytickets-hint-player = <player>
cmd-scp-modifytickets-hint-operation = <add|subtract|set>
cmd-scp-modifytickets-hint-amount = <amount>
cmd-scp-modifyticketmodifier-hint-player = <player>
cmd-scp-modifyticketmodifier-hint-operation = <add|subtract|set>
cmd-scp-modifyticketmodifier-hint-value = <value>
cmd-scp-massmodifytickets-hint-amount = <amount>
