using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using ErenshorGlider.GameState;

namespace ErenshorGlider.Waypoints;

/// <summary>
/// Represents a single waypoint in a path.
/// </summary>
public class Waypoint
{
    /// <summary>
    /// Gets or sets the position of this waypoint.
    /// </summary>
    [JsonRequired]
    public PlayerPosition Position { get; set; }

    /// <summary>
    /// Gets or sets the type of this waypoint.
    /// </summary>
    [JsonRequired]
    public WaypointType Type { get; set; } = WaypointType.Normal;

    /// <summary>
    /// Gets or sets the name of this waypoint (optional).
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets additional metadata for this waypoint.
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();

    /// <summary>
    /// Gets or sets the delay to wait at this waypoint (in seconds).
    /// </summary>
    public float Delay { get; set; }

    /// <summary>
    /// Creates a new waypoint.
    /// </summary>
    public Waypoint()
    {
        Position = new PlayerPosition(0, 0, 0);
    }

    /// <summary>
    /// Creates a new waypoint at the specified position.
    /// </summary>
    public Waypoint(float x, float y, float z, WaypointType type = WaypointType.Normal)
    {
        Position = new PlayerPosition(x, y, z);
        Type = type;
    }

    /// <summary>
    /// Creates a new waypoint at the specified position.
    /// </summary>
    public Waypoint(in PlayerPosition position, WaypointType type = WaypointType.Normal)
    {
        Position = position;
        Type = type;
    }

    public override string ToString()
    {
        return string.IsNullOrEmpty(Name)
            ? $"Waypoint ({Position.X:F1}, {Position.Y:F1}, {Position.Z:F1}) - {Type}"
            : $"{Name} ({Position.X:F1}, {Position.Y:F1}, {Position.Z:F1}) - {Type}";
    }
}

/// <summary>
/// The type of waypoint.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum WaypointType
{
    /// <summary>Normal waypoint for pathing.</summary>
    Normal,
    /// <summary>Vendor waypoint - sells items/buys goods.</summary>
    Vendor,
    /// <summary>Repair waypoint - repairs equipment.</summary>
    Repair,
    /// <summary>Resource node waypoint - mining/herbalism node.</summary>
    Node,
    /// <summary>Quest giver waypoint.</summary>
    QuestGiver,
    /// <summary>Quest turn-in waypoint.</summary>
    QuestTurnIn,
    /// <summary>Rest area waypoint - safe to rest/regen.</summary>
    RestArea,
    /// <summary>Danger zone waypoint - high risk area.</summary>
    DangerZone
}

/// <summary>
/// Represents a complete path of waypoints.
/// </summary>
public class WaypointPath
{
    /// <summary>
    /// Gets or sets the name of this path.
    /// </summary>
    [JsonRequired]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of waypoints in this path.
    /// </summary>
    [JsonRequired]
    public List<Waypoint> Waypoints { get; set; } = new();

    /// <summary>
    /// Gets or sets whether this path loops back to the start.
    /// </summary>
    public bool Loop { get; set; }

    /// <summary>
    /// Gets or sets whether to reverse direction at the end of the path.
    /// </summary>
    public bool ReverseAtEnd { get; set; }

    /// <summary>
    /// Gets or sets the description of this path.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the recommended level range for this path.
    /// </summary>
    public string? LevelRange { get; set; }

    /// <summary>
    /// Gets or sets the zone/area where this path is located.
    /// </summary>
    public string? Zone { get; set; }

    /// <summary>
    /// Gets or sets additional metadata for this path.
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();

    /// <summary>
    /// Gets or sets when this path was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets when this path was last modified.
    /// </summary>
    public DateTime LastModified { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Creates a new waypoint path.
    /// </summary>
    public WaypointPath()
    {
        Name = string.Empty;
    }

    /// <summary>
    /// Creates a new waypoint path with the specified name.
    /// </summary>
    public WaypointPath(string name)
    {
        Name = name ?? string.Empty;
    }

    /// <summary>
    /// Adds a waypoint to the end of the path.
    /// </summary>
    public void AddWaypoint(in Waypoint waypoint)
    {
        Waypoints.Add(waypoint);
        LastModified = DateTime.UtcNow;
    }

    /// <summary>
    /// Adds a waypoint at the specified position.
    /// </summary>
    public void AddWaypoint(float x, float y, float z, WaypointType type = WaypointType.Normal)
    {
        AddWaypoint(new Waypoint(x, y, z, type));
    }

    /// <summary>
    /// Removes the waypoint at the specified index.
    /// </summary>
    public bool RemoveWaypoint(int index)
    {
        if (index < 0 || index >= Waypoints.Count)
            return false;

        Waypoints.RemoveAt(index);
        LastModified = DateTime.UtcNow;
        return true;
    }

    /// <summary>
    /// Clears all waypoints from the path.
    /// </summary>
    public void ClearWaypoints()
    {
        Waypoints.Clear();
        LastModified = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the total number of waypoints.
    /// </summary>
    public int Count => Waypoints.Count;

    /// <summary>
    /// Gets whether the path has any waypoints.
    /// </summary>
    public bool HasWaypoints => Waypoints.Count > 0;

    /// <summary>
    /// Saves this path to a JSON file.
    /// </summary>
    public void SaveToFile(string filePath)
    {
        LastModified = DateTime.UtcNow;
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        string json = JsonSerializer.Serialize(this, options);
        File.WriteAllText(filePath, json);
    }

    /// <summary>
    /// Loads a path from a JSON file.
    /// </summary>
    public static WaypointPath? LoadFromFile(string filePath)
    {
        if (!File.Exists(filePath))
            return null;

        string json = File.ReadAllText(filePath);
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            AllowTrailingCommas = true
        };
        return JsonSerializer.Deserialize<WaypointPath>(json, options);
    }

    /// <summary>
    /// Validates the path and returns any errors.
    /// </summary>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Name))
            errors.Add("Path name is required.");

        if (Waypoints.Count < 2)
            errors.Add("Path must have at least 2 waypoints.");

        for (int i = 0; i < Waypoints.Count; i++)
        {
            var wp = Waypoints[i];
            if (wp.Type == WaypointType.Vendor && string.IsNullOrEmpty(wp.Name))
                errors.Add($"Waypoint {i} is marked as Vendor but has no name.");
        }

        if (Loop && ReverseAtEnd)
            errors.Add("Path cannot have both Loop and ReverseAtEnd enabled.");

        return errors;
    }

    public override string ToString()
    {
        return $"{Name} ({Waypoints.Count} waypoints, {(Loop ? "Loop" : ReverseAtEnd ? "Reverse" : "One-way")})";
    }
}

/// <summary>
/// Manages saving and loading waypoint files.
/// </summary>
public static class WaypointFileManager
{
    /// <summary>
    /// The default directory for waypoint files.
    /// </summary>
    public static string DefaultWaypointDirectory => Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory,
        "waypoints"
    );

    /// <summary>
    /// Gets all waypoint files in the default directory.
    /// </summary>
    public static string[] GetWaypointFiles()
    {
        return GetWaypointFiles(DefaultWaypointDirectory);
    }

    /// <summary>
    /// Gets all waypoint files in the specified directory.
    /// </summary>
    public static string[] GetWaypointFiles(string directory)
    {
        if (!Directory.Exists(directory))
            return Array.Empty<string>();

        return Directory.GetFiles(directory, "*.json");
    }

    /// <summary>
    /// Saves a waypoint path to the default directory.
    /// </summary>
    public static void SavePath(WaypointPath path, string? fileName = null)
    {
        Directory.CreateDirectory(DefaultWaypointDirectory);

        fileName ??= SanitizeFileName(path.Name) + ".json";
        string filePath = Path.Combine(DefaultWaypointDirectory, fileName);

        path.SaveToFile(filePath);
    }

    /// <summary>
    /// Loads a waypoint path by name from the default directory.
    /// </summary>
    public static WaypointPath? LoadPath(string name)
    {
        string fileName = SanitizeFileName(name) + ".json";
        string filePath = Path.Combine(DefaultWaypointDirectory, fileName);

        return WaypointPath.LoadFromFile(filePath);
    }

    /// <summary>
    /// Loads all waypoint paths from the default directory.
    /// </summary>
    public static List<WaypointPath> LoadAllPaths()
    {
        return LoadAllPaths(DefaultWaypointDirectory);
    }

    /// <summary>
    /// Loads all waypoint paths from the specified directory.
    /// </summary>
    public static List<WaypointPath> LoadAllPaths(string directory)
    {
        var paths = new List<WaypointPath>();

        if (!Directory.Exists(directory))
            return paths;

        foreach (var file in GetWaypointFiles(directory))
        {
            try
            {
                var path = WaypointPath.LoadFromFile(file);
                if (path != null)
                    paths.Add(path);
            }
            catch (Exception ex)
            {
                // Log error but continue loading other files
                Console.WriteLine($"Error loading waypoint file {file}: {ex.Message}");
            }
        }

        return paths;
    }

    /// <summary>
    /// Sanitizes a filename by removing invalid characters.
    /// </summary>
    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(fileName.Where(c => !invalidChars.Contains(c)).ToArray());
        return string.IsNullOrWhiteSpace(sanitized) ? "waypoint" : sanitized;
    }
}
