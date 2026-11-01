using System.Text;
using System.Text.RegularExpressions;
using AgroMesh.Application.Interfaces;
using AgroMesh.Domain.Entities;
using AgroMesh.Domain.ValueObjects;
using AgroMesh.Infrastructure.Services.Invoices;
using QuestPDF.Infrastructure;

namespace AgroMesh.Tests;

/// <summary>Real QuestPDF renders (no fakes): the NGF template and the registry that picks a template by key.</summary>
public class InvoiceRendererTests
{
    static InvoiceRendererTests()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    private static readonly InvoiceTemplateRegistry Registry = new([new NgfInvoiceTemplate()]);

    private static Invoice MakeInvoice(IEnumerable<InvoiceLineItem>? items = null, decimal vat = 7.5m, string? terms = "Payment within 14 days.", string billTo = "Acme Foods Ltd") =>
        Invoice.Create("farm-1", 12, new InvoiceDetails(
            new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 15),
            billTo, "0801 234 5678", "buyer@acme.ng", "5 Market Road, Ikeja, Lagos",
            (items ?? [new InvoiceLineItem("Fresh pineapples (grade A)", 1500m, 20m), new InvoiceLineItem("Delivery", 5000m, 1m)]).ToList(),
            vat, "Nature's Gold Farm", "0123456789", "GTBank", terms, "Adaeze Okafor", InvoiceTemplateKeys.Ngf), "user-1");

    private static InvoiceIssuer Issuer(byte[]? logo = null) =>
        new("Nature's Gold Farm", "12 Farm Road, Abeokuta, Ogun State", "+234 801 234 5678", "hello@naturesgold.ng", "www.naturesgold.ng", logo);

    private static int PageCount(byte[] pdf) =>
        Regex.Matches(Encoding.Latin1.GetString(pdf), @"/Type\s*/Page(?![s\w])").Count;

    private static void AssertIsPdf(byte[] bytes)
    {
        Assert.True(bytes.Length > 1000);
        Assert.Equal("%PDF-", Encoding.ASCII.GetString(bytes, 0, 5));
    }

    [Fact]
    public void Registry_ListsNgfAsTheDefaultTemplate()
    {
        var info = Assert.Single(Registry.Templates);
        Assert.Equal("ngf", info.Key);
        Assert.Equal("NGF - Invoice", info.Name);
        Assert.True(Registry.Exists("ngf"));
        Assert.False(Registry.Exists("other"));
        Assert.Contains(InvoiceTemplateKeys.Default, Registry.Templates.Select(t => t.Key));
    }

    [Fact]
    public void Registry_UnknownKey_ThrowsOnRender()
    {
        Assert.Throws<ArgumentException>(() => Registry.Render("nope", new InvoiceRenderModel(MakeInvoice(), Issuer())));
    }

    [Fact]
    public void Registry_RejectsTwoTemplatesWithTheSameKey()
    {
        Assert.Throws<InvalidOperationException>(() => new InvoiceTemplateRegistry([new NgfInvoiceTemplate(), new NgfInvoiceTemplate()]));
    }

    [Fact]
    public void Registry_PicksTheRendererByKey()
    {
        var registry = new InvoiceTemplateRegistry([new NgfInvoiceTemplate(), new StubTemplate()]);
        Assert.Equal("stub"u8.ToArray(), registry.Render("stub", new InvoiceRenderModel(MakeInvoice(), Issuer())));
        Assert.Equal(2, registry.Templates.Count);
        Assert.True(registry.Exists("STUB"));
    }

    private class StubTemplate : IInvoiceTemplateRenderer
    {
        public string Key => "stub";
        public string Name => "Stub";
        public string Description => "Test only";
        public byte[] Render(InvoiceRenderModel model) => "stub"u8.ToArray();
    }

    [Fact]
    public void Ngf_RendersAValidSinglePagePdf_WithoutALogo()
    {
        var pdf = Registry.Render("ngf", new InvoiceRenderModel(MakeInvoice(), Issuer()));
        AssertIsPdf(pdf);
        Assert.Equal(1, PageCount(pdf));
    }

    [Theory]
    [InlineData(ImageFormat.Png)]
    [InlineData(ImageFormat.Jpeg)]
    [InlineData(ImageFormat.Webp)]
    public void Ngf_EmbedsTheLogo_InAnyAllowedFormat(ImageFormat format)
    {
        var withoutLogo = Registry.Render("ngf", new InvoiceRenderModel(MakeInvoice(), Issuer()));
        var withLogo = Registry.Render("ngf", new InvoiceRenderModel(MakeInvoice(), Issuer(ImageInspectorTests.Make(format, 240, 120))));

        AssertIsPdf(withLogo);
        Assert.Equal(1, PageCount(withLogo));
        Assert.Contains("/Subtype /Image", Encoding.Latin1.GetString(withLogo));
        Assert.DoesNotContain("/Subtype /Image", Encoding.Latin1.GetString(withoutLogo));
    }

    [Fact]
    public void Ngf_BrokenLogoBytes_AreSkipped_TheInvoiceStillPrints()
    {
        var pdf = Registry.Render("ngf", new InvoiceRenderModel(MakeInvoice(), Issuer([1, 2, 3, 4, 5])));
        AssertIsPdf(pdf);
        Assert.DoesNotContain("/Subtype /Image", Encoding.Latin1.GetString(pdf));
    }

    [Fact]
    public void Ngf_UsesPlusJakartaSans()
    {
        var pdf = Registry.Render("ngf", new InvoiceRenderModel(MakeInvoice(), Issuer()));
        Assert.Contains("PlusJakartaSans", Encoding.Latin1.GetString(pdf));
    }

    [Fact]
    public void Ngf_ManyItems_FlowOntoMorePages()
    {
        var items = Enumerable.Range(1, 80).Select(i => new InvoiceLineItem($"Crate of produce number {i}", 100m + i, 2m));
        var pdf = Registry.Render("ngf", new InvoiceRenderModel(MakeInvoice(items), Issuer()));
        AssertIsPdf(pdf);
        Assert.True(PageCount(pdf) > 1, "80 line items cannot fit one A4 page");
    }

    [Fact]
    public void Ngf_FewItems_StillPadsToAtLeastFiveRows_OnOnePage()
    {
        var pdf = Registry.Render("ngf", new InvoiceRenderModel(MakeInvoice([new InvoiceLineItem("Only item", 10m, 1m)]), Issuer()));
        Assert.Equal(1, PageCount(pdf));
    }

    [Fact]
    public void Ngf_MinimalData_NoOptionalFields_Renders()
    {
        var invoice = Invoice.Create("farm-1", 1, new InvoiceDetails(
            new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 1), "Walk-in customer", null, null, null,
            [new InvoiceLineItem("Item", 0m, 1m)], 0m, null, null, null, null, null, "ngf"), null);

        var pdf = Registry.Render("ngf", new InvoiceRenderModel(invoice, new InvoiceIssuer("Farm", null, null, null, null, null)));

        AssertIsPdf(pdf);
    }

    [Fact]
    public void Ngf_VeryLongTextAndAwkwardCharacters_DoNotBreakTheLayout()
    {
        var longText = string.Join(' ', Enumerable.Repeat("Ọ̀gbọ́n ẹ̀gbẹ́ & <b>bold</b> \"quotes\" ₦ 100%", 120));
        var items = new[] { new InvoiceLineItem(longText[..490], 12345678.55m, 3.5m) };
        var invoice = MakeInvoice(items, terms: longText[..1900], billTo: "Ünïcödé & Sons <Ltd>");

        var pdf = Registry.Render("ngf", new InvoiceRenderModel(invoice, Issuer()));

        AssertIsPdf(pdf);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(40)]
    [InlineData(1000)]
    public void Ngf_TermsOfAnyLengthUpToTheLimit_Render(int lines)
    {
        var terms = string.Join("\n", Enumerable.Repeat("a", lines));
        if (terms.Length > 2000) terms = terms[..2000];
        AssertIsPdf(Registry.Render("ngf", new InvoiceRenderModel(MakeInvoice(terms: terms), Issuer())));
    }

    [Fact]
    public void Ngf_TwoThousandCharacterTermsWithoutNewlines_Render()
    {
        AssertIsPdf(Registry.Render("ngf", new InvoiceRenderModel(MakeInvoice(terms: new string('x', 2000)), Issuer())));
    }

    [Fact]
    public void Ngf_LongTerms_FlowOntoASecondPage()
    {
        var many = string.Join("\n", Enumerable.Range(0, 200).Select(i => $"Term number {i}"))[..1990];
        var pdf = Registry.Render("ngf", new InvoiceRenderModel(MakeInvoice(terms: many), Issuer()));
        Assert.True(PageCount(pdf) >= 2);
    }

    [Fact]
    public void Ngf_SignatoryAtTheMaximumLength_Renders()
    {
        var invoice = Invoice.Create("farm-1", 1, new InvoiceDetails(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 2), "B", null, null, null,
            [new InvoiceLineItem("x", 1m, 1m)], 0m, null, null, null, null, new string('W', 150), "ngf"), null);
        AssertIsPdf(Registry.Render("ngf", new InvoiceRenderModel(invoice, Issuer())));
    }

    [Fact]
    public void Ngf_LargestAllowedFigures_Render()
    {
        var items = Enumerable.Range(0, 10).Select(i => new InvoiceLineItem("Big", 99_999_999.99m, 9.99m)).ToList();
        var invoice = Invoice.Create("farm-1", 1, new InvoiceDetails(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 2), "B", null, null, null,
            items, 100m, null, null, null, null, null, "ngf"), null);
        AssertIsPdf(Registry.Render("ngf", new InvoiceRenderModel(invoice, Issuer())));
    }

    [Fact]
    public void Ngf_ShowsTheVatPercentageInItsLabel()
    {
        // The PDF text is compressed, so check the label through the template's own text helper.
        Assert.Equal("TAX VAT (7.5%)", NgfInvoiceTemplate.VatLabel(7.5m));
        Assert.Equal("TAX VAT (0%)", NgfInvoiceTemplate.VatLabel(0m));
        Assert.Equal("TAX VAT (7%)", NgfInvoiceTemplate.VatLabel(7.00m));
        Assert.Equal("TAX VAT (12.25%)", NgfInvoiceTemplate.VatLabel(12.25m));
    }

    [Fact]
    public void Ngf_FormatsMoneyWithThousandsAndTwoDecimals()
    {
        Assert.Equal("₦1,234,567.50", NgfInvoiceTemplate.Money(1234567.5m));
        Assert.Equal("₦0.00", NgfInvoiceTemplate.Money(0m));
    }
}
