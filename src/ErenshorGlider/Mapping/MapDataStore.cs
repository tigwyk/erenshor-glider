using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ErenshorGlider.GameState;

namespace ErenshorGlider.Mapping;

/// <summary>
/// Stores discovered map data (nodes, NPCs, mobs).
/// Uses in-memory storage with optional JSON persistence.
/// </summary>
public class MapDataStore
{
    private readonly string _dataDirectory;
    private readonly List<ResourceNodeDiscovery> _resourceNodes;
    private readonly List<NpcDiscovery> _npcs;
    private readonly List<MobSpawnPoint> _mobSpawns;
    private readonly object _lock = new object();

    /// <summary>
    /// Default deduplication radius for map discoveries (in game units).
    /// Nodes within this distance are considered the same.
    /// </summary>
    public const float DefaultDeduplicationRadius = 5.0f;

    /// <summary>
    /// Gets or sets the deduplication radius for new discoveries.
    /// </summary>
    public float DeduplicationRadius { get; set; } = DefaultDeduplicationRadius;

    /// <summary>
    /// Gets the current zone name (set by the bot).
    /// </summary>
    public string CurrentZone { get; set; } = "Unknown";

    /// <summary>
    /// Gets all discovered resource nodes.
    /// </summary>
    public IReadOnlyList<ResourceNodeDiscovery> ResourceNodes
    {
        get
        {
            lock (_lock)
            {
                return _resourceNodes.ToList();
            }
        }
    }

    /// <summary>
    /// Gets all discovered NPCs.
    /// </summary>
    public IReadOnlyList<NpcDiscovery> Npcs
    {
        get
        {
            lock (_lock)
            {
                return _npcs.ToList();
            }
        }
    }

    /// <summary>
    /// Gets all discovered mob spawn points.
    /// </summary>
    public IReadOnlyList<MobSpawnPoint> MobSpawns
    {
        get
        {
            lock (_lock)
            {
                return _mobSpawns.ToList();
            }
        }
    }

    /// <summary>
    /// Event raised when a new resource node is discovered.
    /// </summary>
    public event Action<ResourceNodeDiscovery>? OnNodeDiscovered;

    /// <summary>
    /// Event raised when a new NPC is discovered.
    /// </summary>
    public event Action<NpcDiscovery>? OnNpcDiscovered;

    /// <summary>
    /// Event raised when a new mob spawn point is discovered.
    /// </summary>
    public event Action<MobSpawnPoint>? OnMobSpawnDiscovered;

    /// <summary>
    /// Creates a new MapDataStore.
    /// </summary>
    /// <param name="dataDirectory">Directory to store map data files.</param>
    public MapDataStore(string dataDirectory = "./mapdata")
    {
        _dataDirectory = dataDirectory;
        _resourceNodes = new List<ResourceNodeDiscovery>();
        _npcs = new List<NpcDiscovery>();
        _mobSpawns = new List<MobSpawnPoint>();

        // Ensure data directory exists
        if (!Directory.Exists(_dataDirectory))
        {
            Directory.CreateDirectory(_dataDirectory);
        }
    }

    /// <summary>
    /// Records or updates a resource node discovery.
    /// </summary>
    /// <returns>True if this was a new discovery, false if it was an update to an existing node.</returns>
    public bool RecordResourceNode(
        string nodeName,
        PlayerPosition position,
        string? requiredSkill = null)
    {
        lock (_lock)
        {
            var now = DateTime.UtcNow;

            // Check for nearby existing node
            var existing = FindNearbyResourceNode(position.X, position.Y, position.Z, nodeName);
            if (existing != null)
            {
                // Update last seen time and increment times seen
                var index = _resourceNodes.IndexOf(existing.Value);
                var updated = new ResourceNodeDiscovery(
                    existing.Value.Id,
                    existing.Value.NodeName,
                    existing.Value.X,
                    existing.Value.Y,
                    existing.Value.Z,
                    existing.Value.Zone,
                    existing.Value.DiscoveredAt,
                    now,
                    existing.Value.TimesSeen + 1,
                    existing.Value.RequiredSkill
                );
                _resourceNodes[index] = updated;
                return false;
            }

            // Create new discovery
            int id = _resourceNodes.Count > 0 ? _resourceNodes.Max(n => n.Id) + 1 : 1;
            var discovery = new ResourceNodeDiscovery(
                id,
                nodeName,
                position.X,
                position.Y,
                position.Z,
                CurrentZone,
                now,
                now,
                1,
                requiredSkill
            );
            _resourceNodes.Add(discovery);

            OnNodeDiscovered?.Invoke(discovery);
            return true;
        }
    }

    /// <summary>
    /// Records or updates an NPC discovery.
    /// </summary>
    /// <returns>True if this was a new discovery, false if it was an update to an existing NPC.</returns>
    public bool RecordNpc(
        string npcName,
        PlayerPosition position,
        bool isVendor,
        bool hasQuests)
    {
        lock (_lock)
        {
            var now = DateTime.UtcNow;

            // Check for nearby existing NPC with same name
            var existing = FindNearbyNpc(position.X, position.Y, position.Z, npcName);
            if (existing != null)
            {
                // Update last seen time
                var index = _npcs.IndexOf(existing.Value);
                var updated = new NpcDiscovery(
                    existing.Value.Id,
                    existing.Value.NpcName,
                    existing.Value.X,
                    existing.Value.Y,
                    existing.Value.Z,
                    existing.Value.Zone,
                    isVendor || existing.Value.IsVendor,
                    hasQuests || existing.Value.HasQuests,
                    existing.Value.DiscoveredAt,
                    now
                );
                _npcs[index] = updated;
                return false;
            }

            // Create new discovery
            int id = _npcs.Count > 0 ? _npcs.Max(n => n.Id) + 1 : 1;
            var discovery = new NpcDiscovery(
                id,
                npcName,
                position.X,
                position.Y,
                position.Z,
                CurrentZone,
                isVendor,
                hasQuests,
                now,
                now
            );
            _npcs.Add(discovery);

            OnNpcDiscovered?.Invoke(discovery);
            return true;
        }
    }

    /// <summary>
    /// Records or updates a mob spawn point.
    /// </summary>
    /// <returns>True if this was a new discovery, false if it was an update to an existing spawn point.</returns>
    public bool RecordMobSpawn(
        string mobName,
        PlayerPosition position,
        int level,
        string faction)
    {
        lock (_lock)
        {
            var now = DateTime.UtcNow;

            // Check for nearby existing mob spawn with same name
            var existing = FindNearbyMobSpawn(position.X, position.Y, position.Z, mobName);
            if (existing != null)
            {
                // Update last seen time and increment times seen
                var index = _mobSpawns.IndexOf(existing.Value);
                var updated = new MobSpawnPoint(
                    existing.Value.Id,
                    existing.Value.MobName,
                    existing.Value.X,
                    existing.Value.Y,
                    existing.Value.Z,
                    existing.Value.Zone,
                    existing.Value.Level,
                    existing.Value.Faction,
                    existing.Value.DiscoveredAt,
                    now,
                    existing.Value.TimesSeen + 1
                );
                _mobSpawns[index] = updated;
                return false;
            }

            // Create new discovery
            int id = _mobSpawns.Count > 0 ? _mobSpawns.Max(m => m.Id) + 1 : 1;
            var discovery = new MobSpawnPoint(
                id,
                mobName,
                position.X,
                position.Y,
                position.Z,
                CurrentZone,
                level,
                faction,
                now,
                now,
                1
            );
            _mobSpawns.Add(discovery);

            OnMobSpawnDiscovered?.Invoke(discovery);
            return true;
        }
    }

    /// <summary>
    /// Finds a nearby resource node within the deduplication radius.
    /// </summary>
    private ResourceNodeDiscovery? FindNearbyResourceNode(float x, float y, float z, string nodeName)
    {
        float radiusSquared = DeduplicationRadius * DeduplicationRadius;

        return _resourceNodes.FirstOrDefault(n =>
            n.NodeName == nodeName &&
            CalculateDistanceSquared(x, y, z, n.X, n.Y, n.Z) <= radiusSquared);
    }

    /// <summary>
    /// Finds a nearby NPC within the deduplication radius.
    /// </summary>
    private NpcDiscovery? FindNearbyNpc(float x, float y, float z, string npcName)
    {
        float radiusSquared = DeduplicationRadius * DeduplicationRadius;

        return _npcs.FirstOrDefault(n =>
            n.NpcName == npcName &&
            CalculateDistanceSquared(x, y, z, n.X, n.Y, n.Z) <= radiusSquared);
    }

    /// <summary>
    /// Finds a nearby mob spawn within the deduplication radius.
    /// </summary>
    private MobSpawnPoint? FindNearbyMobSpawn(float x, float y, float z, string mobName)
    {
        float radiusSquared = DeduplicationRadius * DeduplicationRadius;

        return _mobSpawns.FirstOrDefault(m =>
            m.MobName == mobName &&
            CalculateDistanceSquared(x, y, z, m.X, m.Y, m.Z) <= radiusSquared);
    }

    /// <summary>
    /// Calculates the squared distance between two points (X-Z plane for horizontal distance).
    /// </summary>
    private static float CalculateDistanceSquared(float x1, float y1, float z1, float x2, float y2, float z2)
    {
        float dx = x2 - x1;
        float dz = z2 - z1;
        return dx * dx + dz * dz;
    }

    /// <summary>
    /// Saves all discovered data to JSON files.
    /// </summary>
    public void SaveToDisk()
    {
        lock (_lock)
        {
            SaveResourceNodes();
            SaveNpcs();
            SaveMobSpawns();
        }
    }

    /// <summary>
    /// Loads discovered data from JSON files.
    /// </summary>
    public void LoadFromDisk()
    {
        lock (_lock)
        {
            LoadResourceNodes();
            LoadNpcs();
            LoadMobSpawns();
        }
    }

    private void SaveResourceNodes()
    {
        string path = Path.Combine(_dataDirectory, "resource_nodes.json");
        string json = System.Text.Json.JsonSerializer.Serialize(_resourceNodes, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
        });
        File.WriteAllText(path, json);
    }

    private void SaveNpcs()
    {
        string path = Path.Combine(_dataDirectory, "npcs.json");
        string json = System.Text.Json.JsonSerializer.Serialize(_npcs, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
        });
        File.WriteAllText(path, json);
    }

    private void SaveMobSpawns()
    {
        string path = Path.Combine(_dataDirectory, "mob_spawns.json");
        string json = System.Text.Json.JsonSerializer.Serialize(_mobSpawns, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
        });
        File.WriteAllText(path, json);
    }

    private void LoadResourceNodes()
    {
        string path = Path.Combine(_dataDirectory, "resource_nodes.json");
        if (!File.Exists(path))
            return;

        string json = File.ReadAllText(path);
        var nodes = System.Text.Json.JsonSerializer.Deserialize<List<ResourceNodeDiscovery>>(json, new System.Text.Json.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (nodes != null)
        {
            _resourceNodes.Clear();
            _resourceNodes.AddRange(nodes);
        }
    }

    private void LoadNpcs()
    {
        string path = Path.Combine(_dataDirectory, "npcs.json");
        if (!File.Exists(path))
            return;

        string json = File.ReadAllText(path);
        var npcs = System.Text.Json.JsonSerializer.Deserialize<List<NpcDiscovery>>(json, new System.Text.Json.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (npcs != null)
        {
            _npcs.Clear();
            _npcs.AddRange(npcs);
        }
    }

    private void LoadMobSpawns()
    {
        string path = Path.Combine(_dataDirectory, "mob_spawns.json");
        if (!File.Exists(path))
            return;

        string json = File.ReadAllText(path);
        var spawns = System.Text.Json.JsonSerializer.Deserialize<List<MobSpawnPoint>>(json, new System.Text.Json.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (spawns != null)
        {
            _mobSpawns.Clear();
            _mobSpawns.AddRange(spawns);
        }
    }

    /// <summary>
    /// Clears all discovered data.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _resourceNodes.Clear();
            _npcs.Clear();
            _mobSpawns.Clear();
        }
    }

    /// <summary>
    /// Gets statistics about the discovered data.
    /// </summary>
    public MapDataStatistics GetStatistics()
    {
        lock (_lock)
        {
            return new MapDataStatistics(
                _resourceNodes.Count,
                _npcs.Count(n => n.IsVendor),
                _npcs.Count(n => n.HasQuests),
                _mobSpawns.Count
            );
        }
    }
}

/// <summary>
/// Statistics about discovered map data.
/// </summary>
public readonly struct MapDataStatistics
{
    /// <summary>
    /// The number of discovered resource nodes.
    /// </summary>
    public int ResourceNodeCount { get; }

    /// <summary>
    /// The number of discovered vendors.
    /// </summary>
    public int VendorCount { get; }

    /// <summary>
    /// The number of discovered quest givers.
    /// </summary>
    public int QuestGiverCount { get; }

    /// <summary>
    /// The number of discovered mob spawn points.
    /// </summary>
    public int MobSpawnCount { get; }

    /// <summary>
    /// Creates a new MapDataStatistics.
    /// </summary>
    public MapDataStatistics(
        int resourceNodeCount,
        int vendorCount,
        int questGiverCount,
        int mobSpawnCount)
    {
        ResourceNodeCount = resourceNodeCount;
        VendorCount = vendorCount;
        QuestGiverCount = questGiverCount;
        MobSpawnCount = mobSpawnCount;
    }

    /// <summary>
    /// Returns a string representation of these statistics.
    /// </summary>
    public override string ToString() =>
        $"Nodes: {ResourceNodeCount}, Vendors: {VendorCount}, Quest Givers: {QuestGiverCount}, Mob Spawns: {MobSpawnCount}";
}
