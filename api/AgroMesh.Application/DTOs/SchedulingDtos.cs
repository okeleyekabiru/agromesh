using AgroMesh.Domain.Enums;

namespace AgroMesh.Application.DTOs;

public record UpcomingTaskDto(
    string SectionId,
    string SectionName,
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
    bool WeatherDataUsed);

public record WeatherWidgetDto(
    double Latitude,
    double Longitude,
    string CurrentSeason,
    double TodayPrecipitationMm,
    double TodayTempMaxC,
    DateTime? NextDryWindowStart,
    DateTime? NextDryWindowEnd,
    double RainfallLast30Days);
