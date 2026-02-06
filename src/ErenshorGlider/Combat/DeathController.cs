using System;
using ErenshorGlider.GameState;
using ErenshorGlider.Input;

namespace ErenshorGlider.Combat;

/// <summary>
/// Controller for handling player death and resurrection.
/// </summary>
public class DeathController
{
    private readonly PositionTracker _positionTracker;
    private readonly InputController _inputController;

    private bool _isResurrecting = false;
    private DateTime _deathTime = DateTime.UtcNow;
    private DateTime _resurrectStartTime = DateTime.UtcNow;
    private PlayerPosition? _deathPosition;

    /// <summary>
    /// Gets whether the player is currently dead.
    /// </summary>
    public bool IsDead { get; private set; }

    /// <summary>
    /// Gets whether the controller is currently in the resurrection process.
    /// </summary>
    public bool IsResurrecting => _isResurrecting;

    /// <summary>
    /// Gets whether the player needs to rebuff after resurrection.
    /// </summary>
    public bool NeedsRebuff { get; private set; }

    /// <summary>
    /// Gets or sets the maximum time to wait for resurrection before giving up.
    /// </summary>
    public float MaxResurrectWaitSeconds { get; set; } = 30f;

    /// <summary>
    /// Gets or sets the delay after resurrection before resuming activities.
    /// This allows time for the game to fully load after resurrection.
    /// </summary>
    public float PostResurrectDelaySeconds { get; set; } = 3f;

    /// <summary>
    /// Gets or sets whether to auto-release spirit after death.
    /// </summary>
    public bool AutoReleaseSpirit { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to auto-accept resurrection at graveyard.
    /// </summary>
    public bool AutoResurrectAtGraveyard { get; set; } = true;

    /// <summary>
    /// Gets the number of times the player has died this session.
    /// </summary>
    public int DeathCount { get; private set; }

    /// <summary>
    /// Gets the time since the player died.
    /// </summary>
    public TimeSpan TimeSinceDeath => IsDead ? DateTime.UtcNow - _deathTime : TimeSpan.Zero;

    /// <summary>
    /// Gets the position where the player died (if known).
    /// </summary>
    public PlayerPosition? DeathPosition => _deathPosition;

    /// <summary>
    /// Event raised when the player dies.
    /// </summary>
    public event Action? OnPlayerDeath;

    /// <summary>
    /// Event raised when resurrection begins.
    /// </summary>
    public event Action? OnResurrectionStarted;

    /// <summary>
    /// Event raised when resurrection completes successfully.
    /// </summary>
    public event Action<ResurrectResult>? OnResurrectionCompleted;

    /// <summary>
    /// Event raised when resurrection fails or times out.
    /// </summary>
    public event Action<ResurrectResult>? OnResurrectionFailed;

    /// <summary>
    /// Creates a new DeathController.
    /// </summary>
    public DeathController(
        PositionTracker positionTracker,
        InputController inputController)
    {
        _positionTracker = positionTracker ?? throw new ArgumentNullException(nameof(positionTracker));
        _inputController = inputController ?? throw new ArgumentNullException(nameof(inputController));
    }

    /// <summary>
    /// Updates the death controller. Should be called regularly.
    /// </summary>
    public void Update()
    {
        var combatState = _positionTracker.CurrentCombatState;
        bool currentlyDead = combatState.HasValue && !combatState.Value.IsAlive;

        // Check for death transition
        if (currentlyDead && !IsDead)
        {
            HandlePlayerDeath();
        }
        // Check for resurrection (alive again)
        else if (!currentlyDead && IsDead)
        {
            HandlePlayerResurrected();
        }

        // Handle resurrection process
        if (_isResurrecting)
        {
            UpdateResurrection();
        }
    }

    /// <summary>
    /// Handles the player's death.
    /// </summary>
    private void HandlePlayerDeath()
    {
        IsDead = true;
        NeedsRebuff = true; // Player loses buffs on death
        DeathCount++;
        _deathTime = DateTime.UtcNow;
        _deathPosition = _positionTracker.CurrentPosition;

        OnPlayerDeath?.Invoke();

        // Auto-release spirit if enabled
        if (AutoReleaseSpirit)
        {
            ReleaseSpirit();
        }
    }

    /// <summary>
    /// Handles the player being resurrected (either via graveyard or other means).
    /// </summary>
    private void HandlePlayerResurrected()
    {
        IsDead = false;
        _isResurrecting = false;

        OnResurrectionCompleted?.Invoke(ResurrectResult.Success);
    }

    /// <summary>
    /// Releases the player's spirit (clicks "Release Spirit" button).
    /// In Erenshor, this is typically done by pressing a specific key or clicking a UI button.
    /// </summary>
    public void ReleaseSpirit()
    {
        if (!IsDead || _isResurrecting)
            return;

        _isResurrecting = true;
        _resurrectStartTime = DateTime.UtcNow;

        // The release spirit button varies by game - in Erenshor it's typically Enter or a dedicated button
        // We'll use Enter which is commonly used for UI interactions
        _inputController.Interact();

        OnResurrectionStarted?.Invoke();
    }

    /// <summary>
    /// Accepts resurrection at the graveyard (spirit healer).
    /// </summary>
    public void AcceptGraveyardResurrection()
    {
        if (!IsDead)
            return;

        // Typically this is done by clicking "Accept" on the resurrection dialog
        // which uses the Enter key
        _inputController.Interact();
    }

    /// <summary>
    /// Updates the resurrection process, checking for timeout.
    /// </summary>
    private void UpdateResurrection()
    {
        var timeSinceResurrectStart = DateTime.UtcNow - _resurrectStartTime;

        // Check for resurrection timeout
        if (timeSinceResurrectStart.TotalSeconds > MaxResurrectWaitSeconds)
        {
            _isResurrecting = false;
            OnResurrectionFailed?.Invoke(ResurrectResult.Timeout);
            return;
        }

        // If auto-resurrect at graveyard is enabled, try to accept resurrection
        // This assumes the graveyard dialog appears after releasing spirit
        if (AutoResurrectAtGraveyard && timeSinceResurrectStart.TotalSeconds > 1.0)
        {
            AcceptGraveyardResurrection();
        }
    }

    /// <summary>
    /// Cancels the current resurrection attempt.
    /// </summary>
    public void CancelResurrection()
    {
        _isResurrecting = false;
    }

    /// <summary>
    /// Resets the death controller state (e.g., after a successful bot stop).
    /// </summary>
    public void Reset()
    {
        IsDead = false;
        _isResurrecting = false;
        NeedsRebuff = false;
        _deathPosition = null;
    }

    /// <summary>
    /// Marks the player as having rebuffed after resurrection.
    /// </summary>
    public void MarkRebuffed()
    {
        NeedsRebuff = false;
    }

    /// <summary>
    /// Gets the time remaining until resurrection timeout.
    /// </summary>
    public TimeSpan GetResurrectTimeoutRemaining()
    {
        if (!_isResurrecting)
            return TimeSpan.Zero;

        var elapsed = DateTime.UtcNow - _resurrectStartTime;
        var timeout = TimeSpan.FromSeconds(MaxResurrectWaitSeconds);
        return elapsed > timeout ? TimeSpan.Zero : timeout - elapsed;
    }
}

/// <summary>
/// The result of a resurrection attempt.
/// </summary>
public enum ResurrectResult
{
    /// <summary>Resurrection succeeded.</summary>
    Success,
    /// <summary>Resurrection timed out.</summary>
    Timeout,
    /// <summary>Resurrection was cancelled.</summary>
    Cancelled,
    /// <summary>Resurrection failed for another reason.</summary>
    Failed
}
