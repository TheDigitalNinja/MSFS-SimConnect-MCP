using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MsfsMcpServer.Models;
using MsfsMcpServer.Services;
using MsfsMcpServer.Tools;

namespace MsfsMcpServer.Tests.Tools;

public class FlightPlanWaypointToolTests
{
    [Fact]
    public async Task GetFlightPlanWaypoint_WhenNextRequested_ReturnsNext()
    {
        var simConnect = new Mock<ISimConnectService>();
        simConnect.Setup(s => s.IsConnected).Returns(true);
        simConnect
            .Setup(s => s.RequestDataAsync<FlightPlanWaypointData>(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FlightPlanWaypointData
            {
                ActiveWaypointIndex = 3,
                NextWaypointId = "NEXT1",
                NextWaypointLatitude = 40.123456,
                NextWaypointLongitude = -105.123456,
                NextWaypointAltitudeFeet = 9000.7,
                NextWaypointBearingDegrees = 270.44,
                NextWaypointDistanceNauticalMiles = 10.6,
                PreviousWaypointId = "PREV1",
                PreviousWaypointLatitude = 39.000111,
                PreviousWaypointLongitude = -104.000222,
                PreviousWaypointAltitudeFeet = 8500.2,
                PreviousWaypointBearingDegrees = 90.33,
                PreviousWaypointDistanceNauticalMiles = 5.4
            });

        var callLogger = new Mock<IToolCallLogger>();
        var tool = new FlightPlanWaypointTool(simConnect.Object, NullLogger<FlightPlanWaypointTool>.Instance, callLogger.Object);

        var result = await tool.GetFlightPlanWaypoint(3, CancellationToken.None);

        result.Error.Should().BeNull();
        result.WaypointId.Should().Be("NEXT1");
        result.Latitude.Should().Be(40.12346);
        result.Longitude.Should().Be(-105.12346);
        result.AltitudeFeet.Should().Be(9001);
        result.BearingDegrees.Should().Be(270.4);
        result.DistanceNauticalMiles.Should().Be(10.6);
        callLogger.Verify(l => l.LogSuccess("get_flight_plan_waypoint", It.IsAny<TimeSpan>()), Times.Once);
    }

    [Fact]
    public async Task GetFlightPlanWaypoint_WhenPreviousRequested_ReturnsPrevious()
    {
        var simConnect = new Mock<ISimConnectService>();
        simConnect.Setup(s => s.IsConnected).Returns(true);
        simConnect
            .Setup(s => s.RequestDataAsync<FlightPlanWaypointData>(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FlightPlanWaypointData
            {
                ActiveWaypointIndex = 3,
                NextWaypointId = "NEXT1",
                NextWaypointLatitude = 0,
                NextWaypointLongitude = 0,
                NextWaypointAltitudeFeet = 0,
                NextWaypointBearingDegrees = 0,
                NextWaypointDistanceNauticalMiles = 0,
                PreviousWaypointId = "PREV1",
                PreviousWaypointLatitude = 39.000111,
                PreviousWaypointLongitude = -104.000222,
                PreviousWaypointAltitudeFeet = 8500.2,
                PreviousWaypointBearingDegrees = 90.33,
                PreviousWaypointDistanceNauticalMiles = 5.4
            });

        var callLogger = new Mock<IToolCallLogger>();
        var tool = new FlightPlanWaypointTool(simConnect.Object, NullLogger<FlightPlanWaypointTool>.Instance, callLogger.Object);

        var result = await tool.GetFlightPlanWaypoint(2, CancellationToken.None);

        result.Error.Should().BeNull();
        result.WaypointId.Should().Be("PREV1");
        result.Latitude.Should().Be(39.00011);
        result.Longitude.Should().Be(-104.00022);
        result.AltitudeFeet.Should().Be(8500);
        result.BearingDegrees.Should().Be(90.3);
        result.DistanceNauticalMiles.Should().Be(5.4);
        callLogger.Verify(l => l.LogSuccess("get_flight_plan_waypoint", It.IsAny<TimeSpan>()), Times.Once);
    }

    [Fact]
    public async Task GetFlightPlanWaypoint_WhenIndexUnsupported_ReturnsFriendlyError()
    {
        var simConnect = new Mock<ISimConnectService>();
        simConnect.Setup(s => s.IsConnected).Returns(true);
        simConnect
            .Setup(s => s.RequestDataAsync<FlightPlanWaypointData>(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FlightPlanWaypointData { ActiveWaypointIndex = 3 });

        var callLogger = new Mock<IToolCallLogger>();
        var tool = new FlightPlanWaypointTool(simConnect.Object, NullLogger<FlightPlanWaypointTool>.Instance, callLogger.Object);

        var result = await tool.GetFlightPlanWaypoint(5, CancellationToken.None);

        result.Error.Should().Be("Only the active leg waypoints (active and previous) are exposed by GPS SimVars.");
        callLogger.Verify(l => l.LogFailure("get_flight_plan_waypoint", It.IsAny<TimeSpan>(), "Only the active leg waypoints (active and previous) are exposed by GPS SimVars."), Times.Once);
    }

    [Fact]
    public async Task GetFlightPlanWaypoint_WhenDisconnected_ReturnsFriendlyError()
    {
        var simConnect = new Mock<ISimConnectService>();
        simConnect.Setup(s => s.IsConnected).Returns(false);
        var callLogger = new Mock<IToolCallLogger>();
        var tool = new FlightPlanWaypointTool(simConnect.Object, NullLogger<FlightPlanWaypointTool>.Instance, callLogger.Object);

        var result = await tool.GetFlightPlanWaypoint(1, CancellationToken.None);

        result.Error.Should().Be("SimConnect not available. Is MSFS running?");
        callLogger.Verify(l => l.LogFailure("get_flight_plan_waypoint", It.IsAny<TimeSpan>(), "SimConnect not available. Is MSFS running?"), Times.Once);
    }
}
