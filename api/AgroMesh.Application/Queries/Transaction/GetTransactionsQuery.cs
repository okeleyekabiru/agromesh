using AgroMesh.Application.DTOs;
using AgroMesh.Application.Interfaces;
using MediatR;

namespace AgroMesh.Application.Queries.Transaction;

public record GetTransactionsQuery(string FarmId, int Page = 1, int PageSize = 20, bool? PendingReview = null)
    : IRequest<PagedResult<TransactionDto>>;

public class GetTransactionsQueryHandler : IRequestHandler<GetTransactionsQuery, PagedResult<TransactionDto>>
{
    private readonly ITransactionRepository _transactionRepo;

    public GetTransactionsQueryHandler(ITransactionRepository transactionRepo) => _transactionRepo = transactionRepo;

    public async Task<PagedResult<TransactionDto>> Handle(GetTransactionsQuery query, CancellationToken ct)
    {
        var (items, total) = await _transactionRepo.GetByFarmIdAsync(query.FarmId, query.Page, query.PageSize, query.PendingReview, ct);

        var dtos = items.Select(t => new TransactionDto(
            t.Id, t.FarmId, t.MonoId, t.Narration, t.Amount, t.Date, t.Category,
            t.CropAllocation, t.Confidence, t.Source, t.ReviewedByUser, t.ReviewNotes)).ToList();

        return new PagedResult<TransactionDto>(dtos, total, query.Page, query.PageSize);
    }
}
