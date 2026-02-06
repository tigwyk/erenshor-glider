using System;
using System.Windows.Forms;

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

        var mainWindow = new MainWindow();
        Application.Run(mainWindow);
    }
}
