using AgroMesh.Domain.Entities;
using AgroMesh.Domain.Enums;
using AgroMesh.Domain.Exceptions;

namespace AgroMesh.Tests;

public class StockAdjustmentRequestTests
{
    private static StockAdjustmentRequest CreateRequest(decimal quantity = 50, string notes = "Physical recount") =>
        StockAdjustmentRequest.Create("farm1", "item1", quantity, notes, "user1");

    [Fact]
    public void Create_SetsStatusToPending()
    {
        var request = CreateRequest();
        Assert.Equal(StockAdjustmentStatus.Pending, request.Status);
    }

    [Fact]
    public void Create_SetsCoreFields()
    {
        var request = CreateRequest(75, "Spillage during application");
        Assert.Equal("farm1", request.FarmId);
        Assert.Equal("item1", request.InventoryItemId);
        Assert.Equal(75m, request.RequestedQuantity);
        Assert.Equal("Spillage during application", request.Notes);
        Assert.Equal("user1", request.RequestedByUserId);
        Assert.Null(request.ReviewedByUserId);
        Assert.Null(request.ReviewedAt);
        Assert.Null(request.ReviewNotes);
    }

    [Fact]
    public void Create_BlankNotes_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => StockAdjustmentRequest.Create("farm1", "item1", 50, "   ", "user1"));
    }

    [Fact]
    public void Create_NegativeRequestedQuantity_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => CreateRequest(-5));
    }

    [Fact]
    public void Approve_SetsStatusAndReviewFields()
    {
        var request = CreateRequest();
        request.Approve("admin1", "Confirmed by physical count");

        Assert.Equal(StockAdjustmentStatus.Approved, request.Status);
        Assert.Equal("admin1", request.ReviewedByUserId);
        Assert.Equal("Confirmed by physical count", request.ReviewNotes);
        Assert.NotNull(request.ReviewedAt);
    }

    [Fact]
    public void Approve_AlreadyApproved_ThrowsDomainException()
    {
        var request = CreateRequest();
        request.Approve("admin1");
        Assert.Throws<DomainException>(() => request.Approve("admin1"));
    }

    [Fact]
    public void Approve_AlreadyRejected_ThrowsDomainException()
    {
        var request = CreateRequest();
        request.Reject("admin1");
        Assert.Throws<DomainException>(() => request.Approve("admin1"));
    }

    [Fact]
    public void Reject_SetsStatusAndReviewFields()
    {
        var request = CreateRequest();
        request.Reject("admin1", "Numbers don't add up");

        Assert.Equal(StockAdjustmentStatus.Rejected, request.Status);
        Assert.Equal("admin1", request.ReviewedByUserId);
        Assert.Equal("Numbers don't add up", request.ReviewNotes);
        Assert.NotNull(request.ReviewedAt);
    }

    [Fact]
    public void Reject_AlreadyReviewed_ThrowsDomainException()
    {
        var request = CreateRequest();
        request.Approve("admin1");
        Assert.Throws<DomainException>(() => request.Reject("admin1"));
    }
}
