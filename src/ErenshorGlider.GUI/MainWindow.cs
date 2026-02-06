using System;
using System.Drawing;
using System.Windows.Forms;
using ErenshorGlider.GUI.Controls;

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
    public MainWindow() : this(new MockBotController())
    {
    }

    /// <summary>
    /// Creates a new MainWindow with the specified bot controller.
    /// </summary>
    /// <param name="botController">The bot controller to use.</param>
    public MainWindow(IBotController botController)
    {
        _botController = botController ?? throw new ArgumentNullException(nameof(botController));

        // Form properties
        Text = "Erenshor Glider";
        Size = new Size(900, 650);
        MinimumSize = new Size(800, 500);
        StartPosition = FormStartPosition.CenterScreen;
        Icon = SystemIcons.Application;

        // Initialize components
        _headerPanel = CreateHeaderPanel();
        _titleLabel = CreateTitleLabel();
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
        // Add title to header
        _headerPanel.Controls.Add(_titleLabel);

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
}
