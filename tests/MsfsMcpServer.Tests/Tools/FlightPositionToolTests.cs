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
                GroundSpeedKnots = 119.6,
                VerticalSpeedFpm = 499.5,
                PitchDegrees = 5.24,
                BankDegrees = -2.12
            });

        var tool = new FlightPositionTool(simConnect.Object, NullLogger<FlightPositionTool>.Instance);

        var result = await tool.GetFlightPosition(CancellationToken.None);

        result.Error.Should().BeNull();
        result.Latitude.Should().Be(39.8561);
        result.Longitude.Should().Be(-104.6737);
        result.AltitudeMslFeet.Should().Be(8500);
        result.HeadingTrue.Should().Be(270.5);
        result.GroundSpeedKnots.Should().Be(120);
        result.VerticalSpeedFpm.Should().Be(500);
        result.PitchDegrees.Should().Be(5.2);
        result.BankDegrees.Should().Be(-2.1);
        DateTimeOffset.Parse(result.Timestamp).Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task GetFlightPosition_WhenDisconnected_ReturnsFriendlyError()
    {
        var simConnect = new Mock<ISimConnectService>();
        simConnect.Setup(s => s.IsConnected).Returns(false);
        var tool = new FlightPositionTool(simConnect.Object, NullLogger<FlightPositionTool>.Instance);

        var result = await tool.GetFlightPosition(CancellationToken.None);

        result.Error.Should().Be("SimConnect not available. Is MSFS running?");
    }

    [Fact]
    public async Task GetFlightPosition_WhenTimeout_ReturnsTimeoutMessage()
    {
        var simConnect = new Mock<ISimConnectService>();
        simConnect.Setup(s => s.IsConnected).Returns(true);
        simConnect
            .Setup(s => s.RequestDataAsync<FlightPositionData>(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException());

        var tool = new FlightPositionTool(simConnect.Object, NullLogger<FlightPositionTool>.Instance);

        var result = await tool.GetFlightPosition(CancellationToken.None);

        result.Error.Should().Be("Request timed out. MSFS may be loading or on main menu.");
    }

    [Fact]
    public async Task GetFlightPosition_WhenDataIsNull_ReturnsFriendlyError()
    {
        var simConnect = new Mock<ISimConnectService>();
        simConnect.Setup(s => s.IsConnected).Returns(true);
        simConnect
            .Setup(s => s.RequestDataAsync<FlightPositionData>(It.IsAny<CancellationToken>()))
            .ReturnsAsync((FlightPositionData?)null);

        var tool = new FlightPositionTool(simConnect.Object, NullLogger<FlightPositionTool>.Instance);

        var result = await tool.GetFlightPosition(CancellationToken.None);

        result.Error.Should().Be("Unable to retrieve flight data. Ensure you are in an active flight.");
    }

    [Fact]
    public async Task GetFlightPosition_WhenCanceled_ReturnsCanceledMessage()
    {
        var simConnect = new Mock<ISimConnectService>();
        var tool = new FlightPositionTool(simConnect.Object, NullLogger<FlightPositionTool>.Instance);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await tool.GetFlightPosition(cts.Token);

        result.Error.Should().Be("Request canceled.");
    }
}
