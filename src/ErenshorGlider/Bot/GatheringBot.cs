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
/// Coordinates all bot systems into a gathering-focused state machine.
/// Prioritizes resource nodes over combat, only fights when attacked.
/// </summary>
public class GatheringBot
{
    private readonly WaypointPlayer _waypointPlayer;
    private readonly CombatController _combatController;
    private readonly RestController _restController;
    private readonly DeathController _deathController;
    private readonly MapDiscoveryController _mapDiscoveryController;
    private readonly PositionTracker _positionTracker;
    private readonly SafetyController _safetyController;
    private readonly AutoStopController _autoStopController;
    private readonly ErenshorGlider.Navigation.Navigation _navigation;

    /// <summary>
    /// Represents the current state of the gathering bot.
    /// </summary>
    public enum GatheringState
    {
        Idle,
        Pathing,
        SearchingForNode,
        MovingToNode,
        Gathering,
        DefensiveCombat,  // Only when attacked
        Resting,
        Dead
    }

    private GatheringState _currentState = GatheringState.Idle;
    private EntityInfo? _targetNode;
    private DateTime _sessionStartTime = DateTime.UtcNow;
    private int _nodesGathered = 0;

    /// <summary>
    /// Gets the current bot state.
    /// </summary>
    public GatheringState CurrentState => _currentState;

    /// <summary>
    /// Gets or sets the maximum death count before stopping.
    /// </summary>
    public int MaxDeathCount { get; set; } = 3;

    /// <summary>
    /// Gets or sets whether to enable auto-rest.
    /// </summary>
    public bool AutoRest { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable auto-mapping.
    /// </summary>
    public bool AutoMapping { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum distance to detour for a resource node.
    /// </summary>
    public float MaxNodeDetourDistance { get; set; } = 100f;

    /// <summary>
    /// Gets or sets whether to engage mobs that attack while gathering.
    /// </summary>
    public bool DefendAgainstAttackers { get; set; } = true;

    /// <summary>
    /// Gets or sets the time to wait at a node after gathering (in seconds).
    /// </summary>
    public float GatherWaitSeconds { get; set; } = 2f;

    /// <summary>
    /// Gets the number of nodes gathered this session.
    /// </summary>
    public int NodesGathered => _nodesGathered;

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
    /// Gets whether the bot is currently being attacked (in defensive combat).
    /// </summary>
    public bool IsBeingAttacked { get; private set; }

    /// <summary>
    /// Event raised when the bot state changes.
    /// </summary>
    public event Action<GatheringState, GatheringState>? OnStateChanged;

    /// <summary>
    /// Event raised when the bot starts.
    /// </summary>
    public event Action? OnStarted;

    /// <summary>
    /// Event raised when the bot stops.
    /// </summary>
    public event Action<StopReason>? OnStopped;

    /// <summary>
    /// Event raised when a node is gathered.
    /// </summary>
    public event Action? OnNodeGathered;

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
    /// Creates a new GatheringBot.
    /// </summary>
    public GatheringBot(
        WaypointPlayer waypointPlayer,
        CombatController combatController,
        RestController restController,
        DeathController deathController,
        MapDiscoveryController mapDiscoveryController,
        PositionTracker positionTracker,
        SafetyController safetyController,
        AutoStopController autoStopController,
        Navigation navigation)
    {
        _waypointPlayer = waypointPlayer ?? throw new ArgumentNullException(nameof(waypointPlayer));
        _combatController = combatController ?? throw new ArgumentNullException(nameof(combatController));
        _restController = restController ?? throw new ArgumentNullException(nameof(restController));
        _deathController = deathController ?? throw new ArgumentNullException(nameof(deathController));
        _mapDiscoveryController = mapDiscoveryController ?? throw new ArgumentNullException(nameof(mapDiscoveryController));
        _positionTracker = positionTracker ?? throw new ArgumentNullException(nameof(positionTracker));
        _safetyController = safetyController ?? throw new ArgumentNullException(nameof(safetyController));
        _autoStopController = autoStopController ?? throw new ArgumentNullException(nameof(autoStopController));
        _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));

        // Wire up event handlers
        _combatController.OnCombatEnded += HandleCombatEnded;
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
        _currentState = GatheringState.Pathing;
        _sessionStartTime = DateTime.UtcNow;
        _nodesGathered = 0;

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
        _restController.StopResting();
        _deathController.CancelResurrection();
        _navigation.StopMovement();

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
        if (_deathController.IsDead && _currentState != GatheringState.Dead)
        {
            TransitionTo(GatheringState.Dead);
            return;
        }

        // Check death count limit
        if (_deathController.DeathCount >= MaxDeathCount)
        {
            Stop(StopReason.DeathLimitReached);
            return;
        }

        // Check if we're being attacked (defensive combat trigger)
        CheckForAttackers();

        // State machine
        switch (_currentState)
        {
            case GatheringState.Idle:
                UpdateIdle();
                break;

            case GatheringState.Pathing:
                UpdatePathing();
                break;

            case GatheringState.SearchingForNode:
                UpdateSearchingForNode();
                break;

            case GatheringState.MovingToNode:
                UpdateMovingToNode();
                break;

            case GatheringState.Gathering:
                UpdateGathering();
                break;

            case GatheringState.DefensiveCombat:
                UpdateDefensiveCombat();
                break;

            case GatheringState.Resting:
                UpdateResting();
                break;

            case GatheringState.Dead:
                UpdateDead();
                break;
        }
    }

    /// <summary>
    /// Checks if the player is being attacked and triggers defensive combat.
    /// </summary>
    private void CheckForAttackers()
    {
        if (!DefendAgainstAttackers)
            return;

        var combatState = _positionTracker.CurrentCombatState;
        if (!combatState.HasValue)
            return;

        // Check if we're in combat and were not already in defensive combat
        bool isInCombat = combatState.Value.InCombat;
        bool wasNotInDefensiveCombat = _currentState != GatheringState.DefensiveCombat;

        if (isInCombat && wasNotInDefensiveCombat && !IsBeingAttacked)
        {
            // We're being attacked - transition to defensive combat
            IsBeingAttacked = true;

            // Stop whatever we were doing
            if (_currentState == GatheringState.MovingToNode)
            {
                _navigation.StopMovement();
            }
            else if (_currentState == GatheringState.Gathering)
            {
                // Cancel gathering
                _navigation.StopMovement();
            }

            TransitionTo(GatheringState.DefensiveCombat);
        }
        else if (!isInCombat && IsBeingAttacked)
        {
            // No longer in combat
            IsBeingAttacked = false;
        }
    }

    /// <summary>
    /// Updates the idle state.
    /// </summary>
    private void UpdateIdle()
    {
        // Transition to pathing
        TransitionTo(GatheringState.Pathing);
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
            _waypointPlayer.Pause();
            TransitionTo(GatheringState.Resting);
            return;
        }

        // Check for nearby resource nodes
        var nearbyNode = FindNearestResourceNode();
        if (nearbyNode.HasValue)
        {
            var node = nearbyNode.Value;
            var playerPos = _positionTracker.CurrentPosition;

            if (playerPos.HasValue)
            {
                float distance = ErenshorGlider.Navigation.Navigation.CalculateDistance(playerPos.Value, node.Position);

                // Only detour if node is within reasonable distance
                if (distance <= MaxNodeDetourDistance)
                {
                    _waypointPlayer.Pause();
                    _targetNode = node;
                    TransitionTo(GatheringState.MovingToNode);
                    return;
                }
            }
        }
    }

    /// <summary>
    /// Updates the node search state.
    /// </summary>
    private void UpdateSearchingForNode()
    {
        var node = FindNearestResourceNode();
        if (node == null)
        {
            // No node found, resume pathing
            _waypointPlayer.Resume();
            TransitionTo(GatheringState.Pathing);
            return;
        }

        _targetNode = node.Value;
        TransitionTo(GatheringState.MovingToNode);
    }

    /// <summary>
    /// Updates the moving to node state.
    /// </summary>
    private void UpdateMovingToNode()
    {
        if (!_targetNode.HasValue)
        {
            TransitionTo(GatheringState.Pathing);
            return;
        }

        var node = _targetNode.Value;
        var playerPos = _positionTracker.CurrentPosition;

        if (!playerPos.HasValue)
        {
            TransitionTo(GatheringState.Pathing);
            return;
        }

        float distance = ErenshorGlider.Navigation.Navigation.CalculateDistance(playerPos.Value, node.Position);

        // Check if we've reached the node
        if (distance <= 2f) // Within gathering range
        {
            TransitionTo(GatheringState.Gathering);
            return;
        }

        // Move toward the node
        _navigation.MoveTo(node.Position.X, node.Position.Y, node.Position.Z);
        _navigation.Update();

        // Face the node
        _navigation.FaceTarget(node.Position);

        // Check if we got stuck
        if (_navigation.IsStuck)
        {
            // Give up on this node and return to pathing
            _navigation.StopMovement();
            _targetNode = null;
            _waypointPlayer.Resume();
            TransitionTo(GatheringState.Pathing);
        }
    }

    /// <summary>
    /// Updates the gathering state.
    /// </summary>
    private DateTime _gatherStartTime;

    private void UpdateGathering()
    {
        if (!_targetNode.HasValue)
        {
            TransitionTo(GatheringState.Pathing);
            return;
        }

        // Check if we're being attacked while gathering
        if (IsBeingAttacked)
        {
            TransitionTo(GatheringState.DefensiveCombat);
            return;
        }

        var playerPos = _positionTracker.CurrentPosition;
        if (!playerPos.HasValue)
        {
            TransitionTo(GatheringState.Pathing);
            return;
        }

        float distance = ErenshorGlider.Navigation.Navigation.CalculateDistance(playerPos.Value, _targetNode.Value.Position);

        // If we've moved too far from the node, go back to moving to it
        if (distance > 5f)
        {
            TransitionTo(GatheringState.MovingToNode);
            return;
        }

        // Face the node and interact
        _navigation.FaceTarget(_targetNode.Value.Position);

        // Simulate gathering interaction (this would use the actual game interaction)
        if (_gatherStartTime == default)
        {
            _gatherStartTime = DateTime.UtcNow;
        }

        // Check if gathering is complete (based on wait time)
        if ((DateTime.UtcNow - _gatherStartTime).TotalSeconds >= GatherWaitSeconds)
        {
            // Gathering complete
            _nodesGathered++;
            OnNodeGathered?.Invoke();

            // Reset gather time
            _gatherStartTime = default;

            // Clear target node
            _targetNode = null;

            // Check if we need to rest
            if (AutoRest && _restController.NeedsRest())
            {
                TransitionTo(GatheringState.Resting);
            }
            else
            {
                // Resume pathing
                _waypointPlayer.Resume();
                TransitionTo(GatheringState.Pathing);
            }
        }
    }

    /// <summary>
    /// Updates the defensive combat state (only when attacked).
    /// </summary>
    private void UpdateDefensiveCombat()
    {
        _combatController.Update();

        // Check if combat ended (no longer being attacked)
        if (!IsBeingAttacked && !_combatController.IsInCombat)
        {
            // Combat over - decide what to do next
            if (_targetNode.HasValue)
            {
                // We were gathering, try to return to it
                TransitionTo(GatheringState.MovingToNode);
            }
            else if (AutoRest && _restController.NeedsRest())
            {
                TransitionTo(GatheringState.Resting);
            }
            else
            {
                _waypointPlayer.Resume();
                TransitionTo(GatheringState.Pathing);
            }
        }
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
            TransitionTo(GatheringState.Resting);
        }
    }

    /// <summary>
    /// Finds the nearest resource node.
    /// </summary>
    private EntityInfo? FindNearestResourceNode()
    {
        var entities = _positionTracker.CurrentNearbyEntities;
        if (!entities.HasValue)
            return null;

        var playerPos = _positionTracker.CurrentPosition;
        if (!playerPos.HasValue)
            return null;

        EntityInfo? nearestNode = null;
        float nearestDistance = float.MaxValue;

        foreach (var entity in entities.Value.Items)
        {
            // Only look for resource nodes
            if (entity.Type != EntityType.Node || entity.IsDead)
                continue;

            float distance = ErenshorGlider.Navigation.Navigation.CalculateDistance(playerPos.Value, entity.Position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestNode = entity;
            }
        }

        return nearestNode;
    }

    /// <summary>
    /// Transitions to a new state.
    /// </summary>
    private void TransitionTo(GatheringState newState)
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
        if (reason == CombatEndReason.TargetDead && IsBeingAttacked)
        {
            // We defeated an attacker, but may still be in combat
            var combatState = _positionTracker.CurrentCombatState;
            if (combatState.HasValue && !combatState.Value.InCombat)
            {
                IsBeingAttacked = false;
            }
        }
    }

    /// <summary>
    /// Handles rest complete event.
    /// </summary>
    private void HandleRestCompleted(RestResult result)
    {
        _restController.StopResting();

        if (_targetNode.HasValue)
        {
            // Return to gathering the node we were at
            TransitionTo(GatheringState.MovingToNode);
        }
        else
        {
            _waypointPlayer.Resume();
            TransitionTo(GatheringState.Pathing);
        }
    }

    /// <summary>
    /// Handles resurrection completed event.
    /// </summary>
    private void HandleResurrectionCompleted(ResurrectResult result)
    {
        // After resurrection, transition to resting state to recover
        if (AutoRest)
        {
            TransitionTo(GatheringState.Resting);
        }
        else
        {
            _waypointPlayer.Resume();
            TransitionTo(GatheringState.Pathing);
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
        _navigation.StopMovement();

        // Forward the event
        OnPaused?.Invoke();
    }

    /// <summary>
    /// Handles resumed event from SafetyController.
    /// </summary>
    private void HandleResumed()
    {
        // Resume waypoint player if not in special state
        if (_currentState == GatheringState.Pathing)
        {
            _waypointPlayer.Resume();
        }

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

        // Apply settings
        MaxDeathCount = config.MaxDeathCount;
        AutoRest = true;
        AutoMapping = config.AutoMappingEnabled;
        MaxNodeDetourDistance = config.MaxWaypointDistance;
        _autoStopController.MaxSessionRuntimeMinutes = config.MaxSessionRuntimeMinutes;
        _autoStopController.MaxStuckTimeSeconds = config.MaxStuckTimeSeconds;
    }

    /// <summary>
    /// Event raised when the stuck state changes.
    /// </summary>
    public event Action<bool>? OnStuckStateChanged;
}
