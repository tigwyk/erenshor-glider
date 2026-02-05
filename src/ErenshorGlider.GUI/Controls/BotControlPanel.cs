using System;
using System.Drawing;
using System.Windows.Forms;

namespace ErenshorGlider.GUI.Controls;

/// <summary>
/// Control panel for bot Start/Stop/Pause operations.
/// </summary>
public class BotControlPanel : Panel
{
    private readonly Button _startButton;
    private readonly Button _stopButton;
    private readonly Button _pauseButton;
    private readonly Label _stateLabel;
    private readonly IBotController _botController;

    /// <summary>
    /// Creates a new BotControlPanel.
    /// </summary>
    /// <param name="botController">The bot controller to use.</param>
    public BotControlPanel(IBotController botController)
    {
        _botController = botController ?? throw new ArgumentNullException(nameof(botController));

        BackColor = Color.FromArgb(40, 40, 43);
        BorderStyle = BorderStyle.FixedSingle;
        Padding = new Padding(10);
        Height = 120;

        // Create controls
        _stateLabel = CreateStateLabel();
        _startButton = CreateStartButton();
        _stopButton = CreateStopButton();
        _pauseButton = CreatePauseButton();

        // Layout controls
        LayoutControls();

        // Wire up events
        WireUpEvents();

        // Initialize button states
        UpdateButtonStates();
    }

    /// <summary>
    /// Creates the state display label.
    /// </summary>
    private Label CreateStateLabel()
    {
        return new Label
        {
            Text = $"State: {_botController.CurrentState}",
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            Dock = DockStyle.Top,
            Height = 30,
            TextAlign = ContentAlignment.MiddleLeft
        };
    }

    /// <summary>
    /// Creates the Start button.
    /// </summary>
    private Button CreateStartButton()
    {
        return new Button
        {
            Text = "Start",
            BackColor = Color.FromArgb(100, 180, 100),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            Size = new Size(100, 40),
            UseVisualStyleBackColor = false,
            Cursor = Cursors.Hand
        };
    }

    /// <summary>
    /// Creates the Stop button.
    /// </summary>
    private Button CreateStopButton()
    {
        return new Button
        {
            Text = "Stop",
            BackColor = Color.FromArgb(200, 80, 80),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            Size = new Size(100, 40),
            UseVisualStyleBackColor = false,
            Cursor = Cursors.Hand
        };
    }

    /// <summary>
    /// Creates the Pause/Resume button.
    /// </summary>
    private Button CreatePauseButton()
    {
        return new Button
        {
            Text = "Pause",
            BackColor = Color.FromArgb(220, 180, 80),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            Size = new Size(100, 40),
            UseVisualStyleBackColor = false,
            Cursor = Cursors.Hand
        };
    }

    /// <summary>
    /// Layouts the controls on the panel.
    /// </summary>
    private void LayoutControls()
    {
        // Create button panel
        var buttonPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 50
        };

        // Position buttons
        _startButton.Location = new Point(10, 5);
        _stopButton.Location = new Point(120, 5);
        _pauseButton.Location = new Point(230, 5);

        buttonPanel.Controls.AddRange(new Control[] { _startButton, _stopButton, _pauseButton });

        // Add to main panel
        Controls.Add(_stateLabel);
        Controls.Add(buttonPanel);
    }

    /// <summary>
    /// Wires up event handlers.
    /// </summary>
    private void WireUpEvents()
    {
        // Button click events
        _startButton.Click += (s, e) => HandleStart();
        _stopButton.Click += (s, e) => HandleStop();
        _pauseButton.Click += (s, e) => HandlePauseResume();

        // Bot controller events
        _botController.BotRunningChanged += (s, e) =>
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => _botController_BotRunningChanged(s, e)));
                return;
            }
            _botController_BotRunningChanged(s, e);
        };

        _botController.BotStateChanged += (s, e) =>
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => _botController_BotStateChanged(s, e)));
                return;
            }
            _botController_BotStateChanged(s, e);
        };

        // Button mouse enter/leave for hover effect
        _startButton.MouseEnter += (s, e) => SetButtonHoverColor(_startButton, true);
        _startButton.MouseLeave += (s, e) => SetButtonHoverColor(_startButton, false);
        _stopButton.MouseEnter += (s, e) => SetButtonHoverColor(_stopButton, true);
        _stopButton.MouseLeave += (s, e) => SetButtonHoverColor(_stopButton, false);
        _pauseButton.MouseEnter += (s, e) => SetButtonHoverColor(_pauseButton, true);
        _pauseButton.MouseLeave += (s, e) => SetButtonHoverColor(_pauseButton, false);
    }

    /// <summary>
    /// Handles the Start button click.
    /// </summary>
    private void HandleStart()
    {
        if (_botController.Start())
        {
            UpdateButtonStates();
        }
    }

    /// <summary>
    /// Handles the Stop button click.
    /// </summary>
    private void HandleStop()
    {
        _botController.Stop();
        UpdateButtonStates();
    }

    /// <summary>
    /// Handles the Pause/Resume button click.
    /// </summary>
    private void HandlePauseResume()
    {
        if (_botController.IsPaused)
        {
            _botController.Resume();
        }
        else
        {
            _botController.Pause();
        }
        UpdateButtonStates();
    }

    /// <summary>
    /// Handles bot running state changes from the controller.
    /// </summary>
    private void _botController_BotRunningChanged(object? sender, BotRunningChangedEventArgs e)
    {
        UpdateButtonStates();
    }

    /// <summary>
    /// Handles bot state changes from the controller.
    /// </summary>
    private void _botController_BotStateChanged(object? sender, BotStateChangedEventArgs e)
    {
        _stateLabel.Text = $"State: {e.NewState}";
    }

    /// <summary>
    /// Updates button enabled/disabled states based on bot state.
    /// </summary>
    private void UpdateButtonStates()
    {
        bool isRunning = _botController.IsRunning;
        bool isPaused = _botController.IsPaused;

        // Start: enabled when not running
        _startButton.Enabled = !isRunning;

        // Stop: enabled when running
        _stopButton.Enabled = isRunning;

        // Pause/Resume: enabled when running
        _pauseButton.Enabled = isRunning;
        _pauseButton.Text = isPaused ? "Resume" : "Pause";
        _pauseButton.BackColor = isPaused
            ? Color.FromArgb(100, 180, 100)  // Green for resume
            : Color.FromArgb(220, 180, 80);   // Yellow for pause
    }

    /// <summary>
    /// Sets button hover color.
    /// </summary>
    private void SetButtonHoverColor(Button button, bool isHovering)
    {
        if (!button.Enabled) return;

        var baseColor = button.BackColor;

        if (isHovering)
        {
            button.BackColor = LightenColor(baseColor, 20);
        }
        else
        {
            // Restore base color based on button type
            if (button == _startButton)
                button.BackColor = Color.FromArgb(100, 180, 100);
            else if (button == _stopButton)
                button.BackColor = Color.FromArgb(200, 80, 80);
            else if (button == _pauseButton)
                button.BackColor = _botController.IsPaused
                    ? Color.FromArgb(100, 180, 100)
                    : Color.FromArgb(220, 180, 80);
        }
    }

    /// <summary>
    /// Lightens a color by the specified amount.
    /// </summary>
    private Color LightenColor(Color color, int amount)
    {
        int r = Math.Min(255, color.R + amount);
        int g = Math.Min(255, color.G + amount);
        int b = Math.Min(255, color.B + amount);
        return Color.FromArgb(r, g, b);
    }
}
