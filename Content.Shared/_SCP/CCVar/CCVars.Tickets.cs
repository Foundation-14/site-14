using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    /// <summary>
    ///     Базовое количество билетов, начисляемых за час отыгранного времени (до применения модификатора).
    /// </summary>
    public static readonly CVarDef<float> ScpTicketsBaseRatePerHour =
        CVarDef.Create("scp_tickets.base_rate_per_hour", 1.0f, CVar.SERVER);

    /// <summary>
    ///     Интервал в секундах между тиками системы билетов (начисление и пересчёт авто-модификатора).
    /// </summary>
    public static readonly CVarDef<int> ScpTicketsTickIntervalSec =
        CVarDef.Create("scp_tickets.tick_interval_sec", 30, CVar.SERVER);

    /// <summary>
    ///     На сколько увеличивается авто-модификатор за каждые <see cref="ScpTicketsModifierHoursStep"/> часов плейтайма.
    /// </summary>
    public static readonly CVarDef<float> ScpTicketsModifierStep =
        CVarDef.Create("scp_tickets.modifier_step", 0.15f, CVar.SERVER);

    /// <summary>
    ///     Количество часов плейтайма за один шаг повышения авто-модификатора.
    ///     Формула: auto = 1 + floor(totalHours / hoursStep) * step.
    /// </summary>
    public static readonly CVarDef<int> ScpTicketsModifierHoursStep =
        CVarDef.Create("scp_tickets.modifier_hours_step", 100, CVar.SERVER);

    /// <summary>
    ///     Верхний предел эффективного модификатора (auto + bonus). Значения выше него клампятся.
    /// </summary>
    public static readonly CVarDef<float> ScpTicketsModifierMax =
        CVarDef.Create("scp_tickets.modifier_max", 10.0f, CVar.SERVER);

    /// <summary>
    ///     Если true — команда scp_massmodifytickets отправляет объявление в чат после массовой выдачи билетов.
    /// </summary>
    public static readonly CVarDef<bool> ScpTicketsMassGiveAnnounce =
        CVarDef.Create("scp_tickets.mass_give_announce", true, CVar.SERVER);
}
