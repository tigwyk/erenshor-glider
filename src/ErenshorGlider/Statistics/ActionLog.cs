using System;
using System.Collections.Generic;
using System.IO;

namespace ErenshorGlider.Statistics;

/// <summary>
/// Represents a single log entry.
/// </summary>
public readonly struct LogEntry
{
    /// <summary>
    /// Gets the timestamp when this log entry was created.
    /// </summary>
    public DateTime Timestamp { get; }

    /// <summary>
    /// Gets the category of this log entry.
    /// </summary>
    public LogCategory Category { get; }

    /// <summary>
    /// Gets the message of this log entry.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets the optional details associated with this log entry.
    /// </summary>
    public string? Details { get; }

    /// <summary>
    /// Creates a new LogEntry.
    /// </summary>
    public LogEntry(DateTime timestamp, LogCategory category, string message, string? details = null)
    {
        Timestamp = timestamp;
        Category = category;
        Message = message;
        Details = details;
    }

    /// <summary>
    /// Returns a formatted string representation of this log entry.
    /// </summary>
    public override string ToString()
    {
        var time = Timestamp.ToString("HH:mm:ss");
        var category = Category.ToString().ToUpper();
        return Details != null
            ? $"[{time}] [{category}] {Message}: {Details}"
            : $"[{time}] [{category}] {Message}";
    }
}

/// <summary>
/// Categories for log entries.
/// </summary>
public enum LogCategory
{
    /// <summary>General information.</summary>
    Info,
    /// <summary>Bot state transitions.</summary>
    State,
    /// <summary>Combat-related actions.</summary>
    Combat,
    /// <summary>Looting actions.</summary>
    Loot,
    /// <summary>Rest and recovery actions.</summary>
    Rest,
    /// <summary>Movement and navigation.</summary>
    Movement,
    /// <summary>Error messages.</summary>
    Error,
    /// <summary>Warning messages.</summary>
    Warning,
    /// <summary>Debug information.</summary>
    Debug
}

/// <summary>
/// Logs bot actions for display and optional file persistence.
/// </summary>
public class ActionLog
{
    private readonly CircularBuffer<LogEntry> _logBuffer;
    private readonly object _lock = new object();
    private StreamWriter? _logWriter;
    private string? _logFilePath;

    /// <summary>
    /// Gets or sets the maximum number of log entries to keep in memory.
    /// </summary>
    public int MaxLogEntries { get; set; } = 500;

    /// <summary>
    /// Gets or sets whether to persist logs to a file.
    /// </summary>
    public bool PersistToFile { get; set; }

    /// <summary>
    /// Gets the current log entries.
    /// </summary>
    public IReadOnlyList<LogEntry> Entries => _logBuffer.ToList();

    /// <summary>
    /// Event raised when a new log entry is added.
    /// </summary>
    public event Action<LogEntry>? OnLogEntryAdded;

    /// <summary>
    /// Creates a new ActionLog.
    /// </summary>
    /// <param name="maxLogEntries">Maximum number of entries to keep in memory.</param>
    public ActionLog(int maxLogEntries = 500)
    {
        _logBuffer = new CircularBuffer<LogEntry>(maxLogEntries);
        MaxLogEntries = maxLogEntries;
    }

    /// <summary>
    /// Starts logging to a file.
    /// </summary>
    /// <param name="filePath">Path to the log file.</param>
    public void StartFileLogging(string filePath)
    {
        lock (_lock)
        {
            StopFileLogging();

            _logFilePath = filePath;
            PersistToFile = true;

            // Ensure directory exists
            string directory = Path.GetDirectoryName(filePath) ?? ".";
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Create or append to log file
            _logWriter = new StreamWriter(filePath, append: true);
            _logWriter.AutoFlush = true;

            // Write session start header
            _logWriter.WriteLine($"=== Session started at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC ===");
        }
    }

    /// <summary>
    /// Stops logging to a file.
    /// </summary>
    public void StopFileLogging()
    {
        lock (_lock)
        {
            if (_logWriter != null)
            {
                _logWriter.WriteLine($"=== Session ended at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC ===");
                _logWriter.WriteLine();
                _logWriter.Dispose();
                _logWriter = null;
            }

            _logFilePath = null;
        }
    }

    /// <summary>
    /// Logs a message.
    /// </summary>
    public void Log(LogCategory category, string message, string? details = null)
    {
        var entry = new LogEntry(DateTime.UtcNow, category, message, details);

        lock (_lock)
        {
            _logBuffer.Add(entry);

            // Write to file if enabled
            if (_logWriter != null)
            {
                _logWriter.WriteLine(entry.ToString());
            }
        }

        OnLogEntryAdded?.Invoke(entry);
    }

    /// <summary>
    /// Logs an info message.
    /// </summary>
    public void Info(string message, string? details = null)
    {
        Log(LogCategory.Info, message, details);
    }

    /// <summary>
    /// Logs a state transition.
    /// </summary>
    public void State(string fromState, string toState, string? details = null)
    {
        Log(LogCategory.State, $"State changed: {fromState} -> {toState}", details);
    }

    /// <summary>
    /// Logs a combat action.
    /// </summary>
    public void Combat(string action, string? target = null, string? details = null)
    {
        string message = target != null ? $"{action} (Target: {target})" : action;
        Log(LogCategory.Combat, message, details);
    }

    /// <summary>
    /// Logs an ability used.
    /// </summary>
    public void AbilityUsed(string abilityName, string? target = null)
    {
        Combat($"Used ability: {abilityName}", target);
    }

    /// <summary>
    /// Logs a target selection.
    /// </summary>
    public void TargetSelected(string targetName, int level, string? details = null)
    {
        Combat($"Target selected: {targetName} (Level {level})", details);
    }

    /// <summary>
    /// Logs a looting action.
    /// </summary>
    public void Loot(string action, string? target = null, string? details = null)
    {
        string message = target != null ? $"{action} (Target: {target})" : action;
        Log(LogCategory.Loot, message, details);
    }

    /// <summary>
    /// Logs an item looted.
    /// </summary>
    public void ItemLooted(string itemName, int quantity, string? details = null)
    {
        Loot($"Looted: {itemName} x{quantity}", details: details);
    }

    /// <summary>
    /// Logs a rest action.
    /// </summary>
    public void Rest(string action, string? details = null)
    {
        Log(LogCategory.Rest, action, details);
    }

    /// <summary>
    /// Logs a movement action.
    /// </summary>
    public void Movement(string action, string? details = null)
    {
        Log(LogCategory.Movement, action, details);
    }

    /// <summary>
    /// Logs a waypoint reached.
    /// </summary>
    public void WaypointReached(int waypointIndex, string? waypointName = null)
    {
        string name = waypointName ?? $"#{waypointIndex}";
        Movement($"Reached waypoint: {name}");
    }

    /// <summary>
    /// Logs an error.
    /// </summary>
    public void Error(string message, string? details = null)
    {
        Log(LogCategory.Error, message, details);
    }

    /// <summary>
    /// Logs a warning.
    /// </summary>
    public void Warning(string message, string? details = null)
    {
        Log(LogCategory.Warning, message, details);
    }

    /// <summary>
    /// Logs debug information.
    /// </summary>
    public void Debug(string message, string? details = null)
    {
        Log(LogCategory.Debug, message, details);
    }

    /// <summary>
    /// Clears all log entries.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _logBuffer.Clear();
        }
    }

    /// <summary>
    /// Gets recent log entries.
    /// </summary>
    /// <param name="count">Number of recent entries to get.</param>
    public IReadOnlyList<LogEntry> GetRecentEntries(int count)
    {
        lock (_lock)
        {
            return _logBuffer.GetLatest(count);
        }
    }

    /// <summary>
    /// Gets log entries filtered by category.
    /// </summary>
    public IReadOnlyList<LogEntry> GetEntriesByCategory(LogCategory category)
    {
        lock (_lock)
        {
            var entries = _logBuffer.ToList();
            var filtered = new List<LogEntry>();
            foreach (var entry in entries)
            {
                if (entry.Category == category)
                    filtered.Add(entry);
            }
            return filtered;
        }
    }
}

/// <summary>
/// A circular buffer for storing log entries.
/// </summary>
internal class CircularBuffer<T>
{
    private readonly T[] _buffer;
    private int _head = 0;
    private int _tail = 0;
    private int _count = 0;

    public CircularBuffer(int capacity)
    {
        _buffer = new T[capacity];
    }

    public void Add(T item)
    {
        _buffer[_tail] = item;
        _tail = (_tail + 1) % _buffer.Length;

        if (_count < _buffer.Length)
        {
            _count++;
        }
        else
        {
            // Buffer is full, move head
            _head = (_head + 1) % _buffer.Length;
        }
    }

    public List<T> ToList()
    {
        var result = new List<T>(_count);
        for (int i = 0; i < _count; i++)
        {
            result.Add(_buffer[(_head + i) % _buffer.Length]);
        }
        return result;
    }

    public IReadOnlyList<T> GetLatest(int count)
    {
        var toGet = Math.Min(count, _count);
        var result = new List<T>(toGet);

        for (int i = 0; i < toGet; i++)
        {
            int index = (_tail - toGet + i + _buffer.Length) % _buffer.Length;
            result.Add(_buffer[index]);
        }

        return result;
    }

    public void Clear()
    {
        _head = 0;
        _tail = 0;
        _count = 0;
    }
}
