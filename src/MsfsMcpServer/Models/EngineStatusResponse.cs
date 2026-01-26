using System.Text.Json.Serialization;

namespace MsfsMcpServer.Models;

/// <summary>
/// Response contract for engine status data.
/// </summary>
public sealed class EngineStatusResponse
{
    [JsonPropertyName("engine_count")]
    public int EngineCount { get; init; }

    [JsonPropertyName("engine1_rpm")]
    public double EngineOneRpm { get; init; }

    [JsonPropertyName("engine1_throttle_pct")]
    public double EngineOneThrottlePercent { get; init; }

    [JsonPropertyName("fuel_flow_gph")]
    public double FuelFlowGallonsPerHour { get; init; }

    [JsonPropertyName("fuel_total_gal")]
    public double FuelTotalGallons { get; init; }

    [JsonPropertyName("egt_celsius")]
    public double ExhaustGasTemperatureCelsius { get; init; }

    [JsonPropertyName("oil_pressure_psi")]
    public double OilPressurePsi { get; init; }

    [JsonPropertyName("oil_temp_celsius")]
    public double OilTemperatureCelsius { get; init; }

    [JsonPropertyName("timestamp")]
    public string Timestamp { get; init; } = DateTimeOffset.UtcNow.ToString("O");

    [JsonPropertyName("error")]
    public string? Error { get; init; }

    /// <summary>
    /// Builds a response from raw SimConnect data.
    /// </summary>
    public static EngineStatusResponse FromData(EngineStatusData data, DateTimeOffset? timestamp = null) =>
        new()
        {
            EngineCount = data.EngineCount,
            EngineOneRpm = Round(data.EngineOneRpm, 0),
            EngineOneThrottlePercent = Round(data.EngineOneThrottlePercent, 1),
            FuelFlowGallonsPerHour = Round(data.FuelFlowGph, 1),
            FuelTotalGallons = Round(data.FuelTotalGallons, 1),
            ExhaustGasTemperatureCelsius = Round(data.ExhaustGasTemperatureCelsius, 0),
            OilPressurePsi = Round(data.OilPressurePsi, 1),
            OilTemperatureCelsius = Round(data.OilTemperatureCelsius, 0),
            Timestamp = (timestamp ?? DateTimeOffset.UtcNow).ToString("O"),
            Error = null
        };

    /// <summary>
    /// Builds an error response with the provided message.
    /// </summary>
    public static EngineStatusResponse ErrorResponse(string message, DateTimeOffset? timestamp = null) =>
        new()
        {
            EngineCount = 0,
            EngineOneRpm = 0,
            EngineOneThrottlePercent = 0,
            FuelFlowGallonsPerHour = 0,
            FuelTotalGallons = 0,
            ExhaustGasTemperatureCelsius = 0,
            OilPressurePsi = 0,
            OilTemperatureCelsius = 0,
            Timestamp = (timestamp ?? DateTimeOffset.UtcNow).ToString("O"),
            Error = message
        };

    private static double Round(double value, int decimals) =>
        Math.Round(value, decimals, MidpointRounding.AwayFromZero);
}
