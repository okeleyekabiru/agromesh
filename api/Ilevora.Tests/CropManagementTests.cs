using System.Text.Json;
using Ilevora.Application.Commands.Crop;
using Ilevora.Application.DTOs;
using Ilevora.Application.Interfaces;
using Ilevora.Application.Queries.Crop;
using Ilevora.Application.Services;
using Ilevora.Domain.Crops;
using Ilevora.Domain.Entities;
using Ilevora.Domain.Enums;
using Ilevora.Domain.Exceptions;
using Ilevora.Domain.ValueObjects;
using Xunit;

namespace Ilevora.Tests;

public class CropManagementTests
{
    private class FakeCrops : ICropRepository
    {
        public List<Crop> Items { get; } = [];
        public Task<Crop?> GetByIdAsync(string id, CancellationToken ct = default) => Task.FromResult(Items.FirstOrDefault(c => c.Id == id));
        public Task<List<Crop>> GetByFarmIdAsync(string farmId, CancellationToken ct = default) => Task.FromResult(Items.Where(c => c.FarmId == farmId).ToList());
        public Task<Crop> AddAsync(Crop crop, CancellationToken ct = default) { Items.Add(crop); return Task.FromResult(crop); }
    }

    private class FakeFarms(Farm farm) : IFarmRepository
    {
        public Task<Farm?> GetByIdAsync(string id, CancellationToken ct = default) => Task.FromResult(farm.Id == id ? farm : null);
        public Task<Farm?> GetByIdWithBlocksAsync(string id, CancellationToken ct = default) => GetByIdAsync(id, ct);
        public Task<List<Farm>> GetAllAsync(CancellationToken ct = default) => Task.FromResult(new List<Farm> { farm });
        public Task<List<Farm>> GetByOwnerIdAsync(string ownerId, CancellationToken ct = default) => Task.FromResult(new List<Farm> { farm });
        public Task<List<Farm>> GetByCompanyIdAsync(string companyId, CancellationToken ct = default) => Task.FromResult(new List<Farm> { farm });
        public Task<Farm> AddAsync(Farm f, CancellationToken ct = default) => Task.FromResult(f);
        public Task UpdateAsync(Farm f, CancellationToken ct = default) => Task.CompletedTask;
        public Task<bool> ExistsAsync(string id, CancellationToken ct = default) => Task.FromResult(farm.Id == id);
    }

    private class FakeUser(string? companyId = "co-1") : ICurrentUserAccessor
    {
        public string? UserId => "u1";
        public string? CompanyId => companyId;
        public UserRole Role => UserRole.Admin;
    }

    private static readonly Farm TheFarm = Farm.Create("F", "co-1", "owner");

    // ── legacy data reads back losslessly ──

    [Theory]
    [InlineData(0, "OilPalm", "Oil palm")]
    [InlineData(1, "Maize", "Maize")]
    [InlineData(2, "Mixed", "Mixed")]
    [InlineData(3, "Cocoa", "Cocoa")]
    [InlineData(4, "Cassava", "Cassava")]
    public void Stored_blocks_with_the_old_enum_ordinal_read_back_as_the_same_crop(int ordinal, string id, string name)
    {
        var json = $"[{{\"CropType\":{ordinal},\"Hectares\":2.5,\"Percentage\":100}}]";
        var crops = JsonSerializer.Deserialize<List<BlockCrop>>(json)!;
        Assert.Equal(new BlockCrop(id, name, 2.5, 100), crops.Single());
    }

    [Fact]
    public void Crops_round_trip_through_json()
    {
        var custom = new BlockCrop("abc-123", "Yam", 1.5, 40);
        var list = new List<BlockCrop> { new(BuiltInCrop.Plantain, 3.5, 60), custom };
        var back = JsonSerializer.Deserialize<List<BlockCrop>>(JsonSerializer.Serialize(list))!;
        Assert.Equal(list, back);
    }

    // ── smart enum ──

    [Fact]
    public void Built_in_crops_keep_old_ordinals_and_new_ones_have_none()
    {
        Assert.Equal(0, BuiltInCrop.OilPalm.LegacyOrdinal);
        Assert.Equal(4, BuiltInCrop.Cassava.LegacyOrdinal);
        Assert.Null(BuiltInCrop.Plantain.LegacyOrdinal);
        Assert.Contains(BuiltInCrop.Plantain, BuiltInCrop.All);
        Assert.Contains(BuiltInCrop.Banana, BuiltInCrop.All);
        Assert.DoesNotContain(BuiltInCrop.Mixed, BuiltInCrop.All);
    }

    [Fact]
    public void A_custom_crop_gets_the_generic_fertilizer_cycle()
    {
        var today = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
        var last = today.AddDays(-7);
        var r = BlockCycleCalculator.Calculate(new CycleInputs(today, null, null, null, true, 120, 60, [], [],
            [new BlockCrop("custom-1", "Yam", 1, 100)], last, null)).Fertilizing!;
        Assert.Equal(last.AddDays(BuiltInCrop.GenericFertilizer.Weeks * 7), r.DueDate);
        Assert.Contains(r.Factors, f => f.StartsWith("Yam"));
    }

    [Fact]
    public void Mixed_is_still_not_a_real_crop()
    {
        Assert.Throws<DomainException>(() => Block.Create("f1", "A", [new BlockCrop("Mixed", "Mixed", 1, 100)], 1));
    }

    // ── resolving what clients send ──

    [Fact]
    public async Task Old_clients_sending_the_enum_ordinal_still_resolve()
    {
        var crops = await CropResolver.ResolveAsync(new FakeCrops(), TheFarm.Id,
            [new BlockCropInput(60, CropType: 0), new BlockCropInput(40, CropType: 4)], 10, default);
        Assert.Equal(["OilPalm", "Cassava"], crops.Select(c => c.CropId));
        Assert.Equal(6.0, crops[0].Hectares);
    }

    [Fact]
    public async Task A_crop_id_resolves_built_in_and_the_farms_own_crops()
    {
        var repo = new FakeCrops();
        var yam = Crop.CreateCustom(TheFarm.Id, "Yam");
        repo.Items.Add(yam);
        var crops = await CropResolver.ResolveAsync(repo, TheFarm.Id,
            [new BlockCropInput(50, CropId: "Plantain"), new BlockCropInput(50, CropId: yam.Id)], 4, default);
        Assert.Equal("Plantain", crops[0].CropId);
        Assert.Equal(("Yam", yam.Id), (crops[1].CropName, crops[1].CropId));
    }

    [Fact]
    public async Task Another_farms_crop_or_an_unknown_id_is_rejected()
    {
        var repo = new FakeCrops();
        var other = Crop.CreateCustom("other-farm", "Yam");
        repo.Items.Add(other);
        await Assert.ThrowsAsync<DomainException>(() => CropResolver.ResolveAsync(repo, TheFarm.Id, [new BlockCropInput(100, CropId: other.Id)], 1, default));
        await Assert.ThrowsAsync<DomainException>(() => CropResolver.ResolveAsync(repo, TheFarm.Id, [new BlockCropInput(100, CropId: "nope")], 1, default));
        await Assert.ThrowsAsync<DomainException>(() => CropResolver.ResolveAsync(repo, TheFarm.Id, [new BlockCropInput(100)], 1, default));
        await Assert.ThrowsAsync<DomainException>(() => CropResolver.ResolveAsync(repo, TheFarm.Id, [new BlockCropInput(100, CropType: 99)], 1, default));
    }

    [Fact]
    public void Wire_ordinal_is_the_old_one_for_originals_and_mixed_for_newer_crops()
    {
        Assert.Equal(3, CropResolver.WireOrdinal("Cocoa"));
        Assert.Equal(2, CropResolver.WireOrdinal("Plantain"));
        Assert.Equal(2, CropResolver.WireOrdinal("some-custom-guid"));
    }

    // ── create / list crops ──

    [Fact]
    public async Task Creating_a_crop_adds_it_to_the_farms_list_after_the_built_ins()
    {
        var repo = new FakeCrops();
        var create = new CreateCropCommandHandler(repo, new FakeFarms(TheFarm), new FakeUser());
        var made = await create.Handle(new CreateCropCommand(TheFarm.Id, new CreateCropRequest("  Yam ")), default);
        Assert.Equal("Yam", made.Name);
        Assert.False(made.IsBuiltIn);

        var list = await new GetCropsQueryHandler(repo, new FakeFarms(TheFarm), new FakeUser()).Handle(new GetCropsQuery(TheFarm.Id), default);
        Assert.Equal(["Oil palm", "Maize", "Cocoa", "Cassava", "Plantain", "Banana", "Yam"], list.Select(c => c.Name));
        Assert.Equal(6, list.Count(c => c.IsBuiltIn));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("maize")]
    [InlineData("PLANTAIN")]
    public async Task Blank_or_duplicate_crop_names_are_rejected(string name)
    {
        var create = new CreateCropCommandHandler(new FakeCrops(), new FakeFarms(TheFarm), new FakeUser());
        await Assert.ThrowsAsync<DomainException>(() => create.Handle(new CreateCropCommand(TheFarm.Id, new CreateCropRequest(name)), default));
    }

    [Fact]
    public async Task A_second_custom_crop_with_the_same_name_is_rejected()
    {
        var repo = new FakeCrops();
        var create = new CreateCropCommandHandler(repo, new FakeFarms(TheFarm), new FakeUser());
        await create.Handle(new CreateCropCommand(TheFarm.Id, new CreateCropRequest("Yam")), default);
        await Assert.ThrowsAsync<DomainException>(() => create.Handle(new CreateCropCommand(TheFarm.Id, new CreateCropRequest("yam")), default));
    }

    [Fact]
    public async Task Another_companys_farm_cannot_add_crops()
    {
        var create = new CreateCropCommandHandler(new FakeCrops(), new FakeFarms(TheFarm), new FakeUser("co-2"));
        await Assert.ThrowsAsync<NotFoundException>(() => create.Handle(new CreateCropCommand(TheFarm.Id, new CreateCropRequest("Yam")), default));
    }
}
