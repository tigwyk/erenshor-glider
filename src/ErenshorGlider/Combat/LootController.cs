using System;
using System.Collections.Generic;
using System.Linq;
using ErenshorGlider.GameState;
using ErenshorGlider.Input;
using ErenshorGlider.Navigation;

namespace ErenshorGlider.Combat;

/// <summary>
/// Manages looting of corpses after combat.
/// </summary>
public class LootController
{
    private readonly InputController _inputController;
    private readonly Navigation _navigation;
    private readonly PositionTracker _positionTracker;

    private EntityInfo? _currentCorpse;
    private bool _isLooting;
    private DateTime _lootStartTime;

    /// <summary>
    /// Gets or sets the maximum time to wait for looting to complete.
    /// </summary>
    public float MaxLootWaitSeconds { get; set; } = 5f;

    /// <summary>
    /// Gets or sets the distance to consider "at" the corpse.
    /// </summary>
    public float LootDistance { get; set; } = 2f;

    /// <summary>
    /// Gets whether currently looting.
    /// </summary>
    public bool IsLooting => _isLooting;

    /// <summary>
    /// Gets the current corpse being looted.
    /// </summary>
    public EntityInfo? CurrentCorpse => _currentCorpse;

    /// <summary>
    /// Gets or sets whether to skip looting when bags are full.
    /// </summary>
    public bool SkipWhenFull { get; set; } = true;

    /// <summary>
    /// Event raised when looting starts.
    /// </summary>
    public event Action<EntityInfo>? OnLootStarted;

    /// <summary>
    /// Event raised when looting completes.
    /// </summary>
    public event Action<LootResult>? OnLootCompleted;

    /// <summary>
    /// Event raised when looting fails.
    /// </summary>
    public event Action<string>? OnLootFailed;

    /// <summary>
    /// Creates a new LootController.
    /// </summary>
    public LootController(
        InputController inputController,
        Navigation navigation,
        PositionTracker positionTracker)
    {
        _inputController = inputController ?? throw new ArgumentNullException(nameof(inputController));
        _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
        _positionTracker = positionTracker ?? throw new ArgumentNullException(nameof(positionTracker));
    }

    /// <summary>
    /// Starts looting the nearest lootable corpse.
    /// </summary>
    /// <returns>True if looting started, false if no corpse found.</returns>
    public bool LootNearestCorpse()
    {
        var corpse = FindNearestLootableCorpse();
        if (corpse == null)
            return false;

        return StartLooting(corpse.Value);
    }

    /// <summary>
    /// Starts looting a specific corpse.
    /// </summary>
    public bool StartLooting(in EntityInfo corpse)
    {
        if (!corpse.IsCorpse)
        {
            OnLootFailed?.Invoke("Not a corpse");
            return false;
        }

        // Check if bags are full
        if (SkipWhenFull && AreBagsFull())
        {
            OnLootFailed?.Invoke("Bags are full");
            return false;
        }

        _currentCorpse = corpse;
        _isLooting = true;
        _lootStartTime = DateTime.UtcNow;

        OnLootStarted?.Invoke(corpse);
        return true;
    }

    /// <summary>
    /// Cancels the current looting operation.
    /// </summary>
    public void CancelLooting()
    {
        _currentCorpse = null;
        _isLooting = false;
        _navigation.StopMovement();
        _inputController.CancelInteraction();
    }

    /// <summary>
    /// Updates the loot controller. Should be called regularly.
    /// </summary>
    public void Update()
    {
        if (!_isLooting || _currentCorpse == null)
            return;

        // Check for timeout
        var elapsed = (DateTime.UtcNow - _lootStartTime).TotalSeconds;
        if (elapsed > MaxLootWaitSeconds)
        {
            CancelLooting();
            OnLootFailed?.Invoke("Loot timeout");
            return;
        }

        var currentPos = _positionTracker.CurrentPosition;
        if (currentPos == null)
            return;

        float distance = Navigation.CalculateDistance(currentPos.Value, _currentCorpse.Value.Position);

        // Move to corpse if too far
        if (distance > LootDistance)
        {
            _navigation.MoveTo(_currentCorpse.Value.Position);
            return;
        }

        // Face the corpse
        if (!_navigation.IsFacing(_currentCorpse.Value.Position))
        {
            _navigation.FaceTarget(_currentCorpse.Value.Position);
            return;
        }

        // Attempt to loot
        _inputController.Interact();

        // Check if looting is complete (corpse no longer exists or was looted)
        var nearbyEntities = _positionTracker.CurrentNearbyEntities;
        if (nearbyEntities != null)
        {
            var stillExists = nearbyEntities.FirstOrDefault(e =>
                e.Position.X == _currentCorpse.Value.Position.X &&
                e.Position.Y == _currentCorpse.Value.Position.Y &&
                e.Position.Z == _currentCorpse.Value.Position.Z &&
                e.IsCorpse);

            if (stillEqualsDefault(stillExists))
            {
                // Corpse is gone - loot complete
                CompleteLooting(LootResult.Success);
            }
        }
    }

    /// <summary>
    /// Finds the nearest lootable corpse.
    /// </summary>
    public EntityInfo? FindNearestLootableCorpse()
    {
        var nearbyEntities = _positionTracker.CurrentNearbyEntities;
        if (nearbyEntities == null)
            return null;

        return nearbyEntities
            .Where(e => e.IsCorpse)
            .OrderBy(e => e.Distance)
            .FirstOrDefault();
    }

    /// <summary>
    /// Gets all lootable corpses in range.
    /// </summary>
    public List<EntityInfo> GetLootableCorpses()
    {
        var nearbyEntities = _positionTracker.CurrentNearbyEntities;
        if (nearbyEntities == null)
            return new List<EntityInfo>();

        return nearbyEntities
            .Where(e => e.IsCorpse)
            .OrderBy(e => e.Distance)
            .ToList();
    }

    /// <summary>
    /// Gets the number of lootable corpses in range.
    /// </summary>
    public int GetLootableCorpseCount()
    {
        return GetLootableCorpses().Count;
    }

    /// <summary>
    /// Checks if bags are full.
    /// </summary>
    public bool AreBagsFull()
    {
        var inventory = _positionTracker.CurrentInventory;
        return inventory != null && inventory.IsFull;
    }

    /// <summary>
    /// Gets the number of free bag slots.
    /// </summary>
    public int GetFreeSlotCount()
    {
        var inventory = _positionTracker.CurrentInventory;
        return inventory?.FreeSlots ?? 0;
    }

    /// <summary>
    /// Marks looting as complete.
    /// </summary>
    private void CompleteLooting(LootResult result)
    {
        var corpse = _currentCorpse;
        _currentCorpse = null;
        _isLooting = false;

        OnLootCompleted?.Invoke(result);
    }

    /// <summary>
    /// Checks if an EntityInfo is default (empty).
    /// </summary>
    private bool stillEqualsDefault(EntityInfo entity)
    {
        return entity.Name == null && entity.Type == EntityType.Mob;
    }
}

/// <summary>
/// The result of a looting operation.
/// </summary>
public enum LootResult
{
    /// <summary>Looting completed successfully.</summary>
    Success,
    /// <summary>Looting failed (bags full, etc.).</summary>
    Failed,
    /// <summary>Looting timed out.</summary>
    Timeout,
    /// <summary>Corpse was already looted.</summary>
    AlreadyLooted
}
