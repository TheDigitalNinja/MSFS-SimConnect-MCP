using System.Text.Json.Serialization;

namespace MsfsMcpServer.Models;

/// <summary>
/// Response contract for aircraft configuration and lights.
/// </summary>
public sealed class AircraftConfigurationResponse
{
    [JsonPropertyName("gear_handle_percent")]
    public double GearHandlePercent { get; init; }

    [JsonPropertyName("gear_extension_percent")]
    public double GearExtensionPercent { get; init; }

    [JsonPropertyName("flaps_handle_index")]
    public int FlapsHandleIndex { get; init; }

    [JsonPropertyName("flaps_le_percent")]
    public double FlapsLeadingEdgePercent { get; init; }

    [JsonPropertyName("flaps_te_percent")]
    public double FlapsTrailingEdgePercent { get; init; }

    [JsonPropertyName("spoilers_handle_percent")]
    public double SpoilersHandlePercent { get; init; }

    [JsonPropertyName("autobrake_level")]
    public int AutobrakeLevel { get; init; }

    [JsonPropertyName("parking_brake_engaged")]
    public bool ParkingBrakeEngaged { get; init; }

    [JsonPropertyName("parking_brake_indicator")]
    public bool ParkingBrakeIndicator { get; init; }

    [JsonPropertyName("rudder_trim_percent")]
    public double RudderTrimPercent { get; init; }

    [JsonPropertyName("elevator_trim_percent")]
    public double ElevatorTrimPercent { get; init; }

    [JsonPropertyName("aileron_trim_percent")]
    public double AileronTrimPercent { get; init; }

    [JsonPropertyName("lights_beacon_on")]
    public bool BeaconLightOn { get; init; }

    [JsonPropertyName("lights_strobe_on")]
    public bool StrobeLightOn { get; init; }

    [JsonPropertyName("lights_landing_on")]
    public bool LandingLightOn { get; init; }

    [JsonPropertyName("lights_nav_on")]
    public bool NavLightOn { get; init; }

    [JsonPropertyName("lights_taxi_on")]
    public bool TaxiLightOn { get; init; }

    [JsonPropertyName("timestamp")]
    public string Timestamp { get; init; } = DateTimeOffset.UtcNow.ToString("O");

    [JsonPropertyName("error")]
    public string? Error { get; init; }

    public static AircraftConfigurationResponse FromData(AircraftConfigurationData data, DateTimeOffset? timestamp = null) =>
        new()
        {
            GearHandlePercent = Round(data.GearHandlePosition, 0),
            GearExtensionPercent = Round(data.GearTotalPercent, 0),
            FlapsHandleIndex = data.FlapsHandleIndex,
            FlapsLeadingEdgePercent = Round(data.FlapsLeadingEdgePercent, 0),
            FlapsTrailingEdgePercent = Round(data.FlapsTrailingEdgePercent, 0),
            SpoilersHandlePercent = Round(data.SpoilersHandlePercent, 0),
            AutobrakeLevel = data.AutobrakeLevel,
            ParkingBrakeEngaged = ToBool(data.ParkingBrakeEngaged),
            ParkingBrakeIndicator = ToBool(data.ParkingBrakeIndicator),
            RudderTrimPercent = Round(data.RudderTrimPercent, 1),
            ElevatorTrimPercent = Round(data.ElevatorTrimPercent, 1),
            AileronTrimPercent = Round(data.AileronTrimPercent, 1),
            BeaconLightOn = ToBool(data.BeaconLightOn),
            StrobeLightOn = ToBool(data.StrobeLightOn),
            LandingLightOn = ToBool(data.LandingLightOn),
            NavLightOn = ToBool(data.NavLightOn),
            TaxiLightOn = ToBool(data.TaxiLightOn),
            Timestamp = (timestamp ?? DateTimeOffset.UtcNow).ToString("O")
        };

    public static AircraftConfigurationResponse ErrorResponse(string message, DateTimeOffset? timestamp = null) =>
        new()
        {
            GearHandlePercent = 0,
            GearExtensionPercent = 0,
            FlapsHandleIndex = 0,
            FlapsLeadingEdgePercent = 0,
            FlapsTrailingEdgePercent = 0,
            SpoilersHandlePercent = 0,
            AutobrakeLevel = 0,
            ParkingBrakeEngaged = false,
            ParkingBrakeIndicator = false,
            RudderTrimPercent = 0,
            ElevatorTrimPercent = 0,
            AileronTrimPercent = 0,
            BeaconLightOn = false,
            StrobeLightOn = false,
            LandingLightOn = false,
            NavLightOn = false,
            TaxiLightOn = false,
            Timestamp = (timestamp ?? DateTimeOffset.UtcNow).ToString("O"),
            Error = message
        };

    private static bool ToBool(int value) => value != 0;

    private static double Round(double value, int decimals) =>
        Math.Round(value, decimals, MidpointRounding.AwayFromZero);
}
