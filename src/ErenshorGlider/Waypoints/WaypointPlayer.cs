using System;
using ErenshorGlider.GameState;
using ErenshorGlider.Input;
using ErenshorGlider.Navigation;
using NavNavigation = ErenshorGlider.Navigation.Navigation;

namespace ErenshorGlider.Waypoints;

/// <summary>
/// Plays back a waypoint path, moving the character along the route.
/// </summary>
public class WaypointPlayer
{
    private readonly NavNavigation _navigation;
    private readonly PositionTracker _positionTracker;
    private WaypointPath? _currentPath;
    private int _currentWaypointIndex;
    private bool _isPlaying;
    private bool _isReversed;

    /// <summary>
    /// Tracks when we started waiting at a waypoint with a delay.
    /// </summary>
    private DateTime _delayStartTime;

    /// <summary>
    /// Whether we're currently waiting for a waypoint delay to complete.
    /// </summary>
    private bool _waitingForDelay;

    /// <summary>
    /// Gets or sets whether to override the path's loop setting.
    /// If null, uses the path's Loop setting.
    /// </summary>
    public bool? OverrideLoop { get; set; }

    /// <summary>
    /// Gets whether currently playing a path.
    /// </summary>
    public bool IsPlaying => _isPlaying;

    /// <summary>
    /// Gets the current path being played.
    /// </summary>
    public WaypointPath? CurrentPath => _currentPath;

    /// <summary>
    /// Gets the index of the current target waypoint.
    /// </summary>
    public int CurrentWaypointIndex => _currentWaypointIndex;

    /// <summary>
    /// Gets the current target waypoint.
    /// </summary>
    public Waypoint? CurrentWaypoint
    {
        get
        {
            if (_currentPath == null || _currentPath.Waypoints.Count == 0)
                return null;

            int actualIndex = _isReversed
                ? _currentPath.Waypoints.Count - 1 - _currentWaypointIndex
                : _currentWaypointIndex;

            if (actualIndex < 0 || actualIndex >= _currentPath.Waypoints.Count)
                return null;

            return _currentPath.Waypoints[actualIndex];
        }
    }

    /// <summary>
    /// Event raised when a waypoint is reached.
    /// </summary>
    public event Action<Waypoint>? OnWaypointReached;

    /// <summary>
    /// Event raised when path playback completes.
    /// </summary>
    public event Action<WaypointPath>? OnPathCompleted;

    /// <summary>
    /// Event raised when playback starts.
    /// </summary>
    public event Action<WaypointPath>? OnPlaybackStarted;

    /// <summary>
    /// Event raised when movement is stuck (no progress).
    /// </summary>
    public event Action<bool>? OnMovementStuckChanged;

    /// <summary>
    /// Creates a new WaypointPlayer.
    /// </summary>
    public WaypointPlayer(NavNavigation navigation, PositionTracker positionTracker)
    {
        _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
        _positionTracker = positionTracker ?? throw new ArgumentNullException(nameof(positionTracker));

        // Subscribe to navigation stuck state changes
        _navigation.OnStuckStateChanged += HandleNavigationStuckStateChanged;
    }

    /// <summary>
    /// Handles navigation stuck state changed event.
    /// </summary>
    private void HandleNavigationStuckStateChanged(bool isStuck)
    {
        // Forward the stuck state change
        OnMovementStuckChanged?.Invoke(isStuck);
    }

    /// <summary>
    /// Loads and starts playing a path from file.
    /// </summary>
    /// <param name="pathName">The name of the path to load.</param>
    /// <returns>True if playback started, false if path not found.</returns>
    public bool PlayPath(string pathName)
    {
        var path = WaypointFileManager.LoadPath(pathName);
        if (path == null)
            return false;

        return PlayPath(path);
    }

    /// <summary>
    /// Starts playing a waypoint path.
    /// </summary>
    public bool PlayPath(WaypointPath path)
    {
        if (path == null || path.Waypoints.Count == 0)
            return false;

        _currentPath = path;
        _currentWaypointIndex = 0;
        _isReversed = false;
        _isPlaying = true;

        // Find nearest waypoint to start from
        FindNearestWaypoint();

        _navigation.ResetStuckDetection();

        OnPlaybackStarted?.Invoke(path);
        return true;
    }

    /// <summary>
    /// Stops the current playback.
    /// </summary>
    public void Stop()
    {
        _isPlaying = false;
        _waitingForDelay = false;
        _navigation.StopMovement();
    }

    /// <summary>
    /// Pauses the current playback.
    /// </summary>
    public void Pause()
    {
        _isPlaying = false;
        _navigation.StopMovement();
    }

    /// <summary>
    /// Resumes paused playback.
    /// </summary>
    public void Resume()
    {
        if (_currentPath != null)
            _isPlaying = true;
    }

    /// <summary>
    /// Starts or resumes playback. Alias for Resume().
    /// </summary>
    public void Play()
    {
        Resume();
    }

    /// <summary>
    /// Updates the waypoint player. Should be called regularly.
    /// </summary>
    public void Update()
    {
        if (!_isPlaying || _currentPath == null)
            return;

        // Check for stuck state
        if (_navigation.CheckAndAttemptUnstick())
            return;

        // Skip delay if we enter combat while waiting
        if (_waitingForDelay && IsInCombat())
        {
            _waitingForDelay = false;
            AdvanceToNextWaypoint();
            return;
        }

        // Check if we're waiting for a delay to complete
        if (_waitingForDelay)
        {
            var elapsed = (DateTime.UtcNow - _delayStartTime).TotalSeconds;
            if (elapsed >= CurrentWaypoint?.Delay)
            {
                // Delay completed
                _waitingForDelay = false;
                AdvanceToNextWaypoint();
            }
            return;
        }

        var currentWaypoint = CurrentWaypoint;
        if (currentWaypoint == null)
        {
            // Should not happen, but handle gracefully
            AdvanceToNextWaypoint();
            return;
        }

        // Check if we've reached the current waypoint
        if (_navigation.HasReached(currentWaypoint.Position))
        {
            OnWaypointReached?.Invoke(currentWaypoint);

            // Wait at waypoint if delay is specified
            if (currentWaypoint.Delay > 0)
            {
                _delayStartTime = DateTime.UtcNow;
                _waitingForDelay = true;
                return;
            }

            AdvanceToNextWaypoint();
        }
        else
        {
            // Move toward current waypoint
            _navigation.MoveTo(currentWaypoint.Position);
        }
    }

    /// <summary>
    /// Checks if the player is currently in combat.
    /// </summary>
    private bool IsInCombat()
    {
        var combatState = _positionTracker.CurrentCombatState;
        return combatState.HasValue && combatState.Value.InCombat;
    }

    /// <summary>
    /// Advances to the next waypoint in the path.
    /// </summary>
    private void AdvanceToNextWaypoint()
    {
        if (_currentPath == null)
            return;

        bool shouldLoop = OverrideLoop ?? _currentPath.Loop;

        if (_isReversed)
        {
            // Moving backward through path
            _currentWaypointIndex++;

            if (_currentWaypointIndex >= _currentPath.Waypoints.Count)
            {
                if (shouldLoop)
                {
                    // Reverse direction
                    _isReversed = false;
                    _currentWaypointIndex = 1; // Skip the first waypoint as we're already there
                }
                else if (_currentPath.ReverseAtEnd)
                {
                    // Continue in forward direction
                    _isReversed = false;
                    _currentWaypointIndex = 1;
                }
                else
                {
                    // Path completed
                    CompletePath();
                }
            }
        }
        else
        {
            // Moving forward through path
            _currentWaypointIndex++;

            if (_currentWaypointIndex >= _currentPath.Waypoints.Count)
            {
                if (shouldLoop)
                {
                    // Loop back to start
                    _currentWaypointIndex = 0;
                }
                else if (_currentPath.ReverseAtEnd)
                {
                    // Reverse direction
                    _isReversed = true;
                    _currentWaypointIndex = 1; // Start from second-to-last waypoint
                }
                else
                {
                    // Path completed
                    CompletePath();
                }
            }
        }
    }

    /// <summary>
    /// Completes the current path playback.
    /// </summary>
    private void CompletePath()
    {
        if (_currentPath == null)
            return;

        _isPlaying = false;
        _navigation.StopMovement();

        OnPathCompleted?.Invoke(_currentPath);
    }

    /// <summary>
    /// Finds the nearest waypoint to start from.
    /// </summary>
    private void FindNearestWaypoint()
    {
        if (_currentPath == null)
            return;

        var currentPosition = _positionTracker.CurrentPosition;
        if (currentPosition == null)
            return;

        int nearestIndex = 0;
        float nearestDistance = float.MaxValue;

        for (int i = 0; i < _currentPath.Waypoints.Count; i++)
        {
            float distance = NavNavigation.CalculateDistance(
                currentPosition.Value,
                _currentPath.Waypoints[i].Position
            );

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestIndex = i;
            }
        }

        _currentWaypointIndex = nearestIndex;
    }

    /// <summary>
    /// Jumps to a specific waypoint in the path.
    /// </summary>
    public bool JumpToWaypoint(int index)
    {
        if (_currentPath == null || index < 0 || index >= _currentPath.Waypoints.Count)
            return false;

        _currentWaypointIndex = index;
        return true;
    }

    /// <summary>
    /// Skips to the next waypoint.
    /// </summary>
    public bool SkipToNext()
    {
        if (_currentPath == null)
            return false;

        _currentWaypointIndex = Math.Min(_currentWaypointIndex + 1, _currentPath.Waypoints.Count - 1);
        return true;
    }

    /// <summary>
    /// Skips to the previous waypoint.
    /// </summary>
    public bool SkipToPrevious()
    {
        if (_currentPath == null)
            return false;

        _currentWaypointIndex = Math.Max(_currentWaypointIndex - 1, 0);
        return true;
    }
}
