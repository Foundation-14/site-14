# SCP Tickets

scp-tickets-lobby-balance = Билеты: { $balance } (модификатор x{ $modifier })
scp-tickets-insufficient = Недостаточно билетов. Требуется: { $cost }
scp-tickets-job-cost = Стоимость входа: { $cost } билетов

# Commands

cmd-scp-modifytickets-desc = Изменить баланс билетов игрока
cmd-scp-modifytickets-help = Использование:
    scp_modifytickets <игрок> add <количество>
    scp_modifytickets <игрок> subtract <количество>
    scp_modifytickets <игрок> set <количество>
cmd-scp-modifytickets-error-args = Неверное количество аргументов. Ожидается: scp_modifytickets <игрок> add|subtract|set <количество>
cmd-scp-modifytickets-error-session = Игрок { $user } не найден.
cmd-scp-modifytickets-error-amount = Неверное количество билетов.
cmd-scp-modifytickets-error-op = Неизвестная операция.
cmd-scp-modifytickets-error-insufficient = Недостаточно билетов для списания.
cmd-scp-modifytickets-error-failed = Не удалось начислить билеты для { $user }: запись игрока не найдена.
cmd-scp-modifytickets-error-overflow = Сумма превышает максимальный размер баланса.
cmd-scp-modifytickets-ok-balance = Баланс билетов игрока { $user } изменён.

cmd-scp-modifyticketmodifier-desc = Изменить бонус-модификатор билетов игрока
cmd-scp-modifyticketmodifier-help = Использование: scp_modifyticketmodifier <игрок> add|subtract|set <значение>
cmd-scp-modifyticketmodifier-error-args = Неверное количество аргументов. Ожидается: scp_modifyticketmodifier <игрок> add|subtract|set <значение>
cmd-scp-modifyticketmodifier-error-session = Игрок { $user } не найден.
cmd-scp-modifyticketmodifier-error-value = Неверное значение модификатора.
cmd-scp-modifyticketmodifier-error-op = Неизвестная операция.
cmd-scp-modifyticketmodifier-ok = Бонус-модификатор игрока { $user } изменён на { $modifier }.

cmd-scp-massmodifytickets-desc = Выдать билеты всем подключённым игрокам
cmd-scp-massmodifytickets-help = Использование: scp_massmodifytickets <количество>
cmd-scp-massmodifytickets-error-args = Неверное количество аргументов.
cmd-scp-massmodifytickets-error-amount = Неверное количество билетов.
cmd-scp-massmodifytickets-ok = Билеты выданы { $success } игрокам.
cmd-scp-massmodifytickets-partial = Билеты выданы { $success } игрокам, с ошибкой у { $failed }.
cmd-scp-massmodifytickets-announce = { $amount } билетов выдано всем игрокам.

cmd-scp-modifytickets-hint-player = <игрок>
cmd-scp-modifytickets-hint-operation = <add|subtract|set>
cmd-scp-modifytickets-hint-amount = <количество>
cmd-scp-modifyticketmodifier-hint-player = <игрок>
cmd-scp-modifyticketmodifier-hint-operation = <add|subtract|set>
cmd-scp-modifyticketmodifier-hint-value = <значение>
cmd-scp-massmodifytickets-hint-amount = <количество>
