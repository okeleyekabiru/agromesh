using Ilevora.Application.Commands.VendorInvoice;
using Ilevora.Application.DTOs;
using Ilevora.Application.Interfaces;
using Ilevora.Application.Queries.VendorInvoice;
using Ilevora.Domain.Entities;
using Ilevora.Domain.Enums;
using Ilevora.Domain.Exceptions;
using Ilevora.Domain.ValueObjects;
using Ilevora.Tests.Support;

namespace Ilevora.Tests;

public class VendorInvoiceHandlerTests
{
    private class FakeOrders : IOrderRepository
    {
        public List<Order> Orders = [];
        public Task<Order?> GetByIdAsync(string id, CancellationToken ct = default) => Task.FromResult(Orders.FirstOrDefault(o => o.Id == id));
        public Task<List<Order>> GetByFarmIdAsync(string farmId, OrderStatus? status = null, CancellationToken ct = default) =>
            Task.FromResult(Orders.Where(o => o.FarmId == farmId && (status == null || o.Status == status)).ToList());
        public Task<Order?> GetByNumberAsync(string farmId, int number, CancellationToken ct = default) =>
            Task.FromResult(Orders.FirstOrDefault(o => o.FarmId == farmId && o.Number == number));
        public Task<int> GetNextNumberAsync(string farmId, CancellationToken ct = default) => Task.FromResult(1001);
        public Task<Order> AddAsync(Order order, CancellationToken ct = default) { Orders.Add(order); return Task.FromResult(order); }
        public Task UpdateAsync(Order order, CancellationToken ct = default) => Task.CompletedTask;
    }

    private class FakeSuppliers : ISupplierRepository
    {
        public List<Supplier> Suppliers = [];
        public Task<Supplier?> GetByIdAsync(string id, CancellationToken ct = default) => Task.FromResult(Suppliers.FirstOrDefault(s => s.Id == id));
        public Task<List<Supplier>> GetByFarmIdAsync(string farmId, CancellationToken ct = default) => Task.FromResult(Suppliers.Where(s => s.FarmId == farmId).ToList());
        public Task<Supplier> AddAsync(Supplier supplier, CancellationToken ct = default) { Suppliers.Add(supplier); return Task.FromResult(supplier); }
    }

    private class FakeVendorInvoices : IVendorInvoiceRepository
    {
        public List<VendorInvoice> Items = [];
        public bool FailOnAdd;
        public bool DuplicateOnAdd;

        public Task<VendorInvoice?> GetByIdAsync(string id, CancellationToken ct = default) => Task.FromResult(Items.FirstOrDefault(v => v.Id == id));
        public Task<List<VendorInvoice>> GetByFarmIdAsync(string farmId, string? orderId = null, VendorInvoiceStatus? status = null, CancellationToken ct = default) =>
            Task.FromResult(Items.Where(v => v.FarmId == farmId && (orderId == null || v.OrderId == orderId) && (status == null || v.Status == status))
                .OrderByDescending(v => v.CreatedAt).ToList());
        public Task<bool> ExistsAsync(string farmId, string supplierId, string invoiceNumber, CancellationToken ct = default) =>
            Task.FromResult(Items.Any(v => v.FarmId == farmId && v.SupplierId == supplierId && string.Equals(v.InvoiceNumber, invoiceNumber, StringComparison.OrdinalIgnoreCase)));
        public Task<VendorInvoice> AddAsync(VendorInvoice invoice, CancellationToken ct = default)
        {
            if (FailOnAdd) throw new InvalidOperationException("db down");
            if (DuplicateOnAdd) throw new DuplicateVendorInvoiceException();
            Items.Add(invoice);
            return Task.FromResult(invoice);
        }
        public Task UpdateAsync(VendorInvoice invoice, CancellationToken ct = default) => Task.CompletedTask;
        public Task DeleteAsync(VendorInvoice invoice, CancellationToken ct = default) { Items.Remove(invoice); return Task.CompletedTask; }
    }

    private class Harness
    {
        public Farm Farm = Farm.Create("NGF", "co-1", "owner-1", id: "farm-1");
        public FakeFarms Farms;
        public FakeOrders Orders = new();
        public FakeSuppliers Suppliers = new();
        public FakeFiles Files = new();
        public FakeVendorInvoices Bills = new();
        public Supplier Supplier = Supplier.Create("farm-1", "Saro Agro Sciences");
        public Order Received;
        public FakeUser User;

        public Harness(UserRole role = UserRole.Admin, string? companyId = "co-1")
        {
            Farms = new FakeFarms(Farm);
            User = new FakeUser(role, companyId);
            Suppliers.Suppliers.Add(Supplier);
            Received = NewOrder(OrderStatus.Received);
        }

        public Order NewOrder(OrderStatus status, string farmId = "farm-1", int number = 1001)
        {
            var order = Order.Create(farmId, Supplier.Id, [new OrderLineItem("item-1", 10m, 500m)], number);
            if (status >= OrderStatus.Placed) order.Place();
            if (status >= OrderStatus.Received) order.Receive();
            Orders.Orders.Add(order);
            return order;
        }

        public UploadVendorInvoiceCommandHandler Upload => new(Farms, Orders, Suppliers, Files, Bills, User);
        public GetVendorInvoicesQueryHandler List => new(Farms, Orders, Bills, User);
        public GetVendorInvoiceQueryHandler Get => new(Farms, Orders, Bills, User);
        public DownloadVendorInvoiceFileQueryHandler Download => new(Farms, Bills, Files, User);
        public SetVendorInvoiceStatusCommandHandler SetStatus => new(Farms, Orders, Bills, User);
        public DeleteVendorInvoiceCommandHandler Delete => new(Farms, Bills, Files, User);
    }

    private static UploadVendorInvoiceFields Fields(string? vendor = null, string number = "SARO-2041", decimal amount = 5000m,
        DateOnly? date = null, DateOnly? due = null, string? notes = "ok") =>
        new(vendor, number, amount, date ?? new DateOnly(2026, 10, 5), due ?? new DateOnly(2026, 11, 4), notes);

    private static UploadVendorInvoiceCommand Cmd(Harness h, Order? order = null, byte[]? content = null, string? type = "application/pdf",
        string? name = "bill.pdf", UploadVendorInvoiceFields? fields = null) =>
        new("farm-1", (order ?? h.Received).Id, content ?? UploadedFileValidatorTests.Pdf, name, type, fields ?? Fields());

    // ---- upload ---------------------------------------------------------------------

    [Fact]
    public async Task Upload_ToAReceivedOrder_StoresFileAndRecord()
    {
        var h = new Harness();

        var dto = await h.Upload.Handle(Cmd(h), default);

        Assert.Equal(h.Received.Id, dto.OrderId);
        Assert.Equal("ORD-1001", dto.OrderReference);
        Assert.Equal(5000m, dto.OrderTotal);
        Assert.False(dto.OrderIsPaid);
        Assert.Equal(h.Supplier.Id, dto.SupplierId);
        Assert.Equal("Saro Agro Sciences", dto.VendorName);   // defaults to the order's supplier
        Assert.Equal("SARO-2041", dto.InvoiceNumber);
        Assert.Equal(5000m, dto.Amount);
        Assert.Equal("Unpaid", dto.Status);
        Assert.False(dto.IsOverdue);
        Assert.Equal("bill.pdf", dto.FileName);
        Assert.Equal("application/pdf", dto.ContentType);
        Assert.Equal(UploadedFileValidatorTests.Pdf.Length, dto.FileSize);
        Assert.Equal("user-1", dto.UploadedByUserId);

        var stored = Assert.Single(h.Files.Files.Values);
        Assert.Equal(UploadedFileValidatorTests.Pdf, stored.Content);
        Assert.Equal("farm-1", stored.FarmId);
        Assert.Equal(stored.Id, h.Bills.Items.Single().FileId);
    }

    [Fact]
    public async Task Upload_ExplicitVendorName_IsKept()
    {
        var h = new Harness();
        var dto = await h.Upload.Handle(Cmd(h, fields: Fields(vendor: "Saro (Lagos branch)")), default);
        Assert.Equal("Saro (Lagos branch)", dto.VendorName);
    }

    [Theory]
    [InlineData(OrderStatus.Draft)]
    [InlineData(OrderStatus.Placed)]
    public async Task Upload_ToAnOrderThatIsNotCompleted_Is422_AndNothingIsStored(OrderStatus status)
    {
        var h = new Harness();
        var order = h.NewOrder(status, number: 1002);

        var ex = await Assert.ThrowsAsync<DomainException>(() => h.Upload.Handle(Cmd(h, order), default));

        Assert.Contains("Received", ex.Message);
        Assert.Empty(h.Files.Files);
        Assert.Empty(h.Bills.Items);
    }

    [Fact]
    public async Task Upload_ToAnOrderOfAnotherFarm_Is404()
    {
        var h = new Harness();
        var foreign = h.NewOrder(OrderStatus.Received, farmId: "farm-OTHER", number: 7);
        await Assert.ThrowsAsync<NotFoundException>(() => h.Upload.Handle(Cmd(h, foreign), default));
        Assert.Empty(h.Files.Files);
    }

    [Fact]
    public async Task Upload_UnknownOrder_Is404()
    {
        var h = new Harness();
        await Assert.ThrowsAsync<NotFoundException>(() => h.Upload.Handle(Cmd(h) with { OrderId = "nope" }, default));
    }

    [Theory]
    [InlineData(UserRole.Admin)]
    [InlineData(UserRole.FarmManager)]
    [InlineData(UserRole.Supervisor)]
    public async Task Upload_FarmStaffMayAttachBills(UserRole role)
    {
        var h = new Harness(role);
        Assert.NotNull(await h.Upload.Handle(Cmd(h), default));
    }

    [Fact]
    public async Task Upload_MemberIsForbidden()
    {
        var h = new Harness(UserRole.Member);
        await Assert.ThrowsAsync<ForbiddenException>(() => h.Upload.Handle(Cmd(h), default));
        Assert.Empty(h.Files.Files);
    }

    [Fact]
    public async Task Upload_OtherCompany_Is404()
    {
        var h = new Harness(companyId: "other");
        await Assert.ThrowsAsync<NotFoundException>(() => h.Upload.Handle(Cmd(h), default));
    }

    [Theory]
    [InlineData("application/pdf", "pdf")]
    [InlineData("image/png", "png")]
    [InlineData("image/jpeg", "jpg")]
    public async Task Upload_AcceptsPdfPngAndJpeg(string type, string kind)
    {
        var h = new Harness();
        var bytes = kind switch { "pdf" => UploadedFileValidatorTests.Pdf, "png" => UploadedFileValidatorTests.Png, _ => UploadedFileValidatorTests.Jpeg };
        var dto = await h.Upload.Handle(Cmd(h, content: bytes, type: type), default);
        Assert.Equal(type, dto.ContentType);
    }

    [Fact]
    public async Task Upload_StoresTheSniffedType_EvenWhenNoneIsDeclared()
    {
        var h = new Harness();
        var dto = await h.Upload.Handle(Cmd(h, content: UploadedFileValidatorTests.Png, type: null, name: null), default);
        Assert.Equal("image/png", dto.ContentType);
        Assert.Equal("invoice.png", dto.FileName);
    }

    [Fact]
    public async Task Upload_RejectsExecutablesGifsHtmlAndWebP()
    {
        var h = new Harness();
        byte[][] bad = [[0x4D, 0x5A, 0x90, 0], UploadedFileValidatorTests.Gif, "<html><script>alert(1)</script></html>"u8.ToArray(), UploadedFileValidatorTests.WebP()];
        foreach (var bytes in bad)
            await Assert.ThrowsAsync<UnsupportedFileTypeException>(() => h.Upload.Handle(Cmd(h, content: bytes, type: null), default));
        Assert.Empty(h.Files.Files);
    }

    [Fact]
    public async Task Upload_PdfDeclaredAsPng_Is415()
    {
        var h = new Harness();
        await Assert.ThrowsAsync<UnsupportedFileTypeException>(() => h.Upload.Handle(Cmd(h, type: "image/png"), default));
    }

    [Fact]
    public async Task Upload_OverTenMegabytes_Is413()
    {
        var h = new Harness();
        var big = new byte[10 * 1024 * 1024 + 1];
        UploadedFileValidatorTests.Pdf.CopyTo(big, 0);
        await Assert.ThrowsAsync<PayloadTooLargeException>(() => h.Upload.Handle(Cmd(h, content: big), default));
        Assert.Empty(h.Files.Files);
    }

    [Fact]
    public async Task Upload_EmptyFile_Is422()
    {
        var h = new Harness();
        await Assert.ThrowsAsync<DomainException>(() => h.Upload.Handle(Cmd(h, content: []), default));
    }

    [Fact]
    public async Task Upload_InvalidFields_AreRejected_BeforeAnyFileIsStored()
    {
        var h = new Harness();
        await Assert.ThrowsAsync<DomainException>(() => h.Upload.Handle(Cmd(h, fields: Fields(amount: 0m)), default));
        await Assert.ThrowsAsync<DomainException>(() => h.Upload.Handle(Cmd(h, fields: Fields(amount: -5m)), default));
        await Assert.ThrowsAsync<DomainException>(() => h.Upload.Handle(Cmd(h, fields: Fields(number: " ")), default));
        await Assert.ThrowsAsync<DomainException>(() => h.Upload.Handle(Cmd(h, fields: Fields(date: new DateOnly(2026, 10, 5), due: new DateOnly(2026, 10, 1))), default));
        Assert.Empty(h.Files.Files);
        Assert.Empty(h.Bills.Items);
    }

    [Fact]
    public async Task Upload_DuplicateInvoiceNumberForTheSameVendor_IsRejected_CaseInsensitively()
    {
        var h = new Harness();
        await h.Upload.Handle(Cmd(h), default);

        var ex = await Assert.ThrowsAsync<DomainException>(() => h.Upload.Handle(Cmd(h, fields: Fields(number: "saro-2041")), default));

        Assert.Contains("already", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Single(h.Bills.Items);
        Assert.Single(h.Files.Files);
    }

    [Fact]
    public async Task Upload_WhenARacingUploadWonTheUniqueKey_IsTheSame422_AndTheStoredFileIsRemoved()
    {
        // The existence check passed (both uploads saw no bill), but the database's unique key rejected the second insert.
        var h = new Harness();
        h.Bills.DuplicateOnAdd = true;

        var ex = await Assert.ThrowsAsync<DomainException>(() => h.Upload.Handle(Cmd(h), default));

        Assert.Contains("already been uploaded", ex.Message);
        Assert.IsNotType<DuplicateVendorInvoiceException>(ex);
        Assert.Empty(h.Files.Files);
    }

    [Fact]
    public async Task Upload_SameNumberFromAnotherVendor_IsAllowed()
    {
        var h = new Harness();
        await h.Upload.Handle(Cmd(h), default);
        var other = Supplier.Create("farm-1", "Other Vendor");
        h.Suppliers.Suppliers.Add(other);
        var order2 = Order.Create("farm-1", other.Id, [new OrderLineItem("i", 1m, 1m)], 1002);
        order2.Place(); order2.Receive();
        h.Orders.Orders.Add(order2);

        Assert.NotNull(await h.Upload.Handle(Cmd(h, order2), default));
    }

    [Fact]
    public async Task Upload_SeveralBillsForOneOrder_AreAllowed()
    {
        var h = new Harness();
        await h.Upload.Handle(Cmd(h, fields: Fields(number: "A-1")), default);
        await h.Upload.Handle(Cmd(h, fields: Fields(number: "A-2")), default);
        Assert.Equal(2, h.Bills.Items.Count);
    }

    [Fact]
    public async Task Upload_IfSavingTheRecordFails_TheStoredFileIsRemoved()
    {
        var h = new Harness();
        h.Bills.FailOnAdd = true;

        await Assert.ThrowsAsync<InvalidOperationException>(() => h.Upload.Handle(Cmd(h), default));

        Assert.Empty(h.Files.Files);
    }

    [Fact]
    public async Task Upload_ReportsWhetherTheOrderIsPaid()
    {
        var h = new Harness();
        h.Received.MarkPaid("txn-1", DateTime.UtcNow);
        var dto = await h.Upload.Handle(Cmd(h), default);
        Assert.True(dto.OrderIsPaid);
    }

    [Fact]
    public void Upload_NeverTouchesTheLedger_NoTransactionRepositoryIsInvolved()
    {
        var handlerTypes = new[]
        {
            typeof(UploadVendorInvoiceCommandHandler), typeof(SetVendorInvoiceStatusCommandHandler), typeof(DeleteVendorInvoiceCommandHandler),
            typeof(GetVendorInvoicesQueryHandler), typeof(GetVendorInvoiceQueryHandler), typeof(DownloadVendorInvoiceFileQueryHandler),
        };
        foreach (var type in handlerTypes)
        {
            var dependencies = type.GetConstructors().Single().GetParameters().Select(p => p.ParameterType);
            Assert.DoesNotContain(typeof(ITransactionRepository), dependencies);
        }
    }

    // ---- list / get ------------------------------------------------------------------

    [Fact]
    public async Task List_ForAFarm_ReturnsAllNewestFirst_WithOrderFigures()
    {
        var h = new Harness();
        var second = h.NewOrder(OrderStatus.Received, number: 1002);
        await h.Upload.Handle(Cmd(h, fields: Fields(number: "A-1")), default);
        await Task.Delay(5);
        await h.Upload.Handle(Cmd(h, second, fields: Fields(number: "A-2")), default);

        var list = await h.List.Handle(new GetVendorInvoicesQuery("farm-1", null, null), default);

        Assert.Equal(["A-2", "A-1"], list.Select(v => v.InvoiceNumber));
        Assert.Equal(["ORD-1002", "ORD-1001"], list.Select(v => v.OrderReference));
    }

    [Fact]
    public async Task List_ForAnOrder_OnlyThatOrdersBills()
    {
        var h = new Harness();
        var second = h.NewOrder(OrderStatus.Received, number: 1002);
        await h.Upload.Handle(Cmd(h, fields: Fields(number: "A-1")), default);
        await h.Upload.Handle(Cmd(h, second, fields: Fields(number: "A-2")), default);

        var list = await h.List.Handle(new GetVendorInvoicesQuery("farm-1", second.Id, null), default);

        Assert.Equal(["A-2"], list.Select(v => v.InvoiceNumber));
    }

    [Fact]
    public async Task List_ForAnOrderOfAnotherFarm_Is404()
    {
        var h = new Harness();
        var foreign = h.NewOrder(OrderStatus.Received, farmId: "farm-OTHER", number: 9);
        await Assert.ThrowsAsync<NotFoundException>(() => h.List.Handle(new GetVendorInvoicesQuery("farm-1", foreign.Id, null), default));
    }

    [Fact]
    public async Task List_FiltersByStatus()
    {
        var h = new Harness();
        var a = await h.Upload.Handle(Cmd(h, fields: Fields(number: "A-1")), default);
        await h.Upload.Handle(Cmd(h, fields: Fields(number: "A-2")), default);
        await h.SetStatus.Handle(new SetVendorInvoiceStatusCommand("farm-1", a.Id, new SetVendorInvoiceStatusRequest("Paid", null)), default);

        var paid = await h.List.Handle(new GetVendorInvoicesQuery("farm-1", null, "paid"), default);
        var unpaid = await h.List.Handle(new GetVendorInvoicesQuery("farm-1", null, "Unpaid"), default);

        Assert.Equal(["A-1"], paid.Select(v => v.InvoiceNumber));
        Assert.Equal(["A-2"], unpaid.Select(v => v.InvoiceNumber));
    }

    [Fact]
    public async Task List_UnknownStatusFilter_Is422()
    {
        var h = new Harness();
        await Assert.ThrowsAsync<DomainException>(() => h.List.Handle(new GetVendorInvoicesQuery("farm-1", null, "maybe"), default));
    }

    [Fact]
    public async Task List_DoesNotLeakOtherFarmsBills()
    {
        var h = new Harness();
        await h.Upload.Handle(Cmd(h), default);
        var second = Farm.Create("Second", "co-1", "owner-1", id: "farm-2");
        h.Farms.Farms.Add(second);

        Assert.Empty(await h.List.Handle(new GetVendorInvoicesQuery("farm-2", null, null), default));
    }

    [Fact]
    public async Task List_AndGet_AreOpenToEveryRoleOfTheCompany_IncludingMember()
    {
        var h = new Harness();
        var dto = await h.Upload.Handle(Cmd(h), default);
        var member = new FakeUser(UserRole.Member);

        var list = await new GetVendorInvoicesQueryHandler(h.Farms, h.Orders, h.Bills, member).Handle(new GetVendorInvoicesQuery("farm-1", null, null), default);
        var one = await new GetVendorInvoiceQueryHandler(h.Farms, h.Orders, h.Bills, member).Handle(new GetVendorInvoiceQuery("farm-1", dto.Id), default);

        Assert.Single(list);
        Assert.Equal(dto.Id, one.Id);
    }

    [Fact]
    public async Task List_OtherCompany_Is404()
    {
        var h = new Harness(companyId: "other");
        await Assert.ThrowsAsync<NotFoundException>(() => h.List.Handle(new GetVendorInvoicesQuery("farm-1", null, null), default));
    }

    [Fact]
    public async Task Get_ShowsOverdue_WhenUnpaidAndPastDue()
    {
        var h = new Harness();
        var dto = await h.Upload.Handle(Cmd(h, fields: Fields(date: new DateOnly(2020, 1, 1), due: new DateOnly(2020, 1, 31))), default);
        Assert.True(dto.IsOverdue);

        var paid = await h.SetStatus.Handle(new SetVendorInvoiceStatusCommand("farm-1", dto.Id, new SetVendorInvoiceStatusRequest("Paid", null)), default);
        Assert.False(paid.IsOverdue);
    }

    [Fact]
    public async Task Get_UnknownOrAnotherFarms_Is404()
    {
        var h = new Harness();
        var dto = await h.Upload.Handle(Cmd(h), default);
        h.Farms.Farms.Add(Farm.Create("Second", "co-1", "owner-1", id: "farm-2"));

        await Assert.ThrowsAsync<NotFoundException>(() => h.Get.Handle(new GetVendorInvoiceQuery("farm-1", "nope"), default));
        await Assert.ThrowsAsync<NotFoundException>(() => h.Get.Handle(new GetVendorInvoiceQuery("farm-2", dto.Id), default));
    }

    // ---- download ---------------------------------------------------------------------

    [Fact]
    public async Task Download_ReturnsTheStoredBytes_WithTypeAndName()
    {
        var h = new Harness();
        var dto = await h.Upload.Handle(Cmd(h, name: "Saro October.pdf"), default);

        var file = await h.Download.Handle(new DownloadVendorInvoiceFileQuery("farm-1", dto.Id), default);

        Assert.Equal(UploadedFileValidatorTests.Pdf, file.Content);
        Assert.Equal("application/pdf", file.ContentType);
        Assert.Equal("Saro October.pdf", file.FileName);
    }

    [Fact]
    public async Task Download_AnotherFarmsBill_Is404()
    {
        var h = new Harness();
        var dto = await h.Upload.Handle(Cmd(h), default);
        h.Farms.Farms.Add(Farm.Create("Second", "co-1", "owner-1", id: "farm-2"));
        await Assert.ThrowsAsync<NotFoundException>(() => h.Download.Handle(new DownloadVendorInvoiceFileQuery("farm-2", dto.Id), default));
    }

    [Fact]
    public async Task Download_WhenTheFileHasGone_Is404()
    {
        var h = new Harness();
        var dto = await h.Upload.Handle(Cmd(h), default);
        h.Files.Files.Clear();
        await Assert.ThrowsAsync<NotFoundException>(() => h.Download.Handle(new DownloadVendorInvoiceFileQuery("farm-1", dto.Id), default));
    }

    // ---- status ------------------------------------------------------------------------

    [Fact]
    public async Task SetStatus_PaidThenUnpaid_SetsAndClearsPaidAt()
    {
        var h = new Harness();
        var dto = await h.Upload.Handle(Cmd(h), default);
        var when = new DateTime(2026, 10, 20, 0, 0, 0, DateTimeKind.Utc);

        var paid = await h.SetStatus.Handle(new SetVendorInvoiceStatusCommand("farm-1", dto.Id, new SetVendorInvoiceStatusRequest("Paid", when)), default);
        Assert.Equal("Paid", paid.Status);
        Assert.Equal(when, paid.PaidAt);

        var unpaid = await h.SetStatus.Handle(new SetVendorInvoiceStatusCommand("farm-1", dto.Id, new SetVendorInvoiceStatusRequest("unpaid", null)), default);
        Assert.Equal("Unpaid", unpaid.Status);
        Assert.Null(unpaid.PaidAt);
    }

    [Fact]
    public async Task SetStatus_UnknownValue_Is422()
    {
        var h = new Harness();
        var dto = await h.Upload.Handle(Cmd(h), default);
        await Assert.ThrowsAsync<DomainException>(() =>
            h.SetStatus.Handle(new SetVendorInvoiceStatusCommand("farm-1", dto.Id, new SetVendorInvoiceStatusRequest("Void", null)), default));
    }

    [Theory]
    [InlineData(UserRole.Supervisor)]
    [InlineData(UserRole.Member)]
    public async Task SetStatus_AndDelete_RequireAManager(UserRole role)
    {
        var h = new Harness();
        var dto = await h.Upload.Handle(Cmd(h), default);
        var user = new FakeUser(role);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            new SetVendorInvoiceStatusCommandHandler(h.Farms, h.Orders, h.Bills, user).Handle(new SetVendorInvoiceStatusCommand("farm-1", dto.Id, new("Paid", null)), default));
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            new DeleteVendorInvoiceCommandHandler(h.Farms, h.Bills, h.Files, user).Handle(new DeleteVendorInvoiceCommand("farm-1", dto.Id), default));

        Assert.Single(h.Bills.Items);
        Assert.Equal(VendorInvoiceStatus.Unpaid, h.Bills.Items[0].Status);
    }

    [Fact]
    public async Task SetStatus_AnotherFarmsBill_Is404()
    {
        var h = new Harness();
        var dto = await h.Upload.Handle(Cmd(h), default);
        h.Farms.Farms.Add(Farm.Create("Second", "co-1", "owner-1", id: "farm-2"));
        await Assert.ThrowsAsync<NotFoundException>(() =>
            h.SetStatus.Handle(new SetVendorInvoiceStatusCommand("farm-2", dto.Id, new("Paid", null)), default));
    }

    // ---- delete -----------------------------------------------------------------------

    [Fact]
    public async Task Delete_RemovesTheRecordAndItsFile()
    {
        var h = new Harness();
        var dto = await h.Upload.Handle(Cmd(h), default);
        var fileId = h.Bills.Items[0].FileId;

        await h.Delete.Handle(new DeleteVendorInvoiceCommand("farm-1", dto.Id), default);

        Assert.Empty(h.Bills.Items);
        Assert.Empty(h.Files.Files);
        Assert.Equal([fileId], h.Files.Deleted);
    }

    [Fact]
    public async Task Delete_AllowsTheSameInvoiceNumberToBeUploadedAgain()
    {
        var h = new Harness();
        var dto = await h.Upload.Handle(Cmd(h), default);
        await h.Delete.Handle(new DeleteVendorInvoiceCommand("farm-1", dto.Id), default);
        Assert.NotNull(await h.Upload.Handle(Cmd(h), default));
    }

    [Fact]
    public async Task Delete_UnknownOrAnotherFarms_Is404()
    {
        var h = new Harness();
        var dto = await h.Upload.Handle(Cmd(h), default);
        h.Farms.Farms.Add(Farm.Create("Second", "co-1", "owner-1", id: "farm-2"));
        await Assert.ThrowsAsync<NotFoundException>(() => h.Delete.Handle(new DeleteVendorInvoiceCommand("farm-1", "nope"), default));
        await Assert.ThrowsAsync<NotFoundException>(() => h.Delete.Handle(new DeleteVendorInvoiceCommand("farm-2", dto.Id), default));
        Assert.Single(h.Bills.Items);
    }
}

public class VendorUploadRoleBeforeFileValidationTests
{
    [Fact]
    public async Task Member_SendingAnOversizedOrWrongFile_GetsForbidden_Not413Or415()
    {
        var farm = Farm.Create("NGF", "co-1", "owner-1", id: "farm-1");
        var supplier = Supplier.Create("farm-1", "Saro");
        var suppliers = new List<Supplier> { supplier };
        var orders = new List<Order>();
        var order = Order.Create("farm-1", supplier.Id, [new OrderLineItem("i", 1m, 1m)], 1001);
        order.Place(); order.Receive();
        var handler = new UploadVendorInvoiceCommandHandler(new FakeFarms(farm), new OrdersStub(order), new SuppliersStub(supplier), new FakeFiles(), new BillsStub(), new FakeUser(UserRole.Member));
        var fields = new UploadVendorInvoiceFields(null, "A-1", 1m, new DateOnly(2026, 1, 1), null, null);

        await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(new UploadVendorInvoiceCommand("farm-1", order.Id, new byte[11 * 1024 * 1024], "a.pdf", "application/pdf", fields), default));
        await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(new UploadVendorInvoiceCommand("farm-1", order.Id, "MZ"u8.ToArray(), "a.pdf", "application/pdf", fields), default));
    }

    private class OrdersStub(Order order) : IOrderRepository
    {
        public Task<Order?> GetByIdAsync(string id, CancellationToken ct = default) => Task.FromResult<Order?>(order);
        public Task<List<Order>> GetByFarmIdAsync(string farmId, OrderStatus? status = null, CancellationToken ct = default) => Task.FromResult(new List<Order> { order });
        public Task<Order?> GetByNumberAsync(string farmId, int number, CancellationToken ct = default) => Task.FromResult<Order?>(order);
        public Task<int> GetNextNumberAsync(string farmId, CancellationToken ct = default) => Task.FromResult(1);
        public Task<Order> AddAsync(Order o, CancellationToken ct = default) => Task.FromResult(o);
        public Task UpdateAsync(Order o, CancellationToken ct = default) => Task.CompletedTask;
    }

    private class SuppliersStub(Supplier supplier) : ISupplierRepository
    {
        public Task<Supplier?> GetByIdAsync(string id, CancellationToken ct = default) => Task.FromResult<Supplier?>(supplier);
        public Task<List<Supplier>> GetByFarmIdAsync(string farmId, CancellationToken ct = default) => Task.FromResult(new List<Supplier> { supplier });
        public Task<Supplier> AddAsync(Supplier s, CancellationToken ct = default) => Task.FromResult(s);
    }

    private class BillsStub : IVendorInvoiceRepository
    {
        public Task<VendorInvoice?> GetByIdAsync(string id, CancellationToken ct = default) => Task.FromResult<VendorInvoice?>(null);
        public Task<List<VendorInvoice>> GetByFarmIdAsync(string farmId, string? orderId = null, VendorInvoiceStatus? status = null, CancellationToken ct = default) => Task.FromResult(new List<VendorInvoice>());
        public Task<bool> ExistsAsync(string farmId, string supplierId, string invoiceNumber, CancellationToken ct = default) => Task.FromResult(false);
        public Task<VendorInvoice> AddAsync(VendorInvoice invoice, CancellationToken ct = default) => Task.FromResult(invoice);
        public Task UpdateAsync(VendorInvoice invoice, CancellationToken ct = default) => Task.CompletedTask;
        public Task DeleteAsync(VendorInvoice invoice, CancellationToken ct = default) => Task.CompletedTask;
    }
}
