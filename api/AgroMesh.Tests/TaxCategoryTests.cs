using AgroMesh.Domain.Enums;
using AgroMesh.Domain.Utilities;
using AgroMesh.Infrastructure.Services.Financial;

namespace AgroMesh.Tests;

public class TaxCategoryTests
{
    [Theory]
    [InlineData("VAT on charges")]
    [InlineData("Stamp duty")]
    [InlineData("STAMP DUTY CHARGE")]
    [InlineData("NIP transfer charge")]
    [InlineData("SMS alert charges")]
    [InlineData("Commission on turnover")]
    [InlineData("Electronic money transfer levy EMTL")]
    public void Bank_charges_vat_and_stamp_duty_are_tax_and_charges(string narration)
    {
        var (category, confidence) = KeywordDictionary.TryMatch(narration);
        Assert.Equal(TransactionCategory.TaxAndCharges, category);
        Assert.Equal(ConfidenceLevel.High, confidence);
    }

    [Theory]
    [InlineData("tax", TransactionCategory.TaxAndCharges)]
    [InlineData("VAT", TransactionCategory.TaxAndCharges)]
    [InlineData("stamp duty", TransactionCategory.TaxAndCharges)]
    [InlineData("bank charges", TransactionCategory.TaxAndCharges)]
    public void The_type_in_an_order_narration_can_name_it(string text, TransactionCategory expected) =>
        Assert.Equal(expected, BankNarration.TypeFromText(text));

    [Fact]
    public void The_new_category_is_added_last_so_stored_numbers_do_not_move()
    {
        Assert.Equal(9, (int)TransactionCategory.Other);
        Assert.Equal(10, (int)TransactionCategory.TaxAndCharges);
    }
}
