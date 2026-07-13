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
}
