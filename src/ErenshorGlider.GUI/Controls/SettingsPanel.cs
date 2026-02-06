using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ErenshorGlider.Configuration;
using ErenshorGlider.GUI;

namespace ErenshorGlider.GUI.Controls;

/// <summary>
/// Panel for editing bot settings with categorized tabs.
/// </summary>
public class SettingsPanel : Panel
{
    private readonly TabControl _tabControl;
    private readonly Button _saveButton;
    private readonly Button _resetButton;
    private readonly Label _statusLabel;
    private readonly IBotController _configProvider;
    private BotConfig _workingConfig;

    // Combat tab controls
    private NumericUpDown _minHealthCombat;
    private NumericUpDown _minManaCombat;
    private NumericUpDown _maxDeathCount;
    private NumericUpDown _combatTimeout;
    private CheckBox _chaseFleeing;
    private NumericUpDown _maxChaseDistance;
    private NumericUpDown _maxAttackRange;

    // Target selection tab controls
    private NumericUpDown _maxLevelAbove;
    private NumericUpDown _maxLevelBelow;
    private NumericUpDown _maxSearchRadius;
    private NumericUpDown _maxWaypointDistance;
    private CheckBox _prioritizeAttackers;
    private TextBox _blacklistedMobs;
    private TextBox _blacklistedTypes;

    // Rest and recovery tab controls
    private NumericUpDown _minHealthRest;
    private NumericUpDown _minManaRest;
    private NumericUpDown _targetHealthRest;
    private NumericUpDown _targetManaRest;
    private NumericUpDown _maxRestDuration;
    private TextBox _foodItem;
    private TextBox _drinkItem;

    // Looting tab controls
    private CheckBox _autoLoot;
    private NumericUpDown _lootDistance;
    private NumericUpDown _maxLootWait;
    private CheckBox _skipLootWhenFull;
    private NumericUpDown _minFreeBagSlots;

    // Navigation tab controls
    private NumericUpDown _stoppingDistance;
    private NumericUpDown _stuckThreshold;
    private NumericUpDown _maxUnstuckAttempts;
    private NumericUpDown _movementProgress;
    private NumericUpDown _facingTolerance;

    // Waypoints tab controls
    private NumericUpDown _minWaypointDistance;
    private NumericUpDown _minWaypointInterval;

    // Input tab controls
    private NumericUpDown _inputDelay;
    private NumericUpDown _inputRandomization;

    // Death tab controls
    private CheckBox _autoReleaseSpirit;
    private CheckBox _autoResurrectGraveyard;
    private NumericUpDown _maxResurrectWait;
    private NumericUpDown _postResurrectDelay;

    // Map discovery tab controls
    private CheckBox _autoMappingEnabled;
    private CheckBox _recordResourceNodes;
    private CheckBox _recordNpcs;
    private CheckBox _recordMobSpawns;
    private NumericUpDown _mapDedupeRadius;

    // Hotkeys tab controls
    private ComboBox _emergencyStopHotkey;
    private ComboBox _pauseResumeHotkey;

    // Session limits tab controls
    private NumericUpDown _maxSessionRuntime;
    private NumericUpDown _maxStuckTime;

    /// <summary>
    /// Creates a new SettingsPanel.
    /// </summary>
    /// <param name="configProvider">The configuration provider.</param>
    public SettingsPanel(IBotController configProvider)
    {
        _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));
        _workingConfig = CloneConfig(configProvider.CurrentConfig);

        BackColor = Color.FromArgb(40, 40, 43);
        BorderStyle = BorderStyle.FixedSingle;
        Padding = new Padding(10);

        // Create main controls
        _tabControl = CreateTabControl();
        _saveButton = CreateSaveButton();
        _resetButton = CreateResetButton();
        _statusLabel = CreateStatusLabel();

        // Create all setting controls
        CreateCombatTabControls();
        CreateTargetSelectionTabControls();
        CreateRestRecoveryTabControls();
        CreateLootingTabControls();
        CreateNavigationTabControls();
        CreateWaypointsTabControls();
        CreateInputTabControls();
        CreateDeathTabControls();
        CreateMapDiscoveryTabControls();
        CreateHotkeysTabControls();
        CreateSessionLimitsTabControls();

        // Layout controls
        LayoutControls();

        // Wire up events
        WireUpEvents();

        // Load initial values
        LoadCurrentConfig();
    }

    /// <summary>
    /// Creates the main tab control.
    /// </summary>
    private TabControl CreateTabControl()
    {
        return new TabControl
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(45, 45, 48),
            ForeColor = Color.White,
            DrawMode = TabDrawMode.Normal,
            Appearance = TabAppearance.Normal
        };
    }

    /// <summary>
    /// Creates the Save button.
    /// </summary>
    private Button CreateSaveButton()
    {
        return new Button
        {
            Text = "Save Settings",
            BackColor = Color.FromArgb(100, 180, 100),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9F),
            Size = new Size(120, 30),
            UseVisualStyleBackColor = false,
            Cursor = Cursors.Hand
        };
    }

    /// <summary>
    /// Creates the Reset button.
    /// </summary>
    private Button CreateResetButton()
    {
        return new Button
        {
            Text = "Reset",
            BackColor = Color.FromArgb(180, 180, 180),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9F),
            Size = new Size(100, 30),
            UseVisualStyleBackColor = false,
            Cursor = Cursors.Hand
        };
    }

    /// <summary>
    /// Creates the status label.
    /// </summary>
    private Label CreateStatusLabel()
    {
        return new Label
        {
            Text = "",
            ForeColor = Color.FromArgb(150, 150, 150),
            Font = new Font("Segoe UI", 8F),
            TextAlign = ContentAlignment.MiddleRight,
            Dock = DockStyle.Fill
        };
    }

    /// <summary>
    /// Creates all combat tab controls.
    /// </summary>
    private void CreateCombatTabControls()
    {
        _minHealthCombat = CreateNumericUpDown(0, 100, 1);
        _minManaCombat = CreateNumericUpDown(0, 100, 1);
        _maxDeathCount = CreateNumericUpDown(0, 100, 1);
        _combatTimeout = CreateNumericUpDown(5, 300, 1);
        _chaseFleeing = CreateCheckBox();
        _maxChaseDistance = CreateNumericUpDown(0, 200, 1);
        _maxAttackRange = CreateNumericUpDown(0, 100, 1);

        var combatPage = CreateTabPage("Combat");
        combatPage.Controls.Add(CreateSettingRow("Min Health for Combat (%)", _minHealthCombat, 10));
        combatPage.Controls.Add(CreateSettingRow("Min Mana for Combat (%)", _minManaCombat, 50));
        combatPage.Controls.Add(CreateSettingRow("Max Death Count", _maxDeathCount, 90));
        combatPage.Controls.Add(CreateSettingRow("Combat Timeout (sec)", _combatTimeout, 130));
        combatPage.Controls.Add(CreateSettingRow("Chase Fleeing Targets", _chaseFleeing, 170));
        combatPage.Controls.Add(CreateSettingRow("Max Chase Distance", _maxChaseDistance, 210));
        combatPage.Controls.Add(CreateSettingRow("Max Attack Range", _maxAttackRange, 250));

        _tabControl.TabPages.Add(combatPage);
    }

    /// <summary>
    /// Creates all target selection tab controls.
    /// </summary>
    private void CreateTargetSelectionTabControls()
    {
        _maxLevelAbove = CreateNumericUpDown(0, 20, 1);
        _maxLevelBelow = CreateNumericUpDown(0, 50, 1);
        _maxSearchRadius = CreateNumericUpDown(10, 200, 1);
        _maxWaypointDistance = CreateNumericUpDown(0, 500, 1);
        _prioritizeAttackers = CreateCheckBox();
        _blacklistedMobs = CreateTextBox();
        _blacklistedTypes = CreateTextBox();

        var targetPage = CreateTabPage("Target Selection");
        targetPage.Controls.Add(CreateSettingRow("Max Level Above", _maxLevelAbove, 10));
        targetPage.Controls.Add(CreateSettingRow("Max Level Below", _maxLevelBelow, 50));
        targetPage.Controls.Add(CreateSettingRow("Max Search Radius", _maxSearchRadius, 90));
        targetPage.Controls.Add(CreateSettingRow("Max Waypoint Distance", _maxWaypointDistance, 130));
        targetPage.Controls.Add(CreateSettingRow("Prioritize Attackers", _prioritizeAttackers, 170));
        targetPage.Controls.Add(CreateSettingRow("Blacklisted Mob Names (comma)", _blacklistedMobs, 210, multiline: true));
        targetPage.Controls.Add(CreateSettingRow("Blacklisted Types (comma)", _blacklistedTypes, 270, multiline: true));

        _tabControl.TabPages.Add(targetPage);
    }

    /// <summary>
    /// Creates all rest and recovery tab controls.
    /// </summary>
    private void CreateRestRecoveryTabControls()
    {
        _minHealthRest = CreateNumericUpDown(0, 100, 1);
        _minManaRest = CreateNumericUpDown(0, 100, 1);
        _targetHealthRest = CreateNumericUpDown(0, 100, 1);
        _targetManaRest = CreateNumericUpDown(0, 100, 1);
        _maxRestDuration = CreateNumericUpDown(10, 300, 1);
        _foodItem = CreateTextBox();
        _drinkItem = CreateTextBox();

        var restPage = CreateTabPage("Rest & Recovery");
        restPage.Controls.Add(CreateSettingRow("Min Health to Rest (%)", _minHealthRest, 10));
        restPage.Controls.Add(CreateSettingRow("Min Mana to Rest (%)", _minManaRest, 50));
        restPage.Controls.Add(CreateSettingRow("Target Health After Rest (%)", _targetHealthRest, 90));
        restPage.Controls.Add(CreateSettingRow("Target Mana After Rest (%)", _targetManaRest, 130));
        restPage.Controls.Add(CreateSettingRow("Max Rest Duration (sec)", _maxRestDuration, 170));
        restPage.Controls.Add(CreateSettingRow("Food Item Name", _foodItem, 210));
        restPage.Controls.Add(CreateSettingRow("Drink Item Name", _drinkItem, 250));

        _tabControl.TabPages.Add(restPage);
    }

    /// <summary>
    /// Creates all looting tab controls.
    /// </summary>
    private void CreateLootingTabControls()
    {
        _autoLoot = CreateCheckBox();
        _lootDistance = CreateNumericUpDown(0.5m, 10m, 0.1m);
        _maxLootWait = CreateNumericUpDown(1, 60, 1);
        _skipLootWhenFull = CreateCheckBox();
        _minFreeBagSlots = CreateNumericUpDown(0, 20, 1);

        var lootPage = CreateTabPage("Looting");
        lootPage.Controls.Add(CreateSettingRow("Auto Loot", _autoLoot, 10));
        lootPage.Controls.Add(CreateSettingRow("Loot Distance", _lootDistance, 50));
        lootPage.Controls.Add(CreateSettingRow("Max Loot Wait (sec)", _maxLootWait, 90));
        lootPage.Controls.Add(CreateSettingRow("Skip Loot When Full", _skipLootWhenFull, 130));
        lootPage.Controls.Add(CreateSettingRow("Min Free Bag Slots", _minFreeBagSlots, 170));

        _tabControl.TabPages.Add(lootPage);
    }

    /// <summary>
    /// Creates all navigation tab controls.
    /// </summary>
    private void CreateNavigationTabControls()
    {
        _stoppingDistance = CreateNumericUpDown(0.5m, 10m, 0.1m);
        _stuckThreshold = CreateNumericUpDown(0.5m, 10m, 0.1m);
        _maxUnstuckAttempts = CreateNumericUpDown(1, 20, 1);
        _movementProgress = CreateNumericUpDown(0.1m, 5m, 0.1m);
        _facingTolerance = CreateNumericUpDown(1, 45, 1);

        var navPage = CreateTabPage("Navigation");
        navPage.Controls.Add(CreateSettingRow("Stopping Distance", _stoppingDistance, 10));
        navPage.Controls.Add(CreateSettingRow("Stuck Threshold (sec)", _stuckThreshold, 50));
        navPage.Controls.Add(CreateSettingRow("Max Unstuck Attempts", _maxUnstuckAttempts, 90));
        navPage.Controls.Add(CreateSettingRow("Movement Progress Threshold", _movementProgress, 130));
        navPage.Controls.Add(CreateSettingRow("Facing Tolerance (degrees)", _facingTolerance, 170));

        _tabControl.TabPages.Add(navPage);
    }

    /// <summary>
    /// Creates all waypoints tab controls.
    /// </summary>
    private void CreateWaypointsTabControls()
    {
        _minWaypointDistance = CreateNumericUpDown(1, 50, 1);
        _minWaypointInterval = CreateNumericUpDown(0.1m, 10m, 0.1m);

        var wpPage = CreateTabPage("Waypoints");
        wpPage.Controls.Add(CreateSettingRow("Min Waypoint Distance", _minWaypointDistance, 10));
        wpPage.Controls.Add(CreateSettingRow("Min Waypoint Interval (sec)", _minWaypointInterval, 50));

        _tabControl.TabPages.Add(wpPage);
    }

    /// <summary>
    /// Creates all input tab controls.
    /// </summary>
    private void CreateInputTabControls()
    {
        _inputDelay = CreateNumericUpDown(10, 500, 10);
        _inputRandomization = CreateNumericUpDown(0, 200, 5);

        var inputPage = CreateTabPage("Input");
        inputPage.Controls.Add(CreateSettingRow("Input Delay (ms)", _inputDelay, 10));
        inputPage.Controls.Add(CreateSettingRow("Input Randomization (ms)", _inputRandomization, 50));

        _tabControl.TabPages.Add(inputPage);
    }

    /// <summary>
    /// Creates all death tab controls.
    /// </summary>
    private void CreateDeathTabControls()
    {
        _autoReleaseSpirit = CreateCheckBox();
        _autoResurrectGraveyard = CreateCheckBox();
        _maxResurrectWait = CreateNumericUpDown(5, 120, 1);
        _postResurrectDelay = CreateNumericUpDown(0, 30, 1);

        var deathPage = CreateTabPage("Death");
        deathPage.Controls.Add(CreateSettingRow("Auto Release Spirit", _autoReleaseSpirit, 10));
        deathPage.Controls.Add(CreateSettingRow("Auto Resurrect at Graveyard", _autoResurrectGraveyard, 50));
        deathPage.Controls.Add(CreateSettingRow("Max Resurrect Wait (sec)", _maxResurrectWait, 90));
        deathPage.Controls.Add(CreateSettingRow("Post Resurrect Delay (sec)", _postResurrectDelay, 130));

        _tabControl.TabPages.Add(deathPage);
    }

    /// <summary>
    /// Creates all map discovery tab controls.
    /// </summary>
    private void CreateMapDiscoveryTabControls()
    {
        _autoMappingEnabled = CreateCheckBox();
        _recordResourceNodes = CreateCheckBox();
        _recordNpcs = CreateCheckBox();
        _recordMobSpawns = CreateCheckBox();
        _mapDedupeRadius = CreateNumericUpDown(1, 20, 1);

        var mapPage = CreateTabPage("Map Discovery");
        mapPage.Controls.Add(CreateSettingRow("Auto Mapping Enabled", _autoMappingEnabled, 10));
        mapPage.Controls.Add(CreateSettingRow("Record Resource Nodes", _recordResourceNodes, 50));
        mapPage.Controls.Add(CreateSettingRow("Record NPCs", _recordNpcs, 90));
        mapPage.Controls.Add(CreateSettingRow("Record Mob Spawns", _recordMobSpawns, 130));
        mapPage.Controls.Add(CreateSettingRow("Dedupe Radius", _mapDedupeRadius, 170));

        _tabControl.TabPages.Add(mapPage);
    }

    /// <summary>
    /// Creates all hotkeys tab controls.
    /// </summary>
    private void CreateHotkeysTabControls()
    {
        _emergencyStopHotkey = CreateHotkeyComboBox();
        _pauseResumeHotkey = CreateHotkeyComboBox();

        var hotkeyPage = CreateTabPage("Hotkeys");
        hotkeyPage.Controls.Add(CreateSettingRow("Emergency Stop Hotkey", _emergencyStopHotkey, 10));
        hotkeyPage.Controls.Add(CreateSettingRow("Pause/Resume Hotkey", _pauseResumeHotkey, 50));

        _tabControl.TabPages.Add(hotkeyPage);
    }

    /// <summary>
    /// Creates all session limits tab controls.
    /// </summary>
    private void CreateSessionLimitsTabControls()
    {
        _maxSessionRuntime = CreateNumericUpDown(0, 1440, 10); // 0 = no limit, max 24 hours
        _maxStuckTime = CreateNumericUpDown(0, 600, 10); // 0 = no limit, max 10 minutes

        var sessionPage = CreateTabPage("Session Limits");
        sessionPage.Controls.Add(CreateSettingRow("Max Session Runtime (min, 0=unlimited)", _maxSessionRuntime, 10));
        sessionPage.Controls.Add(CreateSettingRow("Max Stuck Time (sec, 0=unlimited)", _maxStuckTime, 50));

        _tabControl.TabPages.Add(sessionPage);
    }

    /// <summary>
    /// Creates a new tab page with standard styling.
    /// </summary>
    private TabPage CreateTabPage(string text)
    {
        return new TabPage(text)
        {
            BackColor = Color.FromArgb(35, 35, 38),
            ForeColor = Color.White,
            Padding = new Padding(10)
        };
    }

    /// <summary>
    /// Creates a numeric up-down control with standard styling.
    /// </summary>
    private NumericUpDown CreateNumericUpDown(decimal min, decimal max, decimal increment)
    {
        return new NumericUpDown
        {
            Minimum = min,
            Maximum = max,
            Increment = increment,
            BackColor = Color.FromArgb(50, 50, 55),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Width = 100,
            TextAlign = HorizontalAlignment.Right
        };
    }

    /// <summary>
    /// Creates a checkbox with standard styling.
    /// </summary>
    private CheckBox CreateCheckBox()
    {
        return new CheckBox
        {
            BackColor = Color.FromArgb(35, 35, 38),
            ForeColor = Color.White,
            AutoSize = true
        };
    }

    /// <summary>
    /// Creates a text box with standard styling.
    /// </summary>
    private TextBox CreateTextBox()
    {
        return new TextBox
        {
            BackColor = Color.FromArgb(50, 50, 55),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Width = 200
        };
    }

    /// <summary>
    /// Creates a combo box for hotkey selection.
    /// </summary>
    private ComboBox CreateHotkeyComboBox()
    {
        var comboBox = new ComboBox
        {
            BackColor = Color.FromArgb(50, 50, 55),
            ForeColor = Color.White,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 120,
            FlatStyle = FlatStyle.Flat
        };

        // Add common hotkeys
        comboBox.Items.AddRange(new object[] { "F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8", "F9", "F10", "F11", "F12" });
        comboBox.SelectedIndex = 0;

        return comboBox;
    }

    /// <summary>
    /// Creates a setting row with label and control.
    /// </summary>
    private Control CreateSettingRow(string labelText, Control control, int y, bool multiline = false)
    {
        var panel = new Panel
        {
            Dock = DockStyle.Top,
            Height = multiline ? 50 : 35,
            BackColor = Color.Transparent
        };

        var label = new Label
        {
            Text = labelText,
            ForeColor = Color.FromArgb(200, 200, 200),
            Font = new Font("Segoe UI", 9F),
            AutoSize = true,
            Location = new Point(10, multiline ? 5 : 8)
        };

        if (multiline)
        {
            ((TextBox)control).Multiline = true;
            ((TextBox)control).Height = 40;
            ((TextBox)control).Width = 250;
            control.Location = new Point(220, 5);
        }
        else
        {
            control.Location = new Point(220, 5);
        }

        panel.Controls.Add(label);
        panel.Controls.Add(control);

        return panel;
    }

    /// <summary>
    /// Layouts the controls on the panel.
    /// </summary>
    private void LayoutControls()
    {
        // Create button panel at bottom
        var buttonPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 45,
            BackColor = Color.FromArgb(40, 40, 43)
        };

        _resetButton.Location = new Point(Width - 230, 8);
        _saveButton.Location = new Point(Width - 105, 8);
        _statusLabel.Location = new Point(10, 12);

        buttonPanel.Controls.Add(_statusLabel);
        buttonPanel.Controls.Add(_resetButton);
        buttonPanel.Controls.Add(_saveButton);

        Controls.Add(_tabControl);
        Controls.Add(buttonPanel);
    }

    /// <summary>
    /// Wires up event handlers.
    /// </summary>
    private void WireUpEvents()
    {
        _saveButton.Click += (s, e) => HandleSave();
        _resetButton.Click += (s, e) => HandleReset();
        _saveButton.MouseEnter += (s, e) => _saveButton.BackColor = Color.FromArgb(120, 200, 120);
        _saveButton.MouseLeave += (s, e) => _saveButton.BackColor = Color.FromArgb(100, 180, 100);
        _resetButton.MouseEnter += (s, e) => _resetButton.BackColor = Color.FromArgb(200, 200, 200);
        _resetButton.MouseLeave += (s, e) => _resetButton.BackColor = Color.FromArgb(180, 180, 180);
    }

    /// <summary>
    /// Loads current configuration values into the controls.
    /// </summary>
    private void LoadCurrentConfig()
    {
        var config = _workingConfig;

        // Combat
        _minHealthCombat.Value = (decimal)config.MinHealthPercentForCombat;
        _minManaCombat.Value = (decimal)config.MinManaPercentForCombat;
        _maxDeathCount.Value = config.MaxDeathCount;
        _combatTimeout.Value = (decimal)config.CombatTimeoutSeconds;
        _chaseFleeing.Checked = config.ChaseFleeingTargets;
        _maxChaseDistance.Value = (decimal)config.MaxChaseDistance;
        _maxAttackRange.Value = (decimal)config.MaxAttackRange;

        // Target Selection
        _maxLevelAbove.Value = config.MaxLevelAbove;
        _maxLevelBelow.Value = config.MaxLevelBelow;
        _maxSearchRadius.Value = (decimal)config.MaxSearchRadius;
        _maxWaypointDistance.Value = (decimal)config.MaxWaypointDistance;
        _prioritizeAttackers.Checked = config.PrioritizeAttackers;
        _blacklistedMobs.Text = string.Join(", ", config.BlacklistedMobNames);
        _blacklistedTypes.Text = string.Join(", ", config.BlacklistedTypes);

        // Rest & Recovery
        _minHealthRest.Value = (decimal)config.MinHealthPercentToRest;
        _minManaRest.Value = (decimal)config.MinManaPercentToRest;
        _targetHealthRest.Value = (decimal)config.TargetHealthPercentAfterRest;
        _targetManaRest.Value = (decimal)config.TargetManaPercentAfterRest;
        _maxRestDuration.Value = (decimal)config.MaxRestDurationSeconds;
        _foodItem.Text = config.FoodItem ?? string.Empty;
        _drinkItem.Text = config.DrinkItem ?? string.Empty;

        // Looting
        _autoLoot.Checked = config.AutoLoot;
        _lootDistance.Value = (decimal)config.LootDistance;
        _maxLootWait.Value = (decimal)config.MaxLootWaitSeconds;
        _skipLootWhenFull.Checked = config.SkipLootWhenFull;
        _minFreeBagSlots.Value = config.MinFreeBagSlots;

        // Navigation
        _stoppingDistance.Value = (decimal)config.NavigationStoppingDistance;
        _stuckThreshold.Value = (decimal)config.StuckDetectionThresholdSeconds;
        _maxUnstuckAttempts.Value = config.MaxUnstuckAttempts;
        _movementProgress.Value = (decimal)config.MovementProgressThreshold;
        _facingTolerance.Value = (decimal)config.FacingToleranceDegrees;

        // Waypoints
        _minWaypointDistance.Value = (decimal)config.MinWaypointDistance;
        _minWaypointInterval.Value = (decimal)config.MinWaypointRecordIntervalSeconds;

        // Input
        _inputDelay.Value = config.InputDelayMs;
        _inputRandomization.Value = config.InputRandomizationRangeMs;

        // Death
        _autoReleaseSpirit.Checked = config.AutoReleaseSpirit;
        _autoResurrectGraveyard.Checked = config.AutoResurrectAtGraveyard;
        _maxResurrectWait.Value = (decimal)config.MaxResurrectWaitSeconds;
        _postResurrectDelay.Value = (decimal)config.PostResurrectDelaySeconds;

        // Map Discovery
        _autoMappingEnabled.Checked = config.AutoMappingEnabled;
        _recordResourceNodes.Checked = config.RecordResourceNodes;
        _recordNpcs.Checked = config.RecordNpcs;
        _recordMobSpawns.Checked = config.RecordMobSpawns;
        _mapDedupeRadius.Value = (decimal)config.MapDeduplicationRadius;

        // Hotkeys
        SetHotkeySelection(_emergencyStopHotkey, config.EmergencyStopHotkey);
        SetHotkeySelection(_pauseResumeHotkey, config.PauseResumeHotkey);

        // Session Limits
        _maxSessionRuntime.Value = config.MaxSessionRuntimeMinutes;
        _maxStuckTime.Value = (decimal)config.MaxStuckTimeSeconds;
    }

    /// <summary>
    /// Sets the selected hotkey in a combo box.
    /// </summary>
    private void SetHotkeySelection(ComboBox comboBox, string hotkey)
    {
        int index = comboBox.Items.IndexOf(hotkey);
        comboBox.SelectedIndex = index >= 0 ? index : 0;
    }

    /// <summary>
    /// Handles the Save button click.
    /// </summary>
    private void HandleSave()
    {
        try
        {
            // Update working config from controls
            var config = _workingConfig;

            // Combat
            config.MinHealthPercentForCombat = (float)_minHealthCombat.Value;
            config.MinManaPercentForCombat = (float)_minManaCombat.Value;
            config.MaxDeathCount = (int)_maxDeathCount.Value;
            config.CombatTimeoutSeconds = (float)_combatTimeout.Value;
            config.ChaseFleeingTargets = _chaseFleeing.Checked;
            config.MaxChaseDistance = (float)_maxChaseDistance.Value;
            config.MaxAttackRange = (float)_maxAttackRange.Value;

            // Target Selection
            config.MaxLevelAbove = (int)_maxLevelAbove.Value;
            config.MaxLevelBelow = (int)_maxLevelBelow.Value;
            config.MaxSearchRadius = (float)_maxSearchRadius.Value;
            config.MaxWaypointDistance = (float)_maxWaypointDistance.Value;
            config.PrioritizeAttackers = _prioritizeAttackers.Checked;
            config.BlacklistedMobNames = ParseCommaSeparatedList(_blacklistedMobs.Text);
            config.BlacklistedTypes = ParseCommaSeparatedList(_blacklistedTypes.Text);

            // Rest & Recovery
            config.MinHealthPercentToRest = (float)_minHealthRest.Value;
            config.MinManaPercentToRest = (float)_minManaRest.Value;
            config.TargetHealthPercentAfterRest = (float)_targetHealthRest.Value;
            config.TargetManaPercentAfterRest = (float)_targetManaRest.Value;
            config.MaxRestDurationSeconds = (float)_maxRestDuration.Value;
            config.FoodItem = string.IsNullOrWhiteSpace(_foodItem.Text) ? null : _foodItem.Text.Trim();
            config.DrinkItem = string.IsNullOrWhiteSpace(_drinkItem.Text) ? null : _drinkItem.Text.Trim();

            // Looting
            config.AutoLoot = _autoLoot.Checked;
            config.LootDistance = (float)_lootDistance.Value;
            config.MaxLootWaitSeconds = (float)_maxLootWait.Value;
            config.SkipLootWhenFull = _skipLootWhenFull.Checked;
            config.MinFreeBagSlots = (int)_minFreeBagSlots.Value;

            // Navigation
            config.NavigationStoppingDistance = (float)_stoppingDistance.Value;
            config.StuckDetectionThresholdSeconds = (float)_stuckThreshold.Value;
            config.MaxUnstuckAttempts = (int)_maxUnstuckAttempts.Value;
            config.MovementProgressThreshold = (float)_movementProgress.Value;
            config.FacingToleranceDegrees = (float)_facingTolerance.Value;

            // Waypoints
            config.MinWaypointDistance = (float)_minWaypointDistance.Value;
            config.MinWaypointRecordIntervalSeconds = (float)_minWaypointInterval.Value;

            // Input
            config.InputDelayMs = (int)_inputDelay.Value;
            config.InputRandomizationRangeMs = (int)_inputRandomization.Value;

            // Death
            config.AutoReleaseSpirit = _autoReleaseSpirit.Checked;
            config.AutoResurrectAtGraveyard = _autoResurrectGraveyard.Checked;
            config.MaxResurrectWaitSeconds = (float)_maxResurrectWait.Value;
            config.PostResurrectDelaySeconds = (float)_postResurrectDelay.Value;

            // Map Discovery
            config.AutoMappingEnabled = _autoMappingEnabled.Checked;
            config.RecordResourceNodes = _recordResourceNodes.Checked;
            config.RecordNpcs = _recordNpcs.Checked;
            config.RecordMobSpawns = _recordMobSpawns.Checked;
            config.MapDeduplicationRadius = (float)_mapDedupeRadius.Value;

            // Hotkeys
            config.EmergencyStopHotkey = _emergencyStopHotkey.SelectedItem?.ToString() ?? "F12";
            config.PauseResumeHotkey = _pauseResumeHotkey.SelectedItem?.ToString() ?? "F11";

            // Session Limits
            config.MaxSessionRuntimeMinutes = (int)_maxSessionRuntime.Value;
            config.MaxStuckTimeSeconds = (float)_maxStuckTime.Value;

            // Save to file
            ConfigManager.Save(config);

            // Update status
            SetStatus("Settings saved successfully!", Color.FromArgb(100, 200, 100));

            // Trigger config updated event through UpdateConfig method
            // Note: Using reflection since BotController is not in the interface
            var updateConfigMethod = _configProvider.GetType().GetMethod("UpdateConfig");
            updateConfigMethod?.Invoke(_configProvider, new object[] { config });
        }
        catch (Exception ex)
        {
            SetStatus($"Error saving settings: {ex.Message}", Color.FromArgb(255, 100, 100));
        }
    }

    /// <summary>
    /// Handles the Reset button click.
    /// </summary>
    private void HandleReset()
    {
        _workingConfig = CloneConfig(_configProvider.CurrentConfig);
        LoadCurrentConfig();
        SetStatus("Settings reset to current values.", Color.FromArgb(150, 150, 150));
    }

    /// <summary>
    /// Parses a comma-separated list into a List<string>.
    /// </summary>
    private List<string> ParseCommaSeparatedList(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new List<string>();

        return text.Split(',')
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrEmpty(s))
            .ToList();
    }

    /// <summary>
    /// Creates a deep copy of the configuration.
    /// </summary>
    private BotConfig CloneConfig(BotConfig source)
    {
        // Simple clone using JSON serialization
        var json = System.Text.Json.JsonSerializer.Serialize(source);
        return System.Text.Json.JsonSerializer.Deserialize<BotConfig>(json)
            ?? new BotConfig();
    }

    /// <summary>
    /// Sets the status label with the specified message and color.
    /// </summary>
    private void SetStatus(string message, Color color)
    {
        _statusLabel.Text = message;
        _statusLabel.ForeColor = color;
    }

    /// <summary>
    /// Reloads the configuration from the provider.
    /// </summary>
    public void ReloadConfig()
    {
        _workingConfig = CloneConfig(_configProvider.CurrentConfig);
        LoadCurrentConfig();
    }
}
