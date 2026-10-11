using AgroMesh.Domain.Constants;
using AgroMesh.Domain.Enums;

namespace AgroMesh.Tests;

public class ActivityStockCategoryTests
{
    [Theory]
    [InlineData(ActivityType.Spraying, "Chemical", true)]
    [InlineData(ActivityType.Spraying, "Fertilizer", false)]
    [InlineData(ActivityType.PestControl, "Fuel", false)]
    [InlineData(ActivityType.FertilizerApplication, "Fertilizer", true)]
    [InlineData(ActivityType.FertilizerApplication, "Chemical", false)]
    [InlineData(ActivityType.Weeding, "Chemical", true)]
    [InlineData(ActivityType.Weeding, "Fertilizer", false)]
    [InlineData(ActivityType.Maintenance, "Spare Parts", true)]
    [InlineData(ActivityType.Maintenance, "Chemical", false)]
    [InlineData(ActivityType.Harvesting, "fuel", true)]
    [InlineData(ActivityType.Spraying, null, true)]
    public void Stock_category_must_fit_the_activity(ActivityType type, string? category, bool allowed) =>
        Assert.Equal(allowed, ActivityConstants.AllowsStockCategory(type, category));
}
