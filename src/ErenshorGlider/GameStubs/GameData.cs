// ReSharper disable UnusedMember.Global
// ReSharper disable InconsistentNaming
// ReSharper disable UnassignedField.Global
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value

using UnityEngine;

namespace ErenshorGlider.GameStubs;

/// <summary>
/// Stub for Erenshor's GameData singleton.
/// This is replaced at runtime by the actual game class.
/// When building with the game's Assembly-CSharp.dll reference, use the real type instead.
/// </summary>
/// <remarks>
/// To use the real game types:
/// 1. Add a reference to Erenshor's Assembly-CSharp.dll in the csproj
/// 2. Delete or exclude this stub file
/// 3. Change using directives from ErenshorGlider.GameStubs to the actual namespace
/// </remarks>
public static class GameData
{
    /// <summary>
    /// The player control component (contains transform for position).
    /// </summary>
    public static PlayerControl PlayerControl;
}

/// <summary>
/// Stub for Erenshor's PlayerControl class.
/// </summary>
public class PlayerControl : MonoBehaviour
{
    /// <summary>
    /// The player's Character component.
    /// </summary>
    public Character Myself;

    /// <summary>
    /// The currently targeted Character.
    /// </summary>
    public Character CurrentTarget;
}

/// <summary>
/// Stub for Erenshor's Character class.
/// </summary>
public class Character : MonoBehaviour
{
    /// <summary>
    /// The character's stats component.
    /// </summary>
    public CharacterStats MyStats;

    /// <summary>
    /// The character's faction.
    /// </summary>
    public Faction MyFaction;
}

/// <summary>
/// Stub for Erenshor's CharacterStats class.
/// </summary>
public class CharacterStats : MonoBehaviour
{
    /// <summary>
    /// Current health points.
    /// </summary>
    public float CurrentHP;

    /// <summary>
    /// Maximum health points.
    /// </summary>
    public float MaxHP;

    /// <summary>
    /// Current mana points.
    /// </summary>
    public float CurrentMP;

    /// <summary>
    /// Maximum mana points.
    /// </summary>
    public float MaxMP;
}

/// <summary>
/// Stub for Erenshor's Faction enum.
/// </summary>
public enum Faction
{
    Player,
    Enemy,
    Neutral,
    Friendly
}
