using AgroMesh.Domain.Entities;
using AgroMesh.Domain.Enums;
using AgroMesh.Domain.Exceptions;
using AgroMesh.Domain.ValueObjects;

namespace AgroMesh.Tests;

public class ActivityLogTests
{
    [Fact]
    public void Create_WithValidBlockId_Succeeds()
    {
        var activity = ActivityLog.Create(
            "farm1", "block1", ActivityType.Weeding,
            DateTime.UtcNow, [], [], "supervisor1");
        Assert.Equal("block1", activity.BlockId);
    }

    [Fact]
    public void Create_WithEmptyBlockId_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => ActivityLog.Create(
            "farm1", "", ActivityType.Weeding,
            DateTime.UtcNow, [], [], "supervisor1"));
    }

    [Fact]
    public void LabourCost_CalculatedFromDailyRateWorkerLogs()
    {
        var workers = new List<WorkerLog>
        {
            new("w1", "Worker 1", WorkerType.Employee, 8.0, 2200m, PayFrequency.Daily),
            new("w2", "Worker 2", WorkerType.Employee, 8.0, 2200m, PayFrequency.Daily)
        };
        var activity = ActivityLog.Create(
            "farm1", "block1", ActivityType.Weeding,
            DateTime.UtcNow, workers, [], "supervisor1");

        // Each worker: 2200 * (8/8) = 2200 per worker, 2 workers = 4400
        Assert.Equal(4400m, activity.LabourCost);
    }

    [Fact]
    public void LabourCost_ContractorPaidDaily_ProratesByHoursWorked()
    {
        var workers = new List<WorkerLog>
        {
            new("w1", "Contractor 1", WorkerType.Contractor, 4.0, 4000m, PayFrequency.Daily)
        };
        var activity = ActivityLog.Create(
            "farm1", "block1", ActivityType.Weeding,
            DateTime.UtcNow, workers, [], "supervisor1");

        // Half a day: 4000 * (4/8) = 2000
        Assert.Equal(2000m, activity.LabourCost);
    }

    [Fact]
    public void LabourCost_SupervisorPaidMonthly_UsesDailyEquivalentNotFullSalary()
    {
        var workers = new List<WorkerLog>
        {
            // ₦91,000/month ÷ 26 work days = ₦3,500/day equivalent
            new("w1", "Supervisor 1", WorkerType.Employee, 8.0, 91000m, PayFrequency.Monthly)
        };
        var activity = ActivityLog.Create(
            "farm1", "block1", ActivityType.Weeding,
            DateTime.UtcNow, workers, [], "supervisor1");

        // A single day's activity must NOT be charged the full ₦91,000 monthly salary.
        Assert.Equal(3500m, activity.LabourCost);
    }

    [Fact]
    public void LabourCost_WeeklyPaidWorker_UsesDailyEquivalent()
    {
        var workers = new List<WorkerLog>
        {
            // ₦21,000/week ÷ 6 work days = ₦3,500/day equivalent
            new("w1", "Worker 1", WorkerType.Employee, 8.0, 21000m, PayFrequency.Weekly)
        };
        var activity = ActivityLog.Create(
            "farm1", "block1", ActivityType.Weeding,
            DateTime.UtcNow, workers, [], "supervisor1");

        Assert.Equal(3500m, activity.LabourCost);
    }

    [Fact]
    public void LabourCost_MixedFrequenciesOnSameActivity_SumsCorrectly()
    {
        var workers = new List<WorkerLog>
        {
            new("w1", "Contractor", WorkerType.Contractor, 8.0, 4000m, PayFrequency.Daily),  // 4000
            new("w2", "Supervisor",  WorkerType.Employee,   8.0, 91000m, PayFrequency.Monthly), // 3500
        };
        var activity = ActivityLog.Create(
            "farm1", "block1", ActivityType.Weeding,
            DateTime.UtcNow, workers, [], "supervisor1");

        Assert.Equal(7500m, activity.LabourCost);
    }

    [Theory]
    [InlineData(DateTimeKind.Unspecified)]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Utc)]
    public void Create_AlwaysStoresDateAsUtc(DateTimeKind kind)
    {
        // Postgres' timestamptz rejects non-UTC DateTimes; the web form sends a bare date (Unspecified).
        var activity = ActivityLog.Create(
            "farm1", "block1", ActivityType.Weeding,
            DateTime.SpecifyKind(new DateTime(2026, 10, 11), kind), [], [], "supervisor1");

        Assert.Equal(DateTimeKind.Utc, activity.Date.Kind);
    }
}
