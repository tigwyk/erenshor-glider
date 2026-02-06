using System;
using System.Collections.Generic;
using System.Linq;
using ErenshorGlider.GameState;
using ErenshorGlider.Waypoints;

namespace ErenshorGlider.Combat;

/// <summary>
/// Selects appropriate targets for combat based on configurable criteria.
/// </summary>
public class TargetSelector
{
    private readonly PositionTracker _positionTracker;
    private readonly HashSet<string> _blacklistedNames = new();
    private readonly HashSet<string> _blacklistedTypes = new();

    /// <summary>
    /// Gets or sets the maximum level difference for targets.
    /// Targets with level higher than player level + MaxLevelAbove are skipped.
    /// </summary>
    public int MaxLevelAbove { get; set; } = 3;

    /// <summary>
    /// Gets or sets the minimum level difference for targets.
    /// Targets with level lower than player level - MaxLevelBelow are skipped.
    /// </summary>
    public int MaxLevelBelow { get; set; } = 10;

    /// <summary>
    /// Gets or sets the maximum search radius for targets.
    /// </summary>
    public float MaxSearchRadius { get; set; } = 50f;

    /// <summary>
    /// Gets or sets the maximum distance from the waypoint path.
    /// Targets beyond this distance are skipped.
    /// </summary>
    public float MaxWaypointDistance { get; set; } = 100f;

    /// <summary>
    /// Gets or sets whether to prioritize targets attacking the player.
    /// </summary>
    public bool PrioritizeAttackers { get; set; } = true;

    /// <summary>
    /// Event raised when a target is selected.
    /// </summary>
    public event Action<EntityInfo>? OnTargetSelected;

    /// <summary>
    /// Event raised when no suitable target is found.
    /// </summary>
    public event Action? OnNoTargetFound;

    /// <summary>
    /// Creates a new TargetSelector.
    /// </summary>
    public TargetSelector(PositionTracker positionTracker)
    {
        _positionTracker = positionTracker ?? throw new ArgumentNullException(nameof(positionTracker));
    }

    /// <summary>
    /// Adds a mob name to the blacklist.
    /// </summary>
    public void BlacklistName(string name)
    {
        if (!string.IsNullOrWhiteSpace(name))
            _blacklistedNames.Add(name);
    }

    /// <summary>
    /// Removes a mob name from the blacklist.
    /// </summary>
    public void UnblacklistName(string name)
    {
        _blacklistedNames.Remove(name);
    }

    /// <summary>
    /// Adds a mob type to the blacklist.
    /// </summary>
    public void BlacklistType(string type)
    {
        if (!string.IsNullOrWhiteSpace(type))
            _blacklistedTypes.Add(type);
    }

    /// <summary>
    /// Clears all blacklists.
    /// </summary>
    public void ClearBlacklists()
    {
        _blacklistedNames.Clear();
        _blacklistedTypes.Clear();
    }

    /// <summary>
    /// Finds the best target based on current criteria.
    /// </summary>
    /// <returns>The best target, or null if no suitable target found.</returns>
    public EntityInfo? FindBestTarget()
    {
        var nearbyEntities = _positionTracker.CurrentNearbyEntities;
        if (nearbyEntities == null || nearbyEntities.Count == 0)
        {
            OnNoTargetFound?.Invoke();
            return null;
        }

        var playerVitals = _positionTracker.CurrentVitals;
        int playerLevel = playerVitals?.Level ?? 1;

        // Filter candidates based on criteria
        var candidates = nearbyEntities
            .Where(e => IsCandidateValid(e, playerLevel))
            .ToList();

        if (candidates.Count == 0)
        {
            OnNoTargetFound?.Invoke();
            return null;
        }

        // Sort candidates by priority
        candidates.Sort((a, b) => CompareTargetPriority(a, b));

        var selected = candidates[0];
        OnTargetSelected?.Invoke(selected);
        return selected;
    }

    /// <summary>
    /// Finds the best target within a specific radius.
    /// </summary>
    public EntityInfo? FindBestTarget(float radius)
    {
        float originalRadius = MaxSearchRadius;
        MaxSearchRadius = radius;
        try
        {
            return FindBestTarget();
        }
        finally
        {
            MaxSearchRadius = originalRadius;
        }
    }

    /// <summary>
    /// Checks if a target candidate is valid based on filtering criteria.
    /// </summary>
    private bool IsCandidateValid(EntityInfo entity, int playerLevel)
    {
        // Must be a hostile mob that can be attacked
        if (!entity.CanBeAttacked)
            return false;

        // Skip blacklisted names
        if (_blacklistedNames.Count > 0)
        {
            foreach (var blacklisted in _blacklistedNames)
            {
                if (entity.Name.IndexOf(blacklisted, StringComparison.OrdinalIgnoreCase) >= 0)
                    return false;
            }
        }

        // Skip blacklisted types
        if (_blacklistedTypes.Count > 0)
        {
            foreach (var blacklisted in _blacklistedTypes)
            {
                if (entity.Name.IndexOf(blacklisted, StringComparison.OrdinalIgnoreCase) >= 0)
                    return false;
            }
        }

        // Check level range
        int levelDiff = entity.Level - playerLevel;
        if (levelDiff > MaxLevelAbove || levelDiff < -MaxLevelBelow)
            return false;

        // Check search radius
        if (entity.Distance > MaxSearchRadius)
            return false;

        // Check waypoint distance (if playing a path)
        // TODO: Implement waypoint path distance checking

        return true;
    }

    /// <summary>
    /// Compares two targets to determine which has higher priority.
    /// Returns negative if a has higher priority, positive if b has higher priority.
    /// </summary>
    private int CompareTargetPriority(EntityInfo a, EntityInfo b)
    {
        // Priority 1: Targets attacking the player
        if (PrioritizeAttackers)
        {
            // TODO: Check if target is attacking player
            // For now, use distance as proxy
        }

        // Priority 2: Distance (closer is better)
        int distanceComparison = a.Distance.CompareTo(b.Distance);
        if (distanceComparison != 0)
            return distanceComparison;

        // Priority 3: Lower level first (easier kills)
        int levelComparison = a.Level.CompareTo(b.Level);
        if (levelComparison != 0)
            return levelComparison;

        // Priority 4: Health percentage (lower is better - already fighting)
        return a.HealthPercent.CompareTo(b.HealthPercent);
    }

    /// <summary>
    /// Checks if there are any valid targets nearby.
    /// </summary>
    public bool HasValidTargets()
    {
        return FindBestTarget() != null;
    }

    /// <summary>
    /// Gets all valid targets in range.
    /// </summary>
    public List<EntityInfo> GetValidTargets()
    {
        var nearbyEntities = _positionTracker.CurrentNearbyEntities;
        if (nearbyEntities == null)
            return new List<EntityInfo>();

        var playerVitals = _positionTracker.CurrentVitals;
        int playerLevel = playerVitals?.Level ?? 1;

        return nearbyEntities
            .Where(e => IsCandidateValid(e, playerLevel))
            .OrderBy(e => e.Distance)
            .ToList();
    }

    /// <summary>
    /// Gets the number of valid targets in range.
    /// </summary>
    public int GetValidTargetCount()
    {
        return GetValidTargets().Count;
    }
}
