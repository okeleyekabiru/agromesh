using Ilevora.Application.Interfaces;
using Ilevora.Application.Services;
using Ilevora.Domain.Entities;

namespace Ilevora.Tests;

public class InvoiceThemeResolverTests
{
    private static readonly InvoiceTemplateInfo Template = new("t", "T", "d", "#111111", "#222222");

    private static Farm FarmWith(string? primary, string? accent)
    {
        var farm = Farm.Create("F", "co", "o", id: "f");
        farm.SetBusinessProfile(null, null, null, null, null, null, null, primary, accent);
        return farm;
    }

    [Fact]
    public void NothingChosen_UsesTheTemplateDefaults()
    {
        var r = InvoiceThemeResolver.Resolve(null, null, FarmWith(null, null), Template);
        Assert.Equal(new InvoiceTheme("#111111", "#222222"), r.Theme);
        Assert.Equal("default", r.PrimarySource);
        Assert.Equal("default", r.AccentSource);
    }

    [Fact]
    public void FarmBrand_BeatsTheTemplate()
    {
        var r = InvoiceThemeResolver.Resolve(null, null, FarmWith("#1B2A6B", "#E67E22"), Template);
        Assert.Equal(new InvoiceTheme("#1B2A6B", "#E67E22"), r.Theme);
        Assert.Equal("farm", r.PrimarySource);
        Assert.Equal("farm", r.AccentSource);
    }

    [Fact]
    public void InvoiceColour_BeatsTheFarmBrand()
    {
        var r = InvoiceThemeResolver.Resolve("#C0392B", "#8E44AD", FarmWith("#1B2A6B", "#E67E22"), Template);
        Assert.Equal(new InvoiceTheme("#C0392B", "#8E44AD"), r.Theme);
        Assert.Equal("invoice", r.PrimarySource);
        Assert.Equal("invoice", r.AccentSource);
    }

    [Fact]
    public void EachColourIsResolvedOnItsOwn()
    {
        var r = InvoiceThemeResolver.Resolve("#C0392B", null, FarmWith(null, "#E67E22"), Template);
        Assert.Equal(new InvoiceTheme("#C0392B", "#E67E22"), r.Theme);
        Assert.Equal("invoice", r.PrimarySource);
        Assert.Equal("farm", r.AccentSource);

        var r2 = InvoiceThemeResolver.Resolve(null, "#8E44AD", FarmWith("#1B2A6B", null), Template);
        Assert.Equal(new InvoiceTheme("#1B2A6B", "#8E44AD"), r2.Theme);
        Assert.Equal(("farm", "invoice"), (r2.PrimarySource, r2.AccentSource));

        var r3 = InvoiceThemeResolver.Resolve(null, null, FarmWith("#1B2A6B", null), Template);
        Assert.Equal(("farm", "default"), (r3.PrimarySource, r3.AccentSource));
        Assert.Equal("#222222", r3.Theme.Accent);
    }

    [Fact]
    public void TemplateGone_FallsBackToTheNgfColours()
    {
        var r = InvoiceThemeResolver.Resolve(null, null, FarmWith(null, null), null);
        Assert.Equal(new InvoiceTheme("#138A44", "#F5C90C"), r.Theme);
    }
}
