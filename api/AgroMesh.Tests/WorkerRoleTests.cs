using AgroMesh.Domain.Entities;
using AgroMesh.Domain.Enums;
using AgroMesh.Domain.ValueObjects;

namespace AgroMesh.Tests;

public class WorkerRoleTests
{
    [Fact]
    public void WorkerLog_SnapshotsPayRateAndFrequencyAtCreation()
    {
        var role = WorkerRole.Create("farm1", "Farm Worker", WorkerType.Employee, 2200m, PayFrequency.Daily);
        var log = new WorkerLog("worker1", "Test Worker", WorkerType.Employee, 8.0, role.PayRate, role.PayFrequency);
        Assert.Equal(2200m, log.PayRate);
        Assert.Equal(PayFrequency.Daily, log.PayFrequency);
    }

    [Fact]
    public void UpdateRolePay_DoesNotAffectExistingWorkerLogSnapshot()
    {
        var role = WorkerRole.Create("farm1", "Farm Worker", WorkerType.Employee, 2200m, PayFrequency.Daily);
        var log = new WorkerLog("worker1", "Test Worker", WorkerType.Employee, 8.0, role.PayRate, role.PayFrequency);

        role.UpdatePay(3000m, PayFrequency.Weekly);

        Assert.Equal(2200m, log.PayRate); // snapshot is immutable
        Assert.Equal(PayFrequency.Daily, log.PayFrequency); // snapshot is immutable
        Assert.Equal(3000m, role.PayRate); // role updated
        Assert.Equal(PayFrequency.Weekly, role.PayFrequency); // role updated
    }

    [Fact]
    public void WorkerHasNoDirectPayRate_RateComesFromRole()
    {
        var role = WorkerRole.Create("farm1", "Farm Worker", WorkerType.Employee, 2200m, PayFrequency.Daily);
        var worker = Worker.Create("farm1", "Emeka", "EJ", role.Id, WorkerType.Employee);

        // Worker entity itself has no PayRate property
        var workerType = typeof(Worker);
        Assert.Null(workerType.GetProperty("PayRate"));
        Assert.Null(workerType.GetProperty("PayFrequency"));
    }

    [Theory]
    [InlineData(WorkerType.Contractor, PayFrequency.Daily)]
    [InlineData(WorkerType.Employee, PayFrequency.Monthly)]
    [InlineData(WorkerType.Employee, PayFrequency.Weekly)]
    [InlineData(WorkerType.Contractor, PayFrequency.Weekly)]
    public void Create_AcceptsAnyFrequencyRegardlessOfWorkerType(WorkerType type, PayFrequency frequency)
    {
        // All three frequencies must stay selectable for both worker types — only the
        // UI's *default* selection differs by type, the domain itself doesn't restrict it.
        var role = WorkerRole.Create("farm1", "Some Role", type, 1000m, frequency);
        Assert.Equal(frequency, role.PayFrequency);
    }
}
