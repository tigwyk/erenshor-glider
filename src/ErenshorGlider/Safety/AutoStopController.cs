using System;
using ErenshorGlider.Statistics;

namespace ErenshorGlider.Safety;

/// <summary>
/// Manages automatic stopping conditions for the bot.
/// Monitors session runtime, stuck time, and other safety limits.
/// </summary>
public class AutoStopController
{
    private readonly ActionLog _actionLog;
    private readonly object _lock = new object();

    // Session tracking
    private DateTime _sessionStartTime = DateTime.UtcNow;
    private bool _isSessionActive = false;

    // Stuck tracking
    private DateTime _stuckStartTime = DateTime.UtcNow;
    private bool _isStuck = false;

    /// <summary>
    /// Gets or sets the maximum session runtime in minutes (0 = no limit).
    /// </summary>
    public int MaxSessionRuntimeMinutes { get; set; } = 0;

    /// <summary>
    /// Gets or sets the maximum stuck time in seconds before stopping (0 = no limit).
    /// </summary>
    public float MaxStuckTimeSeconds { get; set; } = 60f;

    /// <summary>
    /// Gets whether the session is currently active.
    /// </summary>
    public bool IsSessionActive => _isSessionActive;

    /// <summary>
    /// Gets whether the bot is currently stuck.
    /// </summary>
    public bool IsStuck => _isStuck;

    /// <summary>
    /// Gets the current session runtime.
    /// </summary>
    public TimeSpan CurrentSessionRuntime => _isSessionActive
        ? DateTime.UtcNow - _sessionStartTime
        : TimeSpan.Zero;

    /// <summary>
    /// Gets how long the bot has been stuck.
    /// </summary>
    public TimeSpan TimeStuck => _isStuck
        ? DateTime.UtcNow - _stuckStartTime
        : TimeSpan.Zero;

    /// <summary>
    /// Event raised when runtime limit is reached.
    /// </summary>
    public event Action? OnRuntimeLimitReached;

    /// <summary>
    /// Event raised when stuck time limit is reached.
    /// </summary>
    public event Action? OnStuckTimeLimitReached;

    /// <summary>
    /// Event raised when stuck state changes.
    /// </summary>
    public event Action<bool>? OnStuckStateChanged;

    /// <summary>
    /// Creates a new AutoStopController.
    /// </summary>
    /// <param name="actionLog">The action log for logging stop events.</param>
    public AutoStopController(ActionLog actionLog)
    {
        _actionLog = actionLog ?? throw new ArgumentNullException(nameof(actionLog));
    }

    /// <summary>
    /// Starts a new session.
    /// </summary>
    public void StartSession()
    {
        lock (_lock)
        {
            _sessionStartTime = DateTime.UtcNow;
            _isSessionActive = true;
            _isStuck = false;
            _actionLog.Info("Session started - auto-stop monitoring active");
        }
    }

    /// <summary>
    /// Stops the current session.
    /// </summary>
    public void StopSession()
    {
        lock (_lock)
        {
            _isSessionActive = false;
            _isStuck = false;
            var runtime = CurrentSessionRuntime;
            _actionLog.Info($"Session stopped - runtime: {FormatTimeSpan(runtime)}");
        }
    }

    /// <summary>
    /// Resets the session start time to now.
    /// Useful for resuming after a pause without counting paused time.
    /// </summary>
    public void ResetSessionTime()
    {
        lock (_lock)
        {
            _sessionStartTime = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Sets the stuck state.
    /// Should be called by navigation system when stuck state changes.
    /// </summary>
    /// <param name="stuck">True if the bot is stuck, false if it recovered.</param>
    public void SetStuckState(bool stuck)
    {
        lock (_lock)
        {
            if (_isStuck != stuck)
            {
                _isStuck = stuck;

                if (stuck)
                {
                    _stuckStartTime = DateTime.UtcNow;
                    _actionLog.Warning("Bot is stuck - unstuck attempts in progress");
                }
                else
                {
                    var timeStuck = DateTime.UtcNow - _stuckStartTime;
                    _actionLog.Info($"Bot recovered from being stuck - stuck for {FormatTimeSpan(timeStuck)}");
                }

                OnStuckStateChanged?.Invoke(stuck);
            }
        }
    }

    /// <summary>
    /// Checks if any auto-stop conditions have been met.
    /// Should be called every update tick.
    /// </summary>
    /// <returns>True if a stop condition was triggered, false otherwise.</returns>
    public bool CheckStopConditions()
    {
        lock (_lock)
        {
            if (!_isSessionActive)
                return false;

            // Check runtime limit
            if (MaxSessionRuntimeMinutes > 0)
            {
                var runtime = CurrentSessionRuntime;
                if (runtime.TotalMinutes >= MaxSessionRuntimeMinutes)
                {
                    _actionLog.Warning($"Auto-stop: Runtime limit reached ({FormatTimeSpan(runtime)} >= {MaxSessionRuntimeMinutes} minutes)");
                    OnRuntimeLimitReached?.Invoke();
                    return true;
                }
            }

            // Check stuck time limit
            if (MaxStuckTimeSeconds > 0 && _isStuck)
            {
                var timeStuck = TimeStuck;
                if (timeStuck.TotalSeconds >= MaxStuckTimeSeconds)
                {
                    _actionLog.Warning($"Auto-stop: Stuck time limit reached ({FormatTimeSpan(timeStuck)} >= {MaxStuckTimeSeconds} seconds)");
                    OnStuckTimeLimitReached?.Invoke();
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// Gets the remaining runtime before the session limit is reached.
    /// </summary>
    /// <returns>Remaining time, or null if no limit is set.</returns>
    public TimeSpan? GetRemainingRuntime()
    {
        if (MaxSessionRuntimeMinutes <= 0)
            return null;

        var maxRuntime = TimeSpan.FromMinutes(MaxSessionRuntimeMinutes);
        var remaining = maxRuntime - CurrentSessionRuntime;
        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }

    /// <summary>
    /// Gets the remaining stuck time before the stop is triggered.
    /// </summary>
    /// <returns>Remaining time, or null if not stuck or no limit is set.</returns>
    public TimeSpan? GetRemainingStuckTime()
    {
        if (MaxStuckTimeSeconds <= 0 || !_isStuck)
            return null;

        var maxStuckTime = TimeSpan.FromSeconds(MaxStuckTimeSeconds);
        var remaining = maxStuckTime - TimeStuck;
        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }

    /// <summary>
    /// Formats a TimeSpan for display.
    /// </summary>
    private static string FormatTimeSpan(TimeSpan timeSpan)
    {
        if (timeSpan.TotalHours >= 1)
        {
            return $"{(int)timeSpan.TotalHours}h {timeSpan.Minutes}m";
        }
        else if (timeSpan.TotalMinutes >= 1)
        {
            return $"{timeSpan.Minutes}m {timeSpan.Seconds}s";
        }
        else
        {
            return $"{timeSpan.Seconds}s";
        }
    }

    /// <summary>
    /// Resets the auto-stop controller state.
    /// </summary>
    public void Reset()
    {
        lock (_lock)
        {
            _sessionStartTime = DateTime.UtcNow;
            _isSessionActive = false;
            _isStuck = false;
            _stuckStartTime = DateTime.UtcNow;
        }
    }
}
