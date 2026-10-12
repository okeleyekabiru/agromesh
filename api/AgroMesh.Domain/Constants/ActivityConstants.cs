using AgroMesh.Domain.Enums;

namespace AgroMesh.Domain.Constants;

public static class ActivityConstants
{
    public static readonly IReadOnlySet<ActivityType> SpatialActivityTypes = new HashSet<ActivityType>
    {
        ActivityType.Weeding,
        ActivityType.Spraying,
        ActivityType.FertilizerApplication,
        ActivityType.Pruning,
        ActivityType.PestControl,
        ActivityType.Clearing,
        ActivityType.Harvesting
    };

    /// <summary>
    /// Which stock can be used in which activity: chemicals only for spraying / pest control, fertilizer
    /// only for fertilizer application (and weeding may use chemicals but not fertilizer). Everything else
    /// (fuel, tools, spare parts, custom categories) is free for the remaining activities.
    /// </summary>
    public static bool AllowsStockCategory(ActivityType type, string? categoryName)
    {
        if (string.IsNullOrWhiteSpace(categoryName)) return true;
        var cat = categoryName.Trim().ToLowerInvariant();
        var chemical = cat == "chemical";
        var fertilizer = cat == "fertilizer";
        return type switch
        {
            ActivityType.Spraying or ActivityType.PestControl => chemical,
            ActivityType.FertilizerApplication => fertilizer,
            ActivityType.Weeding => !fertilizer,
            _ => !chemical && !fertilizer,
        };
    }

    /// <summary>
    /// Work that covers part of a block at a time: the manager marks the area that was worked (8+ points), so the
    /// block can show what is done and what is left.
    /// </summary>
    public static readonly IReadOnlySet<ActivityType> RequiresWorkedArea = new HashSet<ActivityType>
    {
        ActivityType.Weeding,
        ActivityType.Spraying,
        ActivityType.FertilizerApplication,
        ActivityType.Harvesting
    };

    public static readonly IReadOnlySet<ActivityType> ChemicalActivityTypes = new HashSet<ActivityType>
    {
        ActivityType.Spraying,
        ActivityType.PestControl
    };
}
