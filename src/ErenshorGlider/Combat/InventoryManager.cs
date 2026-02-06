using System;
using System.Collections.Generic;
using System.Linq;
using ErenshorGlider.GameState;
using ErenshorGlider.Input;
using ErenshorGlider.Navigation;
using ErenshorGlider.Waypoints;

namespace ErenshorGlider.Combat;

/// <summary>
/// The result of a vendor run.
/// </summary>
public enum VendorRunResult
{
    /// <summary>Selling completed successfully.</summary>
    Success,
    /// <summary>No vendor waypoint available.</summary>
    NoVendorAvailable,
    /// <summary>Vendor run was cancelled.</summary>
    Cancelled,
    /// <summary>Failed to reach vendor.</summary>
    FailedToReachVendor,
    /// <summary>Failed to sell items.</summary>
    FailedToSell,
    /// <summary>Vendor interaction timed out.</summary>
    Timeout
}

/// <summary>
/// Manages inventory space and automatic vendor runs.
/// </summary>
public class InventoryManager
{
    private readonly PositionTracker _positionTracker;
    private readonly GameStateReader _gameStateReader;
    private readonly Navigation _navigation;
    private readonly InputController _inputController;

    /// <summary>
    /// Gets or sets the minimum free bag slots before considering bags full.
    /// </summary>
    public int MinFreeBagSlots { get; set; } = 5;

    /// <summary>
    /// Gets or sets the maximum time to wait for vendor interaction (seconds).
    /// </summary>
    public float MaxVendorWaitSeconds { get; set; } = 10f;

    /// <summary>
    /// Gets or sets whether to automatically sell grey quality items.
    /// </summary>
    public bool SellGreyItems { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to automatically sell white quality items.
    /// </summary>
    public bool SellWhiteItems { get; set; } = false;

    /// <summary>
    /// Gets or sets the vendor waypoint to use for vendor runs.
    /// </summary>
    public Waypoint? VendorWaypoint { get; set; }

    /// <summary>
    /// Gets whether currently on a vendor run.
    /// </summary>
    public bool IsOnVendorRun { get; private set; }

    /// <summary>
    /// Gets whether currently selling items.
    /// </summary>
    public bool IsSelling { get; private set; }

    /// <summary>
    /// Gets whether bags are full.
    /// </summary>
    public bool AreBagsFull => _areBagsFull;

    private bool _areBagsFull;
    private DateTime _vendorRunStartTime;
    private int _itemsSoldCount;
    private int _goldEarned;

    /// <summary>
    /// Event raised when a vendor run starts.
    /// </summary>
    public event Action? OnVendorRunStarted;

    /// <summary>
    /// Event raised when a vendor run completes.
    /// </summary>
    public event Action<VendorRunResult>? OnVendorRunCompleted;

    /// <summary>
    /// Event raised when items are sold.
    /// </summary>
    public event Action<int, int>? OnItemsSold; // (itemCount, goldEarned)

    /// <summary>
    /// Creates a new InventoryManager.
    /// </summary>
    public InventoryManager(
        PositionTracker positionTracker,
        GameStateReader gameStateReader,
        Navigation navigation,
        InputController inputController)
    {
        _positionTracker = positionTracker ?? throw new ArgumentNullException(nameof(positionTracker));
        _gameStateReader = gameStateReader ?? throw new ArgumentNullException(nameof(gameStateReader));
        _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
        _inputController = inputController ?? throw new ArgumentNullException(nameof(inputController));
    }

    /// <summary>
    /// Checks if bags are full or nearly full.
    /// </summary>
    public bool CheckBagSpace()
    {
        var inventory = _positionTracker.CurrentInventory;
        if (!inventory.HasValue)
        {
            _areBagsFull = false;
            return false;
        }

        _areBagsFull = inventory.Value.FreeSlots < MinFreeBagSlots;
        return _areBagsFull;
    }

    /// <summary>
    /// Starts a vendor run to sell items.
    /// </summary>
    public bool StartVendorRun()
    {
        if (VendorWaypoint == null)
            return false;

        if (IsOnVendorRun)
            return false;

        IsOnVendorRun = true;
        _vendorRunStartTime = DateTime.UtcNow;
        _itemsSoldCount = 0;
        _goldEarned = 0;

        OnVendorRunStarted?.Invoke();
        return true;
    }

    /// <summary>
    /// Cancels the current vendor run.
    /// </summary>
    public void CancelVendorRun()
    {
        if (!IsOnVendorRun)
            return;

        IsOnVendorRun = false;
        IsSelling = false;
        _navigation.StopMovement();

        OnVendorRunCompleted?.Invoke(VendorRunResult.Cancelled);
    }

    /// <summary>
    /// Updates the vendor run. Should be called regularly while on a vendor run.
    /// </summary>
    public void UpdateVendorRun()
    {
        if (!IsOnVendorRun)
            return;

        // Check for timeout
        if ((DateTime.UtcNow - _vendorRunStartTime).TotalSeconds > MaxVendorWaitSeconds * 4)
        {
            CancelVendorRun();
            OnVendorRunCompleted?.Invoke(VendorRunResult.Timeout);
            return;
        }

        if (!IsSelling)
        {
            UpdateMovingToVendor();
        }
        else
        {
            UpdateSelling();
        }
    }

    /// <summary>
    /// Updates the moving to vendor state.
    /// </summary>
    private void UpdateMovingToVendor()
    {
        if (VendorWaypoint == null)
        {
            CancelVendorRun();
            OnVendorRunCompleted?.Invoke(VendorRunResult.NoVendorAvailable);
            return;
        }

        var playerPos = _positionTracker.CurrentPosition;
        if (!playerPos.HasValue)
        {
            CancelVendorRun();
            OnVendorRunCompleted?.Invoke(VendorRunResult.FailedToReachVendor);
            return;
        }

        float distance = Navigation.Navigation.CalculateDistance(playerPos.Value, VendorWaypoint.Value.Position);

        // Check if we've reached the vendor
        if (distance <= 3f) // Within interaction range
        {
            IsSelling = true;
            return;
        }

        // Move toward the vendor
        _navigation.MoveTo(VendorWaypoint.Value.Position);
        _navigation.FaceTarget(VendorWaypoint.Value.Position);

        // Check for stuck
        if (_navigation.IsStuck)
        {
            _navigation.StopMovement();
            CancelVendorRun();
            OnVendorRunCompleted?.Invoke(VendorRunResult.FailedToReachVendor);
        }
    }

    /// <summary>
    /// Updates the selling state.
    /// </summary>
    private DateTime _sellingStartTime;

    private void UpdateSelling()
    {
        if (_sellingStartTime == default)
        {
            _sellingStartTime = DateTime.UtcNow;
            // Interact with vendor
            _inputController.Interact();
        }

        // Wait for vendor window to open and sell items
        if ((DateTime.UtcNow - _sellingStartTime).TotalSeconds >= MaxVendorWaitSeconds)
        {
            // Timeout waiting for vendor
            CancelVendorRun();
            OnVendorRunCompleted?.Invoke(VendorRunResult.Timeout);
            return;
        }

        // Get sellable items
        var sellableItems = GetSellableItems();
        if (sellableItems.Count == 0)
        {
            // Nothing to sell
            CompleteVendorRun(VendorRunResult.Success);
            return;
        }

        // Sell items (this is a placeholder - actual implementation would use game-specific API)
        // For now, we'll simulate selling by counting the items
        int itemsSold = sellableItems.Count;
        int goldEarned = EstimateGoldValue(sellableItems);

        _itemsSoldCount += itemsSold;
        _goldEarned += goldEarned;

        OnItemsSold?.Invoke(itemsSold, goldEarned);

        // Complete the vendor run
        CompleteVendorRun(VendorRunResult.Success);
    }

    /// <summary>
    /// Gets the list of sellable items based on settings.
    /// </summary>
    private List<ItemInfo> GetSellableItems()
    {
        var inventory = _positionTracker.CurrentInventory;
        if (!inventory.HasValue)
            return new List<ItemInfo>();

        var sellable = new List<ItemInfo>();

        foreach (var item in inventory.Value.Items)
        {
            if (ShouldSellItem(item))
            {
                sellable.Add(item);
            }
        }

        return sellable;
    }

    /// <summary>
    /// Determines if an item should be sold.
    /// </summary>
    private bool ShouldSellItem(ItemInfo item)
    {
        // Don't sell if it's a quest item or special item
        // This would need to be expanded based on actual game item categories

        switch (item.Quality)
        {
            case ItemQuality.Poor: // Grey
                return SellGreyItems;
            case ItemQuality.Common: // White
                return SellWhiteItems;
            default:
                // Don't sell uncommon or better items
                return false;
        }
    }

    /// <summary>
    /// Estimates the gold value of items (placeholder).
    /// </summary>
    private int EstimateGoldValue(List<ItemInfo> items)
    {
        // This is a placeholder - actual implementation would need
        // to query the item's sell price from the game
        // For now, estimate 1 gold per poor item, 5 silver per white item
        int gold = 0;
        foreach (var item in items)
        {
            if (item.Quality == ItemQuality.Poor)
                gold += 1 * item.Quantity; // 1 gold each
            else if (item.Quality == ItemQuality.Common)
                gold += (1 * item.Quantity) / 10; // ~5 silver each
        }
        return gold;
    }

    /// <summary>
    /// Completes the vendor run.
    /// </summary>
    private void CompleteVendorRun(VendorRunResult result)
    {
        IsOnVendorRun = false;
        IsSelling = false;
        _sellingStartTime = default;

        OnVendorRunCompleted?.Invoke(result);
    }

    /// <summary>
    /// Finds the nearest vendor waypoint in a path.
    /// </summary>
    public static Waypoint? FindNearestVendorWaypoint(WaypointPath path, PlayerPosition currentPosition)
    {
        if (path == null || !path.HasWaypoints)
            return null;

        Waypoint? nearestVendor = null;
        float nearestDistance = float.MaxValue;

        foreach (var waypoint in path.Waypoints)
        {
            if (waypoint.Type == WaypointType.Vendor)
            {
                float distance = Navigation.Navigation.CalculateDistance(currentPosition, waypoint.Position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestVendor = waypoint;
                }
            }
        }

        return nearestVendor;
    }

    /// <summary>
    /// Finds all vendor waypoints in a path.
    /// </summary>
    public static List<Waypoint> FindVendorWaypoints(WaypointPath path)
    {
        if (path == null || !path.HasWaypoints)
            return new List<Waypoint>();

        return path.Waypoints
            .Where(wp => wp.Type == WaypointType.Vendor)
            .ToList();
    }

    /// <summary>
    /// Gets the current inventory status.
    /// </summary>
    public InventoryStatus GetInventoryStatus()
    {
        var inventory = _positionTracker.CurrentInventory;
        if (!inventory.HasValue)
        {
            return new InventoryStatus(0, 0, false, 0);
        }

        var inv = inventory.Value;
        return new InventoryStatus(
            inv.TotalSlots,
            inv.FreeSlots,
            inv.FreeSlots < MinFreeBagSlots,
            GetSellableItems().Count
        );
    }

    /// <summary>
    /// Gets the number of items sold during the current vendor run.
    /// </summary>
    public int ItemsSoldThisRun => _itemsSoldCount;

    /// <summary>
    /// Gets the gold earned during the current vendor run.
    /// </summary>
    public int GoldEarnedThisRun => _goldEarned;
}

/// <summary>
/// Represents the current inventory status.
/// </summary>
public readonly struct InventoryStatus
{
    /// <summary>Gets the total number of bag slots.</summary>
    public int TotalSlots { get; }

    /// <summary>Gets the number of free bag slots.</summary>
    public int FreeSlots { get; }

    /// <summary>Gets whether bags are considered full.</summary>
    public bool IsFull { get; }

    /// <summary>Gets the number of sellable items in bags.</summary>
    public int SellableItemCount { get; }

    public InventoryStatus(int totalSlots, int freeSlots, bool isFull, int sellableItemCount)
    {
        TotalSlots = totalSlots;
        FreeSlots = freeSlots;
        IsFull = isFull;
        SellableItemCount = sellableItemCount;
    }
}
