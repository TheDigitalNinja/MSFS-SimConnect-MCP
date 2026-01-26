using System.Collections.Concurrent;
using Microsoft.FlightSimulator.SimConnect;
using Microsoft.Extensions.Logging;
using MsfsMcpServer.Models;
using MsfsMcpServer.UI;

namespace MsfsMcpServer.Services;

/// <summary>
/// Real SimConnect-backed implementation that manages connection lifecycle and data requests.
/// </summary>
internal sealed class SimConnectService : ISimConnectService, IDisposable
{
    private const int WmUserSimConnect = 0x0402;
    private static readonly TimeSpan DefaultRequestTimeout = TimeSpan.FromSeconds(2);

    private readonly ILogger<SimConnectService> _logger;
    private readonly SimConnectMessageWindow _messageWindow;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private readonly ConcurrentDictionary<uint, PendingRequest> _pendingRequests = new();
    private readonly SemaphoreSlim _requestLock = new(1, 1);

    private SimConnect? _simConnect;
    private int _requestId;
    private bool _disposed;

    public SimConnectService(ILogger<SimConnectService> logger, SimConnectMessageWindow messageWindow)
    {
        _logger = logger;
        _messageWindow = messageWindow;
    }

    public bool IsConnected => _simConnect is not null;

    public async Task<bool> ConnectAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        await _connectionLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (_simConnect is not null)
            {
                return true;
            }

            try
            {
                _logger.LogInformation("Opening SimConnect session.");
                _simConnect = new SimConnect("MSFS MCP Server", _messageWindow.HandlePointer, WmUserSimConnect, null, 0);
                _messageWindow.Attach(_simConnect);

                RegisterEvents(_simConnect);
                RegisterDataDefinitions(_simConnect);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to open SimConnect session.");
                CleanupSimConnect();
                return false;
            }
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    public void Disconnect()
    {
        CleanupSimConnect();
    }

    public async Task<T?> RequestDataAsync<T>(CancellationToken ct = default) where T : struct
    {
        await _requestLock.WaitAsync(ct).ConfigureAwait(false);
        var simConnect = _simConnect ?? throw new InvalidOperationException("Not connected to SimConnect.");

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(DefaultRequestTimeout);

        var requestId = (uint)Interlocked.Increment(ref _requestId);
        var tcs = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);

        var registration = timeoutCts.Token.Register(() =>
        {
            if (_pendingRequests.TryRemove(requestId, out var pending))
            {
                pending.CancellationRegistration.Dispose();
                if (ct.IsCancellationRequested)
                {
                    pending.Tcs.TrySetCanceled(ct);
                }
                else
                {
                    pending.Tcs.TrySetException(new TimeoutException("SimConnect request timed out."));
                }
            }
        });

        var pendingRequest = new PendingRequest(typeof(T), tcs, registration);
        _pendingRequests[requestId] = pendingRequest;

        try
        {
            if (typeof(T) == typeof(FlightPositionData))
            {
                simConnect.RequestDataOnSimObject(
                    (RequestId)requestId,
                    DataDefinition.FlightPosition,
                    SimConnect.SIMCONNECT_OBJECT_ID_USER,
                    SIMCONNECT_PERIOD.ONCE,
                    SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT,
                    0,
                    0,
                    0);
            }
            else if (typeof(T) == typeof(FlightInstrumentsData))
            {
                simConnect.RequestDataOnSimObject(
                    (RequestId)requestId,
                    DataDefinition.FlightInstruments,
                    SimConnect.SIMCONNECT_OBJECT_ID_USER,
                    SIMCONNECT_PERIOD.ONCE,
                    SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT,
                    0,
                    0,
                    0);
            }
            else if (typeof(T) == typeof(EngineStatusData))
            {
                simConnect.RequestDataOnSimObject(
                    (RequestId)requestId,
                    DataDefinition.EngineStatus,
                    SimConnect.SIMCONNECT_OBJECT_ID_USER,
                    SIMCONNECT_PERIOD.ONCE,
                    SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT,
                    0,
                    0,
                    0);
            }
            else if (typeof(T) == typeof(AutopilotStatusData))
            {
                simConnect.RequestDataOnSimObject(
                    (RequestId)requestId,
                    DataDefinition.AutopilotStatus,
                    SimConnect.SIMCONNECT_OBJECT_ID_USER,
                    SIMCONNECT_PERIOD.ONCE,
                    SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT,
                    0,
                    0,
                    0);
            }
            else if (typeof(T) == typeof(AircraftInfoData))
            {
                simConnect.RequestDataOnSimObject(
                    (RequestId)requestId,
                    DataDefinition.AircraftInfo,
                    SimConnect.SIMCONNECT_OBJECT_ID_USER,
                    SIMCONNECT_PERIOD.ONCE,
                    SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT,
                    0,
                    0,
                    0);
            }
            else
            {
                throw new NotSupportedException($"Unsupported data request type: {typeof(T).Name}");
            }
        }
        catch
        {
            _pendingRequests.TryRemove(requestId, out var pending);
            pending?.CancellationRegistration.Dispose();
            throw;
        }

        try
        {
            var result = await tcs.Task.ConfigureAwait(false);
            return result is T typed ? typed : null;
        }
        finally
        {
            _requestLock.Release();
            _pendingRequests.TryRemove(requestId, out var _);
            registration.Dispose();
        }
    }

    private void RegisterEvents(SimConnect simConnect)
    {
        simConnect.OnRecvOpen += OnRecvOpen;
        simConnect.OnRecvQuit += OnRecvQuit;
        simConnect.OnRecvException += OnRecvException;
        simConnect.OnRecvSimobjectData += OnRecvSimobjectData;
        simConnect.OnRecvSystemState += OnRecvSystemState;
    }

    private void RegisterDataDefinitions(SimConnect simConnect)
    {
        simConnect.AddToDataDefinition(DataDefinition.FlightPosition, "PLANE LATITUDE", "Degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
        simConnect.AddToDataDefinition(DataDefinition.FlightPosition, "PLANE LONGITUDE", "Degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
        simConnect.AddToDataDefinition(DataDefinition.FlightPosition, "PLANE ALTITUDE", "Feet", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
        simConnect.AddToDataDefinition(DataDefinition.FlightPosition, "PLANE HEADING DEGREES TRUE", "Degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
        simConnect.AddToDataDefinition(DataDefinition.FlightPosition, "GROUND VELOCITY", "Knots", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
        simConnect.AddToDataDefinition(DataDefinition.FlightPosition, "VERTICAL SPEED", "Feet per minute", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
        simConnect.AddToDataDefinition(DataDefinition.FlightPosition, "PLANE PITCH DEGREES", "Degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
        simConnect.AddToDataDefinition(DataDefinition.FlightPosition, "PLANE BANK DEGREES", "Degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
        simConnect.RegisterDataDefineStruct<FlightPositionData>(DataDefinition.FlightPosition);

        simConnect.AddToDataDefinition(DataDefinition.FlightInstruments, "INDICATED ALTITUDE", "Feet", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
        simConnect.AddToDataDefinition(DataDefinition.FlightInstruments, "AIRSPEED INDICATED", "Knots", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
        simConnect.AddToDataDefinition(DataDefinition.FlightInstruments, "AIRSPEED TRUE", "Knots", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
        simConnect.AddToDataDefinition(DataDefinition.FlightInstruments, "AIRSPEED MACH", "Mach", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
        simConnect.AddToDataDefinition(DataDefinition.FlightInstruments, "HEADING INDICATOR", "Degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
        simConnect.AddToDataDefinition(DataDefinition.FlightInstruments, "KOHLSMAN SETTING HG", "inHg", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
        simConnect.AddToDataDefinition(DataDefinition.FlightInstruments, "ATTITUDE INDICATOR PITCH DEGREES", "Degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
        simConnect.AddToDataDefinition(DataDefinition.FlightInstruments, "ATTITUDE INDICATOR BANK DEGREES", "Degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
        simConnect.RegisterDataDefineStruct<FlightInstrumentsData>(DataDefinition.FlightInstruments);

        simConnect.AddToDataDefinition(DataDefinition.EngineStatus, "NUMBER OF ENGINES", "Number", SIMCONNECT_DATATYPE.INT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
        simConnect.AddToDataDefinition(DataDefinition.EngineStatus, "GENERAL ENG RPM:1", "Rpm", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
        simConnect.AddToDataDefinition(DataDefinition.EngineStatus, "GENERAL ENG THROTTLE LEVER POSITION:1", "Percent", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
        simConnect.AddToDataDefinition(DataDefinition.EngineStatus, "ENG FUEL FLOW GPH:1", "Gallons per hour", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
        simConnect.AddToDataDefinition(DataDefinition.EngineStatus, "FUEL TOTAL QUANTITY", "Gallons", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
        simConnect.AddToDataDefinition(DataDefinition.EngineStatus, "ENG EXHAUST GAS TEMPERATURE:1", "Celsius", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
        simConnect.AddToDataDefinition(DataDefinition.EngineStatus, "ENG OIL PRESSURE:1", "Psi", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
        simConnect.AddToDataDefinition(DataDefinition.EngineStatus, "ENG OIL TEMPERATURE:1", "Celsius", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
        simConnect.RegisterDataDefineStruct<EngineStatusData>(DataDefinition.EngineStatus);

        simConnect.AddToDataDefinition(DataDefinition.AutopilotStatus, "AUTOPILOT MASTER", "Bool", SIMCONNECT_DATATYPE.INT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
        simConnect.AddToDataDefinition(DataDefinition.AutopilotStatus, "AUTOPILOT HEADING LOCK", "Bool", SIMCONNECT_DATATYPE.INT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
        simConnect.AddToDataDefinition(DataDefinition.AutopilotStatus, "AUTOPILOT HEADING LOCK DIR", "Degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
        simConnect.AddToDataDefinition(DataDefinition.AutopilotStatus, "AUTOPILOT ALTITUDE LOCK", "Bool", SIMCONNECT_DATATYPE.INT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
        simConnect.AddToDataDefinition(DataDefinition.AutopilotStatus, "AUTOPILOT ALTITUDE LOCK VAR", "Feet", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
        simConnect.AddToDataDefinition(DataDefinition.AutopilotStatus, "AUTOPILOT AIRSPEED HOLD", "Bool", SIMCONNECT_DATATYPE.INT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
        simConnect.AddToDataDefinition(DataDefinition.AutopilotStatus, "AUTOPILOT AIRSPEED HOLD VAR", "Knots", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
        simConnect.AddToDataDefinition(DataDefinition.AutopilotStatus, "AUTOPILOT VERTICAL HOLD", "Bool", SIMCONNECT_DATATYPE.INT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
        simConnect.AddToDataDefinition(DataDefinition.AutopilotStatus, "AUTOPILOT VERTICAL HOLD VAR", "Feet per minute", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
        simConnect.AddToDataDefinition(DataDefinition.AutopilotStatus, "AUTOPILOT NAV1 LOCK", "Bool", SIMCONNECT_DATATYPE.INT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
        simConnect.AddToDataDefinition(DataDefinition.AutopilotStatus, "AUTOPILOT APPROACH HOLD", "Bool", SIMCONNECT_DATATYPE.INT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
        simConnect.RegisterDataDefineStruct<AutopilotStatusData>(DataDefinition.AutopilotStatus);

        simConnect.AddToDataDefinition(DataDefinition.AircraftInfo, "TITLE", null, SIMCONNECT_DATATYPE.STRING256, 0.0f, SimConnect.SIMCONNECT_UNUSED);
        simConnect.AddToDataDefinition(DataDefinition.AircraftInfo, "ATC ID", null, SIMCONNECT_DATATYPE.STRING64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
        simConnect.AddToDataDefinition(DataDefinition.AircraftInfo, "ATC AIRLINE", null, SIMCONNECT_DATATYPE.STRING64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
        simConnect.AddToDataDefinition(DataDefinition.AircraftInfo, "TOTAL WEIGHT", "Pounds", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
        simConnect.AddToDataDefinition(DataDefinition.AircraftInfo, "MAX GROSS WEIGHT", "Pounds", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
        simConnect.AddToDataDefinition(DataDefinition.AircraftInfo, "EMPTY WEIGHT", "Pounds", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
        simConnect.RegisterDataDefineStruct<AircraftInfoData>(DataDefinition.AircraftInfo);
    }

    private void OnRecvOpen(SimConnect sender, SIMCONNECT_RECV_OPEN data)
    {
        _logger.LogInformation("SimConnect session opened: {ApplicationName}", data.szApplicationName);
    }

    private void OnRecvQuit(SimConnect sender, SIMCONNECT_RECV data)
    {
        _logger.LogWarning("SimConnect session quit detected.");
        FailAllPending(new InvalidOperationException("SimConnect session ended."));
        CleanupSimConnect();
    }

    private void OnRecvException(SimConnect sender, SIMCONNECT_RECV_EXCEPTION data)
    {
        _logger.LogError("SimConnect exception received: {ExceptionId} {SendId}", data.dwException, data.dwSendID);
        FailAllPending(new InvalidOperationException($"SimConnect exception: {data.dwException}"));
    }

    private void OnRecvSimobjectData(SimConnect sender, SIMCONNECT_RECV_SIMOBJECT_DATA data)
    {
        if (_pendingRequests.TryRemove(data.dwRequestID, out var pending))
        {
            pending.CancellationRegistration.Dispose();
            try
            {
                var raw = data.dwData[0];
                if (raw is null)
                {
                    pending.Tcs.TrySetResult(null);
                }
                else if (pending.ExpectedType.IsInstanceOfType(raw))
                {
                    pending.Tcs.TrySetResult(raw);
                }
                else
                {
                    pending.Tcs.TrySetException(new InvalidCastException($"Unexpected data type {raw.GetType().Name} for request {data.dwRequestID}."));
                }
            }
            catch (Exception ex)
            {
                pending.Tcs.TrySetException(ex);
            }
        }
    }

    private void OnRecvSystemState(SimConnect sender, SIMCONNECT_RECV_SYSTEM_STATE data)
    {
        if (_pendingRequests.TryRemove(data.dwRequestID, out var pending))
        {
            pending.CancellationRegistration.Dispose();
            try
            {
                if (pending.ExpectedType == typeof(int))
                {
                    pending.Tcs.TrySetResult((object?)data.dwInteger);
                }
                else
                {
                    pending.Tcs.TrySetException(new InvalidCastException($"Unexpected system state type for request {data.dwRequestID}."));
                }
            }
            catch (Exception ex)
            {
                pending.Tcs.TrySetException(ex);
            }
        }
    }

    private void FailAllPending(Exception ex)
    {
        foreach (var kvp in _pendingRequests.ToArray())
        {
            if (_pendingRequests.TryRemove(kvp.Key, out var pending))
            {
                pending.CancellationRegistration.Dispose();
                pending.Tcs.TrySetException(ex);
            }
        }
    }

    private void CleanupSimConnect()
    {
        if (_simConnect is null)
        {
            _messageWindow.Clear();
            FailAllPending(new InvalidOperationException("SimConnect disconnected."));
            return;
        }

        try
        {
            _simConnect.OnRecvOpen -= OnRecvOpen;
            _simConnect.OnRecvQuit -= OnRecvQuit;
            _simConnect.OnRecvException -= OnRecvException;
            _simConnect.OnRecvSimobjectData -= OnRecvSimobjectData;
            _simConnect.OnRecvSystemState -= OnRecvSystemState;
            _simConnect.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while disposing SimConnect.");
        }
        finally
        {
            _simConnect = null;
            _messageWindow.Clear();
            FailAllPending(new InvalidOperationException("SimConnect disconnected."));
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        CleanupSimConnect();
        _connectionLock.Dispose();
        GC.SuppressFinalize(this);
    }

    private enum DataDefinition
    {
        FlightPosition = 1,
        FlightInstruments = 2,
        EngineStatus = 3,
        AutopilotStatus = 4,
        AircraftInfo = 5
    }

    private enum RequestId : uint
    {
    }

    private sealed record PendingRequest(Type ExpectedType, TaskCompletionSource<object?> Tcs, CancellationTokenRegistration CancellationRegistration);

    private async Task<int?> RequestSystemStateAsync(string stateName, CancellationToken ct)
    {
        await _requestLock.WaitAsync(ct).ConfigureAwait(false);
        var simConnect = _simConnect ?? throw new InvalidOperationException("Not connected to SimConnect.");

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(DefaultRequestTimeout);

        var requestId = (uint)Interlocked.Increment(ref _requestId);
        var tcs = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);

        var registration = timeoutCts.Token.Register(() =>
        {
            if (_pendingRequests.TryRemove(requestId, out var pending))
            {
                pending.CancellationRegistration.Dispose();
                if (ct.IsCancellationRequested)
                {
                    pending.Tcs.TrySetCanceled(ct);
                }
                else
                {
                    pending.Tcs.TrySetException(new TimeoutException("SimConnect request timed out."));
                }
            }
        });

        _pendingRequests[requestId] = new PendingRequest(typeof(int), tcs, registration);

        try
        {
            simConnect.RequestSystemState((RequestId)requestId, stateName);
        }
        catch
        {
            _pendingRequests.TryRemove(requestId, out var pending);
            pending?.CancellationRegistration.Dispose();
            throw;
        }

        try
        {
            var result = await tcs.Task.ConfigureAwait(false);
            return result is int value ? value : null;
        }
        finally
        {
            _requestLock.Release();
            _pendingRequests.TryRemove(requestId, out var _);
            registration.Dispose();
        }
    }
}
