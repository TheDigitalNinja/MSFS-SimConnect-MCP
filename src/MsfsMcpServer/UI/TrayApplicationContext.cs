using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;
using MsfsMcpServer.Models;
using MsfsMcpServer.Services;
using Timer = System.Windows.Forms.Timer;

namespace MsfsMcpServer.UI;

/// <summary>
/// Windows Forms application context that hosts the system tray icon and menu.
/// </summary>
internal sealed class TrayApplicationContext : ApplicationContext
{
    private const int TooltipMaxLength = 63;
    private static readonly TimeSpan UpdateInterval = TimeSpan.FromSeconds(2);

    private readonly ISimConnectService _simConnect;
    private readonly ILogger<TrayApplicationContext> _logger;
    private readonly Uri _dashboardUri;
    private readonly Func<Task> _shutdownAsync;

    private readonly NotifyIcon _notifyIcon;
    private readonly ToolStripMenuItem _statusMenuItem;
    private readonly ToolStripMenuItem _serverMenuItem;
    private readonly Timer _updateTimer;
    private readonly Icon _connectedIcon;
    private readonly Icon _disconnectedIcon;

    private bool _disposed;

    public TrayApplicationContext(
        ISimConnectService simConnect,
        ILogger<TrayApplicationContext> logger,
        Uri dashboardUri,
        Func<Task> shutdownAsync)
    {
        _simConnect = simConnect;
        _logger = logger;
        _dashboardUri = dashboardUri;
        _shutdownAsync = shutdownAsync;

        _connectedIcon = CreatePlaneIcon(Color.DeepSkyBlue, Color.Navy);
        _disconnectedIcon = CreatePlaneIcon(Color.Gray, Color.DimGray);

        _statusMenuItem = new ToolStripMenuItem("Connecting to MSFS...") { Enabled = false };
        _serverMenuItem = new ToolStripMenuItem($"MCP server: {_dashboardUri.Authority}") { Enabled = false };

        var openDashboardItem = new ToolStripMenuItem("Open Dashboard", image: null, (_, _) => OpenDashboard());
        var exitItem = new ToolStripMenuItem("Exit", image: null, async (_, _) => await ExitAsync().ConfigureAwait(false));

        var menu = new ContextMenuStrip
        {
            ShowCheckMargin = false,
            ShowImageMargin = false
        };

        menu.Items.AddRange(
        [
            _statusMenuItem,
            _serverMenuItem,
            new ToolStripSeparator(),
            openDashboardItem,
            new ToolStripSeparator(),
            exitItem
        ]);

        _notifyIcon = new NotifyIcon
        {
            Visible = true,
            Icon = _disconnectedIcon,
            Text = "MSFS MCP Server - starting...",
            ContextMenuStrip = menu
        };

        _notifyIcon.DoubleClick += (_, _) => OpenDashboard();

        _updateTimer = new Timer
        {
            Interval = (int)UpdateInterval.TotalMilliseconds
        };
        _updateTimer.Tick += async (_, _) => await UpdateStatusAsync().ConfigureAwait(false);
        _updateTimer.Start();

        _ = UpdateStatusAsync();
    }

    private void OpenDashboard()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = _dashboardUri.ToString(),
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open dashboard at {DashboardUri}", _dashboardUri);
        }
    }

    private async Task UpdateStatusAsync()
    {
        if (_disposed)
        {
            return;
        }

        var connected = _simConnect.IsConnected;
        _statusMenuItem.Text = connected ? "Connected to MSFS" : "Disconnected (start MSFS)";
        _notifyIcon.Icon = connected ? _connectedIcon : _disconnectedIcon;

        var tooltip = connected
            ? "MSFS MCP Server - Connected"
            : "MSFS MCP Server - Not connected";

        if (connected)
        {
            try
            {
                var data = await _simConnect.RequestDataAsync<FlightPositionData>().ConfigureAwait(false);
                if (data is { } flight)
                {
                    tooltip = $"MSFS MCP Server - Connected - {FormatAltitude(flight.AltitudeMslFeet)}";
                }
                else
                {
                    tooltip = "MSFS MCP Server - Connected - awaiting data";
                }
            }
            catch (TimeoutException)
            {
                tooltip = "MSFS MCP Server - Connected - request timed out";
                _logger.LogWarning("Timed out while refreshing tray tooltip with flight data.");
            }
            catch (OperationCanceledException)
            {
                tooltip = "MSFS MCP Server - Connected - request canceled";
            }
            catch (Exception ex)
            {
                tooltip = "MSFS MCP Server - Connected - data unavailable";
                _logger.LogError(ex, "Failed to refresh flight data for tray tooltip.");
            }
        }

        _notifyIcon.Text = TrimTooltip(tooltip);
    }

    private static string FormatAltitude(double altitudeFeet)
    {
        var rounded = Math.Max(0, Math.Round(altitudeFeet));
        return $"{rounded:N0} ft";
    }

    private static string TrimTooltip(string text) =>
        text.Length <= TooltipMaxLength ? text : text[..TooltipMaxLength];

    private async Task ExitAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _updateTimer.Stop();
        _notifyIcon.Visible = false;

        try
        {
            await _shutdownAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while shutting down application.");
        }
        finally
        {
            _updateTimer.Dispose();
            _notifyIcon.Dispose();
            _connectedIcon.Dispose();
            _disconnectedIcon.Dispose();
            ExitThread();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (!disposing || _disposed)
        {
            base.Dispose(disposing);
            return;
        }

        _disposed = true;
        _updateTimer.Dispose();
        _notifyIcon.Dispose();
        _connectedIcon.Dispose();
        _disconnectedIcon.Dispose();

        base.Dispose(disposing);
    }

    private static Icon CreatePlaneIcon(Color bodyColor, Color accentColor)
    {
        using var bitmap = new Bitmap(32, 32);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            graphics.Clear(Color.Transparent);

            using var fuselageBrush = new SolidBrush(bodyColor);
            using var accentBrush = new SolidBrush(accentColor);
            using var outlinePen = new Pen(Color.FromArgb(80, Color.Black), 1);

            // Fuselage
            graphics.FillPolygon(fuselageBrush, new[]
            {
                new Point(16, 2),
                new Point(20, 18),
                new Point(16, 30),
                new Point(12, 18)
            });
            graphics.DrawPolygon(outlinePen, new[]
            {
                new Point(16, 2),
                new Point(20, 18),
                new Point(16, 30),
                new Point(12, 18)
            });

            // Wings
            graphics.FillPolygon(accentBrush, new[]
            {
                new Point(6, 14),
                new Point(26, 14),
                new Point(22, 18),
                new Point(10, 18)
            });
            graphics.DrawPolygon(outlinePen, new[]
            {
                new Point(6, 14),
                new Point(26, 14),
                new Point(22, 18),
                new Point(10, 18)
            });

            // Tail
            graphics.FillPolygon(accentBrush, new[]
            {
                new Point(14, 22),
                new Point(18, 22),
                new Point(17, 27),
                new Point(15, 27)
            });
            graphics.DrawPolygon(outlinePen, new[]
            {
                new Point(14, 22),
                new Point(18, 22),
                new Point(17, 27),
                new Point(15, 27)
            });
        }

        return CloneIconFromBitmap(bitmap);
    }

    private static Icon CloneIconFromBitmap(Bitmap bitmap)
    {
        var hIcon = bitmap.GetHicon();
        try
        {
            using var icon = Icon.FromHandle(hIcon);
            return (Icon)icon.Clone();
        }
        finally
        {
            DestroyIcon(hIcon);
        }
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr handle);
}
