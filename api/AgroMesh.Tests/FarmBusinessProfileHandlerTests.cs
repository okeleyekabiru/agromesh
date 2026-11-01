using AgroMesh.Application.Commands.Farm;
using AgroMesh.Application.DTOs;
using AgroMesh.Application.Queries.Farm;
using AgroMesh.Domain.Entities;
using AgroMesh.Domain.Enums;
using AgroMesh.Domain.Exceptions;
using AgroMesh.Tests.Support;

namespace AgroMesh.Tests;

public class FarmBusinessProfileHandlerTests
{
    private static Farm NewFarm(string companyId = "co-1") => Farm.Create("NGF", companyId, "owner-1", id: "farm-1");

    private static UpdateFarmBusinessProfileRequest Full() =>
        new("12 Farm Road", "+234 801", "hello@ngf.ng", "www.ngf.ng", "Nature's Gold Farm", "0123456789", "GTBank");

    [Fact]
    public async Task Update_SavesEveryField_AndReturnsTheProfile()
    {
        var farm = NewFarm();
        var farms = new FakeFarms(farm);
        var handler = new UpdateFarmBusinessProfileCommandHandler(farms, new FakeUser(UserRole.FarmManager));

        var dto = await handler.Handle(new UpdateFarmBusinessProfileCommand("farm-1", Full()), default);

        Assert.Equal("12 Farm Road", dto.Address);
        Assert.Equal("GTBank", dto.BankName);
        Assert.Equal("0123456789", farm.BankAccountNumber);
        Assert.Equal(1, farms.UpdateCount);
    }

    [Fact]
    public async Task Update_IsAFullReplace_MissingFieldsAreCleared()
    {
        var farm = NewFarm();
        var farms = new FakeFarms(farm);
        var handler = new UpdateFarmBusinessProfileCommandHandler(farms, new FakeUser());
        await handler.Handle(new UpdateFarmBusinessProfileCommand("farm-1", Full()), default);

        var dto = await handler.Handle(new UpdateFarmBusinessProfileCommand("farm-1", new("Only address", null, null, null, null, null, null)), default);

        Assert.Equal("Only address", dto.Address);
        Assert.Null(dto.Phone);
        Assert.Null(dto.BankName);
    }

    [Theory]
    [InlineData(UserRole.Supervisor)]
    [InlineData(UserRole.Member)]
    public async Task Update_NonManagersAreForbidden(UserRole role)
    {
        var farm = NewFarm();
        var farms = new FakeFarms(farm);
        var handler = new UpdateFarmBusinessProfileCommandHandler(farms, new FakeUser(role));

        await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(new UpdateFarmBusinessProfileCommand("farm-1", Full()), default));
        Assert.Null(farm.Address);
        Assert.Equal(0, farms.UpdateCount);
    }

    [Fact]
    public async Task Update_BadEmail_Is422AndNothingIsSaved()
    {
        var farm = NewFarm();
        var farms = new FakeFarms(farm);
        var handler = new UpdateFarmBusinessProfileCommandHandler(farms, new FakeUser());

        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new UpdateFarmBusinessProfileCommand("farm-1", Full() with { Email = "nope" }), default));
        Assert.Equal(0, farms.UpdateCount);
    }

    [Fact]
    public async Task Update_OtherCompanysFarm_Is404()
    {
        var handler = new UpdateFarmBusinessProfileCommandHandler(new FakeFarms(NewFarm("other")), new FakeUser());
        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(new UpdateFarmBusinessProfileCommand("farm-1", Full()), default));
    }

    [Fact]
    public async Task Get_AnyRoleOfTheCompanyCanRead()
    {
        var farm = NewFarm();
        farm.SetBusinessProfile("Addr", null, null, null, null, null, null);
        var handler = new GetFarmBusinessProfileQueryHandler(new FakeFarms(farm), new FakeUser(UserRole.Supervisor));

        var dto = await handler.Handle(new GetFarmBusinessProfileQuery("farm-1"), default);

        Assert.Equal("Addr", dto.Address);
        Assert.Null(dto.Phone);
    }

    [Fact]
    public async Task Get_OtherCompanysFarm_Is404()
    {
        var handler = new GetFarmBusinessProfileQueryHandler(new FakeFarms(NewFarm("other")), new FakeUser());
        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(new GetFarmBusinessProfileQuery("farm-1"), default));
    }

    [Fact]
    public void FarmDto_ExposesHasLogoAndLogoUpdatedAt()
    {
        var farm = NewFarm();
        Assert.False(GetFarmsQueryHandler.MapToDto(farm).HasLogo);

        farm.SetLogo("f1");
        var dto = GetFarmsQueryHandler.MapToDto(farm);
        Assert.True(dto.HasLogo);
        Assert.Equal(farm.LogoUpdatedAt, dto.LogoUpdatedAt);
    }
}

public class FarmBrandColourTests
{
    private static Farm NewFarm() => Farm.Create("NGF", "co-1", "owner-1", id: "farm-1");

    [Fact]
    public void Farm_StartsWithNoBrandColours()
    {
        var farm = NewFarm();
        Assert.Null(farm.BrandPrimaryColor);
        Assert.Null(farm.BrandAccentColor);
    }

    [Fact]
    public void SetBusinessProfile_NormalisesAndStoresTheBrandColours()
    {
        var farm = NewFarm();
        farm.SetBusinessProfile(null, null, null, null, null, null, null, " #1b2a6b ", "#e67e22");
        Assert.Equal("#1B2A6B", farm.BrandPrimaryColor);
        Assert.Equal("#E67E22", farm.BrandAccentColor);
    }

    [Fact]
    public void SetBusinessProfile_BlankColoursResetToDefault_AndOldCallsStillWork()
    {
        var farm = NewFarm();
        farm.SetBusinessProfile(null, null, null, null, null, null, null, "#1B2A6B", "#E67E22");
        farm.SetBusinessProfile(null, null, null, null, null, null, null, "", null);
        Assert.Null(farm.BrandPrimaryColor);
        Assert.Null(farm.BrandAccentColor);
    }

    [Theory]
    [InlineData("blue")]
    [InlineData("#FFF")]
    [InlineData("#FFFFFF")]
    public void SetBusinessProfile_RejectsBadColours_AndChangesNothing(string bad)
    {
        var farm = NewFarm();
        farm.SetBusinessProfile("Addr", null, null, null, null, null, null, "#1B2A6B", null);
        Assert.Throws<DomainException>(() => farm.SetBusinessProfile("Other", null, null, null, null, null, null, "#1B2A6B", bad));
        Assert.Equal("Addr", farm.Address);
    }

    private static UpdateFarmBusinessProfileCommandHandler Handler(Farm farm, FakeFarms farms) => new(farms, new FakeUser());

    [Fact]
    public async Task Update_OmittedColours_KeepTheCurrentOnes_SoOlderClientsDoNotWipeThem()
    {
        var farm = NewFarm();
        farm.SetBusinessProfile(null, null, null, null, null, null, null, "#1B2A6B", "#E67E22");
        var farms = new FakeFarms(farm);

        var dto = await Handler(farm, farms).Handle(new UpdateFarmBusinessProfileCommand("farm-1",
            new UpdateFarmBusinessProfileRequest("New address", null, null, null, null, null, null)), default);

        Assert.Equal("#1B2A6B", dto.BrandPrimaryColor);
        Assert.Equal("#E67E22", dto.BrandAccentColor);
        Assert.Equal("New address", dto.Address);
    }

    [Fact]
    public async Task Update_EmptyStringResetsAColourToDefault_AValueReplacesIt()
    {
        var farm = NewFarm();
        farm.SetBusinessProfile(null, null, null, null, null, null, null, "#1B2A6B", "#E67E22");
        var farms = new FakeFarms(farm);

        var dto = await Handler(farm, farms).Handle(new UpdateFarmBusinessProfileCommand("farm-1",
            new UpdateFarmBusinessProfileRequest(null, null, null, null, null, null, null, "", "#c0392b")), default);

        Assert.Null(dto.BrandPrimaryColor);
        Assert.Equal("#C0392B", dto.BrandAccentColor);
    }

    [Fact]
    public async Task Update_InvalidColour_Is422_AndNothingSaved()
    {
        var farm = NewFarm();
        var farms = new FakeFarms(farm);
        await Assert.ThrowsAsync<DomainException>(() => Handler(farm, farms).Handle(new UpdateFarmBusinessProfileCommand("farm-1",
            new UpdateFarmBusinessProfileRequest("A", null, null, null, null, null, null, "rgb(1,2,3)", null)), default));
        Assert.Equal(0, farms.UpdateCount);
    }

    [Fact]
    public async Task Get_ReturnsTheColours_NullWhenDefault()
    {
        var farm = NewFarm();
        var handler = new GetFarmBusinessProfileQueryHandler(new FakeFarms(farm), new FakeUser());
        var dto = await handler.Handle(new GetFarmBusinessProfileQuery("farm-1"), default);
        Assert.Null(dto.BrandPrimaryColor);
        Assert.Null(dto.BrandAccentColor);
    }
}
