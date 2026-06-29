using AgroMesh.Domain.Entities;
using AgroMesh.Domain.Enums;
using AgroMesh.Domain.ValueObjects;

namespace AgroMesh.Tests;

public class WorkerRoleTests
{
    [Fact]
    public void WorkerLog_SnapshotsDailyRateAtCreation()
    {
        var role = WorkerRole.Create("farm1", "Farm Worker", WorkerType.Employee, 2200m);
        var log = new WorkerLog("worker1", "Test Worker", WorkerType.Employee, 8.0, role.DailyRate);
        Assert.Equal(2200m, log.DailyRate);
    }

    [Fact]
    public void UpdateRoleRate_DoesNotAffectExistingWorkerLogSnapshot()
    {
        var role = WorkerRole.Create("farm1", "Farm Worker", WorkerType.Employee, 2200m);
        var log = new WorkerLog("worker1", "Test Worker", WorkerType.Employee, 8.0, role.DailyRate);

        role.UpdateRate(3000m);

        Assert.Equal(2200m, log.DailyRate); // snapshot is immutable
        Assert.Equal(3000m, role.DailyRate); // role updated
    }

    [Fact]
    public void WorkerHasNoDirectDailyRate_RateComesFromRole()
    {
        var role = WorkerRole.Create("farm1", "Farm Worker", WorkerType.Employee, 2200m);
        var worker = Worker.Create("farm1", "Emeka", "EJ", role.Id, WorkerType.Employee);

        // Worker entity itself has no DailyRate property
        var workerType = typeof(Worker);
        Assert.Null(workerType.GetProperty("DailyRate"));
    }
}
