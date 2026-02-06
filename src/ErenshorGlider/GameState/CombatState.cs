namespace ErenshorGlider.GameState;

/// <summary>
/// Represents the player's combat-related state.
/// </summary>
public readonly struct CombatState
{
    /// <summary>
    /// Whether the player is currently in combat.
    /// </summary>
    public bool InCombat { get; }

    /// <summary>
    /// Whether the player is currently casting a spell/ability.
    /// </summary>
    public bool IsCasting { get; }

    /// <summary>
    /// Whether the player is alive (not dead).
    /// </summary>
    public bool IsAlive { get; }

    /// <summary>
    /// Whether the player is dead.
    /// </summary>
    public bool IsDead => !IsAlive;

    /// <summary>
    /// Creates a new CombatState instance.
    /// </summary>
    public CombatState(bool inCombat, bool isCasting, bool isAlive)
    {
        InCombat = inCombat;
        IsCasting = isCasting;
        IsAlive = isAlive;
    }

    /// <summary>
    /// Returns true if the player can take actions (alive, not casting).
    /// </summary>
    public bool CanAct => IsAlive && !IsCasting;

    /// <summary>
    /// Returns true if the player is idle (alive, not in combat, not casting).
    /// </summary>
    public bool IsIdle => IsAlive && !InCombat && !IsCasting;

    public override string ToString() =>
        $"Combat: {(InCombat ? "Yes" : "No")}, " +
        $"Casting: {(IsCasting ? "Yes" : "No")}, " +
        $"Alive: {(IsAlive ? "Yes" : "No")}";
}
