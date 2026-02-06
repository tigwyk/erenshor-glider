using System;
using System.Drawing;
using System.Windows.Forms;

namespace ErenshorGlider.GUI.Controls;

/// <summary>
/// Panel displaying action log entries.
/// </summary>
public class ActionLogPanel : Panel
{
    private readonly RichTextBox _logTextBox;
    private readonly IActionLogProvider _logProvider;
    private readonly Label _clearButton;
    private bool _autoScroll = true;
    private int _lastEntryCount = 0;

    /// <summary>
    /// Creates a new ActionLogPanel.
    /// </summary>
    /// <param name="logProvider">The log provider to use.</param>
    public ActionLogPanel(IActionLogProvider logProvider)
    {
        _logProvider = logProvider ?? throw new ArgumentNullException(nameof(logProvider));

        BackColor = Color.FromArgb(40, 40, 43);
        BorderStyle = BorderStyle.FixedSingle;
        Padding = new Padding(5);

        // Create controls
        _logTextBox = CreateLogTextBox();
        _clearButton = CreateClearButton();

        // Layout controls
        LayoutControls();

        // Wire up events
        WireUpEvents();

        // Load initial entries
        LoadInitialEntries();
    }

    /// <summary>
    /// Creates the log RichTextBox.
    /// </summary>
    private RichTextBox CreateLogTextBox()
    {
        return new RichTextBox
        {
            BackColor = Color.FromArgb(20, 20, 20),
            ForeColor = Color.FromArgb(220, 220, 220),
            Font = new Font("Consolas", 9F),
            BorderStyle = BorderStyle.None,
            ReadOnly = true,
            ScrollBars = RichTextBoxScrollBars.Vertical,
            WordWrap = false,
            Dock = DockStyle.Fill
        };
    }

    /// <summary>
    /// Creates the clear button.
    /// </summary>
    private Label CreateClearButton()
    {
        return new Label
        {
            Text = "Clear",
            ForeColor = Color.FromArgb(150, 150, 150),
            Font = new Font("Segoe UI", 8F),
            Cursor = Cursors.Hand,
            TextAlign = ContentAlignment.MiddleCenter,
            Width = 50,
            Height = 20
        };
    }

    /// <summary>
    /// Layouts the controls on the panel.
    /// </summary>
    private void LayoutControls()
    {
        // Create header panel for clear button
        var headerPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 25,
            BackColor = Color.FromArgb(35, 35, 38)
        };

        _clearButton.Location = new Point(Width - 55, 3);
        headerPanel.Controls.Add(_clearButton);

        var titleLabel = new Label
        {
            Text = "Action Log",
            ForeColor = Color.FromArgb(180, 180, 180),
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            Dock = DockStyle.Left,
            Padding = new Padding(5, 3, 0, 0)
        };
        headerPanel.Controls.Add(titleLabel);

        // Add controls
        Controls.Add(_logTextBox);
        Controls.Add(headerPanel);
    }

    /// <summary>
    /// Wires up event handlers.
    /// </summary>
    private void WireUpEvents()
    {
        // Log entry added event
        _logProvider.LogEntryAdded += (s, entry) =>
        {
            if (InvokeRequired)
            {
                try
                {
                    Invoke(new Action<LogEntry>(AddLogEntry), entry);
                }
                catch { } // Form might be closing
                return;
            }
            AddLogEntry(entry);
        };

        // Clear button click
        _clearButton.Click += (s, e) => _logProvider.Clear();

        // Clear button hover effects
        _clearButton.MouseEnter += (s, e) =>
        {
            _clearButton.ForeColor = Color.FromArgb(200, 200, 200);
            _clearButton.Font = new Font("Segoe UI", 8F, FontStyle.Underline);
        };

        _clearButton.MouseLeave += (s, e) =>
        {
            _clearButton.ForeColor = Color.FromArgb(150, 150, 150);
            _clearButton.Font = new Font("Segoe UI", 8F);
        };

        // Detect user scroll to pause auto-scroll
        _logTextBox.VScroll += (s, e) =>
        {
            // If user scrolled up (not at bottom), disable auto-scroll
            _autoScroll = _logTextBox.GetScrollState(1, 0) == 0; // SB_BOTTOM = 0
        };
    }

    /// <summary>
    /// Loads initial log entries.
    /// </summary>
    private void LoadInitialEntries()
    {
        var entries = _logProvider.GetRecentEntries(100);
        foreach (var entry in entries)
        {
            AppendLogEntry(entry, false);
        }
        _lastEntryCount = entries.Length;
        ScrollToBottom();
    }

    /// <summary>
    /// Adds a log entry to the display.
    /// </summary>
    private void AddLogEntry(LogEntry entry)
    {
        AppendLogEntry(entry, true);

        // Auto-scroll if enabled
        if (_autoScroll)
        {
            ScrollToBottom();
        }
    }

    /// <summary>
    /// Appends a log entry to the text box.
    /// </summary>
    private void AppendLogEntry(LogEntry entry, bool selectText)
    {
        // Save current selection
        int startPos = _logTextBox.SelectionStart;
        int startLen = _logTextBox.SelectionLength;

        // Move to end
        _logTextBox.SelectionStart = _logTextBox.TextLength;
        _logTextBox.SelectionLength = 0;

        // Append formatted entry with color
        var color = Color.FromArgb(entry.Color);
        _logTextBox.SelectionColor = color;
        _logTextBox.AppendText($"[{entry.FormattedTimestamp}] ");

        // Category-based color for the category name
        var categoryColor = Color.FromArgb(entry.Color);
        _logTextBox.SelectionColor = categoryColor;
        _logTextBox.AppendText($"[{entry.Category}] ");

        // Message in default color
        _logTextBox.SelectionColor = Color.FromArgb(220, 220, 220);
        _logTextBox.AppendLine(entry.Message);

        // Restore selection or select new text
        if (selectText)
        {
            _logTextBox.SelectionStart = _logTextBox.TextLength;
            _logTextBox.SelectionLength = 0;
        }
        else
        {
            _logTextBox.SelectionStart = startPos;
            _logTextBox.SelectionLength = startLen;
        }
    }

    /// <summary>
    /// Scrolls the text box to the bottom.
    /// </summary>
    private void ScrollToBottom()
    {
        _logTextBox.SelectionStart = _logTextBox.TextLength;
        _logTextBox.ScrollToCaret();
    }

    /// <summary>
    /// Clears the log display.
    /// </summary>
    public void ClearLog()
    {
        _logTextBox.Clear();
    }
}
