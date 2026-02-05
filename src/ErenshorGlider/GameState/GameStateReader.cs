using System;
using ErenshorGlider.GameStubs;
using UnityEngine;

namespace ErenshorGlider.GameState;

/// <summary>
/// Reads game state from Erenshor via the GameData singleton.
/// Provides a clean API for accessing player position and other game state.
/// </summary>
public class GameStateReader
{
    private PlayerPosition _lastPosition;
    private DateTime _lastPositionUpdate;
    private readonly object _positionLock = new();

    /// <summary>
    /// Event raised when player position changes.
    /// </summary>
    public event Action<PlayerPosition>? OnPositionChanged;

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
}
