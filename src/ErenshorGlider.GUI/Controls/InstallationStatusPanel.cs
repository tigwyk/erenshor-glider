using System;
using System.Drawing;
using System.Windows.Forms;
using ErenshorGlider.GUI.Installation;

namespace ErenshorGlider.GUI.Controls;

/// <summary>
/// Panel displaying installation status for BepInEx and the ErenshorGlider plugin.
/// </summary>
public class InstallationStatusPanel : Panel
{
    private readonly Label _bepInExStatusLabel;
    private readonly Label _bepInExIconLabel;
    private readonly Label _bepInExVersionLabel;
    private readonly Label _pluginStatusLabel;
    private readonly Label _pluginIconLabel;
    private readonly Label _pluginVersionLabel;
    private readonly Label _erenshorPathLabel;
    private readonly Button _refreshButton;
    private readonly IInstallationService _installationService;
    private readonly Timer _updateTimer;
    private InstallationStatus _bepInExStatus;
    private InstallationStatus _pluginStatus;
    private string? _bepInExVersion;
    private string? _pluginVersion;

    /// <summary>
    /// Creates a new InstallationStatusPanel.
    /// </summary>
    /// <param name="installationService">The installation service to use.</param>
    public InstallationStatusPanel(IInstallationService installationService)
    {
        _installationService = installationService ?? throw new ArgumentNullException(nameof(installationService));

        BackColor = Color.FromArgb(40, 40, 43);
        BorderStyle = BorderStyle.FixedSingle;
        Padding = new Padding(10);
        MinimumSize = new Size(300, 140);

        // Create controls
        _bepInExIconLabel = CreateIconLabel("✗");
        _bepInExStatusLabel = CreateStatusLabel("BepInEx:");
        _bepInExVersionLabel = CreateVersionLabel("Not Installed");

        _pluginIconLabel = CreateIconLabel("✗");
        _pluginStatusLabel = CreateStatusLabel("Plugin:");
        _pluginVersionLabel = CreateVersionLabel("Not Installed");

        _erenshorPathLabel = CreatePathLabel();

        _refreshButton = CreateRefreshButton();

        // Layout controls
        LayoutControls();

        // Wire up events
        WireUpEvents();

        // Set up update timer (check every 5 seconds)
        _updateTimer = new Timer { Interval = 5000 };
#pragma warning disable CS4014 // Fire and forget is acceptable for timer-triggered async
        _updateTimer.Tick += async (s, e) => await RefreshStatusAsync();
#pragma warning restore CS4014
        _updateTimer.Start();

        // Initial status check (fire and forget from constructor)
#pragma warning disable CS4014
        _ = RefreshStatusAsync();
#pragma warning restore CS4014
    }

    /// <summary>
    /// Creates an icon label for status indication.
    /// </summary>
    private Label CreateIconLabel(string text)
    {
        return new Label
        {
            Text = text,
            ForeColor = Color.FromArgb(150, 150, 150),
            Font = new Font("Segoe UI", 14F),
            Width = 25,
            TextAlign = ContentAlignment.MiddleCenter
        };
    }

    /// <summary>
    /// Creates a status label.
    /// </summary>
    private Label CreateStatusLabel(string text)
    {
        return new Label
        {
            Text = text,
            ForeColor = Color.FromArgb(200, 200, 200),
            Font = new Font("Segoe UI", 9F),
            Height = 20
        };
    }

    /// <summary>
    /// Creates a version label.
    /// </summary>
    private Label CreateVersionLabel(string text)
    {
        return new Label
        {
            Text = text,
            ForeColor = Color.FromArgb(150, 150, 150),
            Font = new Font("Segoe UI", 8F),
            Height = 18
        };
    }

    /// <summary>
    /// Creates the path label.
    /// </summary>
    private Label CreatePathLabel()
    {
        return new Label
        {
            Text = "Erenshor: Not configured",
            ForeColor = Color.FromArgb(150, 150, 150),
            Font = new Font("Consolas", 8F),
            Height = 18
        };
    }

    /// <summary>
    /// Creates the refresh button.
    /// </summary>
    private Button CreateRefreshButton()
    {
        return new Button
        {
            Text = "Refresh",
            BackColor = Color.FromArgb(70, 130, 180),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8F),
            Size = new Size(70, 24),
            UseVisualStyleBackColor = false,
            Cursor = Cursors.Hand
        };
    }

    /// <summary>
    /// Layouts the controls on the panel.
    /// </summary>
    private void LayoutControls()
    {
        int rowHeight = 22;
        int y = 10;

        // BepInEx row
        _bepInExIconLabel.Location = new Point(10, y);
        _bepInExStatusLabel.Location = new Point(40, y);
        _bepInExVersionLabel.Location = new Point(110, y);
        _bepInExVersionLabel.Width = Width - 150;

        Controls.Add(_bepInExIconLabel);
        Controls.Add(_bepInExStatusLabel);
        Controls.Add(_bepInExVersionLabel);

        y += rowHeight;

        // Plugin row
        _pluginIconLabel.Location = new Point(10, y);
        _pluginStatusLabel.Location = new Point(40, y);
        _pluginVersionLabel.Location = new Point(110, y);
        _pluginVersionLabel.Width = Width - 150;

        Controls.Add(_pluginIconLabel);
        Controls.Add(_pluginStatusLabel);
        Controls.Add(_pluginVersionLabel);

        y += rowHeight;

        // Erenshor path row
        _erenshorPathLabel.Location = new Point(10, y);
        _erenshorPathLabel.Width = Width - 90;

        Controls.Add(_erenshorPathLabel);

        // Refresh button (top right)
        _refreshButton.Location = new Point(Width - 80, 10);

        Controls.Add(_refreshButton);
    }

    /// <summary>
    /// Wires up event handlers.
    /// </summary>
    private void WireUpEvents()
    {
        _refreshButton.Click += async (s, e) => await RefreshStatusAsync();

        _refreshButton.MouseEnter += (s, e) =>
        {
            if (_refreshButton.Enabled)
                _refreshButton.BackColor = Color.FromArgb(90, 150, 200);
        };

        _refreshButton.MouseLeave += (s, e) =>
        {
            _refreshButton.BackColor = Color.FromArgb(70, 130, 180);
        };

        // Handle resize to reposition controls
        Resize += (s, e) =>
        {
            _bepInExVersionLabel.Width = Width - 150;
            _pluginVersionLabel.Width = Width - 150;
            _erenshorPathLabel.Width = Width - 90;
            _refreshButton.Location = new Point(Width - 80, 10);
        };
    }

    /// <summary>
    /// Refreshes the installation status asynchronously.
    /// </summary>
    public async System.Threading.Tasks.Task RefreshStatusAsync()
    {
        if (!IsHandleCreated || IsDisposed)
            return;

        try
        {
            // Get Erenshor path from config
            var erenshorPath = _installationService.Config?.ErenshorPath;

            if (InvokeRequired)
            {
                Invoke(new Action(() =>
                {
                    _erenshorPathLabel.Text = string.IsNullOrEmpty(erenshorPath)
                        ? "Erenshor: Not configured"
                        : $"Erenshor: {ShortenPath(erenshorPath!, 50)}";
                }));
            }
            else
            {
                _erenshorPathLabel.Text = string.IsNullOrEmpty(erenshorPath)
                    ? "Erenshor: Not configured"
                    : $"Erenshor: {ShortenPath(erenshorPath!, 50)}";
            }

            if (string.IsNullOrEmpty(erenshorPath))
            {
                UpdateStatusDisplay(InstallationStatus.NotInstalled, null, true);
                UpdateStatusDisplay(InstallationStatus.NotInstalled, null, false);
                return;
            }

            // Check BepInEx status
            var bepInExStatus = await _installationService.GetBepInExStatusAsync(erenshorPath!);
            _bepInExStatus = bepInExStatus;
            _bepInExVersion = GetBepInExVersion(erenshorPath!);

            // Check plugin status
            var pluginStatus = await _installationService.GetPluginStatusAsync(erenshorPath!);
            _pluginStatus = pluginStatus;
            _pluginVersion = GetPluginVersion(erenshorPath!);

            // Update UI on UI thread
            if (InvokeRequired)
            {
                Invoke(new Action(() =>
                {
                    UpdateStatusDisplay(_bepInExStatus, _bepInExVersion, true);
                    UpdateStatusDisplay(_pluginStatus, _pluginVersion, false);
                }));
            }
            else
            {
                UpdateStatusDisplay(_bepInExStatus, _bepInExVersion, true);
                UpdateStatusDisplay(_pluginStatus, _pluginVersion, false);
            }
        }
        catch
        {
            // Ignore errors during refresh (likely disposed)
        }
    }

    /// <summary>
    /// Updates the status display for a component.
    /// </summary>
    /// <param name="status">The installation status.</param>
    /// <param name="version">The version string, if available.</param>
    /// <param name="isBepInEx">True if updating BepInEx status, false for plugin.</param>
    private void UpdateStatusDisplay(InstallationStatus status, string? version, bool isBepInEx)
    {
        var iconLabel = isBepInEx ? _bepInExIconLabel : _pluginIconLabel;
        var versionLabel = isBepInEx ? _bepInExVersionLabel : _pluginVersionLabel;

        switch (status)
        {
            case InstallationStatus.NotInstalled:
                iconLabel.Text = "✗";
                iconLabel.ForeColor = Color.FromArgb(180, 80, 80); // Red
                versionLabel.Text = "Not Installed";
                versionLabel.ForeColor = Color.FromArgb(150, 150, 150);
                break;

            case InstallationStatus.Installed:
                iconLabel.Text = "✓";
                iconLabel.ForeColor = Color.FromArgb(100, 180, 100); // Green
                versionLabel.Text = string.IsNullOrEmpty(version) ? "Installed" : $"v{version}";
                versionLabel.ForeColor = Color.FromArgb(150, 200, 150);
                break;

            case InstallationStatus.UpdateAvailable:
                iconLabel.Text = "⚠";
                iconLabel.ForeColor = Color.FromArgb(220, 180, 80); // Yellow
                versionLabel.Text = string.IsNullOrEmpty(version) ? "Update Available" : $"v{version} (Update)";
                versionLabel.ForeColor = Color.FromArgb(220, 200, 100);
                break;
        }
    }

    /// <summary>
    /// Gets the BepInEx version from the installation.
    /// </summary>
    /// <param name="erenshorPath">The Erenshor installation path.</param>
    /// <returns>The version string, or null if not found.</returns>
    private string? GetBepInExVersion(string erenshorPath)
    {
        try
        {
            var versionFile = System.IO.Path.Combine(erenshorPath, "BepInEx", "version.txt");
            if (System.IO.File.Exists(versionFile))
            {
                return System.IO.File.ReadAllText(versionFile).Trim();
            }

            // Fallback: check for core DLL
            var coreDll = System.IO.Path.Combine(erenshorPath, "BepInEx", "core", "BepInEx.dll");
            if (System.IO.File.Exists(coreDll))
            {
                var versionInfo = System.Diagnostics.FileVersionInfo.GetVersionInfo(coreDll);
                return versionInfo.FileVersion ?? "5.x";
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets the plugin version from the installation.
    /// </summary>
    /// <param name="erenshorPath">The Erenshor installation path.</param>
    /// <returns>The version string, or null if not found.</returns>
    private string? GetPluginVersion(string erenshorPath)
    {
        try
        {
            var pluginDll = System.IO.Path.Combine(erenshorPath, "BepInEx", "plugins", "ErenshorGlider.dll");
            if (System.IO.File.Exists(pluginDll))
            {
                var versionInfo = System.Diagnostics.FileVersionInfo.GetVersionInfo(pluginDll);
                return versionInfo.FileVersion ?? "unknown";
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Shortens a file path for display.
    /// </summary>
    /// <param name="path">The path to shorten.</param>
    /// <param name="maxLength">The maximum length.</param>
    /// <returns>The shortened path.</returns>
    private static string ShortenPath(string path, int maxLength)
    {
        if (path.Length <= maxLength)
            return path;

        // Try to keep the filename and truncate the directory
        var fileName = System.IO.Path.GetFileName(path.TrimEnd('\\', '/'));
        var directory = System.IO.Path.GetDirectoryName(path);

        if (directory != null)
        {
            var shortenedDir = directory.Length > maxLength - fileName.Length - 5
                ? "..." + directory.Substring(directory.Length - (maxLength - fileName.Length - 5))
                : directory;

            return System.IO.Path.Combine(shortenedDir, fileName);
        }

        return "..." + path.Substring(path.Length - maxLength);
    }

    /// <summary>
    /// Clean up resources.
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _updateTimer?.Stop();
            _updateTimer?.Dispose();
        }
        base.Dispose(disposing);
    }
}
