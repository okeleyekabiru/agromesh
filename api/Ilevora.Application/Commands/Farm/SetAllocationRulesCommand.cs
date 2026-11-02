using Ilevora.Application.Interfaces;
using Ilevora.Domain.Enums;
using MediatR;

namespace Ilevora.Application.Commands.Farm;

public record AllocationRuleInput(TransactionCategory Category, Dictionary<string, double> Allocation, List<int>? SeasonMonths);

public record SetAllocationRulesCommand(string FarmId, List<AllocationRuleInput> Rules) : IRequest<Unit>;

public class SetAllocationRulesCommandHandler : IRequestHandler<SetAllocationRulesCommand, Unit>
{
    private readonly ICropAllocationRuleRepository _ruleRepo;

    public SetAllocationRulesCommandHandler(ICropAllocationRuleRepository ruleRepo) => _ruleRepo = ruleRepo;

    public async Task<Unit> Handle(SetAllocationRulesCommand command, CancellationToken ct)
    {
        var entities = command.Rules
            .Select(r => Domain.Entities.CropAllocationRule.Create(command.FarmId, r.Category, r.Allocation, seasonMonths: r.SeasonMonths))
            .ToList();

        await _ruleRepo.AddBulkAsync(entities, ct);
        return Unit.Value;
    }
}
