using AgroMesh.Domain.Entities;
using AgroMesh.Domain.Enums;
using AgroMesh.Domain.Exceptions;
using AgroMesh.Domain.ValueObjects;

namespace AgroMesh.Tests;

public class SectionCropTests
{
    [Fact]
    public void Create_CropsPercentageSumsTo100_Succeeds()
    {
        var crops = new List<SectionCrop>
        {
            new(CropType.OilPalm, 5.0, 50),
            new(CropType.Maize, 5.0, 50)
        };
        var section = Section.Create("farm1", "Section D", crops, 10.0);
        Assert.Equal(2, section.Crops.Count);
    }

    [Fact]
    public void Create_CropsPercentageDoesNotSumTo100_ThrowsDomainException()
    {
        var crops = new List<SectionCrop>
        {
            new(CropType.OilPalm, 5.0, 60),
            new(CropType.Maize, 4.0, 30)
        };
        Assert.Throws<DomainException>(() => Section.Create("farm1", "Bad Section", crops, 9.0));
    }
}
