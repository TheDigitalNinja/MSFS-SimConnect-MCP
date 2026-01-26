using System.ComponentModel;
using Microsoft.FlightSimulator.SimConnect;
using Microsoft.Extensions.Logging;
using System.Windows.Forms;

namespace MsfsMcpServer.UI;

/// <summary>
/// Hidden native window that receives SimConnect messages and forwards them to the active session.
/// </summary>
internal sealed class SimConnectMessageWindow : NativeWindow, IDisposable
{
    private const int WmUserSimConnect = 0x0402;

    private readonly ILogger<SimConnectMessageWindow> _logger;
    private readonly object _sync = new();
    private bool _disposed;
    private SimConnect? _simConnect;

    public SimConnectMessageWindow(ILogger<SimConnectMessageWindow> logger)
    {
        _logger = logger;

        var cp = new CreateParams
        {
            Caption = "MsfsMcpServer_SimConnect",
            Style = 0,
            ExStyle = 0,
            X = 0,
            Y = 0,
            Height = 0,
            Width = 0
        };

        CreateHandle(cp);
    }

    /// <summary>
    /// Gets the native window handle used for SimConnect message delivery.
    /// </summary>
    public IntPtr HandlePointer => Handle;

    /// <summary>
    /// Associates an active SimConnect session with the window to receive callbacks.
    /// </summary>
    public void Attach(SimConnect simConnect)
    {
        if (simConnect is null)
        {
            throw new ArgumentNullException(nameof(simConnect));
        }

        lock (_sync)
        {
            _simConnect = simConnect;
        }
    }

    /// <summary>
    /// Clears the current SimConnect association.
    /// </summary>
    public void Clear()
    {
        lock (_sync)
        {
            _simConnect = null;
        }
    }

    /// <inheritdoc />
    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WmUserSimConnect)
        {
            SimConnect? simConnect = null;
            lock (_sync)
            {
                simConnect = _simConnect;
            }

            try
            {
                simConnect?.ReceiveMessage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while receiving SimConnect message.");
            }
        }

        base.WndProc(ref m);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Clear();
        DestroyHandle();
        GC.SuppressFinalize(this);
    }
}
