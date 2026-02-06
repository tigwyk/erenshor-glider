using System;
using ErenshorGlider.GameState;
using ErenshorGlider.Input;

#if !USE_REAL_GAME_TYPES
using ErenshorGlider.GameStubs;
#endif

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
        if (vitals.Value.HealthPercent < MinHealthPercent)
            return true;

        if (vitals.Value.ManaPercent < MinManaPercent)
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
            UseConsumable(FoodItem!);
        }

        // Use drink if available
        if (!string.IsNullOrEmpty(DrinkItem))
        {
            UseConsumable(DrinkItem!);
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
            bool healthRecovered = vitals.Value.HealthPercent >= TargetHealthPercent;
            bool manaRecovered = vitals.Value.ManaPercent >= TargetManaPercent;

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
    /// Searches inventory for the item and activates it via keybind.
    /// </summary>
    /// <param name="itemName">The name of the consumable item to use.</param>
    /// <returns>True if the item was found and used, false otherwise.</returns>
    private bool UseConsumable(string itemName)
    {
#if !USE_REAL_GAME_TYPES
        // Stub implementation - no-op when using stubs
        return false;
#else
        try
        {
            if (GameData.PlayerInv == null)
                return false;

            // Search inventory for the item
            int? hotkeySlot = FindItemHotkeySlot(itemName);
            if (hotkeySlot.HasValue)
            {
                // Use the item via its hotkey slot
                _inputController.UseAbilitySlot(hotkeySlot.Value);
                return true;
            }

            return false;
        }
        catch
        {
            // Gracefully handle errors during scene transitions
            return false;
        }
#endif
    }

#if USE_REAL_GAME_TYPES
    /// <summary>
    /// Finds the hotkey slot (1-9) assigned to an item by name.
    /// In a full implementation, this would track the player's action bar setup.
    /// For now, uses a simple heuristic: common food/drink items in early slots.
    /// </summary>
    /// <param name="itemName">The name of the item to find.</param>
    /// <returns>The hotkey slot number (1-9), or null if not found.</returns>
    private static int? FindItemHotkeySlot(string itemName)
    {
        if (string.IsNullOrEmpty(itemName))
            return null;

        // Search all inventory slots for the item
        if (GameData.PlayerInv?.ALLSLOTS == null)
            return null;

        foreach (var slot in GameData.PlayerInv.ALLSLOTS)
        {
            if (slot?.Item?.ItemName?.Equals(itemName, StringComparison.OrdinalIgnoreCase) == true)
            {
                // TODO: In a full implementation, we would track which slot
                // the item is bound to. For now, we'll use a heuristic:
                // - Food items typically go in slot 1-3
                // - Drink items typically go in slot 4-6
                // This could be made configurable via the combat profile

                // Check if it's a food item (contains "bread", "meat", "food", etc.)
                if (IsFoodItem(itemName))
                    return 1;

                // Check if it's a drink item (contains "water", "potion", "drink", etc.)
                if (IsDrinkItem(itemName))
                    return 2;

                // Default to slot 3 for other consumables
                return 3;
            }
        }

        return null;
    }

    /// <summary>
    /// Checks if an item name suggests it's a food item.
    /// </summary>
    private static bool IsFoodItem(string itemName)
    {
        string lower = itemName.ToLower();
        return lower.Contains("bread") ||
               lower.Contains("meat") ||
               lower.Contains("fish") ||
               lower.Contains("food") ||
               lower.Contains("apple") ||
               lower.Contains("cheese");
    }

    /// <summary>
    /// Checks if an item name suggests it's a drink item.
    /// </summary>
    private static bool IsDrinkItem(string itemName)
    {
        string lower = itemName.ToLower();
        return lower.Contains("water") ||
               lower.Contains("potion") ||
               lower.Contains("drink") ||
               lower.Contains("juice") ||
               lower.Contains("milk") ||
               lower.Contains("ale");
    }
#endif

    /// <summary>
    /// Checks if the player is at full health and mana.
    /// </summary>
    public bool IsFullyRecovered()
    {
        var vitals = _positionTracker.CurrentVitals;
        if (vitals == null)
            return true;

        return vitals.Value.HealthPercent >= 100f && vitals.Value.ManaPercent >= 100f;
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
