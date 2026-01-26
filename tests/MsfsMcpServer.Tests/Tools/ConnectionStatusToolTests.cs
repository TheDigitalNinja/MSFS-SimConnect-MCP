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
        var tool = new ConnectionStatusTool(simConnect.Object, NullLogger<ConnectionStatusTool>.Instance);

        var result = await tool.GetConnectionStatus(CancellationToken.None);

        result.Should().BeEquivalentTo(new ConnectionStatusResponse
        {
            Connected = true,
            Simulator = "Microsoft Flight Simulator 2024",
            Error = null
        });
    }

    [Fact]
    public async Task GetConnectionStatus_WhenDisconnected_ReturnsFriendlyError()
    {
        var simConnect = new Mock<ISimConnectService>();
        simConnect.Setup(s => s.IsConnected).Returns(false);
        var tool = new ConnectionStatusTool(simConnect.Object, NullLogger<ConnectionStatusTool>.Instance);

        var result = await tool.GetConnectionStatus(CancellationToken.None);

        result.Connected.Should().BeFalse();
        result.Error.Should().Be("SimConnect not available. Is MSFS running?");
    }

    [Fact]
    public async Task GetConnectionStatus_WhenCanceled_ReturnsCanceledMessage()
    {
        var simConnect = new Mock<ISimConnectService>();
        var tool = new ConnectionStatusTool(simConnect.Object, NullLogger<ConnectionStatusTool>.Instance);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await tool.GetConnectionStatus(cts.Token);

        result.Connected.Should().BeFalse();
        result.Error.Should().Be("Request canceled.");
    }

    [Fact]
    public async Task GetConnectionStatus_WhenUnexpectedError_ReturnsGenericError()
    {
        var simConnect = new Mock<ISimConnectService>();
        simConnect.Setup(s => s.IsConnected).Throws(new InvalidOperationException("boom"));
        var tool = new ConnectionStatusTool(simConnect.Object, NullLogger<ConnectionStatusTool>.Instance);

        var result = await tool.GetConnectionStatus(CancellationToken.None);

        result.Connected.Should().BeFalse();
        result.Error.Should().Be("An unexpected error occurred.");
    }
}
