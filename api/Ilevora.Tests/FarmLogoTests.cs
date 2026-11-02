using Ilevora.Application.Commands.Farm;
using Ilevora.Application.Interfaces;
using Ilevora.Application.Queries.Farm;
using Ilevora.Domain.Entities;
using Ilevora.Domain.Enums;
using Ilevora.Domain.Exceptions;
using Ilevora.Tests.Support;

namespace Ilevora.Tests;

public class FarmLogoTests
{
    private static Farm NewFarm(string companyId = "co-1") => Farm.Create("NGF", companyId, "owner-1", id: "farm-1");

    private static (UploadFarmLogoCommandHandler Upload, DeleteFarmLogoCommandHandler Delete, GetFarmLogoQueryHandler Get, FakeFarms Farms, FakeFiles Files)
        Build(Farm farm, UserRole role = UserRole.Admin, string? companyId = "co-1", FakeInspector? inspector = null)
    {
        var farms = new FakeFarms(farm);
        var files = new FakeFiles();
        var user = new FakeUser(role, companyId);
        return (new UploadFarmLogoCommandHandler(farms, files, inspector ?? new FakeInspector(), user),
                new DeleteFarmLogoCommandHandler(farms, files, user),
                new GetFarmLogoQueryHandler(farms, files, user),
                farms, files);
    }

    private static UploadFarmLogoCommand Png(string farmId = "farm-1", string? type = "image/png", string? name = "logo.png") =>
        new(farmId, UploadedFileValidatorTests.Png, name, type);

    // ---- upload ----------------------------------------------------------------

    [Fact]
    public async Task Upload_StoresTheFile_AndPointsTheFarmAtIt()
    {
        var farm = NewFarm();
        var h = Build(farm);

        var dto = await h.Upload.Handle(Png(), default);

        Assert.True(dto.HasLogo);
        Assert.Equal("image/png", dto.ContentType);
        Assert.Equal(UploadedFileValidatorTests.Png.Length, dto.Size);
        Assert.NotNull(dto.UpdatedAt);
        Assert.True(farm.HasLogo);
        var stored = Assert.Single(h.Files.Files.Values);
        Assert.Equal(farm.LogoFileId, stored.Id);
        Assert.Equal("farm-1", stored.FarmId);
        Assert.Equal("logo.png", stored.FileName);
        Assert.Equal(UploadedFileValidatorTests.Png, stored.Content);
        Assert.Equal(1, h.Farms.UpdateCount);
    }

    [Fact]
    public async Task Upload_StoresTheSniffedContentType_NotTheDeclaredOne()
    {
        var h = Build(NewFarm());
        var dto = await h.Upload.Handle(new UploadFarmLogoCommand("farm-1", UploadedFileValidatorTests.Jpeg, "x", null), default);
        Assert.Equal("image/jpeg", dto.ContentType);
    }

    [Fact]
    public async Task Upload_Replace_DeletesThePreviousStoredFile()
    {
        var farm = NewFarm();
        var h = Build(farm);
        await h.Upload.Handle(Png(), default);
        var firstId = farm.LogoFileId!;

        await h.Upload.Handle(new UploadFarmLogoCommand("farm-1", UploadedFileValidatorTests.Jpeg, "new.jpg", "image/jpeg"), default);

        Assert.NotEqual(firstId, farm.LogoFileId);
        Assert.Equal([firstId], h.Files.Deleted);
        Assert.Single(h.Files.Files);
        Assert.Equal("image/jpeg", h.Files.Files[farm.LogoFileId!].ContentType);
    }

    [Fact]
    public async Task Upload_Rejected_LeavesTheExistingLogoUntouched()
    {
        var farm = NewFarm();
        var h = Build(farm);
        await h.Upload.Handle(Png(), default);
        var before = farm.LogoFileId;

        await Assert.ThrowsAsync<UnsupportedFileTypeException>(() =>
            h.Upload.Handle(new UploadFarmLogoCommand("farm-1", UploadedFileValidatorTests.Gif, "x.gif", "image/gif"), default));

        Assert.Equal(before, farm.LogoFileId);
        Assert.Single(h.Files.Files);
        Assert.Empty(h.Files.Deleted);
    }

    [Fact]
    public async Task Upload_IfSavingTheFarmFails_TheNewFileIsRemovedAndTheOldOneKept()
    {
        var farm = NewFarm();
        var h = Build(farm);
        await h.Upload.Handle(Png(), default);
        var existing = farm.LogoFileId!;
        h.Farms.FailOnUpdate = true;

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            h.Upload.Handle(new UploadFarmLogoCommand("farm-1", UploadedFileValidatorTests.Jpeg, "new.jpg", "image/jpeg"), default));

        Assert.Equal([existing], h.Files.Files.Keys);
        Assert.DoesNotContain(existing, h.Files.Deleted);
    }

    [Fact]
    public async Task Upload_TextRenamedToPng_Is415()
    {
        var h = Build(NewFarm());
        await Assert.ThrowsAsync<UnsupportedFileTypeException>(() =>
            h.Upload.Handle(new UploadFarmLogoCommand("farm-1", "just text"u8.ToArray(), "logo.png", "image/png"), default));
        Assert.Empty(h.Files.Files);
    }

    [Fact]
    public async Task Upload_Svg_Is415()
    {
        var h = Build(NewFarm());
        await Assert.ThrowsAsync<UnsupportedFileTypeException>(() =>
            h.Upload.Handle(new UploadFarmLogoCommand("farm-1", UploadedFileValidatorTests.Svg, "logo.svg", "image/svg+xml"), default));
    }

    [Fact]
    public async Task Upload_DeclaredTypeDisagreesWithBytes_Is415()
    {
        var h = Build(NewFarm());
        await Assert.ThrowsAsync<UnsupportedFileTypeException>(() => h.Upload.Handle(Png(type: "image/jpeg"), default));
    }

    [Fact]
    public async Task Upload_Empty_Is422()
    {
        var h = Build(NewFarm());
        var ex = await Assert.ThrowsAsync<DomainException>(() => h.Upload.Handle(new UploadFarmLogoCommand("farm-1", [], "a.png", "image/png"), default));
        Assert.IsNotType<UnsupportedFileTypeException>(ex);
    }

    [Fact]
    public async Task Upload_TooLarge_Is413()
    {
        var h = Build(NewFarm());
        var big = new byte[2 * 1024 * 1024 + 1];
        UploadedFileValidatorTests.Png.CopyTo(big, 0);
        await Assert.ThrowsAsync<PayloadTooLargeException>(() => h.Upload.Handle(new UploadFarmLogoCommand("farm-1", big, "a.png", "image/png"), default));
    }

    [Fact]
    public async Task Upload_ValidHeaderButUndecodableImage_IsRejected()
    {
        var h = Build(NewFarm(), inspector: new FakeInspector { Info = null });
        var ex = await Assert.ThrowsAsync<UnsupportedFileTypeException>(() => h.Upload.Handle(Png(), default));
        Assert.Contains("image", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(h.Files.Files);
    }

    [Theory]
    [InlineData(4097, 100)]
    [InlineData(100, 4097)]
    public async Task Upload_HugeDimensions_AreRejected(int w, int hgt)
    {
        var h = Build(NewFarm(), inspector: new FakeInspector(new ImageInfo(w, hgt)));
        var ex = await Assert.ThrowsAsync<DomainException>(() => h.Upload.Handle(Png(), default));
        Assert.Contains("4096", ex.Message);
        Assert.Empty(h.Files.Files);
    }

    [Fact]
    public async Task Upload_ExactlyMaxDimensions_IsAccepted()
    {
        var h = Build(NewFarm(), inspector: new FakeInspector(new ImageInfo(4096, 4096)));
        Assert.True((await h.Upload.Handle(Png(), default)).HasLogo);
    }

    // ---- authorisation & tenancy ------------------------------------------------

    [Theory]
    [InlineData(UserRole.Admin)]
    [InlineData(UserRole.FarmManager)]
    public async Task Upload_ManagersAndOwnersAreAllowed(UserRole role)
    {
        var h = Build(NewFarm(), role);
        Assert.True((await h.Upload.Handle(Png(), default)).HasLogo);
    }

    [Theory]
    [InlineData(UserRole.Supervisor)]
    [InlineData(UserRole.Member)]
    public async Task Upload_SupervisorsAndMembersAreForbidden(UserRole role)
    {
        var farm = NewFarm();
        var h = Build(farm, role);
        await Assert.ThrowsAsync<ForbiddenException>(() => h.Upload.Handle(Png(), default));
        Assert.False(farm.HasLogo);
        Assert.Empty(h.Files.Files);
    }

    [Theory]
    [InlineData(UserRole.Supervisor)]
    [InlineData(UserRole.Member)]
    public async Task Delete_SupervisorsAndMembersAreForbidden(UserRole role)
    {
        var farm = NewFarm();
        farm.SetLogo("f");
        var h = Build(farm, role);
        await Assert.ThrowsAsync<ForbiddenException>(() => h.Delete.Handle(new DeleteFarmLogoCommand("farm-1"), default));
        Assert.True(farm.HasLogo);
    }

    [Fact]
    public async Task Upload_FarmOfAnotherCompany_Is404()
    {
        var h = Build(NewFarm(companyId: "other-co"));
        await Assert.ThrowsAsync<NotFoundException>(() => h.Upload.Handle(Png(), default));
    }

    [Fact]
    public async Task Upload_CallerWithoutACompany_Is404_FailsClosed()
    {
        var h = Build(NewFarm(), companyId: null);
        await Assert.ThrowsAsync<NotFoundException>(() => h.Upload.Handle(Png(), default));
    }

    [Fact]
    public async Task Upload_UnknownFarm_Is404()
    {
        var h = Build(NewFarm());
        await Assert.ThrowsAsync<NotFoundException>(() => h.Upload.Handle(Png("nope"), default));
    }

    [Fact]
    public async Task OtherCompanysSupervisor_GetsNotFound_NotForbidden()
    {
        var h = Build(NewFarm(companyId: "other-co"), UserRole.Supervisor);
        await Assert.ThrowsAsync<NotFoundException>(() => h.Upload.Handle(Png(), default));
    }

    // ---- delete -----------------------------------------------------------------

    [Fact]
    public async Task Delete_ClearsTheFarm_AndRemovesTheStoredFile()
    {
        var farm = NewFarm();
        var h = Build(farm);
        await h.Upload.Handle(Png(), default);
        var id = farm.LogoFileId!;

        await h.Delete.Handle(new DeleteFarmLogoCommand("farm-1"), default);

        Assert.False(farm.HasLogo);
        Assert.Null(farm.LogoUpdatedAt);
        Assert.Empty(h.Files.Files);
        Assert.Equal([id], h.Files.Deleted);
    }

    [Fact]
    public async Task Delete_WithNoLogo_IsANoOp()
    {
        var farm = NewFarm();
        var h = Build(farm);
        await h.Delete.Handle(new DeleteFarmLogoCommand("farm-1"), default);
        Assert.Empty(h.Files.Deleted);
        Assert.Equal(0, h.Farms.UpdateCount);
    }

    [Fact]
    public async Task Delete_FarmOfAnotherCompany_Is404()
    {
        var h = Build(NewFarm(companyId: "other-co"));
        await Assert.ThrowsAsync<NotFoundException>(() => h.Delete.Handle(new DeleteFarmLogoCommand("farm-1"), default));
    }

    // ---- get --------------------------------------------------------------------

    [Fact]
    public async Task Get_ReturnsTheBytesAndContentType()
    {
        var farm = NewFarm();
        var h = Build(farm);
        await h.Upload.Handle(Png(), default);

        var file = await h.Get.Handle(new GetFarmLogoQuery("farm-1"), default);

        Assert.Equal(UploadedFileValidatorTests.Png, file.Content);
        Assert.Equal("image/png", file.ContentType);
        Assert.Equal(farm.LogoUpdatedAt, file.UpdatedAt);
    }

    [Theory]
    [InlineData(UserRole.Supervisor)]
    [InlineData(UserRole.Member)]
    [InlineData(UserRole.FarmManager)]
    public async Task Get_AnyRoleOfTheCompanyCanRead(UserRole role)
    {
        var farm = NewFarm();
        var h = Build(farm, UserRole.Admin);
        await h.Upload.Handle(Png(), default);
        var reader = new GetFarmLogoQueryHandler(h.Farms, h.Files, new FakeUser(role));
        Assert.NotEmpty((await reader.Handle(new GetFarmLogoQuery("farm-1"), default)).Content);
    }

    [Fact]
    public async Task Get_NoLogo_Is404()
    {
        var h = Build(NewFarm());
        await Assert.ThrowsAsync<NotFoundException>(() => h.Get.Handle(new GetFarmLogoQuery("farm-1"), default));
    }

    [Fact]
    public async Task Get_FarmPointsAtAMissingFile_Is404()
    {
        var farm = NewFarm();
        farm.SetLogo("ghost");
        var h = Build(farm);
        await Assert.ThrowsAsync<NotFoundException>(() => h.Get.Handle(new GetFarmLogoQuery("farm-1"), default));
    }

    [Fact]
    public async Task Get_FarmOfAnotherCompany_Is404()
    {
        var farm = NewFarm(companyId: "other-co");
        farm.SetLogo("f");
        var h = Build(farm);
        await Assert.ThrowsAsync<NotFoundException>(() => h.Get.Handle(new GetFarmLogoQuery("farm-1"), default));
    }

    [Fact]
    public async Task Get_FileBelongingToAnotherFarm_Is404()
    {
        var farm = NewFarm();
        var h = Build(farm);
        var foreign = StoredFile.Create("farm-OTHER", "x.png", "image/png", [1, 2, 3]);
        await h.Files.AddAsync(foreign);
        farm.SetLogo(foreign.Id);
        await Assert.ThrowsAsync<NotFoundException>(() => h.Get.Handle(new GetFarmLogoQuery("farm-1"), default));
    }
}

public class RoleBeforeFileValidationTests
{
    [Theory]
    [InlineData(UserRole.Supervisor)]
    [InlineData(UserRole.Member)]
    public async Task Logo_ACallerWhoMayNotUpload_GetsForbidden_EvenWithAHugeOrWrongFile(UserRole role)
    {
        var farm = Farm.Create("NGF", "co-1", "owner-1", id: "farm-1");
        var handler = new UploadFarmLogoCommandHandler(new FakeFarms(farm), new FakeFiles(), new FakeInspector(), new FakeUser(role));

        await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(new UploadFarmLogoCommand("farm-1", new byte[3 * 1024 * 1024], "a.png", "image/png"), default));
        await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(new UploadFarmLogoCommand("farm-1", "text"u8.ToArray(), "a.png", "image/png"), default));
    }
}
