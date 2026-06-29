namespace AgroMesh.Application.Interfaces;

public interface IMonoBankService
{
    Task<int> SyncTransactionsAsync(string farmId, string monoAccountId, CancellationToken ct = default);
}
