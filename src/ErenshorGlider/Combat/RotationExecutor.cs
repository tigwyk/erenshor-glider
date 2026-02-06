using System;
using System.Collections.Generic;
using ErenshorGlider.GameState;
using ErenshorGlider.Input;
using ErenshorGlider.Navigation;

namespace ErenshorGlider.Combat;

/// <summary>
/// Executes combat rotations based on a loaded combat profile.
/// </summary>
public class RotationExecutor
{
    private readonly InputController _inputController;
    private readonly PositionTracker _positionTracker;
    private CombatProfile? _currentProfile;
    private readonly Dictionary<string, DateTime> _abilityLastUsed = new();
    private DateTime _lastGcdTrigger = DateTime.MinValue;

    /// <summary>
    /// Gets the currently loaded combat profile.
    /// </summary>
    public CombatProfile? CurrentProfile => _currentProfile;

    /// <summary>
    /// Gets whether an ability is currently casting (GCD active).
    /// </summary>
    public bool IsCasting => (DateTime.UtcNow - _lastGcdTrigger).TotalSeconds < (_currentProfile?.GlobalCooldown ?? 1.5f);

    /// <summary>
    /// Gets or sets whether to enable auto-attack between abilities.
    /// </summary>
    public bool UseAutoAttack { get; set; } = true;

    /// <summary>
    /// Event raised when an ability is executed.
    /// </summary>
    public event Action<Ability>? OnAbilityExecuted;

    /// <summary>
    /// Event raised when no ability is ready.
    /// </summary>
    public event Action? OnNoAbilityReady;

    /// <summary>
    /// Event raised when a profile is loaded.
    /// </summary>
    public event Action<CombatProfile>? OnProfileLoaded;

    /// <summary>
    /// Creates a new RotationExecutor.
    /// </summary>
    public RotationExecutor(InputController inputController, PositionTracker positionTracker)
    {
        _inputController = inputController ?? throw new ArgumentNullException(nameof(inputController));
        _positionTracker = positionTracker ?? throw new ArgumentNullException(nameof(positionTracker));
    }

    /// <summary>
    /// Loads a combat profile for execution.
    /// </summary>
    public bool LoadProfile(CombatProfile profile)
    {
        if (profile == null)
            return false;

        var errors = profile.Validate();
        if (errors.Count > 0)
        {
            // Log validation errors
            Console.WriteLine($"Profile validation errors: {string.Join(", ", errors)}");
            return false;
        }

        _currentProfile = profile;
        _abilityLastUsed.Clear();
        _lastGcdTrigger = DateTime.MinValue;

        OnProfileLoaded?.Invoke(profile);
        return true;
    }

    /// <summary>
    /// Loads a combat profile by name from the profiles directory.
    /// </summary>
    public bool LoadProfile(string profileName)
    {
        var profile = CombatProfileFileManager.LoadProfile(profileName);
        if (profile == null)
            return false;

        return LoadProfile(profile);
    }

    /// <summary>
    /// Executes the next available ability in the rotation.
    /// </summary>
    /// <returns>True if an ability was executed, false otherwise.</returns>
    public bool ExecuteNextAbility()
    {
        if (_currentProfile == null)
            return false;

        // Check if GCD is active
        if (IsCasting)
            return false;

        // Get current game state
        var combatState = _positionTracker.CurrentCombatState;
        var vitals = _positionTracker.CurrentVitals;
        var playerBuffs = _positionTracker.CurrentPlayerBuffs;
        var targetBuffs = _positionTracker.CurrentTargetBuffs;
        var targetInfo = _positionTracker.CurrentTargetInfo;

        // Create default states if not available
        combatState ??= new CombatState(false, false, true);
        vitals ??= PlayerVitals.Empty;
        playerBuffs ??= BuffState.Empty;

        // Unwrap nullable values
        var unwrappedCombatState = combatState.Value;
        var unwrappedVitals = vitals.Value;
        var unwrappedPlayerBuffs = playerBuffs.Value;

        // Check if player can act
        if (!unwrappedCombatState.CanAct)
            return false;

        // Iterate through rotation entries by priority
        foreach (var entry in _currentProfile.SortedRotation)
        {
            if (!entry.Enabled)
                continue;

            // Get the ability for this entry
            var ability = _currentProfile.GetAbility(entry.AbilityId);
            if (ability == null)
                continue;

            // Check if ability is on cooldown
            if (IsAbilityOnCooldown(ability))
                continue;

            // Check if ability has enough mana
            if (ability.ManaCost > 0 && unwrappedVitals.CurrentMana < ability.ManaCost)
                continue;

            // Check if target is required and present
            if (ability.RequiresTarget && (!targetInfo.HasValue || !targetInfo.Value.HasTarget))
                continue;

            // Check if target is in range
            if (ability.Range > 0 && targetInfo.HasValue && targetInfo.Value.HasTarget)
            {
                float targetDistance = GetDistanceToTarget(targetInfo.Value.Position);
                if (targetDistance > ability.Range)
                    continue;
            }

            // Evaluate conditions
            if (!entry.CanExecute(unwrappedCombatState, unwrappedVitals, unwrappedPlayerBuffs, targetBuffs, targetInfo))
                continue;

            // Execute the ability
            return ExecuteAbility(ability);
        }

        // No ability was ready
        OnNoAbilityReady?.Invoke();

        // Use auto-attack if enabled and no ability ready
        if (_currentProfile.UseAutoAttack && UseAutoAttack && targetInfo.HasValue && targetInfo.Value.HasTarget)
        {
            // TODO: Implement auto-attack
        }

        return false;
    }

    /// <summary>
    /// Executes a specific ability.
    /// </summary>
    private bool ExecuteAbility(Ability ability)
    {
        // Try to use by keybind first
        if (!string.IsNullOrEmpty(ability.Keybind))
        {
            KeyCode key = ParseKeybind(ability.Keybind);
            _inputController.UseAbility(key);
        }
        else
        {
            // Use ability ID (direct activation)
            _inputController.UseAbilityById(ability.Id);
        }

        // Update cooldown tracking
        _abilityLastUsed[ability.Id] = DateTime.UtcNow;

        // Update GCD if ability triggers it
        if (ability.TriggersGcd)
        {
            _lastGcdTrigger = DateTime.UtcNow;
        }

        OnAbilityExecuted?.Invoke(ability);
        return true;
    }

    /// <summary>
    /// Checks if an ability is on cooldown.
    /// </summary>
    private bool IsAbilityOnCooldown(Ability ability)
    {
        if (!_abilityLastUsed.TryGetValue(ability.Id, out var lastUsed))
            return false;

        var timeSinceUse = (DateTime.UtcNow - lastUsed).TotalSeconds;
        return timeSinceUse < ability.Cooldown;
    }

    /// <summary>
    /// Gets the remaining cooldown for an ability.
    /// </summary>
    public float GetRemainingCooldown(string abilityId)
    {
        var ability = _currentProfile?.GetAbility(abilityId);
        if (ability == null)
            return 0f;

        if (!_abilityLastUsed.TryGetValue(abilityId, out var lastUsed))
            return 0f;

        var timeSinceUse = (DateTime.UtcNow - lastUsed).TotalSeconds;
        return Math.Max(0f, ability.Cooldown - (float)timeSinceUse);
    }

    /// <summary>
    /// Calculates distance to target.
    /// </summary>
    private float GetDistanceToTarget(in PlayerPosition targetPosition)
    {
        var currentPos = _positionTracker.CurrentPosition;
        if (currentPos == null)
            return float.MaxValue;

        return Navigation.Navigation.CalculateDistance(currentPos.Value, targetPosition);
    }

    /// <summary>
    /// Parses a keybind string to a KeyCode enum.
    /// </summary>
    private KeyCode ParseKeybind(string keybind)
    {
        if (string.IsNullOrEmpty(keybind))
            return KeyCode.Alpha1;

        // Handle number keys (1-9)
        if (keybind.Length == 1 && char.IsDigit(keybind[0]))
        {
            return keybind[0] switch
            {
                '1' => KeyCode.Alpha1,
                '2' => KeyCode.Alpha2,
                '3' => KeyCode.Alpha3,
                '4' => KeyCode.Alpha4,
                '5' => KeyCode.Alpha5,
                '6' => KeyCode.Alpha6,
                '7' => KeyCode.Alpha7,
                '8' => KeyCode.Alpha8,
                '9' => KeyCode.Alpha9,
                '0' => KeyCode.Alpha0,
                _ => KeyCode.Alpha1
            };
        }

        // Handle F keys
        if (keybind.StartsWith("F", StringComparison.OrdinalIgnoreCase) &&
            keybind.Length > 1 &&
            int.TryParse(keybind.Substring(1), out int fNum) &&
            fNum >= 1 && fNum <= 12)
        {
            return fNum switch
            {
                1 => KeyCode.F1,
                2 => KeyCode.F2,
                3 => KeyCode.F3,
                4 => KeyCode.F4,
                5 => KeyCode.F5,
                6 => KeyCode.F6,
                7 => KeyCode.F7,
                8 => KeyCode.F8,
                9 => KeyCode.F9,
                10 => KeyCode.F10,
                11 => KeyCode.F11,
                12 => KeyCode.F12,
                _ => KeyCode.Alpha1
            };
        }

        return KeyCode.Alpha1;
    }

    /// <summary>
    /// Clears all cooldown tracking.
    /// </summary>
    public void ClearCooldowns()
    {
        _abilityLastUsed.Clear();
        _lastGcdTrigger = DateTime.MinValue;
    }
}
