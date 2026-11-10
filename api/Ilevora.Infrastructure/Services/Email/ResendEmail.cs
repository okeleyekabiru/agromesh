using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Ilevora.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ilevora.Infrastructure.Services.Email;

public class ResendOptions
{
    public string? ApiKey { get; set; }
    public string? FromAddress { get; set; }
    public bool Enabled { get; set; }
}

public class AppOptions
{
    /// <summary>Public base URL of the web app, used for links in emails.</summary>
    public string WebBaseUrl { get; set; } = "http://localhost:5173";
}

public class AppUrls : IAppUrls
{
    private readonly string _base;

    public AppUrls(IOptions<AppOptions> options) => _base = options.Value.WebBaseUrl.TrimEnd('/');

    public string AcceptInvite(string token) => $"{_base}/accept-invite?token={Uri.EscapeDataString(token)}";
    public string ResetPassword(string token) => $"{_base}/reset-password?token={Uri.EscapeDataString(token)}";
    public string Login => _base + "/";
}

/// <summary>Delivers one message through the Resend HTTP API. Never throws: a delivery problem is logged (never the key or body) and reported as false.</summary>
public class ResendEmailTransport : IEmailTransport
{
    private readonly HttpClient _http;
    private readonly ResendOptions _options;
    private readonly ILogger<ResendEmailTransport> _logger;

    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(2);

    public ResendEmailTransport(HttpClient http, IOptions<ResendOptions> options, ILogger<ResendEmailTransport> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<bool> DeliverAsync(EmailMessage message, CancellationToken ct = default)
    {
        if (!_options.Enabled || string.IsNullOrWhiteSpace(_options.ApiKey) || string.IsNullOrWhiteSpace(_options.FromAddress))
        {
            _logger.LogInformation("Email not sent (Resend disabled or not configured): to {To}, subject {Subject}", message.To, message.Subject);
            return false;
        }

        var payload = JsonSerializer.Serialize(new { from = _options.FromAddress, to = new[] { message.To }, subject = message.Subject, html = message.Html, text = message.Text });

        for (var attempt = 1; attempt <= 2; attempt++)
        {
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails")
                {
                    Content = new StringContent(payload, Encoding.UTF8, "application/json"),
                };
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
                using var res = await _http.SendAsync(req, ct);
                if (res.IsSuccessStatusCode) return true;

                _logger.LogWarning("Resend rejected an email to {To} with status {Status} (attempt {Attempt})", message.To, (int)res.StatusCode, attempt);
                if ((int)res.StatusCode < 500 && (int)res.StatusCode != 429) return false;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning("Resend call failed for an email to {To}: {Error} (attempt {Attempt})", message.To, ex.GetType().Name, attempt);
            }
            if (attempt == 1 && RetryDelay > TimeSpan.Zero) await Task.Delay(RetryDelay, ct);
        }
        return false;
    }
}

