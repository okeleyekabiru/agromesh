using Ilevora.Application.Interfaces;
using Ilevora.Domain.Entities;
using Ilevora.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ilevora.Infrastructure.Services.Email;

public interface IEmailTransport
{
    Task<bool> DeliverAsync(EmailMessage message, CancellationToken ct = default);
}

public interface IEmailOutboxStore
{
    /// <summary>Atomically claims up to <paramref name="max"/> due rows (pending and due, or sending with an expired lease).</summary>
    Task<List<EmailOutboxMessage>> ClaimDueAsync(int max, DateTime now, TimeSpan lease, CancellationToken ct);
    Task UpdateAsync(EmailOutboxMessage message, CancellationToken ct);
    Task<int> PurgeAsync(DateTime olderThan, CancellationToken ct);
}

public class EmailOutboxStore : IEmailOutboxStore
{
    private readonly IlevoraDbContext _db;

    public EmailOutboxStore(IlevoraDbContext db) => _db = db;

    public async Task<List<EmailOutboxMessage>> ClaimDueAsync(int max, DateTime now, TimeSpan lease, CancellationToken ct)
    {
        var candidates = await _db.EmailOutbox.AsNoTracking()
            .Where(m => (m.Status == EmailOutboxStatus.Pending && m.NextAttemptAt <= now)
                     || (m.Status == EmailOutboxStatus.Sending && m.LeaseUntil < now))
            .OrderBy(m => m.NextAttemptAt).Select(m => m.Id).Take(max).ToListAsync(ct);

        var token = Guid.NewGuid().ToString("N");
        var leaseUntil = now + lease;
        foreach (var id in candidates)
        {
            // One UPDATE per row, guarded by the same "still due" condition: if another instance got there first, 0 rows change
            // and this instance simply skips the row.
            await _db.EmailOutbox
                .Where(m => m.Id == id
                    && ((m.Status == EmailOutboxStatus.Pending && m.NextAttemptAt <= now)
                     || (m.Status == EmailOutboxStatus.Sending && m.LeaseUntil < now)))
                .ExecuteUpdateAsync(u => u
                    .SetProperty(m => m.Status, EmailOutboxStatus.Sending)
                    .SetProperty(m => m.Attempts, m => m.Attempts + 1)
                    .SetProperty(m => m.LeaseUntil, leaseUntil)
                    .SetProperty(m => m.ClaimToken, token)
                    .SetProperty(m => m.UpdatedAt, now), ct);
        }
        return await _db.EmailOutbox.Where(m => m.ClaimToken == token).ToListAsync(ct);
    }

    public Task UpdateAsync(EmailOutboxMessage message, CancellationToken ct) => _db.SaveChangesAsync(ct);

    public Task<int> PurgeAsync(DateTime olderThan, CancellationToken ct) =>
        _db.EmailOutbox.Where(m => (m.Status == EmailOutboxStatus.Sent || m.Status == EmailOutboxStatus.Failed) && m.UpdatedAt < olderThan)
            .ExecuteDeleteAsync(ct);
}

public class EmailOutboxProcessor
{
    public static readonly TimeSpan Lease = TimeSpan.FromMinutes(2);
    public static readonly TimeSpan Retention = TimeSpan.FromDays(7);
    private readonly IEmailOutboxStore _store;
    private readonly IEmailTransport _transport;

    public EmailOutboxProcessor(IEmailOutboxStore store, IEmailTransport transport)
    {
        _store = store;
        _transport = transport;
    }

    /// <summary>Sends everything that is due. Returns how many rows were handled.</summary>
    public async Task<int> RunOnceAsync(DateTime now, CancellationToken ct)
    {
        var rows = await _store.ClaimDueAsync(10, now, Lease, ct);
        foreach (var row in rows)
        {
            try
            {
                var ok = await _transport.DeliverAsync(new EmailMessage(row.To, row.Subject, row.Html, row.Text), ct);
                if (ok) row.MarkSent(now); else row.RecordFailure("Delivery failed", now);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                row.RecordFailure(ex.GetType().Name, now);
            }
            await _store.UpdateAsync(row, ct);
        }
        return rows.Count;
    }

    public Task PurgeAsync(DateTime now, CancellationToken ct) => _store.PurgeAsync(now - Retention, ct);
}

/// <summary>The IEmailSender the app uses: writes a row and returns. A separate service delivers, so email survives restarts and never slows a request.</summary>
public class OutboxEmailSender : IEmailSender
{
    private readonly IlevoraDbContext _db;
    private readonly ResendOptions _options;
    private readonly TimeProvider _clock;
    private readonly ILogger<OutboxEmailSender> _logger;

    public OutboxEmailSender(IlevoraDbContext db, IOptions<ResendOptions> options, TimeProvider clock, ILogger<OutboxEmailSender> logger)
    {
        _db = db; _options = options.Value; _clock = clock; _logger = logger;
    }

    public async Task SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        if (!_options.Enabled || string.IsNullOrWhiteSpace(_options.ApiKey) || string.IsNullOrWhiteSpace(_options.FromAddress))
        {
            _logger.LogInformation("Email not sent (Resend disabled or not configured): to {To}, subject {Subject}", message.To, message.Subject);
            return;
        }
        _db.EmailOutbox.Add(EmailOutboxMessage.Create(message.To, message.Subject, message.Html, message.Text, _clock.GetUtcNow().UtcDateTime));
        await _db.SaveChangesAsync(ct);
    }
}

public class EmailDispatchService : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly TimeProvider _clock;
    private readonly ILogger<EmailDispatchService> _logger;

    public EmailDispatchService(IServiceScopeFactory scopes, TimeProvider clock, ILogger<EmailDispatchService> logger)
    {
        _scopes = scopes; _clock = clock; _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var lastPurge = DateTime.MinValue;
        while (!stoppingToken.IsCancellationRequested)
        {
            var handled = 0;
            try
            {
                using var scope = _scopes.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<EmailOutboxProcessor>();
                var now = _clock.GetUtcNow().UtcDateTime;
                handled = await processor.RunOnceAsync(now, stoppingToken);
                if (now - lastPurge > TimeSpan.FromHours(6))
                {
                    await processor.PurgeAsync(now, stoppingToken);
                    lastPurge = now;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError("Email outbox run failed: {Error}", ex.GetType().Name);
            }
            try { await Task.Delay(handled > 0 ? TimeSpan.FromSeconds(1) : TimeSpan.FromSeconds(10), stoppingToken); }
            catch (OperationCanceledException) { }
        }
    }
}
