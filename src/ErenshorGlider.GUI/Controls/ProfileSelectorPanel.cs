using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace ErenshorGlider.GUI.Controls;

/// <summary>
/// Panel for selecting combat profiles.
/// </summary>
public class ProfileSelectorPanel : Panel
{
    private readonly Label _profileLabel;
    private readonly ComboBox _profileComboBox;
    private readonly Label _currentProfileLabel;

    /// <summary>
    /// Gets the currently selected profile name.
    /// </summary>
    public string SelectedProfile => _profileComboBox.SelectedItem?.ToString() ?? string.Empty;

    /// <summary>
    /// Event raised when a profile is selected.
    /// </summary>
    public event EventHandler<string>? ProfileSelected;

    /// <summary>
    /// Creates a new ProfileSelectorPanel.
    /// </summary>
    public ProfileSelectorPanel()
    {
        BackColor = Color.FromArgb(40, 40, 43);
        BorderStyle = BorderStyle.FixedSingle;
        Padding = new Padding(10);
        Height = 70;

        // Create controls
        _profileLabel = new Label
        {
            Text = "Combat Profile:",
            ForeColor = Color.FromArgb(200, 200, 200),
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            Dock = DockStyle.Top,
            Height = 20
        };

        _profileComboBox = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9F),
            BackColor = Color.FromArgb(30, 30, 33),
            ForeColor = Color.FromArgb(220, 220, 220),
            FlatStyle = FlatStyle.Flat,
            Dock = DockStyle.Top,
            Height = 25
        };

        _currentProfileLabel = new Label
        {
            Text = "Current: None",
            ForeColor = Color.FromArgb(150, 150, 150),
            Font = new Font("Segoe UI", 8F),
            Dock = DockStyle.Top,
            Height = 15
        };

        // Layout controls
        _profileLabel.Dock = DockStyle.Top;
        _profileComboBox.Dock = DockStyle.Top;
        _currentProfileLabel.Dock = DockStyle.Top;

        Controls.Add(_currentProfileLabel);
        Controls.Add(_profileComboBox);
        Controls.Add(_profileLabel);

        // Wire up events
        _profileComboBox.SelectedIndexChanged += (s, e) =>
        {
            var selected = SelectedProfile;
            if (!string.IsNullOrEmpty(selected))
            {
                _currentProfileLabel.Text = $"Current: {selected}";
                ProfileSelected?.Invoke(this, selected);
            }
        };

        // Load profiles (mock for now - would read from profiles/ directory)
        LoadMockProfiles();
    }

    /// <summary>
    /// Loads mock profiles for testing.
    /// </summary>
    private void LoadMockProfiles()
    {
        // In real implementation, this would scan the profiles/ directory
        _profileComboBox.Items.AddRange(new object[]
        {
            "Warrior.json",
            "Mage.json",
            "Rogue.json",
            "Cleric.json",
            "Ranger.json",
            "Necromancer.json"
        });

        if (_profileComboBox.Items.Count > 0)
        {
            _profileComboBox.SelectedIndex = 0;
        }
    }

    /// <summary>
    /// Sets the current profile.
    /// </summary>
    /// <param name="profileName">The profile name to select.</param>
    public void SetProfile(string profileName)
    {
        var index = _profileComboBox.Items.IndexOf(profileName);
        if (index >= 0)
        {
            _profileComboBox.SelectedIndex = index;
        }
    }
}

/// <summary>
/// Panel for selecting waypoint paths.
/// </summary>
public class WaypointSelectorPanel : Panel
{
    private readonly Label _waypointLabel;
    private readonly ComboBox _waypointComboBox;
    private readonly Label _currentWaypointLabel;

    /// <summary>
    /// Gets the currently selected waypoint path name.
    /// </summary>
    public string SelectedPath => _waypointComboBox.SelectedItem?.ToString() ?? string.Empty;

    /// <summary>
    /// Event raised when a waypoint path is selected.
    /// </summary>
    public event EventHandler<string>? WaypointSelected;

    /// <summary>
    /// Creates a new WaypointSelectorPanel.
    /// </summary>
    public WaypointSelectorPanel()
    {
        BackColor = Color.FromArgb(40, 40, 43);
        BorderStyle = BorderStyle.FixedSingle;
        Padding = new Padding(10);
        Height = 70;

        // Create controls
        _waypointLabel = new Label
        {
            Text = "Waypoint Path:",
            ForeColor = Color.FromArgb(200, 200, 200),
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            Dock = DockStyle.Top,
            Height = 20
        };

        _waypointComboBox = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9F),
            BackColor = Color.FromArgb(30, 30, 33),
            ForeColor = Color.FromArgb(220, 220, 220),
            FlatStyle = FlatStyle.Flat,
            Dock = DockStyle.Top,
            Height = 25
        };

        _currentWaypointLabel = new Label
        {
            Text = "Current: None",
            ForeColor = Color.FromArgb(150, 150, 150),
            Font = new Font("Segoe UI", 8F),
            Dock = DockStyle.Top,
            Height = 15
        };

        // Layout controls
        _waypointLabel.Dock = DockStyle.Top;
        _waypointComboBox.Dock = DockStyle.Top;
        _currentWaypointLabel.Dock = DockStyle.Top;

        Controls.Add(_currentWaypointLabel);
        Controls.Add(_waypointComboBox);
        Controls.Add(_waypointLabel);

        // Wire up events
        _waypointComboBox.SelectedIndexChanged += (s, e) =>
        {
            var selected = SelectedPath;
            if (!string.IsNullOrEmpty(selected))
            {
                _currentWaypointLabel.Text = $"Current: {selected}";
                WaypointSelected?.Invoke(this, selected);
            }
        };

        // Load waypoint paths (mock for now - would read from waypoints/ directory)
        LoadMockWaypoints();
    }

    /// <summary>
    /// Loads mock waypoint paths for testing.
    /// </summary>
    private void LoadMockWaypoints()
    {
        // In real implementation, this would scan the waypoints/ directory
        _waypointComboBox.Items.AddRange(new object[]
        {
            "StarterZone_Patrol.json",
            "StarterZone_Loop.json",
            "Forest_Circuit.json",
            "Cave_Farming.json",
            "Dungeon_Grind.json"
        });

        if (_waypointComboBox.Items.Count > 0)
        {
            _waypointComboBox.SelectedIndex = 0;
        }
    }

    /// <summary>
    /// Sets the current waypoint path.
    /// </summary>
    /// <param name="pathName">The path name to select.</param>
    public void SetPath(string pathName)
    {
        var index = _waypointComboBox.Items.IndexOf(pathName);
        if (index >= 0)
        {
            _waypointComboBox.SelectedIndex = index;
        }
    }
}
