using AgroMesh.Application.Interfaces;
using MediatR;

namespace AgroMesh.Application.Commands.Transaction;

public record SyncTransactionsCommand(string FarmId, string MonoAccountId) : IRequest<int>;

public class SyncTransactionsCommandHandler : IRequestHandler<SyncTransactionsCommand, int>
{
    private readonly IMonoBankService _monoService;

    public SyncTransactionsCommandHandler(IMonoBankService monoService) => _monoService = monoService;

    public async Task<int> Handle(SyncTransactionsCommand command, CancellationToken ct) =>
        await _monoService.SyncTransactionsAsync(command.FarmId, command.MonoAccountId, ct);
}
