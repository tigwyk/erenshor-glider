using UnityEngine;

namespace ErenshorGlider.GameState;

/// <summary>
/// Represents the player's position in the game world.
/// </summary>
public readonly struct PlayerPosition
{
    /// <summary>
    /// X coordinate (horizontal axis).
    /// </summary>
    public float X { get; }

    /// <summary>
    /// Y coordinate (vertical axis / height).
    /// </summary>
    public float Y { get; }

    /// <summary>
    /// Z coordinate (horizontal axis, depth).
    /// </summary>
    public float Z { get; }

    /// <summary>
    /// Creates a new PlayerPosition from coordinates.
    /// </summary>
    public PlayerPosition(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    /// <summary>
    /// Creates a new PlayerPosition from a Unity Vector3.
    /// </summary>
    public PlayerPosition(Vector3 position)
    {
        X = position.x;
        Y = position.y;
        Z = position.z;
    }

    /// <summary>
    /// Converts this position to a Unity Vector3.
    /// </summary>
    public Vector3 ToVector3() => new(X, Y, Z);

    /// <summary>
    /// Calculates the distance to another position.
    /// </summary>
    public float DistanceTo(PlayerPosition other)
    {
        float dx = X - other.X;
        float dy = Y - other.Y;
        float dz = Z - other.Z;
        return Mathf.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    /// <summary>
    /// Calculates the horizontal (XZ plane) distance to another position.
    /// </summary>
    public float HorizontalDistanceTo(PlayerPosition other)
    {
        float dx = X - other.X;
        float dz = Z - other.Z;
        return Mathf.Sqrt(dx * dx + dz * dz);
    }

    public override string ToString() => $"({X:F2}, {Y:F2}, {Z:F2})";
}
