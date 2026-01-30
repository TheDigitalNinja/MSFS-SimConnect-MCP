using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MsfsMcpServer.Models;
using MsfsMcpServer.Services;
using MsfsMcpServer.Tools;

namespace MsfsMcpServer.Tests.Tools;

public class NavigationStatusToolTests
{
    [Fact]
    public async Task GetNavigationStatus_WhenConnected_ReturnsData()
    {
        var simConnect = new Mock<ISimConnectService>();
        simConnect.Setup(s => s.IsConnected).Returns(true);
        simConnect
            .Setup(s => s.RequestDataAsync<NavigationStatusData>(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NavigationStatusData
            {
                GpsDrivesNav1 = 1,
                GpsObsDegrees = 180.44,
                NavObsDegrees = 181.55,
                NavCdi = -0.23,
                NavGsi = 0.45,
                NavHasLocalizer = 1,
                NavHasGlideSlope = 0,
                Nav1ActiveFrequencyMHz = 110.3,
                Nav2ActiveFrequencyMHz = 111.55,
                Nav1DmeNauticalMiles = 12.34,
                Nav1ToFrom = 1,
                MagneticVariationDegrees = -7.5
            });

        var callLogger = new Mock<IToolCallLogger>();
        var tool = new NavigationStatusTool(simConnect.Object, NullLogger<NavigationStatusTool>.Instance, callLogger.Object);

        var result = await tool.GetNavigationStatus(CancellationToken.None);

        result.Error.Should().BeNull();
        result.GpsDrivesNav1.Should().BeTrue();
        result.GpsObsDegrees.Should().Be(180.4);
        result.NavObsDegrees.Should().Be(181.6);
        result.NavCdiDots.Should().Be(-0.23);
        result.NavGsiDots.Should().Be(0.45);
        result.NavHasLocalizer.Should().BeTrue();
        result.NavHasGlideSlope.Should().BeFalse();
        result.Nav1ActiveFrequencyMHz.Should().Be(110.3);
        result.Nav2ActiveFrequencyMHz.Should().Be(111.55);
        result.Nav1DmeNauticalMiles.Should().Be(12.3);
        result.Nav1ToFrom.Should().Be(1);
        result.MagneticVariationDegrees.Should().Be(-7.5);
        callLogger.Verify(l => l.LogSuccess("get_navigation_status", It.IsAny<TimeSpan>()), Times.Once);
    }

    [Fact]
    public async Task GetNavigationStatus_WhenDisconnected_ReturnsFriendlyError()
    {
        var simConnect = new Mock<ISimConnectService>();
        simConnect.Setup(s => s.IsConnected).Returns(false);
        var callLogger = new Mock<IToolCallLogger>();
        var tool = new NavigationStatusTool(simConnect.Object, NullLogger<NavigationStatusTool>.Instance, callLogger.Object);

        var result = await tool.GetNavigationStatus(CancellationToken.None);

        result.Error.Should().Be("SimConnect not available. Is MSFS running?");
        callLogger.Verify(l => l.LogFailure("get_navigation_status", It.IsAny<TimeSpan>(), "SimConnect not available. Is MSFS running?"), Times.Once);
    }

    [Fact]
    public async Task GetNavigationStatus_WhenTimeout_ReturnsTimeoutMessage()
    {
        var simConnect = new Mock<ISimConnectService>();
        simConnect.Setup(s => s.IsConnected).Returns(true);
        simConnect
            .Setup(s => s.RequestDataAsync<NavigationStatusData>(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException());

        var callLogger = new Mock<IToolCallLogger>();
        var tool = new NavigationStatusTool(simConnect.Object, NullLogger<NavigationStatusTool>.Instance, callLogger.Object);

        var result = await tool.GetNavigationStatus(CancellationToken.None);

        result.Error.Should().Be("Request timed out. MSFS may be loading or on main menu.");
        callLogger.Verify(l => l.LogFailure("get_navigation_status", It.IsAny<TimeSpan>(), "Request timed out. MSFS may be loading or on main menu."), Times.Once);
    }
}
