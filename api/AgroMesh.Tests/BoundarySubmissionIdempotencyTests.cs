using AgroMesh.Application.Commands.Section;
using AgroMesh.Application.DTOs;
using AgroMesh.Application.Interfaces;
using AgroMesh.Domain.Entities;
using AgroMesh.Domain.Enums;
using AgroMesh.Domain.ValueObjects;

namespace AgroMesh.Tests;

public class BoundarySubmissionIdempotencyTests
{
    private class FakeSectionRepository : ISectionRepository
    {
        public Section? Section;
        public int UpdateCallCount;

        public Task<Section?> GetByIdAsync(string id, CancellationToken ct = default) => Task.FromResult(Section);
        public Task<Section?> GetByIdWithBlocksAsync(string id, CancellationToken ct = default) => Task.FromResult(Section);
        public Task<List<Section>> GetByFarmIdAsync(string farmId, CancellationToken ct = default) => Task.FromResult(new List<Section>());

        public Task<Section> AddAsync(Section section, CancellationToken ct = default)
        {
            Section = section;
            return Task.FromResult(section);
        }

        public Task UpdateAsync(Section section, CancellationToken ct = default)
        {
            UpdateCallCount++;
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(string id, CancellationToken ct = default) => Task.FromResult(Section != null);
    }

    private class FakeFarmRepository : IFarmRepository
    {
        public Task<Farm?> GetByIdAsync(string id, CancellationToken ct = default) => Task.FromResult<Farm?>(null);
        public Task<Farm?> GetByIdWithSectionsAsync(string id, CancellationToken ct = default) => Task.FromResult<Farm?>(null);
        public Task<List<Farm>> GetAllAsync(CancellationToken ct = default) => Task.FromResult(new List<Farm>());
        public Task<List<Farm>> GetByOwnerIdAsync(string ownerId, CancellationToken ct = default) => Task.FromResult(new List<Farm>());
        public Task<List<Farm>> GetByCompanyIdAsync(string companyId, CancellationToken ct = default) => Task.FromResult(new List<Farm>());
        public Task<Farm> AddAsync(Farm farm, CancellationToken ct = default) => Task.FromResult(farm);
        public Task UpdateAsync(Farm farm, CancellationToken ct = default) => Task.CompletedTask;
        public Task<bool> ExistsAsync(string id, CancellationToken ct = default) => Task.FromResult(false);
    }

    private class FakeBoundarySubmissionRepository : IBoundarySubmissionRepository
    {
        private readonly List<BoundarySubmission> _submissions = [];
        public int AddCallCount;

        public Task<BoundarySubmission?> GetByOperationIdAsync(string operationId, CancellationToken ct = default) =>
            Task.FromResult(_submissions.FirstOrDefault(s => s.OperationId == operationId));

        public Task<BoundarySubmission> AddAsync(BoundarySubmission submission, CancellationToken ct = default)
        {
            AddCallCount++;
            _submissions.Add(submission);
            return Task.FromResult(submission);
        }
    }

    // No company on the caller means the ownership check is skipped — the tests below
    // exercise idempotency, not authorization, so ownership is not the concern here.
    private class FakeCurrentUserAccessor : ICurrentUserAccessor
    {
        public string? UserId => "user-1";
        public string? CompanyId => null;
    }

    private static List<GeoPointInput> SquareCorners() =>
    [
        new(7.3775, 3.9470),
        new(7.3775, 3.9560),
        new(7.3865, 3.9560),
        new(7.3865, 3.9470),
    ];

    private static SubmitBoundaryPointsCommandHandler CreateHandler(FakeSectionRepository sectionRepo, FakeBoundarySubmissionRepository submissionRepo) =>
        new(sectionRepo, new FakeFarmRepository(), submissionRepo, new FakeCurrentUserAccessor());

    [Fact]
    public async Task Handle_SameOperationIdSubmittedTwice_DoesNotCreateDuplicateSubmission()
    {
        var section = Section.Create("farm-1", "Section A", [new SectionCrop(CropType.Maize, 1.0, 100)], 1.0);
        var sectionRepo = new FakeSectionRepository { Section = section };
        var submissionRepo = new FakeBoundarySubmissionRepository();
        var handler = CreateHandler(sectionRepo, submissionRepo);

        var request = new SubmitBoundaryPointsRequest("op-123", SquareCorners());
        var command = new SubmitBoundaryPointsCommand(section.Id, request);

        var first = await handler.Handle(command, CancellationToken.None);
        var second = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(1, submissionRepo.AddCallCount);
        Assert.Equal(1, sectionRepo.UpdateCallCount);
        Assert.Equal(first.ComputedHectares, second.ComputedHectares);
        Assert.Equal(first.ComputedGeoJson, second.ComputedGeoJson);
        Assert.Equal("op-123", second.OperationId);
    }

    [Fact]
    public async Task Handle_DifferentOperationIds_CreatesSeparateSubmissions()
    {
        var section = Section.Create("farm-1", "Section A", [new SectionCrop(CropType.Maize, 1.0, 100)], 1.0);
        var sectionRepo = new FakeSectionRepository { Section = section };
        var submissionRepo = new FakeBoundarySubmissionRepository();
        var handler = CreateHandler(sectionRepo, submissionRepo);

        await handler.Handle(new SubmitBoundaryPointsCommand(section.Id, new SubmitBoundaryPointsRequest("op-1", SquareCorners())), CancellationToken.None);
        await handler.Handle(new SubmitBoundaryPointsCommand(section.Id, new SubmitBoundaryPointsRequest("op-2", SquareCorners())), CancellationToken.None);

        Assert.Equal(2, submissionRepo.AddCallCount);
        Assert.Equal(2, sectionRepo.UpdateCallCount);
    }
}
