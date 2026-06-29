using System.Text;
using System.Text.Json;
using AgroMesh.Application.Interfaces;
using AgroMesh.Domain.Entities;
using AgroMesh.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AgroMesh.Infrastructure.Services.Financial;

public class FinancialCategorizationService : IFinancialCategorizationService
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly ILogger<FinancialCategorizationService> _logger;
    private const string Model = "claude-sonnet-4-6";
    private const int MaxPerBatch = 100;

    public FinancialCategorizationService(
        IHttpClientFactory httpFactory,
        IConfiguration config,
        ILogger<FinancialCategorizationService> logger)
    {
        _http = httpFactory.CreateClient("claude-client");
        _apiKey = config["Anthropic:ApiKey"] ?? throw new InvalidOperationException("Anthropic:ApiKey is required");
        _logger = logger;
    }

    public async Task<CategorizationResult> CategorizeAsync(string narration, decimal amount, CancellationToken ct = default)
    {
        var (kwCat, kwConf) = KeywordDictionary.TryMatch(narration);
        if (kwCat.HasValue) return new CategorizationResult(kwCat.Value, kwConf, CategorizationSource.Dictionary);

        var (ptCat, ptConf) = PatternMatcher.TryMatch(narration);
        if (ptCat.HasValue) return new CategorizationResult(ptCat.Value, ptConf, CategorizationSource.Pattern);

        try
        {
            var result = await CallClaudeForSingleAsync(narration, amount, ct);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Claude categorization failed for narration: {Narration}", narration);
            return new CategorizationResult(TransactionCategory.Other, ConfidenceLevel.Low, CategorizationSource.Dictionary);
        }
    }

    public async Task BatchCategorizeAsync(List<Transaction> transactions, CancellationToken ct = default)
    {
        var uncategorized = transactions.Where(t => !t.ReviewedByUser).ToList();
        if (!uncategorized.Any()) return;

        var batches = uncategorized.Chunk(MaxPerBatch);
        foreach (var batch in batches)
        {
            await ProcessBatchWithKeywordsAsync(batch.ToList(), ct);
        }
    }

    private async Task ProcessBatchWithKeywordsAsync(List<Transaction> transactions, CancellationToken ct)
    {
        var needsClaude = new List<Transaction>();

        foreach (var t in transactions)
        {
            var (kwCat, kwConf) = KeywordDictionary.TryMatch(t.Narration);
            if (kwCat.HasValue)
            {
                t.Review(kwCat.Value, null);
                continue;
            }

            var (ptCat, _) = PatternMatcher.TryMatch(t.Narration);
            if (ptCat.HasValue)
            {
                t.Review(ptCat.Value, null);
                continue;
            }

            needsClaude.Add(t);
        }

        if (needsClaude.Any())
        {
            try
            {
                await CallClaudeForBatchAsync(needsClaude, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Claude batch categorization failed for {Count} transactions", needsClaude.Count);
            }
        }
    }

    private async Task<CategorizationResult> CallClaudeForSingleAsync(string narration, decimal amount, CancellationToken ct)
    {
        var categories = Enum.GetNames<TransactionCategory>();
        var payload = new
        {
            model = Model,
            max_tokens = 100,
            system = "You are a financial transaction categorizer for Nigerian farms. Return ONLY a JSON object: {\"category\": string, \"confidence\": \"High\"|\"Medium\"|\"Low\"}. Valid categories: " + string.Join(", ", categories),
            messages = new[] { new { role = "user", content = $"Categorize: \"{narration}\" Amount: {amount}" } }
        };

        var response = await SendClaudeRequestAsync(payload, ct);
        using var doc = JsonDocument.Parse(response);
        var text = doc.RootElement.GetProperty("content")[0].GetProperty("text").GetString() ?? "{}";
        using var result = JsonDocument.Parse(text);

        Enum.TryParse<TransactionCategory>(result.RootElement.GetProperty("category").GetString(), out var cat);
        Enum.TryParse<ConfidenceLevel>(result.RootElement.GetProperty("confidence").GetString(), out var conf);
        return new CategorizationResult(cat, conf, CategorizationSource.Llm);
    }

    private async Task CallClaudeForBatchAsync(List<Transaction> transactions, CancellationToken ct)
    {
        var categories = Enum.GetNames<TransactionCategory>();
        var items = transactions.Select((t, i) => $"{i}: \"{t.Narration}\" {t.Amount}");
        var payload = new
        {
            model = Model,
            max_tokens = 2000,
            system = "You are a financial transaction categorizer for Nigerian farms. Return ONLY a JSON array where each item is {\"index\": number, \"category\": string, \"confidence\": \"High\"|\"Medium\"|\"Low\"}. Valid categories: " + string.Join(", ", categories),
            messages = new[] { new { role = "user", content = "Categorize these transactions:\n" + string.Join("\n", items) } }
        };

        var response = await SendClaudeRequestAsync(payload, ct);
        using var doc = JsonDocument.Parse(response);
        var text = doc.RootElement.GetProperty("content")[0].GetProperty("text").GetString() ?? "[]";
        var results = JsonSerializer.Deserialize<List<JsonElement>>(text.Trim()) ?? [];

        foreach (var r in results)
        {
            var idx = r.GetProperty("index").GetInt32();
            if (idx < 0 || idx >= transactions.Count) continue;
            Enum.TryParse<TransactionCategory>(r.GetProperty("category").GetString(), out var cat);
            transactions[idx].Review(cat, null);
        }
    }

    private async Task<string> SendClaudeRequestAsync(object payload, CancellationToken ct)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
        request.Headers.Add("x-api-key", _apiKey);
        request.Headers.Add("anthropic-version", "2023-06-01");
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await _http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(ct);
    }
}
