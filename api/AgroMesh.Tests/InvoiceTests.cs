using AgroMesh.Domain.Entities;
using AgroMesh.Domain.Exceptions;
using AgroMesh.Domain.ValueObjects;

namespace AgroMesh.Tests;

public class InvoiceTests
{
    internal static InvoiceDetails Details(
        IEnumerable<InvoiceLineItem>? items = null,
        decimal vat = 0m,
        string billTo = "Acme Foods Ltd",
        DateOnly? date = null,
        DateOnly? due = null,
        string template = InvoiceTemplateKeys.Ngf) =>
        new(
            date ?? new DateOnly(2026, 10, 1),
            due ?? new DateOnly(2026, 10, 15),
            billTo, "0801 234 5678", "buyer@acme.ng", "5 Market Road, Lagos",
            (items ?? [new InvoiceLineItem("Fresh pineapples", 1500m, 2m)]).ToList(),
            vat,
            "Nature's Gold Farm", "0123456789", "GTBank",
            "Payment within 14 days.", "A. Farmer", template);

    private static Invoice New(InvoiceDetails? d = null, int number = 1) => Invoice.Create("farm-1", number, d ?? Details(), "user-1");

    // ---- money -----------------------------------------------------------------

    [Fact]
    public void LineAmount_IsRateTimesQuantity_RoundedToTwoDecimals()
    {
        Assert.Equal(3001.00m, new InvoiceLineItem("a", 1500.50m, 2m).Amount);
        Assert.Equal(1.00m, new InvoiceLineItem("a", 0.333m, 3m).Amount);
        Assert.Equal(0.01m, new InvoiceLineItem("a", 0.005m, 1m).Amount);   // away from zero, not banker's
        Assert.Equal(2.50m, new InvoiceLineItem("a", 1m, 2.5m).Amount);
    }

    [Fact]
    public void Totals_SubtotalVatAndTotal()
    {
        var invoice = New(Details(
            [new InvoiceLineItem("A", 1500.50m, 2m), new InvoiceLineItem("B", 333.33m, 3m)],
            vat: 7.5m));

        Assert.Equal(4000.99m, invoice.Subtotal);
        Assert.Equal(300.07m, invoice.VatAmount);   // 300.07425
        Assert.Equal(4301.06m, invoice.Total);
    }

    [Fact]
    public void ZeroVat_AddsNothing()
    {
        var invoice = New(Details(vat: 0m));
        Assert.Equal(3000m, invoice.Subtotal);
        Assert.Equal(0m, invoice.VatAmount);
        Assert.Equal(3000m, invoice.Total);
    }

    [Fact]
    public void HundredPercentVat_DoublesTheSubtotal()
    {
        var invoice = New(Details(vat: 100m));
        Assert.Equal(6000m, invoice.Total);
    }

    [Fact]
    public void SubtotalSumsRoundedLineAmounts_SoTheDocumentAddsUp()
    {
        // Three lines of 0.333 x 1 would sum to 0.999 -> 1.00 if rounded late, but each line prints as 0.33.
        var invoice = New(Details([new("a", 0.333m, 1m), new("b", 0.333m, 1m), new("c", 0.333m, 1m)]));
        Assert.Equal(0.99m, invoice.Subtotal);
    }

    // ---- numbering & basics ------------------------------------------------------

    [Theory]
    [InlineData(1, "INV-0001")]
    [InlineData(42, "INV-0042")]
    [InlineData(12345, "INV-12345")]
    public void Reference_IsPaddedToFourDigits(int number, string reference)
    {
        Assert.Equal(reference, New(number: number).Reference);
    }

    [Fact]
    public void Create_CopiesEveryField()
    {
        var invoice = New();
        Assert.Equal("farm-1", invoice.FarmId);
        Assert.Equal(1, invoice.Number);
        Assert.Equal(new DateOnly(2026, 10, 1), invoice.Date);
        Assert.Equal(new DateOnly(2026, 10, 15), invoice.DueDate);
        Assert.Equal("Acme Foods Ltd", invoice.BillToName);
        Assert.Equal("0801 234 5678", invoice.BillToPhone);
        Assert.Equal("buyer@acme.ng", invoice.BillToEmail);
        Assert.Equal("5 Market Road, Lagos", invoice.BillToAddress);
        Assert.Equal("GTBank", invoice.PaymentBankName);
        Assert.Equal("0123456789", invoice.PaymentAccountNumber);
        Assert.Equal("Nature's Gold Farm", invoice.PaymentAccountName);
        Assert.Equal("Payment within 14 days.", invoice.TermsAndNotes);
        Assert.Equal("A. Farmer", invoice.SignatoryName);
        Assert.Equal("ngf", invoice.TemplateKey);
        Assert.Equal("user-1", invoice.CreatedByUserId);
        Assert.Single(invoice.LineItems);
    }

    [Fact]
    public void Update_ReplacesDetails_ButKeepsNumberAndFarm()
    {
        var invoice = New(number: 7);
        invoice.Update(Details(billTo: "New Buyer", vat: 5m));
        Assert.Equal("New Buyer", invoice.BillToName);
        Assert.Equal(5m, invoice.VatPercent);
        Assert.Equal(7, invoice.Number);
        Assert.Equal("farm-1", invoice.FarmId);
    }

    [Fact]
    public void Update_Invalid_LeavesTheInvoiceUnchanged()
    {
        var invoice = New();
        Assert.Throws<DomainException>(() => invoice.Update(Details(billTo: " ")));
        Assert.Equal("Acme Foods Ltd", invoice.BillToName);
    }

    [Fact]
    public void BlankOptionalTextBecomesNull_AndTextIsTrimmed()
    {
        var d = Details() with { BillToPhone = "  ", BillToEmail = null, BillToAddress = " x ", TermsAndNotes = "", SignatoryName = " " };
        var invoice = New(d);
        Assert.Null(invoice.BillToPhone);
        Assert.Null(invoice.BillToEmail);
        Assert.Equal("x", invoice.BillToAddress);
        Assert.Null(invoice.TermsAndNotes);
        Assert.Null(invoice.SignatoryName);
    }

    // ---- validation --------------------------------------------------------------

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void BillToName_IsRequired(string name)
    {
        var ex = Assert.Throws<DomainException>(() => New(Details(billTo: name)));
        Assert.Contains("bill", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AtLeastOneLineItem_IsRequired()
    {
        Assert.Throws<DomainException>(() => New(Details(items: [])));
    }

    [Fact]
    public void AtMost200LineItems()
    {
        var items = Enumerable.Range(0, 201).Select(i => new InvoiceLineItem($"i{i}", 1m, 1m));
        Assert.Throws<DomainException>(() => New(Details(items)));
        var ok = Enumerable.Range(0, 200).Select(i => new InvoiceLineItem($"i{i}", 1m, 1m));
        Assert.Equal(200, New(Details(ok)).LineItems.Count);
    }

    [Theory]
    [InlineData("", 1, 1)]
    [InlineData("  ", 1, 1)]
    [InlineData("x", -1, 1)]
    [InlineData("x", 1, 0)]
    [InlineData("x", 1, -2)]
    public void LineItem_RulesAreEnforced(string description, double rate, double quantity)
    {
        Assert.Throws<DomainException>(() => New(Details([new InvoiceLineItem(description, (decimal)rate, (decimal)quantity)])));
    }

    [Fact]
    public void LineItem_ZeroRate_IsAllowed_ForFreeSamples()
    {
        Assert.Equal(0m, New(Details([new InvoiceLineItem("Free sample", 0m, 1m)])).Total);
    }

    [Fact]
    public void LineItem_DescriptionOver500Chars_IsRejected()
    {
        Assert.Throws<DomainException>(() => New(Details([new InvoiceLineItem(new string('a', 501), 1m, 1m)])));
    }

    [Fact]
    public void LineItem_AbsurdAmounts_AreRejected()
    {
        Assert.Throws<DomainException>(() => New(Details([new InvoiceLineItem("x", 1_000_000_000_000m, 1m)])));
        Assert.Throws<DomainException>(() => New(Details([new InvoiceLineItem("x", 1m, 1_000_000_000m)])));
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(100.1)]
    public void VatPercent_MustBeBetween0And100(double vat)
    {
        Assert.Throws<DomainException>(() => New(Details(vat: (decimal)vat)));
    }

    [Fact]
    public void DueDate_CannotBeBeforeTheInvoiceDate()
    {
        Assert.Throws<DomainException>(() => New(Details(date: new DateOnly(2026, 10, 10), due: new DateOnly(2026, 10, 9))));
        Assert.Equal(new DateOnly(2026, 10, 10), New(Details(date: new DateOnly(2026, 10, 10), due: new DateOnly(2026, 10, 10))).DueDate);
    }

    [Theory]
    [InlineData("bad")]
    [InlineData("a@b")]
    public void BillToEmail_MustLookLikeAnEmail(string email)
    {
        Assert.Throws<DomainException>(() => New(Details() with { BillToEmail = email }));
    }

    [Fact]
    public void FieldLengths_AreCapped()
    {
        Assert.Throws<DomainException>(() => New(Details(billTo: new string('a', 201))));
        Assert.Throws<DomainException>(() => New(Details() with { BillToAddress = new string('a', 501) }));
        Assert.Throws<DomainException>(() => New(Details() with { TermsAndNotes = new string('a', 2001) }));
        Assert.Throws<DomainException>(() => New(Details() with { SignatoryName = new string('a', 151) }));
        Assert.Throws<DomainException>(() => New(Details() with { PaymentBankName = new string('a', 101) }));
    }

    [Fact]
    public void Number_MustBePositive()
    {
        Assert.Throws<DomainException>(() => Invoice.Create("farm-1", 0, Details(), "u"));
    }

    [Fact]
    public void AssignNumber_ChangesTheNumber()
    {
        var invoice = New(number: 3);
        invoice.AssignNumber(4);
        Assert.Equal(4, invoice.Number);
        Assert.Equal("INV-0004", invoice.Reference);
    }
}

public class InvoiceLimitTests
{
    private static InvoiceDetails D(IEnumerable<InvoiceLineItem> items, DateOnly? date = null, DateOnly? due = null) =>
        InvoiceTests.Details(items, date: date ?? new DateOnly(2026, 10, 1), due: due ?? new DateOnly(2026, 10, 15));

    [Fact]
    public void Rate_MustBeBelow100Million()
    {
        Assert.Equal(99_999_999.99m, Invoice.Create("f", 1, D([new InvoiceLineItem("x", 99_999_999.99m, 1m)]), null).Total);
        Assert.Throws<DomainException>(() => Invoice.Create("f", 1, D([new InvoiceLineItem("x", 100_000_000m, 1m)]), null));
    }

    [Fact]
    public void Quantity_MustBeBelowOneMillion()
    {
        Assert.NotNull(Invoice.Create("f", 1, D([new InvoiceLineItem("x", 1m, 999_999.9999m)]), null));
        Assert.Throws<DomainException>(() => Invoice.Create("f", 1, D([new InvoiceLineItem("x", 1m, 1_000_000m)]), null));
    }

    [Fact]
    public void LineAmount_MustBeBelowOneBillion()
    {
        Assert.Throws<DomainException>(() => Invoice.Create("f", 1, D([new InvoiceLineItem("x", 99_999_999m, 999_999m)]), null));
        Assert.NotNull(Invoice.Create("f", 1, D([new InvoiceLineItem("x", 999_999m, 999m)]), null));
    }

    [Fact]
    public void Subtotal_MustBeBelowTenBillion()
    {
        var items = Enumerable.Range(0, 12).Select(i => new InvoiceLineItem($"i{i}", 99_999_999m, 9m)); // 12 x ~900M
        Assert.Throws<DomainException>(() => Invoice.Create("f", 1, D(items), null));
    }

    [Theory]
    [InlineData(1999)]
    [InlineData(2101)]
    [InlineData(9999)]
    public void Dates_MustBeBetween2000And2100(int year)
    {
        var bad = new DateOnly(year, 6, 1);
        var items = new[] { new InvoiceLineItem("x", 1m, 1m) };
        Assert.Throws<DomainException>(() => Invoice.Create("f", 1, D(items, date: bad, due: bad), null));
        if (year > 2100)
            Assert.Throws<DomainException>(() => Invoice.Create("f", 1, D(items, date: new DateOnly(2026, 1, 1), due: bad), null));
    }

    [Fact]
    public void DateBoundaries_2000And2100_AreAccepted()
    {
        Assert.NotNull(Invoice.Create("f", 1, D([new InvoiceLineItem("x", 1m, 1m)], new DateOnly(2000, 1, 1), new DateOnly(2100, 12, 31)), null));
    }
}

public class InvoiceColourTests
{
    private static Invoice New(string? primary, string? accent) =>
        Invoice.Create("f", 1, InvoiceTests.Details() with { PrimaryColor = primary, AccentColor = accent }, null);

    [Fact]
    public void Colours_DefaultToNull_MeaningFollowTheFarmThenTheTemplate()
    {
        var invoice = Invoice.Create("f", 1, InvoiceTests.Details(), null);
        Assert.Null(invoice.PrimaryColor);
        Assert.Null(invoice.AccentColor);
    }

    [Fact]
    public void Colours_AreNormalisedToUpperCase()
    {
        var invoice = New(" #1b2a6b", "#e67e22 ");
        Assert.Equal("#1B2A6B", invoice.PrimaryColor);
        Assert.Equal("#E67E22", invoice.AccentColor);
    }

    [Theory]
    [InlineData("", null)]
    [InlineData("  ", "")]
    public void BlankColours_MeanNone(string? p, string? a)
    {
        var invoice = New(p, a);
        Assert.Null(invoice.PrimaryColor);
        Assert.Null(invoice.AccentColor);
    }

    [Theory]
    [InlineData("red")]
    [InlineData("rgb(1,2,3)")]
    [InlineData("#12345")]
    [InlineData("#11223344")]
    [InlineData("#FFFFFF")]
    public void BadColours_AreRejected_NamingTheField(string bad)
    {
        var ex1 = Assert.Throws<DomainException>(() => New(bad, null));
        Assert.Contains("rimary", ex1.Message);
        var ex2 = Assert.Throws<DomainException>(() => New(null, bad));
        Assert.Contains("ccent", ex2.Message);
    }

    [Fact]
    public void Update_CanChangeAndClearColours_AndABadOneChangesNothing()
    {
        var invoice = New("#1B2A6B", null);
        invoice.Update(InvoiceTests.Details() with { PrimaryColor = null, AccentColor = "#C0392B" });
        Assert.Null(invoice.PrimaryColor);
        Assert.Equal("#C0392B", invoice.AccentColor);

        Assert.Throws<DomainException>(() => invoice.Update(InvoiceTests.Details() with { PrimaryColor = "#111111", AccentColor = "nope" }));
        Assert.Null(invoice.PrimaryColor);
    }
}
