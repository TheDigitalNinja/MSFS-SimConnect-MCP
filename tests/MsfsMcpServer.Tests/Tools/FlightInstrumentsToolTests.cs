using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MsfsMcpServer.Models;
using MsfsMcpServer.Services;
using MsfsMcpServer.Tools;

namespace MsfsMcpServer.Tests.Tools;

public class FlightInstrumentsToolTests
{
    [Fact]
    public async Task GetFlightInstruments_WhenConnected_ReturnsRoundedData()
    {
        var simConnect = new Mock<ISimConnectService>();
        simConnect.Setup(s => s.IsConnected).Returns(true);
        simConnect
            .Setup(s => s.RequestDataAsync<FlightInstrumentsData>(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FlightInstrumentsData
            {
                IndicatedAltitudeFeet = 1234.6,
                AirspeedIndicatedKnots = 145.5,
                AirspeedTrueKnots = 152.4,
                Mach = 0.78456,
                HeadingIndicatorDegrees = 123.44,
                AltimeterSettingInHg = 29.9213,
                AttitudePitchDegrees = 2.36,
                AttitudeBankDegrees = -3.26
            });

        var callLogger = new Mock<IToolCallLogger>();
        var tool = new FlightInstrumentsTool(simConnect.Object, NullLogger<FlightInstrumentsTool>.Instance, callLogger.Object);

        var result = await tool.GetFlightInstruments(CancellationToken.None);

        result.Error.Should().BeNull();
        result.IndicatedAltitudeFeet.Should().Be(1235);
        result.AirspeedIndicatedKnots.Should().Be(146);
        result.AirspeedTrueKnots.Should().Be(152);
        result.Mach.Should().Be(0.785);
        result.HeadingIndicatorDegrees.Should().Be(123.4);
        result.AltimeterSettingInHg.Should().Be(29.921);
        result.AttitudePitchDegrees.Should().Be(2.4);
        result.AttitudeBankDegrees.Should().Be(-3.3);
        DateTimeOffset.Parse(result.Timestamp).Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
        callLogger.Verify(l => l.LogSuccess("get_flight_instruments", It.IsAny<TimeSpan>()), Times.Once);
    }

    [Fact]
    public async Task GetFlightInstruments_WhenDisconnected_ReturnsFriendlyError()
    {
        var simConnect = new Mock<ISimConnectService>();
        simConnect.Setup(s => s.IsConnected).Returns(false);
        var callLogger = new Mock<IToolCallLogger>();
        var tool = new FlightInstrumentsTool(simConnect.Object, NullLogger<FlightInstrumentsTool>.Instance, callLogger.Object);

        var result = await tool.GetFlightInstruments(CancellationToken.None);

        result.Error.Should().Be("SimConnect not available. Is MSFS running?");
        callLogger.Verify(l => l.LogFailure("get_flight_instruments", It.IsAny<TimeSpan>(), "SimConnect not available. Is MSFS running?"), Times.Once);
    }

    [Fact]
    public async Task GetFlightInstruments_WhenTimeout_ReturnsTimeoutMessage()
    {
        var simConnect = new Mock<ISimConnectService>();
        simConnect.Setup(s => s.IsConnected).Returns(true);
        simConnect
            .Setup(s => s.RequestDataAsync<FlightInstrumentsData>(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException());

        var callLogger = new Mock<IToolCallLogger>();
        var tool = new FlightInstrumentsTool(simConnect.Object, NullLogger<FlightInstrumentsTool>.Instance, callLogger.Object);

        var result = await tool.GetFlightInstruments(CancellationToken.None);

        result.Error.Should().Be("Request timed out. MSFS may be loading or on main menu.");
        callLogger.Verify(l => l.LogFailure("get_flight_instruments", It.IsAny<TimeSpan>(), "Request timed out. MSFS may be loading or on main menu."), Times.Once);
    }

    [Fact]
    public async Task GetFlightInstruments_WhenDataIsNull_ReturnsFriendlyError()
    {
        var simConnect = new Mock<ISimConnectService>();
        simConnect.Setup(s => s.IsConnected).Returns(true);
        simConnect
            .Setup(s => s.RequestDataAsync<FlightInstrumentsData>(It.IsAny<CancellationToken>()))
            .ReturnsAsync((FlightInstrumentsData?)null);

        var callLogger = new Mock<IToolCallLogger>();
        var tool = new FlightInstrumentsTool(simConnect.Object, NullLogger<FlightInstrumentsTool>.Instance, callLogger.Object);

        var result = await tool.GetFlightInstruments(CancellationToken.None);

        result.Error.Should().Be("Unable to retrieve flight data. Ensure you are in an active flight.");
        callLogger.Verify(l => l.LogFailure("get_flight_instruments", It.IsAny<TimeSpan>(), "Unable to retrieve flight data. Ensure you are in an active flight."), Times.Once);
    }

    [Fact]
    public async Task GetFlightInstruments_WhenCanceled_ReturnsCanceledMessage()
    {
        var simConnect = new Mock<ISimConnectService>();
        var callLogger = new Mock<IToolCallLogger>();
        var tool = new FlightInstrumentsTool(simConnect.Object, NullLogger<FlightInstrumentsTool>.Instance, callLogger.Object);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await tool.GetFlightInstruments(cts.Token);

        result.Error.Should().Be("Request canceled.");
        callLogger.Verify(l => l.LogFailure("get_flight_instruments", It.IsAny<TimeSpan>(), "Request canceled."), Times.Once);
    }
}
