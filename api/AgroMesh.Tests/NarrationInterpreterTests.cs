using AgroMesh.Application.Services;
using AgroMesh.Domain.Entities;
using AgroMesh.Domain.Enums;
using AgroMesh.Domain.Utilities;
using AgroMesh.Domain.ValueObjects;

namespace AgroMesh.Tests;

public class StructuredNarrationTests
{
    [Fact]
    public void Order_block_and_type_are_read()
    {
        var p = BankNarration.TryParseStructured("ORD-1042/Alpha/Fertilizer");
        Assert.Equal(1042, p!.OrderNumber);
        Assert.Equal("Alpha", p.Block);
        Assert.Equal("Fertilizer", p.Type);
    }

    [Fact]
    public void The_bank_text_around_the_reference_is_ignored()
    {
        var p = BankNarration.TryParseStructured("NIP/KUDA/SAMUEL OLAMIDE/ORD-7/North Block/chemical/ref 99");
        Assert.Equal(7, p!.OrderNumber);
        Assert.Equal("North Block", p.Block);
        Assert.Equal("chemical", p.Type);
    }

    [Fact]
    public void The_older_two_part_format_is_still_read_as_a_type()
    {
        var p = BankNarration.TryParseStructured("ORD-1042/chemical");
        Assert.Null(p!.Block);
        Assert.Equal("chemical", p.Type);
    }

    [Fact]
    public void A_two_part_narration_with_an_unknown_word_is_taken_as_the_block()
    {
        var p = BankNarration.TryParseStructured("ORD-1042/Alpha");
        Assert.Equal("Alpha", p!.Block);
        Assert.Null(p.Type);
    }

    [Fact]
    public void An_order_number_alone_has_no_block_or_type()
    {
        var p = BankNarration.TryParseStructured("ORD-1042");
        Assert.Equal(1042, p!.OrderNumber);
        Assert.Null(p.Block);
        Assert.Null(p.Type);
    }

    [Fact] public void No_order_means_no_structure() => Assert.Null(BankNarration.TryParseStructured("school fees"));

    [Theory]
    [InlineData("Fertilizer", TransactionCategory.Fertilizer)]
    [InlineData("fertiliser", TransactionCategory.Fertilizer)]
    [InlineData("Chemicals", TransactionCategory.Chemicals)]
    [InlineData("Diesel", TransactionCategory.Fuel)]
    [InlineData("wages", TransactionCategory.Labour)]
    [InlineData("Seeds", TransactionCategory.Seeds)]
    [InlineData("transportation", TransactionCategory.Transportation)]
    public void Words_map_to_spending_categories(string text, TransactionCategory expected) =>
        Assert.Equal(expected, BankNarration.TypeFromText(text));

    [Fact] public void An_unknown_word_is_not_a_category() => Assert.Null(BankNarration.TypeFromText("gifts"));

    [Fact]
    public void Block_names_are_compared_loosely() =>
        Assert.Equal(BankNarration.BlockKey("North Block"), BankNarration.BlockKey("north-block"));
}

public class NarrationInterpreterTests
{
    private static Block MakeBlock(string name, params (CropType Crop, int Pct)[] crops) =>
        Block.Create("farm", name, crops.Select(c => new BlockCrop(c.Crop, 0, c.Pct)).ToList(), 5);

    private static Transaction Debit(string narration, decimal amount = 5000) =>
        Transaction.Create("farm", Guid.NewGuid().ToString(), narration, -amount, DateTime.UtcNow,
            TransactionCategory.Other, ConfidenceLevel.Low, CategorizationSource.Llm);

    [Fact]
    public void The_spend_goes_to_the_named_block_and_category()
    {
        var alpha = MakeBlock("Alpha", (CropType.OilPalm, 100));
        var tx = Debit("ORD-1042/alpha/Fertilizer");

        Assert.True(NarrationInterpreter.Apply(tx, [alpha]));

        Assert.Equal(alpha.Id, tx.BlockId);
        Assert.Equal(TransactionCategory.Fertilizer, tx.Category);
        Assert.Equal(ConfidenceLevel.High, tx.Confidence);
        Assert.Equal(100, tx.CropAllocation["OilPalm"]);
    }

    [Fact]
    public void A_mixed_block_splits_the_cost_across_its_crops()
    {
        var mixed = MakeBlock("North", (CropType.OilPalm, 60), (CropType.Cassava, 40));
        var tx = Debit("ORD-1/North/Seeds");

        NarrationInterpreter.Apply(tx, [mixed]);

        Assert.Equal(60, tx.CropAllocation["OilPalm"]);
        Assert.Equal(40, tx.CropAllocation["Cassava"]);
    }

    [Fact]
    public void An_unknown_block_is_flagged_for_a_person_but_the_category_still_applies()
    {
        var tx = Debit("ORD-1/Nowhere/Fertilizer");

        NarrationInterpreter.Apply(tx, [MakeBlock("Alpha", (CropType.Maize, 100))]);

        Assert.Null(tx.BlockId);
        Assert.Equal(TransactionCategory.Fertilizer, tx.Category);
        Assert.Equal(ReconciliationStatus.NeedsReview, tx.Reconciliation);
        Assert.Contains("Nowhere", tx.ReconciliationNote);
    }

    [Fact]
    public void An_unknown_type_is_flagged_and_the_block_still_applies()
    {
        var alpha = MakeBlock("Alpha", (CropType.Maize, 100));
        var tx = Debit("ORD-1/Alpha/gifts");

        NarrationInterpreter.Apply(tx, [alpha]);

        Assert.Equal(alpha.Id, tx.BlockId);
        Assert.Equal(TransactionCategory.Other, tx.Category);
        Assert.Equal(ReconciliationStatus.NeedsReview, tx.Reconciliation);
    }

    [Fact]
    public void Money_coming_in_is_not_read_this_way()
    {
        var credit = Transaction.Create("farm", "m", "ORD-1/Alpha/Fertilizer", 5000, DateTime.UtcNow,
            TransactionCategory.Other, ConfidenceLevel.Low, CategorizationSource.Llm);
        Assert.False(NarrationInterpreter.Apply(credit, [MakeBlock("Alpha", (CropType.Maize, 100))]));
        Assert.Null(credit.BlockId);
    }

    [Fact]
    public void A_narration_with_no_order_is_left_alone()
    {
        var tx = Debit("diesel for generator");
        Assert.False(NarrationInterpreter.Apply(tx, [MakeBlock("Alpha", (CropType.Maize, 100))]));
        Assert.Equal(TransactionCategory.Other, tx.Category);
    }
}
