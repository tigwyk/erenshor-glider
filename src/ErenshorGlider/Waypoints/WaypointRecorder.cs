using System;
using System.Collections.Generic;
using ErenshorGlider.GameState;

namespace ErenshorGlider.Waypoints;

/// <summary>
/// Records waypoints as the player moves through the world.
/// </summary>
public class WaypointRecorder
{
    private readonly PositionTracker _positionTracker;
    private readonly WaypointPath _recordingPath;
    private PlayerPosition? _lastRecordedPosition;
    private DateTime _lastRecordTime = DateTime.UtcNow;
    private bool _isRecording;

    /// <summary>
    /// Gets or sets the minimum distance between recorded waypoints.
    /// Waypoints closer than this will not be recorded.
    /// </summary>
    public float MinDistanceBetweenWaypoints { get; set; } = 5f;

    /// <summary>
    /// Gets or sets the minimum time between recording waypoints.
    /// </summary>
    public float MinTimeBetweenRecords { get; set; } = 1f;

    /// <summary>
    /// Gets or sets the default waypoint type for recorded waypoints.
    /// </summary>
    public WaypointType DefaultWaypointType { get; set; } = WaypointType.Normal;

    /// <summary>
    /// Gets whether currently recording.
    /// </summary>
    public bool IsRecording => _isRecording;

    /// <summary>
    /// Gets the number of waypoints recorded in the current session.
    /// </summary>
    public int RecordedWaypointCount => _recordingPath.Waypoints.Count;

    /// <summary>
    /// Gets the name of the current recording.
    /// </summary>
    public string? RecordingName { get; private set; }

    /// <summary>
    /// Event raised when a waypoint is recorded.
    /// </summary>
    public event Action<Waypoint>? OnWaypointRecorded;

    /// <summary>
    /// Event raised when recording starts.
    /// </summary>
    public event Action? OnRecordingStarted;

    /// <summary>
    /// Event raised when recording stops.
    /// </summary>
    public event Action<WaypointPath>? OnRecordingStopped;

    /// <summary>
    /// Creates a new WaypointRecorder.
    /// </summary>
    public WaypointRecorder(PositionTracker positionTracker)
    {
        _positionTracker = positionTracker ?? throw new ArgumentNullException(nameof(positionTracker));
        _recordingPath = new WaypointPath();
    }

    /// <summary>
    /// Starts recording a new waypoint path.
    /// </summary>
    /// <param name="pathName">The name for the path being recorded.</param>
    /// <returns>True if recording started, false if already recording.</returns>
    public bool StartRecording(string pathName)
    {
        if (_isRecording)
            return false;

        _recordingPath.Name = pathName ?? $"Recording_{DateTime.UtcNow:yyyyMMdd_HHmmss}";
        _recordingPath.Waypoints.Clear();
        _recordingPath.CreatedAt = DateTime.UtcNow;
        _recordingPath.LastModified = DateTime.UtcNow;

        _isRecording = true;
        RecordingName = _recordingPath.Name;
        _lastRecordedPosition = null;
        _lastRecordTime = DateTime.UtcNow;

        // Record the starting position
        RecordCurrentPosition();

        OnRecordingStarted?.Invoke();
        return true;
    }

    /// <summary>
    /// Stops recording and returns the recorded path.
    /// </summary>
    /// <returns>The recorded waypoint path, or null if not recording.</returns>
    public WaypointPath? StopRecording()
    {
        if (!_isRecording)
            return null;

        _isRecording = false;

        // Record the final position
        RecordCurrentPosition();

        var result = new WaypointPath(_recordingPath.Name)
        {
            Description = _recordingPath.Description,
            Waypoints = new List<global::System.Collections.Generic.Waypoint>(_recordingPath.Waypoints),
            CreatedAt = _recordingPath.CreatedAt,
            LastModified = DateTime.UtcNow
        };

        RecordingName = null;
        OnRecordingStopped?.Invoke(result);

        return result;
    }

    /// <summary>
    /// Stops recording and saves the path to a file.
    /// </summary>
    /// <returns>The saved path, or null if not recording or save failed.</returns>
    public WaypointPath? StopAndSave()
    {
        var path = StopRecording();
        if (path != null)
        {
            try
            {
                WaypointFileManager.SavePath(path);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving waypoint path: {ex.Message}");
                return null;
            }
        }
        return path;
    }

    /// <summary>
    /// Updates the recorder. Should be called periodically to capture waypoints.
    /// </summary>
    public void Update()
    {
        if (!_isRecording)
            return;

        var currentPosition = _positionTracker.CurrentPosition;
        if (currentPosition == null)
            return;

        var timeSinceLastRecord = (float)(DateTime.UtcNow - _lastRecordTime).TotalSeconds;

        // Check if enough time has passed
        if (timeSinceLastRecord < MinTimeBetweenRecords)
            return;

        // Check if we've moved far enough
        if (_lastRecordedPosition.HasValue)
        {
            float distance = CalculateDistance(_lastRecordedPosition.Value, currentPosition.Value);
            if (distance < MinDistanceBetweenWaypoints)
                return;
        }

        // Record the position
        RecordCurrentPosition();
    }

    /// <summary>
    /// Manually records the current position as a waypoint.
    /// </summary>
    /// <param name="waypointType">Optional type for this waypoint.</param>
    /// <param name="name">Optional name for this waypoint.</param>
    /// <returns>True if waypoint was recorded, false if not recording.</returns>
    public bool RecordCurrentPosition(WaypointType? waypointType = null, string? name = null)
    {
        if (!_isRecording)
            return false;

        var currentPosition = _positionTracker.CurrentPosition;
        if (currentPosition == null)
            return false;

        var waypoint = new Waypoint(currentPosition.Value, waypointType ?? DefaultWaypointType)
        {
            Name = name
        };

        _recordingPath.AddWaypoint(waypoint);
        _lastRecordedPosition = currentPosition;
        _lastRecordTime = DateTime.UtcNow;

        OnWaypointRecorded?.Invoke(waypoint);
        return true;
    }

    /// <summary>
    /// Records a vendor waypoint at the current position.
    /// </summary>
    public bool RecordVendor(string? name = null)
    {
        return RecordCurrentPosition(WaypointType.Vendor, name ?? "Vendor");
    }

    /// <summary>
    /// Records a repair waypoint at the current position.
    /// </summary>
    public bool RecordRepair(string? name = null)
    {
        return RecordCurrentPosition(WaypointType.Repair, name ?? "Repair");
    }

    /// <summary>
    /// Records a resource node waypoint at the current position.
    /// </summary>
    public bool RecordNode(string? name = null)
    {
        return RecordCurrentPosition(WaypointType.Node, name ?? "Resource Node");
    }

    /// <summary>
    /// Clears the current recording without saving.
    /// </summary>
    public void CancelRecording()
    {
        if (!_isRecording)
            return;

        _isRecording = false;
        _recordingPath.Waypoints.Clear();
        RecordingName = null;
        _lastRecordedPosition = null;
    }

    /// <summary>
    /// Calculates distance between two positions.
    /// </summary>
    private static float CalculateDistance(in PlayerPosition from, in PlayerPosition to)
    {
        float dx = to.X - from.X;
        float dy = to.Y - from.Y;
        float dz = to.Z - from.Z;
        return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }
}
