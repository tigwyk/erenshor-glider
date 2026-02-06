using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using ErenshorGlider.GameState;

namespace ErenshorGlider.Combat;

/// <summary>
/// Represents an ability in a combat rotation.
/// </summary>
public class Ability
{
    /// <summary>
    /// Gets or sets the unique ID for this ability.
    /// </summary>
    [JsonRequired]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name of this ability.
    /// </summary>
    [JsonRequired]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the keybind for this ability (1-9, F1-F12, etc.).
    /// </summary>
    public string? Keybind { get; set; }

    /// <summary>
    /// Gets or sets the cooldown of this ability in seconds.
    /// </summary>
    public float Cooldown { get; set; }

    /// <summary>
    /// Gets or sets the global cooldown (GCD) for this ability.
    /// If true, this ability triggers the GCD.
    /// </summary>
    public bool TriggersGcd { get; set; } = true;

    /// <summary>
    /// Gets or sets the mana cost of this ability.
    /// </summary>
    public float ManaCost { get; set; }

    /// <summary>
    /// Gets or sets the range of this ability.
    /// </summary>
    public float Range { get; set; }

    /// <summary>
    /// Gets or sets whether this ability requires a target.
    /// </summary>
    public bool RequiresTarget { get; set; } = true;

    /// <summary>
    /// Gets or sets whether this is a channeled ability.
    /// </summary>
    public bool IsChanneled { get; set; }

    /// <summary>
    /// Gets or sets the cast time of this ability (if not instant).
    /// </summary>
    public float CastTime { get; set; }

    /// <summary>
    /// Gets or sets additional metadata for this ability.
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();

    public override string ToString() => $"{Name} ({Id})";
}

/// <summary>
/// Represents a condition that must be met for an ability to be used.
/// </summary>
public class Condition
{
    /// <summary>
    /// Gets or sets the type of condition.
    /// </summary>
    [JsonRequired]
    public ConditionType Type { get; set; }

    /// <summary>
    /// Gets or sets the value to compare against (interpretation depends on type).
    /// </summary>
    public float Value { get; set; }

    /// <summary>
    /// Gets or sets the comparison operator.
    /// </summary>
    public ComparisonOperator Operator { get; set; } = ComparisonOperator.GreaterThanOrEqual;

    /// <summary>
    /// Gets or sets the buff/debuff name to check (for buff-related conditions).
    /// </summary>
    public string? BuffName { get; set; }

    /// <summary>
    /// Gets or sets whether to check the target's buffs instead of player's.
    /// </summary>
    public bool CheckTarget { get; set; }

    /// <summary>
    /// Evaluates this condition against the current game state.
    /// </summary>
    public bool Evaluate(CombatState combatState, PlayerVitals vitals, BuffState playerBuffs, BuffState? targetBuffs, TargetInfo? target)
    {
        return Type switch
        {
            ConditionType.PlayerHealthPercent => Compare(vitals.HealthPercent, Value, Operator),
            ConditionType.PlayerManaPercent => Compare(vitals.ManaPercent, Value, Operator),
            ConditionType.PlayerLevel => Compare(vitals.Level, Value, Operator),
            ConditionType.TargetHealthPercent => target.HasValue && target.Value.HasTarget && Compare(target.Value.HealthPercent, Value, Operator),
            ConditionType.TargetExists => target.HasValue && target.Value.HasTarget,
            ConditionType.TargetIsHostile => target.HasValue && target.Value.IsHostile,
            ConditionType.TargetIsDead => target.HasValue && target.Value.IsDead,
            ConditionType.InCombat => combatState.InCombat,
            ConditionType.NotInCombat => !combatState.InCombat,
            ConditionType.BuffPresent => CheckTarget
                ? (targetBuffs?.HasBuff(BuffName ?? "") ?? false)
                : playerBuffs.HasBuff(BuffName ?? ""),
            ConditionType.BuffAbsent => CheckTarget
                ? !(targetBuffs?.HasBuff(BuffName ?? "") ?? false)
                : !playerBuffs.HasBuff(BuffName ?? ""),
            ConditionType.BuffStackCount => EvaluateBuffStackCount(CheckTarget ? targetBuffs : playerBuffs, BuffName ?? "", Value, Operator),
            ConditionType.CanAct => combatState.CanAct,
            _ => true
        };
    }

    /// <summary>
    /// Evaluates a buff stack count condition.
    /// </summary>
    private static bool EvaluateBuffStackCount(BuffState? buffState, string buffName, float value, ComparisonOperator op)
    {
        if (buffState == null || string.IsNullOrEmpty(buffName))
            return false;

        int stackCount = buffState.Value.GetBuffStacks(buffName);
        return Compare(stackCount, value, op);
    }

    private static bool Compare(float actual, float expected, ComparisonOperator op)
    {
        return op switch
        {
            ComparisonOperator.Equal => Math.Abs(actual - expected) < 0.01f,
            ComparisonOperator.NotEqual => Math.Abs(actual - expected) >= 0.01f,
            ComparisonOperator.GreaterThan => actual > expected,
            ComparisonOperator.GreaterThanOrEqual => actual >= expected,
            ComparisonOperator.LessThan => actual < expected,
            ComparisonOperator.LessThanOrEqual => actual <= expected,
            _ => false
        };
    }
}

/// <summary>
/// The type of condition to check.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ConditionType
{
    /// <summary>Player's health percentage.</summary>
    PlayerHealthPercent,
    /// <summary>Player's mana percentage.</summary>
    PlayerManaPercent,
    /// <summary>Player's level.</summary>
    PlayerLevel,
    /// <summary>Target's health percentage.</summary>
    TargetHealthPercent,
    /// <summary>Whether a target exists.</summary>
    TargetExists,
    /// <summary>Whether target is hostile.</summary>
    TargetIsHostile,
    /// <summary>Whether target is dead.</summary>
    TargetIsDead,
    /// <summary>Whether in combat.</summary>
    InCombat,
    /// <summary>Whether not in combat.</summary>
    NotInCombat,
    /// <summary>Whether a specific buff is present.</summary>
    BuffPresent,
    /// <summary>Whether a specific buff is absent.</summary>
    BuffAbsent,
    /// <summary>Buff stack count.</summary>
    BuffStackCount,
    /// <summary>Whether player can act (not casting, not dead).</summary>
    CanAct
}

/// <summary>
/// Comparison operators for conditions.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ComparisonOperator
{
    Equal,
    NotEqual,
    GreaterThan,
    GreaterThanOrEqual,
    LessThan,
    LessThanOrEqual
}

/// <summary>
/// Represents a single entry in a combat rotation.
/// </summary>
public class RotationEntry
{
    /// <summary>
    /// Gets or sets the ability to use.
    /// </summary>
    [JsonRequired]
    public string AbilityId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the conditions that must be met to use this ability.
    /// All conditions must be true (AND logic).
    /// </summary>
    public List<Condition> Conditions { get; set; } = new();

    /// <summary>
    /// Gets or sets the priority of this entry.
    /// Lower numbers = higher priority (evaluated first).
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Gets or sets whether this entry is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets a description of when to use this ability.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Evaluates whether all conditions for this entry are met.
    /// </summary>
    public bool CanExecute(CombatState combatState, PlayerVitals vitals, BuffState playerBuffs, BuffState? targetBuffs, TargetInfo? target)
    {
        if (!Enabled)
            return false;

        foreach (var condition in Conditions)
        {
            if (!condition.Evaluate(combatState, vitals, playerBuffs, targetBuffs, target))
                return false;
        }

        return true;
    }
}

/// <summary>
/// Represents a complete combat profile/rotation.
/// </summary>
public class CombatProfile
{
    /// <summary>
    /// Gets or sets the name of this profile.
    /// </summary>
    [JsonRequired]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the character class this profile is for.
    /// </summary>
    public string? CharacterClass { get; set; }

    /// <summary>
    /// Gets or sets the description of this profile.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the abilities available in this profile.
    /// </summary>
    public Dictionary<string, Ability> Abilities { get; set; } = new();

    /// <summary>
    /// Gets or sets the rotation entries (priority-sorted).
    /// </summary>
    [JsonRequired]
    public List<RotationEntry> Rotation { get; set; } = new();

    /// <summary>
    /// Gets or sets the global cooldown (GCD) in seconds.
    /// </summary>
    public float GlobalCooldown { get; set; } = 1.5f;

    /// <summary>
    /// Gets or sets whether to use auto-attack between abilities.
    /// </summary>
    public bool UseAutoAttack { get; set; } = true;

    /// <summary>
    /// Gets or sets the minimum health percentage to start combat.
    /// </summary>
    public float MinHealthPercent { get; set; } = 50f;

    /// <summary>
    /// Gets or sets the minimum mana percentage to start combat.
    /// </summary>
    public float MinManaPercent { get; set; } = 20f;

    /// <summary>
    /// Gets or sets additional metadata for this profile.
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();

    /// <summary>
    /// Gets or sets when this profile was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets when this profile was last modified.
    /// </summary>
    public DateTime LastModified { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the sorted rotation entries by priority.
    /// </summary>
    public List<RotationEntry> SortedRotation
    {
        get
        {
            var sorted = new List<RotationEntry>(Rotation);
            sorted.Sort((a, b) => a.Priority.CompareTo(b.Priority));
            return sorted;
        }
    }

    /// <summary>
    /// Gets an ability by ID.
    /// </summary>
    public Ability? GetAbility(string abilityId)
    {
        return Abilities.TryGetValue(abilityId, out var ability) ? ability : null;
    }

    /// <summary>
    /// Saves this profile to a JSON file.
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
    /// Loads a profile from a JSON file.
    /// </summary>
    public static CombatProfile? LoadFromFile(string filePath)
    {
        if (!File.Exists(filePath))
            return null;

        string json = File.ReadAllText(filePath);
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            AllowTrailingCommas = true
        };
        return JsonSerializer.Deserialize<CombatProfile>(json, options);
    }

    /// <summary>
    /// Validates the profile and returns any errors.
    /// </summary>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Name))
            errors.Add("Profile name is required.");

        if (Rotation.Count == 0)
            errors.Add("Profile must have at least one rotation entry.");

        foreach (var entry in Rotation)
        {
            if (string.IsNullOrWhiteSpace(entry.AbilityId))
                errors.Add($"Rotation entry has empty ability ID.");

            if (!Abilities.ContainsKey(entry.AbilityId))
                errors.Add($"Rotation entry references unknown ability: {entry.AbilityId}");
        }

        return errors;
    }

    public override string ToString() => $"{Name} ({CharacterClass ?? "Any Class"})";
}

/// <summary>
/// Manages saving and loading combat profile files.
/// </summary>
public static class CombatProfileFileManager
{
    /// <summary>
    /// The default directory for profile files.
    /// </summary>
    public static string DefaultProfileDirectory => Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory,
        "profiles"
    );

    /// <summary>
    /// Gets all profile files in the default directory.
    /// </summary>
    public static string[] GetProfileFiles()
    {
        return GetProfileFiles(DefaultProfileDirectory);
    }

    /// <summary>
    /// Gets all profile files in the specified directory.
    /// </summary>
    public static string[] GetProfileFiles(string directory)
    {
        if (!Directory.Exists(directory))
            return Array.Empty<string>();

        return Directory.GetFiles(directory, "*.json");
    }

    /// <summary>
    /// Saves a profile to the default directory.
    /// </summary>
    public static void SaveProfile(CombatProfile profile, string? fileName = null)
    {
        Directory.CreateDirectory(DefaultProfileDirectory);

        fileName ??= SanitizeFileName(profile.Name) + ".json";
        string filePath = Path.Combine(DefaultProfileDirectory, fileName);

        profile.SaveToFile(filePath);
    }

    /// <summary>
    /// Loads a profile by name from the default directory.
    /// </summary>
    public static CombatProfile? LoadProfile(string name)
    {
        string fileName = SanitizeFileName(name) + ".json";
        string filePath = Path.Combine(DefaultProfileDirectory, fileName);

        return CombatProfile.LoadFromFile(filePath);
    }

    /// <summary>
    /// Loads all profiles from the default directory.
    /// </summary>
    public static List<CombatProfile> LoadAllProfiles()
    {
        return LoadAllProfiles(DefaultProfileDirectory);
    }

    /// <summary>
    /// Loads all profiles from the specified directory.
    /// </summary>
    public static List<CombatProfile> LoadAllProfiles(string directory)
    {
        var profiles = new List<CombatProfile>();

        if (!Directory.Exists(directory))
            return profiles;

        foreach (var file in GetProfileFiles(directory))
        {
            try
            {
                var profile = CombatProfile.LoadFromFile(file);
                if (profile != null)
                    profiles.Add(profile);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading profile file {file}: {ex.Message}");
            }
        }

        return profiles;
    }

    /// <summary>
    /// Sanitizes a filename by removing invalid characters.
    /// </summary>
    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(fileName.Where(c => !invalidChars.Contains(c)).ToArray());
        return string.IsNullOrWhiteSpace(sanitized) ? "profile" : sanitized;
    }
}
