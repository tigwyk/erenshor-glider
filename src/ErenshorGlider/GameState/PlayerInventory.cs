using System;
using System.Collections.Generic;
using ErenshorGlider.GameStubs;

namespace ErenshorGlider.GameState;

/// <summary>
/// Represents information about an item in the player's inventory.
/// </summary>
public readonly struct ItemInfo : IEquatable<ItemInfo>
{
    /// <summary>
    /// The item's display name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The quantity/stack size of this item.
    /// </summary>
    public int Quantity { get; }

    /// <summary>
    /// The item quality (rarity tier).
    /// </summary>
    public ItemQuality Quality { get; }

    /// <summary>
    /// The inventory slot index this item occupies.
    /// </summary>
    public int SlotIndex { get; }

    /// <summary>
    /// The unique item ID (if available).
    /// </summary>
    public string? ItemId { get; }

    /// <summary>
    /// The maximum stack size for this item type.
    /// </summary>
    public int MaxStackSize { get; }

    /// <summary>
    /// Creates a new ItemInfo instance.
    /// </summary>
    public ItemInfo(
        string name,
        int quantity,
        ItemQuality quality,
        int slotIndex,
        string? itemId = null,
        int maxStackSize = 1)
    {
        Name = name ?? string.Empty;
        Quantity = Math.Max(0, quantity);
        Quality = quality;
        SlotIndex = slotIndex;
        ItemId = itemId;
        MaxStackSize = Math.Max(1, maxStackSize);
    }

    /// <summary>
    /// Returns true if this item is of common quality.
    /// </summary>
    public bool IsCommon => Quality == ItemQuality.Common;

    /// <summary>
    /// Returns true if this item is of uncommon quality or better.
    /// </summary>
    public bool IsUncommonOrBetter => Quality >= ItemQuality.Uncommon;

    /// <summary>
    /// Returns true if this item is of rare quality or better.
    /// </summary>
    public bool IsRareOrBetter => Quality >= ItemQuality.Rare;

    /// <summary>
    /// Returns true if this item is of epic quality or better.
    /// </summary>
    public bool IsEpicOrBetter => Quality >= ItemQuality.Epic;

    /// <summary>
    /// Returns true if the item stack is full.
    /// </summary>
    public bool IsFullStack => Quantity >= MaxStackSize;

    /// <summary>
    /// Returns true if this item can be stacked further.
    /// </summary>
    public bool CanStackMore => MaxStackSize > 1 && Quantity < MaxStackSize;

    public bool Equals(ItemInfo other)
    {
        return SlotIndex == other.SlotIndex &&
               Name == other.Name &&
               Quantity == other.Quantity &&
               Quality == other.Quality;
    }

    public override bool Equals(object? obj)
    {
        return obj is ItemInfo other && Equals(other);
    }

    public override int GetHashCode()
    {
        // Manual hash code combination for .NET Framework 4.7.2 compatibility
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + SlotIndex.GetHashCode();
            hash = hash * 31 + (Name?.GetHashCode() ?? 0);
            hash = hash * 31 + Quantity.GetHashCode();
            hash = hash * 31 + Quality.GetHashCode();
            return hash;
        }
    }

    public override string ToString()
    {
        return Quantity > 1
            ? $"{Name} x{Quantity} ({Quality})"
            : $"{Name} ({Quality})";
    }
}

/// <summary>
/// Represents the quality/rarity tier of an item.
/// </summary>
public enum ItemQuality
{
    /// <summary>
    /// Poor quality (typically gray/junk items).
    /// </summary>
    Poor = 0,

    /// <summary>
    /// Common quality (typically white items).
    /// </summary>
    Common = 1,

    /// <summary>
    /// Uncommon quality (typically green items).
    /// </summary>
    Uncommon = 2,

    /// <summary>
    /// Rare quality (typically blue items).
    /// </summary>
    Rare = 3,

    /// <summary>
    /// Epic quality (typically purple items).
    /// </summary>
    Epic = 4,

    /// <summary>
    /// Legendary quality (typically orange/gold items).
    /// </summary>
    Legendary = 5
}

/// <summary>
/// Represents the player's inventory state.
/// </summary>
public readonly struct PlayerInventory
{
    /// <summary>
    /// The total number of bag slots available.
    /// </summary>
    public int TotalSlots { get; }

    /// <summary>
    /// The number of free bag slots.
    /// </summary>
    public int FreeSlots { get; }

    /// <summary>
    /// The number of occupied bag slots.
    /// </summary>
    public int UsedSlots => TotalSlots - FreeSlots;

    /// <summary>
    /// The list of items in the inventory.
    /// </summary>
    public IReadOnlyList<ItemInfo> Items { get; }

    /// <summary>
    /// Whether any bags are full (no free slots).
    /// </summary>
    public bool IsFull => FreeSlots == 0;

    /// <summary>
    /// The percentage of bag space used (0-100).
    /// </summary>
    public float FillPercent => TotalSlots > 0 ? (UsedSlots / (float)TotalSlots) * 100f : 0f;

    /// <summary>
    /// Creates a new PlayerInventory instance.
    /// </summary>
    public PlayerInventory(
        int totalSlots,
        int freeSlots,
        IReadOnlyList<ItemInfo> items)
    {
        TotalSlots = Math.Max(0, totalSlots);
        FreeSlots = Math.Max(0, Math.Min(freeSlots, totalSlots));
        Items = items ?? Array.Empty<ItemInfo>();
    }

    /// <summary>
    /// Returns true if bags are full or nearly full (above threshold).
    /// </summary>
    /// <param name="thresholdPercent">The threshold percentage (default 95).</param>
    public bool IsNearlyFull(float thresholdPercent = 95f)
    {
        return FillPercent >= thresholdPercent;
    }

    /// <summary>
    /// Returns true if bags have at least the specified number of free slots.
    /// </summary>
    public bool HasFreeSlots(int count)
    {
        return FreeSlots >= count;
    }

    /// <summary>
    /// Counts items matching the given name (case-insensitive partial match).
    /// </summary>
    public int CountItem(string itemName)
    {
        if (string.IsNullOrEmpty(itemName))
            return 0;

        int total = 0;
        foreach (var item in Items)
        {
            if (item.Name.IndexOf(itemName, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                total += item.Quantity;
            }
        }
        return total;
    }

    /// <summary>
    /// Returns true if the inventory contains the specified item.
    /// </summary>
    public bool HasItem(string itemName)
    {
        return CountItem(itemName) > 0;
    }

    /// <summary>
    /// Gets all items of the specified quality or better.
    /// </summary>
    public IReadOnlyList<ItemInfo> GetItemsOfQuality(ItemQuality minQuality)
    {
        var result = new List<ItemInfo>();
        foreach (var item in Items)
        {
            if (item.Quality >= minQuality)
            {
                result.Add(item);
            }
        }
        return result;
    }

    /// <summary>
    /// An empty inventory state.
    /// </summary>
    public static PlayerInventory Empty => new(0, 0, Array.Empty<ItemInfo>());

    public override string ToString()
    {
        return $"Inventory: {UsedSlots}/{TotalSlots} slots used ({FillPercent:F1}%)";
    }
}
