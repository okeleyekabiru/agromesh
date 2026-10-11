using AgroMesh.Domain.Entities;
using AgroMesh.Domain.Enums;

namespace AgroMesh.Tests;

public class StockMovementTests
{
    [Fact]
    public void CreateOut_SetsCoreFields()
    {
        var movement = StockMovement.CreateOut("farm1", "item1", 15, notes: "Spraying Block A");

        Assert.Equal("farm1", movement.FarmId);
        Assert.Equal("item1", movement.InventoryItemId);
        Assert.Equal(StockMovementType.Out, movement.Type);
        Assert.Equal(15m, movement.Quantity);
        Assert.Equal("Spraying Block A", movement.Notes);
    }

    [Fact]
    public void CreateOut_SetsWorkerIdWhenProvided()
    {
        var movement = StockMovement.CreateOut("farm1", "item1", 15, notes: "Spraying Block A", workerId: "worker1");

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

    [Fact]
    public void CreateIn_WithOrderId_SetsOrderId()
    {
        var movement = StockMovement.CreateIn("farm1", "item1", 15, orderId: "order1", notes: "Received from order1");

        Assert.Equal("order1", movement.OrderId);
        Assert.Equal(StockMovementType.In, movement.Type);
    }

    [Fact]
    public void CreateIn_WithoutOrderId_LeavesOrderIdNull()
    {
        // Regression: the existing RecordInventoryReceiptCommandHandler call site never supplies
        // an orderId — must keep working unchanged.
        var movement = StockMovement.CreateIn("farm1", "item1", 15, transactionId: "txn1", notes: "Receipt");

        Assert.Null(movement.OrderId);
        Assert.Equal("txn1", movement.TransactionId);
    }

    [Fact]
    public void CreateOut_WithBlockAndPerformedByUserId_SetsBoth()
    {
        var movement = StockMovement.CreateOut(
            "farm1", "item1", 15, notes: "Spraying Block A",
            blockId: "sec1", performedByUserId: "user1");

        Assert.Equal("sec1", movement.BlockId);
        Assert.Equal("user1", movement.PerformedByUserId);
    }

    [Fact]
    public void CreateOut_WithoutBlockOrPerformedByUserId_LeavesBothNull()
    {
        // Regression: the existing activity-log auto-deduction call site (InventoryDeductionService)
        // supplies none of these — must keep working unchanged.
        var movement = StockMovement.CreateOut("farm1", "item1", 15, activityLogId: "act1", notes: "Activity usage: 15 L");

        Assert.Null(movement.BlockId);
        Assert.Null(movement.PerformedByUserId);
    }
}
