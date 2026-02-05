using System;
using ErenshorGlider.Combat;
using ErenshorGlider.Configuration;
using ErenshorGlider.GameState;
using ErenshorGlider.Mapping;
using ErenshorGlider.Navigation;
using ErenshorGlider.Safety;
using ErenshorGlider.Waypoints;

namespace ErenshorGlider.Bot;

/// <summary>
/// Coordinates all bot systems into a complete grinding mode state machine.
/// </summary>
public class GrindingBot
{
    private readonly WaypointPlayer _waypointPlayer;
    private readonly TargetSelector _targetSelector;
    private readonly CombatController _combatController;
    private readonly LootController _lootController;
    private readonly RestController _restController;
    private readonly DeathController _deathController;
    private readonly MapDiscoveryController _mapDiscoveryController;
    private readonly PositionTracker _positionTracker;
    private readonly SafetyController _safetyController;
    private readonly AutoStopController _autoStopController;

    /// <summary>
    /// Represents the current state of the grinding bot.
    /// </summary>
    public enum BotState
    {
        Idle,
        Pathing,
        SearchingForTarget,
        Pulling,
        InCombat,
        Looting,
        Resting,
        Dead
    }

    private BotState _currentState = BotState.Idle;
    private DateTime _sessionStartTime = DateTime.UtcNow;
    private int _killsCount = 0;

    /// <summary>
    /// Gets the current bot state.
    /// </summary>
    public BotState CurrentState => _currentState;

    /// <summary>
    /// Gets or sets the maximum death count before stopping.
    /// </summary>
    public int MaxDeathCount { get; set; } = 3;

    /// <summary>
    /// Gets or sets whether to enable auto-looting.
    /// </summary>
    public bool AutoLoot { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable auto-rest.
    /// </summary>
    public bool AutoRest { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable auto-mapping.
    /// </summary>
    public bool AutoMapping { get; set; } = true;

    /// <summary>
    /// Gets the number of deaths this session.
    /// </summary>
    [Obsolete("Use SessionDeathCount instead. This property returns the max death count setting, not the actual death count.")]
    public int DeathCount => MaxDeathCount;

    /// <summary>
    /// Gets the number of kills this session.
    /// </summary>
    public int KillsCount => _killsCount;

    /// <summary>
    /// Gets the number of deaths this session.
    /// </summary>
    public int SessionDeathCount => _deathController.DeathCount;

    /// <summary>
    /// Gets the session runtime.
    /// </summary>
    public TimeSpan SessionRuntime => DateTime.UtcNow - _sessionStartTime;

    /// <summary>
    /// Gets whether the bot is running.
    /// </summary>
    public bool IsRunning { get; private set; }

    /// <summary>
    /// Gets whether the bot is paused.
    /// </summary>
    public bool IsPaused => _safetyController.IsPaused;

    /// <summary>
    /// Event raised when the bot state changes.
    /// </summary>
    public event Action<BotState, BotState>? OnStateChanged;

    /// <summary>
    /// Event raised when the bot starts.
    /// </summary>
    public event Action? OnStarted;

    /// <summary>
    /// Event raised when the bot stops.
    /// </summary>
    public event Action<StopReason>? OnStopped;

    /// <summary>
    /// Event raised when a kill is recorded.
    /// </summary>
    public event Action? OnKill;

    /// <summary>
    /// Event raised when the player dies.
    /// </summary>
    public event Action? OnDeath;

    /// <summary>
    /// Event raised when the bot is paused.
    /// </summary>
    public event Action? OnPaused;

    /// <summary>
    /// Event raised when the bot is resumed.
    /// </summary>
    public event Action? OnResumed;

    /// <summary>
    /// Creates a new GrindingBot.
    /// </summary>
    public GrindingBot(
        WaypointPlayer waypointPlayer,
        TargetSelector targetSelector,
        CombatController combatController,
        LootController lootController,
        RestController restController,
        DeathController deathController,
        MapDiscoveryController mapDiscoveryController,
        PositionTracker positionTracker,
        SafetyController safetyController,
        AutoStopController autoStopController)
    {
        _waypointPlayer = waypointPlayer ?? throw new ArgumentNullException(nameof(waypointPlayer));
        _targetSelector = targetSelector ?? throw new ArgumentNullException(nameof(targetSelector));
        _combatController = combatController ?? throw new ArgumentNullException(nameof(combatController));
        _lootController = lootController ?? throw new ArgumentNullException(nameof(lootController));
        _restController = restController ?? throw new ArgumentNullException(nameof(restController));
        _deathController = deathController ?? throw new ArgumentNullException(nameof(deathController));
        _mapDiscoveryController = mapDiscoveryController ?? throw new ArgumentNullException(nameof(mapDiscoveryController));
        _positionTracker = positionTracker ?? throw new ArgumentNullException(nameof(positionTracker));
        _safetyController = safetyController ?? throw new ArgumentNullException(nameof(safetyController));
        _autoStopController = autoStopController ?? throw new ArgumentNullException(nameof(autoStopController));

        // Wire up event handlers
        _combatController.OnCombatEnded += HandleCombatEnded;
        _lootController.OnLootCompleted += HandleLootCompleted;
        _restController.OnRestCompleted += HandleRestCompleted;
        _deathController.OnResurrectionCompleted += HandleResurrectionCompleted;
        _deathController.OnResurrectionFailed += HandleResurrectionFailed;
        _deathController.OnPlayerDeath += HandlePlayerDeath;
        _safetyController.OnEmergencyStopTriggered += HandleEmergencyStop;
        _safetyController.OnPaused += HandlePaused;
        _safetyController.OnResumed += HandleResumed;
        _autoStopController.OnRuntimeLimitReached += HandleRuntimeLimitReached;
        _autoStopController.OnStuckTimeLimitReached += HandleStuckTimeLimitReached;
        _autoStopController.OnStuckStateChanged += HandleStuckStateChanged;
        _waypointPlayer.OnMovementStuckChanged += HandleMovementStuckChanged;
    }

    /// <summary>
    /// Starts the bot.
    /// </summary>
    public bool Start()
    {
        if (IsRunning)
            return false;

        IsRunning = true;
        _currentState = BotState.Pathing;
        _sessionStartTime = DateTime.UtcNow;
        _killsCount = 0;

        // Reset death controller state
        _deathController.Reset();

        // Start auto-stop monitoring
        _autoStopController.StartSession();

        // Start waypoint playback
        _waypointPlayer.Play();

        OnStarted?.Invoke();
        return true;
    }

    /// <summary>
    /// Stops the bot.
    /// </summary>
    public void Stop(StopReason reason = StopReason.Manual)
    {
        if (!IsRunning)
            return;

        IsRunning = false;

        // Stop auto-stop monitoring
        _autoStopController.StopSession();

        // Save map data before stopping
        _mapDiscoveryController.SaveToDisk();

        // Stop all subsystems
        _waypointPlayer.Stop();
        _combatController.StopCombat();
        _lootController.CancelLooting();
        _restController.StopResting();
        _deathController.CancelResurrection();

        OnStopped?.Invoke(reason);
    }

    /// <summary>
    /// Saves map discovery data to disk.
    /// </summary>
    public void SaveMapData()
    {
        _mapDiscoveryController.SaveToDisk();
    }

    /// <summary>
    /// Loads map discovery data from disk.
    /// </summary>
    public void LoadMapData()
    {
        _mapDiscoveryController.LoadFromDisk();
    }

    /// <summary>
    /// Gets the current map discovery statistics.
    /// </summary>
    public MapDataStatistics GetMapStatistics()
    {
        return _mapDiscoveryController.GetStatistics();
    }

    /// <summary>
    /// Updates the bot. Should be called regularly.
    /// </summary>
    public void Update()
    {
        if (!IsRunning)
            return;

        // Check if paused - if so, skip all updates
        if (_safetyController.IsPaused)
            return;

        // Check auto-stop conditions (runtime, stuck time)
        if (_autoStopController.CheckStopConditions())
            return;

        // Update map discovery (always active when enabled)
        if (AutoMapping)
        {
            _mapDiscoveryController.Update();
        }

        // Update death controller (detects death/resurrection)
        _deathController.Update();

        // Check for death state transition
        if (_deathController.IsDead && _currentState != BotState.Dead)
        {
            TransitionTo(BotState.Dead);
            return;
        }

        // Check death count limit
        if (_deathController.DeathCount >= MaxDeathCount)
        {
            Stop(StopReason.DeathLimitReached);
            return;
        }

        // State machine
        switch (_currentState)
        {
            case BotState.Idle:
                UpdateIdle();
                break;

            case BotState.Pathing:
                UpdatePathing();
                break;

            case BotState.SearchingForTarget:
                UpdateSearchingForTarget();
                break;

            case BotState.Pulling:
            case BotState.InCombat:
                UpdateCombat();
                break;

            case BotState.Looting:
                UpdateLooting();
                break;

            case BotState.Resting:
                UpdateResting();
                break;

            case BotState.Dead:
                UpdateDead();
                break;
        }
    }

    /// <summary>
    /// Updates the idle state.
    /// </summary>
    private void UpdateIdle()
    {
        // Transition to pathing
        TransitionTo(BotState.Pathing);
    }

    /// <summary>
    /// Updates the pathing state.
    /// </summary>
    private void UpdatePathing()
    {
        _waypointPlayer.Update();

        // Check if we need to rest
        if (AutoRest && _restController.NeedsRest())
        {
            TransitionTo(BotState.Resting);
            return;
        }

        // Check for available targets while pathing
        if (_targetSelector.HasValidTargets())
        {
            _waypointPlayer.Pause();
            TransitionTo(BotState.SearchingForTarget);
        }
    }

    /// <summary>
    /// Updates the target search state.
    /// </summary>
    private void UpdateSearchingForTarget()
    {
        var target = _targetSelector.FindBestTarget();
        if (target == null)
        {
            // No target found, resume pathing
            _waypointPlayer.Resume();
            TransitionTo(BotState.Pathing);
            return;
        }

        // Found target - engage
        _combatController.EngageTarget(target.Value);
        TransitionTo(BotState.Pulling);
    }

    /// <summary>
    /// Updates the combat states (Pulling and InCombat).
    /// </summary>
    private void UpdateCombat()
    {
        _combatController.Update();

        // Check if combat ended (not in combat anymore)
        if (!_combatController.IsInCombat)
        {
            if (AutoLoot && _lootController.GetLootableCorpseCount() > 0)
            {
                TransitionTo(BotState.Looting);
            }
            else if (AutoRest && _restController.NeedsRest())
            {
                TransitionTo(BotState.Resting);
            }
            else
            {
                TransitionTo(BotState.Pathing);
            }
        }
    }

    /// <summary>
    /// Updates the looting state.
    /// </summary>
    private void UpdateLooting()
    {
        if (!_lootController.IsLooting)
        {
            // Start looting nearest corpse
            if (!_lootController.LootNearestCorpse())
            {
                // No more corpses to loot
                if (AutoRest && _restController.NeedsRest())
                {
                    TransitionTo(BotState.Resting);
                }
                else
                {
                    TransitionTo(BotState.Pathing);
                }
                return;
            }
        }

        _lootController.Update();
    }

    /// <summary>
    /// Updates the resting state.
    /// </summary>
    private void UpdateResting()
    {
        if (!_restController.IsResting)
        {
            _restController.StartResting();
        }

        _restController.Update();
    }

    /// <summary>
    /// Updates the dead state.
    /// </summary>
    private void UpdateDead()
    {
        // DeathController handles resurrection
        // If resurrection fails/times out, the bot will be stopped via event
        if (!_deathController.IsDead)
        {
            // Player has been resurrected, transition to resting to recover
            TransitionTo(BotState.Resting);
        }
    }

    /// <summary>
    /// Transitions to a new state.
    /// </summary>
    private void TransitionTo(BotState newState)
    {
        if (_currentState == newState)
            return;

        var oldState = _currentState;
        _currentState = newState;

        OnStateChanged?.Invoke(oldState, newState);
    }

    /// <summary>
    /// Handles combat end event.
    /// </summary>
    private void HandleCombatEnded(CombatEndReason reason)
    {
        if (reason == CombatEndReason.TargetDead)
        {
            _killsCount++;
            OnKill?.Invoke();
        }
    }

    /// <summary>
    /// Handles loot complete event.
    /// </summary>
    private void HandleLootCompleted(LootResult result)
    {
        _lootController.CancelLooting();

        if (AutoRest && _restController.NeedsRest())
        {
            TransitionTo(BotState.Resting);
        }
        else
        {
            TransitionTo(BotState.Pathing);
        }
    }

    /// <summary>
    /// Handles rest complete event.
    /// </summary>
    private void HandleRestCompleted(RestResult result)
    {
        _restController.StopResting();
        TransitionTo(BotState.Pathing);
    }

    /// <summary>
    /// Handles resurrection completed event.
    /// </summary>
    private void HandleResurrectionCompleted(ResurrectResult result)
    {
        // After resurrection, transition to resting state to recover
        // This allows the player to regen health/mana and rebuff if needed
        if (AutoRest)
        {
            TransitionTo(BotState.Resting);
        }
        else
        {
            TransitionTo(BotState.Pathing);
        }
    }

    /// <summary>
    /// Handles player death event from DeathController.
    /// </summary>
    private void HandlePlayerDeath()
    {
        // Forward the death event to any listeners
        OnDeath?.Invoke();
    }

    /// <summary>
    /// Handles resurrection failed event.
    /// </summary>
    private void HandleResurrectionFailed(ResurrectResult result)
    {
        // If resurrection fails, stop the bot
        Stop(StopReason.PlayerDied);
    }

    /// <summary>
    /// Handles emergency stop triggered event from SafetyController.
    /// </summary>
    private void HandleEmergencyStop()
    {
        // Stop the bot immediately with emergency stop reason
        Stop(StopReason.EmergencyStop);
    }

    /// <summary>
    /// Handles paused event from SafetyController.
    /// </summary>
    private void HandlePaused()
    {
        // Pause all subsystems
        _waypointPlayer.Pause();

        // Forward the event
        OnPaused?.Invoke();
    }

    /// <summary>
    /// Handles resumed event from SafetyController.
    /// </summary>
    private void HandleResumed()
    {
        // Resume waypoint player
        _waypointPlayer.Resume();

        // Forward the event
        OnResumed?.Invoke();
    }

    /// <summary>
    /// Checks hotkey state for safety controls.
    /// </summary>
    /// <param name="isKeyDown">Function to check if a key is currently pressed.</param>
    public void UpdateHotkeys(Func<Input.KeyCode, bool> isKeyDown)
    {
        _safetyController.UpdateHotkeys(isKeyDown);

        // If emergency stop was triggered, handle it
        if (_safetyController.EmergencyStopTriggered && IsRunning)
        {
            HandleEmergencyStop();
            _safetyController.ResetEmergencyStop();
        }
    }

    /// <summary>
    /// Handles runtime limit reached event from AutoStopController.
    /// </summary>
    private void HandleRuntimeLimitReached()
    {
        Stop(StopReason.RuntimeLimitReached);
    }

    /// <summary>
    /// Handles stuck time limit reached event from AutoStopController.
    /// </summary>
    private void HandleStuckTimeLimitReached()
    {
        Stop(StopReason.Stuck);
    }

    /// <summary>
    /// Handles stuck state changed event from AutoStopController.
    /// Logs stuck state changes and forwards the event.
    /// </summary>
    private void HandleStuckStateChanged(bool isStuck)
    {
        // Forward the event if anyone is listening
        OnStuckStateChanged?.Invoke(isStuck);
    }

    /// <summary>
    /// Handles movement stuck state changed event from WaypointPlayer.
    /// Updates the AutoStopController with the new stuck state.
    /// </summary>
    private void HandleMovementStuckChanged(bool isStuck)
    {
        _autoStopController.SetStuckState(isStuck);
    }

    /// <summary>
    /// Applies configuration from a BotConfig to the bot.
    /// </summary>
    /// <param name="config">The configuration to apply.</param>
    public void ApplyConfig(BotConfig config)
    {
        if (config == null)
            return;

        // Apply session limits
        MaxDeathCount = config.MaxDeathCount;
        AutoLoot = config.AutoLoot;
        AutoRest = true; // Auto rest is always enabled
        AutoMapping = config.AutoMappingEnabled;
        _autoStopController.MaxSessionRuntimeMinutes = config.MaxSessionRuntimeMinutes;
        _autoStopController.MaxStuckTimeSeconds = config.MaxStuckTimeSeconds;
    }

    /// <summary>
    /// Event raised when the stuck state changes.
    /// </summary>
    public event Action<bool>? OnStuckStateChanged;
}

/// <summary>
/// The reason why the bot stopped.
/// </summary>
public enum StopReason
{
    /// <summary>Bot was stopped manually.</summary>
    Manual,
    /// <summary>Death limit was reached.</summary>
    DeathLimitReached,
    /// <summary>Player died.</summary>
    PlayerDied,
    /// <summary>Runtime limit was reached.</summary>
    RuntimeLimitReached,
    /// <summary>Got stuck.</summary>
    Stuck,
    /// <summary>Bags are full and no vendor available.</summary>
    BagsFull,
    /// <summary>Emergency stop was triggered.</summary>
    EmergencyStop
}
