using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MsfsMcpServer.Models;
using MsfsMcpServer.Services;
using MsfsMcpServer.Tools;

namespace MsfsMcpServer.Tests.Tools;

public class EngineStatusToolTests
{
    [Fact]
    public async Task GetEngineStatus_WhenConnected_ReturnsRoundedData()
    {
        var simConnect = new Mock<ISimConnectService>();
        simConnect.Setup(s => s.IsConnected).Returns(true);
        simConnect
            .Setup(s => s.RequestDataAsync<EngineStatusData>(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EngineStatusData
            {
                EngineCount = 2,
                EngineOneRpm = 2350.6,
                EngineOneThrottlePercent = 57.35,
                FuelFlowGph = 12.45,
                FuelTotalGallons = 85.55,
                ExhaustGasTemperatureCelsius = 650.4,
                OilPressurePsi = 48.25,
                OilTemperatureCelsius = 98.6
            });

        var tool = new EngineStatusTool(simConnect.Object, NullLogger<EngineStatusTool>.Instance);

        var result = await tool.GetEngineStatus(CancellationToken.None);

        result.Error.Should().BeNull();
        result.EngineCount.Should().Be(2);
        result.EngineOneRpm.Should().Be(2351);
        result.EngineOneThrottlePercent.Should().Be(57.4);
        result.FuelFlowGallonsPerHour.Should().Be(12.5);
        result.FuelTotalGallons.Should().Be(85.6);
        result.ExhaustGasTemperatureCelsius.Should().Be(650);
        result.OilPressurePsi.Should().Be(48.3);
        result.OilTemperatureCelsius.Should().Be(99);
        DateTimeOffset.Parse(result.Timestamp).Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task GetEngineStatus_WhenDisconnected_ReturnsFriendlyError()
    {
        var simConnect = new Mock<ISimConnectService>();
        simConnect.Setup(s => s.IsConnected).Returns(false);
        var tool = new EngineStatusTool(simConnect.Object, NullLogger<EngineStatusTool>.Instance);

        var result = await tool.GetEngineStatus(CancellationToken.None);

        result.Error.Should().Be("SimConnect not available. Is MSFS running?");
    }

    [Fact]
    public async Task GetEngineStatus_WhenTimeout_ReturnsTimeoutMessage()
    {
        var simConnect = new Mock<ISimConnectService>();
        simConnect.Setup(s => s.IsConnected).Returns(true);
        simConnect
            .Setup(s => s.RequestDataAsync<EngineStatusData>(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException());

        var tool = new EngineStatusTool(simConnect.Object, NullLogger<EngineStatusTool>.Instance);

        var result = await tool.GetEngineStatus(CancellationToken.None);

        result.Error.Should().Be("Request timed out. MSFS may be loading or on main menu.");
    }

    [Fact]
    public async Task GetEngineStatus_WhenDataIsNull_ReturnsFriendlyError()
    {
        var simConnect = new Mock<ISimConnectService>();
        simConnect.Setup(s => s.IsConnected).Returns(true);
        simConnect
            .Setup(s => s.RequestDataAsync<EngineStatusData>(It.IsAny<CancellationToken>()))
            .ReturnsAsync((EngineStatusData?)null);

        var tool = new EngineStatusTool(simConnect.Object, NullLogger<EngineStatusTool>.Instance);

        var result = await tool.GetEngineStatus(CancellationToken.None);

        result.Error.Should().Be("Unable to retrieve engine data. Ensure you are in an active flight.");
    }

    [Fact]
    public async Task GetEngineStatus_WhenCanceled_ReturnsCanceledMessage()
    {
        var simConnect = new Mock<ISimConnectService>();
        var tool = new EngineStatusTool(simConnect.Object, NullLogger<EngineStatusTool>.Instance);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await tool.GetEngineStatus(cts.Token);

        result.Error.Should().Be("Request canceled.");
    }

    [Fact]
    public async Task GetEngineStatus_WhenUnexpectedError_ReturnsGenericError()
    {
        var simConnect = new Mock<ISimConnectService>();
        simConnect.Setup(s => s.IsConnected).Returns(true);
        simConnect
            .Setup(s => s.RequestDataAsync<EngineStatusData>(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var tool = new EngineStatusTool(simConnect.Object, NullLogger<EngineStatusTool>.Instance);

        var result = await tool.GetEngineStatus(CancellationToken.None);

        result.Error.Should().Be("An unexpected error occurred.");
    }
}
