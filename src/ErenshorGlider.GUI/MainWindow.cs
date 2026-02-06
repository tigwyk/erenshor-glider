using System;
using System.Drawing;
using System.Windows.Forms;
using ErenshorGlider.GUI.Controls;
using ErenshorGlider.GUI.Forms;
using ErenshorGlider.GUI.Installation;

namespace ErenshorGlider.GUI;

/// <summary>
/// Main application window for the Erenshor Glider GUI.
/// </summary>
public class MainWindow : Form
{
    private readonly StatusStrip _statusStrip;
    private readonly ToolStripStatusLabel _statusLabel;
    private readonly ToolStripStatusLabel _connectionStatusLabel;
    private readonly Panel _headerPanel;
    private readonly Label _titleLabel;
    private readonly Panel _contentPanel;
    private readonly NotifyIcon _notifyIcon;
    private readonly ContextMenuStrip _trayContextMenu;
    private readonly IBotController _botController;
    private readonly Button _settingsButton;
    private readonly Button _installPluginButton;
    private readonly Button _launchGameButton;
    private readonly IInstallationService? _installationService;

    /// <summary>
    /// Gets the main content panel where other UI components can be added.
    /// </summary>
    public Panel ContentPanel => _contentPanel;

    /// <summary>
    /// Gets the header title label.
    /// </summary>
    public Label TitleLabel => _titleLabel;

    /// <summary>
    /// Event raised when the window is minimized to tray.
    /// </summary>
    public event EventHandler? MinimizedToTray;

    /// <summary>
    /// Event raised when the window is restored from tray.
    /// </summary>
    public event EventHandler? RestoredFromTray;

    /// <summary>
    /// Event raised when the application is exiting.
    /// </summary>
    public event EventHandler? ApplicationExiting;

    /// <summary>
    /// Creates a new MainWindow.
    /// </summary>
    public MainWindow() : this(new MockBotController(), null)
    {
    }

    /// <summary>
    /// Creates a new MainWindow with the specified bot controller.
    /// </summary>
    /// <param name="botController">The bot controller to use.</param>
    public MainWindow(IBotController botController) : this(botController, null)
    {
    }

    /// <summary>
    /// Creates a new MainWindow with the specified bot controller and installation service.
    /// </summary>
    /// <param name="botController">The bot controller to use.</param>
    /// <param name="installationService">The installation service to use.</param>
    public MainWindow(IBotController botController, IInstallationService? installationService)
    {
        _botController = botController ?? throw new ArgumentNullException(nameof(botController));
        _installationService = installationService;

        // Form properties
        Text = "Erenshor Glider";
        Size = new Size(900, 650);
        MinimumSize = new Size(800, 500);
        StartPosition = FormStartPosition.CenterScreen;
        Icon = SystemIcons.Application;

        // Initialize components
        _headerPanel = CreateHeaderPanel();
        _titleLabel = CreateTitleLabel();
        _settingsButton = CreateSettingsButton();
        _installPluginButton = CreateInstallPluginButton();
        _launchGameButton = CreateLaunchGameButton();
        _contentPanel = CreateContentPanel();
        _statusStrip = CreateStatusStrip();
        _statusLabel = CreateStatusLabel();
        _connectionStatusLabel = CreateConnectionStatusLabel();
        _trayContextMenu = CreateTrayContextMenu();
        _notifyIcon = CreateNotifyIcon();

        // Layout controls
        LayoutControls();
        WireUpEvents();
        InitializeBotControls();
        InitializeSelectors();
        InitializeStatusDisplay();
    }

    /// <summary>
    /// Creates the header panel.
    /// </summary>
    private Panel CreateHeaderPanel()
    {
        var panel = new Panel
        {
            BackColor = Color.FromArgb(45, 45, 48),
            Dock = DockStyle.Top,
            Height = 50
        };
        return panel;
    }

    /// <summary>
    /// Creates the title label.
    /// </summary>
    private Label CreateTitleLabel()
    {
        var label = new Label
        {
            Text = "Erenshor Glider",
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 16, FontStyle.Bold),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(15, 0, 0, 0)
        };
        return label;
    }

    /// <summary>
    /// Creates the settings button in the header.
    /// </summary>
    private Button CreateSettingsButton()
    {
        var button = new Button
        {
            Text = "⚙ Settings",
            BackColor = Color.FromArgb(60, 60, 65),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9F),
            Size = new Size(110, 30),
            UseVisualStyleBackColor = false,
            Cursor = Cursors.Hand,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        button.MouseEnter += (s, e) => button.BackColor = Color.FromArgb(80, 80, 85);
        button.MouseLeave += (s, e) => button.BackColor = Color.FromArgb(60, 60, 65);
        button.Click += (s, e) => ShowSettingsDialog();
        return button;
    }

    /// <summary>
    /// Creates the Install Plugin button in the header.
    /// </summary>
    private Button CreateInstallPluginButton()
    {
        var button = new Button
        {
            Text = "Install Plugin",
            BackColor = Color.FromArgb(70, 130, 180),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9F),
            Size = new Size(110, 30),
            UseVisualStyleBackColor = false,
            Cursor = Cursors.Hand,
            Enabled = _installationService != null
        };
        button.MouseEnter += (s, e) =>
        {
            if (button.Enabled)
                button.BackColor = Color.FromArgb(90, 150, 200);
        };
        button.MouseLeave += (s, e) =>
        {
            button.BackColor = Color.FromArgb(70, 130, 180);
        };
        button.Click += async (s, e) => await HandleInstallPluginClick();
        return button;
    }

    /// <summary>
    /// Creates the Launch Game button in the header.
    /// </summary>
    private Button CreateLaunchGameButton()
    {
        var button = new Button
        {
            Text = "Launch Game",
            BackColor = Color.FromArgb(100, 160, 100),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9F),
            Size = new Size(110, 30),
            UseVisualStyleBackColor = false,
            Cursor = Cursors.Hand,
            Enabled = _installationService != null
        };
        button.MouseEnter += (s, e) =>
        {
            if (button.Enabled)
                button.BackColor = Color.FromArgb(120, 180, 120);
        };
        button.MouseLeave += (s, e) =>
        {
            button.BackColor = Color.FromArgb(100, 160, 100);
        };
        button.Click += async (s, e) => await HandleLaunchGameClick();
        return button;
    }

    /// <summary>
    /// Creates the main content panel.
    /// </summary>
    private Panel CreateContentPanel()
    {
        var panel = new Panel
        {
            BackColor = Color.FromArgb(30, 30, 30),
            Dock = DockStyle.Fill,
            Padding = new Padding(10)
        };
        return panel;
    }

    /// <summary>
    /// Creates the status strip.
    /// </summary>
    private StatusStrip CreateStatusStrip()
    {
        var strip = new StatusStrip
        {
            BackColor = Color.FromArgb(45, 45, 48),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9F),
            GripStyle = ToolStripGripStyle.Hidden
        };
        return strip;
    }

    /// <summary>
    /// Creates the status label.
    /// </summary>
    private ToolStripStatusLabel CreateStatusLabel()
    {
        var label = new ToolStripStatusLabel
        {
            Text = "Ready",
            Spring = true,
            TextAlign = ContentAlignment.MiddleLeft
        };
        return label;
    }

    /// <summary>
    /// Creates the connection status label.
    /// </summary>
    private ToolStripStatusLabel CreateConnectionStatusLabel()
    {
        var label = new ToolStripStatusLabel
        {
            Text = "Not Connected",
            ForeColor = Color.FromArgb(255, 100, 100),
            BorderStyle = Border3DStyle.SunkenInner,
            Margin = new Padding(10, 0, 5, 0)
        };
        return label;
    }

    /// <summary>
    /// Creates the system tray context menu.
    /// </summary>
    private ContextMenuStrip CreateTrayContextMenu()
    {
        var menu = new ContextMenuStrip();

        var showItem = new ToolStripMenuItem("Show", null, (s, e) => RestoreFromTray());
        var exitItem = new ToolStripMenuItem("Exit", null, (s, e) => ExitApplication());

        menu.Items.AddRange(new ToolStripItem[] { showItem, new ToolStripSeparator(), exitItem });

        return menu;
    }

    /// <summary>
    /// Creates the notify icon for system tray support.
    /// </summary>
    private NotifyIcon CreateNotifyIcon()
    {
        var icon = new NotifyIcon
        {
            Text = "Erenshor Glider",
            Icon = SystemIcons.Application,
            Visible = false,
            ContextMenuStrip = _trayContextMenu
        };

        icon.DoubleClick += (s, e) => RestoreFromTray();

        return icon;
    }

    /// <summary>
    /// Layouts the controls on the form.
    /// </summary>
    private void LayoutControls()
    {
        // Add title and buttons to header
        _titleLabel.Dock = DockStyle.Left;
        _titleLabel.Width = Width - 370; // Make room for 3 buttons

        // Position buttons from right to left
        _settingsButton.Location = new Point(Width - 120, 10);
        _launchGameButton.Location = new Point(Width - 240, 10);
        _installPluginButton.Location = new Point(Width - 360, 10);

        _headerPanel.Controls.Add(_titleLabel);
        _headerPanel.Controls.Add(_settingsButton);
        _headerPanel.Controls.Add(_installPluginButton);
        _headerPanel.Controls.Add(_launchGameButton);

        // Add controls to form
        Controls.Add(_contentPanel);
        Controls.Add(_headerPanel);
        Controls.Add(_statusStrip);

        // Add labels to status strip
        _statusStrip.Items.AddRange(new ToolStripItem[] { _statusLabel, _connectionStatusLabel });
    }

    /// <summary>
    /// Wires up form events.
    /// </summary>
    private void WireUpEvents()
    {
        // Handle resize to minimize to tray
        Resize += (s, e) =>
        {
            if (WindowState == FormWindowState.Minimized)
            {
                MinimizeToTray();
            }
        };

        // Handle form closing
        FormClosing += (s, e) =>
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                // Instead of closing, minimize to tray
                e.Cancel = true;
                MinimizeToTray();
            }
        };

        // Handle key press for ESC to minimize
        KeyPreview = true;
        KeyDown += (s, e) =>
        {
            if (e.KeyCode == Keys.Escape && e.Modifiers == Keys.None)
            {
                WindowState = FormWindowState.Minimized;
            }
        };

        // Wire up bot controller events
        _botController.BotRunningChanged += (s, e) =>
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => HandleBotRunningChanged(s, e)));
                return;
            }
            HandleBotRunningChanged(s, e);
        };

        // Wire up installation service events
        if (_installationService != null)
        {
            _installationService.GameExited += (s, e) =>
            {
                if (InvokeRequired)
                {
                    Invoke(new Action(() => UpdateGameRunningButtons(false)));
                    return;
                }
                UpdateGameRunningButtons(false);
            };

            // Check for updates on startup (fire and forget, doesn't block UI)
#pragma warning disable CS4014
            _ = CheckForUpdatesOnStartupAsync();
#pragma warning restore CS4014
        }
    }

    /// <summary>
    /// Checks for updates on startup and shows balloon notification if available.
    /// </summary>
    private async System.Threading.Tasks.Task CheckForUpdatesOnStartupAsync()
    {
        try
        {
            var updateResult = await _installationService!.CheckForUpdatesAsync();

            if (updateResult.HasUpdate && !string.IsNullOrEmpty(updateResult.LatestVersion))
            {
                // Show balloon notification for update
                if (InvokeRequired)
                {
                    Invoke(new Action(() =>
                    {
                        ShowBalloonTip(
                            $"Update Available (v{updateResult.LatestVersion})",
                            $"A new version of Erenshor Glider is available.\nCurrent: {updateResult.CurrentVersion} → Latest: {updateResult.LatestVersion}",
                            ToolTipIcon.Info);
                    }));
                }
                else
                {
                    ShowBalloonTip(
                        $"Update Available (v{updateResult.LatestVersion})",
                        $"A new version of Erenshor Glider is available.\nCurrent: {updateResult.CurrentVersion} → Latest: {updateResult.LatestVersion}",
                        ToolTipIcon.Info);
                }
            }
        }
        catch
        {
            // Ignore update check errors on startup
        }
    }

    /// <summary>
    /// Initializes the bot control panel.
    /// </summary>
    private void InitializeBotControls()
    {
        var botControlPanel = new BotControlPanel(_botController)
        {
            Dock = DockStyle.Top
        };
        _contentPanel.Controls.Add(botControlPanel);
    }

    /// <summary>
    /// Initializes the selector panels (profile and waypoint).
    /// </summary>
    private void InitializeSelectors()
    {
        // Profile and waypoint selectors side by side
        var selectorPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 80,
            Padding = new Padding(5)
        };

        var profileSelector = new ProfileSelectorPanel
        {
            Dock = DockStyle.Left,
            Width = (Width / 2) - 20
        };

        var waypointSelector = new WaypointSelectorPanel
        {
            Dock = DockStyle.Right,
            Width = (Width / 2) - 20
        };

        selectorPanel.Controls.Add(profileSelector);
        selectorPanel.Controls.Add(waypointSelector);
        _contentPanel.Controls.Add(selectorPanel);
    }

    /// <summary>
    /// Initializes the status display panel.
    /// </summary>
    private void InitializeStatusDisplay()
    {
        if (_botController is IBotStatusProvider statusProvider)
        {
            var statusDisplay = new StatusDisplayPanel(statusProvider)
            {
                Dock = DockStyle.Top
            };
            _contentPanel.Controls.Add(statusDisplay);
        }

        if (_botController is ISessionStatisticsProvider statsProvider)
        {
            var statsPanel = new SessionStatisticsPanel(statsProvider)
            {
                Dock = DockStyle.Top
            };
            _contentPanel.Controls.Add(statsPanel);
        }

        // Add radar panel
        var radarProvider = new MockRadarDataProvider();
        radarProvider.SetupMockWaypointPath();
        var radarControl = new RadarControl(radarProvider)
        {
            Dock = DockStyle.Top
        };
        _contentPanel.Controls.Add(radarControl);

        // Add action log panel (fills remaining space)
        var actionLog = new ActionLogPanel(_botController.Log)
        {
            Dock = DockStyle.Fill
        };
        _contentPanel.Controls.Add(actionLog);
    }

    /// <summary>
    /// Handles bot running state changes.
    /// </summary>
    private void HandleBotRunningChanged(object? sender, BotRunningChangedEventArgs e)
    {
        if (e.IsRunning)
        {
            SetStatus(e.IsPaused ? "Bot paused" : "Bot running");
        }
        else
        {
            SetStatus("Bot stopped");
        }
    }

    /// <summary>
    /// Minimizes the window to the system tray.
    /// </summary>
    private void MinimizeToTray()
    {
        _notifyIcon.Visible = true;
        Hide();
        MinimizedToTray?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Restores the window from the system tray.
    /// </summary>
    private void RestoreFromTray()
    {
        Show();
        WindowState = FormWindowState.Normal;
        _notifyIcon.Visible = false;
        Focus();
        RestoredFromTray?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Exits the application.
    /// </summary>
    private void ExitApplication()
    {
        ApplicationExiting?.Invoke(this, EventArgs.Empty);
        _notifyIcon.Visible = false;
        Application.Exit();
    }

    /// <summary>
    /// Sets the status message displayed in the status bar.
    /// </summary>
    /// <param name="status">The status message to display.</param>
    public void SetStatus(string status)
    {
        if (InvokeRequired)
        {
            Invoke(new Action(() => SetStatus(status)));
            return;
        }

        _statusLabel.Text = status;
    }

    /// <summary>
    /// Sets the connection status displayed in the status bar.
    /// </summary>
    /// <param name="connected">True if connected, false otherwise.</param>
    public void SetConnectionStatus(bool connected)
    {
        if (InvokeRequired)
        {
            Invoke(new Action(() => SetConnectionStatus(connected)));
            return;
        }

        _connectionStatusLabel.Text = connected ? "Connected" : "Not Connected";
        _connectionStatusLabel.ForeColor = connected
            ? Color.FromArgb(100, 255, 100)
            : Color.FromArgb(255, 100, 100);
    }

    /// <summary>
    /// Shows a balloon tooltip from the system tray.
    /// </summary>
    /// <param name="title">The title of the balloon.</param>
    /// <param name="message">The message to display.</param>
    /// <param name="icon">The icon to display.</param>
    public void ShowBalloonTip(string title, string message, ToolTipIcon icon = ToolTipIcon.Info)
    {
        if (InvokeRequired)
        {
            Invoke(new Action(() => ShowBalloonTip(title, message, icon)));
            return;
        }

        _notifyIcon.Visible = true;
        _notifyIcon.ShowBalloonTip(3000, title, message, icon);
    }

    /// <summary>
    /// Clean up resources when the form is disposed.
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _notifyIcon?.Dispose();
            _trayContextMenu?.Dispose();
        }
        base.Dispose(disposing);
    }

    /// <summary>
    /// Shows the settings dialog with installation management options.
    /// </summary>
    private void ShowSettingsDialog()
    {
        var form = new Form
        {
            Text = "Settings",
            Size = new Size(750, 600),
            MinimumSize = new Size(700, 550),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.Sizable,
            BackColor = Color.FromArgb(30, 30, 30),
            ForeColor = Color.White,
            ShowInTaskbar = false
        };

        // Create tab control for different settings categories
        var tabControl = new TabControl
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(45, 45, 48),
            ForeColor = Color.White,
            DrawMode = TabDrawMode.Normal,
            Appearance = TabAppearance.Normal
        };

        // Bot Settings tab
        var botSettingsPage = new TabPage("Bot Settings")
        {
            BackColor = Color.FromArgb(35, 35, 38),
            ForeColor = Color.White,
            Padding = new Padding(10)
        };

        var settingsPanel = new SettingsPanel(_botController)
        {
            Dock = DockStyle.Fill
        };
        botSettingsPage.Controls.Add(settingsPanel);

        // Installation Management tab
        var installationPage = new TabPage("Installation")
        {
            BackColor = Color.FromArgb(35, 35, 38),
            ForeColor = Color.White,
            Padding = new Padding(15)
        };

        CreateInstallationManagementTab(installationPage);

        tabControl.TabPages.Add(botSettingsPage);
        tabControl.TabPages.Add(installationPage);

        form.Controls.Add(tabControl);
        form.ShowDialog(this);
    }

    /// <summary>
    /// Creates the installation management tab content.
    /// </summary>
    /// <param name="parentPanel">The parent panel to add controls to.</param>
    private void CreateInstallationManagementTab(Panel parentPanel)
    {
        int y = 15;

        // Section title
        var titleLabel = new Label
        {
            Text = "Installation Management",
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 14F, FontStyle.Bold),
            Location = new Point(15, y),
            AutoSize = true
        };
        parentPanel.Controls.Add(titleLabel);
        y += 35;

        // Description
        var descLabel = new Label
        {
            Text = "Manage your BepInEx and Erenshor Glider installation.",
            ForeColor = Color.FromArgb(180, 180, 180),
            Font = new Font("Segoe UI", 9F),
            Location = new Point(15, y),
            AutoSize = true
        };
        parentPanel.Controls.Add(descLabel);
        y += 35;

        // Run Setup Wizard button
        var wizardButton = CreateActionButton("Run Setup Wizard", "Re-run the initial setup wizard to configure your installation.",
            Color.FromArgb(70, 130, 180), async (s, e) => await HandleRunWizardClick());
        wizardButton.Location = new Point(15, y);
        parentPanel.Controls.Add(wizardButton);
        y += 55;

        // Repair Installation button
        var repairButton = CreateActionButton("Repair Installation", "Reinstall BepInEx and plugin files without changing configuration.",
            Color.FromArgb(160, 120, 60), async (s, e) => await HandleRepairInstallationClick());
        repairButton.Location = new Point(15, y);
        parentPanel.Controls.Add(repairButton);
        y += 55;

        // Uninstall button
        var uninstallButton = CreateActionButton("Uninstall", "Remove BepInEx and plugin files from your Erenshor installation.",
            Color.FromArgb(180, 70, 70), async (s, e) => await HandleUninstallClick());
        uninstallButton.Location = new Point(15, y);
        parentPanel.Controls.Add(uninstallButton);
        y += 55;

        // Installation status display
        y += 20;
        var statusSeparator = new Label
        {
            Text = "Installation Status",
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 11F, FontStyle.Bold),
            Location = new Point(15, y),
            AutoSize = true
        };
        parentPanel.Controls.Add(statusSeparator);
        y += 30;

        // Add installation status panel
        if (_installationService != null)
        {
            var statusPanel = new InstallationStatusPanel(_installationService)
            {
                Location = new Point(15, y),
                Width = parentPanel.Width - 40,
                Height = 160
            };
            parentPanel.Controls.Add(statusPanel);
        }
    }

    /// <summary>
    /// Creates an action button with description label.
    /// </summary>
    /// <param name="buttonText">The button text.</param>
    /// <param name="description">The description text.</param>
    /// <param name="color">The button color.</param>
    /// <param name="onClick">The click handler.</param>
    /// <returns>A panel containing the button and description.</returns>
    private Panel CreateActionButton(string buttonText, string description, Color color, EventHandler onClick)
    {
        var panel = new Panel
        {
            Height = 50,
            Width = 600,
            BackColor = Color.Transparent
        };

        var button = new Button
        {
            Text = buttonText,
            BackColor = color,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            Size = new Size(180, 32),
            UseVisualStyleBackColor = false,
            Cursor = Cursors.Hand,
            Location = new Point(0, 0)
        };

        var lighterColor = Color.FromArgb(
            Math.Min(255, color.R + 20),
            Math.Min(255, color.G + 20),
            Math.Min(255, color.B + 20));
        button.MouseEnter += (s, e) => button.BackColor = lighterColor;
        button.MouseLeave += (s, e) => button.BackColor = color;
        button.Click += onClick;

        var descLabel = new Label
        {
            Text = description,
            ForeColor = Color.FromArgb(180, 180, 180),
            Font = new Font("Segoe UI", 8F),
            Location = new Point(195, 8),
            Width = 400,
            AutoSize = false
        };

        panel.Controls.Add(button);
        panel.Controls.Add(descLabel);

        return panel;
    }

    /// <summary>
    /// Handles the Run Setup Wizard button click.
    /// </summary>
    private async System.Threading.Tasks.Task HandleRunWizardClick()
    {
        if (_installationService == null)
        {
            MessageBox.Show(
                "Installation service is not available.",
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        // NOTE: Do not attempt to close a parent form from here using `this` (MainWindow),
        // as that would close the main window instead of just a settings dialog.
        // Any settings dialog should be closed by the caller using the actual sender/control.

        // Show the setup wizard
        using var wizard = new Forms.SetupWizard(_installationService);
        wizard.ShowDialog();
    }

    /// <summary>
    /// Handles the Repair Installation button click.
    /// </summary>
    private async System.Threading.Tasks.Task HandleRepairInstallationClick()
    {
        if (_installationService == null)
        {
            MessageBox.Show(
                "Installation service is not available.",
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        var erenshorPath = _installationService.Config?.ErenshorPath;
        if (string.IsNullOrEmpty(erenshorPath))
        {
            MessageBox.Show(
                "Erenshor installation path is not configured. Please run the Setup Wizard first.",
                "Path Not Configured",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        var result = MessageBox.Show(
            "This will repair your installation by:\n\n" +
            "• Reinstalling BepInEx files\n" +
            "• Reinstalling the plugin DLL\n" +
            "• Validating all files\n\n" +
            "Your configuration will not be affected.\n\n" +
            "Do you want to continue?",
            "Repair Installation",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result != DialogResult.Yes)
            return;

        try
        {
            // Show progress dialog
            using var progressDialog = CreateProgressDialog("Repairing installation...");
            progressDialog.Show(this);

            // Step 1: Download/Get BepInEx
            progressDialog.Text = "Checking BepInEx...";
            var bepInExPath = await _installationService.DownloadBepInExAsync();

            if (string.IsNullOrEmpty(bepInExPath))
            {
                progressDialog.Close();
                MessageBox.Show(
                    "Failed to download BepInEx. Please check your internet connection.",
                    "Download Failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            // Step 2: Install BepInEx
            progressDialog.Text = "Installing BepInEx...";
            var bepInExResult = await _installationService.InstallBepInExAsync(erenshorPath!, bepInExPath!);

            if (!bepInExResult.Success)
            {
                progressDialog.Close();
                MessageBox.Show(
                    $"Failed to install BepInEx: {bepInExResult.ErrorMessage}",
                    "Installation Failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            // Step 3: Install plugin
            progressDialog.Text = "Installing plugin...";
            var guiDirectory = System.IO.Path.GetDirectoryName(Application.ExecutablePath);
            var pluginDllPath = System.IO.Path.Combine(guiDirectory!, "ErenshorGlider.dll");

            if (System.IO.File.Exists(pluginDllPath))
            {
                var pluginResult = await _installationService.InstallPluginAsync(pluginDllPath, erenshorPath!);

                if (!pluginResult.Success)
                {
                    progressDialog.Close();
                    MessageBox.Show(
                        $"BepInEx was repaired, but plugin installation failed: {pluginResult.ErrorMessage}",
                        "Partial Success",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }
            }

            progressDialog.Close();

            MessageBox.Show(
                "Installation repaired successfully!\n\n" +
                "BepInEx and the plugin have been reinstalled.",
                "Repair Complete",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"An error occurred during repair:\n{ex.Message}",
                "Repair Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// Handles the Uninstall button click.
    /// </summary>
    private async System.Threading.Tasks.Task HandleUninstallClick()
    {
        if (_installationService == null)
        {
            MessageBox.Show(
                "Installation service is not available.",
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        var erenshorPath = _installationService.Config?.ErenshorPath;
        if (string.IsNullOrEmpty(erenshorPath))
        {
            MessageBox.Show(
                "No installation found to uninstall.",
                "Nothing to Uninstall",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        // Check if game is running
        if (_installationService.IsGameRunning())
        {
            MessageBox.Show(
                "Cannot uninstall while Erenshor is running. Please close the game first.",
                "Game Running",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        var result = MessageBox.Show(
            "This will uninstall Erenshor Glider by removing:\n\n" +
            "• BepInEx folders and files\n" +
            "• ErenshorGlider plugin DLL\n" +
            "• doorstop_config.dll (if present)\n\n" +
            "Your game saves and Erenshor installation will NOT be affected.\n\n" +
            "Do you want to continue?",
            "Confirm Uninstallation",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (result != DialogResult.Yes)
            return;

        try
        {
            var deleteCount = 0;
            var errors = new System.Collections.Generic.List<string>();

            // Delete BepInEx folder
            var bepInExPath = System.IO.Path.Combine(erenshorPath!, "BepInEx");
            if (System.IO.Directory.Exists(bepInExPath))
            {
                try
                {
                    System.IO.Directory.Delete(bepInExPath, recursive: true);
                    deleteCount++;
                }
                catch (Exception ex)
                {
                    errors.Add($"BepInEx folder: {ex.Message}");
                }
            }

            // Delete doorstop_config.dll
            var doorstopPath = System.IO.Path.Combine(erenshorPath!, "doorstop_config.dll");
            if (System.IO.File.Exists(doorstopPath))
            {
                try
                {
                    System.IO.File.Delete(doorstopPath);
                    deleteCount++;
                }
                catch (Exception ex)
                {
                    errors.Add($"doorstop_config.dll: {ex.Message}");
                }
            }

            // Delete doorstop_config.dll.backup
            var backupPath = System.IO.Path.Combine(erenshorPath!, "doorstop_config.dll.backup");
            if (System.IO.File.Exists(backupPath))
            {
                try
                {
                    System.IO.File.Delete(backupPath);
                }
                catch { /* Ignore backup file errors */ }
            }

            // Clear the installation config
            _installationService.Config!.ErenshorPath = null;
            _installationService.SaveConfig();

            // Show results
            if (errors.Count > 0)
            {
                MessageBox.Show(
                    $"Uninstallation completed with some errors:\n\n" +
                    $"{string.Join("\n", errors)}\n\n" +
                    "You may need to manually remove some files.",
                    "Partial Uninstallation",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            else
            {
                MessageBox.Show(
                    "Erenshor Glider has been successfully uninstalled.\n\n" +
                    $"Removed {deleteCount} items from your Erenshor installation.",
                    "Uninstallation Complete",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"An error occurred during uninstallation:\n{ex.Message}",
                "Uninstallation Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// Finds the parent form for a control.
    /// </summary>
    /// <param name="control">The control to start from.</param>
    /// <returns>The parent form, or null if not found.</returns>
    private Form? FindParentForm(Control? control)
    {
        while (control != null)
        {
            if (control is Form form)
                return form;
            control = control.Parent;
        }
        return null;
    }

    /// <summary>
    /// Handles the Install Plugin button click.
    /// </summary>
    private async System.Threading.Tasks.Task HandleInstallPluginClick()
    {
        if (_installationService == null)
        {
            MessageBox.Show(
                "Installation service is not available.",
                "Installation Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        var erenshorPath = _installationService.Config?.ErenshorPath;
        if (string.IsNullOrEmpty(erenshorPath))
        {
            MessageBox.Show(
                "Erenshor installation path is not configured. Please configure it in Settings.",
                "Path Not Configured",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        // Check if game is running
        if (_installationService.IsGameRunning())
        {
            MessageBox.Show(
                "Cannot install plugin while Erenshor is running. Please close the game first.",
                "Game Running",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        try
        {
            // Get the current GUI directory to find the plugin DLL
            var guiDirectory = System.IO.Path.GetDirectoryName(Application.ExecutablePath);
            var pluginDllPath = System.IO.Path.Combine(guiDirectory!, "ErenshorGlider.dll");

            if (!System.IO.File.Exists(pluginDllPath))
            {
                MessageBox.Show(
                    $"Plugin DLL not found at: {pluginDllPath}",
                    "File Not Found",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            // Show progress dialog
            using var progressDialog = CreateProgressDialog("Installing Plugin...");
            progressDialog.Show(this);

            var result = await _installationService.InstallPluginAsync(pluginDllPath, erenshorPath!);

            progressDialog.Close();

            if (result.Success)
            {
                MessageBox.Show(
                    $"Plugin installed successfully!\n\n{result.Details ?? "The plugin has been copied to the BepInEx/plugins folder."}",
                    "Installation Complete",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show(
                    $"Failed to install plugin:\n{result.ErrorMessage}",
                    "Installation Failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"An error occurred during installation:\n{ex.Message}",
                "Installation Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// Handles the Launch Game button click.
    /// </summary>
    private async System.Threading.Tasks.Task HandleLaunchGameClick()
    {
        if (_installationService == null)
        {
            MessageBox.Show(
                "Installation service is not available.",
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        var erenshorPath = _installationService.Config?.ErenshorPath;
        if (string.IsNullOrEmpty(erenshorPath))
        {
            MessageBox.Show(
                "Erenshor installation path is not configured. Please configure it in Settings.",
                "Path Not Configured",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        // Check if game is already running
        if (_installationService.IsGameRunning())
        {
            MessageBox.Show(
                "Erenshor is already running!",
                "Game Running",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        try
        {
            var result = await _installationService.LaunchGameAsync(erenshorPath!);

            if (result.Success)
            {
                SetStatus("Game launched");
                UpdateGameRunningButtons(true);
            }
            else
            {
                MessageBox.Show(
                    $"Failed to launch Erenshor:\n{result.ErrorMessage}",
                    "Launch Failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"An error occurred while launching the game:\n{ex.Message}",
                "Launch Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// Updates button states when game running status changes.
    /// </summary>
    /// <param name="isRunning">True if game is running.</param>
    private void UpdateGameRunningButtons(bool isRunning)
    {
        _launchGameButton.Enabled = !isRunning && _installationService != null;
        _launchGameButton.Text = isRunning ? "Game Running" : "Launch Game";
        _installPluginButton.Enabled = !isRunning && _installationService != null;
    }

    /// <summary>
    /// Creates a simple progress dialog.
    /// </summary>
    /// <param name="message">The message to display.</param>
    /// <returns>A progress dialog form.</returns>
    private Form CreateProgressDialog(string message)
    {
        var form = new Form
        {
            Text = "Please Wait",
            Size = new Size(350, 120),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            ShowInTaskbar = false,
            BackColor = Color.FromArgb(30, 30, 30),
            ForeColor = Color.White
        };

        var label = new Label
        {
            Text = message,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10F)
        };

        var progressBar = new ProgressBar
        {
            Dock = DockStyle.Bottom,
            Height = 8,
            Style = ProgressBarStyle.Marquee
        };

        form.Controls.Add(label);
        form.Controls.Add(progressBar);

        return form;
    }
}
