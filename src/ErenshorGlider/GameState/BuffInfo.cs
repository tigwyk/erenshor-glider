using System;

namespace ErenshorGlider.GameState;

/// <summary>
/// Represents information about an active buff or debuff.
/// </summary>
public readonly struct BuffInfo : IEquatable<BuffInfo>
{
    /// <summary>
    /// The buff/debuff display name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The unique ID for this buff type.
    /// </summary>
    public string? BuffId { get; }

    /// <summary>
    /// The remaining duration in seconds.
    /// </summary>
    public float RemainingDuration { get; }

    /// <summary>
    /// The total/max duration of this buff (for calculating percentage).
    /// </summary>
    public float MaxDuration { get; }

    /// <summary>
    /// The number of stacks (if applicable).
    /// </summary>
    public int Stacks { get; }

    /// <summary>
    /// The icon index for UI display.
    /// </summary>
    public int IconIndex { get; }

    /// <summary>
    /// Whether this is a debuff (harmful effect) rather than a buff.
    /// </summary>
    public bool IsDebuff { get; }

    /// <summary>
    /// Creates a new BuffInfo instance.
    /// </summary>
    public BuffInfo(
        string name,
        string? buffId,
        float remainingDuration,
        float maxDuration,
        int stacks = 1,
        int iconIndex = 0,
        bool isDebuff = false)
    {
        Name = name ?? string.Empty;
        BuffId = buffId;
        RemainingDuration = Math.Max(0f, remainingDuration);
        MaxDuration = Math.Max(0f, maxDuration);
        Stacks = Math.Max(1, stacks);
        IconIndex = iconIndex;
        IsDebuff = isDebuff;
    }

    /// <summary>
    /// The percentage of duration remaining (0-100).
    /// Returns 0 if MaxDuration is 0.
    /// </summary>
    public float DurationPercent => MaxDuration > 0 ? (RemainingDuration / MaxDuration) * 100f : 0f;

    /// <summary>
    /// Returns true if the buff has expired (no duration remaining).
    /// </summary>
    public bool IsExpired => RemainingDuration <= 0f;

    /// <summary>
    /// Returns true if duration is below the given threshold in seconds.
    /// </summary>
    public bool IsDurationBelow(float seconds) => RemainingDuration < seconds;

    /// <summary>
    /// Returns true if duration is above the given threshold in seconds.
    /// </summary>
    public bool IsDurationAbove(float seconds) => RemainingDuration > seconds;

    public bool Equals(BuffInfo other)
    {
        return Name == other.Name &&
               BuffId == other.BuffId &&
               Math.Abs(RemainingDuration - other.RemainingDuration) < 0.1f &&
               Math.Abs(MaxDuration - other.MaxDuration) < 0.1f &&
               Stacks == other.Stacks &&
               IsDebuff == other.IsDebuff;
    }

    public override bool Equals(object? obj)
    {
        return obj is BuffInfo other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Name, BuffId, RemainingDuration, Stacks, IsDebuff);
    }

    public override string ToString()
    {
        string prefix = IsDebuff ? "[Debuff] " : "[Buff] ";
        string duration = MaxDuration > 0 ? $"{RemainingDuration:F1}s / {MaxDuration:F1}s" : "Permanent";
        string stacks = Stacks > 1 ? $" x{Stacks}" : "";
        return $"{prefix}{Name}{stacks} ({duration})";
    }
}

/// <summary>
/// Represents the collection of active buffs and debuffs on a character.
/// </summary>
public readonly struct BuffState
{
    /// <summary>
    /// List of active buffs (beneficial effects).
    /// </summary>
    public IReadOnlyList<BuffInfo> Buffs { get; }

    /// <summary>
    /// List of active debuffs (harmful effects).
    /// </summary>
    public IReadOnlyList<BuffInfo> Debuffs { get; }

    /// <summary>
    /// All effects combined (buffs + debuffs).
    /// </summary>
    public IReadOnlyList<BuffInfo> AllEffects { get; }

    /// <summary>
    /// Creates a new BuffState instance.
    /// </summary>
    public BuffState(
        IReadOnlyList<BuffInfo> buffs,
        IReadOnlyList<BuffInfo> debuffs)
    {
        Buffs = buffs ?? Array.Empty<BuffInfo>();
        Debuffs = debuffs ?? Array.Empty<BuffInfo>();

        // Combine for easier iteration
        var all = new BuffInfo[Buffs.Count + Debuffs.Count];
        Buffs.CopyTo(all, 0);
        Debuffs.CopyTo(all, Buffs.Count);
        AllEffects = all;
    }

    /// <summary>
    /// Returns true if the player has a specific buff (case-insensitive name match).
    /// </summary>
    public bool HasBuff(string buffName)
    {
        if (string.IsNullOrEmpty(buffName))
            return false;

        foreach (var buff in Buffs)
        {
            if (buff.Name.IndexOf(buffName, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Returns true if the player has a specific debuff (case-insensitive name match).
    /// </summary>
    public bool HasDebuff(string debuffName)
    {
        if (string.IsNullOrEmpty(debuffName))
            return false;

        foreach (var debuff in Debuffs)
        {
            if (debuff.Name.IndexOf(debuffName, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Gets a buff by name (case-insensitive partial match).
    /// Returns null if not found.
    /// </summary>
    public BuffInfo? GetBuff(string buffName)
    {
        if (string.IsNullOrEmpty(buffName))
            return null;

        foreach (var buff in Buffs)
        {
            if (buff.Name.IndexOf(buffName, StringComparison.OrdinalIgnoreCase) >= 0)
                return buff;
        }
        return null;
    }

    /// <summary>
    /// Gets a debuff by name (case-insensitive partial match).
    /// Returns null if not found.
    /// </summary>
    public BuffInfo? GetDebuff(string debuffName)
    {
        if (string.IsNullOrEmpty(debuffName))
            return null;

        foreach (var debuff in Debuffs)
        {
            if (debuff.Name.IndexOf(debuffName, StringComparison.OrdinalIgnoreCase) >= 0)
                return debuff;
        }
        return null;
    }

    /// <summary>
    /// Returns true if any buff is about to expire (below threshold).
    /// </summary>
    public bool HasBuffExpiringSoon(float thresholdSeconds = 5f)
    {
        foreach (var buff in Buffs)
        {
            if (buff.MaxDuration > 0 && buff.RemainingDuration < thresholdSeconds)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Returns true if any debuff is about to expire (below threshold).
    /// </summary>
    public bool HasDebuffExpiringSoon(float thresholdSeconds = 5f)
    {
        foreach (var debuff in Debuffs)
        {
            if (debuff.MaxDuration > 0 && debuff.RemainingDuration < thresholdSeconds)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Counts the number of active buffs.
    /// </summary>
    public int BuffCount => Buffs.Count;

    /// <summary>
    /// Counts the number of active debuffs.
    /// </summary>
    public int DebuffCount => Debuffs.Count;

    /// <summary>
    /// An empty buff state (no buffs or debuffs).
    /// </summary>
    public static BuffState Empty => new(Array.Empty<BuffInfo>(), Array.Empty<BuffInfo>());

    public override string ToString()
    {
        return $"BuffState: {BuffCount} buffs, {DebuffCount} debuffs";
    }
}
