using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MsfsMcpServer.Models;
using MsfsMcpServer.Services;
using MsfsMcpServer.Tools;

namespace MsfsMcpServer.Tests.Tools;

public class AircraftConfigurationToolTests
{
    [Fact]
    public async Task GetAircraftConfiguration_WhenConnected_ReturnsData()
    {
        var simConnect = new Mock<ISimConnectService>();
        simConnect.Setup(s => s.IsConnected).Returns(true);
        simConnect
            .Setup(s => s.RequestDataAsync<AircraftConfigurationData>(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AircraftConfigurationData
            {
                GearHandlePosition = 100,
                GearTotalPercent = 98.2,
                FlapsHandleIndex = 2,
                FlapsLeadingEdgePercent = 10.2,
                FlapsTrailingEdgePercent = 20.4,
                SpoilersHandlePercent = 0,
                AutobrakeLevel = 3,
                ParkingBrakeEngaged = 0,
                ParkingBrakeIndicator = 0,
                RudderTrimPercent = 1.2,
                ElevatorTrimPercent = 3.4,
                AileronTrimPercent = -0.2,
                BeaconLightOn = 1,
                StrobeLightOn = 1,
                LandingLightOn = 0,
                NavLightOn = 1,
                TaxiLightOn = 0
            });

        var callLogger = new Mock<IToolCallLogger>();
        var tool = new AircraftConfigurationTool(simConnect.Object, NullLogger<AircraftConfigurationTool>.Instance, callLogger.Object);

        var result = await tool.GetAircraftConfiguration(CancellationToken.None);

        result.Error.Should().BeNull();
        result.GearHandlePercent.Should().Be(100);
        result.GearExtensionPercent.Should().Be(98);
        result.FlapsHandleIndex.Should().Be(2);
        result.FlapsLeadingEdgePercent.Should().Be(10);
        result.FlapsTrailingEdgePercent.Should().Be(20);
        result.SpoilersHandlePercent.Should().Be(0);
        result.AutobrakeLevel.Should().Be(3);
        result.ParkingBrakeEngaged.Should().BeFalse();
        result.ParkingBrakeIndicator.Should().BeFalse();
        result.RudderTrimPercent.Should().Be(1.2);
        result.ElevatorTrimPercent.Should().Be(3.4);
        result.AileronTrimPercent.Should().Be(-0.2);
        result.BeaconLightOn.Should().BeTrue();
        result.StrobeLightOn.Should().BeTrue();
        result.LandingLightOn.Should().BeFalse();
        result.NavLightOn.Should().BeTrue();
        result.TaxiLightOn.Should().BeFalse();
        callLogger.Verify(l => l.LogSuccess("get_aircraft_configuration", It.IsAny<TimeSpan>()), Times.Once);
    }

    [Fact]
    public async Task GetAircraftConfiguration_WhenDisconnected_ReturnsFriendlyError()
    {
        var simConnect = new Mock<ISimConnectService>();
        simConnect.Setup(s => s.IsConnected).Returns(false);
        var callLogger = new Mock<IToolCallLogger>();
        var tool = new AircraftConfigurationTool(simConnect.Object, NullLogger<AircraftConfigurationTool>.Instance, callLogger.Object);

        var result = await tool.GetAircraftConfiguration(CancellationToken.None);

        result.Error.Should().Be("SimConnect not available. Is MSFS running?");
        callLogger.Verify(l => l.LogFailure("get_aircraft_configuration", It.IsAny<TimeSpan>(), "SimConnect not available. Is MSFS running?"), Times.Once);
    }

    [Fact]
    public async Task GetAircraftConfiguration_WhenTimeout_ReturnsTimeoutMessage()
    {
        var simConnect = new Mock<ISimConnectService>();
        simConnect.Setup(s => s.IsConnected).Returns(true);
        simConnect
            .Setup(s => s.RequestDataAsync<AircraftConfigurationData>(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException());

        var callLogger = new Mock<IToolCallLogger>();
        var tool = new AircraftConfigurationTool(simConnect.Object, NullLogger<AircraftConfigurationTool>.Instance, callLogger.Object);

        var result = await tool.GetAircraftConfiguration(CancellationToken.None);

        result.Error.Should().Be("Request timed out. MSFS may be loading or on main menu.");
        callLogger.Verify(l => l.LogFailure("get_aircraft_configuration", It.IsAny<TimeSpan>(), "Request timed out. MSFS may be loading or on main menu."), Times.Once);
    }
}
