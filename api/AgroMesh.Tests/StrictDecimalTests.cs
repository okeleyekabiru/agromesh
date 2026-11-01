using AgroMesh.Application.Services;

namespace AgroMesh.Tests;

public class StrictDecimalTests
{
    [Theory]
    [InlineData("12", 12)]
    [InlineData("5000.50", 5000.50)]
    [InlineData("0.5", 0.5)]
    [InlineData("0", 0)]
    [InlineData("007", 7)]
    [InlineData("999999999999999", 999999999999999)]
    public void Accepts_DigitsWithAnOptionalDotFraction(string text, double expected)
    {
        Assert.True(StrictDecimal.TryParse(text, out var value));
        Assert.Equal((decimal)expected, value);
    }

    [Theory]
    [InlineData("1,5")]
    [InlineData("1,2,3")]
    [InlineData("12,")]
    [InlineData("1,000.50")]
    [InlineData("1e3")]
    [InlineData("1E3")]
    [InlineData(" 12 ")]
    [InlineData("12 ")]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    [InlineData("-5")]
    [InlineData("+5")]
    [InlineData(".5")]
    [InlineData("5.")]
    [InlineData("1.2.3")]
    [InlineData("1 000")]
    [InlineData("abc")]
    [InlineData("NaN")]
    [InlineData("١٢")]            // Arabic-Indic digits
    [InlineData("1234567890123456")]        // 16 digits
    [InlineData("1.1234567")]               // 7 decimals
    [InlineData("12\n")]
    public void Rejects_EverythingElse(string? text)
    {
        Assert.False(StrictDecimal.TryParse(text, out _));
    }
}
