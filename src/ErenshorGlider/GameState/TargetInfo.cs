using UnityEngine;

namespace ErenshorGlider.GameState;

/// <summary>
/// Represents information about the player's current target.
/// </summary>
public readonly struct TargetInfo
{
    /// <summary>
    /// Whether the player has a valid target.
    /// </summary>
    public bool HasTarget { get; }

    /// <summary>
    /// The target's display name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The target's level.
    /// </summary>
    public int Level { get; }

    /// <summary>
    /// The target's current health.
    /// </summary>
    public float CurrentHealth { get; }

    /// <summary>
    /// The target's maximum health.
    /// </summary>
    public float MaxHealth { get; }

    /// <summary>
    /// The target's health percentage (0-100).
    /// </summary>
    public float HealthPercent => MaxHealth > 0 ? (CurrentHealth / MaxHealth) * 100f : 0f;

    /// <summary>
    /// The target's position in the game world.
    /// </summary>
    public PlayerPosition Position { get; }

    /// <summary>
    /// The target's hostility type.
    /// </summary>
    public TargetHostility Hostility { get; }

    /// <summary>
    /// Whether the target is dead.
    /// </summary>
    public bool IsDead { get; }

    /// <summary>
    /// Creates a TargetInfo representing no target.
    /// </summary>
    public static TargetInfo NoTarget => new(
        hasTarget: false,
        name: string.Empty,
        level: 0,
        currentHealth: 0,
        maxHealth: 0,
        position: new PlayerPosition(0, 0, 0),
        hostility: TargetHostility.Neutral,
        isDead: false
    );

    /// <summary>
    /// Creates a new TargetInfo instance.
    /// </summary>
    public TargetInfo(
        bool hasTarget,
        string name,
        int level,
        float currentHealth,
        float maxHealth,
        PlayerPosition position,
        TargetHostility hostility,
        bool isDead)
    {
        HasTarget = hasTarget;
        Name = name ?? string.Empty;
        Level = level;
        CurrentHealth = currentHealth;
        MaxHealth = maxHealth;
        Position = position;
        Hostility = hostility;
        IsDead = isDead;
    }

    /// <summary>
    /// Returns true if the target is hostile.
    /// </summary>
    public bool IsHostile => Hostility == TargetHostility.Hostile;

    /// <summary>
    /// Returns true if the target is friendly.
    /// </summary>
    public bool IsFriendly => Hostility == TargetHostility.Friendly;

    /// <summary>
    /// Returns true if the target is neutral.
    /// </summary>
    public bool IsNeutral => Hostility == TargetHostility.Neutral;

    /// <summary>
    /// Returns true if health is below the given percentage threshold.
    /// </summary>
    public bool IsHealthBelowPercent(float percent) => HealthPercent < percent;

    /// <summary>
    /// Returns true if health is above the given percentage threshold.
    /// </summary>
    public bool IsHealthAbovePercent(float percent) => HealthPercent > percent;

    public override string ToString()
    {
        if (!HasTarget)
            return "No Target";

        return $"{Name} (Lvl {Level}) - {HealthPercent:F1}% HP, {Hostility}";
    }
}

/// <summary>
/// Represents the hostility level of a target.
/// </summary>
public enum TargetHostility
{
    /// <summary>
    /// Target is neutral (will not attack unless provoked).
    /// </summary>
    Neutral,

    /// <summary>
    /// Target is hostile (will attack on sight).
    /// </summary>
    Hostile,

    /// <summary>
    /// Target is friendly (cannot be attacked).
    /// </summary>
    Friendly
}
