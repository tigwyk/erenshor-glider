using System;
using System.Collections.Generic;
using UnityEngine;

namespace ErenshorGlider.GameState;

/// <summary>
/// MonoBehaviour that tracks player position at a configurable update rate.
/// Attaches to the Plugin GameObject to ensure regular updates.
/// </summary>
public class PositionTracker : MonoBehaviour
{
    private GameStateReader? _gameStateReader;
    private float _updateInterval = 0.1f; // 10Hz default (100ms between updates)
    private float _timeSinceLastUpdate;

    /// <summary>
    /// Gets or sets the update interval in seconds.
    /// Default is 0.1 (10Hz). Minimum is 0.01 (100Hz).
    /// </summary>
    public float UpdateInterval
    {
        get => _updateInterval;
        set => _updateInterval = Math.Max(0.01f, value);
    }

    /// <summary>
    /// Gets the current update rate in Hz.
    /// </summary>
    public float UpdateRateHz => 1f / _updateInterval;

    /// <summary>
    /// Gets the GameStateReader instance used by this tracker.
    /// </summary>
    public GameStateReader GameStateReader => _gameStateReader ??= new GameStateReader();

    /// <summary>
    /// Gets the most recent player position, or null if unavailable.
    /// </summary>
    public PlayerPosition? CurrentPosition => GameStateReader.GetCachedPosition();

    /// <summary>
    /// Gets the most recent player vitals, or null if unavailable.
    /// </summary>
    public PlayerVitals? CurrentVitals => GameStateReader.GetCachedVitals();

    /// <summary>
    /// Gets the most recent combat state, or null if unavailable.
    /// </summary>
    public CombatState? CurrentCombatState => GameStateReader.GetCachedCombatState();

    /// <summary>
    /// Gets the most recent target info, or null if unavailable.
    /// </summary>
    public TargetInfo? CurrentTargetInfo => GameStateReader.GetCachedTargetInfo();

    /// <summary>
    /// Gets the most recent nearby entities list, or null if unavailable.
    /// </summary>
    public IReadOnlyList<EntityInfo>? CurrentNearbyEntities => GameStateReader.GetCachedNearbyEntities();

    /// <summary>
    /// Gets the most recent inventory state, or null if unavailable.
    /// </summary>
    public PlayerInventory? CurrentInventory => GameStateReader.GetCachedInventory();

    /// <summary>
    /// Gets the most recent player buff state, or null if unavailable.
    /// </summary>
    public BuffState? CurrentPlayerBuffs => GameStateReader.GetCachedPlayerBuffs();

    /// <summary>
    /// Gets the most recent target buff state, or null if unavailable.
    /// </summary>
    public BuffState? CurrentTargetBuffs => GameStateReader.GetCachedTargetBuffs();

    /// <summary>
    /// Gets or sets the radius for nearby entity detection.
    /// </summary>
    public float NearbyEntitiesRadius
    {
        get => GameStateReader.NearbyEntitiesRadius;
        set => GameStateReader.NearbyEntitiesRadius = value;
    }

    /// <summary>
    /// Event raised when player position is updated.
    /// </summary>
    public event Action<PlayerPosition>? OnPositionUpdated;

    /// <summary>
    /// Event raised when player vitals are updated.
    /// </summary>
    public event Action<PlayerVitals>? OnVitalsUpdated;

    /// <summary>
    /// Event raised when combat state is updated.
    /// </summary>
    public event Action<CombatState>? OnCombatStateUpdated;

    /// <summary>
    /// Event raised when target info is updated.
    /// </summary>
    public event Action<TargetInfo>? OnTargetInfoUpdated;

    /// <summary>
    /// Event raised when nearby entities list is updated.
    /// </summary>
    public event Action<IReadOnlyList<EntityInfo>>? OnNearbyEntitiesUpdated;

    /// <summary>
    /// Event raised when inventory state is updated.
    /// </summary>
    public event Action<PlayerInventory>? OnInventoryUpdated;

    /// <summary>
    /// Event raised when player buffs/debuffs are updated.
    /// </summary>
    public event Action<BuffState>? OnPlayerBuffsUpdated;

    /// <summary>
    /// Event raised when target buffs/debuffs are updated.
    /// </summary>
    public event Action<BuffState>? OnTargetBuffsUpdated;

    private void Awake()
    {
        _gameStateReader = new GameStateReader();
        _gameStateReader.OnPositionChanged += HandlePositionChanged;
        _gameStateReader.OnVitalsChanged += HandleVitalsChanged;
        _gameStateReader.OnCombatStateChanged += HandleCombatStateChanged;
        _gameStateReader.OnTargetInfoChanged += HandleTargetInfoChanged;
        _gameStateReader.OnNearbyEntitiesChanged += HandleNearbyEntitiesChanged;
        _gameStateReader.OnInventoryChanged += HandleInventoryChanged;
        _gameStateReader.OnPlayerBuffsChanged += HandlePlayerBuffsChanged;
        _gameStateReader.OnTargetBuffsChanged += HandleTargetBuffsChanged;
    }

    private void Update()
    {
        _timeSinceLastUpdate += Time.deltaTime;

        if (_timeSinceLastUpdate >= _updateInterval)
        {
            _timeSinceLastUpdate = 0f;
            _gameStateReader?.UpdatePosition();
            _gameStateReader?.UpdateVitals();
            _gameStateReader?.UpdateCombatState();
            _gameStateReader?.UpdateTargetInfo();
            _gameStateReader?.UpdateNearbyEntities();
            // Inventory updates less frequently (on loot/vend) - update every 1 second instead of every 0.1s
            // We'll still call it here but with a check inside to throttle
            _gameStateReader?.UpdateInventory();
            // Buffs/debuffs update - can throttle if needed
            _gameStateReader?.UpdatePlayerBuffs();
            _gameStateReader?.UpdateTargetBuffs();
        }
    }

    private void HandlePositionChanged(PlayerPosition position)
    {
        OnPositionUpdated?.Invoke(position);
    }

    private void HandleVitalsChanged(PlayerVitals vitals)
    {
        OnVitalsUpdated?.Invoke(vitals);
    }

    private void HandleCombatStateChanged(CombatState combatState)
    {
        OnCombatStateUpdated?.Invoke(combatState);
    }

    private void HandleTargetInfoChanged(TargetInfo targetInfo)
    {
        OnTargetInfoUpdated?.Invoke(targetInfo);
    }

    private void HandleNearbyEntitiesChanged(IReadOnlyList<EntityInfo> entities)
    {
        OnNearbyEntitiesUpdated?.Invoke(entities);
    }

    private void HandleInventoryChanged(PlayerInventory inventory)
    {
        OnInventoryUpdated?.Invoke(inventory);
    }

    private void HandlePlayerBuffsChanged(BuffState buffState)
    {
        OnPlayerBuffsUpdated?.Invoke(buffState);
    }

    private void HandleTargetBuffsChanged(BuffState buffState)
    {
        OnTargetBuffsUpdated?.Invoke(buffState);
    }

    private void OnDestroy()
    {
        if (_gameStateReader != null)
        {
            _gameStateReader.OnPositionChanged -= HandlePositionChanged;
            _gameStateReader.OnVitalsChanged -= HandleVitalsChanged;
            _gameStateReader.OnCombatStateChanged -= HandleCombatStateChanged;
            _gameStateReader.OnTargetInfoChanged -= HandleTargetInfoChanged;
            _gameStateReader.OnNearbyEntitiesChanged -= HandleNearbyEntitiesChanged;
            _gameStateReader.OnInventoryChanged -= HandleInventoryChanged;
            _gameStateReader.OnPlayerBuffsChanged -= HandlePlayerBuffsChanged;
            _gameStateReader.OnTargetBuffsChanged -= HandleTargetBuffsChanged;
        }
    }

    /// <summary>
    /// Sets the update rate in Hz.
    /// </summary>
    /// <param name="hz">Update rate in Hz (minimum 1, maximum 100).</param>
    public void SetUpdateRateHz(float hz)
    {
        hz = Mathf.Clamp(hz, 1f, 100f);
        _updateInterval = 1f / hz;
    }
}
