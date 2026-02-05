using BepInEx;
using BepInEx.Logging;
using ErenshorGlider.GameState;

namespace ErenshorGlider;

/// <summary>
/// Main BepInEx plugin entry point for Erenshor Glider.
/// </summary>
[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger { get; private set; } = null!;

    /// <summary>
    /// Gets the position tracker for reading player position.
    /// </summary>
    public static PositionTracker? PositionTracker { get; private set; }

    private void Awake()
    {
        Logger = base.Logger;
        Logger.LogInfo($"{PluginInfo.PLUGIN_NAME} v{PluginInfo.PLUGIN_VERSION} loaded!");

        // Initialize position tracker (updates at 10Hz by default)
        PositionTracker = gameObject.AddComponent<PositionTracker>();
        PositionTracker.OnPositionUpdated += OnPlayerPositionUpdated;

        Logger.LogInfo("Position tracker initialized (10Hz update rate)");
    }

    private void OnPlayerPositionUpdated(PlayerPosition position)
    {
        // Log position changes at debug level (can be disabled via BepInEx config)
        Logger.LogDebug($"Player position: {position}");
    }
}
