using System.Text;
using System.Text.RegularExpressions;
using Ilevora.Application.Interfaces;
using Ilevora.Domain.Entities;
using Ilevora.Domain.Utilities;
using Ilevora.Domain.ValueObjects;
using Ilevora.Infrastructure;
using Ilevora.Infrastructure.Services.Invoices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QuestPDF.Infrastructure;

namespace Ilevora.Tests;

/// <summary>The ten standard templates: the catalogue itself, and every template rendered against the awkward inputs.</summary>
public class InvoiceTemplateCatalogTests
{
    static InvoiceTemplateCatalogTests()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    internal static readonly string[] ExpectedKeys = ["ngf", "band", "sidebar", "minimal", "callout", "cards", "ledger", "angle", "anchor", "duo"];

    private static IInvoiceTemplateRegistry RealRegistry()
    {
        var services = new ServiceCollection();
        services.AddInfrastructure(new ConfigurationBuilder().Build());
        return services.BuildServiceProvider().GetRequiredService<IInvoiceTemplateRegistry>();
    }

    // ---- the catalogue --------------------------------------------------------------------------------------------

    [Fact]
    public void TheApp_RegistersExactlyTheTenStandardTemplates()
    {
        var registry = RealRegistry();
        Assert.Equal(10, registry.Templates.Count);
        Assert.Equal(ExpectedKeys.OrderBy(k => k), registry.Templates.Select(t => t.Key).OrderBy(k => k));
    }

    [Fact]
    public void Templates_AreInAStableOrder_NgfFirstThenByName()
    {
        var first = RealRegistry().Templates.Select(t => t.Key).ToList();
        var second = RealRegistry().Templates.Select(t => t.Key).ToList();
        Assert.Equal(first, second);
        Assert.Equal("ngf", first[0]);
        var rest = RealRegistry().Templates.Skip(1).Select(t => t.Name).ToList();
        Assert.Equal(rest.OrderBy(n => n, StringComparer.OrdinalIgnoreCase), rest);
    }

    [Fact]
    public void EveryTemplate_HasAUniqueLowercaseKey_AName_AndADescription()
    {
        var all = RealRegistry().Templates;
        Assert.Equal(all.Count, all.Select(t => t.Key).Distinct().Count());
        Assert.Equal(all.Count, all.Select(t => t.Name).Distinct().Count());
        foreach (var t in all)
        {
            Assert.Matches("^[a-z]{3,12}$", t.Key);
            Assert.False(string.IsNullOrWhiteSpace(t.Name));
            Assert.InRange(t.Description.Length, 20, 200);
        }
    }

    [Fact]
    public void EveryTemplate_DefaultColours_AreValidBrandColours_ThatWouldPassValidation()
    {
        foreach (var t in RealRegistry().Templates)
        {
            Assert.Equal(t.DefaultPrimaryColor, BrandColor.Normalise(t.DefaultPrimaryColor, "p"));
            Assert.Equal(t.DefaultAccentColor, BrandColor.Normalise(t.DefaultAccentColor, "a"));
            Assert.NotEqual(t.DefaultPrimaryColor, t.DefaultAccentColor);
        }
    }

    [Fact]
    public void Ngf_KeepsItsOriginalGreenAndYellowAsDefaults()
    {
        var ngf = RealRegistry().Find("ngf")!;
        Assert.Equal("#138A44", ngf.DefaultPrimaryColor);
        Assert.Equal("#F5C90C", ngf.DefaultAccentColor);
        Assert.Equal("NGF - Invoice", ngf.Name);
    }

    [Fact]
    public void TheNineNewTemplates_UseDistinctDefaultPalettes()
    {
        var palettes = RealRegistry().Templates.Select(t => (t.DefaultPrimaryColor, t.DefaultAccentColor)).ToList();
        Assert.Equal(palettes.Count, palettes.Distinct().Count());
    }

    [Fact]
    public void Registry_AllowsTen_ButRefusesAnEleventh()
    {
        List<IInvoiceTemplateRenderer> Make(int n) => Enumerable.Range(0, n).Select(i => (IInvoiceTemplateRenderer)new FakeRenderer($"t{(char)('a' + i)}")).ToList();

        Assert.Equal(10, new InvoiceTemplateRegistry(Make(10)).Templates.Count);
        var ex = Assert.Throws<InvalidOperationException>(() => new InvoiceTemplateRegistry(Make(11)));
        Assert.Contains("10", ex.Message);
    }

    [Fact]
    public void Registry_RefusesATemplateWithABadDefaultColour()
    {
        Assert.Throws<InvalidOperationException>(() => new InvoiceTemplateRegistry([new FakeRenderer("bad", "green", "#FFFFFF")]));
    }

    [Fact]
    public void Registry_Find_IsCaseInsensitive_AndNullForUnknown()
    {
        var registry = RealRegistry();
        Assert.Equal("band", registry.Find("BAND")!.Key);
        Assert.Null(registry.Find("nope"));
    }

    private class FakeRenderer(string key, string primary = "#111111", string accent = "#999999") : IInvoiceTemplateRenderer
    {
        public string Key => key;
        public string Name => key;
        public string Description => "fake";
        public string DefaultPrimaryColor => primary;
        public string DefaultAccentColor => accent;
        public byte[] Render(InvoiceRenderModel model) => [];
    }

    // ---- rendering matrix --------------------------------------------------------------------------------------------------

    private static Invoice Make(IEnumerable<InvoiceLineItem> items, string? signatory = "Adaeze Okafor", string? terms = "Payment within 14 days.",
        decimal vat = 7.5m, string key = "ngf") =>
        Invoice.Create("farm-1", 12, new InvoiceDetails(
            new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 15),
            "Acme Foods Ltd", "0801 234 5678", "buyer@acme.ng", "5 Market Road, Ikeja, Lagos",
            items.ToList(), vat, "Nature Gold Farm", "0123456789", "GTBank", terms, signatory, key), "u");

    private static readonly InvoiceIssuer Farm = new("Nature Gold Farm", "12 Farm Road, Abeokuta, Ogun State", "+234 801 234 5678", "hello@naturesgold.ng", "www.naturesgold.ng", null);

    public record Scenario(string Name, Func<Invoice> Invoice, byte[]? Logo, InvoiceTheme? Theme, int? ExpectedPages, bool MoreThanOnePage = false);

    private static IEnumerable<Scenario> Scenarios()
    {
        var one = new[] { new InvoiceLineItem("Fresh pineapples", 1500m, 20m) };
        var two = new[] { new InvoiceLineItem("Pineapples", 1500m, 20m), new InvoiceLineItem("Delivery", 5000m, 1m) };
        var six = Enumerable.Range(1, 6).Select(i => new InvoiceLineItem($"Item number {i}", 1000m * i, 3m)).ToArray();
        var many = Enumerable.Range(1, 200).Select(i => new InvoiceLineItem($"Crate of produce number {i}", 100m + i, 2m)).ToArray();
        var max = Enumerable.Range(0, 10).Select(_ => new InvoiceLineItem("Largest allowed amounts", 99_999_999.99m, 9.99m)).ToArray();
        var longSignatory = new string('W', 150);
        var longTermsLines = string.Join("\n", Enumerable.Range(0, 700).Select(i => "t" + i))[..2000];
        var longTermsText = string.Join(' ', Enumerable.Repeat("Goods once sold are not returnable.", 70))[..2000];
        var logoLandscape = ImageInspectorTests.Make(ImageFormat.Png, 240, 100);
        var logoPortrait = ImageInspectorTests.Make(ImageFormat.Png, 100, 240);
        var longText = string.Join(' ', Enumerable.Repeat("Long description text", 40))[..490];

        yield return new("one item, no logo", () => Make(one), null, null, 1);
        yield return new("six items, landscape logo", () => Make(six), logoLandscape, null, 1);
        yield return new("two items, landscape logo", () => Make(two), logoLandscape, null, 1);
        yield return new("portrait logo", () => Make(two), logoPortrait, null, 1);
        yield return new("200 items", () => Make(many), logoLandscape, null, null, MoreThanOnePage: true);
        yield return new("max signatory", () => Make(one, signatory: longSignatory), null, null, 1);
        yield return new("long terms (many lines)", () => Make(one, terms: longTermsLines), null, null, null);
        yield return new("long terms (long text)", () => Make(one, terms: longTermsText), null, null, null);
        yield return new("max amounts, 100% VAT", () => Make(max, vat: 100m), null, null, null);
        yield return new("long item description", () => Make([new InvoiceLineItem(longText, 1m, 1m)]), null, null, null);
        yield return new("no optional text", () => Make(one, signatory: null, terms: null), null, null, 1);
        yield return new("very light colours", () => Make(two), logoLandscape, new InvoiceTheme("#FFE066", "#FFF2A8"), 1);
        yield return new("very dark colours", () => Make(two), null, new InvoiceTheme("#000000", "#0A0A14"), 1);
        yield return new("mid colours", () => Make(two), null, new InvoiceTheme("#808080", "#7F7F7F"), 1);
        yield return new("light primary, dark accent", () => Make(six), logoLandscape, new InvoiceTheme("#FFD700", "#1B2A6B"), 1);
        yield return new("saturated colours", () => Make(two), logoPortrait, new InvoiceTheme("#C0392B", "#8E44AD"), 1);
    }

    public static IEnumerable<object[]> Matrix() =>
        ExpectedKeys.SelectMany(key => Scenarios().Select(s => new object[] { key, s.Name }));

    private static int PageCount(byte[] pdf) => Regex.Matches(Encoding.Latin1.GetString(pdf), @"/Type\s*/Page(?![s\w])").Count;

    [Theory]
    [MemberData(nameof(Matrix))]
    public void EveryTemplate_Renders_EveryScenario(string key, string scenarioName)
    {
        var s = Scenarios().Single(x => x.Name == scenarioName);
        var invoice = s.Invoice();
        var registry = RealRegistry();
        var model = new InvoiceRenderModel(invoice, Farm with { Logo = s.Logo }, s.Theme);

        var pdf = registry.Render(key, model);

        Assert.Equal("%PDF-", Encoding.ASCII.GetString(pdf, 0, 5));
        var pages = PageCount(pdf);
        if (s.ExpectedPages is { } expected) Assert.True(pages == expected, $"{key} / {scenarioName}: expected {expected} page(s), got {pages}");
        if (s.MoreThanOnePage) Assert.True(pages > 1, $"{key} / {scenarioName}: 200 items must flow onto more pages");
    }

    [Theory]
    [InlineData("ngf")]
    [InlineData("band")]
    [InlineData("sidebar")]
    [InlineData("minimal")]
    [InlineData("callout")]
    [InlineData("cards")]
    [InlineData("ledger")]
    [InlineData("angle")]
    [InlineData("anchor")]
    [InlineData("duo")]
    public void EveryTemplate_UsesPlusJakartaSans_AndEmbedsTheLogo(string key)
    {
        var registry = RealRegistry();
        var logo = ImageInspectorTests.Make(ImageFormat.Png, 200, 80);
        var withLogo = Encoding.Latin1.GetString(registry.Render(key, new InvoiceRenderModel(Make([new InvoiceLineItem("x", 1m, 1m)]), Farm with { Logo = logo })));
        var without = Encoding.Latin1.GetString(registry.Render(key, new InvoiceRenderModel(Make([new InvoiceLineItem("x", 1m, 1m)]), Farm)));

        Assert.Contains("PlusJakartaSans", withLogo);
        Assert.Contains("/Subtype /Image", withLogo);
        Assert.DoesNotContain("/Subtype /Image", without);
    }

    [Theory]
    [InlineData("ngf")]
    [InlineData("band")]
    [InlineData("sidebar")]
    [InlineData("angle")]
    public void TheColoursChosen_ReachThePdf(string key)
    {
        // Two renders that differ only by the theme must differ in the bytes: the renderer cannot be ignoring it.
        var registry = RealRegistry();
        var invoice = Make([new InvoiceLineItem("x", 1m, 1m)]);
        var a = registry.Render(key, new InvoiceRenderModel(invoice, Farm, new InvoiceTheme("#C0392B", "#8E44AD")));
        var b = registry.Render(key, new InvoiceRenderModel(invoice, Farm, new InvoiceTheme("#1B2A6B", "#E67E22")));
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void NoTheme_MeansTheTemplatesOwnDefaults()
    {
        var registry = RealRegistry();
        var invoice = Make([new InvoiceLineItem("x", 1m, 1m)]);
        foreach (var t in registry.Templates)
        {
            var implicitDefaults = registry.Render(t.Key, new InvoiceRenderModel(invoice, Farm));
            var explicitDefaults = registry.Render(t.Key, new InvoiceRenderModel(invoice, Farm, new InvoiceTheme(t.DefaultPrimaryColor, t.DefaultAccentColor)));
            Assert.Equal(PageCount(explicitDefaults), PageCount(implicitDefaults));
            Assert.Equal(explicitDefaults.Length / 500, implicitDefaults.Length / 500);
        }
    }
}

public class InvoiceBlocksFitSizeTests
{
    [Theory]
    [InlineData("₦365,930.00", 172f, 30f)]
    [InlineData("₦17,981,999,998.20", 172f, 30f)]
    [InlineData("₦99,999,999,999.99", 190f, 20f)]
    public void FitSize_KeepsABigFigureOnOneLine(string text, float width, float max)
    {
        var size = InvoiceBlocks.FitSize(text, width, max);
        Assert.InRange(size, 11f, max);
        // An ExtraBold figure is about 0.66 em per character, so it must fit the width at this size (11pt is the floor).
        if (size > 11f) Assert.True(text.Length * size * 0.66f <= width, $"{text} at {size}pt would wrap in {width}pt");
    }
}
