using System;
using System.Drawing;
using System.Windows.Forms;

namespace ErenshorGlider.GUI.Controls;

/// <summary>
/// Panel displaying session statistics.
/// </summary>
public class SessionStatisticsPanel : Panel
{
    private readonly Label _runtimeLabel;
    private readonly Label _killsLabel;
    private readonly Label _deathsLabel;
    private readonly Label _xpLabel;
    private readonly Label _xpPerHourLabel;
    private readonly Label _goldLabel;
    private readonly Label _goldPerHourLabel;
    private readonly Label _itemsLabel;
    private readonly ISessionStatisticsProvider _statisticsProvider;
    private readonly Timer _updateTimer;

    /// <summary>
    /// Creates a new SessionStatisticsPanel.
    /// </summary>
    /// <param name="statisticsProvider">The statistics provider to use.</param>
    public SessionStatisticsPanel(ISessionStatisticsProvider statisticsProvider)
    {
        _statisticsProvider = statisticsProvider ?? throw new ArgumentNullException(nameof(statisticsProvider));

        BackColor = Color.FromArgb(40, 40, 43);
        BorderStyle = BorderStyle.FixedSingle;
        Padding = new Padding(10);
        Height = 160;

        // Create controls
        _runtimeLabel = CreateStatLabel("Runtime: 0s");
        _killsLabel = CreateStatLabel("Kills: 0 (0/hr)");
        _deathsLabel = CreateStatLabel("Deaths: 0");
        _xpLabel = CreateStatLabel("XP Gained: 0");
        _xpPerHourLabel = CreateStatLabel("XP/Hour: 0");
        _goldLabel = CreateStatLabel("Gold: 0 (0/hr)");
        _goldPerHourLabel = CreateStatLabel("Gold/Hour: 0");
        _itemsLabel = CreateStatLabel("Items Looted: 0");

        // Layout controls
        LayoutControls();

        // Set up update timer (1 update per second is sufficient for stats)
        _updateTimer = new Timer { Interval = 1000 };
        _updateTimer.Tick += (s, e) => UpdateStatistics();
        _updateTimer.Start();
    }

    /// <summary>
    /// Creates a statistics label.
    /// </summary>
    private Label CreateStatLabel(string text)
    {
        return new Label
        {
            Text = text,
            ForeColor = Color.FromArgb(220, 220, 220),
            Font = new Font("Segoe UI", 9F),
            Height = 20
        };
    }

    /// <summary>
    /// Layouts the controls on the panel.
    /// </summary>
    private void LayoutControls()
    {
        // First column
        _runtimeLabel.Location = new Point(10, 10);
        _runtimeLabel.Width = 200;
        Controls.Add(_runtimeLabel);

        _killsLabel.Location = new Point(10, 35);
        _killsLabel.Width = 200;
        Controls.Add(_killsLabel);

        _deathsLabel.Location = new Point(10, 60);
        _deathsLabel.Width = 200;
        Controls.Add(_deathsLabel);

        // Second column
        _xpLabel.Location = new Point(220, 10);
        _xpLabel.Width = 200;
        Controls.Add(_xpLabel);

        _xpPerHourLabel.Location = new Point(220, 35);
        _xpPerHourLabel.Width = 200;
        Controls.Add(_xpPerHourLabel);

        _goldLabel.Location = new Point(220, 60);
        _goldLabel.Width = 200;
        Controls.Add(_goldLabel);

        // Third column
        _goldPerHourLabel.Location = new Point(430, 10);
        _goldPerHourLabel.Width = 200;
        Controls.Add(_goldPerHourLabel);

        _itemsLabel.Location = new Point(430, 35);
        _itemsLabel.Width = 200;
        Controls.Add(_itemsLabel);
    }

    /// <summary>
    /// Updates the statistics display with current data.
    /// </summary>
    private void UpdateStatistics()
    {
        if (!IsHandleCreated || IsDisposed)
            return;

        try
        {
            var stats = _statisticsProvider.GetStatistics();

            // Update labels (only if text changed to reduce flicker)
            UpdateLabel(_runtimeLabel, $"Runtime: {stats.FormattedRuntime}");
            UpdateLabel(_killsLabel, $"Kills: {stats.Kills} ({stats.KillsPerHour}/hr)");
            UpdateLabel(_deathsLabel, $"Deaths: {stats.Deaths}");
            UpdateLabel(_xpLabel, $"XP Gained: {stats.XpGained:N0}");
            UpdateLabel(_xpPerHourLabel, $"XP/Hour: {stats.XpPerHour:N0}");
            UpdateLabel(_goldLabel, $"Gold: {stats.GoldEarned:N0} ({stats.GoldPerHour}/hr)");
            UpdateLabel(_goldPerHourLabel, $"Gold/Hour: {stats.GoldPerHour:N0}");
            UpdateLabel(_itemsLabel, $"Items Looted: {stats.ItemsLooted}");
        }
        catch
        {
            // Ignore errors during update (likely disposed)
        }
    }

    /// <summary>
    /// Updates a label only if the text has changed.
    /// </summary>
    private void UpdateLabel(Label label, string text)
    {
        if (label.Text != text)
            label.Text = text;
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
