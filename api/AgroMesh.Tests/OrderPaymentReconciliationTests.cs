using AgroMesh.Application.Commands.Transaction;
using AgroMesh.Application.DTOs;
using AgroMesh.Application.Interfaces;
using AgroMesh.Application.Services;
using AgroMesh.Domain.Entities;
using AgroMesh.Domain.Enums;
using AgroMesh.Domain.Exceptions;
using AgroMesh.Domain.Utilities;
using AgroMesh.Domain.ValueObjects;

namespace AgroMesh.Tests;

public class BankNarrationTests
{
    [Theory]
    [InlineData("ORD-1042/chemical", 1042)]
    [InlineData("ord-1042 chemical", 1042)]
    [InlineData("TRF FROM KABIRU ORD1042", 1042)]
    [InlineData("payment for ORD 1042", 1042)]
    [InlineData("ORD-001/seed", 1)]
    public void Order_numbers_are_read_from_the_narration(string text, int expected) =>
        Assert.Equal(expected, BankNarration.TryGetOrderNumber(text));

    [Theory]
    [InlineData("school fees")]
    [InlineData("ORDINARY transfer")]
    [InlineData("ORD-")]
    [InlineData("")]
    [InlineData(null)]
    public void Narrations_without_an_order_give_nothing(string? text) =>
        Assert.Null(BankNarration.TryGetOrderNumber(text));

    [Theory]
    [InlineData("Capital injection", true)]
    [InlineData("farm INVESTMENT march", true)]
    [InlineData("capital", true)]
    [InlineData("sale of palm oil", false)]
    [InlineData("capitalise", false)]
    public void Capital_and_investment_are_recognised(string text, bool expected) =>
        Assert.Equal(expected, BankNarration.IsCapital(text));
}

public class TransactionKindTests
{
    private static Transaction Tx(decimal amount, string narration) =>
        Transaction.Create("f1", Guid.NewGuid().ToString(), narration, amount, DateTime.UtcNow,
            TransactionCategory.Other, ConfidenceLevel.High, CategorizationSource.Manual);

    [Fact] public void Money_out_is_an_expense() => Assert.Equal(TransactionKind.Expense, Tx(-5000, "ORD-1/chemical").Kind);
    [Fact] public void Money_in_is_income() => Assert.Equal(TransactionKind.Income, Tx(5000, "sale of palm oil").Kind);
    [Fact] public void Money_in_marked_capital_is_capital() => Assert.Equal(TransactionKind.Capital, Tx(5000, "Capital from owner").Kind);
    [Fact] public void Money_in_marked_investment_is_capital() => Assert.Equal(TransactionKind.Capital, Tx(5000, "investment").Kind);

    [Fact]
    public void A_person_can_move_a_credit_between_income_and_capital()
    {
        var t = Tx(5000, "transfer");
        t.SetKind(TransactionKind.Capital);
        Assert.Equal(TransactionKind.Capital, t.Kind);
    }

    [Fact]
    public void Money_out_cannot_become_income_or_capital() =>
        Assert.Throws<DomainException>(() => Tx(-5000, "x").SetKind(TransactionKind.Capital));

    [Fact]
    public void Money_in_cannot_become_an_expense() =>
        Assert.Throws<DomainException>(() => Tx(5000, "x").SetKind(TransactionKind.Expense));
}

public class OrderPaymentTests
{
    private static Order PlacedOrder(int number = 1042, decimal qty = 10, decimal price = 500)
    {
        var o = Order.Create("f1", "sup", [new OrderLineItem("item", qty, price)], number);
        o.Place();
        return o;
    }

    private static Transaction Debit(decimal amount, string narration) =>
        Transaction.Create("f1", Guid.NewGuid().ToString(), narration, -amount, DateTime.UtcNow,
            TransactionCategory.Chemicals, ConfidenceLevel.High, CategorizationSource.Manual);

    [Fact]
    public void An_order_carries_its_reference() => Assert.Equal("ORD-1042", PlacedOrder().Reference);

    [Fact]
    public void An_exact_payment_with_the_reference_confirms_the_order()
    {
        var order = PlacedOrder();                       // 10 x 500 = 5,000
        var r = OrderPaymentMatcher.Decide(Debit(5000, "ORD-1042/chemical"), order);
        Assert.Equal(PaymentMatch.Paid, r.Outcome);
    }

    [Fact]
    public void A_payment_that_is_short_goes_to_a_person()
    {
        var r = OrderPaymentMatcher.Decide(Debit(4000, "ORD-1042/chemical"), PlacedOrder());
        Assert.Equal(PaymentMatch.NeedsReview, r.Outcome);
        Assert.Contains("5,000.00", r.Note);
        Assert.Contains("4,000.00", r.Note);
    }

    [Fact]
    public void A_payment_that_is_too_much_goes_to_a_person() =>
        Assert.Equal(PaymentMatch.NeedsReview, OrderPaymentMatcher.Decide(Debit(6000, "ORD-1042"), PlacedOrder()).Outcome);

    [Fact]
    public void An_unknown_order_goes_to_a_person() =>
        Assert.Equal(PaymentMatch.NeedsReview, OrderPaymentMatcher.Decide(Debit(5000, "ORD-9999"), null).Outcome);

    [Fact]
    public void A_draft_order_goes_to_a_person() =>
        Assert.Equal(PaymentMatch.NeedsReview,
            OrderPaymentMatcher.Decide(Debit(5000, "ORD-1042"), Order.Create("f1", "s", [new OrderLineItem("i", 10, 500)], 1042)).Outcome);

    [Fact]
    public void An_order_that_is_already_paid_goes_to_a_person()
    {
        var order = PlacedOrder();
        order.MarkPaid("earlier", DateTime.UtcNow);
        Assert.Equal(PaymentMatch.NeedsReview, OrderPaymentMatcher.Decide(Debit(5000, "ORD-1042"), order).Outcome);
    }

    [Fact]
    public void A_transfer_with_no_reference_is_not_an_order_payment() =>
        Assert.Equal(PaymentMatch.NotAnOrderPayment, OrderPaymentMatcher.Decide(Debit(5000, "chemicals"), PlacedOrder()).Outcome);

    [Fact]
    public void Money_coming_in_is_never_an_order_payment()
    {
        var credit = Transaction.Create("f1", "m", "ORD-1042 refund", 5000, DateTime.UtcNow,
            TransactionCategory.Other, ConfidenceLevel.High, CategorizationSource.Manual);
        Assert.Equal(PaymentMatch.NotAnOrderPayment, OrderPaymentMatcher.Decide(credit, PlacedOrder()).Outcome);
    }

    [Fact]
    public void A_received_order_can_still_be_paid()
    {
        var order = PlacedOrder();
        order.Receive();
        Assert.Equal(PaymentMatch.Paid, OrderPaymentMatcher.Decide(Debit(5000, "ORD-1042"), order).Outcome);
    }

    [Fact]
    public void An_order_cannot_be_paid_twice()
    {
        var order = PlacedOrder();
        order.MarkPaid("t1", DateTime.UtcNow);
        Assert.Throws<DomainException>(() => order.MarkPaid("t2", DateTime.UtcNow));
    }

    [Fact]
    public void A_draft_order_cannot_be_marked_paid() =>
        Assert.Throws<DomainException>(() => Order.Create("f1", "s", [new OrderLineItem("i", 1, 1)], 1).MarkPaid("t", DateTime.UtcNow));
}

public class PaymentReconcilerTests
{
    private class Orders : IOrderRepository
    {
        public List<Order> All = [];
        public Task<Order?> GetByIdAsync(string id, CancellationToken ct = default) => Task.FromResult(All.FirstOrDefault(o => o.Id == id));
        public Task<List<Order>> GetByFarmIdAsync(string farmId, OrderStatus? status = null, CancellationToken ct = default) =>
            Task.FromResult(All.Where(o => o.FarmId == farmId).ToList());
        public Task<Order?> GetByNumberAsync(string farmId, int number, CancellationToken ct = default) =>
            Task.FromResult(All.FirstOrDefault(o => o.FarmId == farmId && o.Number == number));
        public Task<int> GetNextNumberAsync(string farmId, CancellationToken ct = default) => Task.FromResult(1001);
        public Task<Order> AddAsync(Order order, CancellationToken ct = default) { All.Add(order); return Task.FromResult(order); }
        public Task UpdateAsync(Order order, CancellationToken ct = default) => Task.CompletedTask;
    }

    private class Txs : ITransactionRepository
    {
        public List<Transaction> All = [];
        public Task<Transaction?> GetByIdAsync(string id, CancellationToken ct = default) => Task.FromResult(All.FirstOrDefault(t => t.Id == id));
        public Task<Transaction?> GetByMonoIdAsync(string monoId, CancellationToken ct = default) => Task.FromResult(All.FirstOrDefault(t => t.MonoId == monoId));
        public Task<(List<Transaction> Items, int Total)> GetByFarmIdAsync(string farmId, int page, int pageSize, bool? pendingReview = null, CancellationToken ct = default) =>
            Task.FromResult((All.Where(t => t.FarmId == farmId).ToList(), All.Count));
        public Task<List<Transaction>> GetUncategorizedAsync(string farmId, int limit = 100, CancellationToken ct = default) => Task.FromResult(new List<Transaction>());
        public Task<Transaction> AddAsync(Transaction transaction, CancellationToken ct = default) { All.Add(transaction); return Task.FromResult(transaction); }
        public Task UpdateAsync(Transaction transaction, CancellationToken ct = default) => Task.CompletedTask;
        public Task<bool> ExistsByMonoIdAsync(string monoId, CancellationToken ct = default) => Task.FromResult(All.Any(t => t.MonoId == monoId));
        public Task<int> GetPendingReviewCountAsync(string farmId, CancellationToken ct = default) => Task.FromResult(0);
        public Task<decimal> GetTotalSpendMtdAsync(string farmId, CancellationToken ct = default) => Task.FromResult(0m);
        public Task<List<MonthAmount>> GetMonthlySpendAsync(string farmId, DateTime from, CancellationToken ct = default) => Task.FromResult(new List<MonthAmount>());
        public Task<List<Transaction>> GetBetweenAsync(string farmId, DateTime from, DateTime to, CancellationToken ct = default) =>
            Task.FromResult(All.Where(t => t.FarmId == farmId && t.Date >= from && t.Date < to).ToList());
        public Task<List<Transaction>> GetNeedingReviewAsync(string farmId, CancellationToken ct = default) =>
            Task.FromResult(All.Where(t => t.FarmId == farmId && t.Reconciliation == ReconciliationStatus.NeedsReview).ToList());
        public Task<List<Transaction>> GetMtdAsync(string farmId, CancellationToken ct = default) => Task.FromResult(new List<Transaction>());
    }

    private static Order Placed(string farm = "f1", int number = 1042)
    {
        var o = Order.Create(farm, "sup", [new OrderLineItem("item", 10, 500)], number);
        o.Place();
        return o;
    }

    private static Transaction Debit(decimal amount, string narration, string farm = "f1") =>
        Transaction.Create(farm, Guid.NewGuid().ToString(), narration, -amount, DateTime.UtcNow,
            TransactionCategory.Chemicals, ConfidenceLevel.High, CategorizationSource.Manual);

    [Fact]
    public async Task A_matching_transfer_marks_the_order_paid_and_links_the_transaction()
    {
        var orders = new Orders { All = [Placed()] };
        var txs = new Txs();
        var tx = Debit(5000, "ORD-1042/chemical");
        txs.All.Add(tx);

        var paid = await new PaymentReconciler(orders, txs).ReconcileAsync(tx);

        Assert.True(paid);
        Assert.True(orders.All[0].IsPaid);
        Assert.Equal(tx.Id, orders.All[0].PaymentTransactionId);
        Assert.Equal(ReconciliationStatus.Matched, tx.Reconciliation);
        Assert.Equal(orders.All[0].Id, tx.OrderId);
    }

    [Fact]
    public async Task A_wrong_amount_flags_the_transfer_and_leaves_the_order_unpaid()
    {
        var orders = new Orders { All = [Placed()] };
        var tx = Debit(1000, "ORD-1042/chemical");

        var paid = await new PaymentReconciler(orders, new Txs { All = [tx] }).ReconcileAsync(tx);

        Assert.False(paid);
        Assert.False(orders.All[0].IsPaid);
        Assert.Equal(ReconciliationStatus.NeedsReview, tx.Reconciliation);
        Assert.NotNull(tx.ReconciliationNote);
    }

    [Fact]
    public async Task An_order_number_from_another_farm_is_not_matched()
    {
        var orders = new Orders { All = [Placed("other-farm")] };
        var tx = Debit(5000, "ORD-1042");

        await new PaymentReconciler(orders, new Txs { All = [tx] }).ReconcileAsync(tx);

        Assert.False(orders.All[0].IsPaid);
        Assert.Equal(ReconciliationStatus.NeedsReview, tx.Reconciliation);
    }

    [Fact]
    public async Task A_transfer_without_a_reference_is_left_alone()
    {
        var orders = new Orders { All = [Placed()] };
        var tx = Debit(5000, "diesel");

        await new PaymentReconciler(orders, new Txs { All = [tx] }).ReconcileAsync(tx);

        Assert.False(orders.All[0].IsPaid);
        Assert.Equal(ReconciliationStatus.None, tx.Reconciliation);
    }

    [Fact]
    public async Task A_person_can_confirm_a_flagged_transfer_against_the_right_order()
    {
        var order = Placed();
        var orders = new Orders { All = [order] };
        var tx = Debit(4800, "ORD-1042");
        var txs = new Txs { All = [tx] };
        await new PaymentReconciler(orders, txs).ReconcileAsync(tx);          // short by 200: flagged

        var handler = new ResolveReconciliationCommandHandler(txs, orders);
        var dto = await handler.Handle(new ResolveReconciliationCommand("f1", tx.Id, new ResolveReconciliationRequest(true)), default);

        Assert.True(order.IsPaid);
        Assert.Equal(ReconciliationStatus.Matched, dto.Reconciliation);
    }

    [Fact]
    public async Task A_person_can_dismiss_a_flagged_transfer()
    {
        var orders = new Orders { All = [Placed()] };
        var tx = Debit(1, "ORD-1042");
        var txs = new Txs { All = [tx] };
        await new PaymentReconciler(orders, txs).ReconcileAsync(tx);

        var handler = new ResolveReconciliationCommandHandler(txs, orders);
        var dto = await handler.Handle(new ResolveReconciliationCommand("f1", tx.Id, new ResolveReconciliationRequest(false)), default);

        Assert.False(orders.All[0].IsPaid);
        Assert.Equal(ReconciliationStatus.Dismissed, dto.Reconciliation);
    }

    [Fact]
    public async Task Only_flagged_transfers_can_be_resolved()
    {
        var tx = Debit(5000, "diesel");
        var handler = new ResolveReconciliationCommandHandler(new Txs { All = [tx] }, new Orders());
        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new ResolveReconciliationCommand("f1", tx.Id, new ResolveReconciliationRequest(false)), default));
    }

    [Fact]
    public async Task Another_farms_transfer_cannot_be_resolved()
    {
        var tx = Debit(1, "ORD-1042", "other-farm");
        var txs = new Txs { All = [tx] };
        tx.FlagForReview("x");
        var handler = new ResolveReconciliationCommandHandler(txs, new Orders());
        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new ResolveReconciliationCommand("f1", tx.Id, new ResolveReconciliationRequest(false)), default));
    }
}
