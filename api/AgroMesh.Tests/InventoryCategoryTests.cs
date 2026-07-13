using AgroMesh.Domain.Entities;
using AgroMesh.Domain.Exceptions;

namespace AgroMesh.Tests;

public class InventoryCategoryTests
{
    [Fact]
    public void Create_ValidName_SetsFields()
    {
        var category = InventoryCategory.Create("farm1", "Chemical");

        Assert.Equal("farm1", category.FarmId);
        Assert.Equal("Chemical", category.Name);
        Assert.False(category.IsDefault);
    }

    [Fact]
    public void Create_IsDefaultTrue_SetsIsDefault()
    {
        var category = InventoryCategory.Create("farm1", "Chemical", isDefault: true);

        Assert.True(category.IsDefault);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_BlankName_ThrowsDomainException(string? name)
    {
        Assert.Throws<DomainException>(() => InventoryCategory.Create("farm1", name!));
    }

    [Fact]
    public void Create_TrimsName()
    {
        var category = InventoryCategory.Create("farm1", "  Farm Tools  ");

        Assert.Equal("Farm Tools", category.Name);
    }

    [Fact]
    public void CreateDefaults_ReturnsEightDefaultCategories()
    {
        var defaults = InventoryCategory.CreateDefaults("farm1");

        Assert.Equal(8, defaults.Count);
        Assert.All(defaults, c => Assert.True(c.IsDefault));
        Assert.All(defaults, c => Assert.Equal("farm1", c.FarmId));

        var names = defaults.Select(c => c.Name).ToList();
        Assert.Equal(
            new[] { "Chemical", "Fertilizer", "Farm Tools", "Seed", "Fuel", "Equipment", "Spare Parts", "Other" },
            names);
    }
}
