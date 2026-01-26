using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MsfsMcpServer.Models;
using MsfsMcpServer.Services;
using MsfsMcpServer.Tools;

namespace MsfsMcpServer.Tests.Tools;

public class AircraftInfoToolTests
{
    [Fact]
    public async Task GetAircraftInfo_WhenConnected_ReturnsData()
    {
        var simConnect = new Mock<ISimConnectService>();
        simConnect.Setup(s => s.IsConnected).Returns(true);
        simConnect
            .Setup(s => s.RequestDataAsync<AircraftInfoData>(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AircraftInfoData
            {
                Title = "Cessna 172SP   ",
                AtcId = "N123AB ",
                AtcAirline = "Delta",
                TotalWeightPounds = 2450.4,
                MaxGrossWeightPounds = 2550.6,
                EmptyWeightPounds = 1660.4
            });

        var tool = new AircraftInfoTool(simConnect.Object, NullLogger<AircraftInfoTool>.Instance);

        var result = await tool.GetAircraftInfo(CancellationToken.None);

        result.Error.Should().BeNull();
        result.AircraftTitle.Should().Be("Cessna 172SP");
        result.TailNumber.Should().Be("N123AB");
        result.Airline.Should().Be("Delta");
        result.TotalWeightPounds.Should().Be(2450);
        result.MaxGrossWeightPounds.Should().Be(2551);
        result.EmptyWeightPounds.Should().Be(1660);
        DateTimeOffset.Parse(result.Timestamp).Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task GetAircraftInfo_WhenDisconnected_ReturnsFriendlyError()
    {
        var simConnect = new Mock<ISimConnectService>();
        simConnect.Setup(s => s.IsConnected).Returns(false);
        var tool = new AircraftInfoTool(simConnect.Object, NullLogger<AircraftInfoTool>.Instance);

        var result = await tool.GetAircraftInfo(CancellationToken.None);

        result.Error.Should().Be("SimConnect not available. Is MSFS running?");
    }

    [Fact]
    public async Task GetAircraftInfo_WhenTimeout_ReturnsTimeoutMessage()
    {
        var simConnect = new Mock<ISimConnectService>();
        simConnect.Setup(s => s.IsConnected).Returns(true);
        simConnect
            .Setup(s => s.RequestDataAsync<AircraftInfoData>(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException());

        var tool = new AircraftInfoTool(simConnect.Object, NullLogger<AircraftInfoTool>.Instance);

        var result = await tool.GetAircraftInfo(CancellationToken.None);

        result.Error.Should().Be("Request timed out. MSFS may be loading or on main menu.");
    }

    [Fact]
    public async Task GetAircraftInfo_WhenDataIsNull_ReturnsFriendlyError()
    {
        var simConnect = new Mock<ISimConnectService>();
        simConnect.Setup(s => s.IsConnected).Returns(true);
        simConnect
            .Setup(s => s.RequestDataAsync<AircraftInfoData>(It.IsAny<CancellationToken>()))
            .ReturnsAsync((AircraftInfoData?)null);

        var tool = new AircraftInfoTool(simConnect.Object, NullLogger<AircraftInfoTool>.Instance);

        var result = await tool.GetAircraftInfo(CancellationToken.None);

        result.Error.Should().Be("Unable to retrieve aircraft info. Ensure you are in an active flight.");
    }

    [Fact]
    public async Task GetAircraftInfo_WhenCanceled_ReturnsCanceledMessage()
    {
        var simConnect = new Mock<ISimConnectService>();
        var tool = new AircraftInfoTool(simConnect.Object, NullLogger<AircraftInfoTool>.Instance);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await tool.GetAircraftInfo(cts.Token);

        result.Error.Should().Be("Request canceled.");
    }
}
