using UnityEngine;

namespace ErenshorGlider.GameState;

/// <summary>
/// Represents information about a nearby entity (mob, NPC, node, corpse).
/// </summary>
public readonly struct EntityInfo
{
    /// <summary>
    /// The entity's display name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The type of entity.
    /// </summary>
    public EntityType Type { get; }

    /// <summary>
    /// The Unity instance ID of the entity.
    /// Used for direct entity targeting via the game API.
    /// </summary>
    public int InstanceId { get; }

    /// <summary>
    /// The entity's position in the game world.
    /// </summary>
    public PlayerPosition Position { get; }

    /// <summary>
    /// The entity's level (0 if not applicable, e.g., for nodes).
    /// </summary>
    public int Level { get; }

    /// <summary>
    /// The entity's hostility toward the player.
    /// </summary>
    public TargetHostility Hostility { get; }

    /// <summary>
    /// The entity's current health (0 if not applicable).
    /// </summary>
    public float CurrentHealth { get; }

    /// <summary>
    /// The entity's maximum health (0 if not applicable).
    /// </summary>
    public float MaxHealth { get; }

    /// <summary>
    /// The entity's health percentage (0-100). Returns 0 if MaxHealth is 0.
    /// </summary>
    public float HealthPercent => MaxHealth > 0 ? (CurrentHealth / MaxHealth) * 100f : 0f;

    /// <summary>
    /// Whether the entity is dead.
    /// </summary>
    public bool IsDead { get; }

    /// <summary>
    /// Distance from the player when this entity was detected.
    /// </summary>
    public float Distance { get; }

    /// <summary>
    /// Creates a new EntityInfo instance.
    /// </summary>
    public EntityInfo(
        string name,
        EntityType type,
        PlayerPosition position,
        int level,
        TargetHostility hostility,
        float currentHealth,
        float maxHealth,
        bool isDead,
        float distance,
        int instanceId = 0)
    {
        Name = name ?? string.Empty;
        Type = type;
        Position = position;
        Level = level;
        Hostility = hostility;
        CurrentHealth = currentHealth;
        MaxHealth = maxHealth;
        IsDead = isDead;
        Distance = distance;
        InstanceId = instanceId;
    }

    /// <summary>
    /// Returns true if the entity is hostile.
    /// </summary>
    public bool IsHostile => Hostility == TargetHostility.Hostile;

    /// <summary>
    /// Returns true if the entity is friendly.
    /// </summary>
    public bool IsFriendly => Hostility == TargetHostility.Friendly;

    /// <summary>
    /// Returns true if the entity is neutral.
    /// </summary>
    public bool IsNeutral => Hostility == TargetHostility.Neutral;

    /// <summary>
    /// Returns true if this entity is a mob (hostile or neutral creature).
    /// </summary>
    public bool IsMob => Type == EntityType.Mob;

    /// <summary>
    /// Returns true if this entity is an NPC.
    /// </summary>
    public bool IsNPC => Type == EntityType.NPC;

    /// <summary>
    /// Returns true if this entity is a resource node.
    /// </summary>
    public bool IsNode => Type == EntityType.Node;

    /// <summary>
    /// Returns true if this entity is a lootable corpse.
    /// </summary>
    public bool IsCorpse => Type == EntityType.Corpse;

    /// <summary>
    /// Returns true if this entity can be attacked (hostile mob that is alive).
    /// </summary>
    public bool CanBeAttacked => IsMob && IsHostile && !IsDead;

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
        return Type switch
        {
            EntityType.Mob => $"{Name} (Lvl {Level}) - {HealthPercent:F1}% HP, {Hostility}, {Distance:F1}m",
            EntityType.NPC => $"{Name} (NPC) - {Distance:F1}m",
            EntityType.Node => $"{Name} (Node) - {Distance:F1}m",
            EntityType.Corpse => $"{Name} (Corpse) - {Distance:F1}m",
            _ => $"{Name} - {Distance:F1}m"
        };
    }
}

/// <summary>
/// Represents the type of a nearby entity.
/// </summary>
public enum EntityType
{
    /// <summary>
    /// A mob/creature (can be hostile, neutral, or friendly).
    /// </summary>
    Mob,

    /// <summary>
    /// A non-player character (typically friendly, offers services/quests).
    /// </summary>
    NPC,

    /// <summary>
    /// A resource gathering node (ore, herbs, etc.).
    /// </summary>
    Node,

    /// <summary>
    /// A lootable corpse.
    /// </summary>
    Corpse
}
