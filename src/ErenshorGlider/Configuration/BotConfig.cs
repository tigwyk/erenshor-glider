using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace ErenshorGlider.Configuration;

/// <summary>
/// Main configuration for the Erenshor Glider bot.
/// </summary>
public class BotConfig
{
    // ========== Combat Settings ==========

    /// <summary>
    /// Gets or sets the minimum health percentage before engaging combat.
    /// </summary>
    public float MinHealthPercentForCombat { get; set; } = 80f;

    /// <summary>
    /// Gets or sets the minimum mana percentage before engaging combat.
    /// </summary>
    public float MinManaPercentForCombat { get; set; } = 50f;

    /// <summary>
    /// Gets or sets the maximum number of deaths before stopping the bot.
    /// </summary>
    public int MaxDeathCount { get; set; } = 3;

    /// <summary>
    /// Gets or sets the combat timeout in seconds.
    /// </summary>
    public float CombatTimeoutSeconds { get; set; } = 30f;

    /// <summary>
    /// Gets or sets whether to chase fleeing targets.
    /// </summary>
    public bool ChaseFleeingTargets { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum chase distance.
    /// </summary>
    public float MaxChaseDistance { get; set; } = 30f;

    /// <summary>
    /// Gets or sets the maximum attack range for pulling.
    /// </summary>
    public float MaxAttackRange { get; set; } = 25f;

    // ========== Target Selection Settings ==========

    /// <summary>
    /// Gets or sets the maximum level above the player for target selection.
    /// </summary>
    public int MaxLevelAbove { get; set; } = 3;

    /// <summary>
    /// Gets or sets the maximum level below the player for target selection.
    /// </summary>
    public int MaxLevelBelow { get; set; } = 10;

    /// <summary>
    /// Gets or sets the maximum search radius for finding targets.
    /// </summary>
    public float MaxSearchRadius { get; set; } = 50f;

    /// <summary>
    /// Gets or sets the maximum distance from waypoint to engage targets.
    /// </summary>
    public float MaxWaypointDistance { get; set; } = 100f;

    /// <summary>
    /// Gets or sets whether to prioritize targets attacking the player.
    /// </summary>
    public bool PrioritizeAttackers { get; set; } = true;

    /// <summary>
    /// Gets or sets the list of blacklisted mob names (partial match).
    /// </summary>
    public List<string> BlacklistedMobNames { get; set; } = new List<string>();

    /// <summary>
    /// Gets or sets the list of blacklisted entity types.
    /// </summary>
    public List<string> BlacklistedTypes { get; set; } = new List<string>();

    // ========== Rest and Recovery Settings ==========

    /// <summary>
    /// Gets or sets the minimum health percentage to trigger rest.
    /// </summary>
    public float MinHealthPercentToRest { get; set; } = 50f;

    /// <summary>
    /// Gets or sets the minimum mana percentage to trigger rest.
    /// </summary>
    public float MinManaPercentToRest { get; set; } = 30f;

    /// <summary>
    /// Gets or sets the target health percentage to stop resting.
    /// </summary>
    public float TargetHealthPercentAfterRest { get; set; } = 90f;

    /// <summary>
    /// Gets or sets the target mana percentage to stop resting.
    /// </summary>
    public float TargetManaPercentAfterRest { get; set; } = 80f;

    /// <summary>
    /// Gets or sets the maximum rest duration in seconds.
    /// </summary>
    public float MaxRestDurationSeconds { get; set; } = 60f;

    /// <summary>
    /// Gets or sets the name of the food item to use.
    /// </summary>
    public string? FoodItem { get; set; }

    /// <summary>
    /// Gets or sets the name of the drink item to use.
    /// </summary>
    public string? DrinkItem { get; set; }

    // ========== Looting Settings ==========

    /// <summary>
    /// Gets or sets whether to auto-loot corpses.
    /// </summary>
    public bool AutoLoot { get; set; } = true;

    /// <summary>
    /// Gets or sets the distance at which to loot corpses.
    /// </summary>
    public float LootDistance { get; set; } = 2f;

    /// <summary>
    /// Gets or sets the maximum time to wait for looting in seconds.
    /// </summary>
    public float MaxLootWaitSeconds { get; set; } = 5f;

    /// <summary>
    /// Gets or sets whether to skip looting when bags are full.
    /// </summary>
    public bool SkipLootWhenFull { get; set; } = true;

    /// <summary>
    /// Gets or sets the minimum free bag slots before considering bags full.
    /// </summary>
    public int MinFreeBagSlots { get; set; } = 5;

    // ========== Navigation Settings ==========

    /// <summary>
    /// Gets or sets the stopping distance for navigation.
    /// </summary>
    public float NavigationStoppingDistance { get; set; } = 2f;

    /// <summary>
    /// Gets or sets the stuck detection threshold in seconds.
    /// </summary>
    public float StuckDetectionThresholdSeconds { get; set; } = 2f;

    /// <summary>
    /// Gets or sets the maximum unstuck attempts.
    /// </summary>
    public int MaxUnstuckAttempts { get; set; } = 3;

    /// <summary>
    /// Gets or sets the movement progress threshold for stuck detection.
    /// </summary>
    public float MovementProgressThreshold { get; set; } = 0.5f;

    /// <summary>
    /// Gets or sets the facing tolerance in degrees.
    /// </summary>
    public float FacingToleranceDegrees { get; set; } = 10f;

    // ========== Waypoint Settings ==========

    /// <summary>
    /// Gets or sets the minimum distance between recorded waypoints.
    /// </summary>
    public float MinWaypointDistance { get; set; } = 5f;

    /// <summary>
    /// Gets or sets the minimum time between waypoint recordings.
    /// </summary>
    public float MinWaypointRecordIntervalSeconds { get; set; } = 1f;

    // ========== Input Settings ==========

    /// <summary>
    /// Gets or sets the base input delay in milliseconds.
    /// </summary>
    public int InputDelayMs { get; set; } = 50;

    /// <summary>
    /// Gets or sets the input randomization range in milliseconds.
    /// </summary>
    public int InputRandomizationRangeMs { get; set; } = 25;

    // ========== Death and Resurrection Settings ==========

    /// <summary>
    /// Gets or sets whether to auto-release spirit on death.
    /// </summary>
    public bool AutoReleaseSpirit { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to auto-accept graveyard resurrection.
    /// </summary>
    public bool AutoResurrectAtGraveyard { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum time to wait for resurrection in seconds.
    /// </summary>
    public float MaxResurrectWaitSeconds { get; set; } = 30f;

    /// <summary>
    /// Gets or sets the delay after resurrection before resuming activities.
    /// </summary>
    public float PostResurrectDelaySeconds { get; set; } = 3f;

    // ========== Map Discovery Settings ==========

    /// <summary>
    /// Gets or sets whether to enable auto-mapping.
    /// </summary>
    public bool AutoMappingEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to record resource nodes.
    /// </summary>
    public bool RecordResourceNodes { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to record NPCs.
    /// </summary>
    public bool RecordNpcs { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to record mob spawns.
    /// </summary>
    public bool RecordMobSpawns { get; set; } = true;

    /// <summary>
    /// Gets or sets the deduplication radius for map discoveries.
    /// </summary>
    public float MapDeduplicationRadius { get; set; } = 5f;

    // ========== File Paths ==========

    /// <summary>
    /// Gets or sets the path to the combat profile file.
    /// </summary>
    public string CombatProfilePath { get; set; } = "profiles/default.json";

    /// <summary>
    /// Gets or sets the path to the waypoint path file.
    /// </summary>
    public string WaypointPath { get; set; } = "waypoints/default.json";

    /// <summary>
    /// Gets or sets the directory for storing map data.
    /// </summary>
    public string MapDataDirectory { get; set; } = "mapdata";

    /// <summary>
    /// Gets or sets the path to the log file.
    /// </summary>
    public string? LogFilePath { get; set; }

    // ========== Hotkeys ==========

    /// <summary>
    /// Gets or sets the hotkey for stopping the bot.
    /// </summary>
    public string EmergencyStopHotkey { get; set; } = "F12";

    /// <summary>
    /// Gets or sets the hotkey for pausing/resuming the bot.
    /// </summary>
    public string PauseResumeHotkey { get; set; } = "F11";

    // ========== Session Limits ==========

    /// <summary>
    /// Gets or sets the maximum session runtime in minutes (0 = no limit).
    /// </summary>
    public int MaxSessionRuntimeMinutes { get; set; } = 0;

    /// <summary>
    /// Gets or sets the maximum stuck time before stopping in seconds.
    /// </summary>
    public float MaxStuckTimeSeconds { get; set; } = 60f;
}

/// <summary>
/// Manages loading and saving bot configuration.
/// </summary>
public static class ConfigManager
{
    private const string DefaultConfigFileName = "config.json";
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    /// <summary>
    /// Loads configuration from a file, merging with defaults.
    /// </summary>
    /// <param name="configPath">Path to the config file. If null, uses default path.</param>
    /// <returns>The loaded configuration with defaults applied.</returns>
    public static BotConfig Load(string? configPath = null)
    {
        string path = configPath ?? DefaultConfigFileName;

        if (!File.Exists(path))
        {
            // Return default config if file doesn't exist
            return new BotConfig();
        }

        try
        {
            string json = File.ReadAllText(path);
            var config = JsonSerializer.Deserialize<BotConfig>(json, JsonOptions);

            // Merge with defaults to handle any missing properties
            return MergeWithDefaults(config ?? new BotConfig());
        }
        catch (Exception ex)
        {
            // Log error and return defaults
            Console.WriteLine($"Error loading config from {path}: {ex.Message}");
            return new BotConfig();
        }
    }

    /// <summary>
    /// Saves configuration to a file.
    /// </summary>
    /// <param name="config">The configuration to save.</param>
    /// <param name="configPath">Path to save the config file. If null, uses default path.</param>
    public static void Save(BotConfig config, string? configPath = null)
    {
        string path = configPath ?? DefaultConfigFileName;

        try
        {
            // Ensure directory exists
            string directory = Path.GetDirectoryName(path) ?? ".";
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string json = JsonSerializer.Serialize(config, JsonOptions);
            File.WriteAllText(path, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving config to {path}: {ex.Message}");
        }
    }

    /// <summary>
    /// Merges a partial configuration with defaults.
    /// </summary>
    private static BotConfig MergeWithDefaults(BotConfig config)
    {
        var defaults = new BotConfig();

        // This simple implementation returns the config as-is since
        // the property setters with default values handle missing properties
        // For a more robust implementation, we'd recursively merge nested objects

        // Ensure lists are initialized
        config.BlacklistedMobNames ??= new List<string>();
        config.BlacklistedTypes ??= new List<string>();

        return config;
    }

    /// <summary>
    /// Creates a default configuration file.
    /// </summary>
    /// <param name="configPath">Path to save the config file. If null, uses default path.</param>
    /// <returns>The created default configuration.</returns>
    public static BotConfig CreateDefault(string? configPath = null)
    {
        var config = new BotConfig();
        Save(config, configPath);
        return config;
    }
}
