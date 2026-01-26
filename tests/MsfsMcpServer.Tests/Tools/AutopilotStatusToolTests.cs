using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MsfsMcpServer.Models;
using MsfsMcpServer.Services;
using MsfsMcpServer.Tools;

namespace MsfsMcpServer.Tests.Tools;

public class AutopilotStatusToolTests
{
    [Fact]
    public async Task GetAutopilotStatus_WhenConnected_ReturnsData()
    {
        var simConnect = new Mock<ISimConnectService>();
        simConnect.Setup(s => s.IsConnected).Returns(true);
        simConnect
            .Setup(s => s.RequestDataAsync<AutopilotStatusData>(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AutopilotStatusData
            {
                AutopilotMaster = 1,
                HeadingLock = 1,
                HeadingLockDegrees = 182.4,
                AltitudeLock = 1,
                AltitudeLockFeet = 12000.4,
                AirspeedHold = 0,
                AirspeedHoldKnots = 250.7,
                VerticalHold = 1,
                VerticalHoldFpm = 800.4,
                NavLock = 1,
                ApproachHold = 0
            });

        var callLogger = new Mock<IToolCallLogger>();
        var tool = new AutopilotStatusTool(simConnect.Object, NullLogger<AutopilotStatusTool>.Instance, callLogger.Object);

        var result = await tool.GetAutopilotStatus(CancellationToken.None);

        result.Error.Should().BeNull();
        result.AutopilotMaster.Should().BeTrue();
        result.HeadingMode.Should().BeTrue();
        result.HeadingSelectDegrees.Should().Be(182);
        result.AltitudeHoldMode.Should().BeTrue();
        result.AltitudeSelectFeet.Should().Be(12000);
        result.SpeedHoldMode.Should().BeFalse();
        result.SpeedSelectKnots.Should().Be(251);
        result.VerticalSpeedMode.Should().BeTrue();
        result.VerticalSpeedFpm.Should().Be(800);
        result.NavMode.Should().BeTrue();
        result.ApproachMode.Should().BeFalse();
        DateTimeOffset.Parse(result.Timestamp).Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
        callLogger.Verify(l => l.LogSuccess("get_autopilot_status", It.IsAny<TimeSpan>()), Times.Once);
    }

    [Fact]
    public async Task GetAutopilotStatus_WhenDisconnected_ReturnsFriendlyError()
    {
        var simConnect = new Mock<ISimConnectService>();
        simConnect.Setup(s => s.IsConnected).Returns(false);
        var callLogger = new Mock<IToolCallLogger>();
        var tool = new AutopilotStatusTool(simConnect.Object, NullLogger<AutopilotStatusTool>.Instance, callLogger.Object);

        var result = await tool.GetAutopilotStatus(CancellationToken.None);

        result.Error.Should().Be("SimConnect not available. Is MSFS running?");
        callLogger.Verify(l => l.LogFailure("get_autopilot_status", It.IsAny<TimeSpan>(), "SimConnect not available. Is MSFS running?"), Times.Once);
    }

    [Fact]
    public async Task GetAutopilotStatus_WhenTimeout_ReturnsTimeoutMessage()
    {
        var simConnect = new Mock<ISimConnectService>();
        simConnect.Setup(s => s.IsConnected).Returns(true);
        simConnect
            .Setup(s => s.RequestDataAsync<AutopilotStatusData>(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException());

        var callLogger = new Mock<IToolCallLogger>();
        var tool = new AutopilotStatusTool(simConnect.Object, NullLogger<AutopilotStatusTool>.Instance, callLogger.Object);

        var result = await tool.GetAutopilotStatus(CancellationToken.None);

        result.Error.Should().Be("Request timed out. MSFS may be loading or on main menu.");
        callLogger.Verify(l => l.LogFailure("get_autopilot_status", It.IsAny<TimeSpan>(), "Request timed out. MSFS may be loading or on main menu."), Times.Once);
    }

    [Fact]
    public async Task GetAutopilotStatus_WhenDataIsNull_ReturnsFriendlyError()
    {
        var simConnect = new Mock<ISimConnectService>();
        simConnect.Setup(s => s.IsConnected).Returns(true);
        simConnect
            .Setup(s => s.RequestDataAsync<AutopilotStatusData>(It.IsAny<CancellationToken>()))
            .ReturnsAsync((AutopilotStatusData?)null);

        var callLogger = new Mock<IToolCallLogger>();
        var tool = new AutopilotStatusTool(simConnect.Object, NullLogger<AutopilotStatusTool>.Instance, callLogger.Object);

        var result = await tool.GetAutopilotStatus(CancellationToken.None);

        result.Error.Should().Be("Unable to retrieve autopilot data. Ensure you are in an active flight.");
        callLogger.Verify(l => l.LogFailure("get_autopilot_status", It.IsAny<TimeSpan>(), "Unable to retrieve autopilot data. Ensure you are in an active flight."), Times.Once);
    }

    [Fact]
    public async Task GetAutopilotStatus_WhenCanceled_ReturnsCanceledMessage()
    {
        var simConnect = new Mock<ISimConnectService>();
        var callLogger = new Mock<IToolCallLogger>();
        var tool = new AutopilotStatusTool(simConnect.Object, NullLogger<AutopilotStatusTool>.Instance, callLogger.Object);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await tool.GetAutopilotStatus(cts.Token);

        result.Error.Should().Be("Request canceled.");
        callLogger.Verify(l => l.LogFailure("get_autopilot_status", It.IsAny<TimeSpan>(), "Request canceled."), Times.Once);
    }
}
