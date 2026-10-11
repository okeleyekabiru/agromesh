using AgroMesh.Application.DTOs;
using AgroMesh.Application.Interfaces;
using MediatR;

namespace AgroMesh.Application.Queries.Farm;

public record GetOnboardingStatusQuery(string FarmId) : IRequest<OnboardingStatusDto>;

public class GetOnboardingStatusQueryHandler : IRequestHandler<GetOnboardingStatusQuery, OnboardingStatusDto>
{
    private readonly IFarmRepository _farmRepo;
    private readonly IBlockRepository _blockRepo;
    private readonly IWorkerRoleRepository _roleRepo;
    private readonly IWorkerRepository _workerRepo;
    private readonly IInventoryRepository _inventoryRepo;
    private readonly ICropAllocationRuleRepository _ruleRepo;

    public GetOnboardingStatusQueryHandler(
        IFarmRepository farmRepo,
        IBlockRepository blockRepo,
        IWorkerRoleRepository roleRepo,
        IWorkerRepository workerRepo,
        IInventoryRepository inventoryRepo,
        ICropAllocationRuleRepository ruleRepo)
    {
        _farmRepo = farmRepo;
        _blockRepo = blockRepo;
        _roleRepo = roleRepo;
        _workerRepo = workerRepo;
        _inventoryRepo = inventoryRepo;
        _ruleRepo = ruleRepo;
    }

    public async Task<OnboardingStatusDto> Handle(GetOnboardingStatusQuery query, CancellationToken ct)
    {
        var farm = await _farmRepo.GetByIdAsync(query.FarmId, ct);
        if (farm == null)
            return new OnboardingStatusDto(false, false, false, false, false, false, false);

        var blocks = await _blockRepo.GetByFarmIdAsync(query.FarmId, ct);
        var roles = await _roleRepo.GetByFarmIdAsync(query.FarmId, ct);
        var workers = await _workerRepo.GetByFarmIdAsync(query.FarmId, activeOnly: true, ct);
        var inventory = await _inventoryRepo.GetByFarmIdAsync(query.FarmId, ct);
        var rules = await _ruleRepo.GetByFarmIdAsync(query.FarmId, ct);

        var hasFarm = true;
        var hasRoles = roles.Any();
        var hasWorkers = workers.Any();
        var hasBlocks = blocks.Any();
        var hasInventory = inventory.Any();
        var hasRules = rules.Any();
        var isComplete = hasFarm && hasRoles && hasWorkers && hasBlocks && hasInventory && hasRules;

        return new OnboardingStatusDto(hasFarm, hasRoles, hasWorkers, hasBlocks, hasInventory, hasRules, isComplete);
    }
}
