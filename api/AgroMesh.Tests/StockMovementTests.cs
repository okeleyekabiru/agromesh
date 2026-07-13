using AgroMesh.Domain.Entities;
using AgroMesh.Domain.Enums;

namespace AgroMesh.Tests;

public class StockMovementTests
{
    [Fact]
    public void CreateOut_SetsCoreFields()
    {
        var movement = StockMovement.CreateOut("farm1", "item1", 15, notes: "Spraying Section A");

        Assert.Equal("farm1", movement.FarmId);
        Assert.Equal("item1", movement.InventoryItemId);
        Assert.Equal(StockMovementType.Out, movement.Type);
        Assert.Equal(15m, movement.Quantity);
        Assert.Equal("Spraying Section A", movement.Notes);
    }

    [Fact]
    public void CreateOut_SetsWorkerIdWhenProvided()
    {
        var movement = StockMovement.CreateOut("farm1", "item1", 15, notes: "Spraying Section A", workerId: "worker1");

        Assert.Equal("worker1", movement.WorkerId);
    }

    [Fact]
    public void CreateOut_WorkerIdDefaultsToNullWhenOmitted()
    {
        // Regression: the existing activity-log auto-deduction call site (InventoryDeductionService)
        // never supplies a workerId — must keep working unchanged.
        var movement = StockMovement.CreateOut("farm1", "item1", 15, activityLogId: "act1", notes: "Activity usage: 15 L");

        Assert.Null(movement.WorkerId);
        Assert.Equal("act1", movement.ActivityLogId);
    }
}
