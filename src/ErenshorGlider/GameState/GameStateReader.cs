using System;
using ErenshorGlider.GameStubs;
using UnityEngine;

namespace ErenshorGlider.GameState;

/// <summary>
/// Reads game state from Erenshor via the GameData singleton.
/// Provides a clean API for accessing player position, vitals, and other game state.
/// </summary>
public class GameStateReader
{
    private PlayerPosition _lastPosition;
    private DateTime _lastPositionUpdate;
    private readonly object _positionLock = new();

    private PlayerVitals _lastVitals;
    private DateTime _lastVitalsUpdate;
    private readonly object _vitalsLock = new();

    private CombatState _lastCombatState;
    private DateTime _lastCombatStateUpdate;
    private readonly object _combatStateLock = new();

    /// <summary>
    /// Event raised when player position changes.
    /// </summary>
    public event Action<PlayerPosition>? OnPositionChanged;

    /// <summary>
    /// Event raised when player vitals change.
    /// </summary>
    public event Action<PlayerVitals>? OnVitalsChanged;

    /// <summary>
    /// Event raised when combat state changes.
    /// </summary>
    public event Action<CombatState>? OnCombatStateChanged;

    /// <summary>
    /// Gets whether the game state is currently available (player is loaded).
    /// </summary>
    public bool IsAvailable => GetPlayerControlTransform() != null;

    /// <summary>
    /// Gets the player's current position in the game world.
    /// Returns null if player is not loaded.
    /// </summary>
    public PlayerPosition? GetPlayerPosition()
    {
        var transform = GetPlayerControlTransform();
        if (transform == null)
            return null;

        var position = new PlayerPosition(transform.position);

        lock (_positionLock)
        {
            _lastPosition = position;
            _lastPositionUpdate = DateTime.UtcNow;
        }

        return position;
    }

    /// <summary>
    /// Gets the player's cached position (from last update).
    /// Useful for checking position without polling the game.
    /// </summary>
    public PlayerPosition? GetCachedPosition()
    {
        lock (_positionLock)
        {
            if (_lastPositionUpdate == default)
                return null;

            return _lastPosition;
        }
    }

    /// <summary>
    /// Gets the time since the position was last updated.
    /// </summary>
    public TimeSpan TimeSinceLastUpdate
    {
        get
        {
            lock (_positionLock)
            {
                if (_lastPositionUpdate == default)
                    return TimeSpan.MaxValue;

                return DateTime.UtcNow - _lastPositionUpdate;
            }
        }
    }

    /// <summary>
    /// Updates the position cache and raises events if position changed.
    /// This should be called at the desired update rate (e.g., 10Hz or higher).
    /// </summary>
    /// <returns>True if position was updated successfully, false if game state unavailable.</returns>
    public bool UpdatePosition()
    {
        var newPosition = GetPlayerPosition();
        if (newPosition == null)
            return false;

        // Check if position actually changed (with small epsilon for floating point comparison)
        PlayerPosition? oldPosition;
        lock (_positionLock)
        {
            oldPosition = _lastPositionUpdate != default ? _lastPosition : (PlayerPosition?)null;
        }

        if (oldPosition == null || HasPositionChanged(oldPosition.Value, newPosition.Value))
        {
            OnPositionChanged?.Invoke(newPosition.Value);
        }

        return true;
    }

    private static bool HasPositionChanged(PlayerPosition old, PlayerPosition newPos)
    {
        const float epsilon = 0.001f;
        return Math.Abs(old.X - newPos.X) > epsilon ||
               Math.Abs(old.Y - newPos.Y) > epsilon ||
               Math.Abs(old.Z - newPos.Z) > epsilon;
    }

    #region Vitals Reading

    /// <summary>
    /// Gets the player's current vitals (health, mana, level, XP).
    /// Returns null if player is not loaded.
    /// </summary>
    public PlayerVitals? GetPlayerVitals()
    {
        var stats = GetPlayerStats();
        if (stats == null)
            return null;

        var vitals = new PlayerVitals(
            currentHealth: stats.CurrentHP,
            maxHealth: stats.MaxHP,
            currentMana: stats.CurrentMP,
            maxMana: stats.MaxMP,
            level: stats.Level,
            currentXP: stats.CurrentXP,
            xpToLevel: stats.XPToLevel
        );

        lock (_vitalsLock)
        {
            _lastVitals = vitals;
            _lastVitalsUpdate = DateTime.UtcNow;
        }

        return vitals;
    }

    /// <summary>
    /// Gets the player's cached vitals (from last update).
    /// Useful for checking vitals without polling the game.
    /// </summary>
    public PlayerVitals? GetCachedVitals()
    {
        lock (_vitalsLock)
        {
            if (_lastVitalsUpdate == default)
                return null;

            return _lastVitals;
        }
    }

    /// <summary>
    /// Gets the time since vitals were last updated.
    /// </summary>
    public TimeSpan TimeSinceLastVitalsUpdate
    {
        get
        {
            lock (_vitalsLock)
            {
                if (_lastVitalsUpdate == default)
                    return TimeSpan.MaxValue;

                return DateTime.UtcNow - _lastVitalsUpdate;
            }
        }
    }

    /// <summary>
    /// Updates the vitals cache and raises events if vitals changed.
    /// This should be called at the desired update rate (e.g., 10Hz or higher).
    /// </summary>
    /// <returns>True if vitals were updated successfully, false if game state unavailable.</returns>
    public bool UpdateVitals()
    {
        var newVitals = GetPlayerVitals();
        if (newVitals == null)
            return false;

        // Check if vitals actually changed
        PlayerVitals? oldVitals;
        lock (_vitalsLock)
        {
            oldVitals = _lastVitalsUpdate != default ? _lastVitals : (PlayerVitals?)null;
        }

        if (oldVitals == null || HasVitalsChanged(oldVitals.Value, newVitals.Value))
        {
            OnVitalsChanged?.Invoke(newVitals.Value);
        }

        return true;
    }

    private static bool HasVitalsChanged(PlayerVitals old, PlayerVitals newVitals)
    {
        const float epsilon = 0.01f;
        return Math.Abs(old.CurrentHealth - newVitals.CurrentHealth) > epsilon ||
               Math.Abs(old.MaxHealth - newVitals.MaxHealth) > epsilon ||
               Math.Abs(old.CurrentMana - newVitals.CurrentMana) > epsilon ||
               Math.Abs(old.MaxMana - newVitals.MaxMana) > epsilon ||
               old.Level != newVitals.Level ||
               Math.Abs(old.CurrentXP - newVitals.CurrentXP) > epsilon ||
               Math.Abs(old.XPToLevel - newVitals.XPToLevel) > epsilon;
    }

    #endregion

    #region Combat State Reading

    /// <summary>
    /// Gets the player's current combat state (in combat, casting, alive/dead).
    /// Returns null if player is not loaded.
    /// </summary>
    public CombatState? GetCombatState()
    {
        try
        {
            var playerControl = GameData.PlayerControl;
            if (playerControl == null)
                return null;

            var character = playerControl.Myself;
            if (character == null)
                return null;

            // Read combat state from PlayerCombat component
            var playerCombat = GameData.PlayerCombat;
            bool inCombat = playerCombat?.InCombat ?? false;

            // Read casting state from CastSpell component
            var playerSpells = playerControl.PlayerSpells;
            bool isCasting = playerSpells?.Casting ?? false;

            // Read alive/dead state from Character
            bool isAlive = !character.Dead;

            var combatState = new CombatState(inCombat, isCasting, isAlive);

            lock (_combatStateLock)
            {
                _lastCombatState = combatState;
                _lastCombatStateUpdate = DateTime.UtcNow;
            }

            return combatState;
        }
        catch (Exception)
        {
            // Game state not available
            return null;
        }
    }

    /// <summary>
    /// Gets the player's cached combat state (from last update).
    /// Useful for checking combat state without polling the game.
    /// </summary>
    public CombatState? GetCachedCombatState()
    {
        lock (_combatStateLock)
        {
            if (_lastCombatStateUpdate == default)
                return null;

            return _lastCombatState;
        }
    }

    /// <summary>
    /// Gets the time since combat state was last updated.
    /// </summary>
    public TimeSpan TimeSinceLastCombatStateUpdate
    {
        get
        {
            lock (_combatStateLock)
            {
                if (_lastCombatStateUpdate == default)
                    return TimeSpan.MaxValue;

                return DateTime.UtcNow - _lastCombatStateUpdate;
            }
        }
    }

    /// <summary>
    /// Updates the combat state cache and raises events if state changed.
    /// This should be called at the desired update rate (e.g., 10Hz or higher).
    /// </summary>
    /// <returns>True if combat state was updated successfully, false if game state unavailable.</returns>
    public bool UpdateCombatState()
    {
        var newCombatState = GetCombatState();
        if (newCombatState == null)
            return false;

        // Check if combat state actually changed
        CombatState? oldCombatState;
        lock (_combatStateLock)
        {
            oldCombatState = _lastCombatStateUpdate != default ? _lastCombatState : (CombatState?)null;
        }

        if (oldCombatState == null || HasCombatStateChanged(oldCombatState.Value, newCombatState.Value))
        {
            OnCombatStateChanged?.Invoke(newCombatState.Value);
        }

        return true;
    }

    private static bool HasCombatStateChanged(CombatState old, CombatState newState)
    {
        return old.InCombat != newState.InCombat ||
               old.IsCasting != newState.IsCasting ||
               old.IsAlive != newState.IsAlive;
    }

    #endregion

    #region Private Helpers

    /// <summary>
    /// Attempts to get the PlayerControl transform from the game.
    /// Returns null if not available.
    /// </summary>
    private static Transform? GetPlayerControlTransform()
    {
        try
        {
            // GameData.PlayerControl is the player controller component
            // Its transform.position gives us the world position
            var playerControl = GameData.PlayerControl;
            return playerControl?.transform;
        }
        catch (Exception)
        {
            // Game state not available (e.g., not in game, loading screen)
            return null;
        }
    }

    /// <summary>
    /// Attempts to get the player's CharacterStats from the game.
    /// Returns null if not available.
    /// </summary>
    private static CharacterStats? GetPlayerStats()
    {
        try
        {
            // GameData.PlayerControl.Myself is the player's Character component
            // Character.MyStats gives us the CharacterStats with HP, MP, Level, XP
            var playerControl = GameData.PlayerControl;
            return playerControl?.Myself?.MyStats;
        }
        catch (Exception)
        {
            // Game state not available (e.g., not in game, loading screen)
            return null;
        }
    }

    #endregion
}
