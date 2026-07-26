using AgroMesh.Domain.Entities;
using AgroMesh.Domain.Enums;
using AgroMesh.Domain.Exceptions;
using AgroMesh.Domain.ValueObjects;

namespace AgroMesh.Tests;

public class OrderTests
{
    private static List<OrderLineItem> ValidLineItems() =>
    [
        new OrderLineItem("item-1", 10m, 500m),
        new OrderLineItem("item-2", 5m, 1200m)
    ];

    [Fact]
    public void Create_ValidLineItems_SetsStatusDraft()
    {
        var order = Order.Create("farm-1", "sup-1", ValidLineItems());

        Assert.Equal(OrderStatus.Draft, order.Status);
        Assert.Equal("farm-1", order.FarmId);
        Assert.Equal("sup-1", order.SupplierId);
        Assert.Equal(2, order.LineItems.Count);
    }

    [Fact]
    public void Create_EmptyLineItems_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => Order.Create("farm-1", "sup-1", []));
    }

    [Fact]
    public void Create_LineItemWithZeroQuantity_ThrowsDomainException()
    {
        var lineItems = new List<OrderLineItem> { new("item-1", 0m, 500m) };
        Assert.Throws<DomainException>(() => Order.Create("farm-1", "sup-1", lineItems));
    }

    [Fact]
    public void Create_LineItemWithNegativeQuantity_ThrowsDomainException()
    {
        var lineItems = new List<OrderLineItem> { new("item-1", -1m, 500m) };
        Assert.Throws<DomainException>(() => Order.Create("farm-1", "sup-1", lineItems));
    }

    [Fact]
    public void Create_LineItemWithNegativeUnitPrice_ThrowsDomainException()
    {
        var lineItems = new List<OrderLineItem> { new("item-1", 10m, -1m) };
        Assert.Throws<DomainException>(() => Order.Create("farm-1", "sup-1", lineItems));
    }

    [Fact]
    public void Create_LineItemWithZeroUnitPrice_IsAllowed()
    {
        var lineItems = new List<OrderLineItem> { new("item-1", 10m, 0m) };
        var order = Order.Create("farm-1", "sup-1", lineItems);
        Assert.Equal(0m, order.Total);
    }

    [Fact]
    public void Total_SumsQuantityTimesUnitPriceAcrossLines()
    {
        var order = Order.Create("farm-1", "sup-1", ValidLineItems());

        // (10 * 500) + (5 * 1200) = 5000 + 6000 = 11000
        Assert.Equal(11000m, order.Total);
    }

    [Fact]
    public void Place_FromDraft_SetsPlaced()
    {
        var order = Order.Create("farm-1", "sup-1", ValidLineItems());
        order.Place();
        Assert.Equal(OrderStatus.Placed, order.Status);
    }

    [Fact]
    public void Place_FromPlaced_ThrowsDomainException_StatusUnchanged()
    {
        var order = Order.Create("farm-1", "sup-1", ValidLineItems());
        order.Place();

        Assert.Throws<DomainException>(() => order.Place());
        Assert.Equal(OrderStatus.Placed, order.Status);
    }

    [Fact]
    public void Place_FromReceived_ThrowsDomainException()
    {
        var order = Order.Create("farm-1", "sup-1", ValidLineItems());
        order.Place();
        order.Receive();

        Assert.Throws<DomainException>(() => order.Place());
        Assert.Equal(OrderStatus.Received, order.Status);
    }

    [Fact]
    public void Receive_FromPlaced_SetsReceived()
    {
        var order = Order.Create("farm-1", "sup-1", ValidLineItems());
        order.Place();
        order.Receive();
        Assert.Equal(OrderStatus.Received, order.Status);
    }

    [Fact]
    public void Receive_FromDraft_ThrowsDomainException_StatusUnchanged()
    {
        var order = Order.Create("farm-1", "sup-1", ValidLineItems());

        Assert.Throws<DomainException>(() => order.Receive());
        Assert.Equal(OrderStatus.Draft, order.Status);
    }

    [Fact]
    public void Receive_FromReceived_ThrowsDomainException()
    {
        var order = Order.Create("farm-1", "sup-1", ValidLineItems());
        order.Place();
        order.Receive();

        Assert.Throws<DomainException>(() => order.Receive());
        Assert.Equal(OrderStatus.Received, order.Status);
    }
}
