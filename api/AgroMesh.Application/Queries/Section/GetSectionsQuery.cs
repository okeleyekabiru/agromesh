using AgroMesh.Application.Commands.Section;
using AgroMesh.Application.DTOs;
using AgroMesh.Application.Interfaces;
using AgroMesh.Domain.Enums;
using AgroMesh.Domain.Exceptions;
using MediatR;

namespace AgroMesh.Application.Queries.Section;

public record GetSectionsQuery(string FarmId) : IRequest<List<SectionDto>>;

public class GetSectionsQueryHandler : IRequestHandler<GetSectionsQuery, List<SectionDto>>
{
    private readonly ISectionRepository _sectionRepo;
    private readonly IFarmRepository _farmRepo;
    private readonly ISchedulingPredictionRepository _predictionRepo;

    public GetSectionsQueryHandler(ISectionRepository sectionRepo, IFarmRepository farmRepo, ISchedulingPredictionRepository predictionRepo)
    {
        _sectionRepo = sectionRepo;
        _farmRepo = farmRepo;
        _predictionRepo = predictionRepo;
    }

    public async Task<List<SectionDto>> Handle(GetSectionsQuery query, CancellationToken ct)
    {
        var farm = await _farmRepo.GetByIdAsync(query.FarmId, ct)
            ?? throw new DomainException($"Farm '{query.FarmId}' not found.");

        var sections = await _sectionRepo.GetByFarmIdAsync(query.FarmId, ct);
        var predictions = await _predictionRepo.GetByFarmIdAsync(query.FarmId, ct);

        var today = DateTime.UtcNow.Date;

        return sections.Select(s =>
        {
            var nextWeeding = predictions.FirstOrDefault(p => p.SectionId == s.Id && p.ActivityType == ActivityType.Weeding);
            var nextSpraying = predictions.FirstOrDefault(p => p.SectionId == s.Id && p.ActivityType == ActivityType.Spraying);

            return CreateSectionCommandHandler.MapToDto(s, farm.PreferredAreaUnit) with
            {
                NextWeedingPrediction = nextWeeding == null ? null : new UpcomingTaskDto(
                    s.Id, s.Name, ActivityType.Weeding, nextWeeding.LastActivityDate, nextWeeding.PredictedNextDue,
                    (nextWeeding.PredictedNextDue.Date - today).Days, nextWeeding.Reasoning,
                    nextWeeding.RecommendedChemical, nextWeeding.RecommendedSprayDate,
                    nextWeeding.Confidence, null, null, nextWeeding.IsStale, nextWeeding.WeatherDataUsed),
                NextSprayingPrediction = nextSpraying == null ? null : new UpcomingTaskDto(
                    s.Id, s.Name, ActivityType.Spraying, nextSpraying.LastActivityDate, nextSpraying.PredictedNextDue,
                    (nextSpraying.PredictedNextDue.Date - today).Days, nextSpraying.Reasoning,
                    nextSpraying.RecommendedChemical, nextSpraying.RecommendedSprayDate,
                    nextSpraying.Confidence, null, null, nextSpraying.IsStale, nextSpraying.WeatherDataUsed)
            };
        }).ToList();
    }
}
