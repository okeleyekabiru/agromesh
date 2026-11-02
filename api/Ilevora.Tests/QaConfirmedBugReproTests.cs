using Ilevora.Application.Commands.Invoice;
using Ilevora.Application.DTOs;
using Ilevora.Application.Interfaces;
using Ilevora.Domain.Entities;
using Ilevora.Domain.ValueObjects;
using Ilevora.Infrastructure.Services;
using Ilevora.Infrastructure.Services.Invoices;
using Ilevora.Tests.Support;
using QuestPDF.Infrastructure;

namespace Ilevora.Tests;

/// <summary>
/// Independent-QA reproductions of bugs confirmed against a running API + Postgres 15 (see the QA report).
/// Each test states the CORRECT behaviour, so each is RED until its bug is fixed. They are tagged Category=QA-Repro:
/// run the rest of the suite with <c>dotnet test --filter "Category!=QA-Repro"</c>.
/// </summary>
[Trait("Category", "QA-Repro")]
public class QaConfirmedBugReproTests
{
    static QaConfirmedBugReproTests()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    // ---- QA-1: a logo that is taller than it is wide is refused as "not a readable image" (415) --------------------------

    [Theory]
    [InlineData(ImageFormat.Png, 100, 101)]
    [InlineData(ImageFormat.Png, 60, 120)]
    [InlineData(ImageFormat.Jpeg, 300, 400)]
    [InlineData(ImageFormat.Webp, 100, 400)]
    public void QA1_PortraitLogo_IsAcceptedByTheImageInspector(ImageFormat format, int width, int height)
    {
        var bytes = ImageInspectorTests.Make(format, width, height);

        var info = new QuestPdfImageInspector().Inspect(bytes);

        Assert.NotNull(info); // currently null: the inspector draws the image on a 50x50 page and the layout throws for height > width
        Assert.Equal(width, info!.Width);
        Assert.Equal(height, info.Height);
    }

    // ---- QA-2: a signatory name of ~45+ characters makes the invoice PDF throw (HTTP 500 on /pdf) ----------------------

    private static Invoice MakeInvoice(string? signatory = "Adaeze Okafor", string? terms = "Payment within 14 days.") =>
        Invoice.Create("farm-1", 12, new InvoiceDetails(
            new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 15),
            "Acme Foods Ltd", null, null, null,
            [new InvoiceLineItem("Fresh pineapples", 1500m, 20m)],
            7.5m, "Nature's Gold Farm", "0123456789", "GTBank", terms, signatory, InvoiceTemplateKeys.Ngf), "user-1");

    private static InvoiceIssuer Issuer() => new("Nature's Gold Farm", "12 Farm Road", "+234 801", "hello@ngf.ng", "www.ngf.ng", null);

    [Theory]
    [InlineData("Dr. Adaeze Chinwe Okafor-Nwosu, Managing Director")]
    [InlineData("Chief (Mrs.) Adaeze Chinwe Okafor-Nwosu, Managing Director & CEO")]
    public void QA2_LongSignatoryName_StillRenders(string signatory)
    {
        var bytes = new NgfInvoiceTemplate().Render(new InvoiceRenderModel(MakeInvoice(signatory: signatory), Issuer()));

        Assert.Equal("%PDF-", System.Text.Encoding.ASCII.GetString(bytes, 0, 5));
    }

    // ---- QA-3: terms/notes of ~50 short lines (well inside the 2000 character limit) throw at render time ---------------

    [Fact]
    public void QA3_FiftyLineTermsAndNotes_StillRenders()
    {
        var terms = string.Join("\n", Enumerable.Range(0, 60).Select(i => $"Row {i}"));
        Assert.True(terms.Length <= 2000);

        var bytes = new NgfInvoiceTemplate().Render(new InvoiceRenderModel(MakeInvoice(terms: terms), Issuer()));

        Assert.Equal("%PDF-", System.Text.Encoding.ASCII.GetString(bytes, 0, 5));
    }

    // ---- QA-4: creating an invoice dated 9999-12-31 without a due date throws ArgumentOutOfRangeException (HTTP 500) ----

    [Fact]
    public async Task QA4_CreateInvoice_WithLastSupportedDate_IsNotAnUnhandledException()
    {
        var farm = Farm.Create("Nature's Gold Farm", "co-1", "owner-1", id: "farm-1");
        var handler = new CreateInvoiceCommandHandler(new FakeFarms(farm), new InvoiceHandlerTests.FakeInvoices(), new InvoiceHandlerTests.FakeRegistry(), new FakeUser());
        var request = new SaveInvoiceRequest(new DateOnly(9999, 12, 31), null, "Acme", null, null, null,
            [new InvoiceLineItemRequest("x", 1m, 1m)], null, null, null, null, null, null, null);

        var ex = await Record.ExceptionAsync(() => handler.Handle(new CreateInvoiceCommand("farm-1", request), CancellationToken.None));

        // Either succeeding or a DomainException (422) is fine; ArgumentOutOfRangeException surfaces as a 500.
        Assert.False(ex is ArgumentOutOfRangeException, ex?.ToString());
    }
}
