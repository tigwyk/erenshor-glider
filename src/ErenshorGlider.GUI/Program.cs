using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using ErenshorGlider.GUI.Installation;
using ErenshorGlider.GUI.Forms;

namespace ErenshorGlider.GUI;

/// <summary>
/// Main entry point for the Erenshor Glider GUI application.
/// </summary>
internal static class Program
{
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        // Create installation service
        var installationService = new InstallationService();

        // Check if first run (no Erenshor path configured or BepInEx not installed)
        bool shouldRunWizard = ShouldRunSetupWizard(installationService);

        if (shouldRunWizard)
        {
            // Run the setup wizard first
            RunSetupWizardAndShowMainWindow(installationService);
        }
        else
        {
            // Show main window directly
            var mainWindow = new MainWindow(new MockBotController(), installationService);
            Application.Run(mainWindow);
        }
    }

    /// <summary>
    /// Determines whether the setup wizard should run based on installation state.
    /// </summary>
    /// <param name="installationService">The installation service.</param>
    /// <returns>True if setup wizard should run, false otherwise.</returns>
    private static bool ShouldRunSetupWizard(IInstallationService installationService)
    {
        var config = installationService.Config;

        // If no Erenshor path is configured, run the wizard
        if (string.IsNullOrEmpty(config?.ErenshorPath))
        {
            return true;
        }

        // Check if BepInEx is installed (we know config.ErenshorPath is not null here due to the check above)
        var bepInExStatus = installationService.GetBepInExStatusAsync(config!.ErenshorPath!).GetAwaiter().GetResult();
        if (bepInExStatus == InstallationStatus.NotInstalled)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Runs the setup wizard and then shows the main window.
    /// </summary>
    /// <param name="installationService">The installation service.</param>
    private static void RunSetupWizardAndShowMainWindow(IInstallationService installationService)
    {
        // Show splash screen while checking
        using (var splashForm = CreateSplashForm())
        {
            splashForm.Show();
            Application.DoEvents();

            // Give the splash a moment to render
            System.Threading.Thread.Sleep(500);

            splashForm.Close();
        }

        // Run setup wizard
        using (var wizard = new SetupWizard(installationService))
        {
            // Show the setup wizard and continue after it closes, regardless of DialogResult
            wizard.ShowDialog();
        }

        // Show main window after wizard completes
        var mainWindow = new MainWindow(new MockBotController(), installationService);
        Application.Run(mainWindow);
    }

    /// <summary>
    /// Creates a simple splash form for first-run detection.
    /// </summary>
    private static Form CreateSplashForm()
    {
        var form = new Form
        {
            Text = "Erenshor Glider",
            Size = new Size(400, 200),
            StartPosition = FormStartPosition.CenterScreen,
            FormBorderStyle = FormBorderStyle.None,
            ShowInTaskbar = false,
            TopMost = true,
            BackColor = Color.FromArgb(30, 30, 30)
        };

        var titleLabel = new Label
        {
            Text = "Erenshor Glider",
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 18F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Top,
            Height = 60
        };

        var subtitleLabel = new Label
        {
            Text = "Checking installation...",
            ForeColor = Color.FromArgb(200, 200, 200),
            Font = new Font("Segoe UI", 10F),
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Fill
        };

        form.Controls.Add(subtitleLabel);
        form.Controls.Add(titleLabel);

        return form;
    }
}
