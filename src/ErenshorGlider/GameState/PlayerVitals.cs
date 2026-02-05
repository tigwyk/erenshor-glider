namespace ErenshorGlider.GameState;

/// <summary>
/// Represents the player's vital statistics (health, mana, level, XP).
/// </summary>
public readonly struct PlayerVitals
{
    /// <summary>
    /// Current health points.
    /// </summary>
    public float CurrentHealth { get; }

    /// <summary>
    /// Maximum health points.
    /// </summary>
    public float MaxHealth { get; }

    /// <summary>
    /// Current mana points.
    /// </summary>
    public float CurrentMana { get; }

    /// <summary>
    /// Maximum mana points.
    /// </summary>
    public float MaxMana { get; }

    /// <summary>
    /// Player level.
    /// </summary>
    public int Level { get; }

    /// <summary>
    /// Current experience points.
    /// </summary>
    public float CurrentXP { get; }

    /// <summary>
    /// Experience points required for next level.
    /// </summary>
    public float XPToLevel { get; }

    /// <summary>
    /// Creates a new PlayerVitals instance.
    /// </summary>
    public PlayerVitals(
        float currentHealth,
        float maxHealth,
        float currentMana,
        float maxMana,
        int level,
        float currentXP,
        float xpToLevel)
    {
        CurrentHealth = currentHealth;
        MaxHealth = maxHealth;
        CurrentMana = currentMana;
        MaxMana = maxMana;
        Level = level;
        CurrentXP = currentXP;
        XPToLevel = xpToLevel;
    }

    /// <summary>
    /// Gets the health percentage (0-100).
    /// </summary>
    public float HealthPercent => MaxHealth > 0 ? (CurrentHealth / MaxHealth) * 100f : 0f;

    /// <summary>
    /// Gets the mana percentage (0-100).
    /// </summary>
    public float ManaPercent => MaxMana > 0 ? (CurrentMana / MaxMana) * 100f : 0f;

    /// <summary>
    /// Gets the XP progress percentage toward next level (0-100).
    /// </summary>
    public float XPPercent => XPToLevel > 0 ? (CurrentXP / XPToLevel) * 100f : 0f;

    /// <summary>
    /// Returns true if health is at or below the given percentage threshold.
    /// </summary>
    public bool IsHealthBelowPercent(float percent) => HealthPercent <= percent;

    /// <summary>
    /// Returns true if mana is at or below the given percentage threshold.
    /// </summary>
    public bool IsManaBelowPercent(float percent) => ManaPercent <= percent;

    public override string ToString() =>
        $"HP: {CurrentHealth:F0}/{MaxHealth:F0} ({HealthPercent:F0}%), " +
        $"MP: {CurrentMana:F0}/{MaxMana:F0} ({ManaPercent:F0}%), " +
        $"Lv: {Level}, XP: {CurrentXP:F0}/{XPToLevel:F0} ({XPPercent:F1}%)";
}
