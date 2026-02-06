using System;

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

    /// <summary>
    /// Simulates radar updates with random entities.
    /// </summary>
    public void SimulateUpdate()
    {
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

        RadarDataUpdated?.Invoke(this, EventArgs.Empty);
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
