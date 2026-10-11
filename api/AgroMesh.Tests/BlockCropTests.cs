using AgroMesh.Domain.Entities;
using AgroMesh.Domain.Enums;
using AgroMesh.Domain.Exceptions;
using AgroMesh.Domain.ValueObjects;

namespace AgroMesh.Tests;

public class BlockCropTests
{
    [Fact]
    public void Create_CropsPercentageSumsTo100_Succeeds()
    {
        var crops = new List<BlockCrop>
        {
            new(CropType.OilPalm, 5.0, 50),
            new(CropType.Maize, 5.0, 50)
        };
        var block = Block.Create("farm1", "Block D", crops, 10.0);
        Assert.Equal(2, block.Crops.Count);
    }

    [Fact]
    public void Create_CropsPercentageDoesNotSumTo100_ThrowsDomainException()
    {
        var crops = new List<BlockCrop>
        {
            new(CropType.OilPalm, 5.0, 60),
            new(CropType.Maize, 4.0, 30)
        };
        Assert.Throws<DomainException>(() => Block.Create("farm1", "Bad Block", crops, 9.0));
    }
}
