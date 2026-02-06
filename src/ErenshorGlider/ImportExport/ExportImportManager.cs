using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using ErenshorGlider.Combat;
using ErenshorGlider.Waypoints;

namespace ErenshorGlider.ImportExport;

/// <summary>
/// Result of an import operation.
/// </summary>
public enum ImportResult
{
    /// <summary>Import was successful.</summary>
    Success,
    /// <summary>File not found.</summary>
    FileNotFound,
    /// <summary>Invalid file format.</summary>
    InvalidFormat,
    /// <summary>Validation failed.</summary>
    ValidationFailed,
    /// <summary>Import was cancelled.</summary>
    Cancelled,
    /// <summary>Import failed for other reasons.</summary>
    Failed
}

/// <summary>
/// Result of an export operation.
/// </summary>
public enum ExportResult
{
    /// <summary>Export was successful.</summary>
    Success,
    /// <summary>File already exists.</summary>
    FileExists,
    /// <summary>Invalid data.</summary>
    InvalidData,
    /// <summary>Export failed.</summary>
    Failed
}

/// <summary>
/// Manages export and import of waypoints and profiles.
/// </summary>
public static class ExportImportManager
{
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        AllowTrailingCommas = true
    };

    #region Waypoint Export

    /// <summary>
    /// Exports a waypoint path to a standalone JSON file.
    /// </summary>
    /// <param name="path">The waypoint path to export.</param>
    /// <param name="filePath">The destination file path. If null, generates a filename.</param>
    /// <param name="overwrite">Whether to overwrite existing files.</param>
    /// <returns>The result of the export operation.</returns>
    public static ExportResult ExportWaypointPath(WaypointPath path, string? filePath = null, bool overwrite = false)
    {
        if (path == null)
            return ExportResult.InvalidData;

        // Validate the path before exporting
        var errors = path.Validate();
        if (errors.Count > 0)
            return ExportResult.InvalidData;

        // Generate filename if not provided
        if (string.IsNullOrWhiteSpace(filePath))
        {
            string sanitizedName = SanitizeFileName(path.Name);
            filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "exports", $"waypoints_{sanitizedName}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json");
        }

        // Check if file exists
        if (File.Exists(filePath) && !overwrite)
        {
            return ExportResult.FileExists;
        }

        try
        {
            // Ensure directory exists
            string directory = Path.GetDirectoryName(filePath) ?? "exports";
            Directory.CreateDirectory(directory);

            // Export with metadata
            var exportData = new WaypointExportData
            {
                Version = "1.0",
                ExportedAt = DateTime.UtcNow,
                ExportedBy = "Erenshor Glider",
                Path = path
            };

            string json = JsonSerializer.Serialize(exportData, JsonOptions);
            File.WriteAllText(filePath, json);

            return ExportResult.Success;
        }
        catch (Exception)
        {
            return ExportResult.Failed;
        }
    }

    /// <summary>
    /// Imports a waypoint path from a JSON file.
    /// </summary>
    /// <param name="filePath">The path to the file to import.</param>
    /// <param name="validateOnly">If true, only validates without importing.</param>
    /// <returns>The result of the import operation and the imported path.</returns>
    public static (ImportResult Result, WaypointPath? Path) ImportWaypointPath(string filePath, bool validateOnly = false)
    {
        if (!File.Exists(filePath))
            return (ImportResult.FileNotFound, null);

        try
        {
            string json = File.ReadAllText(filePath);

            // Try to parse as WaypointExportData first
            var exportData = JsonSerializer.Deserialize<WaypointExportData>(json, JsonOptions);
            WaypointPath? path;

            if (exportData != null && exportData.Path != null)
            {
                path = exportData.Path;
            }
            else
            {
                // Try to parse as plain WaypointPath
                path = JsonSerializer.Deserialize<WaypointPath>(json, JsonOptions);
            }

            if (path == null)
                return (ImportResult.InvalidFormat, null);

            // Validate the imported path
            var errors = path.Validate();
            if (errors.Count > 0)
                return (ImportResult.ValidationFailed, null);

            if (validateOnly)
                return (ImportResult.Success, path);

            return (ImportResult.Success, path);
        }
        catch (JsonException)
        {
            return (ImportResult.InvalidFormat, null);
        }
        catch (Exception)
        {
            return (ImportResult.Failed, null);
        }
    }

    /// <summary>
    /// Imports a waypoint path and saves it to the waypoints directory.
    /// </summary>
    /// <param name="filePath">The path to the file to import.</param>
    /// <param name="newName">Optional new name for the imported path.</param>
    /// <returns>The result of the import operation.</returns>
    public static ImportResult ImportAndSaveWaypointPath(string filePath, string? newName = null)
    {
        var (result, path) = ImportWaypointPath(filePath);
        if (result != ImportResult.Success || path == null)
            return result;

        // Set new name if provided
        if (!string.IsNullOrWhiteSpace(newName))
        {
            path.Name = newName!;
        }

        // Save to waypoints directory
        WaypointFileManager.SavePath(path);
        return ImportResult.Success;
    }

    #endregion

    #region Profile Export

    /// <summary>
    /// Exports a combat profile to a standalone JSON file.
    /// </summary>
    /// <param name="profile">The combat profile to export.</param>
    /// <param name="filePath">The destination file path. If null, generates a filename.</param>
    /// <param name="overwrite">Whether to overwrite existing files.</param>
    /// <returns>The result of the export operation.</returns>
    public static ExportResult ExportCombatProfile(CombatProfile profile, string? filePath = null, bool overwrite = false)
    {
        if (profile == null)
            return ExportResult.InvalidData;

        // Validate the profile before exporting
        var errors = profile.Validate();
        if (errors.Count > 0)
            return ExportResult.InvalidData;

        // Generate filename if not provided
        if (string.IsNullOrWhiteSpace(filePath))
        {
            string sanitizedName = SanitizeFileName(profile.Name);
            filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "exports", $"profile_{sanitizedName}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json");
        }

        // Check if file exists
        if (File.Exists(filePath) && !overwrite)
        {
            return ExportResult.FileExists;
        }

        try
        {
            // Ensure directory exists
            string directory = Path.GetDirectoryName(filePath) ?? "exports";
            Directory.CreateDirectory(directory);

            // Export with metadata
            var exportData = new ProfileExportData
            {
                Version = "1.0",
                ExportedAt = DateTime.UtcNow,
                ExportedBy = "Erenshor Glider",
                Profile = profile
            };

            string json = JsonSerializer.Serialize(exportData, JsonOptions);
            File.WriteAllText(filePath, json);

            return ExportResult.Success;
        }
        catch (Exception)
        {
            return ExportResult.Failed;
        }
    }

    /// <summary>
    /// Imports a combat profile from a JSON file.
    /// </summary>
    /// <param name="filePath">The path to the file to import.</param>
    /// <param name="validateOnly">If true, only validates without importing.</param>
    /// <returns>The result of the import operation and the imported profile.</returns>
    public static (ImportResult Result, CombatProfile? Profile) ImportCombatProfile(string filePath, bool validateOnly = false)
    {
        if (!File.Exists(filePath))
            return (ImportResult.FileNotFound, null);

        try
        {
            string json = File.ReadAllText(filePath);

            // Try to parse as ProfileExportData first
            var exportData = JsonSerializer.Deserialize<ProfileExportData>(json, JsonOptions);
            CombatProfile? profile;

            if (exportData != null && exportData.Profile != null)
            {
                profile = exportData.Profile;
            }
            else
            {
                // Try to parse as plain CombatProfile
                profile = JsonSerializer.Deserialize<CombatProfile>(json, JsonOptions);
            }

            if (profile == null)
                return (ImportResult.InvalidFormat, null);

            // Validate the imported profile
            var errors = profile.Validate();
            if (errors.Count > 0)
                return (ImportResult.ValidationFailed, null);

            if (validateOnly)
                return (ImportResult.Success, profile);

            return (ImportResult.Success, profile);
        }
        catch (JsonException)
        {
            return (ImportResult.InvalidFormat, null);
        }
        catch (Exception)
        {
            return (ImportResult.Failed, null);
        }
    }

    /// <summary>
    /// Imports a combat profile and saves it to the profiles directory.
    /// </summary>
    /// <param name="filePath">The path to the file to import.</param>
    /// <param name="newName">Optional new name for the imported profile.</param>
    /// <returns>The result of the import operation.</returns>
    public static ImportResult ImportAndSaveCombatProfile(string filePath, string? newName = null)
    {
        var (result, profile) = ImportCombatProfile(filePath);
        if (result != ImportResult.Success || profile == null)
            return result;

        // Set new name if provided
        if (!string.IsNullOrWhiteSpace(newName))
        {
            profile.Name = newName!;
        }

        // Save to profiles directory
        CombatProfileFileManager.SaveProfile(profile, profile.Name);
        return ImportResult.Success;
    }

    #endregion

    #region Bulk Export

    /// <summary>
    /// Exports all waypoint paths to individual JSON files.
    /// </summary>
    /// <param name="outputDirectory">The directory to export to. If null, uses exports/waypoints.</param>
    /// <param name="overwrite">Whether to overwrite existing files.</param>
    /// <returns>The number of paths successfully exported.</returns>
    public static int ExportAllWaypointPaths(string? outputDirectory = null, bool overwrite = false)
    {
        outputDirectory ??= Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "exports", "waypoints");
        Directory.CreateDirectory(outputDirectory);

        int exportCount = 0;
        var paths = WaypointFileManager.LoadAllPaths();

        foreach (var path in paths)
        {
            string sanitizedName = SanitizeFileName(path.Name);
            string filePath = Path.Combine(outputDirectory, $"{sanitizedName}.json");

            if (ExportWaypointPath(path, filePath, overwrite) == ExportResult.Success)
            {
                exportCount++;
            }
        }

        return exportCount;
    }

    /// <summary>
    /// Exports all combat profiles to individual JSON files.
    /// </summary>
    /// <param name="outputDirectory">The directory to export to. If null, uses exports/profiles.</param>
    /// <param name="overwrite">Whether to overwrite existing files.</param>
    /// <returns>The number of profiles successfully exported.</returns>
    public static int ExportAllCombatProfiles(string? outputDirectory = null, bool overwrite = false)
    {
        outputDirectory ??= Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "exports", "profiles");
        Directory.CreateDirectory(outputDirectory);

        int exportCount = 0;
        var profiles = CombatProfileFileManager.LoadAllProfiles();

        foreach (var profile in profiles)
        {
            string sanitizedName = SanitizeFileName(profile.Name);
            string filePath = Path.Combine(outputDirectory, $"{sanitizedName}.json");

            if (ExportCombatProfile(profile, filePath, overwrite) == ExportResult.Success)
            {
                exportCount++;
            }
        }

        return exportCount;
    }

    #endregion

    #region Bulk Import

    /// <summary>
    /// Imports all waypoint paths from a directory.
    /// </summary>
    /// <param name="inputDirectory">The directory to import from.</param>
    /// <param name="overwrite">Whether to overwrite existing profiles.</param>
    /// <returns>The number of paths successfully imported.</returns>
    public static int ImportWaypointPathsFromDirectory(string inputDirectory, bool overwrite = false)
    {
        if (!Directory.Exists(inputDirectory))
            return 0;

        int importCount = 0;
        var files = Directory.GetFiles(inputDirectory, "*.json");

        foreach (var file in files)
        {
            var (result, path) = ImportWaypointPath(file);
            if (result == ImportResult.Success && path != null)
            {
                // Check if path already exists
                var existing = WaypointFileManager.LoadPath(path.Name);
                if (existing != null && !overwrite)
                    continue;

                WaypointFileManager.SavePath(path);
                importCount++;
            }
        }

        return importCount;
    }

    /// <summary>
    /// Imports all combat profiles from a directory.
    /// </summary>
    /// <param name="inputDirectory">The directory to import from.</param>
    /// <param name="overwrite">Whether to overwrite existing profiles.</param>
    /// <returns>The number of profiles successfully imported.</returns>
    public static int ImportCombatProfilesFromDirectory(string inputDirectory, bool overwrite = false)
    {
        if (!Directory.Exists(inputDirectory))
            return 0;

        int importCount = 0;
        var files = Directory.GetFiles(inputDirectory, "*.json");

        foreach (var file in files)
        {
            var (result, profile) = ImportCombatProfile(file);
            if (result == ImportResult.Success && profile != null)
            {
                // Check if profile already exists
                var existing = CombatProfileFileManager.LoadProfile(profile.Name);
                if (existing != null && !overwrite)
                    continue;

                CombatProfileFileManager.SaveProfile(profile, profile.Name);
                importCount++;
            }
        }

        return importCount;
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Sanitizes a filename by removing invalid characters.
    /// </summary>
    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(fileName.Where(c => !invalidChars.Contains(c)).ToArray());
        return string.IsNullOrWhiteSpace(sanitized) ? "unnamed" : sanitized;
    }

    #endregion
}

#region Export Data Classes

/// <summary>
/// Wrapper for waypoint path export data.
/// </summary>
public class WaypointExportData
{
    /// <summary>Gets or sets the export format version.</summary>
    public string Version { get; set; } = "1.0";

    /// <summary>Gets or sets when the export was created.</summary>
    public DateTime ExportedAt { get; set; }

    /// <summary>Gets or sets who created the export.</summary>
    public string? ExportedBy { get; set; }

    /// <summary>Gets or sets the waypoint path data.</summary>
    public WaypointPath? Path { get; set; }
}

/// <summary>
/// Wrapper for combat profile export data.
/// </summary>
public class ProfileExportData
{
    /// <summary>Gets or sets the export format version.</summary>
    public string Version { get; set; } = "1.0";

    /// <summary>Gets or sets when the export was created.</summary>
    public DateTime ExportedAt { get; set; }

    /// <summary>Gets or sets who created the export.</summary>
    public string? ExportedBy { get; set; }

    /// <summary>Gets or sets the combat profile data.</summary>
    public CombatProfile? Profile { get; set; }
}

#endregion
