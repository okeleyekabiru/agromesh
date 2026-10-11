namespace AgroMesh.Application.Interfaces;

public interface IMonoBankService
{
    /// <summary>Swaps the one-time code from the Mono Connect widget for the permanent account id.</summary>
    Task<string> ExchangeCodeAsync(string code, CancellationToken ct = default);

    /// <summary>Pulls the account's transactions, storing the new ones. Returns how many were new.</summary>
    Task<int> SyncTransactionsAsync(string farmId, string monoAccountId, CancellationToken ct = default);

    /// <summary>Unlinks the account at Mono. Best effort: the farm forgets the account either way.</summary>
    Task UnlinkAsync(string monoAccountId, CancellationToken ct = default);
}
