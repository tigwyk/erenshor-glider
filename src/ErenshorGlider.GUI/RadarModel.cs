using System;
using System.Collections.Generic;

namespace ErenshorGlider.GUI;

/// <summary>
/// Types of entities that can appear on the radar.
/// </summary>
public enum RadarEntityType
{
    /// <summary>Hostile mob.</summary>
    HostileMob,
    /// <summary>Neutral mob.</summary>
    NeutralMob,
    /// <summary>Friendly NPC.</summary>
    FriendlyNpc,
    /// <summary>Resource node (mining, herbalism, etc.).</summary>
    ResourceNode,
    /// <summary>Lootable corpse.</summary>
    Corpse,
    /// <summary>Other player.</summary>
    Player
}

/// <summary>
/// An entity to display on the radar.
/// </summary>
public readonly struct RadarEntity
{
    /// <summary>
    /// Gets the entity type.
    /// </summary>
    public RadarEntityType Type { get; }

    /// <summary>
    /// Gets the relative X position from the player.
    /// </summary>
    public float RelativeX { get; }

    /// <summary>
    /// Gets the relative Z position from the player (depth in game, Y on radar).
    /// </summary>
    public float RelativeZ { get; }

    /// <summary>
    /// Gets the entity name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the entity level.
    /// </summary>
    public int Level { get; }

    public RadarEntity(RadarEntityType type, float relativeX, float relativeZ, string name, int level)
    {
        Type = type;
        RelativeX = relativeX;
        RelativeZ = relativeZ;
        Name = name;
        Level = level;
    }

    /// <summary>
    /// Gets the distance from the player (in game units).
    /// </summary>
    public float Distance => (float)Math.Sqrt(RelativeX * RelativeX + RelativeZ * RelativeZ);

    /// <summary>
    /// Gets the color to render this entity.
    /// </summary>
    public int Color => Type switch
    {
        RadarEntityType.HostileMob => 0xff3333,    // Red
        RadarEntityType.NeutralMob => 0xffcc00,    // Yellow
        RadarEntityType.FriendlyNpc => 0x33ff33,    // Green
        RadarEntityType.ResourceNode => 0x33ccff,   // Blue
        RadarEntityType.Corpse => 0x999999,         // Gray
        RadarEntityType.Player => 0xcc99ff,         // Purple
        _ => 0xffffff
    };
}

/// <summary>
/// Player radar information including position and facing direction.
/// </summary>
public readonly struct RadarPlayerInfo
{
    /// <summary>
    /// Gets the player's facing direction in degrees (0-360, where 0 is North/Z+).
    /// </summary>
    public float FacingDirection { get; }

    /// <summary>
    /// Gets the radar range (how far to display entities).
    /// </summary>
    public float Range { get; }

    public RadarPlayerInfo(float facingDirection, float range)
    {
        FacingDirection = facingDirection;
        Range = range;
    }
}

/// <summary>
/// Types of waypoints for radar display.
/// </summary>
public enum RadarWaypointType
{
    /// <summary>Normal waypoint.</summary>
    Normal,
    /// <summary>Vendor waypoint.</summary>
    Vendor,
    /// <summary>Repair waypoint.</summary>
    Repair,
    /// <summary>Resource node waypoint.</summary>
    Node,
    /// <summary>Quest giver waypoint.</summary>
    QuestGiver,
    /// <summary>Quest turn-in waypoint.</summary>
    QuestTurnIn,
    /// <summary>Rest area waypoint.</summary>
    RestArea,
    /// <summary>Danger zone waypoint.</summary>
    DangerZone
}

/// <summary>
/// A waypoint to display on the radar.
/// </summary>
public readonly struct RadarWaypoint
{
    /// <summary>
    /// Gets the waypoint type.
    /// </summary>
    public RadarWaypointType Type { get; }

    /// <summary>
    /// Gets the relative X position from the player.
    /// </summary>
    public float RelativeX { get; }

    /// <summary>
    /// Gets the relative Z position from the player (depth in game, Y on radar).
    /// </summary>
    public float RelativeZ { get; }

    /// <summary>
    /// Gets the waypoint name (optional).
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// Gets whether this is the current target waypoint.
    /// </summary>
    public bool IsTarget { get; }

    /// <summary>
    /// Gets the index of this waypoint in the path.
    /// </summary>
    public int Index { get; }

    public RadarWaypoint(RadarWaypointType type, float relativeX, float relativeZ, string? name, bool isTarget, int index)
    {
        Type = type;
        RelativeX = relativeX;
        RelativeZ = relativeZ;
        Name = name;
        IsTarget = isTarget;
        Index = index;
    }

    /// <summary>
    /// Gets the distance from the player (in game units).
    /// </summary>
    public float Distance => (float)Math.Sqrt(RelativeX * RelativeX + RelativeZ * RelativeZ);

    /// <summary>
    /// Gets the color to render this waypoint.
    /// </summary>
    public int Color => Type switch
    {
        RadarWaypointType.Normal => 0x666666,           // Dark gray
        RadarWaypointType.Vendor => 0x00cc00,           // Green
        RadarWaypointType.Repair => 0xcc9900,           // Orange
        RadarWaypointType.Node => 0x33ccff,             // Light blue
        RadarWaypointType.QuestGiver => 0xffff00,       // Yellow
        RadarWaypointType.QuestTurnIn => 0xffff00,      // Yellow
        RadarWaypointType.RestArea => 0x99ccff,         // Light blue
        RadarWaypointType.DangerZone => 0xff3333,       // Red
        _ => 0x666666
    };

    /// <summary>
    /// Gets the color to render this waypoint when it's the target.
    /// </summary>
    public int TargetColor => 0xffffff;  // White for target waypoint
}

/// <summary>
/// Interface for providing radar data.
/// </summary>
public interface IRadarDataProvider
{
    /// <summary>
    /// Gets the current player information.
    /// </summary>
    RadarPlayerInfo GetPlayerInfo();

    /// <summary>
    /// Gets the nearby entities within radar range.
    /// </summary>
    /// <param name="range">The radar range.</param>
    /// <returns>Nearby entities.</returns>
    RadarEntity[] GetNearbyEntities(float range);

    /// <summary>
    /// Gets the waypoints to display on the radar.
    /// </summary>
    /// <returns>Waypoints to display.</returns>
    IList<RadarWaypoint> GetWaypoints();

    /// <summary>
    /// Gets the connections between waypoints for path drawing.
    /// </summary>
    /// <returns>Pairs of waypoint indices that should be connected.</returns>
    IList<(int from, int to)> GetWaypointConnections();

    /// <summary>
    /// Event raised when radar data is updated.
    /// </summary>
    event EventHandler? RadarDataUpdated;
}

/// <summary>
/// Mock implementation of IRadarDataProvider for development/testing.
/// </summary>
public class MockRadarDataProvider : IRadarDataProvider
{
    private float _facingDirection = 0f;
    private float _range = 50f;
    private readonly Random _random = new Random();
    private readonly System.Collections.Generic.List<RadarEntity> _entities = new();
    private readonly System.Collections.Generic.List<RadarWaypoint> _waypoints = new();
    private readonly System.Collections.Generic.List<(int from, int to)> _waypointConnections = new();
    private int _targetWaypointIndex = 0;

    public float FacingDirection
    {
        get => _facingDirection;
        set => _facingDirection = value;
    }

    public float Range
    {
        get => _range;
        set => _range = value;
    }

    public event EventHandler? RadarDataUpdated;

    public RadarPlayerInfo GetPlayerInfo()
    {
        return new RadarPlayerInfo(_facingDirection, _range);
    }

    public RadarEntity[] GetNearbyEntities(float range)
    {
        lock (_entities)
        {
            return _entities.ToArray();
        }
    }

    public IList<RadarWaypoint> GetWaypoints()
    {
        lock (_waypoints)
        {
            return _waypoints.ToArray();
        }
    }

    public IList<(int from, int to)> GetWaypointConnections()
    {
        lock (_waypointConnections)
        {
            return _waypointConnections.ToArray();
        }
    }

    /// <summary>
    /// Sets the target waypoint index.
    /// </summary>
    public void SetTargetWaypoint(int index)
    {
        _targetWaypointIndex = index;
        UpdateWaypoints();
    }

    /// <summary>
    /// Simulates radar updates with random entities.
    /// </summary>
    public void SimulateUpdate()
    {
        // Update entities
        lock (_entities)
        {
            _entities.Clear();

            // Add some random entities around the player
            int entityCount = _random.Next(5, 15);
            for (int i = 0; i < entityCount; i++)
            {
                var type = (RadarEntityType)_random.Next(6);
                float x = (float)(_random.NextDouble() - 0.5) * 2 * _range;
                float z = (float)(_random.NextDouble() - 0.5) * 2 * _range;

                // Only add entities within range
                if (Math.Sqrt(x * x + z * z) <= _range)
                {
                    _entities.Add(new RadarEntity(
                        type,
                        x, z,
                        GenerateName(type),
                        _random.Next(1, 20)
                    ));
                }
            }
        }

        // Slowly rotate facing direction
        _facingDirection = (_facingDirection + 1f) % 360f;

        RadarDataUpdated?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Sets up a mock waypoint path for testing.
    /// </summary>
    public void SetupMockWaypointPath()
    {
        lock (_waypoints)
        lock (_waypointConnections)
        {
            _waypoints.Clear();
            _waypointConnections.Clear();

            // Create a circular path around the player
            int waypointCount = 8;
            float pathRadius = 30f;

            for (int i = 0; i < waypointCount; i++)
            {
                float angle = (i * 360f / waypointCount) * (float)Math.PI / 180f;
                float x = (float)Math.Cos(angle) * pathRadius;
                float z = (float)Math.Sin(angle) * pathRadius;

                var type = i switch
                {
                    0 => RadarWaypointType.Vendor,     // Start at vendor
                    2 => RadarWaypointType.Node,       // Resource node
                    4 => RadarWaypointType.Repair,     // Repair point
                    6 => RadarWaypointType.RestArea,   // Rest area
                    _ => RadarWaypointType.Normal
                };

                string? name = type != RadarWaypointType.Normal
                    ? type.ToString()
                    : $"WP {i + 1}";

                _waypoints.Add(new RadarWaypoint(
                    type,
                    x, z,
                    name,
                    i == _targetWaypointIndex,
                    i
                ));

                // Connect to next waypoint
                if (i < waypointCount - 1)
                {
                    _waypointConnections.Add((i, i + 1));
                }
            }

            // Connect last to first for loop
            _waypointConnections.Add((waypointCount - 1, 0));
        }
    }

    private void UpdateWaypoints()
    {
        lock (_waypoints)
        {
            // Update target flag for all waypoints
            var updated = new List<RadarWaypoint>(_waypoints.Count);
            foreach (var wp in _waypoints)
            {
                updated.Add(new RadarWaypoint(
                    wp.Type,
                    wp.RelativeX,
                    wp.RelativeZ,
                    wp.Name,
                    wp.Index == _targetWaypointIndex,
                    wp.Index
                ));
            }
            _waypoints.Clear();
            _waypoints.AddRange(updated);
        }
    }

    private string GenerateName(RadarEntityType type)
    {
        return type switch
        {
            RadarEntityType.HostileMob => $"Mob {_random.Next(1, 100)}",
            RadarEntityType.NeutralMob => $"Animal {_random.Next(1, 50)}",
            RadarEntityType.FriendlyNpc => $"NPC {_random.Next(1, 30)}",
            RadarEntityType.ResourceNode => $"Node",
            RadarEntityType.Corpse => $"Corpse",
            RadarEntityType.Player => $"Player",
            _ => "Unknown"
        };
    }
}
