using AgroMesh.Domain.Exceptions;
using AgroMesh.Domain.Utilities;

namespace AgroMesh.Tests;

public class BrandColorTests
{
    [Theory]
    [InlineData("#138a44", "#138A44")]
    [InlineData("#F5C90C", "#F5C90C")]
    [InlineData("  #f5c90c  ", "#F5C90C")]
    [InlineData("#000000", "#000000")]
    public void Normalise_UpperCasesAValidHex(string input, string expected)
    {
        Assert.Equal(expected, BrandColor.Normalise(input, "Primary colour"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Normalise_BlankMeansNoColour(string? input)
    {
        Assert.Null(BrandColor.Normalise(input, "Primary colour"));
    }

    [Theory]
    [InlineData("138A44")]            // no hash
    [InlineData("#138A4")]            // short
    [InlineData("#138A444")]          // long
    [InlineData("#FFF")]              // shorthand
    [InlineData("#138A44FF")]         // alpha
    [InlineData("#GGGGGG")]
    [InlineData("green")]
    [InlineData("rgb(19,138,68)")]
    [InlineData("hsl(140,70%,30%)")]
    [InlineData("#13 A44")]
    [InlineData("#138A44 x")]
    public void Normalise_RejectsAnythingButSixDigitHex(string input)
    {
        var ex = Assert.Throws<DomainException>(() => BrandColor.Normalise(input, "Primary colour"));
        Assert.Contains("Primary colour", ex.Message);
        Assert.Contains("#RRGGBB", ex.Message);
    }

    [Theory]
    [InlineData("#FFFFFF")]
    [InlineData("#FAFAFA")]
    [InlineData("#FFF8DC")]
    public void Normalise_RejectsColoursTooLightToShowOnWhitePaper(string input)
    {
        var ex = Assert.Throws<DomainException>(() => BrandColor.Normalise(input, "Accent colour"));
        Assert.Contains("too light", ex.Message);
    }

    [Fact]
    public void TheDefaultNgfColours_AreAccepted()
    {
        Assert.Equal("#138A44", BrandColor.Normalise("#138A44", "x"));
        Assert.Equal("#F5C90C", BrandColor.Normalise("#F5C90C", "x"));
    }

    [Fact]
    public void Luminance_OfBlackAndWhite()
    {
        Assert.Equal(0.0, BrandColor.Luminance("#000000"), 3);
        Assert.Equal(1.0, BrandColor.Luminance("#FFFFFF"), 3);
    }

    [Fact]
    public void Contrast_BlackOnWhite_Is21()
    {
        Assert.Equal(21.0, BrandColor.Contrast("#000000", "#FFFFFF"), 1);
        Assert.Equal(1.0, BrandColor.Contrast("#777777", "#777777"), 3);
    }

    [Theory]
    [InlineData("#138A44", "#FFFFFF")]   // the NGF green carries white text
    [InlineData("#F5C90C", "#1F2A24")]   // the NGF yellow carries dark text
    [InlineData("#000000", "#FFFFFF")]
    [InlineData("#FFE066", "#1F2A24")]
    [InlineData("#1B2A6B", "#FFFFFF")]
    [InlineData("#C0392B", "#FFFFFF")]
    public void TextOn_PicksWhiteOrInkByContrast(string fill, string expected)
    {
        Assert.Equal(expected, BrandColor.TextOn(fill));
    }

    [Theory]
    [InlineData("#138A44")]
    [InlineData("#1B2A6B")]
    public void ForText_LeavesColoursThatAreAlreadyDarkEnoughAlone(string colour)
    {
        Assert.Equal(colour, BrandColor.ForText(colour));
    }

    [Theory]
    [InlineData("#F5C90C")]
    [InlineData("#FFE066")]
    [InlineData("#9FD8E8")]
    public void ForText_DarkensPaleColoursUntilTheyReachThreeToOneOnWhite(string colour)
    {
        var adjusted = BrandColor.ForText(colour);
        Assert.NotEqual(colour, adjusted);
        Assert.True(BrandColor.Contrast(adjusted, "#FFFFFF") >= 3.0);
        Assert.Matches("^#[0-9A-F]{6}$", adjusted);
    }

    [Fact]
    public void Tint_MixesTowardsWhite()
    {
        Assert.Equal("#FFFFFF", BrandColor.Tint("#138A44", 1.0));
        Assert.Equal("#138A44", BrandColor.Tint("#138A44", 0.0));
        Assert.NotEqual("#138A44", BrandColor.Tint("#138A44", 0.9));
    }
}
