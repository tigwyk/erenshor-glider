using System;
using System.Collections.Generic;
using ErenshorGlider.GameStubs;
using UnityEngine;

namespace ErenshorGlider.GameState;

/// <summary>
/// Reads game state from Erenshor via the GameData singleton.
/// Provides a clean API for accessing player position, vitals, and other game state.
/// </summary>
public class GameStateReader
{
    private PlayerPosition _lastPosition;
    private DateTime _lastPositionUpdate;
    private readonly object _positionLock = new();

    private PlayerVitals _lastVitals;
    private DateTime _lastVitalsUpdate;
    private readonly object _vitalsLock = new();

    private CombatState _lastCombatState;
    private DateTime _lastCombatStateUpdate;
    private readonly object _combatStateLock = new();

    private TargetInfo _lastTargetInfo;
    private DateTime _lastTargetInfoUpdate;
    private readonly object _targetInfoLock = new();

    private List<EntityInfo> _lastNearbyEntities = new();
    private DateTime _lastNearbyEntitiesUpdate;
    private readonly object _nearbyEntitiesLock = new();
    private float _nearbyEntitiesRadius = 50f; // Default scan radius

    private PlayerInventory _lastInventory;
    private DateTime _lastInventoryUpdate;
    private readonly object _inventoryLock = new();

    private BuffState _lastPlayerBuffs;
    private DateTime _lastPlayerBuffsUpdate;
    private readonly object _playerBuffsLock = new();

    private BuffState _lastTargetBuffs;
    private DateTime _lastTargetBuffsUpdate;
    private readonly object _targetBuffsLock = new();

    /// <summary>
    /// Event raised when player position changes.
    /// </summary>
    public event Action<PlayerPosition>? OnPositionChanged;

    /// <summary>
    /// Event raised when player vitals change.
    /// </summary>
    public event Action<PlayerVitals>? OnVitalsChanged;

    /// <summary>
    /// Event raised when combat state changes.
    /// </summary>
    public event Action<CombatState>? OnCombatStateChanged;

    /// <summary>
    /// Event raised when target info changes.
    /// </summary>
    public event Action<TargetInfo>? OnTargetInfoChanged;

    /// <summary>
    /// Event raised when nearby entities list is updated.
    /// </summary>
    public event Action<IReadOnlyList<EntityInfo>>? OnNearbyEntitiesChanged;

    /// <summary>
    /// Event raised when inventory state changes.
    /// </summary>
    public event Action<PlayerInventory>? OnInventoryChanged;

    /// <summary>
    /// Event raised when player buffs/debuffs change.
    /// </summary>
    public event Action<BuffState>? OnPlayerBuffsChanged;

    /// <summary>
    /// Event raised when target buffs/debuffs change.
    /// </summary>
    public event Action<BuffState>? OnTargetBuffsChanged;

    /// <summary>
    /// Gets whether the game state is currently available (player is loaded).
    /// </summary>
    public bool IsAvailable => GetPlayerControlTransform() != null;

    /// <summary>
    /// Gets the player's current position in the game world.
    /// Returns null if player is not loaded.
    /// </summary>
    public PlayerPosition? GetPlayerPosition()
    {
        var transform = GetPlayerControlTransform();
        if (transform == null)
            return null;

        var position = new PlayerPosition(transform.position);

        lock (_positionLock)
        {
            _lastPosition = position;
            _lastPositionUpdate = DateTime.UtcNow;
        }

        return position;
    }

    /// <summary>
    /// Gets the player's cached position (from last update).
    /// Useful for checking position without polling the game.
    /// </summary>
    public PlayerPosition? GetCachedPosition()
    {
        lock (_positionLock)
        {
            if (_lastPositionUpdate == default)
                return null;

            return _lastPosition;
        }
    }

    /// <summary>
    /// Gets the time since the position was last updated.
    /// </summary>
    public TimeSpan TimeSinceLastUpdate
    {
        get
        {
            lock (_positionLock)
            {
                if (_lastPositionUpdate == default)
                    return TimeSpan.MaxValue;

                return DateTime.UtcNow - _lastPositionUpdate;
            }
        }
    }

    /// <summary>
    /// Updates the position cache and raises events if position changed.
    /// This should be called at the desired update rate (e.g., 10Hz or higher).
    /// </summary>
    /// <returns>True if position was updated successfully, false if game state unavailable.</returns>
    public bool UpdatePosition()
    {
        var newPosition = GetPlayerPosition();
        if (newPosition == null)
            return false;

        // Check if position actually changed (with small epsilon for floating point comparison)
        PlayerPosition? oldPosition;
        lock (_positionLock)
        {
            oldPosition = _lastPositionUpdate != default ? _lastPosition : (PlayerPosition?)null;
        }

        if (oldPosition == null || HasPositionChanged(oldPosition.Value, newPosition.Value))
        {
            OnPositionChanged?.Invoke(newPosition.Value);
        }

        return true;
    }

    private static bool HasPositionChanged(PlayerPosition old, PlayerPosition newPos)
    {
        const float epsilon = 0.001f;
        return Math.Abs(old.X - newPos.X) > epsilon ||
               Math.Abs(old.Y - newPos.Y) > epsilon ||
               Math.Abs(old.Z - newPos.Z) > epsilon;
    }

    #region Vitals Reading

    /// <summary>
    /// Gets the player's current vitals (health, mana, level, XP).
    /// Returns null if player is not loaded.
    /// </summary>
    public PlayerVitals? GetPlayerVitals()
    {
        var stats = GetPlayerStats();
        if (stats == null)
            return null;

        var vitals = new PlayerVitals(
            currentHealth: stats.CurrentHP,
            maxHealth: stats.MaxHP,
            currentMana: stats.CurrentMP,
            maxMana: stats.MaxMP,
            level: stats.Level,
            currentXP: stats.CurrentXP,
            xpToLevel: stats.XPToLevel
        );

        lock (_vitalsLock)
        {
            _lastVitals = vitals;
            _lastVitalsUpdate = DateTime.UtcNow;
        }

        return vitals;
    }

    /// <summary>
    /// Gets the player's cached vitals (from last update).
    /// Useful for checking vitals without polling the game.
    /// </summary>
    public PlayerVitals? GetCachedVitals()
    {
        lock (_vitalsLock)
        {
            if (_lastVitalsUpdate == default)
                return null;

            return _lastVitals;
        }
    }

    /// <summary>
    /// Gets the time since vitals were last updated.
    /// </summary>
    public TimeSpan TimeSinceLastVitalsUpdate
    {
        get
        {
            lock (_vitalsLock)
            {
                if (_lastVitalsUpdate == default)
                    return TimeSpan.MaxValue;

                return DateTime.UtcNow - _lastVitalsUpdate;
            }
        }
    }

    /// <summary>
    /// Updates the vitals cache and raises events if vitals changed.
    /// This should be called at the desired update rate (e.g., 10Hz or higher).
    /// </summary>
    /// <returns>True if vitals were updated successfully, false if game state unavailable.</returns>
    public bool UpdateVitals()
    {
        var newVitals = GetPlayerVitals();
        if (newVitals == null)
            return false;

        // Check if vitals actually changed
        PlayerVitals? oldVitals;
        lock (_vitalsLock)
        {
            oldVitals = _lastVitalsUpdate != default ? _lastVitals : (PlayerVitals?)null;
        }

        if (oldVitals == null || HasVitalsChanged(oldVitals.Value, newVitals.Value))
        {
            OnVitalsChanged?.Invoke(newVitals.Value);
        }

        return true;
    }

    private static bool HasVitalsChanged(PlayerVitals old, PlayerVitals newVitals)
    {
        const float epsilon = 0.01f;
        return Math.Abs(old.CurrentHealth - newVitals.CurrentHealth) > epsilon ||
               Math.Abs(old.MaxHealth - newVitals.MaxHealth) > epsilon ||
               Math.Abs(old.CurrentMana - newVitals.CurrentMana) > epsilon ||
               Math.Abs(old.MaxMana - newVitals.MaxMana) > epsilon ||
               old.Level != newVitals.Level ||
               Math.Abs(old.CurrentXP - newVitals.CurrentXP) > epsilon ||
               Math.Abs(old.XPToLevel - newVitals.XPToLevel) > epsilon;
    }

    #endregion

    #region Combat State Reading

    /// <summary>
    /// Gets the player's current combat state (in combat, casting, alive/dead).
    /// Returns null if player is not loaded.
    /// </summary>
    public CombatState? GetCombatState()
    {
        try
        {
            var playerControl = GameData.PlayerControl;
            if (playerControl == null)
                return null;

            var character = playerControl.Myself;
            if (character == null)
                return null;

            // Read combat state from PlayerCombat component
            var playerCombat = GameData.PlayerCombat;
            bool inCombat = playerCombat?.InCombat ?? false;

            // Read casting state from CastSpell component
            var playerSpells = playerControl.PlayerSpells;
            bool isCasting = playerSpells?.Casting ?? false;

            // Read alive/dead state from Character
            bool isAlive = !character.Dead;

            var combatState = new CombatState(inCombat, isCasting, isAlive);

            lock (_combatStateLock)
            {
                _lastCombatState = combatState;
                _lastCombatStateUpdate = DateTime.UtcNow;
            }

            return combatState;
        }
        catch (Exception)
        {
            // Game state not available
            return null;
        }
    }

    /// <summary>
    /// Gets the player's cached combat state (from last update).
    /// Useful for checking combat state without polling the game.
    /// </summary>
    public CombatState? GetCachedCombatState()
    {
        lock (_combatStateLock)
        {
            if (_lastCombatStateUpdate == default)
                return null;

            return _lastCombatState;
        }
    }

    /// <summary>
    /// Gets the time since combat state was last updated.
    /// </summary>
    public TimeSpan TimeSinceLastCombatStateUpdate
    {
        get
        {
            lock (_combatStateLock)
            {
                if (_lastCombatStateUpdate == default)
                    return TimeSpan.MaxValue;

                return DateTime.UtcNow - _lastCombatStateUpdate;
            }
        }
    }

    /// <summary>
    /// Updates the combat state cache and raises events if state changed.
    /// This should be called at the desired update rate (e.g., 10Hz or higher).
    /// </summary>
    /// <returns>True if combat state was updated successfully, false if game state unavailable.</returns>
    public bool UpdateCombatState()
    {
        var newCombatState = GetCombatState();
        if (newCombatState == null)
            return false;

        // Check if combat state actually changed
        CombatState? oldCombatState;
        lock (_combatStateLock)
        {
            oldCombatState = _lastCombatStateUpdate != default ? _lastCombatState : (CombatState?)null;
        }

        if (oldCombatState == null || HasCombatStateChanged(oldCombatState.Value, newCombatState.Value))
        {
            OnCombatStateChanged?.Invoke(newCombatState.Value);
        }

        return true;
    }

    private static bool HasCombatStateChanged(CombatState old, CombatState newState)
    {
        return old.InCombat != newState.InCombat ||
               old.IsCasting != newState.IsCasting ||
               old.IsAlive != newState.IsAlive;
    }

    #endregion

    #region Target Info Reading

    /// <summary>
    /// Gets information about the player's current target.
    /// Returns TargetInfo with HasTarget=false if no target or player not loaded.
    /// </summary>
    public TargetInfo GetTargetInfo()
    {
        try
        {
            var playerControl = GameData.PlayerControl;
            if (playerControl == null)
                return TargetInfo.NoTarget;

            var target = playerControl.CurrentTarget;
            if (target == null)
            {
                var noTarget = TargetInfo.NoTarget;
                UpdateTargetInfoCache(noTarget);
                return noTarget;
            }

            var stats = target.MyStats;
            var targetTransform = target.transform;

            var targetInfo = new TargetInfo(
                hasTarget: true,
                name: target.CharacterName ?? "Unknown",
                level: stats?.Level ?? 0,
                currentHealth: stats?.CurrentHP ?? 0,
                maxHealth: stats?.MaxHP ?? 0,
                position: targetTransform != null
                    ? new PlayerPosition(targetTransform.position)
                    : new PlayerPosition(0, 0, 0),
                hostility: ConvertFactionToHostility(target.MyFaction),
                isDead: target.Dead
            );

            UpdateTargetInfoCache(targetInfo);
            return targetInfo;
        }
        catch (Exception)
        {
            // Game state not available
            return TargetInfo.NoTarget;
        }
    }

    /// <summary>
    /// Gets the cached target info (from last update).
    /// Useful for checking target without polling the game.
    /// </summary>
    public TargetInfo? GetCachedTargetInfo()
    {
        lock (_targetInfoLock)
        {
            if (_lastTargetInfoUpdate == default)
                return null;

            return _lastTargetInfo;
        }
    }

    /// <summary>
    /// Gets the time since target info was last updated.
    /// </summary>
    public TimeSpan TimeSinceLastTargetInfoUpdate
    {
        get
        {
            lock (_targetInfoLock)
            {
                if (_lastTargetInfoUpdate == default)
                    return TimeSpan.MaxValue;

                return DateTime.UtcNow - _lastTargetInfoUpdate;
            }
        }
    }

    /// <summary>
    /// Updates the target info cache and raises events if target changed.
    /// This should be called at the desired update rate (e.g., 10Hz or higher).
    /// </summary>
    /// <returns>True if target info was updated successfully, false if game state unavailable.</returns>
    public bool UpdateTargetInfo()
    {
        var newTargetInfo = GetTargetInfo();

        // Check if target info actually changed
        TargetInfo? oldTargetInfo;
        lock (_targetInfoLock)
        {
            oldTargetInfo = _lastTargetInfoUpdate != default ? _lastTargetInfo : (TargetInfo?)null;
        }

        if (oldTargetInfo == null || HasTargetInfoChanged(oldTargetInfo.Value, newTargetInfo))
        {
            OnTargetInfoChanged?.Invoke(newTargetInfo);
        }

        return true;
    }

    private void UpdateTargetInfoCache(TargetInfo targetInfo)
    {
        lock (_targetInfoLock)
        {
            _lastTargetInfo = targetInfo;
            _lastTargetInfoUpdate = DateTime.UtcNow;
        }
    }

    private static bool HasTargetInfoChanged(TargetInfo old, TargetInfo newInfo)
    {
        const float epsilon = 0.01f;

        // Target presence changed
        if (old.HasTarget != newInfo.HasTarget)
            return true;

        // No target in both cases
        if (!old.HasTarget && !newInfo.HasTarget)
            return false;

        // Check individual properties
        return old.Name != newInfo.Name ||
               old.Level != newInfo.Level ||
               Math.Abs(old.CurrentHealth - newInfo.CurrentHealth) > epsilon ||
               Math.Abs(old.MaxHealth - newInfo.MaxHealth) > epsilon ||
               old.Hostility != newInfo.Hostility ||
               old.IsDead != newInfo.IsDead ||
               HasPositionChanged(
                   new PlayerPosition(old.Position.X, old.Position.Y, old.Position.Z),
                   new PlayerPosition(newInfo.Position.X, newInfo.Position.Y, newInfo.Position.Z));
    }

    private static TargetHostility ConvertFactionToHostility(Faction faction)
    {
        return faction switch
        {
            Faction.Enemy => TargetHostility.Hostile,
            Faction.Friendly => TargetHostility.Friendly,
            Faction.Player => TargetHostility.Friendly,
            Faction.Neutral => TargetHostility.Neutral,
            _ => TargetHostility.Neutral
        };
    }

    #endregion

    #region Nearby Entities Reading

    /// <summary>
    /// Gets or sets the radius for nearby entity detection.
    /// Default is 50 units.
    /// </summary>
    public float NearbyEntitiesRadius
    {
        get => _nearbyEntitiesRadius;
        set => _nearbyEntitiesRadius = Math.Max(1f, value);
    }

    /// <summary>
    /// Gets a list of entities within the configured radius.
    /// Returns an empty list if player is not loaded.
    /// </summary>
    /// <param name="radius">Optional radius override. Uses NearbyEntitiesRadius if not specified.</param>
    public IReadOnlyList<EntityInfo> GetNearbyEntities(float? radius = null)
    {
        var scanRadius = radius ?? _nearbyEntitiesRadius;
        var entities = new List<EntityInfo>();

        try
        {
            var playerTransform = GetPlayerControlTransform();
            if (playerTransform == null)
                return entities;

            var playerPosition = playerTransform.position;
            var playerCharacter = GameData.PlayerControl?.Myself;

            // Find all Character objects in the scene
            var allCharacters = UnityEngine.Object.FindObjectsOfType<Character>();
            foreach (var character in allCharacters)
            {
                // Skip the player
                if (character == playerCharacter)
                    continue;

                // Skip null or destroyed objects
                if (character == null || character.transform == null)
                    continue;

                var charPosition = character.transform.position;
                var distance = Vector3.Distance(playerPosition, charPosition);

                // Skip if outside radius
                if (distance > scanRadius)
                    continue;

                var entityInfo = CreateEntityInfoFromCharacter(character, distance);
                entities.Add(entityInfo);
            }

            // Find all NPC objects (may be a subclass of Character, but search separately for completeness)
            var allNPCs = UnityEngine.Object.FindObjectsOfType<NPC>();
            foreach (var npc in allNPCs)
            {
                // Skip if already added as Character
                if (npc == playerCharacter)
                    continue;

                if (npc == null || npc.transform == null)
                    continue;

                // Check if already in list (NPC inherits from Character)
                var npcPosition = npc.transform.position;
                var distance = Vector3.Distance(playerPosition, npcPosition);

                if (distance > scanRadius)
                    continue;

                // Only add if not already present (check by position)
                bool alreadyExists = false;
                foreach (var existing in entities)
                {
                    if (Math.Abs(existing.Position.X - npcPosition.x) < 0.1f &&
                        Math.Abs(existing.Position.Y - npcPosition.y) < 0.1f &&
                        Math.Abs(existing.Position.Z - npcPosition.z) < 0.1f)
                    {
                        alreadyExists = true;
                        break;
                    }
                }

                if (!alreadyExists)
                {
                    var entityInfo = CreateEntityInfoFromNPC(npc, distance);
                    entities.Add(entityInfo);
                }
            }

            // Find resource nodes
            var allNodes = UnityEngine.Object.FindObjectsOfType<ResourceNode>();
            foreach (var node in allNodes)
            {
                if (node == null || node.transform == null)
                    continue;

                var nodePosition = node.transform.position;
                var distance = Vector3.Distance(playerPosition, nodePosition);

                if (distance > scanRadius)
                    continue;

                var entityInfo = CreateEntityInfoFromNode(node, distance);
                entities.Add(entityInfo);
            }

            // Find lootable corpses
            var allCorpses = UnityEngine.Object.FindObjectsOfType<LootableCorpse>();
            foreach (var corpse in allCorpses)
            {
                if (corpse == null || corpse.transform == null)
                    continue;

                // Skip already looted corpses
                if (corpse.IsLooted)
                    continue;

                var corpsePosition = corpse.transform.position;
                var distance = Vector3.Distance(playerPosition, corpsePosition);

                if (distance > scanRadius)
                    continue;

                var entityInfo = CreateEntityInfoFromCorpse(corpse, distance);
                entities.Add(entityInfo);
            }

            // Sort by distance
            entities.Sort((a, b) => a.Distance.CompareTo(b.Distance));

            lock (_nearbyEntitiesLock)
            {
                _lastNearbyEntities = entities;
                _lastNearbyEntitiesUpdate = DateTime.UtcNow;
            }

            return entities;
        }
        catch (Exception)
        {
            // Game state not available
            return entities;
        }
    }

    /// <summary>
    /// Gets the cached nearby entities list (from last update).
    /// </summary>
    public IReadOnlyList<EntityInfo>? GetCachedNearbyEntities()
    {
        lock (_nearbyEntitiesLock)
        {
            if (_lastNearbyEntitiesUpdate == default)
                return null;

            return _lastNearbyEntities.AsReadOnly();
        }
    }

    /// <summary>
    /// Gets the time since nearby entities were last updated.
    /// </summary>
    public TimeSpan TimeSinceLastNearbyEntitiesUpdate
    {
        get
        {
            lock (_nearbyEntitiesLock)
            {
                if (_lastNearbyEntitiesUpdate == default)
                    return TimeSpan.MaxValue;

                return DateTime.UtcNow - _lastNearbyEntitiesUpdate;
            }
        }
    }

    /// <summary>
    /// Updates the nearby entities cache and raises events if the list changed.
    /// </summary>
    /// <returns>True if update was successful, false if game state unavailable.</returns>
    public bool UpdateNearbyEntities()
    {
        IReadOnlyList<EntityInfo>? oldEntities;
        lock (_nearbyEntitiesLock)
        {
            oldEntities = _lastNearbyEntitiesUpdate != default
                ? _lastNearbyEntities.AsReadOnly()
                : null;
        }

        var newEntities = GetNearbyEntities();

        if (newEntities.Count == 0 && !IsAvailable)
            return false;

        if (oldEntities == null || HasNearbyEntitiesChanged(oldEntities, newEntities))
        {
            OnNearbyEntitiesChanged?.Invoke(newEntities);
        }

        return true;
    }

    private static bool HasNearbyEntitiesChanged(IReadOnlyList<EntityInfo> old, IReadOnlyList<EntityInfo> newList)
    {
        if (old.Count != newList.Count)
            return true;

        // Simple check: compare names and distances
        for (int i = 0; i < old.Count; i++)
        {
            if (old[i].Name != newList[i].Name ||
                Math.Abs(old[i].Distance - newList[i].Distance) > 0.5f ||
                old[i].IsDead != newList[i].IsDead ||
                Math.Abs(old[i].HealthPercent - newList[i].HealthPercent) > 1f)
            {
                return true;
            }
        }

        return false;
    }

    private static EntityInfo CreateEntityInfoFromCharacter(Character character, float distance)
    {
        var stats = character.MyStats;
        var position = character.transform.position;

        // Determine entity type based on faction and other properties
        EntityType entityType;
        if (character.Dead)
        {
            // Could be a corpse if it's an enemy
            entityType = character.MyFaction == Faction.Enemy ? EntityType.Corpse : EntityType.Mob;
        }
        else if (character.MyFaction == Faction.Friendly || character.MyFaction == Faction.Player)
        {
            entityType = EntityType.NPC;
        }
        else
        {
            entityType = EntityType.Mob;
        }

        return new EntityInfo(
            name: character.CharacterName ?? "Unknown",
            type: entityType,
            position: new PlayerPosition(position),
            level: stats?.Level ?? 0,
            hostility: ConvertFactionToHostility(character.MyFaction),
            currentHealth: stats?.CurrentHP ?? 0,
            maxHealth: stats?.MaxHP ?? 0,
            isDead: character.Dead,
            distance: distance
        );
    }

    private static EntityInfo CreateEntityInfoFromNPC(NPC npc, float distance)
    {
        var stats = npc.MyStats;
        var position = npc.transform.position;

        return new EntityInfo(
            name: npc.CharacterName ?? "Unknown NPC",
            type: EntityType.NPC,
            position: new PlayerPosition(position),
            level: stats?.Level ?? 0,
            hostility: TargetHostility.Friendly,
            currentHealth: stats?.CurrentHP ?? 0,
            maxHealth: stats?.MaxHP ?? 0,
            isDead: npc.Dead,
            distance: distance
        );
    }

    private static EntityInfo CreateEntityInfoFromNode(ResourceNode node, float distance)
    {
        var position = node.transform.position;

        return new EntityInfo(
            name: node.NodeName ?? "Resource Node",
            type: EntityType.Node,
            position: new PlayerPosition(position),
            level: 0,
            hostility: TargetHostility.Neutral,
            currentHealth: 0,
            maxHealth: 0,
            isDead: false,
            distance: distance
        );
    }

    private static EntityInfo CreateEntityInfoFromCorpse(LootableCorpse corpse, float distance)
    {
        var position = corpse.transform.position;
        var originalChar = corpse.OriginalCharacter;

        return new EntityInfo(
            name: corpse.CorpseName ?? originalChar?.CharacterName ?? "Corpse",
            type: EntityType.Corpse,
            position: new PlayerPosition(position),
            level: originalChar?.MyStats?.Level ?? 0,
            hostility: TargetHostility.Neutral,
            currentHealth: 0,
            maxHealth: 0,
            isDead: true,
            distance: distance
        );
    }

    #endregion

    #region Inventory Reading

    /// <summary>
    /// Gets the player's current inventory state.
    /// Returns empty inventory if player is not loaded.
    /// </summary>
    public PlayerInventory GetPlayerInventory()
    {
        try
        {
            var playerInv = GameData.PlayerInv;
            if (playerInv == null)
                return PlayerInventory.Empty;

            var allSlots = playerInv.ALLSLOTS;
            if (allSlots == null)
                return PlayerInventory.Empty;

            int totalSlots = allSlots.Length;
            int freeSlots = 0;
            var items = new List<ItemInfo>();

            for (int i = 0; i < allSlots.Length; i++)
            {
                var slot = allSlots[i];
                if (slot == null)
                {
                    // Null slot might indicate an unavailable slot
                    continue;
                }

                if (slot.Item == null || ReferenceEquals(slot.Item, GameStubs.PlayerInventory.Empty))
                {
                    freeSlots++;
                }
                else
                {
                    var itemInfo = CreateItemInfoFromSlot(slot, i);
                    items.Add(itemInfo);
                }
            }

            var inventory = new PlayerInventory(
                totalSlots: totalSlots,
                freeSlots: freeSlots,
                items: items
            );

            lock (_inventoryLock)
            {
                _lastInventory = inventory;
                _lastInventoryUpdate = DateTime.UtcNow;
            }

            return inventory;
        }
        catch (Exception)
        {
            // Game state not available
            return PlayerInventory.Empty;
        }
    }

    /// <summary>
    /// Gets the cached inventory state (from last update).
    /// Useful for checking inventory without polling the game.
    /// </summary>
    public PlayerInventory? GetCachedInventory()
    {
        lock (_inventoryLock)
        {
            if (_lastInventoryUpdate == default)
                return null;

            return _lastInventory;
        }
    }

    /// <summary>
    /// Gets the time since inventory was last updated.
    /// </summary>
    public TimeSpan TimeSinceLastInventoryUpdate
    {
        get
        {
            lock (_inventoryLock)
            {
                if (_lastInventoryUpdate == default)
                    return TimeSpan.MaxValue;

                return DateTime.UtcNow - _lastInventoryUpdate;
            }
        }
    }

    /// <summary>
    /// Updates the inventory cache and raises events if inventory changed.
    /// </summary>
    /// <returns>True if inventory was updated successfully, false if game state unavailable.</returns>
    public bool UpdateInventory()
    {
        PlayerInventory? oldInventory;
        lock (_inventoryLock)
        {
            oldInventory = _lastInventoryUpdate != default ? _lastInventory : (PlayerInventory?)null;
        }

        var newInventory = GetPlayerInventory();

        if (oldInventory == null || HasInventoryChanged(oldInventory.Value, newInventory))
        {
            OnInventoryChanged?.Invoke(newInventory);
        }

        return true;
    }

    private static bool HasInventoryChanged(PlayerInventory old, PlayerInventory newInventory)
    {
        // Quick check: slot counts
        if (old.TotalSlots != newInventory.TotalSlots ||
            old.FreeSlots != newInventory.FreeSlots)
            return true;

        // Check item count
        if (old.Items.Count != newInventory.Items.Count)
            return true;

        // Detailed item comparison (for changes in quantity or new items)
        for (int i = 0; i < newInventory.Items.Count; i++)
        {
            var oldItem = i < old.Items.Count ? old.Items[i] : default;
            var newItem = newInventory.Items[i];

            if (!oldItem.Equals(newItem))
                return true;
        }

        return false;
    }

    private static ItemInfo CreateItemInfoFromSlot(InventorySlot slot, int slotIndex)
    {
        var item = slot.Item;

        // Convert GameStubs.ItemQuality to GameState.ItemQuality
        GameState.ItemQuality quality = GameState.ItemQuality.Common;
        if (item?.Quality != null)
        {
            quality = (GameState.ItemQuality)item.Quality;
        }

        return new ItemInfo(
            name: item?.ItemName ?? "Unknown Item",
            quantity: slot.Quantity,
            quality: quality,
            slotIndex: slotIndex,
            itemId: item?.ItemId,
            maxStackSize: item?.MaxStackSize ?? 1
        );
    }

    #endregion

    #region Buff/Debuff Reading

    /// <summary>
    /// Gets the player's current buff/debuff state.
    /// Returns empty buff state if player is not loaded.
    /// </summary>
    public BuffState GetPlayerBuffs()
    {
        try
        {
            var playerControl = GameData.PlayerControl;
            if (playerControl == null)
                return BuffState.Empty;

            var character = playerControl.Myself;
            if (character == null)
                return BuffState.Empty;

            return GetBuffStateFromCharacter(character);
        }
        catch (Exception)
        {
            return BuffState.Empty;
        }
    }

    /// <summary>
    /// Gets the cached player buff state (from last update).
    /// </summary>
    public BuffState? GetCachedPlayerBuffs()
    {
        lock (_playerBuffsLock)
        {
            if (_lastPlayerBuffsUpdate == default)
                return null;

            return _lastPlayerBuffs;
        }
    }

    /// <summary>
    /// Gets the time since player buffs were last updated.
    /// </summary>
    public TimeSpan TimeSinceLastPlayerBuffsUpdate
    {
        get
        {
            lock (_playerBuffsLock)
            {
                if (_lastPlayerBuffsUpdate == default)
                    return TimeSpan.MaxValue;

                return DateTime.UtcNow - _lastPlayerBuffsUpdate;
            }
        }
    }

    /// <summary>
    /// Updates the player buff cache and raises events if buffs changed.
    /// </summary>
    public bool UpdatePlayerBuffs()
    {
        BuffState? oldBuffs;
        lock (_playerBuffsLock)
        {
            oldBuffs = _lastPlayerBuffsUpdate != default ? _lastPlayerBuffs : (BuffState?)null;
        }

        var newBuffs = GetPlayerBuffs();

        if (oldBuffs == null || HasBuffStateChanged(oldBuffs.Value, newBuffs))
        {
            lock (_playerBuffsLock)
            {
                _lastPlayerBuffs = newBuffs;
                _lastPlayerBuffsUpdate = DateTime.UtcNow;
            }
            OnPlayerBuffsChanged?.Invoke(newBuffs);
        }

        return true;
    }

    /// <summary>
    /// Gets the target's current buff/debuff state.
    /// Returns empty buff state if no target or target not loaded.
    /// </summary>
    public BuffState GetTargetBuffs()
    {
        try
        {
            var playerControl = GameData.PlayerControl;
            if (playerControl == null)
                return BuffState.Empty;

            var target = playerControl.CurrentTarget;
            if (target == null)
                return BuffState.Empty;

            return GetBuffStateFromCharacter(target);
        }
        catch (Exception)
        {
            return BuffState.Empty;
        }
    }

    /// <summary>
    /// Gets the cached target buff state (from last update).
    /// </summary>
    public BuffState? GetCachedTargetBuffs()
    {
        lock (_targetBuffsLock)
        {
            if (_lastTargetBuffsUpdate == default)
                return null;

            return _lastTargetBuffs;
        }
    }

    /// <summary>
    /// Gets the time since target buffs were last updated.
    /// </summary>
    public TimeSpan TimeSinceLastTargetBuffsUpdate
    {
        get
        {
            lock (_targetBuffsLock)
            {
                if (_lastTargetBuffsUpdate == default)
                    return TimeSpan.MaxValue;

                return DateTime.UtcNow - _lastTargetBuffsUpdate;
            }
        }
    }

    /// <summary>
    /// Updates the target buff cache and raises events if buffs changed.
    /// </summary>
    public bool UpdateTargetBuffs()
    {
        BuffState? oldBuffs;
        lock (_targetBuffsLock)
        {
            oldBuffs = _lastTargetBuffsUpdate != default ? _lastTargetBuffs : (BuffState?)null;
        }

        var newBuffs = GetTargetBuffs();

        if (oldBuffs == null || HasBuffStateChanged(oldBuffs.Value, newBuffs))
        {
            lock (_targetBuffsLock)
            {
                _lastTargetBuffs = newBuffs;
                _lastTargetBuffsUpdate = DateTime.UtcNow;
            }
            OnTargetBuffsChanged?.Invoke(newBuffs);
        }

        return true;
    }

    private static BuffState GetBuffStateFromCharacter(Character character)
    {
        var buffsComponent = character.MyBuffs;
        if (buffsComponent == null)
            return BuffState.Empty;

        var buffList = new List<BuffInfo>();
        var debuffList = new List<BuffInfo>();

        // Read active buffs
        if (buffsComponent.ActiveBuffs != null)
        {
            foreach (var buff in buffsComponent.ActiveBuffs)
            {
                if (buff != null)
                {
                    buffList.Add(CreateBuffInfo(buff));
                }
            }
        }

        // Read active debuffs
        if (buffsComponent.ActiveDebuffs != null)
        {
            foreach (var debuff in buffsComponent.ActiveDebuffs)
            {
                if (debuff != null)
                {
                    debuffList.Add(CreateBuffInfo(debuff));
                }
            }
        }

        return new BuffState(buffList, debuffList);
    }

    private static BuffInfo CreateBuffInfo(Buff buff)
    {
        return new BuffInfo(
            name: buff.BuffName ?? "Unknown Buff",
            buffId: buff.BuffId,
            remainingDuration: buff.TimeRemaining,
            maxDuration: buff.MaxTime,
            stacks: buff.Stacks,
            iconIndex: buff.IconIndex,
            isDebuff: buff.IsDebuff
        );
    }

    private static bool HasBuffStateChanged(BuffState old, BuffState newState)
    {
        if (old.BuffCount != newState.BuffCount ||
            old.DebuffCount != newState.DebuffCount)
            return true;

        // Check buffs
        for (int i = 0; i < newState.Buffs.Count; i++)
        {
            var oldBuff = i < old.Buffs.Count ? old.Buffs[i] : default;
            var newBuff = newState.Buffs[i];

            if (!oldBuff.Equals(newBuff))
                return true;
        }

        // Check debuffs
        for (int i = 0; i < newState.Debuffs.Count; i++)
        {
            var oldDebuff = i < old.Debuffs.Count ? old.Debuffs[i] : default;
            var newDebuff = newState.Debuffs[i];

            if (!oldDebuff.Equals(newDebuff))
                return true;
        }

        return false;
    }

    #endregion

    #region Private Helpers

    /// <summary>
    /// Attempts to get the PlayerControl transform from the game.
    /// Returns null if not available.
    /// </summary>
    private static Transform? GetPlayerControlTransform()
    {
        try
        {
            // GameData.PlayerControl is the player controller component
            // Its transform.position gives us the world position
            var playerControl = GameData.PlayerControl;
            return playerControl?.transform;
        }
        catch (Exception)
        {
            // Game state not available (e.g., not in game, loading screen)
            return null;
        }
    }

    /// <summary>
    /// Attempts to get the player's CharacterStats from the game.
    /// Returns null if not available.
    /// </summary>
    private static CharacterStats? GetPlayerStats()
    {
        try
        {
            // GameData.PlayerControl.Myself is the player's Character component
            // Character.MyStats gives us the CharacterStats with HP, MP, Level, XP
            var playerControl = GameData.PlayerControl;
            return playerControl?.Myself?.MyStats;
        }
        catch (Exception)
        {
            // Game state not available (e.g., not in game, loading screen)
            return null;
        }
    }

    #endregion
}
