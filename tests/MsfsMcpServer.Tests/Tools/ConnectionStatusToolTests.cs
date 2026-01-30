using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MsfsMcpServer.Models;
using MsfsMcpServer.Services;
using MsfsMcpServer.Tools;

namespace MsfsMcpServer.Tests.Tools;

public class ConnectionStatusToolTests
{
    [Fact]
    public async Task GetConnectionStatus_WhenConnected_ReturnsConnectedResponse()
    {
        var simConnect = new Mock<ISimConnectService>();
        simConnect.Setup(s => s.IsConnected).Returns(true);
        var callLogger = new Mock<IToolCallLogger>();
        var tool = new ConnectionStatusTool(simConnect.Object, NullLogger<ConnectionStatusTool>.Instance, callLogger.Object);

        var result = await tool.GetConnectionStatus(CancellationToken.None);

        result.Should().BeEquivalentTo(new ConnectionStatusResponse
        {
            Connected = true,
            Simulator = "Microsoft Flight Simulator 2024",
            Error = null
        });
        callLogger.Verify(l => l.LogSuccess("get_connection_status", It.IsAny<TimeSpan>()), Times.Once);
    }

    [Fact]
    public async Task GetConnectionStatus_WhenReconnectSucceeds_ReturnsConnectedResponse()
    {
        var simConnect = new Mock<ISimConnectService>();
        simConnect.Setup(s => s.IsConnected).Returns(false);
        simConnect.Setup(s => s.ConnectAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var callLogger = new Mock<IToolCallLogger>();
        var tool = new ConnectionStatusTool(simConnect.Object, NullLogger<ConnectionStatusTool>.Instance, callLogger.Object);

        var result = await tool.GetConnectionStatus(CancellationToken.None);

        result.Should().BeEquivalentTo(new ConnectionStatusResponse
        {
            Connected = true,
            Simulator = "Microsoft Flight Simulator 2024",
            Error = null
        });
        callLogger.Verify(l => l.LogSuccess("get_connection_status", It.IsAny<TimeSpan>()), Times.Once);
        simConnect.Verify(s => s.ConnectAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetConnectionStatus_WhenDisconnected_ReturnsFriendlyError()
    {
        var simConnect = new Mock<ISimConnectService>();
        simConnect.Setup(s => s.IsConnected).Returns(false);
        var callLogger = new Mock<IToolCallLogger>();
        var tool = new ConnectionStatusTool(simConnect.Object, NullLogger<ConnectionStatusTool>.Instance, callLogger.Object);

        var result = await tool.GetConnectionStatus(CancellationToken.None);

        result.Connected.Should().BeFalse();
        result.Error.Should().Be("SimConnect not available. Is MSFS running?");
        callLogger.Verify(l => l.LogFailure("get_connection_status", It.IsAny<TimeSpan>(), "SimConnect not available. Is MSFS running?"), Times.Once);
    }

    [Fact]
    public async Task GetConnectionStatus_WhenCanceled_ReturnsCanceledMessage()
    {
        var simConnect = new Mock<ISimConnectService>();
        var callLogger = new Mock<IToolCallLogger>();
        var tool = new ConnectionStatusTool(simConnect.Object, NullLogger<ConnectionStatusTool>.Instance, callLogger.Object);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await tool.GetConnectionStatus(cts.Token);

        result.Connected.Should().BeFalse();
        result.Error.Should().Be("Request canceled.");
        callLogger.Verify(l => l.LogFailure("get_connection_status", It.IsAny<TimeSpan>(), "Request canceled."), Times.Once);
    }

    [Fact]
    public async Task GetConnectionStatus_WhenUnexpectedError_ReturnsGenericError()
    {
        var simConnect = new Mock<ISimConnectService>();
        simConnect.Setup(s => s.IsConnected).Throws(new InvalidOperationException("boom"));
        var callLogger = new Mock<IToolCallLogger>();
        var tool = new ConnectionStatusTool(simConnect.Object, NullLogger<ConnectionStatusTool>.Instance, callLogger.Object);

        var result = await tool.GetConnectionStatus(CancellationToken.None);

        result.Connected.Should().BeFalse();
        result.Error.Should().Be("An unexpected error occurred.");
        callLogger.Verify(l => l.LogFailure("get_connection_status", It.IsAny<TimeSpan>(), "An unexpected error occurred."), Times.Once);
    }
}
