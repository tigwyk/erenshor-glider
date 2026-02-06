using System;

namespace ErenshorGlider.GUI;

/// <summary>
/// Interface for providing session statistics.
/// </summary>
public interface ISessionStatisticsProvider
{
    /// <summary>
    /// Gets the current session statistics snapshot.
    /// </summary>
    SessionStatistics GetStatistics();
}

/// <summary>
/// Snapshot of session statistics.
/// </summary>
public readonly struct SessionStatistics
{
    /// <summary>
    /// Gets the session runtime as a TimeSpan.
    /// </summary>
    public TimeSpan Runtime { get; }

    /// <summary>
    /// Gets the number of kills.
    /// </summary>
    public int Kills { get; }

    /// <summary>
    /// Gets the number of deaths.
    /// </summary>
    public int Deaths { get; }

    /// <summary>
    /// Gets the XP gained this session.
    /// </summary>
    public int XpGained { get; }

    /// <summary>
    /// Gets the gold earned this session.
    /// </summary>
    public int GoldEarned { get; }

    /// <summary>
    /// Gets the number of items looted.
    /// </summary>
    public int ItemsLooted { get; }

    /// <summary>
    /// Gets the XP per hour rate.
    /// </summary>
    public int XpPerHour
    {
        get
        {
            var hours = Runtime.TotalHours;
            return hours > 0 ? (int)(XpGained / hours) : 0;
        }
    }

    /// <summary>
    /// Gets the gold per hour rate.
    /// </summary>
    public int GoldPerHour
    {
        get
        {
            var hours = Runtime.TotalHours;
            return hours > 0 ? (int)(GoldEarned / hours) : 0;
        }
    }

    /// <summary>
    /// Gets the kills per hour rate.
    /// </summary>
    public int KillsPerHour
    {
        get
        {
            var hours = Runtime.TotalHours;
            return hours > 0 ? (int)(Kills / hours) : 0;
        }
    }

    /// <summary>
    /// Gets the formatted runtime string.
    /// </summary>
    public string FormattedRuntime
    {
        get
        {
            if (Runtime.TotalHours >= 1)
                return $"{Runtime.Hours}h {Runtime.Minutes}m {Runtime.Seconds}s";
            if (Runtime.TotalMinutes >= 1)
                return $"{Runtime.Minutes}m {Runtime.Seconds}s";
            return $"{Runtime.Seconds}s";
        }
    }

    public SessionStatistics(
        TimeSpan runtime,
        int kills,
        int deaths,
        int xpGained,
        int goldEarned,
        int itemsLooted)
    {
        Runtime = runtime;
        Kills = kills;
        Deaths = deaths;
        XpGained = xpGained;
        GoldEarned = goldEarned;
        ItemsLooted = itemsLooted;
    }

    /// <summary>
    /// Creates a default/empty session statistics.
    /// </summary>
    public static SessionStatistics Empty => new SessionStatistics(
        TimeSpan.Zero,
        0, 0, 0, 0, 0
    );
}
