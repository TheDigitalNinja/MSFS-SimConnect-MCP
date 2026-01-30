using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MsfsMcpServer.Models;
using MsfsMcpServer.Services;
using MsfsMcpServer.Tools;

namespace MsfsMcpServer.Tests.Tools;

public class FlightPositionToolTests
{
    [Fact]
    public async Task GetFlightPosition_WhenConnected_ReturnsRoundedData()
    {
        var simConnect = new Mock<ISimConnectService>();
        simConnect.Setup(s => s.IsConnected).Returns(true);
        simConnect
            .Setup(s => s.RequestDataAsync<FlightPositionData>(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FlightPositionData
            {
                Latitude = 39.85612,
                Longitude = -104.67368,
                AltitudeMslFeet = 8499.6,
                HeadingTrue = 270.54,
                HeadingMagnetic = 268.21,
                GroundSpeedKnots = 119.6,
                VerticalSpeedFpm = 499.5,
                PitchDegrees = 5.24,
                BankDegrees = -2.12,
                RadioAltitudeFeet = 210.3,
                HeightAboveGroundFeet = 215.2,
                WindSpeedKnots = 12.7,
                WindDirectionDegrees = 185.2,
                TotalAirTemperatureCelsius = -12.44,
                AngleOfAttackRadians = 0.0873,
                SlipSkidBall = -0.04,
                OnGround = 0
            });

        var callLogger = new Mock<IToolCallLogger>();
        var tool = new FlightPositionTool(simConnect.Object, NullLogger<FlightPositionTool>.Instance, callLogger.Object);

        var result = await tool.GetFlightPosition(CancellationToken.None);

        result.Error.Should().BeNull();
        result.Latitude.Should().Be(39.8561);
        result.Longitude.Should().Be(-104.6737);
        result.AltitudeMslFeet.Should().Be(8500);
        result.HeadingTrue.Should().Be(270.5);
        result.HeadingMagnetic.Should().Be(268.2);
        result.GroundSpeedKnots.Should().Be(120);
        result.VerticalSpeedFpm.Should().Be(500);
        result.PitchDegrees.Should().Be(5.2);
        result.BankDegrees.Should().Be(-2.1);
        result.RadioAltitudeFeet.Should().Be(210);
        result.HeightAboveGroundFeet.Should().Be(215);
        result.WindSpeedKnots.Should().Be(13);
        result.WindDirectionDegrees.Should().Be(185);
        result.TotalAirTemperatureCelsius.Should().Be(-12.4);
        result.AngleOfAttackDegrees.Should().Be(5);
        result.SlipSkidBall.Should().Be(-0.04);
        result.OnGround.Should().BeFalse();
        DateTimeOffset.Parse(result.Timestamp).Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
        callLogger.Verify(l => l.LogSuccess("get_flight_position", It.IsAny<TimeSpan>()), Times.Once);
    }

    [Fact]
    public async Task GetFlightPosition_WhenDisconnected_ReturnsFriendlyError()
    {
        var simConnect = new Mock<ISimConnectService>();
        simConnect.Setup(s => s.IsConnected).Returns(false);
        var callLogger = new Mock<IToolCallLogger>();
        var tool = new FlightPositionTool(simConnect.Object, NullLogger<FlightPositionTool>.Instance, callLogger.Object);

        var result = await tool.GetFlightPosition(CancellationToken.None);

        result.Error.Should().Be("SimConnect not available. Is MSFS running?");
        callLogger.Verify(l => l.LogFailure("get_flight_position", It.IsAny<TimeSpan>(), "SimConnect not available. Is MSFS running?"), Times.Once);
    }

    [Fact]
    public async Task GetFlightPosition_WhenTimeout_ReturnsTimeoutMessage()
    {
        var simConnect = new Mock<ISimConnectService>();
        simConnect.Setup(s => s.IsConnected).Returns(true);
        simConnect
            .Setup(s => s.RequestDataAsync<FlightPositionData>(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException());

        var callLogger = new Mock<IToolCallLogger>();
        var tool = new FlightPositionTool(simConnect.Object, NullLogger<FlightPositionTool>.Instance, callLogger.Object);

        var result = await tool.GetFlightPosition(CancellationToken.None);

        result.Error.Should().Be("Request timed out. MSFS may be loading or on main menu.");
        callLogger.Verify(l => l.LogFailure("get_flight_position", It.IsAny<TimeSpan>(), "Request timed out. MSFS may be loading or on main menu."), Times.Once);
    }

    [Fact]
    public async Task GetFlightPosition_WhenDataIsNull_ReturnsFriendlyError()
    {
        var simConnect = new Mock<ISimConnectService>();
        simConnect.Setup(s => s.IsConnected).Returns(true);
        simConnect
            .Setup(s => s.RequestDataAsync<FlightPositionData>(It.IsAny<CancellationToken>()))
            .ReturnsAsync((FlightPositionData?)null);

        var callLogger = new Mock<IToolCallLogger>();
        var tool = new FlightPositionTool(simConnect.Object, NullLogger<FlightPositionTool>.Instance, callLogger.Object);

        var result = await tool.GetFlightPosition(CancellationToken.None);

        result.Error.Should().Be("Unable to retrieve flight data. Ensure you are in an active flight.");
        callLogger.Verify(l => l.LogFailure("get_flight_position", It.IsAny<TimeSpan>(), "Unable to retrieve flight data. Ensure you are in an active flight."), Times.Once);
    }

    [Fact]
    public async Task GetFlightPosition_WhenCanceled_ReturnsCanceledMessage()
    {
        var simConnect = new Mock<ISimConnectService>();
        var callLogger = new Mock<IToolCallLogger>();
        var tool = new FlightPositionTool(simConnect.Object, NullLogger<FlightPositionTool>.Instance, callLogger.Object);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await tool.GetFlightPosition(cts.Token);

        result.Error.Should().Be("Request canceled.");
        callLogger.Verify(l => l.LogFailure("get_flight_position", It.IsAny<TimeSpan>(), "Request canceled."), Times.Once);
    }
}
