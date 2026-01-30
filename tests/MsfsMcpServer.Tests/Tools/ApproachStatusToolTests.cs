using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MsfsMcpServer.Models;
using MsfsMcpServer.Services;
using MsfsMcpServer.Tools;

namespace MsfsMcpServer.Tests.Tools;

public class ApproachStatusToolTests
{
    [Fact]
    public async Task GetApproachStatus_WhenConnected_ReturnsData()
    {
        var simConnect = new Mock<ISimConnectService>();
        simConnect.Setup(s => s.IsConnected).Returns(true);
        simConnect
            .Setup(s => s.RequestDataAsync<ApproachStatusData>(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApproachStatusData
            {
                ApproachLoaded = 1,
                ApproachActive = 1,
                ApproachWaypointIndex = 2,
                ApproachWaypointCount = 6,
                IsFinalApproachSegment = 1,
                IsMissedApproachSegment = 0,
                GpsHasGlidepath = 1,
                GlideSlopeErrorDots = -0.15,
                GpsGsiNeedleDots = 0.05
            });

        var callLogger = new Mock<IToolCallLogger>();
        var tool = new ApproachStatusTool(simConnect.Object, NullLogger<ApproachStatusTool>.Instance, callLogger.Object);

        var result = await tool.GetApproachStatus(CancellationToken.None);

        result.Error.Should().BeNull();
        result.ApproachLoaded.Should().BeTrue();
        result.ApproachActive.Should().BeTrue();
        result.ApproachWaypointIndex.Should().Be(2);
        result.ApproachWaypointCount.Should().Be(6);
        result.IsFinalApproachSegment.Should().BeTrue();
        result.IsMissedApproachSegment.Should().BeFalse();
        result.GpsHasGlidepath.Should().BeTrue();
        result.GlideSlopeErrorDots.Should().Be(-0.15);
        result.GpsGsiDots.Should().Be(0.05);
        callLogger.Verify(l => l.LogSuccess("get_approach_status", It.IsAny<TimeSpan>()), Times.Once);
    }

    [Fact]
    public async Task GetApproachStatus_WhenDisconnected_ReturnsFriendlyError()
    {
        var simConnect = new Mock<ISimConnectService>();
        simConnect.Setup(s => s.IsConnected).Returns(false);
        var callLogger = new Mock<IToolCallLogger>();
        var tool = new ApproachStatusTool(simConnect.Object, NullLogger<ApproachStatusTool>.Instance, callLogger.Object);

        var result = await tool.GetApproachStatus(CancellationToken.None);

        result.Error.Should().Be("SimConnect not available. Is MSFS running?");
        callLogger.Verify(l => l.LogFailure("get_approach_status", It.IsAny<TimeSpan>(), "SimConnect not available. Is MSFS running?"), Times.Once);
    }

    [Fact]
    public async Task GetApproachStatus_WhenTimeout_ReturnsTimeoutMessage()
    {
        var simConnect = new Mock<ISimConnectService>();
        simConnect.Setup(s => s.IsConnected).Returns(true);
        simConnect
            .Setup(s => s.RequestDataAsync<ApproachStatusData>(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException());

        var callLogger = new Mock<IToolCallLogger>();
        var tool = new ApproachStatusTool(simConnect.Object, NullLogger<ApproachStatusTool>.Instance, callLogger.Object);

        var result = await tool.GetApproachStatus(CancellationToken.None);

        result.Error.Should().Be("Request timed out. MSFS may be loading or on main menu.");
        callLogger.Verify(l => l.LogFailure("get_approach_status", It.IsAny<TimeSpan>(), "Request timed out. MSFS may be loading or on main menu."), Times.Once);
    }
}
