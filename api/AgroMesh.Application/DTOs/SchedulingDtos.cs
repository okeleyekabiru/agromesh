using AgroMesh.Domain.Enums;

namespace AgroMesh.Application.DTOs;

public record UpcomingTaskDto(
    string BlockId,
    string BlockName,
    ActivityType ActivityType,
    DateTime? LastDone,
    DateTime PredictedNextDue,
    int DaysUntilDue,
    string Reasoning,
    string? RecommendedChemical,
    DateTime? RecommendedSprayDate,
    ConfidenceLevel Confidence,
    decimal? StockAvailable,
    string? StockUnit,
    bool IsStale,
    bool WeatherDataUsed,
    string? Status = null,
    List<string>? Factors = null,
    bool AiReviewed = false);

public record WeatherWidgetDto(
    double Latitude,
    double Longitude,
    string CurrentSeason,
    double TodayPrecipitationMm,
    double TodayTempMaxC,
    DateTime? NextDryWindowStart,
    DateTime? NextDryWindowEnd,
    double RainfallLast30Days,
    string? Location = null,
    List<DailyForecastDto>? Forecast = null);

/// <summary>One day of the forecast. <see cref="GoodSprayDay"/> = dry, calm and unlikely to rain.</summary>
public record DailyForecastDto(
    DateTime Date,
    double PrecipitationMm,
    int RainChance,
    double TempMaxC,
    double TempMinC,
    double WindKmh,
    int WeatherCode,
    bool GoodSprayDay);
