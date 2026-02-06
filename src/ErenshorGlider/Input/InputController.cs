using System;
using ErenshorGlider.GameState;

#if !USE_REAL_GAME_TYPES
using ErenshorGlider.GameStubs;
#endif

namespace ErenshorGlider.Input;

/// <summary>
/// Controls player character movement via simulated keyboard inputs.
/// Uses Unity's Input system to send key presses and releases.
/// </summary>
public class InputController
{
    private readonly PositionTracker? _positionTracker;

    /// <summary>
    /// Creates a new InputController instance.
    /// </summary>
    /// <param name="positionTracker">Optional PositionTracker for movement feedback.</param>
    public InputController(PositionTracker? positionTracker = null)
    {
        _positionTracker = positionTracker;
    }

    /// <summary>
    /// Sends a forward movement input (W key).
    /// </summary>
    /// <param name="duration">Optional duration in seconds. If null, sends a single press.</param>
    public void MoveForward(float? duration = null)
    {
        if (duration.HasValue)
        {
            PressKey(KeyCode.W);
            System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(duration.Value)).ContinueWith(_ => ReleaseKey(KeyCode.W));
        }
        else
        {
            PressKey(KeyCode.W);
        }
    }

    /// <summary>
    /// Sends a backward movement input (S key).
    /// </summary>
    /// <param name="duration">Optional duration in seconds. If null, sends a single press.</param>
    public void MoveBackward(float? duration = null)
    {
        if (duration.HasValue)
        {
            PressKey(KeyCode.S);
            System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(duration.Value)).ContinueWith(_ => ReleaseKey(KeyCode.S));
        }
        else
        {
            PressKey(KeyCode.S);
        }
    }

    /// <summary>
    /// Sends a strafe left input (A key).
    /// </summary>
    /// <param name="duration">Optional duration in seconds. If null, sends a single press.</param>
    public void StrafeLeft(float? duration = null)
    {
        if (duration.HasValue)
        {
            PressKey(KeyCode.A);
            System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(duration.Value)).ContinueWith(_ => ReleaseKey(KeyCode.A));
        }
        else
        {
            PressKey(KeyCode.A);
        }
    }

    /// <summary>
    /// Sends a strafe right input (D key).
    /// </summary>
    /// <param name="duration">Optional duration in seconds. If null, sends a single press.</param>
    public void StrafeRight(float? duration = null)
    {
        if (duration.HasValue)
        {
            PressKey(KeyCode.D);
            System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(duration.Value)).ContinueWith(_ => ReleaseKey(KeyCode.D));
        }
        else
        {
            PressKey(KeyCode.D);
        }
    }

    /// <summary>
    /// Sends a turn left input (Left Arrow or Q key).
    /// </summary>
    /// <param name="duration">Optional duration in seconds. If null, sends a single press.</param>
    public void TurnLeft(float? duration = null)
    {
        if (duration.HasValue)
        {
            PressKey(KeyCode.LeftArrow);
            System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(duration.Value)).ContinueWith(_ => ReleaseKey(KeyCode.LeftArrow));
        }
        else
        {
            PressKey(KeyCode.LeftArrow);
        }
    }

    /// <summary>
    /// Sends a turn right input (Right Arrow or E key).
    /// </summary>
    /// <param name="duration">Optional duration in seconds. If null, sends a single press.</param>
    public void TurnRight(float? duration = null)
    {
        if (duration.HasValue)
        {
            PressKey(KeyCode.RightArrow);
            System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(duration.Value)).ContinueWith(_ => ReleaseKey(KeyCode.RightArrow));
        }
        else
        {
            PressKey(KeyCode.RightArrow);
        }
    }

    /// <summary>
    /// Sends a jump input (Space key).
    /// </summary>
    public void Jump()
    {
        PressKey(KeyCode.Space);
    }

    /// <summary>
    /// Stops all movement inputs.
    /// </summary>
    public void StopAllMovement()
    {
        ReleaseKey(KeyCode.W);
        ReleaseKey(KeyCode.S);
        ReleaseKey(KeyCode.A);
        ReleaseKey(KeyCode.D);
        ReleaseKey(KeyCode.LeftArrow);
        ReleaseKey(KeyCode.RightArrow);
    }

    #region Targeting

    /// <summary>
    /// Targets the nearest enemy (Tab key by default).
    /// </summary>
    public void TargetNearestEnemy()
    {
        PressKey(KeyCode.Tab);
        System.Threading.Tasks.Task.Delay(50).ContinueWith(_ => ReleaseKey(KeyCode.Tab));
    }

    /// <summary>
    /// Clears the current target (Escape key).
    /// </summary>
    public void ClearTarget()
    {
        PressKey(KeyCode.Escape);
        System.Threading.Tasks.Task.Delay(50).ContinueWith(_ => ReleaseKey(KeyCode.Escape));
    }

    /// <summary>
    /// Targets a specific entity by reference.
    /// This uses game-specific targeting APIs rather than keyboard input.
    /// </summary>
    /// <param name="entityInfo">The entity to target.</param>
    public void TargetEntity(in GameState.EntityInfo entityInfo)
    {
#if !USE_REAL_GAME_TYPES
        // Stub implementation - raise event for test handling
        OnTargetEntityRequested?.Invoke(entityInfo);
#else
        // Real implementation - find and target the entity by instance ID
        TargetEntityById(entityInfo.InstanceId);
#endif
    }

    /// <summary>
    /// Targets a specific entity by its Unity instance ID.
    /// </summary>
    /// <param name="instanceId">The Unity instance ID of the entity.</param>
    public void TargetEntityById(int instanceId)
    {
#if !USE_REAL_GAME_TYPES
        // Stub implementation - raise event for test handling
        OnTargetEntityByIdRequested?.Invoke(instanceId);
#else
        // Real implementation - find the entity and set as target
        try
        {
            if (GameData.PlayerControl == null)
                return;

            // Search for the entity by instance ID
            // We need to check both Character and NPC types
            var entity = FindEntityByInstanceId(instanceId);
            if (entity != null)
            {
                // Set as current target
                GameData.PlayerControl.CurrentTarget = entity;
            }
        }
        catch
        {
            // Gracefully handle errors during scene transitions
            // Fall back to event-based approach
            OnTargetEntityByIdRequested?.Invoke(instanceId);
        }
#endif
    }

#if USE_REAL_GAME_TYPES
    /// <summary>
    /// Finds an entity by its Unity instance ID.
    /// Searches both Character and NPC objects.
    /// </summary>
    /// <param name="instanceId">The Unity instance ID to search for.</param>
    /// <returns>The found entity, or null if not found.</returns>
    private static Character? FindEntityByInstanceId(int instanceId)
    {
        // Search all Character objects (includes NPCs since they inherit from Character)
        var allCharacters = UnityEngine.Object.FindObjectsOfType<Character>();
        foreach (var character in allCharacters)
        {
            if (character != null && character.gameObject != null &&
                character.gameObject.GetInstanceID() == instanceId)
            {
                return character;
            }
        }

        return null;
    }
#endif

    /// <summary>
    /// Event raised when a specific entity targeting is requested.
    /// </summary>
    public event Action<GameState.EntityInfo>? OnTargetEntityRequested;

    /// <summary>
    /// Event raised when targeting by instance ID is requested.
    /// </summary>
    public event Action<int>? OnTargetEntityByIdRequested;

    #endregion

    #region Interaction

    /// <summary>
    /// Interacts with the current target (loot, talk, gather).
    /// Typically the Enter key or F key in MMO-style games.
    /// </summary>
    public void Interact()
    {
        PressKey(KeyCode.Enter);
        System.Threading.Tasks.Task.Delay(50).ContinueWith(_ => ReleaseKey(KeyCode.Enter));
    }

    /// <summary>
    /// Interacts with a specific entity.
    /// Combines targeting the entity and then interacting.
    /// </summary>
    /// <param name="entityInfo">The entity to interact with.</param>
    public void InteractWith(in GameState.EntityInfo entityInfo)
    {
        TargetEntity(entityInfo);
        // Small delay to allow target to register
        System.Threading.Tasks.Task.Delay(100).ContinueWith(_ => Interact());
    }

    /// <summary>
    /// Loots the current target's corpse.
    /// </summary>
    public void Loot()
    {
        // Same as interact for most games
        Interact();
    }

    /// <summary>
    /// Opens a conversation with an NPC.
    /// </summary>
    public void Talk()
    {
        Interact();
    }

    /// <summary>
    /// Gathers a resource node.
    /// </summary>
    public void Gather()
    {
        Interact();
    }

    /// <summary>
    /// Cancels the current interaction (ESC key).
    /// </summary>
    public void CancelInteraction()
    {
        PressKey(KeyCode.Escape);
        System.Threading.Tasks.Task.Delay(50).ContinueWith(_ => ReleaseKey(KeyCode.Escape));
    }

    #endregion

    #region Abilities

    /// <summary>
    /// Activates an ability by its keybind slot (1-9).
    /// </summary>
    /// <param name="slot">The hotbar slot number (1-9).</param>
    /// <param name="delayMs">Optional delay after press in milliseconds.</param>
    public void UseAbilitySlot(int slot, int delayMs = 50)
    {
        KeyCode key = slot switch
        {
            1 => KeyCode.Alpha1,
            2 => KeyCode.Alpha2,
            3 => KeyCode.Alpha3,
            4 => KeyCode.Alpha4,
            5 => KeyCode.Alpha5,
            6 => KeyCode.Alpha6,
            7 => KeyCode.Alpha7,
            8 => KeyCode.Alpha8,
            9 => KeyCode.Alpha9,
            _ => KeyCode.Alpha1
        };

        int actualDelay = ApplyRandomization(delayMs);
        PressKey(key);
        System.Threading.Tasks.Task.Delay(actualDelay).ContinueWith(_ => ReleaseKey(key));
    }

    /// <summary>
    /// Activates an ability by its keybind.
    /// </summary>
    /// <param name="key">The key bound to the ability.</param>
    /// <param name="delayMs">Optional delay after press in milliseconds.</param>
    public void UseAbility(KeyCode key, int delayMs = 50)
    {
        int actualDelay = ApplyRandomization(delayMs);
        PressKey(key);
        System.Threading.Tasks.Task.Delay(actualDelay).ContinueWith(_ => ReleaseKey(key));
    }

    /// <summary>
    /// Activates an ability by its spell/skill ID.
    /// This requires game-specific APIs to trigger the ability directly.
    /// </summary>
    /// <param name="abilityId">The spell/skill ID.</param>
    public void UseAbilityById(string abilityId)
    {
        OnAbilityByIdRequested?.Invoke(abilityId);
    }

    /// <summary>
    /// Event raised when ability activation by ID is requested.
    /// </summary>
    public event Action<string>? OnAbilityByIdRequested;

    #endregion

    #region Humanization

    /// <summary>
    /// Gets or sets the input delay in milliseconds.
    /// </summary>
    public int InputDelayMs { get; set; } = 50;

    /// <summary>
    /// Gets or sets the randomization range for input delay in milliseconds.
    /// Humanization adds +/- this amount to the base delay.
    /// </summary>
    public int RandomizationRangeMs { get; set; } = 25;

    /// <summary>
    /// Gets or sets the random number generator for humanization.
    /// </summary>
    public Random RandomGenerator { get; set; } = new();

    /// <summary>
    /// Applies randomization to a delay value for humanization.
    /// </summary>
    /// <param name="baseDelayMs">The base delay in milliseconds.</param>
    /// <returns>The randomized delay.</returns>
    private int ApplyRandomization(int baseDelayMs)
    {
        if (RandomizationRangeMs <= 0)
            return baseDelayMs;

        int variation = RandomGenerator.Next(-RandomizationRangeMs, RandomizationRangeMs + 1);
        return Math.Max(10, baseDelayMs + variation);
    }

    /// <summary>
    /// Sets the randomization seed for reproducible behavior.
    /// </summary>
    public void SetRandomizationSeed(int seed)
    {
        RandomGenerator = new Random(seed);
    }

    #endregion

    /// <summary>
    /// Gets or sets whether to use Windows API input simulation.
    /// When true, uses SendInput for real keyboard events.
    /// When false, only tracks key state internally.
    /// </summary>
    public bool UseWindowsInputSimulation { get; set; } = true;

    /// <summary>
    /// Presses a key down.
    /// Uses Windows API SendInput when UseWindowsInputSimulation is true.
    /// </summary>
    public void PressKey(KeyCode key)
    {
        // Track internal state for game logic that checks IsKeyPressed
        KeyStates[key] = true;

        // Send actual Windows input event if enabled
        if (UseWindowsInputSimulation)
        {
            WindowsInputSimulator.PressKey(key);
        }

        OnKeyStateChanged?.Invoke(key, true);
    }

    /// <summary>
    /// Releases a key.
    /// Uses Windows API SendInput when UseWindowsInputSimulation is true.
    /// </summary>
    public void ReleaseKey(KeyCode key)
    {
        // Track internal state for game logic that checks IsKeyPressed
        KeyStates[key] = false;

        // Send actual Windows input event if enabled
        if (UseWindowsInputSimulation)
        {
            WindowsInputSimulator.ReleaseKey(key);
        }

        OnKeyStateChanged?.Invoke(key, false);
    }

    /// <summary>
    /// Gets whether a key is currently pressed.
    /// </summary>
    public bool IsKeyPressed(KeyCode key)
    {
        return KeyStates.TryGetValue(key, out var pressed) && pressed;
    }

    /// <summary>
    /// Event raised when a key state changes.
    /// </summary>
    public event Action<KeyCode, bool>? OnKeyStateChanged;

    /// <summary>
    /// Internal tracking of key states.
    /// </summary>
    private readonly System.Collections.Generic.Dictionary<KeyCode, bool> KeyStates = new();
}

/// <summary>
/// Represents a keyboard key code.
/// </summary>
public enum KeyCode
{
    /// <summary>W key - forward movement</summary>
    W,
    /// <summary>A key - strafe left</summary>
    A,
    /// <summary>S key - backward movement</summary>
    S,
    /// <summary>D key - strafe right</summary>
    D,
    /// <summary>Space key - jump</summary>
    Space,
    /// <summary>Left Arrow - turn left</summary>
    LeftArrow,
    /// <summary>Right Arrow - turn right</summary>
    RightArrow,
    /// <summary>Up Arrow - camera up/page up</summary>
    UpArrow,
    /// <summary>Down Arrow - camera down/page down</summary>
    DownArrow,
    /// <summary>Q key - alternative turn left</summary>
    Q,
    /// <summary>E key - alternative turn right</summary>
    E,
    /// <summary>Tab key - target nearest enemy</summary>
    Tab,
    /// <summary>Enter key - interact/confirm</summary>
    Enter,
    /// <summary>Escape key - cancel/close</summary>
    Escape,
    /// <summary>Number keys 0-9 for ability bar</summary>
    Alpha0,
    Alpha1,
    Alpha2,
    Alpha3,
    Alpha4,
    Alpha5,
    Alpha6,
    Alpha7,
    Alpha8,
    Alpha9,
    /// <summary>Function keys</summary>
    F1,
    F2,
    F3,
    F4,
    F5,
    F6,
    F7,
    F8,
    F9,
    F10,
    F11,
    F12
}
