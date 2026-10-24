using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Chat.Managers;
using Content.Server.Database;
using Content.Server.GameTicking.Events;
using Content.Server.Station.Events;
using Content.Server.Players.PlayTimeTracking;
using Content.Shared.CCVar;
using Content.Shared._SCP.Tickets;
using Content.Shared.GameTicking;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Enums;
using Robust.Shared.Player;

namespace Content.Server._SCP.Tickets;

public sealed partial class TicketSystem : EntitySystem, IPostInjectInit
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private IServerDbManager _db = default!;
    [Dependency] private IServerNetManager _net = default!;
    [Dependency] private IChatManager _chat = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private PlayTimeTrackingManager _playTimeTracking = default!;
    [Dependency] private UserDbDataManager _userDb = default!;

    private ISawmill _sawmill = default!;

    private Dictionary<NetUserId, float> _balanceCache = new();
    private Dictionary<NetUserId, float> _autoModCache = new();
    private Dictionary<NetUserId, float> _bonusModCache = new();
    private Dictionary<NetUserId, int> _paidHoursCache = new();

    private TimeSpan _tickInterval = TimeSpan.FromSeconds(30);
    private TimeSpan _lastTick = TimeSpan.Zero;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StationJobsGetCandidatesEvent>(OnStationJobsGetCandidates);
        SubscribeLocalEvent<GetDisallowedJobsEvent>(OnGetDisallowedJobs);
        SubscribeLocalEvent<IsRoleAllowedEvent>(OnIsRoleAllowed);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);

        _sawmill = LogManager.GetSawmill("system.tickets");
        _net.RegisterNetMessage<MsgTicketInfo>();
        _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
        _cfg.OnValueChanged(CCVars.ScpTicketsTickIntervalSec, v => _tickInterval = TimeSpan.FromSeconds(v), true);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _playerManager.PlayerStatusChanged -= OnPlayerStatusChanged;
    }

    void IPostInjectInit.PostInject()
    {
        _userDb.AddOnLoadPlayer(LoadPlayerTickets);
        _userDb.AddOnPlayerDisconnect(OnPlayerDisconnectTickets);
    }

    private async Task LoadPlayerTickets(ICommonSession session, CancellationToken cancel)
    {
        var (balance, autoMod, bonusMod, paidHours) = await _db.GetTicketsAsync(session.UserId, cancel);
        _balanceCache[session.UserId] = balance;
        _autoModCache[session.UserId] = autoMod;
        _bonusModCache[session.UserId] = bonusMod;
        _paidHoursCache[session.UserId] = paidHours;
        await SendTicketInfo(session);
    }

    private void OnPlayerDisconnectTickets(ICommonSession session)
    {
        _balanceCache.Remove(session.UserId);
        _autoModCache.Remove(session.UserId);
        _bonusModCache.Remove(session.UserId);
        _paidHoursCache.Remove(session.UserId);
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
    {
        var session = e.Session;
        if (session == null)
            return;
        if (session.Status == SessionStatus.InGame)
        {
            _ = SendTicketInfo(session);
        }
    }

    private float EffectiveModifier(NetUserId userId)
    {
        float autoMod = _autoModCache.GetValueOrDefault(userId, 0f);
        float bonusMod = _bonusModCache.GetValueOrDefault(userId, 0f);
        return autoMod + bonusMod;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        if (_timing.RealTime < _lastTick + _tickInterval)
            return;

        _lastTick = _timing.RealTime;

        foreach (var session in _playerManager.Sessions)
        {
            if (session.Status == SessionStatus.InGame || session.Status == SessionStatus.Connected)
                _ = ProcessPlayerTicketsAsync(session);
        }
    }

    private async Task ProcessPlayerTicketsAsync(ICommonSession session)
    {
        try
        {
            var userId = session.UserId;
            if (!_paidHoursCache.TryGetValue(userId, out var paidHours))
            {
                return;
            }

            if (!_playTimeTracking.TryGetTrackerTimes(session, out var trackers) || !trackers.TryGetValue(Content.Shared.Players.PlayTimeTracking.PlayTimeTrackingShared.TrackerOverall, out var overall))
            {
                return;
            }

            var totalHours = (int)Math.Floor(overall.TotalHours);
            var rate = _cfg.GetCVar(CCVars.ScpTicketsBaseRatePerHour);
            var step = _cfg.GetCVar(CCVars.ScpTicketsModifierStep);
            var hoursStep = _cfg.GetCVar(CCVars.ScpTicketsModifierHoursStep);
            if (hoursStep <= 0)
                hoursStep = 100;
            var max = _cfg.GetCVar(CCVars.ScpTicketsModifierMax);

            if (totalHours <= paidHours)
            {
                float targetAuto = 1.0f + step * MathF.Floor(totalHours / (float)hoursStep);
                targetAuto = MathF.Min(targetAuto, max);
                targetAuto = MathF.Round(targetAuto, 2);
                if (targetAuto > _autoModCache.GetValueOrDefault(userId, 0f))
                {
                    await _db.SetTicketAutoModifierAsync(userId, targetAuto);
                    _autoModCache[userId] = targetAuto;
                    await TryPushTicketInfoAsync(userId);
                }
                return;
            }

            if (totalHours > paidHours)
            {
                var hoursThisIter = totalHours - paidHours;
                var eff = EffectiveModifier(userId);
                eff = (float)Math.Round((double)eff, 2);
                var earned = hoursThisIter * (rate + (eff - 1f));

                var rows = await _db.AddTicketsAndSetPaidHoursAsync(userId, earned, paidHours, totalHours);
                if (rows == 0)
                {
                    return;
                }

                if (earned > 0)
                {
                    _balanceCache[userId] = _balanceCache.GetValueOrDefault(userId, 0) + earned;
                }

                _paidHoursCache[userId] = totalHours;

                float targetAuto = 1.0f + step * MathF.Floor(totalHours / (float)hoursStep);
                targetAuto = MathF.Min(targetAuto, max);
                targetAuto = MathF.Round(targetAuto, 2);
                if (targetAuto > _autoModCache.GetValueOrDefault(userId, 0f))
                {
                    await _db.SetTicketAutoModifierAsync(userId, targetAuto);
                    _autoModCache[userId] = targetAuto;
                }
            }

            await TryPushTicketInfoAsync(userId);
        }
        catch (Exception ex)
        {
            if (ex is InvalidOperationException)
                _sawmill.Warning($"Ticket processing for {session.Name}: playtime not ready.");
            else
                _sawmill.Error($"Ticket processing for {session.Name}: {ex}");
        }
    }

    private async Task EnsureLoadedAsync(NetUserId userId)
    {
        if (!_balanceCache.ContainsKey(userId))
        {
            if (_playerManager.TryGetSessionById(userId, out var session))
            {
                await LoadPlayerTickets(session, CancellationToken.None);
            }
            else
            {
                try
                {
                    var (balance, autoMod, bonusMod, paidHours) = await _db.GetTicketsAsync(userId, CancellationToken.None);
                    _balanceCache[userId] = balance;
                    _autoModCache[userId] = autoMod;
                    _bonusModCache[userId] = bonusMod;
                    _paidHoursCache[userId] = paidHours;
                }
                catch (Exception ex)
                {
                    _sawmill.Error($"EnsureLoadedAsync failed for {userId}: {ex}");
                }
            }
        }
    }

    private async Task TryPushTicketInfoAsync(NetUserId userId)
    {
        try
        {
            await EnsureLoadedAsync(userId);
            if (!_playerManager.TryGetSessionById(userId, out var session) || session == null)
                return;
            await SendTicketInfo(session);
        }
        catch (Exception ex)
        {
            _sawmill.Error($"TryPushTicketInfo failed: {ex}");
        }
    }

    public async Task SendTicketInfo(ICommonSession session)
    {
        try
        {
            await EnsureLoadedAsync(session.UserId);
            var eff = EffectiveModifier(session.UserId);
            var msg = new MsgTicketInfo
            {
                Balance = _balanceCache.GetValueOrDefault(session.UserId, 0),
                Modifier = (float)Math.Round((double)eff, 2)
            };
            _net.ServerSendMessage(msg, session.Channel);
        }
        catch (Exception ex)
        {
            _sawmill.Warning($"SendTicketInfo failed for {session.Name}: {ex}");
        }
    }

    public void TryPushTicketInfo(NetUserId userId)
    {
        _ = TryPushTicketInfoAsync(userId);
    }

    private void OnStationJobsGetCandidates(ref StationJobsGetCandidatesEvent ev)
    {
        for (int i = ev.Jobs.Count - 1; i >= 0; i--)
        {
            var jobId = ev.Jobs[i];

            if (!_proto.TryIndex(jobId, out var job))
                continue;

            if (job.TicketOnly && job.TicketCost > 0)
            {
                if (!_balanceCache.TryGetValue(ev.Player, out var balance) || balance < job.TicketCost)
                    ev.Jobs.RemoveAt(i);
            }
        }
    }

    private void OnGetDisallowedJobs(ref GetDisallowedJobsEvent ev)
    {
        foreach (var job in _proto.EnumeratePrototypes<JobPrototype>())
        {
            if (!job.TicketOnly || job.TicketCost <= 0)
                continue;

            if (!_balanceCache.TryGetValue(ev.Player.UserId, out var balance) || balance < job.TicketCost)
                ev.Jobs.Add(job.ID);
        }
    }

    private void OnIsRoleAllowed(ref IsRoleAllowedEvent ev)
    {
        if (ev.Jobs == null)
            return;

        foreach (var jobId in ev.Jobs)
        {
            if (!_proto.TryIndex(jobId, out var job))
                continue;

            if (job.TicketOnly && job.TicketCost > 0)
            {
                if (!_balanceCache.TryGetValue(ev.Player.UserId, out var balance) || balance < job.TicketCost)
                {
                    ev.Cancelled = true;
                }
            }
        }
    }

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent ev)
    {
        var profile = ev.Profile;

        if (profile == null)
            return;

        if (ev.JobId == null)
            return;

        if (!_proto.TryIndex<JobPrototype>(ev.JobId, out var job))
            return;

        if (job.TicketOnly && job.TicketCost > 0)
        {
            _ = SpendTickets(ev.Player.UserId, job.TicketCost);
        }
    }

    private async Task SpendTickets(NetUserId userId, float cost)
    {
        try
        {
            await EnsureLoadedAsync(userId);

            if (!await _db.TrySpendTicketsAsync(userId, cost))
            {
                _sawmill.Warning($"SpendTickets failed for {userId}: insufficient balance or missing record.");
                InvalidateCache(userId);
                await TryPushTicketInfoAsync(userId);
                return;
            }

            _balanceCache[userId] = _balanceCache.GetValueOrDefault(userId, 0) - cost;
            await TryPushTicketInfoAsync(userId);
        }
        catch (Exception ex)
        {
            _sawmill.Error($"SpendTickets exception: {ex}");
        }
    }

    public async Task<float> GetBalanceAsync(NetUserId userId)
    {
        await EnsureLoadedAsync(userId);
        return _balanceCache.GetValueOrDefault(userId, 0);
    }

    public async Task<bool> AddTicketsAsync(NetUserId userId, float amount)
    {
        await EnsureLoadedAsync(userId);

        if (amount <= 0)
            return false;

        var rows = await _db.AddTicketsAtomicAsync(userId, amount);

        if (rows == 0)
            return false;

        _balanceCache[userId] = _balanceCache.GetValueOrDefault(userId, 0) + amount;
        await TryPushTicketInfoAsync(userId);
        return true;
    }

    public async Task<bool> SetTicketBalanceAsync(NetUserId userId, float balance)
    {
        if (balance < 0)
            balance = 0;

        await EnsureLoadedAsync(userId);

        if (!await _db.SetTicketBalanceAsync(userId, balance))
            return false;

        _balanceCache[userId] = balance;
        await TryPushTicketInfoAsync(userId);
        return true;
    }

    public async Task<bool> SetTicketBonusModifierAsync(NetUserId userId, float bonus)
    {
        if (!float.IsFinite(bonus))
            return false;

        await EnsureLoadedAsync(userId);

        var max = _cfg.GetCVar(CCVars.ScpTicketsModifierMax);
        var auto = _autoModCache.GetValueOrDefault(userId, 0f);
        bonus = MathF.Max(bonus, -auto);
        bonus = MathF.Min(bonus, max - auto);
        bonus = MathF.Round(bonus, 2);

        if (!await _db.SetTicketBonusModifierAsync(userId, bonus))
            return false;

        _bonusModCache[userId] = bonus;
        await TryPushTicketInfoAsync(userId);
        return true;
    }

    public async Task<bool> TrySpendTicketsAsync(NetUserId userId, float cost)
    {
        await EnsureLoadedAsync(userId);

        if (!await _db.TrySpendTicketsAsync(userId, cost))
            return false;

        _balanceCache[userId] = _balanceCache.GetValueOrDefault(userId, 0) - cost;
        await TryPushTicketInfoAsync(userId);
        return true;
    }

    public void InvalidateCache(NetUserId userId)
    {
        _balanceCache.Remove(userId);
        _autoModCache.Remove(userId);
        _bonusModCache.Remove(userId);
        _paidHoursCache.Remove(userId);
    }

    public async Task<float> GetTicketBonusModifierAsync(NetUserId userId, CancellationToken cancel = default)
    {
        await EnsureLoadedAsync(userId);
        return _bonusModCache.GetValueOrDefault(userId, 0f);
    }

    public async Task<float> GetTicketAutoModifierAsync(NetUserId userId, CancellationToken cancel = default)
    {
        await EnsureLoadedAsync(userId);
        return _autoModCache.GetValueOrDefault(userId, 0f);
    }

    public async Task<int> GetTicketPaidHoursAsync(NetUserId userId, CancellationToken cancel = default)
    {
        await EnsureLoadedAsync(userId);
        return _paidHoursCache.GetValueOrDefault(userId, 0);
    }

    public IEnumerable<ICommonSession> GetOnlineSessions()
    {
        return _playerManager.Sessions.Where(s => s.Status == SessionStatus.InGame || s.Status == SessionStatus.Connected);
    }
}
