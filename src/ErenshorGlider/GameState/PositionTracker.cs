using System;
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

    private void Awake()
    {
        _gameStateReader = new GameStateReader();
        _gameStateReader.OnPositionChanged += HandlePositionChanged;
        _gameStateReader.OnVitalsChanged += HandleVitalsChanged;
        _gameStateReader.OnCombatStateChanged += HandleCombatStateChanged;
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

    private void OnDestroy()
    {
        if (_gameStateReader != null)
        {
            _gameStateReader.OnPositionChanged -= HandlePositionChanged;
            _gameStateReader.OnVitalsChanged -= HandleVitalsChanged;
            _gameStateReader.OnCombatStateChanged -= HandleCombatStateChanged;
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
