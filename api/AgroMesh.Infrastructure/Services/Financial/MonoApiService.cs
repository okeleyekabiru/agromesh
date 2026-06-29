using System.Text.Json;
using AgroMesh.Application.Interfaces;
using AgroMesh.Domain.Entities;
using AgroMesh.Domain.Enums;
// IMonoBankService
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AgroMesh.Infrastructure.Services.Financial;

public record MonoTransactionDto(string Id, string Narration, decimal Amount, DateTime Date, string Type);

public class MonoApiService : IMonoBankService
{
    private readonly HttpClient _http;
    private readonly string? _secretKey;
    private readonly ITransactionRepository _transactionRepo;
    private readonly IFinancialCategorizationService _categorizationService;
    private readonly ILogger<MonoApiService> _logger;

    public MonoApiService(
        IHttpClientFactory httpFactory,
        IConfiguration config,
        ITransactionRepository transactionRepo,
        IFinancialCategorizationService categorizationService,
        ILogger<MonoApiService> logger)
    {
        _http = httpFactory.CreateClient("mono-client");
        _secretKey = config["Mono:SecretKey"];
        _transactionRepo = transactionRepo;
        _categorizationService = categorizationService;
        _logger = logger;
    }

    public async Task<int> SyncTransactionsAsync(string farmId, string monoAccountId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_secretKey))
        {
            _logger.LogWarning("Mono:SecretKey not configured — skipping sync for farm {FarmId}", farmId);
            return 0;
        }

        var transactions = await FetchTransactionsAsync(monoAccountId, ct);
        var newCount = 0;

        foreach (var dto in transactions)
        {
            if (await _transactionRepo.ExistsByMonoIdAsync(dto.Id, ct))
                continue; // idempotency — never drop, never duplicate

            var amount = dto.Type == "debit" ? -Math.Abs(dto.Amount) : Math.Abs(dto.Amount);
            var result = await _categorizationService.CategorizeAsync(dto.Narration, amount, ct);

            var transaction = Transaction.Create(
                farmId, dto.Id, dto.Narration, amount, dto.Date,
                result.Category, result.Confidence, result.Source);

            await _transactionRepo.AddAsync(transaction, ct);
            newCount++;
        }

        return newCount;
    }

    private async Task<List<MonoTransactionDto>> FetchTransactionsAsync(string monoAccountId, CancellationToken ct)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.withmono.com/v2/accounts/{monoAccountId}/transactions");
        request.Headers.Add("mono-sec-key", _secretKey);

        var response = await _http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);

        var results = new List<MonoTransactionDto>();
        if (!doc.RootElement.TryGetProperty("data", out var data)) return results;

        foreach (var item in data.EnumerateArray())
        {
            results.Add(new MonoTransactionDto(
                item.GetProperty("id").GetString() ?? "",
                item.GetProperty("narration").GetString() ?? "",
                item.GetProperty("amount").GetDecimal() / 100m, // kobo to naira
                DateTime.Parse(item.GetProperty("date").GetString()!),
                item.GetProperty("type").GetString() ?? "debit"));
        }

        return results;
    }
}
