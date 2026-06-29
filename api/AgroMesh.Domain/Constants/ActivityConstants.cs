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

    public static readonly IReadOnlySet<ActivityType> ChemicalActivityTypes = new HashSet<ActivityType>
    {
        ActivityType.Spraying,
        ActivityType.PestControl
    };
}
