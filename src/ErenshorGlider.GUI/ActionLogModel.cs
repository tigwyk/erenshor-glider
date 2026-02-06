using System;

namespace ErenshorGlider.GUI;

/// <summary>
/// Categories of log entries for color-coding.
/// </summary>
public enum LogCategory
{
    /// <summary>General information messages.</summary>
    Info,
    /// <summary>State changes (idle, pathing, combat, etc.).</summary>
    State,
    /// <summary>Combat-related actions (abilities, targets).</summary>
    Combat,
    /// <summary>Looting-related actions.</summary>
    Loot,
    /// <summary>Resting/recovery actions.</summary>
    Rest,
    /// <summary>Movement/navigation actions.</summary>
    Movement,
    /// <summary>Error messages.</summary>
    Error,
    /// <summary>Warning messages.</summary>
    Warning
}

/// <summary>
/// A single log entry.
/// </summary>
public readonly struct LogEntry
{
    /// <summary>
    /// Gets the timestamp of the log entry.
    /// </summary>
    public DateTime Timestamp { get; }

    /// <summary>
    /// Gets the category of the log entry.
    /// </summary>
    public LogCategory Category { get; }

    /// <summary>
    /// Gets the message of the log entry.
    /// </summary>
    public string Message { get; }

    public LogEntry(DateTime timestamp, LogCategory category, string message)
    {
        Timestamp = timestamp;
        Category = category;
        Message = message;
    }

    /// <summary>
    /// Gets the formatted timestamp string (HH:mm:ss).
    /// </summary>
    public string FormattedTimestamp => Timestamp.ToString("HH:mm:ss");

    /// <summary>
    /// Gets the color associated with this log category.
    /// </summary>
    public int Color => Category switch
    {
        LogCategory.Info => 0xcccccc,      // Light gray
        LogCategory.State => 0x99ccff,     // Light blue
        LogCategory.Combat => 0xff6666,    // Light red
        LogCategory.Loot => 0xffcc66,      // Light yellow
        LogCategory.Rest => 0x66ff99,      // Light green
        LogCategory.Movement => 0xcc99ff,   // Light purple
        LogCategory.Error => 0xff3333,     // Red
        LogCategory.Warning => 0xff9933,   // Orange
        _ => 0xcccccc
    };
}

/// <summary>
/// Interface for providing action log entries.
/// </summary>
public interface IActionLogProvider
{
    /// <summary>
    /// Gets the recent log entries.
    /// </summary>
    /// <param name="maxCount">Maximum number of entries to return.</param>
    /// <returns>Recent log entries.</returns>
    LogEntry[] GetRecentEntries(int maxCount = 100);

    /// <summary>
    /// Event raised when a new log entry is added.
    /// </summary>
    event EventHandler<LogEntry>? LogEntryAdded;

    /// <summary>
    /// Adds a log entry.
    /// </summary>
    void Log(LogCategory category, string message);

    /// <summary>
    /// Clears all log entries.
    /// </summary>
    void Clear();
}

/// <summary>
/// In-memory implementation of IActionLogProvider for development/testing.
/// </summary>
public class MockActionLogProvider : IActionLogProvider
{
    private readonly System.Collections.Generic.List<LogEntry> _entries = new();
    private readonly int _maxEntries = 500;
    private readonly object _lock = new();

    public event EventHandler<LogEntry>? LogEntryAdded;

    public LogEntry[] GetRecentEntries(int maxCount = 100)
    {
        lock (_lock)
        {
            var count = Math.Min(maxCount, _entries.Count);
            var result = new LogEntry[count];
            _entries.CopyTo(_entries.Count - count, result, 0, count);
            return result;
        }
    }

    public void Log(LogCategory category, string message)
    {
        var entry = new LogEntry(DateTime.Now, category, message);

        lock (_lock)
        {
            _entries.Add(entry);
            if (_entries.Count > _maxEntries)
            {
                _entries.RemoveAt(0);
            }
        }

        LogEntryAdded?.Invoke(this, entry);
    }

    public void Clear()
    {
        lock (_lock)
        {
            _entries.Clear();
        }
    }

    /// <summary>
    /// Logs an info message.
    /// </summary>
    public void Info(string message) => Log(LogCategory.Info, message);

    /// <summary>
    /// Logs a state change message.
    /// </summary>
    public void State(string message) => Log(LogCategory.State, message);

    /// <summary>
    /// Logs a combat message.
    /// </summary>
    public void Combat(string message) => Log(LogCategory.Combat, message);

    /// <summary>
    /// Logs a loot message.
    /// </summary>
    public void Loot(string message) => Log(LogCategory.Loot, message);

    /// <summary>
    /// Logs a rest message.
    /// </summary>
    public void Rest(string message) => Log(LogCategory.Rest, message);

    /// <summary>
    /// Logs a movement message.
    /// </summary>
    public void Movement(string message) => Log(LogCategory.Movement, message);

    /// <summary>
    /// Logs an error message.
    /// </summary>
    public void Error(string message) => Log(LogCategory.Error, message);

    /// <summary>
    /// Logs a warning message.
    /// </summary>
    public void Warning(string message) => Log(LogCategory.Warning, message);
}
