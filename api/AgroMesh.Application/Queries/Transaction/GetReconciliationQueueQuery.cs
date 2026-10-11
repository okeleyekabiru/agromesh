using AgroMesh.Application.DTOs;
using AgroMesh.Application.Interfaces;
using AgroMesh.Application.Mapping;
using MediatR;

namespace AgroMesh.Application.Queries.Transaction;

public record GetReconciliationQueueQuery(string FarmId) : IRequest<List<TransactionDto>>;

public class GetReconciliationQueueQueryHandler : IRequestHandler<GetReconciliationQueueQuery, List<TransactionDto>>
{
    private readonly ITransactionRepository _transactions;

    public GetReconciliationQueueQueryHandler(ITransactionRepository transactions) => _transactions = transactions;

    public async Task<List<TransactionDto>> Handle(GetReconciliationQueueQuery query, CancellationToken ct) =>
        (await _transactions.GetNeedingReviewAsync(query.FarmId, ct)).Select(TransactionMapping.ToDto).ToList();
}
