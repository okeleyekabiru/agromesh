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
            "farm1", "section1", "block1", ActivityType.Weeding,
            DateTime.UtcNow, [], [], "supervisor1");
        Assert.Equal("block1", activity.BlockId);
    }

    [Fact]
    public void Create_WithEmptyBlockId_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => ActivityLog.Create(
            "farm1", "section1", "", ActivityType.Weeding,
            DateTime.UtcNow, [], [], "supervisor1"));
    }

    [Fact]
    public void LabourCost_CalculatedFromWorkerLogs()
    {
        var workers = new List<WorkerLog>
        {
            new("w1", "Worker 1", WorkerType.Employee, 8.0, 2200m),
            new("w2", "Worker 2", WorkerType.Employee, 8.0, 2200m)
        };
        var activity = ActivityLog.Create(
            "farm1", "section1", "block1", ActivityType.Weeding,
            DateTime.UtcNow, workers, [], "supervisor1");

        // Each worker: 2200 * (8/8) = 2200 per worker, 2 workers = 4400
        Assert.Equal(4400m, activity.LabourCost);
    }
}
