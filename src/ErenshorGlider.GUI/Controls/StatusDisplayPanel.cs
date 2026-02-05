using System;
using System.Drawing;
using System.Windows.Forms;

namespace ErenshorGlider.GUI.Controls;

/// <summary>
/// Panel displaying current bot status information.
/// </summary>
public class StatusDisplayPanel : Panel
{
    private readonly Label _stateLabel;
    private readonly Label _targetLabel;
    private readonly Label _positionLabel;
    private readonly ProgressBar _healthBar;
    private readonly ProgressBar _manaBar;
    private readonly Label _healthTextLabel;
    private readonly Label _manaTextLabel;
    private readonly IBotStatusProvider _statusProvider;
    private readonly Timer _updateTimer;

    /// <summary>
    /// Creates a new StatusDisplayPanel.
    /// </summary>
    /// <param name="statusProvider">The status provider to use.</param>
    public StatusDisplayPanel(IBotStatusProvider statusProvider)
    {
        _statusProvider = statusProvider ?? throw new ArgumentNullException(nameof(statusProvider));

        BackColor = Color.FromArgb(40, 40, 43);
        BorderStyle = BorderStyle.FixedSingle;
        Padding = new Padding(10);

        // Create controls
        _stateLabel = CreateInfoLabel("State: Idle");
        _targetLabel = CreateInfoLabel("Target: None");
        _positionLabel = CreateInfoLabel("Position: X: 0.0, Y: 0.0, Z: 0.0");
        _healthBar = CreateProgressBar(Color.FromArgb(180, 60, 60));
        _manaBar = CreateProgressBar(Color.FromArgb(60, 100, 180));
        _healthTextLabel = CreateBarLabel("HP");
        _manaTextLabel = CreateBarLabel("MP");

        // Layout controls
        LayoutControls();

        // Set up update timer
        _updateTimer = new Timer { Interval = 100 }; // 10 updates per second
        _updateTimer.Tick += (s, e) => UpdateStatus();
        _updateTimer.Start();
    }

    /// <summary>
    /// Creates an info label.
    /// </summary>
    private Label CreateInfoLabel(string text)
    {
        return new Label
        {
            Text = text,
            ForeColor = Color.FromArgb(220, 220, 220),
            Font = new Font("Consolas", 9F),
            Height = 20
        };
    }

    /// <summary>
    /// Creates a progress bar with custom color.
    /// </summary>
    private ProgressBar CreateProgressBar(Color foreColor)
    {
        return new ProgressBar
        {
            Minimum = 0,
            Maximum = 100,
            Value = 100,
            Height = 16,
            ForeColor = foreColor,
            BackColor = Color.FromArgb(20, 20, 20),
            Style = ProgressBarStyle.Continuous
        };
    }

    /// <summary>
    /// Creates a label for a progress bar.
    /// </summary>
    private Label CreateBarLabel(string text)
    {
        return new Label
        {
            Text = text,
            ForeColor = Color.FromArgb(200, 200, 200),
            Font = new Font("Segoe UI", 8F),
            Height = 16,
            Width = 30
        };
    }

    /// <summary>
    /// Layouts the controls on the panel.
    /// </summary>
    private void LayoutControls()
    {
        // State row
        _stateLabel.Location = new Point(10, 10);
        _stateLabel.Width = Width - 20;
        Controls.Add(_stateLabel);

        // Target row
        _targetLabel.Location = new Point(10, 35);
        _targetLabel.Width = Width - 20;
        Controls.Add(_targetLabel);

        // Position row
        _positionLabel.Location = new Point(10, 60);
        _positionLabel.Width = Width - 20;
        Controls.Add(_positionLabel);

        // Health bar row
        _healthTextLabel.Location = new Point(10, 90);
        _healthBar.Location = new Point(45, 90);
        _healthBar.Width = Width - 60;
        Controls.Add(_healthTextLabel);
        Controls.Add(_healthBar);

        // Mana bar row
        _manaTextLabel.Location = new Point(10, 115);
        _manaBar.Location = new Point(45, 115);
        _manaBar.Width = Width - 60;
        Controls.Add(_manaTextLabel);
        Controls.Add(_manaBar);
    }

    /// <summary>
    /// Updates the status display with current data.
    /// </summary>
    private void UpdateStatus()
    {
        if (!IsHandleCreated || IsDisposed)
            return;

        try
        {
            var status = _statusProvider.GetStatus();

            // Update labels (only if text changed to reduce flicker)
            if (_stateLabel.Text != $"State: {status.State}")
                _stateLabel.Text = $"State: {status.State}";

            if (status.HasTarget)
            {
                var targetText = $"Target: {status.TargetName}";
                if (_targetLabel.Text != targetText)
                {
                    _targetLabel.Text = targetText;
                    _targetLabel.ForeColor = Color.FromArgb(255, 100, 100);
                }
            }
            else
            {
                if (_targetLabel.Text != "Target: None")
                {
                    _targetLabel.Text = "Target: None";
                    _targetLabel.ForeColor = Color.FromArgb(150, 150, 150);
                }
            }

            if (_positionLabel.Text != $"Position: {status.FormattedPosition}")
                _positionLabel.Text = $"Position: {status.FormattedPosition}";

            // Update health bar
            int healthValue = (int)status.HealthPercent;
            if (_healthBar.Value != healthValue)
                _healthBar.Value = Math.Max(0, Math.Min(100, healthValue));

            // Update mana bar
            int manaValue = (int)status.ManaPercent;
            if (_manaBar.Value != manaValue)
                _manaBar.Value = Math.Max(0, Math.Min(100, manaValue));
        }
        catch
        {
            // Ignore errors during update (likely disposed)
        }
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
