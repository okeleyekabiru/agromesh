using Ilevora.Domain.Crops;
using Ilevora.Domain.Entities;
using Ilevora.Domain.Enums;
using Ilevora.Domain.Exceptions;
using Ilevora.Domain.ValueObjects;

namespace Ilevora.Tests;

public class BlockCropTests
{
    [Fact]
    public void Create_CropsPercentageSumsTo100_Succeeds()
    {
        var crops = new List<BlockCrop>
        {
            new(BuiltInCrop.OilPalm, 5.0, 50),
            new(BuiltInCrop.Maize, 5.0, 50)
        };
        var block = Block.Create("farm1", "Block D", crops, 10.0);
        Assert.Equal(2, block.Crops.Count);
    }

    [Fact]
    public void Create_CropsPercentageDoesNotSumTo100_ThrowsDomainException()
    {
        var crops = new List<BlockCrop>
        {
            new(BuiltInCrop.OilPalm, 5.0, 60),
            new(BuiltInCrop.Maize, 4.0, 30)
        };
        Assert.Throws<DomainException>(() => Block.Create("farm1", "Bad Block", crops, 9.0));
    }
}
