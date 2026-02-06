using System;
using System.IO;
using System.Text.Json;

namespace ErenshorGlider.GUI.Installation;

/// <summary>
/// Configuration for installation settings.
/// </summary>
public class InstallationConfig
{
    /// <summary>
    /// Gets or sets the path to the Erenshor installation.
    /// </summary>
    public string? ErenshorPath { get; set; }

    /// <summary>
    /// Gets or sets the date of the last successful plugin update check (ISO 8601 format).
    /// </summary>
    public string? LastUpdateCheck { get; set; }

    /// <summary>
    /// Gets or sets whether the user has opted out of update checks.
    /// </summary>
    public bool OptOutFromUpdateChecks { get; set; }

    /// <summary>
    /// Gets or sets the last installed plugin version.
    /// </summary>
    public string? LastInstalledPluginVersion { get; set; }
}

/// <summary>
/// Manages loading and saving installation configuration.
/// </summary>
public static class InstallationConfigManager
{
    private const string ConfigFileName = "installation.json";
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    /// <summary>
    /// Gets the full path to the configuration file.
    /// </summary>
    public static string ConfigFilePath
    {
        get
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(localAppData, "ErenshorGlider", ConfigFileName);
        }
    }

    /// <summary>
    /// Loads the installation configuration from disk.
    /// </summary>
    /// <returns>The loaded configuration, or null if the file doesn't exist.</returns>
    public static InstallationConfig? Load()
    {
        string path = ConfigFilePath;

        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            string json = File.ReadAllText(path);
            var config = JsonSerializer.Deserialize<InstallationConfig>(json, JsonOptions);

            // Return defaults if deserialization failed
            return config ?? new InstallationConfig();
        }
        catch (JsonException)
        {
            // Return a default config if JSON is malformed
            return new InstallationConfig();
        }
        catch (IOException)
        {
            // Return null if file is inaccessible
            return null;
        }
    }

    /// <summary>
    /// Saves the installation configuration to disk.
    /// </summary>
    /// <param name="config">The configuration to save.</param>
    /// <returns>True if the configuration was saved successfully, false otherwise.</returns>
    public static bool Save(InstallationConfig config)
    {
        if (config == null)
        {
            return false;
        }

        try
        {
            string path = ConfigFilePath;
            string? directory = Path.GetDirectoryName(path);

            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string json = JsonSerializer.Serialize(config, JsonOptions);
            File.WriteAllText(path, json);

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// Deletes the installation configuration file.
    /// </summary>
    /// <returns>True if the file was deleted successfully, false otherwise.</returns>
    public static bool Delete()
    {
        try
        {
            string path = ConfigFilePath;

            if (File.Exists(path))
            {
                File.Delete(path);
                return true;
            }

            return false;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
