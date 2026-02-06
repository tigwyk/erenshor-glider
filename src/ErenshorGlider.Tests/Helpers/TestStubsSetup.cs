using System;
using UnityEngine;
using ErenshorGlider.Tests.GameStubs;
using ErenshorGlider.GameState;

namespace ErenshorGlider.Tests.Helpers;

/// <summary>
/// Helper class for setting up test stubs for unit tests.
/// Provides convenient methods to create and configure mock game objects.
/// </summary>
public static class TestStubsSetup
{
    /// <summary>
    /// Creates a mock player with basic stats at the given position.
    /// </summary>
    public static PlayerControl CreateMockPlayer(float x = 0f, float y = 0f, float z = 0f)
    {
        var player = new GameObject("Player").AddComponent<PlayerControl>();

        // Set player position
        player.transform.position = new Vector3(x, y, z);

        // Create character with stats
        var character = player.gameObject.AddComponent<Character>();
        character.CharacterName = "TestPlayer";

        var stats = player.gameObject.AddComponent<CharacterStats>();
        stats.CurrentHP = 100f;
        stats.MaxHP = 100f;
        stats.CurrentMP = 50f;
        stats.MaxMP = 50f;
        stats.Level = 1;
        stats.CurrentXP = 0f;
        stats.XPToLevel = 1000f;

        // Link character to stats
        character.MyStats = stats;
        character.MyFaction = Faction.Player;
        character.Dead = false;

        // Add buffs component
        var buffs = player.gameObject.AddComponent<CharacterBuffs>();
        buffs.ActiveBuffs = Array.Empty<Buff>();
        buffs.ActiveDebuffs = Array.Empty<Buff>();
        character.MyBuffs = buffs;

        // Link player to character
        player.Myself = character;

        // Add spell casting component
        player.PlayerSpells = player.gameObject.AddComponent<CastSpell>();
        player.PlayerSpells.isPlayer = true;
        player.PlayerSpells.Casting = false;
        player.PlayerSpells.MyChar = character;

        // Set the static GameData.PlayerControl
        GameData.PlayerControl = player;

        return player;
    }

    /// <summary>
    /// Creates a mock combat controller for the player.
    /// </summary>
    public static PlayerCombat CreateMockPlayerCombat()
    {
        var player = GameData.PlayerControl;
        if (player == null)
        {
            player = CreateMockPlayer();
        }

        var combat = player.gameObject.AddComponent<PlayerCombat>();
        combat.InCombat = false;

        // Set the static GameData.PlayerCombat
        GameData.PlayerCombat = combat;

        return combat;
    }

    /// <summary>
    /// Creates a mock player inventory.
    /// </summary>
    public static GameStubs.PlayerInventory CreateMockPlayerInventory(int slotCount = 20)
    {
        var player = GameData.PlayerControl;
        if (player == null)
        {
            player = CreateMockPlayer();
        }

        var inventory = player.gameObject.AddComponent<GameStubs.PlayerInventory>();
        inventory.SlotCount = slotCount;

        // Create empty slots
        var slots = new InventorySlot[slotCount];
        for (int i = 0; i < slotCount; i++)
        {
            var slotObj = new GameObject($"Slot_{i}");
            slots[i] = slotObj.AddComponent<InventorySlot>();
            slots[i].SlotIndex = i;
            slots[i].Item = null!;
            slots[i].Quantity = 0;
        }
        inventory.ALLSLOTS = slots;

        // Set the static GameData.PlayerInv
        GameData.PlayerInv = inventory;

        return inventory;
    }

    /// <summary>
    /// Creates a mock target (enemy mob) at the given position.
    /// </summary>
    public static Character CreateMockTarget(string name = "TestMob", float x = 10f, float y = 0f, float z = 10f, int level = 1)
    {
        var targetObj = new GameObject(name);
        targetObj.transform.position = new Vector3(x, y, z);

        var target = targetObj.AddComponent<Character>();
        target.CharacterName = name;
        target.MyFaction = Faction.Enemy;
        target.Dead = false;

        var stats = targetObj.AddComponent<CharacterStats>();
        stats.CurrentHP = 50f;
        stats.MaxHP = 50f;
        stats.Level = level;
        stats.CurrentMP = 20f;
        stats.MaxMP = 20f;
        stats.CurrentXP = 0f;
        stats.XPToLevel = 500f;

        target.MyStats = stats;

        // Add buffs component
        var buffs = targetObj.AddComponent<CharacterBuffs>();
        buffs.ActiveBuffs = Array.Empty<Buff>();
        buffs.ActiveDebuffs = Array.Empty<Buff>();
        target.MyBuffs = buffs;

        return target;
    }

    /// <summary>
    /// Creates a mock NPC at the given position.
    /// </summary>
    public static NPC CreateMockNPC(string name = "TestNPC", float x = 5f, float y = 0f, float z = 5f, bool isVendor = false)
    {
        var npcObj = new GameObject(name);
        npcObj.transform.position = new Vector3(x, y, z);

        var npc = npcObj.AddComponent<NPC>();
        npc.CharacterName = name;
        npc.MyFaction = Faction.Friendly;
        npc.Dead = false;
        npc.IsVendor = isVendor;
        npc.HasQuests = false;

        var stats = npcObj.AddComponent<CharacterStats>();
        stats.CurrentHP = 100f;
        stats.MaxHP = 100f;
        stats.Level = 5;
        stats.CurrentMP = 100f;
        stats.MaxMP = 100f;
        stats.CurrentXP = 0f;
        stats.XPToLevel = 0f;

        npc.MyStats = stats;

        // Add buffs component
        var buffs = npcObj.AddComponent<CharacterBuffs>();
        buffs.ActiveBuffs = Array.Empty<Buff>();
        buffs.ActiveDebuffs = Array.Empty<Buff>();
        npc.MyBuffs = buffs;

        return npc;
    }

    /// <summary>
    /// Sets the current target for the player.
    /// </summary>
    public static void SetPlayerTarget(Character? target)
    {
        var player = GameData.PlayerControl;
        if (player != null)
        {
            player.CurrentTarget = target!;
        }
    }

    /// <summary>
    /// Clears all static GameData references.
    /// Call this in test cleanup to avoid test pollution.
    /// </summary>
    public static void ClearStubs()
    {
        GameData.PlayerControl = null!;
        GameData.PlayerCombat = null!;
        GameData.PlayerInv = null!;
    }

    /// <summary>
    /// Creates a mock item in the given slot.
    /// </summary>
    public static Item CreateMockItem(string name, int quantity = 1, GameStubs.ItemQuality quality = GameStubs.ItemQuality.Common, int maxStackSize = 1)
    {
        var itemObj = new GameObject(name);
        var item = itemObj.AddComponent<Item>();
        item.ItemName = name;
        item.ItemId = $"test_item_{name.ToLower().Replace(' ', '_')}";
        item.Quality = quality;
        item.MaxStackSize = maxStackSize;
        item.IsJunk = quality == GameStubs.ItemQuality.Poor;
        item.Description = $"Test item: {name}";

        return item;
    }

    /// <summary>
    /// Creates a mock buff.
    /// </summary>
    public static Buff CreateMockBuff(string name, float duration = 10f, int stacks = 1, bool isDebuff = false)
    {
        var buffObj = new GameObject(name);
        var buff = buffObj.AddComponent<Buff>();
        buff.BuffName = name;
        buff.BuffId = $"test_buff_{name.ToLower().Replace(' ', '_')}";
        buff.TimeRemaining = duration;
        buff.MaxTime = duration;
        buff.Stacks = stacks;
        buff.IconIndex = 0;
        buff.IsDebuff = isDebuff;

        return buff;
    }

    /// <summary>
    /// Creates a PlayerPosition for testing.
    /// </summary>
    public static PlayerPosition CreatePlayerPosition(float x, float y, float z)
    {
        return new PlayerPosition(x, y, z);
    }

    /// <summary>
    /// Creates a PlayerVitals for testing.
    /// </summary>
    public static PlayerVitals CreatePlayerVitals(float currentHealth, float maxHealth, float currentMana, float maxMana, int level = 1)
    {
        return new PlayerVitals(currentHealth, maxHealth, currentMana, maxMana, level, 0f, 1000f);
    }
}
