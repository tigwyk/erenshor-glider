using System;

namespace ErenshorGlider.GUI;

/// <summary>
/// Interface for providing bot status information.
/// </summary>
public interface IBotStatusProvider
{
    /// <summary>
    /// Gets the current bot status snapshot.
    /// </summary>
    BotStatus GetStatus();
}

/// <summary>
/// Snapshot of bot status information.
/// </summary>
public readonly struct BotStatus
{
    /// <summary>
    /// Gets the current bot state.
    /// </summary>
    public string State { get; }

    /// <summary>
    /// Gets the current target name.
    /// </summary>
    public string TargetName { get; }

    /// <summary>
    /// Gets whether a target is selected.
    /// </summary>
    public bool HasTarget { get; }

    /// <summary>
    /// Gets the player's X coordinate.
    /// </summary>
    public float PositionX { get; }

    /// <summary>
    /// Gets the player's Y coordinate.
    /// </summary>
    public float PositionY { get; }

    /// <summary>
    /// Gets the player's Z coordinate.
    /// </summary>
    public float PositionZ { get; }

    /// <summary>
    /// Gets the player's current health.
    /// </summary>
    public int CurrentHealth { get; }

    /// <summary>
    /// Gets the player's maximum health.
    /// </summary>
    public int MaxHealth { get; }

    /// <summary>
    /// Gets the player's current mana.
    /// </summary>
    public int CurrentMana { get; }

    /// <summary>
    /// Gets the player's maximum mana.
    /// </summary>
    public int MaxMana { get; }

    /// <summary>
    /// Gets the health percentage (0-100).
    /// </summary>
    public float HealthPercent => MaxHealth > 0 ? (float)CurrentHealth / MaxHealth * 100f : 0f;

    /// <summary>
    /// Gets the mana percentage (0-100).
    /// </summary>
    public float ManaPercent => MaxMana > 0 ? (float)CurrentMana / MaxMana * 100f : 0f;

    /// <summary>
    /// Gets the formatted position string.
    /// </summary>
    public string FormattedPosition => $"X: {PositionX:F1}, Y: {PositionY:F1}, Z: {PositionZ:F1}";

    public BotStatus(
        string state,
        string targetName,
        bool hasTarget,
        float positionX, float positionY, float positionZ,
        int currentHealth, int maxHealth,
        int currentMana, int maxMana)
    {
        State = state;
        TargetName = targetName;
        HasTarget = hasTarget;
        PositionX = positionX;
        PositionY = positionY;
        PositionZ = positionZ;
        CurrentHealth = currentHealth;
        MaxHealth = maxHealth;
        CurrentMana = currentMana;
        MaxMana = maxMana;
    }

    /// <summary>
    /// Creates a default/empty bot status.
    /// </summary>
    public static BotStatus Empty => new BotStatus(
        "Idle", string.Empty, false,
        0f, 0f, 0f,
        100, 100,
        100, 100
    );
}
