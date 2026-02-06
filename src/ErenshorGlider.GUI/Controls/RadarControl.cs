using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ErenshorGlider.GUI;

namespace ErenshorGlider.GUI.Controls;

/// <summary>
/// Control displaying a 2D radar of nearby entities.
/// </summary>
public class RadarControl : Panel
{
    private readonly IRadarDataProvider _dataProvider;
    private readonly PictureBox _radarCanvas;
    private readonly Timer _updateTimer;
    private readonly Label _zoomLabel;
    private readonly Button _zoomInButton;
    private readonly Button _zoomOutButton;
    private readonly TrackBar _rangeSlider;

    private float _zoomLevel = 1.0f;
    private const float MinZoom = 0.5f;
    private const float MaxZoom = 3.0f;
    private const float ZoomStep = 0.25f;

    /// <summary>
    /// Creates a new RadarControl.
    /// </summary>
    /// <param name="dataProvider">The radar data provider.</param>
    public RadarControl(IRadarDataProvider dataProvider)
    {
        _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));

        BackColor = Color.FromArgb(40, 40, 43);
        BorderStyle = BorderStyle.FixedSingle;
        Padding = new Padding(5);
        MinimumSize = new Size(250, 300);

        // Create controls
        _radarCanvas = CreateRadarCanvas();
        _zoomLabel = CreateZoomLabel();
        _zoomInButton = CreateZoomButton("+");
        _zoomOutButton = CreateZoomButton("-");
        _rangeSlider = CreateRangeSlider();

        // Layout controls
        LayoutControls();

        // Wire up events
        WireUpEvents();

        // Set up update timer (10 Hz for smooth radar)
        _updateTimer = new Timer { Interval = 100 };
        _updateTimer.Tick += (s, e) =>
        {
            if (_dataProvider is MockRadarDataProvider mock)
            {
                mock.SimulateUpdate();
            }
            RefreshRadar();
        };
        _updateTimer.Start();
    }

    /// <summary>
    /// Creates the radar canvas.
    /// </summary>
    private PictureBox CreateRadarCanvas()
    {
        return new PictureBox
        {
            Size = new Size(200, 200),
            BackColor = Color.FromArgb(15, 15, 18),
            BorderStyle = BorderStyle.FixedSingle,
            SizeMode = PictureBoxSizeMode.CenterImage
        };
    }

    /// <summary>
    /// Creates the zoom label.
    /// </summary>
    private Label CreateZoomLabel()
    {
        return new Label
        {
            Text = $"Zoom: {_zoomLevel:F2}x",
            ForeColor = Color.FromArgb(200, 200, 200),
            Font = new Font("Segoe UI", 8F),
            Width = 80
        };
    }

    /// <summary>
    /// Creates a zoom button.
    /// </summary>
    private Button CreateZoomButton(string text)
    {
        return new Button
        {
            Text = text,
            Size = new Size(30, 25),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(60, 60, 65),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Margin = new Padding(2)
        };
    }

    /// <summary>
    /// Creates the range slider.
    /// </summary>
    private TrackBar CreateRangeSlider()
    {
        return new TrackBar
        {
            Minimum = 10,
            Maximum = 200,
            Value = 50,
            TickFrequency = 10,
            Width = 200
        };
    }

    /// <summary>
    /// Layouts the controls on the panel.
    /// </summary>
    private void LayoutControls()
    {
        // Center the radar canvas
        _radarCanvas.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        _radarCanvas.Location = new Point((Width - _radarCanvas.Width) / 2, 5);
        Controls.Add(_radarCanvas);

        // Zoom controls
        var zoomPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 30
        };

        _zoomOutButton.Location = new Point(Width - 70, 3);
        _zoomInButton.Location = new Point(Width - 125, 3);
        _zoomLabel.Location = new Point(Width - 205, 6);

        zoomPanel.Controls.Add(_zoomLabel);
        zoomPanel.Controls.Add(_zoomOutButton);
        zoomPanel.Controls.Add(_zoomInButton);
        Controls.Add(zoomPanel);

        // Range slider
        var rangePanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 35
        };

        var rangeLabel = new Label
        {
            Text = "Range:",
            ForeColor = Color.FromArgb(180, 180, 180),
            Font = new Font("Segoe UI", 8F),
            Location = new Point(5, 8),
            Width = 45
        };

        _rangeSlider.Location = new Point(50, 5);

        rangePanel.Controls.Add(rangeLabel);
        rangePanel.Controls.Add(_rangeSlider);
        Controls.Add(rangePanel);
    }

    /// <summary>
    /// Wires up event handlers.
    /// </summary>
    private void WireUpEvents()
    {
        // Zoom buttons
        _zoomInButton.Click += (s, e) => SetZoom(_zoomLevel + ZoomStep);
        _zoomOutButton.Click += (s, e) => SetZoom(_zoomLevel - ZoomStep);

        // Range slider
        _rangeSlider.Scroll += (s, e) =>
        {
            if (_dataProvider is MockRadarDataProvider mock)
            {
                mock.Range = _rangeSlider.Value;
            }
        };

        // Radar data updated
        _dataProvider.RadarDataUpdated += (s, e) =>
        {
            if (InvokeRequired)
            {
                try
                {
                    Invoke(new Action(RefreshRadar));
                }
                catch { } // Form might be closing
                return;
            }
            RefreshRadar();
        };

        // Paint event for radar
        _radarCanvas.Paint += (s, e) => PaintRadar(e.Graphics);
    }

    /// <summary>
    /// Sets the zoom level.
    /// </summary>
    private void SetZoom(float newZoom)
    {
        _zoomLevel = Math.Max(MinZoom, Math.Min(MaxZoom, newZoom));
        _zoomLabel.Text = $"Zoom: {_zoomLevel:F2}x";
        RefreshRadar();
    }

    /// <summary>
    /// Refreshes the radar display.
    /// </summary>
    private void RefreshRadar()
    {
        if (_radarCanvas.IsHandleCreated && !_radarCanvas.IsDisposed)
        {
            _radarCanvas.Invalidate();
        }
    }

    /// <summary>
    /// Paints the radar display.
    /// </summary>
    private void PaintRadar(Graphics g)
    {
        if (g == null) return;

        var centerX = _radarCanvas.Width / 2f;
        var centerY = _radarCanvas.Height / 2f;
        var playerInfo = _dataProvider.GetPlayerInfo();
        var entities = _dataProvider.GetNearbyEntities(playerInfo.Range);
        var waypoints = _dataProvider.GetWaypoints();
        var connections = _dataProvider.GetWaypointConnections();

        // Clear background
        g.Clear(Color.FromArgb(15, 15, 18));

        // Enable anti-aliasing
        g.SmoothingMode = SmoothingMode.AntiAlias;

        // Draw radar range circles
        DrawRangeCircles(g, centerX, centerY);

        // Draw waypoint path
        DrawWaypointPath(g, centerX, centerY, waypoints, connections);

        // Draw entities
        DrawEntities(g, centerX, centerY, entities, playerInfo);

        // Draw player (center)
        DrawPlayer(g, centerX, centerY, playerInfo.FacingDirection);
    }

    /// <summary>
    /// Draws range circles on the radar.
    /// </summary>
    private void DrawRangeCircles(Graphics g, float cx, float cy)
    {
        float scale = GetScale();
        float range = _dataProvider.GetPlayerInfo().Range;

        // Draw concentric circles at 25%, 50%, 75%, 100% of range
        using var pen = new Pen(Color.FromArgb(40, 40, 45), 1);
        for (int i = 1; i <= 4; i++)
        {
            float radius = (range * i / 4f) * scale;
            g.DrawEllipse(pen, cx - radius, cy - radius, radius * 2, radius * 2);
        }

        // Draw crosshairs
        g.DrawLine(pen, cx - 100, cy, cx + 100, cy);
        g.DrawLine(pen, cx, cy - 100, cx, cy + 100);
    }

    /// <summary>
    /// Draws waypoint path connections and waypoints on the radar.
    /// </summary>
    private void DrawWaypointPath(Graphics g, float cx, float cy, IList<RadarWaypoint> waypoints, IList<(int from, int to)> connections)
    {
        if (waypoints.Count == 0) return;

        float scale = GetScale();

        // Create a lookup dictionary for waypoint positions
        var waypointPositions = new System.Collections.Generic.Dictionary<int, PointF>();
        foreach (var wp in waypoints)
        {
            float radarX = cx + (wp.RelativeX * scale);
            float radarY = cy - (wp.RelativeZ * scale);
            waypointPositions[wp.Index] = new PointF(radarX, radarY);
        }

        // Draw connections first (so waypoints appear on top)
        using var pathPen = new Pen(Color.FromArgb(60, 100, 120), 1);
        pathPen.DashPattern = new float[] { 4, 2 };  // Dashed line for path

        foreach (var (from, to) in connections)
        {
            if (waypointPositions.TryGetValue(from, out var fromPoint) &&
                waypointPositions.TryGetValue(to, out var toPoint))
            {
                g.DrawLine(pathPen, fromPoint, toPoint);
            }
        }

        // Draw waypoints
        foreach (var wp in waypoints)
        {
            if (waypointPositions.TryGetValue(wp.Index, out var point))
            {
                DrawWaypoint(g, point.X, point.Y, wp);
            }
        }
    }

    /// <summary>
    /// Draws a single waypoint on the radar.
    /// </summary>
    private void DrawWaypoint(Graphics g, float x, float y, RadarWaypoint waypoint)
    {
        // Skip if outside radar bounds
        if (x < -10 || x > _radarCanvas.Width + 10 ||
            y < -10 || y > _radarCanvas.Height + 10)
            return;

        var color = Color.FromArgb(waypoint.IsTarget ? waypoint.TargetColor : waypoint.Color);
        float size = waypoint.IsTarget ? 6f : 4f;

        using var brush = new SolidBrush(color);
        using var pen = new Pen(color, waypoint.IsTarget ? 2 : 1);

        if (waypoint.IsTarget)
        {
            // Draw target waypoint with a ring around it
            g.FillEllipse(brush, x - size / 2, y - size / 2, size, size);
            g.DrawEllipse(pen, x - size, y - size, size * 2, size * 2);

            // Draw a small cross on target waypoint
            using var crossPen = new Pen(Color.FromArgb(200, 200, 200), 1);
            g.DrawLine(crossPen, x - 2, y, x + 2, y);
            g.DrawLine(crossPen, x, y - 2, x, y + 2);
        }
        else if (waypoint.Type == RadarWaypointType.Node)
        {
            // Draw diamond for resource nodes
            var points = new PointF[]
            {
                new PointF(x, y - size),
                new PointF(x + size, y),
                new PointF(x, y + size),
                new PointF(x - size, y)
            };
            g.FillPolygon(brush, points);
        }
        else if (waypoint.Type == RadarWaypointType.Vendor)
        {
            // Draw square for vendors
            g.FillRectangle(brush, x - size / 2, y - size / 2, size, size);
        }
        else
        {
            // Draw circle for other waypoints
            g.FillEllipse(brush, x - size / 2, y - size / 2, size, size);
        }
    }

    /// <summary>
    /// Draws entities on the radar.
    /// </summary>
    private void DrawEntities(Graphics g, float cx, float cy, RadarEntity[] entities, RadarPlayerInfo playerInfo)
    {
        float scale = GetScale();

        foreach (var entity in entities)
        {
            // Convert relative position to radar coordinates
            // In game: X is left/right, Z is forward/back
            // On radar: X is left/right, Y is forward/back (inverted Z)
            float radarX = cx + (entity.RelativeX * scale);
            float radarY = cy - (entity.RelativeZ * scale); // Invert Z for radar Y

            // Skip if outside radar bounds
            if (radarX < 0 || radarX > _radarCanvas.Width ||
                radarY < 0 || radarY > _radarCanvas.Height)
                continue;

            // Get entity color
            var color = Color.FromArgb(entity.Color);

            // Draw entity based on type
            DrawEntityDot(g, radarX, radarY, entity.Type, color);
        }
    }

    /// <summary>
    /// Draws a single entity on the radar.
    /// </summary>
    private void DrawEntityDot(Graphics g, float x, float y, RadarEntityType type, Color color)
    {
        float size = type switch
        {
            RadarEntityType.ResourceNode => 6f,  // Larger for nodes
            RadarEntityType.Corpse => 3f,        // Smaller for corpses
            RadarEntityType.Player => 5f,        // Medium for players
            _ => 4f                               // Default for mobs/NPCs
        };

        using var brush = new SolidBrush(color);
        using var pen = new Pen(color, 1);

        if (type == RadarEntityType.ResourceNode)
        {
            // Draw diamond for resource nodes
            var points = new PointF[]
            {
                new PointF(x, y - size),
                new PointF(x + size, y),
                new PointF(x, y + size),
                new PointF(x - size, y)
            };
            g.FillPolygon(brush, points);
        }
        else
        {
            // Draw circle for other entities
            g.FillEllipse(brush, x - size / 2, y - size / 2, size, size);
        }
    }

    /// <summary>
    /// Draws the player indicator.
    /// </summary>
    private void DrawPlayer(Graphics g, float cx, float cy, float facingDirection)
    {
        // Draw player circle
        using var playerBrush = new SolidBrush(Color.FromArgb(100, 200, 100));
        using var playerPen = new Pen(Color.FromArgb(150, 255, 150), 2);
        g.FillEllipse(playerBrush, cx - 5, cy - 5, 10, 10);
        g.DrawEllipse(playerPen, cx - 5, cy - 5, 10, 10);

        // Draw facing direction arrow
        // Convert degrees to radians and adjust for radar orientation
        // 0 degrees = North (up on radar)
        float angleRad = (facingDirection - 90) * (float)Math.PI / 180f;

        float arrowLength = 12;
        float arrowX = cx + (float)Math.Cos(angleRad) * arrowLength;
        float arrowY = cy + (float)Math.Sin(angleRad) * arrowLength;

        using var arrowPen = new Pen(Color.FromArgb(150, 255, 150), 2);
        g.DrawLine(arrowPen, cx, cy, arrowX, arrowY);
    }

    /// <summary>
    /// Gets the scale factor for converting game units to radar pixels.
    /// </summary>
    private float GetScale()
    {
        float range = _dataProvider.GetPlayerInfo().Range;
        float radarRadius = _radarCanvas.Width / 2f - 5; // -5 for padding
        return (radarRadius / range) * _zoomLevel;
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
