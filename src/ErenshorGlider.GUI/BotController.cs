using System;
using ErenshorGlider.Configuration;

namespace ErenshorGlider.GUI;

/// <summary>
/// Interface for bot control operations.
/// </summary>
public interface IBotController
{
    /// <summary>
    /// Gets whether the bot is currently running.
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// Gets whether the bot is currently paused.
    /// </summary>
    bool IsPaused { get; }

    /// <summary>
    /// Gets the current bot state name.
    /// </summary>
    string CurrentState { get; }

    /// <summary>
    /// Gets the action log provider.
    /// </summary>
    IActionLogProvider Log { get; }

    /// <summary>
    /// Gets the current configuration.
    /// </summary>
    BotConfig CurrentConfig { get; }

    /// <summary>
    /// Event raised when the bot state changes.
    /// </summary>
    event EventHandler<BotStateChangedEventArgs>? BotStateChanged;
    /// Event raised when the bot starts or stops.
    /// </summary>
    event EventHandler<BotRunningChangedEventArgs>? BotRunningChanged;

    /// <summary>
    /// Starts the bot.
    /// </summary>
    /// <returns>True if the bot was started, false if already running.</returns>
    bool Start();

    /// <summary>
    /// Stops the bot.
    /// </summary>
    void Stop();

    /// <summary>
    /// Pauses the bot.
    /// </summary>
    void Pause();

    /// <summary>
    /// Resumes the bot.
    /// </summary>
    void Resume();

    /// <summary>
    /// Event raised when configuration is updated.
    /// </summary>
    event EventHandler? ConfigUpdated;
}

/// <summary>
/// Event arguments for bot state changes.
/// </summary>
public class BotStateChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the previous state.
    /// </summary>
    public string PreviousState { get; }

    /// <summary>
    /// Gets the new state.
    /// </summary>
    public string NewState { get; }

    public BotStateChangedEventArgs(string previousState, string newState)
    {
        PreviousState = previousState;
        NewState = newState;
    }
}

/// <summary>
/// Event arguments for bot running state changes.
/// </summary>
public class BotRunningChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets whether the bot is now running.
    /// </summary>
    public bool IsRunning { get; }

    /// <summary>
    /// Gets whether the bot is now paused.
    /// </summary>
    public bool IsPaused { get; }

    public BotRunningChangedEventArgs(bool isRunning, bool isPaused = false)
    {
        IsRunning = isRunning;
        IsPaused = isPaused;
    }
}

/// <summary>
/// Default implementation of IBotController for development/testing.
/// TODO: Replace with actual implementation that communicates with the BepInEx plugin.
/// </summary>
public class MockBotController : IBotController, IBotStatusProvider, ISessionStatisticsProvider
{
    private bool _isRunning;
    private bool _isPaused;
    private string _currentState = "Idle";
    private string _targetName = string.Empty;
    private float _positionX = 100.5f;
    private float _positionY = 10.0f;
    private float _positionZ = -250.3f;
    private int _currentHealth = 100;
    private int _maxHealth = 100;
    private int _currentMana = 80;
    private int _maxMana = 100;
    private readonly Random _random = new Random();

    // Statistics tracking
    private DateTime _sessionStartTime = DateTime.UtcNow;
    private int _kills;
    private int _deaths;
    private int _xpGained;
    private int _goldEarned;
    private int _itemsLooted;

    // Action log
    private readonly MockActionLogProvider _log = new MockActionLogProvider();

    // Configuration
    private BotConfig _config = ConfigManager.Load();

    public bool IsRunning => _isRunning;
    public bool IsPaused => _isPaused;
    public string CurrentState => _currentState;
    public IActionLogProvider Log => _log;
    public BotConfig CurrentConfig => _config;

    public event EventHandler<BotStateChangedEventArgs>? BotStateChanged;
    public event EventHandler<BotRunningChangedEventArgs>? BotRunningChanged;
    public event EventHandler? ConfigUpdated;

    public bool Start()
    {
        if (_isRunning)
            return false;

        _isRunning = true;
        _isPaused = false;
        _currentState = "Pathing";
        _sessionStartTime = DateTime.UtcNow;
        _kills = 0;
        _deaths = 0;
        _xpGained = 0;
        _goldEarned = 0;
        _itemsLooted = 0;
        _log.State("Bot started - beginning patrol");
        BotRunningChanged?.Invoke(this, new BotRunningChangedEventArgs(true));
        BotStateChanged?.Invoke(this, new BotStateChangedEventArgs("Idle", _currentState));
        return true;
    }

    public void Stop()
    {
        if (!_isRunning)
            return;

        _isRunning = false;
        _isPaused = false;
        var previousState = _currentState;
        _currentState = "Idle";
        _targetName = string.Empty;
        BotStateChanged?.Invoke(this, new BotStateChangedEventArgs(previousState, _currentState));
        BotRunningChanged?.Invoke(this, new BotRunningChangedEventArgs(false));
    }

    public void Pause()
    {
        if (!_isRunning || _isPaused)
            return;

        _isPaused = true;
        BotRunningChanged?.Invoke(this, new BotRunningChangedEventArgs(true, true));
    }

    public void Resume()
    {
        if (!_isRunning || !_isPaused)
            return;

        _isPaused = false;
        BotRunningChanged?.Invoke(this, new BotRunningChangedEventArgs(true, false));
    }

    /// <summary>
    /// Gets the current bot status snapshot.
    /// </summary>
    public BotStatus GetStatus()
    {
        // Simulate some activity when running
        if (_isRunning && !_isPaused)
        {
            SimulateActivity();
        }

        return new BotStatus(
            _currentState,
            _targetName,
            !string.IsNullOrEmpty(_targetName),
            _positionX, _positionY, _positionZ,
            _currentHealth, _maxHealth,
            _currentMana, _maxMana
        );
    }

    /// <summary>
    /// Gets the current session statistics snapshot.
    /// </summary>
    public SessionStatistics GetStatistics()
    {
        var runtime = _isRunning
            ? DateTime.UtcNow - _sessionStartTime
            : TimeSpan.Zero;

        return new SessionStatistics(
            runtime,
            _kills,
            _deaths,
            _xpGained,
            _goldEarned,
            _itemsLooted
        );
    }

    /// <summary>
    /// Simulates bot activity for testing purposes.
    /// </summary>
    private void SimulateActivity()
    {
        // Simulate position changes
        _positionX += (float)(_random.NextDouble() - 0.5) * 2;
        _positionZ += (float)(_random.NextDouble() - 0.5) * 2;

        // Simulate target in combat state
        if (_currentState == "InCombat" && string.IsNullOrEmpty(_targetName))
        {
            _targetName = "Test Mob";
        }
        else if (_currentState != "InCombat")
        {
            _targetName = string.Empty;
        }

        // Simulate health/mana regeneration
        if (_currentHealth < _maxHealth)
            _currentHealth = Math.Min(_maxHealth, _currentHealth + 1);
        if (_currentMana < _maxMana)
            _currentMana = Math.Min(_maxMana, _currentMana + 2);

        // Simulate statistics (random gains for testing)
        if (_random.Next(100) < 2) // 2% chance per tick
        {
            _kills++;
            _xpGained += _random.Next(50, 150);
            _goldEarned += _random.Next(1, 20);
            _itemsLooted += _random.Next(0, 3);
        }
    }

    /// <summary>
    /// Sets a simulated target (for testing).
    /// </summary>
    public void SetTestTarget(string targetName)
    {
        _targetName = targetName;
        _currentState = "InCombat";
    }

    /// <summary>
    /// Sets simulated vitals (for testing).
    /// </summary>
    public void SetTestVitals(int health, int maxHealth, int mana, int maxMana)
    {
        _currentHealth = health;
        _maxHealth = maxHealth;
        _currentMana = mana;
        _maxMana = maxMana;
    }

    /// <summary>
    /// Reloads configuration from file.
    /// </summary>
    public void ReloadConfig()
    {
        _config = ConfigManager.Load();
        ConfigUpdated?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Updates the bot configuration.
    /// </summary>
    public void UpdateConfig(BotConfig newConfig)
    {
        _config = newConfig ?? throw new ArgumentNullException(nameof(newConfig));
        ConfigUpdated?.Invoke(this, EventArgs.Empty);
    }
}
