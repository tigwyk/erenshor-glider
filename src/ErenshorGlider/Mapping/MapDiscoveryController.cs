using System;
using System.Linq;
using ErenshorGlider.GameState;

namespace ErenshorGlider.Mapping;

/// <summary>
/// Controller for automatic map discovery.
/// Scans nearby entities and records them to the MapDataStore.
/// </summary>
public class MapDiscoveryController
{
    private readonly MapDataStore _dataStore;
    private readonly PositionTracker _positionTracker;
    private readonly GameStateReader _gameStateReader;

    private HashSet<string> _recordedNodes = new HashSet<string>();
    private HashSet<string> _recordedNpcs = new HashSet<string>();
    private HashSet<string> _recordedMobs = new HashSet<string>();

    /// <summary>
    /// Gets or sets whether auto-discovery is enabled.
    /// </summary>
    public bool AutoDiscoveryEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to record resource nodes.
    /// </summary>
    public bool RecordResourceNodes { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to record NPCs.
    /// </summary>
    public bool RecordNpcs { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to record mob spawn points.
    /// </summary>
    public bool RecordMobSpawns { get; set; } = true;

    /// <summary>
    /// Gets or sets the minimum time between recording the same entity.
    /// </summary>
    public TimeSpan MinRecordInterval { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets the number of nodes discovered this session.
    /// </summary>
    public int NodesDiscoveredCount { get; private set; }

    /// <summary>
    /// Gets the number of NPCs discovered this session.
    /// </summary>
    public int NpcsDiscoveredCount { get; private set; }

    /// <summary>
    /// Gets the number of mob spawns discovered this session.
    /// </summary>
    public int MobSpawnsDiscoveredCount { get; private set; }

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
    /// Creates a new MapDiscoveryController.
    /// </summary>
    public MapDiscoveryController(
        MapDataStore dataStore,
        PositionTracker positionTracker,
        GameStateReader gameStateReader)
    {
        _dataStore = dataStore ?? throw new ArgumentNullException(nameof(dataStore));
        _positionTracker = positionTracker ?? throw new ArgumentNullException(nameof(positionTracker));
        _gameStateReader = gameStateReader ?? throw new ArgumentNullException(nameof(gameStateReader));

        // Forward events from data store
        _dataStore.OnNodeDiscovered += d =>
        {
            NodesDiscoveredCount++;
            OnNodeDiscovered?.Invoke(d);
        };
        _dataStore.OnNpcDiscovered += d =>
        {
            NpcsDiscoveredCount++;
            OnNpcDiscovered?.Invoke(d);
        };
        _dataStore.OnMobSpawnDiscovered += d =>
        {
            MobSpawnsDiscoveredCount++;
            OnMobSpawnDiscovered?.Invoke(d);
        };
    }

    /// <summary>
    /// Updates the discovery controller. Should be called regularly.
    /// </summary>
    public void Update()
    {
        if (!AutoDiscoveryEnabled)
            return;

        var entities = _gameStateReader.GetCachedNearbyEntities();
        if (entities == null || entities.Items.Count == 0)
            return;

        var position = _positionTracker.CurrentPosition;
        if (!position.HasValue)
            return;

        foreach (var entity in entities.Items)
        {
            string entityKey = $"{entity.Name}_{entity.Position.X:F0}_{entity.Position.Z:F0}";

            switch (entity.Type)
            {
                case EntityType.Node:
                    if (RecordResourceNodes && CanRecordEntity(entityKey, _recordedNodes))
                    {
                        RecordResourceNode(entity, position.Value);
                        _recordedNodes.Add(entityKey);
                    }
                    break;

                case EntityType.NPC:
                    if (RecordNpcs && CanRecordEntity(entityKey, _recordedNpcs))
                    {
                        RecordNpc(entity, position.Value);
                        _recordedNpcs.Add(entityKey);
                    }
                    break;

                case EntityType.Mob:
                    if (RecordMobSpawns && CanRecordEntity(entityKey, _recordedMobs))
                    {
                        RecordMobSpawn(entity, position.Value);
                        _recordedMobs.Add(entityKey);
                    }
                    break;
            }
        }
    }

    /// <summary>
    /// Checks if an entity can be recorded based on recent recording history.
    /// </summary>
    private bool CanRecordEntity(string entityKey, HashSet<string> recordedSet)
    {
        // For now, we use a simple session-based approach
        // A more sophisticated implementation would track timestamps per entity
        return !recordedSet.Contains(entityKey);
    }

    /// <summary>
    /// Records a resource node discovery.
    /// </summary>
    private void RecordResourceNode(EntityInfo entity, PlayerPosition playerPosition)
    {
        // We record the node at the entity's position, not the player's position
        var nodePosition = entity.Position;

        _dataStore.RecordResourceNode(
            entity.Name,
            nodePosition,
            GetRequiredSkillForNode(entity.Name)
        );
    }

    /// <summary>
    /// Records an NPC discovery.
    /// </summary>
    private void RecordNpc(EntityInfo entity, PlayerPosition playerPosition)
    {
        // Record NPC at entity's position
        var npcPosition = entity.Position;

        // For NPCs, we need to determine if they're a vendor or quest giver
        // This information may come from the entity or need to be inferred
        bool isVendor = entity.Name.Contains("Vendor") || entity.Name.Contains("Merchant") || entity.Name.Contains("Trader");
        bool hasQuests = entity.Name.Contains("Quest") || entity.Name.Contains("Captain") || entity.Name.Contains("Guard");

        _dataStore.RecordNpc(
            entity.Name,
            npcPosition,
            isVendor,
            hasQuests
        );
    }

    /// <summary>
    /// Records a mob spawn point discovery.
    /// </summary>
    private void RecordMobSpawn(EntityInfo entity, PlayerPosition playerPosition)
    {
        // Record spawn at entity's position
        var spawnPosition = entity.Position;

        _dataStore.RecordMobSpawn(
            entity.Name,
            spawnPosition,
            entity.Level,
            entity.Hostility.ToString()
        );
    }

    /// <summary>
    /// Determines the required skill for a resource node based on its name.
    /// </summary>
    private string? GetRequiredSkillForNode(string nodeName)
    {
        if (nodeName.Contains("Ore") || nodeName.Contains("Iron") || nodeName.Contains("Copper") ||
            nodeName.Contains("Tin") || nodeName.Contains("Silver") || nodeName.Contains("Gold"))
            return "Mining";

        if (nodeName.Contains("Herb") || nodeName.Contains("Plant") || nodeName.Contains("Flower"))
            return "Herbalism";

        if (nodeName.Contains("Tree") || nodeName.Contains("Wood"))
            return "Woodcutting";

        return null;
    }

    /// <summary>
    /// Resets the session recording history.
    /// </summary>
    public void ResetRecordingHistory()
    {
        _recordedNodes.Clear();
        _recordedNpcs.Clear();
        _recordedMobs.Clear();
    }

    /// <summary>
    /// Saves discovered data to disk.
    /// </summary>
    public void SaveToDisk()
    {
        _dataStore.SaveToDisk();
    }

    /// <summary>
    /// Loads discovered data from disk.
    /// </summary>
    public void LoadFromDisk()
    {
        _dataStore.LoadFromDisk();
    }

    /// <summary>
    /// Gets the current statistics.
    /// </summary>
    public MapDataStatistics GetStatistics()
    {
        return _dataStore.GetStatistics();
    }
}
