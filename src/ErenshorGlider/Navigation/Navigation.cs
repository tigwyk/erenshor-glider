using System;
using ErenshorGlider.GameState;
using ErenshorGlider.Input;

namespace ErenshorGlider.Navigation;

/// <summary>
/// Handles navigation to specific coordinates in the game world.
/// </summary>
public class Navigation
{
    private readonly InputController _inputController;
    private readonly PositionTracker _positionTracker;
    private readonly Random _random = new();

    /// <summary>
    /// Gets or sets the stopping distance from the target.
    /// Navigation will stop when within this distance.
    /// </summary>
    public float StoppingDistance { get; set; } = 2f;

    /// <summary>
    /// Gets or sets whether to use pathing (smoother movement).
    /// </summary>
    public bool UsePathing { get; set; } = true;

    /// <summary>
    /// Creates a new Navigation instance.
    /// </summary>
    public Navigation(InputController inputController, PositionTracker positionTracker)
    {
        _inputController = inputController ?? throw new ArgumentNullException(nameof(inputController));
        _positionTracker = positionTracker ?? throw new ArgumentNullException(nameof(positionTracker));
    }

    /// <summary>
    /// Moves to the specified target position.
    /// </summary>
    /// <param name="targetPosition">The target coordinates.</param>
    /// <returns>True if movement started, false if already at target.</returns>
    public bool MoveTo(in PlayerPosition targetPosition)
    {
        var currentPosition = _positionTracker.CurrentPosition;
        if (currentPosition == null)
            return false;

        float distance = CalculateDistance(currentPosition.Value, targetPosition);
        if (distance <= StoppingDistance)
        {
            // Already at target
            StopMovement();
            return false;
        }

        // Calculate direction and start moving
        var direction = CalculateDirection(currentPosition.Value, targetPosition);
        MoveInDirection(direction);

        return true;
    }

    /// <summary>
    /// Moves to the specified target coordinates.
    /// </summary>
    /// <param name="x">Target X coordinate.</param>
    /// <param name="y">Target Y coordinate.</param>
    /// <param name="z">Target Z coordinate.</param>
    /// <returns>True if movement started, false if already at target.</returns>
    public bool MoveTo(float x, float y, float z)
    {
        return MoveTo(new PlayerPosition(x, y, z));
    }

    /// <summary>
    /// Checks if the current destination has been reached.
    /// </summary>
    /// <param name="targetPosition">The target position to check.</param>
    /// <returns>True if within stopping distance of target.</returns>
    public bool HasReached(in PlayerPosition targetPosition)
    {
        var currentPosition = _positionTracker.CurrentPosition;
        if (currentPosition == null)
            return false;

        float distance = CalculateDistance(currentPosition.Value, targetPosition);
        return distance <= StoppingDistance;
    }

    /// <summary>
    /// Gets the distance to the target position.
    /// </summary>
    public float GetDistanceTo(in PlayerPosition targetPosition)
    {
        var currentPosition = _positionTracker.CurrentPosition;
        if (currentPosition == null)
            return float.MaxValue;

        return CalculateDistance(currentPosition.Value, targetPosition);
    }

    /// <summary>
    /// Stops all movement.
    /// </summary>
    public void StopMovement()
    {
        _inputController.StopAllMovement();
    }

    /// <summary>
    /// Calculates the distance between two positions.
    /// </summary>
    public static float CalculateDistance(in PlayerPosition from, in PlayerPosition to)
    {
        float dx = to.X - from.X;
        float dy = to.Y - from.Y;
        float dz = to.Z - from.Z;
        return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    /// <summary>
    /// Calculates the squared distance (faster, no sqrt).
    /// </summary>
    public static float CalculateDistanceSquared(in PlayerPosition from, in PlayerPosition to)
    {
        float dx = to.X - from.X;
        float dy = to.Y - from.Y;
        float dz = to.Z - from.Z;
        return dx * dx + dy * dy + dz * dz;
    }

    /// <summary>
    /// Calculates the direction from current position to target.
    /// </summary>
    public static NavigationDirection CalculateDirection(in PlayerPosition from, in PlayerPosition to)
    {
        float dx = to.X - from.X;
        float dz = to.Z - from.Z; // Use X-Z plane for horizontal direction

        // Calculate angle in degrees
        float angle = (float)Math.Atan2(dz, dx) * (180f / (float)Math.PI);

        // Convert to game-relative direction (assuming 0 degrees = East, +90 = South in game coords)
        // Adjust based on game's coordinate system
        return AngleToDirection(angle);
    }

    /// <summary>
    /// Converts an angle to a navigation direction.
    /// </summary>
    private static NavigationDirection AngleToDirection(float angleDegrees)
    {
        // Normalize angle to 0-360
        angleDegrees = (angleDegrees % 360 + 360) % 360;

        // Determine primary direction based on angle
        // 8 directions: N, NE, E, SE, S, SW, W, NW
        return angleDegrees switch
        {
            >= 337.5f or < 22.5f => NavigationDirection.Forward,
            >= 22.5f and < 67.5f => NavigationDirection.ForwardRight,
            >= 67.5f and < 112.5f => NavigationDirection.Right,
            >= 112.5f and < 157.5f => NavigationDirection.BackwardRight,
            >= 157.5f and < 202.5f => NavigationDirection.Backward,
            >= 202.5f and < 247.5f => NavigationDirection.BackwardLeft,
            >= 247.5f and < 292.5f => NavigationDirection.Left,
            >= 292.5f and < 337.5f => NavigationDirection.ForwardLeft,
            _ => NavigationDirection.Forward
        };
    }

    /// <summary>
    /// Moves in the specified direction.
    /// </summary>
    private void MoveInDirection(NavigationDirection direction)
    {
        // Stop any existing movement
        _inputController.StopAllMovement();

        // Apply new movement based on direction
        switch (direction)
        {
            case NavigationDirection.Forward:
                _inputController.MoveForward();
                break;
            case NavigationDirection.Backward:
                _inputController.MoveBackward();
                break;
            case NavigationDirection.Left:
                _inputController.StrafeLeft();
                break;
            case NavigationDirection.Right:
                _inputController.StrafeRight();
                break;
            case NavigationDirection.ForwardLeft:
                _inputController.MoveForward();
                _inputController.StrafeLeft();
                break;
            case NavigationDirection.ForwardRight:
                _inputController.MoveForward();
                _inputController.StrafeRight();
                break;
            case NavigationDirection.BackwardLeft:
                _inputController.MoveBackward();
                _inputController.StrafeLeft();
                break;
            case NavigationDirection.BackwardRight:
                _inputController.MoveBackward();
                _inputController.StrafeRight();
                break;
        }
    }

    /// <summary>
    /// Event raised when destination is reached.
    /// </summary>
    public event Action<PlayerPosition>? OnDestinationReached;

    /// <summary>
    /// Event raised when movement is stuck (no progress).
    /// </summary>
    public event Action? OnMovementStuck;
}

/// <summary>
/// Represents a cardinal or intercardinal direction for navigation.
/// </summary>
public enum NavigationDirection
{
    /// <summary>Forward (North)</summary>
    Forward,
    /// <summary>Backward (South)</summary>
    Backward,
    /// <summary>Left (West)</summary>
    Left,
    /// <summary>Right (East)</summary>
    Right,
    /// <summary>Forward and Left (Northwest)</summary>
    ForwardLeft,
    /// <summary>Forward and Right (Northeast)</summary>
    ForwardRight,
    /// <summary>Backward and Left (Southwest)</summary>
    BackwardLeft,
    /// <summary>Backward and Right (Southeast)</summary>
    BackwardRight
}
