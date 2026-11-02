using Ilevora.Domain.Entities;
using Ilevora.Domain.Enums;
using Ilevora.Domain.Exceptions;

namespace Ilevora.Tests;

public class VendorInvoiceDomainTests
{
    internal static VendorInvoiceDetails Details(
        string vendor = "Saro Agro Sciences", string number = "SARO-2041", decimal amount = 125000m,
        DateOnly? date = null, DateOnly? due = null, string? notes = "Delivered in full") =>
        new(vendor, number, amount, date ?? new DateOnly(2026, 10, 5), due ?? new DateOnly(2026, 11, 4), notes);

    private static VendorInvoice New(VendorInvoiceDetails? d = null) =>
        VendorInvoice.Create("farm-1", "order-1", "sup-1", "file-1", "bill.pdf", "application/pdf", 2048, d ?? Details(), "user-1");

    [Fact]
    public void Create_CopiesEveryField_AndStartsUnpaid()
    {
        var bill = New();

        Assert.Equal("farm-1", bill.FarmId);
        Assert.Equal("order-1", bill.OrderId);
        Assert.Equal("sup-1", bill.SupplierId);
        Assert.Equal("file-1", bill.FileId);
        Assert.Equal("bill.pdf", bill.FileName);
        Assert.Equal("application/pdf", bill.ContentType);
        Assert.Equal(2048, bill.FileSize);
        Assert.Equal("Saro Agro Sciences", bill.VendorName);
        Assert.Equal("SARO-2041", bill.InvoiceNumber);
        Assert.Equal(125000m, bill.Amount);
        Assert.Equal(new DateOnly(2026, 10, 5), bill.InvoiceDate);
        Assert.Equal(new DateOnly(2026, 11, 4), bill.DueDate);
        Assert.Equal("Delivered in full", bill.Notes);
        Assert.Equal(VendorInvoiceStatus.Unpaid, bill.Status);
        Assert.Null(bill.PaidAt);
        Assert.Equal("user-1", bill.UploadedByUserId);
    }

    [Fact]
    public void Create_TrimsText_AndBlankOptionalsBecomeNull()
    {
        var bill = New(Details(vendor: "  Saro  ", number: "  A-1 ", notes: "  ") with { DueDate = null });
        Assert.Equal("Saro", bill.VendorName);
        Assert.Equal("A-1", bill.InvoiceNumber);
        Assert.Null(bill.Notes);
        Assert.Null(bill.DueDate);
    }

    [Theory]
    [InlineData("", "A-1")]
    [InlineData("  ", "A-1")]
    [InlineData("Saro", "")]
    [InlineData("Saro", "   ")]
    public void VendorNameAndInvoiceNumber_AreRequired(string vendor, string number)
    {
        Assert.Throws<DomainException>(() => New(Details(vendor: vendor, number: number)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-0.01)]
    public void Amount_MustBeMoreThanZero(double amount)
    {
        var ex = Assert.Throws<DomainException>(() => New(Details(amount: (decimal)amount)));
        Assert.Contains("amount", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Amount_IsRoundedToTwoDecimals_AndAbsurdValuesRejected()
    {
        Assert.Equal(10.13m, New(Details(amount: 10.125m)).Amount);
        Assert.Throws<DomainException>(() => New(Details(amount: 1_000_000_000_000m)));
    }

    [Fact]
    public void Amount_ThatRoundsToZero_IsRejected()
    {
        Assert.Throws<DomainException>(() => New(Details(amount: 0.004m)));
    }

    [Fact]
    public void DueDate_CannotBeBeforeInvoiceDate()
    {
        Assert.Throws<DomainException>(() => New(Details(date: new DateOnly(2026, 10, 5), due: new DateOnly(2026, 10, 4))));
        Assert.NotNull(New(Details(date: new DateOnly(2026, 10, 5), due: new DateOnly(2026, 10, 5))));
    }

    [Fact]
    public void FieldLengths_AreCapped()
    {
        Assert.Throws<DomainException>(() => New(Details(vendor: new string('a', 201))));
        Assert.Throws<DomainException>(() => New(Details(number: new string('a', 101))));
        Assert.Throws<DomainException>(() => New(Details(notes: new string('a', 1001))));
        Assert.NotNull(New(Details(notes: new string('a', 1000))));
    }

    [Fact]
    public void Create_RequiresAFile()
    {
        Assert.Throws<DomainException>(() => VendorInvoice.Create("farm-1", "order-1", "sup-1", "", "x.pdf", "application/pdf", 10, Details(), null));
    }

    [Fact]
    public void MarkPaid_SetsStatusAndPaidAt_MarkUnpaidClearsThem()
    {
        var bill = New();
        var when = new DateTime(2026, 10, 20, 9, 30, 0, DateTimeKind.Utc);

        bill.MarkPaid(when);
        Assert.Equal(VendorInvoiceStatus.Paid, bill.Status);
        Assert.Equal(when, bill.PaidAt);

        bill.MarkUnpaid();
        Assert.Equal(VendorInvoiceStatus.Unpaid, bill.Status);
        Assert.Null(bill.PaidAt);
    }

    [Fact]
    public void MarkPaid_WithoutADate_UsesNow()
    {
        var bill = New();
        bill.MarkPaid(null);
        Assert.NotNull(bill.PaidAt);
        Assert.True((DateTime.UtcNow - bill.PaidAt!.Value).TotalSeconds < 5);
    }

    [Fact]
    public void MarkPaid_WhenAlreadyPaid_KeepsTheOriginalPaidAt()
    {
        var bill = New();
        var first = new DateTime(2026, 10, 20, 0, 0, 0, DateTimeKind.Utc);
        bill.MarkPaid(first);
        bill.MarkPaid(first.AddDays(3));
        Assert.Equal(first, bill.PaidAt);
    }

    [Fact]
    public void MarkPaid_NormalisesTheDateToUtc()
    {
        var bill = New();
        bill.MarkPaid(new DateTime(2026, 10, 20, 9, 0, 0, DateTimeKind.Unspecified));
        Assert.Equal(DateTimeKind.Utc, bill.PaidAt!.Value.Kind);
    }

    [Fact]
    public void IsOverdue_OnlyWhenUnpaidAndPastDue()
    {
        var bill = New(Details(date: new DateOnly(2026, 10, 1), due: new DateOnly(2026, 10, 10)));

        Assert.False(bill.IsOverdue(new DateOnly(2026, 10, 10)));   // due today is not yet overdue
        Assert.True(bill.IsOverdue(new DateOnly(2026, 10, 11)));

        bill.MarkPaid(null);
        Assert.False(bill.IsOverdue(new DateOnly(2026, 12, 1)));
    }

    [Fact]
    public void IsOverdue_NoDueDate_IsNeverOverdue()
    {
        var bill = New(Details() with { DueDate = null });
        Assert.False(bill.IsOverdue(new DateOnly(2030, 1, 1)));
    }
}

public class VendorInvoiceDuplicateKeyTests
{
    private static VendorInvoice New(string number) =>
        VendorInvoice.Create("farm-1", "order-1", "sup-1", "file-1", "b.pdf", "application/pdf", 1,
            VendorInvoiceDomainTests.Details(number: number), null);

    [Theory]
    [InlineData("SARO-2041", "SARO-2041")]
    [InlineData("  saro-2041 ", "SARO-2041")]
    [InlineData("Saro-2041", "SARO-2041")]
    public void NumberKey_IsTheTrimmedUpperCaseNumber_SoDuplicatesAreCaseInsensitive(string typed, string key)
    {
        Assert.Equal(key, New(typed).InvoiceNumberKey);
        Assert.Equal(key, VendorInvoice.NormaliseNumber(typed));
    }

    [Fact]
    public void KeyFitsItsColumn_EvenForTheLongestNumber()
    {
        Assert.True(New(new string('a', 100)).InvoiceNumberKey.Length <= VendorInvoice.MaxNumberKeyLength - 20);
    }

    [Theory]
    [InlineData(1999)]
    [InlineData(2101)]
    [InlineData(9999)]
    public void Dates_MustBeBetween2000And2100(int year)
    {
        var bad = new DateOnly(year, 5, 5);
        Assert.Throws<DomainException>(() => VendorInvoice.Create("f", "o", "s", "file", "b.pdf", "application/pdf", 1,
            VendorInvoiceDomainTests.Details(date: bad, due: null) with { DueDate = null }, null));
    }
}
