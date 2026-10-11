using System.Globalization;
using System.Text;
using System.Text.Json;
using AgroMesh.Application.Interfaces;
using AgroMesh.Application.Services;
using AgroMesh.Domain.Entities;
using AgroMesh.Domain.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AgroMesh.Infrastructure.Services.Financial;

public record MonoTransactionDto(string Id, string Narration, decimal Amount, DateTime Date, string Type);

public class MonoApiService : IMonoBankService
{
    private const string BaseUrl = "https://api.withmono.com";
    // Safety stop so a misbehaving account can never loop forever; 50 pages of 100 is far beyond a farm's needs.
    private const int PageSize = 100;
    private const int MaxPages = 50;

    private readonly HttpClient _http;
    private readonly string? _secretKey;
    private readonly ITransactionRepository _transactionRepo;
    private readonly IFinancialCategorizationService _categorizationService;
    private readonly ILogger<MonoApiService> _logger;
    private readonly PaymentReconciler _reconciler;

    public MonoApiService(
        IHttpClientFactory httpFactory,
        IConfiguration config,
        ITransactionRepository transactionRepo,
        IFinancialCategorizationService categorizationService,
        ILogger<MonoApiService> logger,
        PaymentReconciler reconciler)
    {
        _reconciler = reconciler;
        _http = httpFactory.CreateClient("mono-client");
        _secretKey = config["Mono:SecretKey"];
        _transactionRepo = transactionRepo;
        _categorizationService = categorizationService;
        _logger = logger;
    }

    public async Task<string> ExchangeCodeAsync(string code, CancellationToken ct = default)
    {
        RequireKey();
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/v2/accounts/auth")
        {
            Content = new StringContent(JsonSerializer.Serialize(new { code }), Encoding.UTF8, "application/json")
        };
        request.Headers.Add("mono-sec-key", _secretKey);
        request.Headers.Add("accept", "application/json");

        using var response = await _http.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Mono code exchange failed: {Status} {Body}", (int)response.StatusCode, body.Length > 300 ? body[..300] : body);
            throw new DomainException("The bank connection could not be completed. Please try linking the account again.");
        }

        using var doc = JsonDocument.Parse(body);
        // The id is documented at the top level; some responses wrap it in "data".
        var id = TryString(doc.RootElement, "id") ?? (doc.RootElement.TryGetProperty("data", out var d) ? TryString(d, "id") : null);
        return string.IsNullOrWhiteSpace(id) ? throw new DomainException("The bank did not return an account.") : id;
    }

    public async Task UnlinkAsync(string monoAccountId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_secretKey)) return;
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/v2/accounts/{monoAccountId}/unlink");
            request.Headers.Add("mono-sec-key", _secretKey);
            using var response = await _http.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
                _logger.LogWarning("Mono unlink returned {Status}; the farm forgets the account anyway.", (int)response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Mono unlink failed; the farm forgets the account anyway.");
        }
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

        // Oldest first, so an order is placed/paid in the sequence the bank saw it.
        foreach (var dto in transactions.OrderBy(t => t.Date))
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

            // A debit that names an order ("ORD-1042/chemical") either pays it off or is flagged for a person.
            try { await _reconciler.ReconcileAsync(transaction, ct); }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not reconcile transaction {MonoId} against an order", dto.Id);
            }
        }

        return newCount;
    }

    private async Task<List<MonoTransactionDto>> FetchTransactionsAsync(string monoAccountId, CancellationToken ct)
    {
        var results = new List<MonoTransactionDto>();

        for (var page = 1; page <= MaxPages; page++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get,
                $"{BaseUrl}/v2/accounts/{monoAccountId}/transactions?paginate=true&limit={PageSize}&page={page}");
            request.Headers.Add("mono-sec-key", _secretKey);

            using var response = await _http.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            if (!doc.RootElement.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array) break;

            var onPage = 0;
            foreach (var item in data.EnumerateArray())
            {
                onPage++;
                results.Add(new MonoTransactionDto(
                    TryString(item, "id") ?? TryString(item, "_id") ?? "",
                    TryString(item, "narration") ?? "",
                    item.GetProperty("amount").GetDecimal() / 100m, // Mono sends kobo; the farm works in naira
                    // Mono dates are UTC ("...Z"). Parse them as UTC so the database accepts them.
                    DateTime.Parse(item.GetProperty("date").GetString()!, CultureInfo.InvariantCulture,
                        DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal),
                    TryString(item, "type") ?? "debit"));
            }

            // Stop when the page is short or Mono says there is no next page.
            var hasNext = doc.RootElement.TryGetProperty("meta", out var meta)
                          && meta.TryGetProperty("next", out var next) && next.ValueKind == JsonValueKind.String;
            if (onPage < PageSize || !hasNext) break;
        }

        return results.Where(t => t.Id.Length > 0).ToList();
    }

    private void RequireKey()
    {
        if (string.IsNullOrWhiteSpace(_secretKey))
            throw new DomainException("Bank linking is not set up on the server yet (the Mono key is missing).");
    }

    private static string? TryString(JsonElement el, string name) =>
        el.ValueKind == JsonValueKind.Object && el.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
}
