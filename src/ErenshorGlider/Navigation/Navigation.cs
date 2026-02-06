using System;
using ErenshorGlider.GameState;
using ErenshorGlider.Input;

#if !USE_REAL_GAME_TYPES
using ErenshorGlider.GameStubs;
#endif

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
    /// Gets or sets the stuck detection threshold in seconds.
    /// If position hasn't changed after this duration, the bot is considered stuck.
    /// </summary>
    public float StuckDetectionThreshold { get; set; } = 2f;

    /// <summary>
    /// Gets or sets the maximum number of unstuck attempts.
    /// </summary>
    public int MaxUnstuckAttempts { get; set; } = 3;

    /// <summary>
    /// Gets or sets the minimum distance threshold to consider movement as progress.
    /// </summary>
    public float MovementProgressThreshold { get; set; } = 0.5f;

    /// <summary>
    /// Gets the current stuck state.
    /// </summary>
    public bool IsStuck { get; private set; }

    /// <summary>
    /// Gets the number of unstuck attempts made.
    /// </summary>
    public int UnstuckAttempts { get; private set; }

    /// <summary>
    /// Gets the last recorded position for stuck detection.
    /// </summary>
    private PlayerPosition? _lastStuckCheckPosition;

    /// <summary>
    /// Gets the time of the last stuck check.
    /// </summary>
    private DateTime _lastStuckCheckTime = DateTime.UtcNow;

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
    /// Checks if the bot is stuck and attempts to unstuck.
    /// Should be called periodically during navigation.
    /// </summary>
    /// <returns>True if stuck and attempting to recover, false if not stuck.</returns>
    public bool CheckAndAttemptUnstick()
    {
        var currentPosition = _positionTracker.CurrentPosition;
        if (currentPosition == null)
            return false;

        // Check if enough time has passed for a stuck check
        var timeSinceLastCheck = DateTime.UtcNow - _lastStuckCheckTime;
        if (timeSinceLastCheck.TotalSeconds < StuckDetectionThreshold)
            return IsStuck;

        // Update check time and position
        _lastStuckCheckTime = DateTime.UtcNow;

        // Check if we've made progress
        if (_lastStuckCheckPosition.HasValue)
        {
            float distanceMoved = CalculateDistance(_lastStuckCheckPosition.Value, currentPosition.Value);

            if (distanceMoved < MovementProgressThreshold)
            {
                // No significant movement - we might be stuck
                if (!IsStuck)
                {
                    // First detection of being stuck
                    IsStuck = true;
                    UnstuckAttempts = 0;
                    OnStuckStateChanged?.Invoke(true);
                }

                // Attempt to unstuck
                return AttemptUnstick();
            }
            else
            {
                // We're making progress
                if (IsStuck)
                {
                    // Successfully unstuck
                    IsStuck = false;
                    UnstuckAttempts = 0;
                    OnStuckStateChanged?.Invoke(false);
                }
            }
        }

        // Update last position for next check
        _lastStuckCheckPosition = currentPosition.Value;
        return false;
    }

    /// <summary>
    /// Updates the navigation state.
    /// Should be called regularly during navigation to check for stuck conditions.
    /// </summary>
    public void Update()
    {
        CheckAndAttemptUnstick();
    }

    /// <summary>
    /// Attempts to unstuck the bot using various strategies.
    /// </summary>
    /// <returns>True if attempting to unstuck, false if max attempts reached.</returns>
    private bool AttemptUnstick()
    {
        if (UnstuckAttempts >= MaxUnstuckAttempts)
        {
            // Max attempts reached - report stuck state
            OnMovementStuck?.Invoke();
            return false;
        }

        UnstuckAttempts++;

        // Try different unstuck strategies based on attempt number
        switch (UnstuckAttempts)
        {
            case 1:
                // First attempt: jump
                _inputController.Jump();
                break;
            case 2:
                // Second attempt: strafe in random direction
                if (_random.Next(2) == 0)
                    _inputController.StrafeLeft(0.5f);
                else
                    _inputController.StrafeRight(0.5f);
                break;
            case 3:
                // Third attempt: back up
                _inputController.MoveBackward(0.5f);
                _inputController.Jump();
                break;
            default:
                // Last resort: turn around
                _inputController.TurnRight(0.3f);
                _inputController.Jump();
                break;
        }

        return true;
    }

    /// <summary>
    /// Resets the stuck detection state.
    /// Call this when starting a new navigation task.
    /// </summary>
    public void ResetStuckDetection()
    {
        IsStuck = false;
        UnstuckAttempts = 0;
        _lastStuckCheckPosition = null;
        _lastStuckCheckTime = DateTime.UtcNow;
    }

    #region Facing

    /// <summary>
    /// Gets or sets the facing tolerance in degrees.
    /// The bot will stop turning when within this angle of the target direction.
    /// </summary>
    public float FacingTolerance { get; set; } = 10f;

    /// <summary>
    /// Faces the specified target position.
    /// </summary>
    /// <param name="targetPosition">The position to face.</param>
    /// <returns>True if turning started, false if already facing within tolerance.</returns>
    public bool FaceTarget(in PlayerPosition targetPosition)
    {
        var currentPosition = _positionTracker.CurrentPosition;
        if (currentPosition == null)
            return false;

        float currentRotation = GetPlayerRotation();
        float targetAngle = CalculateAngleToTarget(currentPosition.Value, targetPosition);
        float angleDifference = NormalizeAngleDelta(targetAngle - currentRotation);

        if (Math.Abs(angleDifference) <= FacingTolerance)
        {
            // Already facing within tolerance
            StopTurning();
            return false;
        }

        // Turn in the appropriate direction
        if (angleDifference > 0)
            _inputController.TurnRight();
        else
            _inputController.TurnLeft();

        return true;
    }

    /// <summary>
    /// Faces a specific entity.
    /// </summary>
    /// <param name="entityInfo">The entity to face.</param>
    /// <returns>True if turning started, false if already facing within tolerance.</returns>
    public bool FaceEntity(in GameState.EntityInfo entityInfo)
    {
        return FaceTarget(entityInfo.Position);
    }

    /// <summary>
    /// Checks if currently facing the target within tolerance.
    /// </summary>
    public bool IsFacing(in PlayerPosition targetPosition)
    {
        var currentPosition = _positionTracker.CurrentPosition;
        if (currentPosition == null)
            return false;

        float currentRotation = GetPlayerRotation();
        float targetAngle = CalculateAngleToTarget(currentPosition.Value, targetPosition);
        float angleDifference = NormalizeAngleDelta(targetAngle - currentRotation);

        return Math.Abs(angleDifference) <= FacingTolerance;
    }

    /// <summary>
    /// Stops all turning input.
    /// </summary>
    public void StopTurning()
    {
        _inputController.ReleaseKey(KeyCode.LeftArrow);
        _inputController.ReleaseKey(KeyCode.RightArrow);
    }

    /// <summary>
    /// Calculates the angle to the target position in degrees.
    /// </summary>
    private static float CalculateAngleToTarget(in PlayerPosition from, in PlayerPosition to)
    {
        float dx = to.X - from.X;
        float dz = to.Z - from.Z;
        return (float)Math.Atan2(dz, dx) * (180f / (float)Math.PI);
    }

    /// <summary>
    /// Normalizes an angle difference to the range [-180, 180].
    /// </summary>
    private static float NormalizeAngleDelta(float angle)
    {
        while (angle > 180f) angle -= 360f;
        while (angle < -180f) angle += 360f;
        return angle;
    }

    /// <summary>
    /// Gets the player's current rotation in degrees.
    /// Reads from GameData.PlayerControl.transform.rotation.eulerAngles.y
    /// </summary>
    /// <returns>The player's Y-axis rotation in degrees (0-360), or 0 if unavailable.</returns>
    private float GetPlayerRotation()
    {
#if !USE_REAL_GAME_TYPES
        // Stub implementation - return cached rotation or 0
        // In stub mode, we can't read real game state
        return 0f;
#else
        // Real implementation - read from GameData singleton
        try
        {
            if (GameData.PlayerControl == null)
                return 0f;

            if (GameData.PlayerControl.transform == null)
                return 0f;

            // Read rotation from Unity transform
            // eulerAngles.y is the rotation around the Y axis (yaw/heading) in degrees
            float rotation = GameData.PlayerControl.transform.rotation.eulerAngles.y;

            // Normalize to 0-360 range
            if (rotation < 0)
                rotation += 360f;
            if (rotation >= 360f)
                rotation %= 360f;

            return rotation;
        }
        catch
        {
            // Gracefully handle any errors (e.g., during scene transitions)
            return 0f;
        }
#endif
    }

    #endregion

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
#pragma warning disable CS0067 // Event is never used
    public event Action<PlayerPosition>? OnDestinationReached;
#pragma warning restore CS0067

    /// <summary>
    /// Event raised when movement is stuck (no progress).
    /// </summary>
    public event Action? OnMovementStuck;

    /// <summary>
    /// Event raised when the stuck state changes.
    /// </summary>
    public event Action<bool>? OnStuckStateChanged;
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
