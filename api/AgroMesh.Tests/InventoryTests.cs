using AgroMesh.Domain.Entities;
using AgroMesh.Domain.Exceptions;

namespace AgroMesh.Tests;

public class InventoryTests
{
    private static InventoryItem CreateItem(decimal stock = 100) =>
        InventoryItem.Create("farm1", "Glyphosate 360", "L", "cat_chemical", stock, 30);

    [Fact]
    public void DeductStock_SufficientQuantity_UpdatesCurrentStock()
    {
        var item = CreateItem(100);
        item.DeductStock(15);
        Assert.Equal(85m, item.CurrentStock);
    }

    [Fact]
    public void DeductStock_InsufficientQuantity_ThrowsDomainException()
    {
        var item = CreateItem(10);
        Assert.Throws<DomainException>(() => item.DeductStock(20));
    }

    [Fact]
    public void AddStock_ValidQuantity_IncreasesCurrentStock()
    {
        var item = CreateItem(50);
        item.AddStock(100);
        Assert.Equal(150m, item.CurrentStock);
    }

    [Fact]
    public void IsLowStock_CurrentStockBelowThreshold_ReturnsTrue()
    {
        var item = CreateItem(25); // threshold is 30
        Assert.True(item.IsLowStock);
    }

    [Fact]
    public void IsLowStock_CurrentStockAboveThreshold_ReturnsFalse()
    {
        var item = CreateItem(50); // threshold is 30
        Assert.False(item.IsLowStock);
    }

    [Fact]
    public void Create_SetsCategoryIdFromCategoryIdParameter()
    {
        var item = CreateItem();
        Assert.Equal("cat_chemical", item.CategoryId);
    }

    // Regression: a reported "typed 1000, stored 999.980" data-integrity bug. Root-caused to a
    // browser-layer footgun (mouse wheel over a focused number input silently adjusting its value
    // by `step`, fixed via @wheel.prevent on every number input — see InventoryView.vue's doc
    // comment), not the domain layer — this test proves AddStock/Create themselves never introduce
    // any floating-point drift for a whole-number opening stock, confirming the domain layer was
    // never the source of the corruption.
    [Fact]
    public void Create_WholeNumberOpeningStock_IsStoredExactly()
    {
        var item = InventoryItem.Create("farm1", "Dragon", "kg", "cat_chemical", 1000m, 30);
        Assert.Equal(1000m, item.CurrentStock);
    }

    [Fact]
    public void AddStock_WholeNumberQuantity_IsAddedExactly()
    {
        var item = CreateItem(0);
        item.AddStock(1000m);
        Assert.Equal(1000m, item.CurrentStock);
    }
}
