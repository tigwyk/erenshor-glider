using System;
using ErenshorGlider.GameState;
using ErenshorGlider.Input;
using ErenshorGlider.Navigation;
using NavNavigation = ErenshorGlider.Navigation.Navigation;

#if !USE_REAL_GAME_TYPES
using ErenshorGlider.GameStubs;
#endif

namespace ErenshorGlider.Combat;

/// <summary>
/// Manages the complete combat engagement loop from pull to loot.
/// </summary>
public class CombatController
{
    private readonly InputController _inputController;
    private readonly NavNavigation _navigation;
    private readonly PositionTracker _positionTracker;
    private readonly RotationExecutor _rotationExecutor;
    private readonly TargetSelector _targetSelector;

    private enum CombatState
    {
        Idle,
        Pulling,
        InCombat,
        Looting,
        Fleeing
    }

    private CombatState _currentState = CombatState.Idle;
    private EntityInfo? _currentTarget;
    private DateTime _combatStartTime;
    private float _combatTimeoutSeconds = 30f;

    /// <summary>
    /// Gets whether currently in combat.
    /// </summary>
    public bool IsInCombat => _currentState != CombatState.Idle;

    /// <summary>
    /// Gets the current combat target.
    /// </summary>
    public EntityInfo? CurrentTarget => _currentTarget;

    /// <summary>
    /// Gets or sets the combat timeout in seconds.
    /// </summary>
    public float CombatTimeout
    {
        get => _combatTimeoutSeconds;
        set => _combatTimeoutSeconds = Math.Max(10f, value);
    }

    /// <summary>
    /// Gets or sets whether to chase fleeing targets.
    /// </summary>
    public bool ChaseFleeingTargets { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum chase distance.
    /// </summary>
    public float MaxChaseDistance { get; set; } = 30f;

    /// <summary>
    /// Gets or sets whether to use auto-attack during combat.
    /// When true, the bot will toggle auto-attack on when engaging hostile targets.
    /// </summary>
    public bool UseAutoAttack { get; set; } = true;

    /// <summary>
    /// Event raised when combat starts.
    /// </summary>
    public event Action<EntityInfo>? OnCombatStarted;

    /// <summary>
    /// Event raised when combat ends.
    /// </summary>
    public event Action<CombatEndReason>? OnCombatEnded;

    /// <summary>
    /// Event raised when target is pulled.
    /// </summary>
    public event Action<EntityInfo>? OnTargetPulled;

    /// <summary>
    /// Event raised when combat times out.
    /// </summary>
    public event Action? OnCombatTimeout;

    /// <summary>
    /// Creates a new CombatController.
    /// </summary>
    public CombatController(
        InputController inputController,
        NavNavigation navigation,
        PositionTracker positionTracker,
        RotationExecutor rotationExecutor,
        TargetSelector targetSelector)
    {
        _inputController = inputController ?? throw new ArgumentNullException(nameof(inputController));
        _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
        _positionTracker = positionTracker ?? throw new ArgumentNullException(nameof(positionTracker));
        _rotationExecutor = rotationExecutor ?? throw new ArgumentNullException(nameof(rotationExecutor));
        _targetSelector = targetSelector ?? throw new ArgumentNullException(nameof(targetSelector));
    }

    /// <summary>
    /// Starts combat with the specified target.
    /// </summary>
    public bool EngageTarget(in EntityInfo target)
    {
        if (!target.CanBeAttacked)
            return false;

        _currentTarget = target;
        _currentState = CombatState.Pulling;
        _combatStartTime = DateTime.UtcNow;

        // Enable auto-attack if configured and target is hostile
        if (UseAutoAttack && target.Hostility == TargetHostility.Hostile)
        {
            EnableAutoAttack();
        }

        OnTargetPulled?.Invoke(target);
        return true;
    }

    /// <summary>
    /// Finds and engages the best available target.
    /// </summary>
    public bool FindAndEngage()
    {
        var target = _targetSelector.FindBestTarget();
        if (target == null)
            return false;

        return EngageTarget(target.Value);
    }

    /// <summary>
    /// Stops combat and returns to idle state.
    /// </summary>
    public void StopCombat(CombatEndReason reason = CombatEndReason.Manual)
    {
        // Disable auto-attack when ending combat
        DisableAutoAttack();

        _currentTarget = null;
        _currentState = CombatState.Idle;
        _navigation.StopMovement();
        _inputController.StopAllMovement();

        OnCombatEnded?.Invoke(reason);
    }

    /// <summary>
    /// Updates the combat controller. Should be called regularly.
    /// </summary>
    public void Update()
    {
        switch (_currentState)
        {
            case CombatState.Idle:
                // Not in combat
                break;

            case CombatState.Pulling:
                UpdatePulling();
                break;

            case CombatState.InCombat:
                UpdateInCombat();
                break;

            case CombatState.Looting:
                // Looting handled by LootController
                break;

            case CombatState.Fleeing:
                UpdateFleeing();
                break;
        }

        // Check for combat timeout
        if (_currentState != CombatState.Idle)
        {
            var elapsed = (DateTime.UtcNow - _combatStartTime).TotalSeconds;
            if (elapsed > _combatTimeoutSeconds)
            {
                OnCombatTimeout?.Invoke();
                StopCombat(CombatEndReason.Timeout);
            }
        }

        // Update current target status
        UpdateTargetStatus();
    }

    /// <summary>
    /// Updates the pulling phase.
    /// </summary>
    private void UpdatePulling()
    {
        if (_currentTarget == null)
        {
            StopCombat(CombatEndReason.TargetLost);
            return;
        }

        // Move into attack range
        float maxRange = GetMaxAttackRange();
        float distance = _positionTracker.CurrentPosition != null
            ? Navigation.Navigation.CalculateDistance(_positionTracker.CurrentPosition.Value, _currentTarget.Value.Position)
            : float.MaxValue;

        if (distance > maxRange)
        {
            // Move toward target
            _navigation.MoveTo(_currentTarget.Value.Position);
        }
        else
        {
            // In range - face target and start combat
            if (_navigation.FaceTarget(_currentTarget.Value.Position))
            {
                // Still turning, wait
                return;
            }

            // Start combat rotation
            _currentState = CombatState.InCombat;
            OnCombatStarted?.Invoke(_currentTarget.Value);
        }
    }

    /// <summary>
    /// Updates the in-combat phase.
    /// </summary>
    private void UpdateInCombat()
    {
        if (_currentTarget == null || _currentTarget.Value.IsDead)
        {
            // Target dead - combat complete
            StopCombat(CombatEndReason.TargetDead);
            return;
        }

        // Check if target fled (too far)
        float distance = _positionTracker.CurrentPosition != null
            ? Navigation.Navigation.CalculateDistance(_positionTracker.CurrentPosition.Value, _currentTarget.Value.Position)
            : float.MaxValue;

        if (distance > MaxChaseDistance)
        {
            if (ChaseFleeingTargets)
            {
                // Chase target
                _navigation.MoveTo(_currentTarget.Value.Position);
            }
            else
            {
                // Don't chase - end combat
                StopCombat(CombatEndReason.TargetFled);
                return;
            }
        }

        // Face target if not already facing
        if (!_navigation.IsFacing(_currentTarget.Value.Position))
        {
            _navigation.FaceTarget(_currentTarget.Value.Position);
        }

        // Execute rotation
        _rotationExecutor.ExecuteNextAbility();
    }

    /// <summary>
    /// Updates the fleeing phase.
    /// </summary>
    private void UpdateFleeing()
    {
        // TODO: Implement fleeing logic
        _currentState = CombatState.Idle;
    }

    /// <summary>
    /// Updates the target status from game state.
    /// </summary>
    private void UpdateTargetStatus()
    {
        if (_currentTarget == null)
            return;

        // Update target from game state to get current HP, etc.
        var currentTargetInfo = _positionTracker.CurrentTargetInfo;
        if (currentTargetInfo != null && currentTargetInfo.Value.HasTarget)
        {
            // Sync with game target
            // For now, we just check if it's dead
        }
    }

    /// <summary>
    /// Gets the maximum attack range for the current rotation.
    /// </summary>
    private float GetMaxAttackRange()
    {
        // TODO: Calculate based on abilities in rotation
        return 5f; // Default melee range
    }

    #region Auto-Attack

    /// <summary>
    /// Enables auto-attack using the game's PlayerCombat API.
    /// </summary>
    private void EnableAutoAttack()
    {
#if !USE_REAL_GAME_TYPES
        // Stub implementation - no-op when using stubs
        return;
#else
        try
        {
            if (GameData.PlayerCombat == null)
                return;

            // Force auto-attack on for immediate effect
            GameData.PlayerCombat.ForceAttackOn();
        }
        catch
        {
            // Gracefully handle errors during scene transitions
        }
#endif
    }

    /// <summary>
    /// Disables auto-attack using the game's PlayerCombat API.
    /// </summary>
    private void DisableAutoAttack()
    {
#if !USE_REAL_GAME_TYPES
        // Stub implementation - no-op when using stubs
        return;
#else
        try
        {
            if (GameData.PlayerCombat == null)
                return;

            // Check if auto-attack is currently enabled before toggling
            if (GameData.PlayerCombat.InCombat)
            {
                GameData.PlayerCombat.ToggleAttack();
            }
        }
        catch
        {
            // Gracefully handle errors during scene transitions
        }
#endif
    }

    #endregion
}

/// <summary>
/// The reason why combat ended.
/// </summary>
public enum CombatEndReason
{
    /// <summary>Combat was stopped manually.</summary>
    Manual,
    /// <summary>Target died.</summary>
    TargetDead,
    /// <summary>Target fled and wasn't chased.</summary>
    TargetFled,
    /// <summary>Combat timed out (stuck fighting).</summary>
    Timeout,
    /// <summary>Player died.</summary>
    PlayerDied,
    /// <summary>Target was lost.</summary>
    TargetLost,
    /// <summary>No mana/resources.</summary>
    OutOfResources
}
