using System;
using ErenshorGlider.GameState;
using ErenshorGlider.Input;

namespace ErenshorGlider.Combat;

/// <summary>
/// Manages player rest and recovery between combat encounters.
/// </summary>
public class RestController
{
    private readonly PositionTracker _positionTracker;
    private readonly InputController _inputController;

    private bool _isResting;
    private DateTime _restStartTime;
    private float _restDuration;

    /// <summary>
    /// Gets or sets the minimum health percentage to trigger rest.
    /// </summary>
    public float MinHealthPercent { get; set; } = 50f;

    /// <summary>
    /// Gets or sets the minimum mana percentage to trigger rest.
    /// </summary>
    public float MinManaPercent { get; set; } = 30f;

    /// <summary>
    /// Gets or sets the health percentage to reach before stopping rest.
    /// </summary>
    public float TargetHealthPercent { get; set; } = 90f;

    /// <summary>
    /// Gets or sets the mana percentage to reach before stopping rest.
    /// </summary>
    public float TargetManaPercent { get; set; } = 80f;

    /// <summary>
    /// Gets or sets the maximum rest duration in seconds.
    /// </summary>
    public float MaxRestDuration { get; set; } = 60f;

    /// <summary>
    /// Gets or sets the food item to use for health regeneration.
    /// </summary>
    public string? FoodItem { get; set; }

    /// <summary>
    /// Gets or sets the drink item to use for mana regeneration.
    /// </summary>
    public string? DrinkItem { get; set; }

    /// <summary>
    /// Gets whether currently resting.
    /// </summary>
    public bool IsResting => _isResting;

    /// <summary>
    /// Event raised when rest starts.
    /// </summary>
    public event Action? OnRestStarted;

    /// <summary>
    /// Event raised when rest completes.
    /// </summary>
    public event Action<RestResult>? OnRestCompleted;

    /// <summary>
    /// Creates a new RestController.
    /// </summary>
    public RestController(PositionTracker positionTracker, InputController inputController)
    {
        _positionTracker = positionTracker ?? throw new ArgumentNullException(nameof(positionTracker));
        _inputController = inputController ?? throw new ArgumentNullException(nameof(inputController));
    }

    /// <summary>
    /// Checks if the player needs to rest.
    /// </summary>
    public bool NeedsRest()
    {
        // Don't rest if in combat
        var combatState = _positionTracker.CurrentCombatState;
        if (combatState != null && combatState.Value.InCombat)
            return false;

        var vitals = _positionTracker.CurrentVitals;
        if (vitals == null)
            return false;

        // Check if below minimum thresholds
        if (vitals.HealthPercent < MinHealthPercent)
            return true;

        if (vitals.ManaPercent < MinManaPercent)
            return true;

        return false;
    }

    /// <summary>
    /// Starts resting to recover health and mana.
    /// </summary>
    public bool StartResting()
    {
        if (_isResting)
            return false;

        _isResting = true;
        _restStartTime = DateTime.UtcNow;
        _restDuration = MaxRestDuration;

        // Use food if available
        if (!string.IsNullOrEmpty(FoodItem))
        {
            UseConsumable(FoodItem);
        }

        // Use drink if available
        if (!string.IsNullOrEmpty(DrinkItem))
        {
            UseConsumable(DrinkItem);
        }

        OnRestStarted?.Invoke();
        return true;
    }

    /// <summary>
    /// Stops resting.
    /// </summary>
    public void StopResting(RestResult result = RestResult.Manual)
    {
        if (!_isResting)
            return;

        _isResting = false;
        OnRestCompleted?.Invoke(result);
    }

    /// <summary>
    /// Updates the rest controller. Should be called regularly.
    /// </summary>
    public void Update()
    {
        if (!_isResting)
            return;

        // Check if rest is complete
        var vitals = _positionTracker.CurrentVitals;
        if (vitals != null)
        {
            bool healthRecovered = vitals.HealthPercent >= TargetHealthPercent;
            bool manaRecovered = vitals.ManaPercent >= TargetManaPercent;

            if (healthRecovered && manaRecovered)
            {
                StopResting(RestResult.FullyRecovered);
                return;
            }
        }

        // Check for timeout
        var elapsed = (DateTime.UtcNow - _restStartTime).TotalSeconds;
        if (elapsed > _restDuration)
        {
            StopResting(RestResult.Timeout);
            return;
        }

        // Don't rest while in combat
        var combatState = _positionTracker.CurrentCombatState;
        if (combatState != null && combatState.Value.InCombat)
        {
            StopResting(RestResult.EnteredCombat);
        }
    }

    /// <summary>
    /// Uses a consumable item by name.
    /// </summary>
    private void UseConsumable(string itemName)
    {
        // TODO: Implement consumable usage
        // This requires:
        // 1. Finding the item in inventory
        // 2. Determining its keybind or using it directly
        // For now, this is a placeholder
    }

    /// <summary>
    /// Checks if the player is at full health and mana.
    /// </summary>
    public bool IsFullyRecovered()
    {
        var vitals = _positionTracker.CurrentVitals;
        if (vitals == null)
            return true;

        return vitals.HealthPercent >= 100f && vitals.ManaPercent >= 100f;
    }
}

/// <summary>
/// The result of a rest operation.
/// </summary>
public enum RestResult
{
    /// <summary>Rest completed - fully recovered.</summary>
    FullyRecovered,
    /// <summary>Rest was manually stopped.</summary>
    Manual,
    /// <summary>Rest timed out.</summary>
    Timeout,
    /// <summary>Rest was interrupted by combat.</summary>
    EnteredCombat,
    /// <summary>Rest completed but not fully recovered.</summary>
    PartiallyRecovered
}
