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
