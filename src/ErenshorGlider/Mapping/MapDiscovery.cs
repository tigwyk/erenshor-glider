using System;

namespace ErenshorGlider.Mapping;

/// <summary>
/// Represents a discovered resource node on the map.
/// </summary>
public readonly struct ResourceNodeDiscovery
{
    /// <summary>
    /// Unique identifier for this discovery.
    /// </summary>
    public int Id { get; }

    /// <summary>
    /// The name/type of the resource node (e.g., "Iron Ore", "Herb").
    /// </summary>
    public string NodeName { get; }

    /// <summary>
    /// The X coordinate where this node was found.
    /// </summary>
    public float X { get; }

    /// <summary>
    /// The Y coordinate (height) where this node was found.
    /// </summary>
    public float Y { get; }

    /// <summary>
    /// The Z coordinate where this node was found.
    /// </summary>
    public float Z { get; }

    /// <summary>
    /// The zone name where this node was found.
    /// </summary>
    public string Zone { get; }

    /// <summary>
    /// The timestamp when this node was discovered.
    /// </summary>
    public DateTime DiscoveredAt { get; }

    /// <summary>
    /// The timestamp when this node was last seen.
    /// </summary>
    public DateTime LastSeenAt { get; }

    /// <summary>
    /// The number of times this node has been seen.
    /// </summary>
    public int TimesSeen { get; }

    /// <summary>
    /// The required skill to gather this node (e.g., "Mining", "Herbalism").
    /// </summary>
    public string? RequiredSkill { get; }

    /// <summary>
    /// Creates a new ResourceNodeDiscovery.
    /// </summary>
    public ResourceNodeDiscovery(
        int id,
        string nodeName,
        float x, float y, float z,
        string zone,
        DateTime discoveredAt,
        DateTime lastSeenAt,
        int timesSeen,
        string? requiredSkill = null)
    {
        Id = id;
        NodeName = nodeName;
        X = x;
        Y = y;
        Z = z;
        Zone = zone;
        DiscoveredAt = discoveredAt;
        LastSeenAt = lastSeenAt;
        TimesSeen = timesSeen;
        RequiredSkill = requiredSkill;
    }

    /// <summary>
    /// Returns a string representation of this node discovery.
    /// </summary>
    public override string ToString() =>
        $"{NodeName} at ({X:F1}, {Y:F1}, {Z:F1}) in {Zone} - seen {TimesSeen}x";
}

/// <summary>
/// Represents a discovered NPC on the map.
/// </summary>
public readonly struct NpcDiscovery
{
    /// <summary>
    /// Unique identifier for this discovery.
    /// </summary>
    public int Id { get; }

    /// <summary>
    /// The name of the NPC.
    /// </summary>
    public string NpcName { get; }

    /// <summary>
    /// The X coordinate where this NPC was found.
    /// </summary>
    public float X { get; }

    /// <summary>
    /// The Y coordinate (height) where this NPC was found.
    /// </summary>
    public float Y { get; }

    /// <summary>
    /// The Z coordinate where this NPC was found.
    /// </summary>
    public float Z { get; }

    /// <summary>
    /// The zone name where this NPC was found.
    /// </summary>
    public string Zone { get; }

    /// <summary>
    /// Whether this NPC is a vendor.
    /// </summary>
    public bool IsVendor { get; }

    /// <summary>
    /// Whether this NPC offers quests.
    /// </summary>
    public bool HasQuests { get; }

    /// <summary>
    /// The timestamp when this NPC was discovered.
    /// </summary>
    public DateTime DiscoveredAt { get; }

    /// <summary>
    /// The timestamp when this NPC was last seen.
    /// </summary>
    public DateTime LastSeenAt { get; }

    /// <summary>
    /// Creates a new NpcDiscovery.
    /// </summary>
    public NpcDiscovery(
        int id,
        string npcName,
        float x, float y, float z,
        string zone,
        bool isVendor,
        bool hasQuests,
        DateTime discoveredAt,
        DateTime lastSeenAt)
    {
        Id = id;
        NpcName = npcName;
        X = x;
        Y = y;
        Z = z;
        Zone = zone;
        IsVendor = isVendor;
        HasQuests = hasQuests;
        DiscoveredAt = discoveredAt;
        LastSeenAt = lastSeenAt;
    }

    /// <summary>
    /// Returns a string representation of this NPC discovery.
    /// </summary>
    public override string ToString() =>
        $"{NpcName} at ({X:F1}, {Y:F1}, {Z:F1}) in {Zone}" +
        (IsVendor ? " [Vendor]" : "") +
        (HasQuests ? " [Quests]" : "");
}

/// <summary>
/// Represents a discovered mob spawn point.
/// </summary>
public readonly struct MobSpawnPoint
{
    /// <summary>
    /// Unique identifier for this spawn point.
    /// </summary>
    public int Id { get; }

    /// <summary>
    /// The name of the mob.
    /// </summary>
    public string MobName { get; }

    /// <summary>
    /// The X coordinate where this mob spawns.
    /// </summary>
    public float X { get; }

    /// <summary>
    /// The Y coordinate (height) where this mob spawns.
    /// </summary>
    public float Y { get; }

    /// <summary>
    /// The Z coordinate where this mob spawns.
    /// </summary>
    public float Z { get; }

    /// <summary>
    /// The zone name where this mob spawns.
    /// </summary>
    public string Zone { get; }

    /// <summary>
    /// The level of this mob.
    /// </summary>
    public int Level { get; }

    /// <summary>
    /// The faction of this mob (Enemy, Neutral, etc.).
    /// </summary>
    public string Faction { get; }

    /// <summary>
    /// The timestamp when this spawn point was discovered.
    /// </summary>
    public DateTime DiscoveredAt { get; }

    /// <summary>
    /// The timestamp when this mob was last seen.
    /// </summary>
    public DateTime LastSeenAt { get; }

    /// <summary>
    /// The number of times this spawn point has been confirmed.
    /// </summary>
    public int TimesSeen { get; }

    /// <summary>
    /// Creates a new MobSpawnPoint.
    /// </summary>
    public MobSpawnPoint(
        int id,
        string mobName,
        float x, float y, float z,
        string zone,
        int level,
        string faction,
        DateTime discoveredAt,
        DateTime lastSeenAt,
        int timesSeen)
    {
        Id = id;
        MobName = mobName;
        X = x;
        Y = y;
        Z = z;
        Zone = zone;
        Level = level;
        Faction = faction;
        DiscoveredAt = discoveredAt;
        LastSeenAt = lastSeenAt;
        TimesSeen = timesSeen;
    }

    /// <summary>
    /// Returns a string representation of this spawn point.
    /// </summary>
    public override string ToString() =>
        $"{MobName} (Level {Level}) at ({X:F1}, {Y:F1}, {Z:F1}) in {Zone} - seen {TimesSeen}x";
}
