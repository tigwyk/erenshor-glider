using System;

namespace ErenshorGlider.Statistics;

/// <summary>
/// Tracks session statistics for the bot.
/// </summary>
public class SessionStatistics
{
    private DateTime _sessionStartTime;
    private DateTime _lastKillTime;
    private DateTime _lastDeathTime;
    private DateTime _lastLootTime;

    /// <summary>
    /// Gets the session start time.
    /// </summary>
    public DateTime SessionStartTime => _sessionStartTime;

    /// <summary>
    /// Gets the total session runtime.
    /// </summary>
    public TimeSpan SessionRuntime => DateTime.UtcNow - _sessionStartTime;

    /// <summary>
    /// Gets the number of kills this session.
    /// </summary>
    public int KillCount { get; private set; }

    /// <summary>
    /// Gets the number of deaths this session.
    /// </summary>
    public int DeathCount { get; private set; }

    /// <summary>
    /// Gets the total XP gained this session.
    /// </summary>
    public float XpGained { get; private set; }

    /// <summary>
    /// Gets the starting XP for the session.
    /// </summary>
    public float StartingXp { get; private set; }

    /// <summary>
    /// Gets the number of items looted this session.
    /// </summary>
    public int ItemsLooted { get; private set; }

    /// <summary>
    /// Gets the total gold earned this session.
    /// </summary>
    public int GoldEarned { get; private set; }

    /// <summary>
    /// Gets the time of the last kill.
    /// </summary>
    public DateTime LastKillTime => _lastKillTime;

    /// <summary>
    /// Gets the time of the last death.
    /// </summary>
    public DateTime LastDeathTime => _lastDeathTime;

    /// <summary>
    /// Gets the time of the last loot.
    /// </summary>
    public DateTime LastLootTime => _lastLootTime;

    /// <summary>
    /// Gets the kills per hour rate.
    /// </summary>
    public float KillsPerHour => CalculatePerHour(KillCount);

    /// <summary>
    /// Gets the deaths per hour rate.
    /// </summary>
    public float DeathsPerHour => CalculatePerHour(DeathCount);

    /// <summary>
    /// Gets the XP per hour rate.
    /// </summary>
    public float XpPerHour => CalculatePerHour(XpGained);

    /// <summary>
    /// Gets the items looted per hour rate.
    /// </summary>
    public float ItemsPerHour => CalculatePerHour(ItemsLooted);

    /// <summary>
    /// Gets the gold earned per hour rate.
    /// </summary>
    public float GoldPerHour => CalculatePerHour(GoldEarned);

    /// <summary>
    /// Gets the average time between kills.
    /// </summary>
    public TimeSpan AverageTimeBetweenKills =>
        KillCount > 1 ? TimeSpan.FromTicks((long)(_lastKillTime - _sessionStartTime).Ticks / (KillCount - 1)) : TimeSpan.Zero;

    /// <summary>
    /// Event raised when a kill is recorded.
    /// </summary>
    public event Action? OnKill;

    /// <summary>
    /// Event raised when a death is recorded.
    /// </summary>
    public event Action? OnDeath;

    /// <summary>
    /// Event raised when XP is gained.
    /// </summary>
    public event Action<float>? OnXpGained;

    /// <summary>
    /// Event raised when an item is looted.
    /// </summary>
    public event Action<string>? OnItemLooted;

    /// <summary>
    /// Event raised when gold is earned.
    /// </summary>
    public event Action<int>? OnGoldEarned;

    /// <summary>
    /// Creates a new SessionStatistics instance.
    /// </summary>
    public SessionStatistics()
    {
        _sessionStartTime = DateTime.UtcNow;
        _lastKillTime = DateTime.MinValue;
        _lastDeathTime = DateTime.MinValue;
        _lastLootTime = DateTime.MinValue;
    }

    /// <summary>
    /// Starts a new session, resetting all statistics.
    /// </summary>
    public void StartNewSession(float currentXp = 0f)
    {
        _sessionStartTime = DateTime.UtcNow;
        KillCount = 0;
        DeathCount = 0;
        XpGained = 0f;
        StartingXp = currentXp;
        ItemsLooted = 0;
        GoldEarned = 0;
        _lastKillTime = DateTime.MinValue;
        _lastDeathTime = DateTime.MinValue;
        _lastLootTime = DateTime.MinValue;
    }

    /// <summary>
    /// Records a kill.
    /// </summary>
    public void RecordKill()
    {
        KillCount++;
        _lastKillTime = DateTime.UtcNow;
        OnKill?.Invoke();
    }

    /// <summary>
    /// Records a death.
    /// </summary>
    public void RecordDeath()
    {
        DeathCount++;
        _lastDeathTime = DateTime.UtcNow;
        OnDeath?.Invoke();
    }

    /// <summary>
    /// Records XP gained.
    /// </summary>
    /// <param name="amount">The amount of XP gained.</param>
    public void RecordXpGained(float amount)
    {
        XpGained += amount;
        OnXpGained?.Invoke(amount);
    }

    /// <summary>
    /// Records an item being looted.
    /// </summary>
    /// <param name="itemName">The name of the item looted.</param>
    /// <param name="quantity">The quantity of the item (default 1).</param>
    public void RecordItemLooted(string itemName, int quantity = 1)
    {
        ItemsLooted += quantity;
        _lastLootTime = DateTime.UtcNow;
        OnItemLooted?.Invoke(itemName);
    }

    /// <summary>
    /// Records gold earned.
    /// </summary>
    /// <param name="amount">The amount of gold earned.</param>
    public void RecordGoldEarned(int amount)
    {
        GoldEarned += amount;
        OnGoldEarned?.Invoke(amount);
    }

    /// <summary>
    /// Calculates the per-hour rate for a given value.
    /// </summary>
    private float CalculatePerHour(float value)
    {
        var runtime = SessionRuntime.TotalHours;
        return runtime > 0 ? (float)(value / runtime) : 0f;
    }

    /// <summary>
    /// Returns a summary of the current statistics.
    /// </summary>
    public StatisticsSummary GetSummary()
    {
        return new StatisticsSummary(
            SessionRuntime,
            KillCount,
            DeathCount,
            XpGained,
            ItemsLooted,
            GoldEarned,
            KillsPerHour,
            DeathsPerHour,
            XpPerHour,
            ItemsPerHour,
            GoldPerHour,
            AverageTimeBetweenKills
        );
    }

    /// <summary>
    /// Returns a string representation of the current statistics.
    /// </summary>
    public override string ToString()
    {
        return $"Session: {FormatTimespan(SessionRuntime)} | " +
               $"Kills: {KillCount} ({KillsPerHour:F1}/hr) | " +
               $"Deaths: {DeathCount} ({DeathsPerHour:F1}/hr) | " +
               $"XP: {XpGained:F0} ({XpPerHour:F0}/hr) | " +
               $"Items: {ItemsLooted} ({ItemsPerHour:F1}/hr) | " +
               $"Gold: {GoldEarned} ({GoldPerHour:F1}/hr)";
    }

    private static string FormatTimespan(TimeSpan span)
    {
        if (span.TotalHours >= 1)
            return $"{span.Hours}h {span.Minutes}m";
        if (span.TotalMinutes >= 1)
            return $"{span.Minutes}m {span.Seconds}s";
        return $"{span.Seconds}s";
    }
}

/// <summary>
/// A snapshot of session statistics at a point in time.
/// </summary>
public readonly struct StatisticsSummary
{
    /// <summary>
    /// Gets the session runtime.
    /// </summary>
    public TimeSpan SessionRuntime { get; }

    /// <summary>
    /// Gets the number of kills.
    /// </summary>
    public int KillCount { get; }

    /// <summary>
    /// Gets the number of deaths.
    /// </summary>
    public int DeathCount { get; }

    /// <summary>
    /// Gets the total XP gained.
    /// </summary>
    public float XpGained { get; }

    /// <summary>
    /// Gets the number of items looted.
    /// </summary>
    public int ItemsLooted { get; }

    /// <summary>
    /// Gets the total gold earned.
    /// </summary>
    public int GoldEarned { get; }

    /// <summary>
    /// Gets the kills per hour rate.
    /// </summary>
    public float KillsPerHour { get; }

    /// <summary>
    /// Gets the deaths per hour rate.
    /// </summary>
    public float DeathsPerHour { get; }

    /// <summary>
    /// Gets the XP per hour rate.
    /// </summary>
    public float XpPerHour { get; }

    /// <summary>
    /// Gets the items per hour rate.
    /// </summary>
    public float ItemsPerHour { get; }

    /// <summary>
    /// Gets the gold per hour rate.
    /// </summary>
    public float GoldPerHour { get; }

    /// <summary>
    /// Gets the average time between kills.
    /// </summary>
    public TimeSpan AverageTimeBetweenKills { get; }

    /// <summary>
    /// Creates a new StatisticsSummary.
    /// </summary>
    public StatisticsSummary(
        TimeSpan sessionRuntime,
        int killCount,
        int deathCount,
        float xpGained,
        int itemsLooted,
        int goldEarned,
        float killsPerHour,
        float deathsPerHour,
        float xpPerHour,
        float itemsPerHour,
        float goldPerHour,
        TimeSpan averageTimeBetweenKills)
    {
        SessionRuntime = sessionRuntime;
        KillCount = killCount;
        DeathCount = deathCount;
        XpGained = xpGained;
        ItemsLooted = itemsLooted;
        GoldEarned = goldEarned;
        KillsPerHour = killsPerHour;
        DeathsPerHour = deathsPerHour;
        XpPerHour = xpPerHour;
        ItemsPerHour = itemsPerHour;
        GoldPerHour = goldPerHour;
        AverageTimeBetweenKills = averageTimeBetweenKills;
    }

    /// <summary>
    /// Returns a string representation of this summary.
    /// </summary>
    public override string ToString()
    {
        return $"Runtime: {FormatTimespan(SessionRuntime)}\n" +
               $"Kills: {KillCount} ({KillsPerHour:F1}/hr)\n" +
               $"Deaths: {DeathCount} ({DeathsPerHour:F1}/hr)\n" +
               $"XP Gained: {XpGained:F0} ({XpPerHour:F0}/hr)\n" +
               $"Items Looted: {ItemsLooted} ({ItemsPerHour:F1}/hr)\n" +
               $"Gold Earned: {GoldEarned} ({GoldPerHour:F1}/hr)";
    }

    private static string FormatTimespan(TimeSpan span)
    {
        if (span.TotalHours >= 1)
            return $"{span.Hours}h {span.Minutes}m";
        if (span.TotalMinutes >= 1)
            return $"{span.Minutes}m {span.Seconds}s";
        return $"{span.Seconds}s";
    }
}
