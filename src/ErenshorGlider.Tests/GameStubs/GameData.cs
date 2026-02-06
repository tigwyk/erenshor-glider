// ReSharper disable UnusedMember.Global
// ReSharper disable InconsistentNaming
// ReSharper disable UnassignedField.Global
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value

using UnityEngine;

namespace ErenshorGlider.Tests.GameStubs;

/// <summary>
/// Stub implementations for Erenshor game types.
/// These are used in unit tests to run without the actual game assemblies.
///
/// Note: In the main ErenshorGlider project, these stubs are conditionally compiled
/// with #if !USE_REAL_GAME_TYPES. In this test project, we always use these stubs
/// since we never have access to the real game assemblies.
///
/// The stub classes should match the signatures of the real game types exactly.
/// </summary>
public static class GameData
{
    /// <summary>
    /// The player control component (contains transform for position).
    /// </summary>
    public static PlayerControl PlayerControl;

    /// <summary>
    /// The player combat controller.
    /// </summary>
    public static PlayerCombat PlayerCombat;

    /// <summary>
    /// The player inventory controller.
    /// </summary>
    public static PlayerInventory PlayerInv;
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

    /// <summary>
    /// The player's spell casting component.
    /// </summary>
    public CastSpell PlayerSpells;
}

/// <summary>
/// Stub for Erenshor's Character class.
/// </summary>
public class Character : MonoBehaviour
{
    /// <summary>
    /// The character's display name.
    /// </summary>
    public string CharacterName;

    /// <summary>
    /// The character's stats component.
    /// </summary>
    public CharacterStats MyStats;

    /// <summary>
    /// The character's faction.
    /// </summary>
    public Faction MyFaction;

    /// <summary>
    /// Whether the character is dead.
    /// </summary>
    public bool Dead;

    /// <summary>
    /// The character's buffs/debuffs component.
    /// </summary>
    public CharacterBuffs MyBuffs;
}

/// <summary>
/// Stub for Erenshor's PlayerCombat class.
/// Handles player combat state and auto-attack.
/// </summary>
public class PlayerCombat : MonoBehaviour
{
    /// <summary>
    /// Whether the player is currently in combat mode (auto-attack enabled).
    /// </summary>
    public bool InCombat;

    /// <summary>
    /// Toggles auto-attack on/off.
    /// </summary>
    public void ToggleAttack() { }

    /// <summary>
    /// Forces auto-attack on.
    /// </summary>
    public void ForceAttackOn() { }
}

/// <summary>
/// Stub for Erenshor's CastSpell component.
/// Handles spell casting for characters.
/// </summary>
public class CastSpell : MonoBehaviour
{
    /// <summary>
    /// Whether this character is currently casting a spell.
    /// </summary>
    public bool Casting;

    /// <summary>
    /// The parent Character reference.
    /// </summary>
    public Character MyChar;

    /// <summary>
    /// Whether this is the player's CastSpell component.
    /// </summary>
    public bool isPlayer;
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

    /// <summary>
    /// Character level.
    /// </summary>
    public int Level;

    /// <summary>
    /// Current experience points.
    /// </summary>
    public float CurrentXP;

    /// <summary>
    /// Experience points required for next level.
    /// </summary>
    public float XPToLevel;
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

/// <summary>
/// Stub for Erenshor's NPC class.
/// NPCs are non-player characters that can be interacted with.
/// </summary>
public class NPC : Character
{
    /// <summary>
    /// Whether this NPC is a vendor.
    /// </summary>
    public bool IsVendor;

    /// <summary>
    /// Whether this NPC offers quests.
    /// </summary>
    public bool HasQuests;

    /// <summary>
    /// Called when the NPC aggros on a target.
    /// </summary>
    public void AggroOn() { }
}

/// <summary>
/// Stub for Erenshor's SimPlayer class.
/// SimPlayers are AI-controlled party members that simulate other players.
/// </summary>
public class SimPlayer : Character
{
    /// <summary>
    /// The class of this sim player.
    /// </summary>
    public string CharacterClass;
}

/// <summary>
/// Stub for a resource node (ore, herbs, etc.).
/// In Erenshor, these may be represented differently - adjust as needed.
/// </summary>
public class ResourceNode : MonoBehaviour
{
    /// <summary>
    /// The name/type of this resource.
    /// </summary>
    public string NodeName;

    /// <summary>
    /// Whether this node can currently be gathered.
    /// </summary>
    public bool CanGather;

    /// <summary>
    /// The skill required to gather this node.
    /// </summary>
    public string RequiredSkill;
}

/// <summary>
/// Stub for a lootable corpse.
/// </summary>
public class LootableCorpse : MonoBehaviour
{
    /// <summary>
    /// The name of the creature this corpse belongs to.
    /// </summary>
    public string CorpseName;

    /// <summary>
    /// Whether this corpse has been looted.
    /// </summary>
    public bool IsLooted;

    /// <summary>
    /// The original character this corpse came from.
    /// </summary>
    public Character OriginalCharacter;
}

/// <summary>
/// Stub for Erenshor's PlayerInventory class.
/// Handles the player's bag/inventory system.
/// </summary>
public class PlayerInventory : MonoBehaviour
{
    /// <summary>
    /// Array of all inventory slots.
    /// </summary>
    public InventorySlot[] ALLSLOTS;

    /// <summary>
    /// Empty item reference for comparison.
    /// </summary>
    public static Item Empty;

    /// <summary>
    /// The total number of inventory slots available.
    /// </summary>
    public int SlotCount;
}

/// <summary>
/// Stub for an inventory slot.
/// Represents a single slot in the player's bags.
/// </summary>
public class InventorySlot : MonoBehaviour
{
    /// <summary>
    /// The item in this slot (null if empty).
    /// </summary>
    public Item Item;

    /// <summary>
    /// The quantity/stack size of the item.
    /// </summary>
    public int Quantity;

    /// <summary>
    /// The slot index.
    /// </summary>
    public int SlotIndex;
}

/// <summary>
/// Stub for Erenshor's Item class.
/// Represents an item in the game.
/// </summary>
public class Item : MonoBehaviour
{
    /// <summary>
    /// The item's display name.
    /// </summary>
    public string ItemName;

    /// <summary>
    /// The item's unique ID.
    /// </summary>
    public string ItemId;

    /// <summary>
    /// The item's quality/rarity.
    /// </summary>
    public ItemQuality Quality;

    /// <summary>
    /// The maximum stack size for this item.
    /// </summary>
    public int MaxStackSize;

    /// <summary>
    /// Whether this item is a junk item (poor quality).
    /// </summary>
    public bool IsJunk;

    /// <summary>
    /// The item's icon/sprite for UI display.
    /// </summary>
    public UnityEngine.Sprite Icon;

    /// <summary>
    /// The item's description/tooltip text.
    /// </summary>
    public string Description;
}

/// <summary>
/// Stub for Erenshor's ItemQuality enum.
/// </summary>
public enum ItemQuality
{
    Poor = 0,
    Common = 1,
    Uncommon = 2,
    Rare = 3,
    Epic = 4,
    Legendary = 5
}

/// <summary>
/// Stub for Erenshor's CharacterBuffs class.
/// Manages buffs and debuffs on a character.
/// </summary>
public class CharacterBuffs : MonoBehaviour
{
    /// <summary>
    /// List of active buffs on this character.
    /// </summary>
    public Buff[] ActiveBuffs;

    /// <summary>
    /// List of active debuffs on this character.
    /// </summary>
    public Buff[] ActiveDebuffs;
}

/// <summary>
/// Stub for Erenshor's Buff class.
/// Represents a single buff or debuff.
/// </summary>
public class Buff : MonoBehaviour
{
    /// <summary>
    /// The buff's display name.
    /// </summary>
    public string BuffName;

    /// <summary>
    /// Unique ID for this buff type.
    /// </summary>
    public string BuffId;

    /// <summary>
    /// Remaining duration in seconds.
    /// </summary>
    public float TimeRemaining;

    /// <summary>
    /// Max duration of this buff.
    /// </summary>
    public float MaxTime;

    /// <summary>
    /// Current stacks (if applicable).
    /// </summary>
    public int Stacks;

    /// <summary>
    /// Icon index for UI.
    /// </summary>
    public int IconIndex;

    /// <summary>
    /// Whether this is a debuff.
    /// </summary>
    public bool IsDebuff;

    /// <summary>
    /// The caster of this buff.
    /// </summary>
    public Character Caster;
}
