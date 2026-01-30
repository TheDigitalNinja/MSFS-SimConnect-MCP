using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MsfsMcpServer.Models;
using MsfsMcpServer.Services;
using MsfsMcpServer.Tools;

namespace MsfsMcpServer.Tests.Tools;

public class FlightPlanLegToolTests
{
    [Fact]
    public async Task GetFlightPlanLeg_WhenActive_ReturnsLegData()
    {
        var simConnect = new Mock<ISimConnectService>();
        simConnect.Setup(s => s.IsConnected).Returns(true);
        simConnect
            .Setup(s => s.RequestDataAsync<FlightPlanLegData>(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FlightPlanLegData
            {
                IsFlightPlanActive = 1,
                WaypointCount = 5,
                ActiveWaypointIndex = 2,
                NextWaypointId = "DRAKE",
                NextWaypointLatitude = 39.123456,
                NextWaypointLongitude = -104.654321,
                NextWaypointAltitudeFeet = 8000.4,
                NextWaypointBearingDegrees = 123.45,
                DesiredTrackDegrees = 124.44,
                DistanceToNextNauticalMiles = 12.34,
                EteToNextSeconds = 600.5,
                EtaToNextSeconds = 36000.8,
                CrossTrackErrorNauticalMiles = -0.25,
                DistanceToDestinationNauticalMiles = 55.6,
                EteToDestinationSeconds = 3200.2,
                EtaToDestinationSeconds = 39000.7
            });

        var callLogger = new Mock<IToolCallLogger>();
        var tool = new FlightPlanLegTool(simConnect.Object, NullLogger<FlightPlanLegTool>.Instance, callLogger.Object);

        var result = await tool.GetFlightPlanLeg(CancellationToken.None);

        result.Error.Should().BeNull();
        result.FlightPlanActive.Should().BeTrue();
        result.WaypointCount.Should().Be(5);
        result.ActiveWaypointIndex.Should().Be(2);
        result.NextWaypointId.Should().Be("DRAKE");
        result.NextLatitude.Should().Be(39.12346);
        result.NextLongitude.Should().Be(-104.65432);
        result.NextAltitudeFeet.Should().Be(8000);
        result.BearingToNextDegrees.Should().Be(123.5);
        result.DesiredTrackDegrees.Should().Be(124.4);
        result.DistanceToNextNauticalMiles.Should().Be(12.3);
        result.EteToNextSeconds.Should().Be(600.5);
        result.EtaToNextSeconds.Should().Be(36000.8);
        result.CrossTrackErrorNauticalMiles.Should().Be(-0.25);
        result.DistanceToDestinationNauticalMiles.Should().Be(55.6);
        result.EteToDestinationSeconds.Should().Be(3200.2);
        result.EtaToDestinationSeconds.Should().Be(39000.7);
        DateTimeOffset.Parse(result.Timestamp).Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
        callLogger.Verify(l => l.LogSuccess("get_flight_plan_leg", It.IsAny<TimeSpan>()), Times.Once);
    }

    [Fact]
    public async Task GetFlightPlanLeg_WhenDisconnected_ReturnsFriendlyError()
    {
        var simConnect = new Mock<ISimConnectService>();
        simConnect.Setup(s => s.IsConnected).Returns(false);
        var callLogger = new Mock<IToolCallLogger>();
        var tool = new FlightPlanLegTool(simConnect.Object, NullLogger<FlightPlanLegTool>.Instance, callLogger.Object);

        var result = await tool.GetFlightPlanLeg(CancellationToken.None);

        result.Error.Should().Be("SimConnect not available. Is MSFS running?");
        callLogger.Verify(l => l.LogFailure("get_flight_plan_leg", It.IsAny<TimeSpan>(), "SimConnect not available. Is MSFS running?"), Times.Once);
    }

    [Fact]
    public async Task GetFlightPlanLeg_WhenTimeout_ReturnsTimeoutMessage()
    {
        var simConnect = new Mock<ISimConnectService>();
        simConnect.Setup(s => s.IsConnected).Returns(true);
        simConnect
            .Setup(s => s.RequestDataAsync<FlightPlanLegData>(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException());

        var callLogger = new Mock<IToolCallLogger>();
        var tool = new FlightPlanLegTool(simConnect.Object, NullLogger<FlightPlanLegTool>.Instance, callLogger.Object);

        var result = await tool.GetFlightPlanLeg(CancellationToken.None);

        result.Error.Should().Be("Request timed out. MSFS may be loading or on main menu.");
        callLogger.Verify(l => l.LogFailure("get_flight_plan_leg", It.IsAny<TimeSpan>(), "Request timed out. MSFS may be loading or on main menu."), Times.Once);
    }
}
