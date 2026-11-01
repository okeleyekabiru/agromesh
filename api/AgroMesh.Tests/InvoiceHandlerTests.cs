using AgroMesh.Application.Commands.Invoice;
using AgroMesh.Application.DTOs;
using AgroMesh.Application.Interfaces;
using AgroMesh.Application.Queries.Invoice;
using AgroMesh.Domain.Entities;
using AgroMesh.Domain.Enums;
using AgroMesh.Domain.Exceptions;
using AgroMesh.Tests.Support;

namespace AgroMesh.Tests;

public class InvoiceHandlerTests
{
    internal class FakeInvoices : IInvoiceRepository
    {
        public List<Invoice> Invoices = [];

        public Task<Invoice?> GetByIdAsync(string id, CancellationToken ct = default) => Task.FromResult(Invoices.FirstOrDefault(i => i.Id == id));
        public Task<List<Invoice>> GetByFarmIdAsync(string farmId, CancellationToken ct = default) =>
            Task.FromResult(Invoices.Where(i => i.FarmId == farmId).OrderByDescending(i => i.Number).ToList());
        // Like the real counter: a number handed out is never handed out again, whatever happens to the invoice.
        public Dictionary<string, int> Counters = [];
        public int Reservations;
        public Task<int> ReserveNextNumberAsync(string farmId, CancellationToken ct = default)
        {
            Reservations++;
            Counters[farmId] = Counters.GetValueOrDefault(farmId) + 1;
            return Task.FromResult(Counters[farmId]);
        }
        public Task<Invoice> AddAsync(Invoice invoice, CancellationToken ct = default) { Invoices.Add(invoice); return Task.FromResult(invoice); }
        public Task UpdateAsync(Invoice invoice, CancellationToken ct = default) => Task.CompletedTask;
        public Task DeleteAsync(Invoice invoice, CancellationToken ct = default) { Invoices.Remove(invoice); return Task.CompletedTask; }
    }

    internal class FakeRegistry : IInvoiceTemplateRegistry
    {
        public InvoiceRenderModel? LastModel;
        public string? LastKey;
        public IReadOnlyList<InvoiceTemplateInfo> Templates { get; } =
            [new InvoiceTemplateInfo("ngf", "NGF - Invoice", "Nature's Gold Farm"), new InvoiceTemplateInfo("plain", "Plain", "A plain layout")];
        public bool Exists(string key) => Templates.Any(t => t.Key == key);
        public byte[] Render(string key, InvoiceRenderModel model) { LastKey = key; LastModel = model; return "%PDF-fake"u8.ToArray(); }
    }

    private static Farm NewFarm(string companyId = "co-1")
    {
        var farm = Farm.Create("Nature's Gold Farm", companyId, "owner-1", id: "farm-1");
        farm.SetBusinessProfile("12 Farm Road", "+234 801", "hello@ngf.ng", "www.ngf.ng", "NGF Account", "0123456789", "GTBank");
        return farm;
    }

    private class Harness
    {
        public FakeFarms Farms;
        public FakeInvoices Invoices = new();
        public FakeFiles Files = new();
        public FakeRegistry Registry = new();
        public FakeUser User;
        public Farm Farm;

        public Harness(UserRole role = UserRole.Admin, string? companyId = "co-1", Farm? farm = null)
        {
            Farm = farm ?? NewFarm();
            Farms = new FakeFarms(Farm);
            User = new FakeUser(role, companyId);
        }

        public CreateInvoiceCommandHandler Create => new(Farms, Invoices, Registry, User);
        public UpdateInvoiceCommandHandler Update => new(Farms, Invoices, Registry, User);
        public DeleteInvoiceCommandHandler Delete => new(Farms, Invoices, User);
        public GetInvoiceQueryHandler Get => new(Farms, Invoices, User);
        public GetInvoicesQueryHandler List => new(Farms, Invoices, User);
        public GetInvoiceTemplatesQueryHandler Templates => new(Farms, Registry, User);
        public GenerateInvoicePdfQueryHandler Pdf => new(Farms, Invoices, Files, Registry, User);
    }

    private static SaveInvoiceRequest Request(
        string billTo = "Acme Foods Ltd", DateOnly? date = null, DateOnly? due = null, decimal? vat = null,
        string? template = null, string? accountName = null, string? accountNumber = null, string? bank = null,
        List<InvoiceLineItemRequest>? items = null) =>
        new(date, due, billTo, "0801", "buyer@acme.ng", "5 Market Road",
            items ?? [new InvoiceLineItemRequest("Pineapples", 1500m, 2m), new InvoiceLineItemRequest("Delivery", 500m, 1m)],
            vat, accountName, accountNumber, bank, "Pay in 14 days", "A. Farmer", template);

    // ---- create ------------------------------------------------------------------

    [Fact]
    public async Task Create_ReturnsFullDto_WithComputedTotalsAndSerialNumbers()
    {
        var h = new Harness();

        var dto = await h.Create.Handle(new CreateInvoiceCommand("farm-1", Request(vat: 7.5m, date: new DateOnly(2026, 10, 1), due: new DateOnly(2026, 10, 20))), default);

        Assert.Equal(1, dto.Number);
        Assert.Equal("INV-0001", dto.Reference);
        Assert.Equal("farm-1", dto.FarmId);
        Assert.Equal("Acme Foods Ltd", dto.BillToName);
        Assert.Equal(2, dto.LineItems.Count);
        Assert.Equal([1, 2], dto.LineItems.Select(l => l.SerialNumber));
        Assert.Equal(3000m, dto.LineItems[0].Amount);
        Assert.Equal(3500m, dto.Subtotal);
        Assert.Equal(7.5m, dto.VatPercent);
        Assert.Equal(262.50m, dto.VatAmount);
        Assert.Equal(3762.50m, dto.Total);
        Assert.Equal(new DateOnly(2026, 10, 1), dto.Date);
        Assert.Equal(new DateOnly(2026, 10, 20), dto.DueDate);
        Assert.Equal("ngf", dto.TemplateKey);
        Assert.Single(h.Invoices.Invoices);
        Assert.Equal("user-1", h.Invoices.Invoices[0].CreatedByUserId);
    }

    [Fact]
    public async Task Create_DefaultsDateToToday_DueDateTo14DaysLater_VatToZero()
    {
        var h = new Harness();
        var before = DateOnly.FromDateTime(DateTime.UtcNow);

        var dto = await h.Create.Handle(new CreateInvoiceCommand("farm-1", Request()), default);

        Assert.True(dto.Date >= before && dto.Date <= before.AddDays(1));
        Assert.Equal(dto.Date.AddDays(14), dto.DueDate);
        Assert.Equal(0m, dto.VatPercent);
    }

    [Fact]
    public async Task Create_NumbersAreSequentialPerFarm()
    {
        var h = new Harness();
        var a = await h.Create.Handle(new CreateInvoiceCommand("farm-1", Request()), default);
        var b = await h.Create.Handle(new CreateInvoiceCommand("farm-1", Request()), default);

        Assert.Equal([1, 2], new[] { a.Number, b.Number });
    }

    [Fact]
    public async Task Create_NumbersAreIndependentBetweenFarms()
    {
        var h = new Harness();
        var farm2 = Farm.Create("Second", "co-1", "owner-1", id: "farm-2");
        h.Farms.Farms.Add(farm2);

        await h.Create.Handle(new CreateInvoiceCommand("farm-1", Request()), default);
        await h.Create.Handle(new CreateInvoiceCommand("farm-1", Request()), default);
        var other = await h.Create.Handle(new CreateInvoiceCommand("farm-2", Request()), default);

        Assert.Equal(1, other.Number);
    }

    [Fact]
    public async Task Create_DeletedInvoiceNumbersAreNotReusedWhileAHigherOneExists()
    {
        var h = new Harness();
        await h.Create.Handle(new CreateInvoiceCommand("farm-1", Request()), default);
        var second = await h.Create.Handle(new CreateInvoiceCommand("farm-1", Request()), default);
        var third = await h.Create.Handle(new CreateInvoiceCommand("farm-1", Request()), default);
        await h.Delete.Handle(new DeleteInvoiceCommand("farm-1", second.Id), default);

        var next = await h.Create.Handle(new CreateInvoiceCommand("farm-1", Request()), default);

        Assert.Equal(4, next.Number);
        Assert.Equal(3, third.Number);
    }

    [Fact]
    public async Task Create_DeletingTheNewestInvoice_NeverFreesItsNumberForReuse()
    {
        var h = new Harness();
        await h.Create.Handle(new CreateInvoiceCommand("farm-1", Request()), default);
        var newest = await h.Create.Handle(new CreateInvoiceCommand("farm-1", Request()), default);
        await h.Delete.Handle(new DeleteInvoiceCommand("farm-1", newest.Id), default);

        var next = await h.Create.Handle(new CreateInvoiceCommand("farm-1", Request()), default);

        Assert.Equal(2, newest.Number);
        Assert.Equal(3, next.Number);
    }

    [Fact]
    public async Task Create_InvalidInvoice_DoesNotUseUpANumber()
    {
        var h = new Harness();
        await Assert.ThrowsAsync<DomainException>(() => h.Create.Handle(new CreateInvoiceCommand("farm-1", Request(billTo: " ")), default));
        Assert.Equal(0, h.Invoices.Reservations);
        Assert.Equal(1, (await h.Create.Handle(new CreateInvoiceCommand("farm-1", Request()), default)).Number);
    }

    [Fact]
    public async Task Create_PaymentBlockDefaultsFromTheFarmProfile_WhenAllThreeAreBlank()
    {
        var h = new Harness();
        var dto = await h.Create.Handle(new CreateInvoiceCommand("farm-1", Request()), default);

        Assert.Equal("NGF Account", dto.PaymentAccountName);
        Assert.Equal("0123456789", dto.PaymentAccountNumber);
        Assert.Equal("GTBank", dto.PaymentBankName);
    }

    [Fact]
    public async Task Create_ExplicitPaymentBlock_WinsOverTheFarmProfile()
    {
        var h = new Harness();
        var dto = await h.Create.Handle(new CreateInvoiceCommand("farm-1", Request(accountName: "Other", accountNumber: "999", bank: "Zenith")), default);

        Assert.Equal("Other", dto.PaymentAccountName);
        Assert.Equal("999", dto.PaymentAccountNumber);
        Assert.Equal("Zenith", dto.PaymentBankName);
    }

    [Fact]
    public async Task Create_PaymentBlockIsASnapshot_LaterFarmChangesDoNotRewriteIt()
    {
        var h = new Harness();
        var dto = await h.Create.Handle(new CreateInvoiceCommand("farm-1", Request()), default);

        h.Farm.SetBusinessProfile(null, null, null, null, "Changed", "000", "Other Bank");
        var loaded = await h.Get.Handle(new GetInvoiceQuery("farm-1", dto.Id), default);

        Assert.Equal("0123456789", loaded.PaymentAccountNumber);
    }

    [Fact]
    public async Task Create_UnknownTemplate_IsRejectedAndNothingSaved()
    {
        var h = new Harness();
        var ex = await Assert.ThrowsAsync<DomainException>(() => h.Create.Handle(new CreateInvoiceCommand("farm-1", Request(template: "fancy")), default));
        Assert.Contains("ngf", ex.Message);
        Assert.Empty(h.Invoices.Invoices);
    }

    [Fact]
    public async Task Create_AnotherRegisteredTemplate_IsAccepted_CaseInsensitively()
    {
        var h = new Harness();
        var dto = await h.Create.Handle(new CreateInvoiceCommand("farm-1", Request(template: "PLAIN")), default);
        Assert.Equal("plain", dto.TemplateKey);
    }

    [Fact]
    public async Task Create_DomainRuleViolations_Propagate()
    {
        var h = new Harness();
        await Assert.ThrowsAsync<DomainException>(() => h.Create.Handle(new CreateInvoiceCommand("farm-1", Request(billTo: " ")), default));
        await Assert.ThrowsAsync<DomainException>(() => h.Create.Handle(new CreateInvoiceCommand("farm-1", Request(items: [])), default));
        await Assert.ThrowsAsync<DomainException>(() => h.Create.Handle(new CreateInvoiceCommand("farm-1", Request(vat: 101m)), default));
        Assert.Empty(h.Invoices.Invoices);
    }

    [Theory]
    [InlineData(UserRole.Supervisor)]
    [InlineData(UserRole.Member)]
    public async Task Create_NonManagersAreForbidden(UserRole role)
    {
        var h = new Harness(role);
        await Assert.ThrowsAsync<ForbiddenException>(() => h.Create.Handle(new CreateInvoiceCommand("farm-1", Request()), default));
        Assert.Empty(h.Invoices.Invoices);
    }

    [Fact]
    public async Task Create_FarmManagerIsAllowed()
    {
        var h = new Harness(UserRole.FarmManager);
        Assert.Equal(1, (await h.Create.Handle(new CreateInvoiceCommand("farm-1", Request()), default)).Number);
    }

    [Fact]
    public async Task Create_OtherCompanysFarm_Is404()
    {
        var h = new Harness(farm: NewFarm("other"));
        await Assert.ThrowsAsync<NotFoundException>(() => h.Create.Handle(new CreateInvoiceCommand("farm-1", Request()), default));
    }

    // ---- update ------------------------------------------------------------------

    [Fact]
    public async Task Update_ReplacesFields_KeepsNumberAndCreator()
    {
        var h = new Harness();
        var created = await h.Create.Handle(new CreateInvoiceCommand("farm-1", Request(date: new DateOnly(2026, 10, 1), due: new DateOnly(2026, 10, 15))), default);

        var updated = await h.Update.Handle(new UpdateInvoiceCommand("farm-1", created.Id,
            Request(billTo: "New Buyer", vat: 5m, date: new DateOnly(2026, 10, 2), due: new DateOnly(2026, 10, 30),
                    items: [new InvoiceLineItemRequest("Only item", 100m, 1m)],
                    accountName: "A", accountNumber: "1", bank: "B")), default);

        Assert.Equal(created.Id, updated.Id);
        Assert.Equal(created.Number, updated.Number);
        Assert.Equal("New Buyer", updated.BillToName);
        Assert.Single(updated.LineItems);
        Assert.Equal(105m, updated.Total);
        Assert.Equal(new DateOnly(2026, 10, 30), updated.DueDate);
    }

    [Fact]
    public async Task Update_OmittedDates_KeepTheExistingOnes()
    {
        var h = new Harness();
        var created = await h.Create.Handle(new CreateInvoiceCommand("farm-1", Request(date: new DateOnly(2026, 10, 1), due: new DateOnly(2026, 10, 15))), default);

        var updated = await h.Update.Handle(new UpdateInvoiceCommand("farm-1", created.Id, Request(billTo: "X")), default);

        Assert.Equal(new DateOnly(2026, 10, 1), updated.Date);
        Assert.Equal(new DateOnly(2026, 10, 15), updated.DueDate);
    }

    [Fact]
    public async Task Update_BlankPaymentBlock_IsCleared_NotRefilledFromTheFarm()
    {
        var h = new Harness();
        var created = await h.Create.Handle(new CreateInvoiceCommand("farm-1", Request()), default);

        var updated = await h.Update.Handle(new UpdateInvoiceCommand("farm-1", created.Id, Request()), default);

        Assert.Null(updated.PaymentAccountNumber);
    }

    [Fact]
    public async Task Update_InvalidChange_LeavesTheInvoiceUntouched()
    {
        var h = new Harness();
        var created = await h.Create.Handle(new CreateInvoiceCommand("farm-1", Request()), default);

        await Assert.ThrowsAsync<DomainException>(() => h.Update.Handle(new UpdateInvoiceCommand("farm-1", created.Id, Request(billTo: "")), default));

        Assert.Equal("Acme Foods Ltd", h.Invoices.Invoices[0].BillToName);
    }

    [Fact]
    public async Task Update_UnknownTemplate_IsRejected()
    {
        var h = new Harness();
        var created = await h.Create.Handle(new CreateInvoiceCommand("farm-1", Request()), default);
        await Assert.ThrowsAsync<DomainException>(() => h.Update.Handle(new UpdateInvoiceCommand("farm-1", created.Id, Request(template: "zzz")), default));
    }

    [Fact]
    public async Task Update_SupervisorIsForbidden()
    {
        var h = new Harness();
        var created = await h.Create.Handle(new CreateInvoiceCommand("farm-1", Request()), default);
        var sup = new UpdateInvoiceCommandHandler(h.Farms, h.Invoices, h.Registry, new FakeUser(UserRole.Supervisor));
        await Assert.ThrowsAsync<ForbiddenException>(() => sup.Handle(new UpdateInvoiceCommand("farm-1", created.Id, Request()), default));
    }

    // ---- read / list / delete ------------------------------------------------------

    [Fact]
    public async Task Get_UnknownInvoice_Is404()
    {
        var h = new Harness();
        await Assert.ThrowsAsync<NotFoundException>(() => h.Get.Handle(new GetInvoiceQuery("farm-1", "nope"), default));
    }

    [Fact]
    public async Task Get_InvoiceOfAnotherFarm_Is404_EvenInTheSameCompany()
    {
        var h = new Harness();
        var farm2 = Farm.Create("Second", "co-1", "owner-1", id: "farm-2");
        h.Farms.Farms.Add(farm2);
        var created = await h.Create.Handle(new CreateInvoiceCommand("farm-2", Request()), default);

        await Assert.ThrowsAsync<NotFoundException>(() => h.Get.Handle(new GetInvoiceQuery("farm-1", created.Id), default));
    }

    [Theory]
    [InlineData(UserRole.Supervisor)]
    [InlineData(UserRole.Member)]
    public async Task Get_AndList_AreOpenToEveryRoleOfTheCompany(UserRole role)
    {
        var h = new Harness();
        var created = await h.Create.Handle(new CreateInvoiceCommand("farm-1", Request()), default);
        var user = new FakeUser(role);

        var get = await new GetInvoiceQueryHandler(h.Farms, h.Invoices, user).Handle(new GetInvoiceQuery("farm-1", created.Id), default);
        var list = await new GetInvoicesQueryHandler(h.Farms, h.Invoices, user).Handle(new GetInvoicesQuery("farm-1"), default);

        Assert.Equal(created.Id, get.Id);
        Assert.Single(list);
    }

    [Fact]
    public async Task List_IsNewestFirst_AndSummarisesTotals()
    {
        var h = new Harness();
        await h.Create.Handle(new CreateInvoiceCommand("farm-1", Request(billTo: "First")), default);
        await h.Create.Handle(new CreateInvoiceCommand("farm-1", Request(billTo: "Second", vat: 10m)), default);

        var list = await h.List.Handle(new GetInvoicesQuery("farm-1"), default);

        Assert.Equal(["Second", "First"], list.Select(i => i.BillToName));
        Assert.Equal(3850m, list[0].Total);
        Assert.Equal("INV-0002", list[0].Reference);
    }

    [Fact]
    public async Task List_OnlyReturnsTheRequestedFarm()
    {
        var h = new Harness();
        h.Farms.Farms.Add(Farm.Create("Second", "co-1", "owner-1", id: "farm-2"));
        await h.Create.Handle(new CreateInvoiceCommand("farm-1", Request()), default);
        await h.Create.Handle(new CreateInvoiceCommand("farm-2", Request()), default);

        Assert.Single(await h.List.Handle(new GetInvoicesQuery("farm-2"), default));
    }

    [Fact]
    public async Task List_OtherCompanysFarm_Is404()
    {
        var h = new Harness(farm: NewFarm("other"));
        await Assert.ThrowsAsync<NotFoundException>(() => h.List.Handle(new GetInvoicesQuery("farm-1"), default));
    }

    [Fact]
    public async Task Delete_RemovesTheInvoice()
    {
        var h = new Harness();
        var created = await h.Create.Handle(new CreateInvoiceCommand("farm-1", Request()), default);
        await h.Delete.Handle(new DeleteInvoiceCommand("farm-1", created.Id), default);
        Assert.Empty(h.Invoices.Invoices);
    }

    [Fact]
    public async Task Delete_SupervisorIsForbidden_AndUnknownIs404()
    {
        var h = new Harness();
        var created = await h.Create.Handle(new CreateInvoiceCommand("farm-1", Request()), default);
        var sup = new DeleteInvoiceCommandHandler(h.Farms, h.Invoices, new FakeUser(UserRole.Supervisor));
        await Assert.ThrowsAsync<ForbiddenException>(() => sup.Handle(new DeleteInvoiceCommand("farm-1", created.Id), default));
        await Assert.ThrowsAsync<NotFoundException>(() => h.Delete.Handle(new DeleteInvoiceCommand("farm-1", "nope"), default));
        Assert.Single(h.Invoices.Invoices);
    }

    // ---- templates & pdf -------------------------------------------------------------

    [Fact]
    public async Task Templates_ListsTheRegisteredOnes()
    {
        var h = new Harness();
        var list = await h.Templates.Handle(new GetInvoiceTemplatesQuery("farm-1"), default);
        Assert.Contains(list, t => t.Key == "ngf" && t.Name == "NGF - Invoice");
        Assert.Equal(2, list.Count);
    }

    [Fact]
    public async Task Pdf_UsesTheInvoicesTemplate_AndFarmLetterhead()
    {
        var h = new Harness();
        var created = await h.Create.Handle(new CreateInvoiceCommand("farm-1", Request(template: "plain")), default);

        var file = await h.Pdf.Handle(new GenerateInvoicePdfQuery("farm-1", created.Id), default);

        Assert.Equal("plain", h.Registry.LastKey);
        Assert.Equal("INV-0001.pdf", file.FileName);
        Assert.StartsWith("%PDF", System.Text.Encoding.ASCII.GetString(file.Content));
        var issuer = h.Registry.LastModel!.Issuer;
        Assert.Equal("Nature's Gold Farm", issuer.Name);
        Assert.Equal("12 Farm Road", issuer.Address);
        Assert.Equal("+234 801", issuer.Phone);
        Assert.Equal("hello@ngf.ng", issuer.Email);
        Assert.Equal("www.ngf.ng", issuer.Website);
        Assert.Equal(created.Id, h.Registry.LastModel.Invoice.Id);
    }

    [Fact]
    public async Task Pdf_EmbedsTheFarmLogoBytes_WhenThereIsOne()
    {
        var h = new Harness();
        var logo = StoredFile.Create("farm-1", "logo.png", "image/png", UploadedFileValidatorTests.Png);
        await h.Files.AddAsync(logo);
        h.Farm.SetLogo(logo.Id);
        var created = await h.Create.Handle(new CreateInvoiceCommand("farm-1", Request()), default);

        await h.Pdf.Handle(new GenerateInvoicePdfQuery("farm-1", created.Id), default);

        Assert.Equal(UploadedFileValidatorTests.Png, h.Registry.LastModel!.Issuer.Logo);
    }

    [Fact]
    public async Task Pdf_WithoutALogo_RendersWithNull()
    {
        var h = new Harness();
        var created = await h.Create.Handle(new CreateInvoiceCommand("farm-1", Request()), default);
        await h.Pdf.Handle(new GenerateInvoicePdfQuery("farm-1", created.Id), default);
        Assert.Null(h.Registry.LastModel!.Issuer.Logo);
    }

    [Fact]
    public async Task Pdf_LogoPointingAtAMissingFile_StillRenders()
    {
        var h = new Harness();
        h.Farm.SetLogo("ghost");
        var created = await h.Create.Handle(new CreateInvoiceCommand("farm-1", Request()), default);
        await h.Pdf.Handle(new GenerateInvoicePdfQuery("farm-1", created.Id), default);
        Assert.Null(h.Registry.LastModel!.Issuer.Logo);
    }

    [Fact]
    public async Task Pdf_InvoiceOfAnotherFarm_Is404()
    {
        var h = new Harness();
        h.Farms.Farms.Add(Farm.Create("Second", "co-1", "owner-1", id: "farm-2"));
        var created = await h.Create.Handle(new CreateInvoiceCommand("farm-2", Request()), default);
        await Assert.ThrowsAsync<NotFoundException>(() => h.Pdf.Handle(new GenerateInvoicePdfQuery("farm-1", created.Id), default));
    }

    [Fact]
    public async Task Pdf_InvoiceWithATemplateThatNoLongerExists_ExplainsItself()
    {
        var h = new Harness();
        var created = await h.Create.Handle(new CreateInvoiceCommand("farm-1", Request(template: "plain")), default);
        h.Registry = new FakeRegistry();
        var gone = new GenerateInvoicePdfQueryHandler(h.Farms, h.Invoices, h.Files, new NoTemplates(), h.User);
        await Assert.ThrowsAsync<DomainException>(() => gone.Handle(new GenerateInvoicePdfQuery("farm-1", created.Id), default));
    }

    private class NoTemplates : IInvoiceTemplateRegistry
    {
        public IReadOnlyList<InvoiceTemplateInfo> Templates => [];
        public bool Exists(string key) => false;
        public byte[] Render(string key, InvoiceRenderModel model) => throw new InvalidOperationException();
    }
}
