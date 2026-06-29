using AgroMesh.Domain.Enums;

namespace AgroMesh.Domain.Entities;

public class SchedulingPrediction : BaseEntity
{
    public string FarmId { get; private set; } = string.Empty;
    public string SectionId { get; private set; } = string.Empty;
    public ActivityType ActivityType { get; private set; }
    public DateTime? LastActivityDate { get; private set; }
    public DateTime PredictedNextDue { get; private set; }
    public string Reasoning { get; private set; } = string.Empty;
    public string? RecommendedChemical { get; private set; }
    public DateTime? RecommendedSprayDate { get; private set; }
    public ConfidenceLevel Confidence { get; private set; }
    public DateTime ComputedAt { get; private set; }
    public bool WeatherDataUsed { get; private set; }

    public bool IsStale => (DateTime.UtcNow - ComputedAt).TotalHours > 48;

    private SchedulingPrediction() { }

    public static SchedulingPrediction Create(
        string farmId,
        string sectionId,
        ActivityType activityType,
        DateTime predictedNextDue,
        string reasoning,
        ConfidenceLevel confidence,
        bool weatherDataUsed,
        DateTime? lastActivityDate = null,
        string? recommendedChemical = null,
        DateTime? recommendedSprayDate = null)
    {
        return new SchedulingPrediction
        {
            FarmId = farmId,
            SectionId = sectionId,
            ActivityType = activityType,
            PredictedNextDue = predictedNextDue,
            Reasoning = reasoning,
            Confidence = confidence,
            WeatherDataUsed = weatherDataUsed,
            LastActivityDate = lastActivityDate,
            RecommendedChemical = recommendedChemical,
            RecommendedSprayDate = recommendedSprayDate,
            ComputedAt = DateTime.UtcNow
        };
    }

    public void Refresh(
        DateTime predictedNextDue,
        string reasoning,
        ConfidenceLevel confidence,
        bool weatherDataUsed,
        DateTime? lastActivityDate = null,
        string? recommendedChemical = null,
        DateTime? recommendedSprayDate = null)
    {
        PredictedNextDue = predictedNextDue;
        Reasoning = reasoning;
        Confidence = confidence;
        WeatherDataUsed = weatherDataUsed;
        LastActivityDate = lastActivityDate;
        RecommendedChemical = recommendedChemical;
        RecommendedSprayDate = recommendedSprayDate;
        ComputedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}
