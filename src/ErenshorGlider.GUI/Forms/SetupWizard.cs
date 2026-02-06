using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using ErenshorGlider.GUI.Installation;

namespace ErenshorGlider.GUI.Forms;

/// <summary>
/// First-run setup wizard for guiding users through initial installation.
/// </summary>
public class SetupWizard : Form
{
    private readonly IInstallationService _installationService;
    private readonly Panel _contentPanel;
    private readonly Label _titleLabel;
    private readonly Label _descriptionLabel;
    private readonly Button _backButton;
    private readonly Button _nextButton;
    private readonly Button _cancelButton;
    private readonly ProgressBar _progressBar;
    private readonly Label _progressLabel;

    private int _currentStep = 0;
    private string? _erenshorPath;
    private string? _bepInExArchivePath;

    /// <summary>
    /// Creates a new SetupWizard.
    /// </summary>
    /// <param name="installationService">The installation service.</param>
    public SetupWizard(IInstallationService installationService)
    {
        _installationService = installationService ?? throw new ArgumentNullException(nameof(installationService));

        // Form properties
        Text = "Erenshor Glider - Setup Wizard";
        Size = new Size(650, 500);
        MinimumSize = new Size(600, 450);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = true;
        BackColor = Color.FromArgb(30, 30, 30);
        ForeColor = Color.White;

        // Create controls
        _titleLabel = CreateTitleLabel();
        _descriptionLabel = CreateDescriptionLabel();
        _contentPanel = CreateContentPanel();
        _progressBar = CreateProgressBar();
        _progressLabel = CreateProgressLabel();
        _backButton = CreateBackButton();
        _nextButton = CreateNextButton();
        _cancelButton = CreateCancelButton();

        // Layout controls
        LayoutControls();
        WireUpEvents();

        // Show first step
        ShowStep(0);
    }

    /// <summary>
    /// Creates the title label.
    /// </summary>
    private Label CreateTitleLabel()
    {
        return new Label
        {
            Text = "Welcome to Erenshor Glider",
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 16F, FontStyle.Bold),
            Location = new Point(20, 20),
            AutoSize = true
        };
    }

    /// <summary>
    /// Creates the description label.
    /// </summary>
    private Label CreateDescriptionLabel()
    {
        return new Label
        {
            Text = "This wizard will guide you through the installation process.",
            ForeColor = Color.FromArgb(200, 200, 200),
            Font = new Font("Segoe UI", 10F),
            Location = new Point(20, 55),
            AutoSize = true
        };
    }

    /// <summary>
    /// Creates the content panel.
    /// </summary>
    private Panel CreateContentPanel()
    {
        return new Panel
        {
            Location = new Point(20, 90),
            Size = new Size(590, 300),
            BackColor = Color.FromArgb(40, 40, 43),
            BorderStyle = BorderStyle.FixedSingle
        };
    }

    /// <summary>
    /// Creates the progress bar.
    /// </summary>
    private ProgressBar CreateProgressBar()
    {
        return new ProgressBar
        {
            Location = new Point(20, 405),
            Size = new Size(550, 20),
            Style = ProgressBarStyle.Continuous,
            Visible = false
        };
    }

    /// <summary>
    /// Creates the progress label.
    /// </summary>
    private Label CreateProgressLabel()
    {
        return new Label
        {
            Text = "",
            ForeColor = Color.FromArgb(200, 200, 200),
            Font = new Font("Segoe UI", 9F),
            Location = new Point(20, 428),
            AutoSize = true,
            Visible = false
        };
    }

    /// <summary>
    /// Creates the Back button.
    /// </summary>
    private Button CreateBackButton()
    {
        return new Button
        {
            Text = "< Back",
            BackColor = Color.FromArgb(70, 70, 75),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10F),
            Size = new Size(90, 35),
            UseVisualStyleBackColor = false,
            Cursor = Cursors.Hand
        };
    }

    /// <summary>
    /// Creates the Next button.
    /// </summary>
    private Button CreateNextButton()
    {
        return new Button
        {
            Text = "Next >",
            BackColor = Color.FromArgb(70, 130, 180),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            Size = new Size(90, 35),
            UseVisualStyleBackColor = false,
            Cursor = Cursors.Hand
        };
    }

    /// <summary>
    /// Creates the Cancel button.
    /// </summary>
    private Button CreateCancelButton()
    {
        return new Button
        {
            Text = "Cancel",
            BackColor = Color.FromArgb(180, 70, 70),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10F),
            Size = new Size(90, 35),
            UseVisualStyleBackColor = false,
            Cursor = Cursors.Hand
        };
    }

    /// <summary>
    /// Layouts the controls on the form.
    /// </summary>
    private void LayoutControls()
    {
        Controls.Add(_titleLabel);
        Controls.Add(_descriptionLabel);
        Controls.Add(_contentPanel);
        Controls.Add(_progressBar);
        Controls.Add(_progressLabel);

        // Position buttons at bottom right
        _cancelButton.Location = new Point(540, 425);
        _nextButton.Location = new Point(440, 425);
        _backButton.Location = new Point(340, 425);

        Controls.Add(_backButton);
        Controls.Add(_nextButton);
        Controls.Add(_cancelButton);
    }

    /// <summary>
    /// Wires up event handlers.
    /// </summary>
    private void WireUpEvents()
    {
        _backButton.Click += async (s, e) => await HandleBackClick();
        _nextButton.Click += async (s, e) => await HandleNextClick();
        _cancelButton.Click += (s, e) => HandleCancelClick();

        // Button hover effects
        _backButton.MouseEnter += (s, e) => _backButton.BackColor = Color.FromArgb(90, 90, 95);
        _backButton.MouseLeave += (s, e) => _backButton.BackColor = Color.FromArgb(70, 70, 75);

        _nextButton.MouseEnter += (s, e) => _nextButton.BackColor = Color.FromArgb(90, 150, 200);
        _nextButton.MouseLeave += (s, e) => _nextButton.BackColor = Color.FromArgb(70, 130, 180);

        _cancelButton.MouseEnter += (s, e) => _cancelButton.BackColor = Color.FromArgb(200, 90, 90);
        _cancelButton.MouseLeave += (s, e) => _cancelButton.BackColor = Color.FromArgb(180, 70, 70);
    }

    /// <summary>
    /// Shows the specified wizard step.
    /// </summary>
    /// <param name="step">The step number (0-4).</param>
    private void ShowStep(int step)
    {
        _currentStep = step;
        _contentPanel.Controls.Clear();

        switch (step)
        {
            case 0:
                ShowWelcomeStep();
                break;
            case 1:
                ShowErenshorDetectionStep();
                break;
            case 2:
                ShowBepInExInstallationStep();
                break;
            case 3:
                ShowPluginInstallationStep();
                break;
            case 4:
                ShowCompleteStep();
                break;
        }

        UpdateButtonStates();
    }

    /// <summary>
    /// Shows the welcome step.
    /// </summary>
    private void ShowWelcomeStep()
    {
        _titleLabel.Text = "Welcome to Erenshor Glider";
        _descriptionLabel.Text = "This wizard will guide you through the installation process.";

        var content = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20)
        };

        var welcomeLabel = new Label
        {
            Text = "Thank you for installing Erenshor Glider!\n\n" +
                   "This bot automates gameplay in Erenshor, including:\n" +
                   "• Automatic grinding and combat\n" +
                   "• Waypoint-based pathing\n" +
                   "• Automatic looting and resting\n" +
                   "• 2D radar for nearby entities\n\n" +
                   "Before you can start, we need to install BepInEx (the mod loader) " +
                   "and configure your Erenshor installation path.",
            ForeColor = Color.FromArgb(220, 220, 220),
            Font = new Font("Segoe UI", 10F),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.TopCenter
        };

        content.Controls.Add(welcomeLabel);
        _contentPanel.Controls.Add(content);
    }

    /// <summary>
    /// Shows the Erenshor detection step.
    /// </summary>
    private void ShowErenshorDetectionStep()
    {
        _titleLabel.Text = "Locate Erenshor Installation";
        _descriptionLabel.Text = "We need to find your Erenshor installation directory.";

        var content = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20)
        };

        var infoLabel = new Label
        {
            Text = "Please select your Erenshor installation folder.\n\n" +
                   "This is where Erenshor.exe is located.",
            ForeColor = Color.FromArgb(200, 200, 200),
            Font = new Font("Segoe UI", 10F),
            Location = new Point(0, 20),
            Width = 550,
            Height = 60
        };

        var pathTextBox = new TextBox
        {
            Location = new Point(0, 90),
            Width = 450,
            Height = 25,
            Text = _erenshorPath ?? "Select your Erenshor installation folder...",
            ForeColor = Color.White,
            BackColor = Color.FromArgb(50, 50, 55),
            BorderStyle = BorderStyle.FixedSingle,
            ReadOnly = true
        };

        var browseButton = new Button
        {
            Text = "Browse...",
            Location = new Point(460, 88),
            Size = new Size(100, 30),
            BackColor = Color.FromArgb(70, 130, 180),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            UseVisualStyleBackColor = false,
            Cursor = Cursors.Hand
        };

        browseButton.Click += async (s, e) => await HandleBrowseClick(pathTextBox);

        browseButton.MouseEnter += (s, e) => browseButton.BackColor = Color.FromArgb(90, 150, 200);
        browseButton.MouseLeave += (s, e) => browseButton.BackColor = Color.FromArgb(70, 130, 180);

        var autoDetectLabel = new Label
        {
            Text = "Or click below to auto-detect from Steam",
            ForeColor = Color.FromArgb(180, 180, 180),
            Font = new Font("Segoe UI", 9F),
            Location = new Point(0, 140),
            AutoSize = true
        };

        var autoDetectButton = new Button
        {
            Text = "Auto-Detect from Steam",
            Location = new Point(0, 165),
            Size = new Size(160, 35),
            BackColor = Color.FromArgb(100, 160, 100),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            UseVisualStyleBackColor = false,
            Cursor = Cursors.Hand
        };

        autoDetectButton.Click += async (s, e) => await HandleAutoDetectClick(pathTextBox);

        autoDetectButton.MouseEnter += (s, e) => autoDetectButton.BackColor = Color.FromArgb(120, 180, 120);
        autoDetectButton.MouseLeave += (s, e) => autoDetectButton.BackColor = Color.FromArgb(100, 160, 100);

        content.Controls.AddRange(new Control[]
        {
            infoLabel, pathTextBox, browseButton, autoDetectLabel, autoDetectButton
        });

        _contentPanel.Controls.Add(content);
    }

    /// <summary>
    /// Shows the BepInEx installation step.
    /// </summary>
    private void ShowBepInExInstallationStep()
    {
        _titleLabel.Text = "Install BepInEx";
        _descriptionLabel.Text = "BepInEx is required to load the Erenshor Glider plugin.";

        var content = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20)
        };

        var infoLabel = new Label
        {
            Text = "BepInEx is a plugin framework that allows Erenshor Glider to run.\n\n" +
                   "Click Next to download and install BepInEx to your Erenshor folder.",
            ForeColor = Color.FromArgb(220, 220, 220),
            Font = new Font("Segoe UI", 10F),
            Location = new Point(0, 20),
            Width = 550
        };

        var pathLabel = new Label
        {
            Text = $"Target: {_erenshorPath}",
            ForeColor = Color.FromArgb(150, 150, 150),
            Font = new Font("Consolas", 9F),
            Location = new Point(0, 120),
            AutoSize = true
        };

        content.Controls.AddRange(new Control[] { infoLabel, pathLabel });
        _contentPanel.Controls.Add(content);
    }

    /// <summary>
    /// Shows the plugin installation step.
    /// </summary>
    private void ShowPluginInstallationStep()
    {
        _titleLabel.Text = "Install Erenshor Glider Plugin";
        _descriptionLabel.Text = "Copy the plugin DLL to BepInEx plugins folder.";

        var content = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20)
        };

        var infoLabel = new Label
        {
            Text = "Finally, we'll copy the Erenshor Glider plugin to the BepInEx plugins folder.\n\n" +
                   "Click Next to complete the installation.",
            ForeColor = Color.FromArgb(220, 220, 220),
            Font = new Font("Segoe UI", 10F),
            Location = new Point(0, 20),
            Width = 550
        };

        content.Controls.Add(infoLabel);
        _contentPanel.Controls.Add(content);
    }

    /// <summary>
    /// Shows the completion step.
    /// </summary>
    private void ShowCompleteStep()
    {
        _titleLabel.Text = "Installation Complete!";
        _descriptionLabel.Text = "Erenshor Glider is now ready to use.";

        var content = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20)
        };

        var successLabel = new Label
        {
            Text = "✓",
            ForeColor = Color.FromArgb(100, 180, 100),
            Font = new Font("Segoe UI", 48F),
            Location = new Point(250, 30),
            AutoSize = true
        };

        var messageLabel = new Label
        {
            Text = "Erenshor Glider has been successfully installed!\n\n" +
                   "You can now:\n" +
                   "• Launch Erenshor from the main window\n" +
                   "• Configure combat profiles and waypoints\n" +
                   "• Start the bot and automate your gameplay\n\n" +
                   "Click 'Finish' to close this wizard.",
            ForeColor = Color.FromArgb(220, 220, 220),
            Font = new Font("Segoe UI", 10F),
            Location = new Point(50, 100),
            Width = 490,
            Height = 180
        };

        content.Controls.AddRange(new Control[] { successLabel, messageLabel });
        _contentPanel.Controls.Add(content);

        _nextButton.Text = "Finish";
    }

    /// <summary>
    /// Handles the Browse button click.
    /// </summary>
    private async Task HandleBrowseClick(TextBox pathTextBox)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Select your Erenshor installation folder",
            ShowNewFolderButton = false
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            var selectedPath = dialog.SelectedPath;

            // Validate path contains Erenshor.exe
            if (!File.Exists(Path.Combine(selectedPath, "Erenshor.exe")))
            {
                MessageBox.Show(
                    "Erenshor.exe was not found in the selected folder.\n\n" +
                    "Please select the folder where Erenshor.exe is located.",
                    "Invalid Folder",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            _erenshorPath = selectedPath;
            pathTextBox.Text = selectedPath;
        }
    }

    /// <summary>
    /// Handles the Auto-Detect button click.
    /// </summary>
    private async Task HandleAutoDetectClick(TextBox pathTextBox)
    {
        try
        {
            var detectedPath = await _installationService.DetectErenshorPathAsync();

            if (string.IsNullOrEmpty(detectedPath))
            {
                MessageBox.Show(
                    "Could not auto-detect Erenshor installation.\n\n" +
                    "Please make sure Erenshor is installed via Steam, or use the Browse button to manually select the folder.",
                    "Detection Failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            _erenshorPath = detectedPath;
            pathTextBox.Text = detectedPath;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"An error occurred during detection:\n{ex.Message}",
                "Detection Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// Handles the Back button click.
    /// </summary>
    private async Task HandleBackClick()
    {
        if (_currentStep > 0)
        {
            ShowStep(_currentStep - 1);
        }
    }

    /// <summary>
    /// Handles the Next button click.
    /// </summary>
    private async Task HandleNextClick()
    {
        // Validate current step before proceeding
        if (_currentStep == 1 && string.IsNullOrEmpty(_erenshorPath))
        {
            MessageBox.Show(
                "Please select your Erenshor installation folder first.",
                "Path Required",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        // Perform installation steps
        if (_currentStep == 2)
        {
            await InstallBepInEx();
            return;
        }

        if (_currentStep == 3)
        {
            await InstallPlugin();
            return;
        }

        if (_currentStep == 4)
        {
            // Complete - ask if user wants to launch game
            var result = MessageBox.Show(
                "Setup complete! Would you like to launch Erenshor now?",
                "Launch Game",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                await LaunchGame();
            }
            else
            {
                Close();
            }
            return;
        }

        // Move to next step
        ShowStep(_currentStep + 1);
    }

    /// <summary>
    /// Handles the Cancel button click.
    /// </summary>
    private void HandleCancelClick()
    {
        var result = MessageBox.Show(
            "Are you sure you want to cancel the setup?\n\n" +
            "Any progress made so far will be lost.",
            "Cancel Setup",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result == DialogResult.Yes)
        {
            Close();
        }
    }

    /// <summary>
    /// Installs BepInEx.
    /// </summary>
    private async Task InstallBepInEx()
    {
        SetInstallationInProgress(true);
        _progressLabel.Text = "Downloading BepInEx...";
        _progressLabel.Visible = true;
        _progressBar.Visible = true;
        _progressBar.Style = ProgressBarStyle.Continuous;
        _progressBar.Value = 0;

        try
        {
            // Download BepInEx
            var progress = new Progress<DownloadProgress>(p =>
            {
                _progressBar.Value = p.Percentage > 0 ? p.Percentage / 2 : 0; // Download is half the work
                _progressLabel.Text = $"Downloading BepInEx... {p.Percentage}%";
            });

            _bepInExArchivePath = await _installationService.DownloadBepInExAsync(progress);

            if (string.IsNullOrEmpty(_bepInExArchivePath))
            {
                MessageBox.Show(
                    "Failed to download BepInEx. Please check your internet connection and try again.",
                    "Download Failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                SetInstallationInProgress(false);
                return;
            }

            _progressLabel.Text = "Installing BepInEx...";
            _progressBar.Value = 50;

            // Install BepInEx
            var result = await _installationService.InstallBepInExAsync(_erenshorPath!, _bepInExArchivePath!);

            _progressBar.Value = 100;

            if (!result.Success)
            {
                MessageBox.Show(
                    $"Failed to install BepInEx:\n{result.ErrorMessage}",
                    "Installation Failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                SetInstallationInProgress(false);
                return;
            }

            // Save config
            _installationService.Config!.ErenshorPath = _erenshorPath;
            _installationService.SaveConfig();

            await Task.Delay(500); // Brief pause to show completion
            SetInstallationInProgress(false);
            ShowStep(3); // Move to plugin installation
        }
        catch (Exception ex)
        {
            SetInstallationInProgress(false);
            MessageBox.Show(
                $"An error occurred during installation:\n{ex.Message}",
                "Installation Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// Installs the plugin.
    /// </summary>
    private async Task InstallPlugin()
    {
        SetInstallationInProgress(true);
        _progressLabel.Text = "Installing plugin...";
        _progressLabel.Visible = true;
        _progressBar.Visible = true;
        _progressBar.Style = ProgressBarStyle.Marquee;

        try
        {
            // Get plugin DLL path
            var guiDirectory = Path.GetDirectoryName(Application.ExecutablePath);
            var pluginDllPath = Path.Combine(guiDirectory!, "ErenshorGlider.dll");

            if (!File.Exists(pluginDllPath))
            {
                MessageBox.Show(
                    $"Plugin DLL not found at: {pluginDllPath}\n\n" +
                    "Please ensure ErenshorGlider.dll is in the same folder as this GUI application.",
                    "File Not Found",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                SetInstallationInProgress(false);
                return;
            }

            var result = await _installationService.InstallPluginAsync(pluginDllPath, _erenshorPath!);

            if (!result.Success)
            {
                MessageBox.Show(
                    $"Failed to install plugin:\n{result.ErrorMessage}",
                    "Installation Failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                SetInstallationInProgress(false);
                return;
            }

            await Task.Delay(500);
            SetInstallationInProgress(false);
            ShowStep(4); // Move to completion
        }
        catch (Exception ex)
        {
            SetInstallationInProgress(false);
            MessageBox.Show(
                $"An error occurred during installation:\n{ex.Message}",
                "Installation Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// Launches the game.
    /// </summary>
    private async Task LaunchGame()
    {
        try
        {
            var result = await _installationService.LaunchGameAsync(_erenshorPath!);

            if (result.Success)
            {
                Close();
            }
            else
            {
                MessageBox.Show(
                    $"Failed to launch Erenshor:\n{result.ErrorMessage}\n\n" +
                    "You can launch it later from the main window.",
                    "Launch Failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                Close();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"An error occurred while launching the game:\n{ex.Message}\n\n" +
                "You can launch it later from the main window.",
                "Launch Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            Close();
        }
    }

    /// <summary>
    /// Sets the installation in progress state.
    /// </summary>
    /// <param name="inProgress">True if installation is in progress.</param>
    private void SetInstallationInProgress(bool inProgress)
    {
        _backButton.Enabled = !inProgress;
        _nextButton.Enabled = !inProgress;
        _cancelButton.Enabled = !inProgress;
    }

    /// <summary>
    /// Updates button states based on current step.
    /// </summary>
    private void UpdateButtonStates()
    {
        _backButton.Enabled = _currentStep > 0;
        _nextButton.Text = _currentStep == 4 ? "Finish" : "Next >";
    }

    /// <summary>
    /// Clean up resources when the form is disposed.
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // Clean up any resources
        }
        base.Dispose(disposing);
    }
}
