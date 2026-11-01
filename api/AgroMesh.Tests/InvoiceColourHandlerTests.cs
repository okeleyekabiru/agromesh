using AgroMesh.Application.Commands.Invoice;
using AgroMesh.Application.DTOs;
using AgroMesh.Application.Interfaces;
using AgroMesh.Application.Queries.Invoice;
using AgroMesh.Domain.Entities;
using AgroMesh.Domain.Enums;
using AgroMesh.Domain.Exceptions;
using AgroMesh.Tests.Support;

namespace AgroMesh.Tests;

public class InvoiceColourHandlerTests
{
    private static Farm NewFarm(string company = "co-1", string? primary = null, string? accent = null)
    {
        var farm = Farm.Create("Nature Gold Farm", company, "owner-1", id: "farm-1");
        farm.SetBusinessProfile("12 Farm Road", "+234 801", "hello@ngf.ng", "www.ngf.ng", "NGF Account", "0123456789", "GTBank", primary, accent);
        return farm;
    }

    private class H
    {
        public FakeFarms Farms;
        public InvoiceHandlerTests.FakeInvoices Invoices = new();
        public FakeFiles Files = new();
        public InvoiceHandlerTests.FakeRegistry Registry = new();
        public FakeUser User;
        public H(Farm farm, UserRole role = UserRole.Admin, string? company = "co-1") { Farms = new FakeFarms(farm); User = new FakeUser(role, company); }
        public CreateInvoiceCommandHandler Create => new(Farms, Invoices, Registry, User);
        public UpdateInvoiceCommandHandler Update => new(Farms, Invoices, Registry, User);
        public GetInvoiceQueryHandler Get => new(Farms, Invoices, Registry, User);
        public GenerateInvoicePdfQueryHandler Pdf => new(Farms, Invoices, Files, Registry, User);
        public GetInvoiceTemplatesQueryHandler Templates => new(Farms, Registry, User);
        public GenerateInvoicePreviewQueryHandler Preview => new(Farms, Files, Registry, User);
    }

    private static SaveInvoiceRequest Req(string? primary = null, string? accent = null, string? template = null) =>
        new(null, null, "Acme", null, null, null, [new InvoiceLineItemRequest("x", 1m, 1m)], null, null, null, null, null, null, template, primary, accent);

    [Fact]
    public async Task Create_NoColours_ResolvesToTheTemplateDefaults_AndStoresNull()
    {
        var h = new H(NewFarm());
        var dto = await h.Create.Handle(new CreateInvoiceCommand("farm-1", Req()), default);

        Assert.Null(dto.PrimaryColor);
        Assert.Null(dto.AccentColor);
        Assert.Equal("#138A44", dto.ResolvedPrimaryColor);
        Assert.Equal("#F5C90C", dto.ResolvedAccentColor);
        Assert.Equal("default", dto.PrimaryColorSource);
        Assert.Equal("default", dto.AccentColorSource);
        Assert.Null(h.Invoices.Invoices.Single().PrimaryColor);
    }

    [Fact]
    public async Task Create_FarmBrand_IsUsed_ButNotCopiedOntoTheInvoice()
    {
        var h = new H(NewFarm(primary: "#1B2A6B", accent: "#E67E22"));
        var dto = await h.Create.Handle(new CreateInvoiceCommand("farm-1", Req()), default);

        Assert.Null(dto.PrimaryColor);
        Assert.Equal("#1B2A6B", dto.ResolvedPrimaryColor);
        Assert.Equal("#E67E22", dto.ResolvedAccentColor);
        Assert.Equal("farm", dto.PrimaryColorSource);
        Assert.Null(h.Invoices.Invoices.Single().PrimaryColor);
    }

    [Fact]
    public async Task Create_InvoiceColours_WinAndAreNormalised()
    {
        var h = new H(NewFarm(primary: "#1B2A6B", accent: "#E67E22"));
        var dto = await h.Create.Handle(new CreateInvoiceCommand("farm-1", Req("#c0392b", "#8e44ad")), default);

        Assert.Equal("#C0392B", dto.PrimaryColor);
        Assert.Equal("#8E44AD", dto.AccentColor);
        Assert.Equal("#C0392B", dto.ResolvedPrimaryColor);
        Assert.Equal("invoice", dto.PrimaryColorSource);
        Assert.Equal("invoice", dto.AccentColorSource);
    }

    [Fact]
    public async Task Create_TheResolvedDefaultsFollowTheChosenTemplate()
    {
        var h = new H(NewFarm());
        var dto = await h.Create.Handle(new CreateInvoiceCommand("farm-1", Req(template: "plain")), default);
        Assert.Equal("#222222", dto.ResolvedPrimaryColor);
        Assert.Equal("#888888", dto.ResolvedAccentColor);
    }

    [Theory]
    [InlineData("red")]
    [InlineData("#FFF")]
    [InlineData("#FFFFFF")]
    public async Task Create_BadColour_Is422_AndNothingSaved(string bad)
    {
        var h = new H(NewFarm());
        await Assert.ThrowsAsync<DomainException>(() => h.Create.Handle(new CreateInvoiceCommand("farm-1", Req(primary: bad)), default));
        await Assert.ThrowsAsync<DomainException>(() => h.Create.Handle(new CreateInvoiceCommand("farm-1", Req(accent: bad)), default));
        Assert.Empty(h.Invoices.Invoices);
        Assert.Equal(0, h.Invoices.Reservations);
    }

    [Fact]
    public async Task Update_NullClearsAnOverride_BackToFarmThenDefault()
    {
        var h = new H(NewFarm(primary: "#1B2A6B"));
        var created = await h.Create.Handle(new CreateInvoiceCommand("farm-1", Req("#C0392B", "#8E44AD")), default);

        var updated = await h.Update.Handle(new UpdateInvoiceCommand("farm-1", created.Id, Req()), default);

        Assert.Null(updated.PrimaryColor);
        Assert.Equal("#1B2A6B", updated.ResolvedPrimaryColor);
        Assert.Equal("farm", updated.PrimaryColorSource);
        Assert.Equal("default", updated.AccentColorSource);
    }

    [Fact]
    public async Task Get_ReflectsALaterChangeOfTheFarmBrand_ForInvoicesThatDidNotOverride()
    {
        var farm = NewFarm();
        var h = new H(farm);
        var created = await h.Create.Handle(new CreateInvoiceCommand("farm-1", Req(accent: "#8E44AD")), default);
        farm.SetBusinessProfile(null, null, null, null, null, null, null, "#1B2A6B", "#E67E22");

        var dto = await h.Get.Handle(new GetInvoiceQuery("farm-1", created.Id), default);

        Assert.Equal("#1B2A6B", dto.ResolvedPrimaryColor);   // follows the farm
        Assert.Equal("#8E44AD", dto.ResolvedAccentColor);    // kept its own
    }

    [Fact]
    public async Task Pdf_IsRenderedWithTheResolvedColours()
    {
        var h = new H(NewFarm(primary: "#1B2A6B"));
        var created = await h.Create.Handle(new CreateInvoiceCommand("farm-1", Req(accent: "#8E44AD")), default);

        await h.Pdf.Handle(new GenerateInvoicePdfQuery("farm-1", created.Id), default);

        Assert.Equal(new InvoiceTheme("#1B2A6B", "#8E44AD"), h.Registry.LastModel!.Theme);
    }

    [Fact]
    public async Task Templates_ListDefaultsAndPreviewRoute()
    {
        var h = new H(NewFarm());
        var list = await h.Templates.Handle(new GetInvoiceTemplatesQuery("farm-1"), default);

        var ngf = list.Single(t => t.Key == "ngf");
        Assert.Equal("#138A44", ngf.DefaultPrimaryColor);
        Assert.Equal("#F5C90C", ngf.DefaultAccentColor);
        Assert.Equal("/api/farms/farm-1/invoice-templates/ngf/preview", ngf.PreviewUrl);
        Assert.True(ngf.IsDefault);
    }

    // ---- preview ---------------------------------------------------------------------------------------------

    [Fact]
    public async Task Preview_UsesSampleData_TheFarmLogoAndProfile_AndSavesNothing()
    {
        var farm = NewFarm();
        var h = new H(farm);
        var logo = StoredFile.Create("farm-1", "l.png", "image/png", UploadedFileValidatorTests.Png);
        await h.Files.AddAsync(logo);
        farm.SetLogo(logo.Id);

        var file = await h.Preview.Handle(new GenerateInvoicePreviewQuery("farm-1", "plain", null, null), default);

        Assert.StartsWith("%PDF", System.Text.Encoding.ASCII.GetString(file.Content));
        Assert.Equal("plain", h.Registry.LastKey);
        var model = h.Registry.LastModel!;
        Assert.Equal(UploadedFileValidatorTests.Png, model.Issuer.Logo);
        Assert.Equal("12 Farm Road", model.Issuer.Address);
        Assert.Equal("0123456789", model.Invoice.PaymentAccountNumber);   // the farm bank details
        Assert.True(model.Invoice.LineItems.Count >= 5);
        Assert.Empty(h.Invoices.Invoices);
        Assert.Equal(0, h.Invoices.Reservations);
    }

    [Fact]
    public async Task Preview_QueryColoursBeatTheFarmBrand_WhichBeatTheTemplate()
    {
        var h = new H(NewFarm(primary: "#1B2A6B", accent: null));
        await h.Preview.Handle(new GenerateInvoicePreviewQuery("farm-1", "plain", "#c0392b", null), default);
        Assert.Equal(new InvoiceTheme("#C0392B", "#888888"), h.Registry.LastModel!.Theme);

        await h.Preview.Handle(new GenerateInvoicePreviewQuery("farm-1", "plain", null, null), default);
        Assert.Equal(new InvoiceTheme("#1B2A6B", "#888888"), h.Registry.LastModel!.Theme);
    }

    [Theory]
    [InlineData("red")]
    [InlineData("rgb(1,2,3)")]
    [InlineData("#FFFFFF")]
    [InlineData("1B2A6B")]
    public async Task Preview_BadColour_Is422(string bad)
    {
        var h = new H(NewFarm());
        await Assert.ThrowsAsync<DomainException>(() => h.Preview.Handle(new GenerateInvoicePreviewQuery("farm-1", "ngf", bad, null), default));
        await Assert.ThrowsAsync<DomainException>(() => h.Preview.Handle(new GenerateInvoicePreviewQuery("farm-1", "ngf", null, bad), default));
    }

    [Fact]
    public async Task Preview_UnknownTemplate_Is404()
    {
        var h = new H(NewFarm());
        await Assert.ThrowsAsync<NotFoundException>(() => h.Preview.Handle(new GenerateInvoicePreviewQuery("farm-1", "nope", null, null), default));
    }

    [Fact]
    public async Task Preview_OtherCompany_Is404()
    {
        var h = new H(NewFarm("other"));
        await Assert.ThrowsAsync<NotFoundException>(() => h.Preview.Handle(new GenerateInvoicePreviewQuery("farm-1", "ngf", null, null), default));
    }

    [Theory]
    [InlineData(UserRole.Supervisor)]
    [InlineData(UserRole.Member)]
    public async Task Preview_AnyCompanyUserMayPreview(UserRole role)
    {
        var h = new H(NewFarm(), role);
        Assert.NotEmpty((await h.Preview.Handle(new GenerateInvoicePreviewQuery("farm-1", "ngf", null, null), default)).Content);
    }

    [Fact]
    public async Task Preview_FarmWithoutBankDetails_StillShowsASamplePaymentBlock()
    {
        var farm = Farm.Create("Bare Farm", "co-1", "owner-1", id: "farm-1");
        var h = new H(farm);
        await h.Preview.Handle(new GenerateInvoicePreviewQuery("farm-1", "ngf", null, null), default);
        Assert.False(string.IsNullOrWhiteSpace(h.Registry.LastModel!.Invoice.PaymentAccountNumber));
        Assert.Equal("Bare Farm", h.Registry.LastModel.Issuer.Name);
    }
}
