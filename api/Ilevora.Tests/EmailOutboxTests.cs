using Microsoft.Extensions.Configuration;
using Ilevora.Application.Interfaces;
using Ilevora.Domain.Entities;
using Ilevora.Domain.Enums;
using Ilevora.Infrastructure.Services.Email;

namespace Ilevora.Tests;

public class EmailOutboxMessageTests
{
    private static readonly DateTime Now = new(2026, 11, 10, 12, 0, 0, DateTimeKind.Utc);
    private static EmailOutboxMessage New() => EmailOutboxMessage.Create("a@b.com", "Hi", "<p>link?token=abc</p>", "link?token=abc", Now);

    [Fact]
    public void Create_IsPendingAndDue()
    {
        var m = New();
        Assert.Equal(EmailOutboxStatus.Pending, m.Status);
        Assert.Equal(0, m.Attempts);
        Assert.Equal(Now, m.NextAttemptAt);
    }

    [Fact]
    public void MarkSent_ClearsBody_SoTokensDoNotLinger()
    {
        var m = New();
        m.MarkSent(Now);
        Assert.Equal(EmailOutboxStatus.Sent, m.Status);
        Assert.Equal(Now, m.SentAt);
        Assert.Equal("", m.Html);
        Assert.Equal("", m.Text);
    }

    [Fact]
    public void Failure_BacksOffThenFailsAtTheCap_AndClearsBody()
    {
        var m = New();
        var previous = Now;
        for (var attempt = 1; attempt < EmailOutboxMessage.MaxAttempts; attempt++)
        {
            m.RecordClaim(Now, TimeSpan.FromMinutes(2));
            m.RecordFailure("HttpRequestException", Now);
            Assert.Equal(EmailOutboxStatus.Pending, m.Status);
            Assert.True(m.NextAttemptAt > previous);
            previous = m.NextAttemptAt;
        }
        m.RecordClaim(Now, TimeSpan.FromMinutes(2));
        m.RecordFailure("HttpRequestException", Now);
        Assert.Equal(EmailOutboxStatus.Failed, m.Status);
        Assert.Equal("HttpRequestException", m.LastError);
        Assert.Equal("", m.Html);
    }
}

public class EmailOutboxProcessorTests
{
    private static readonly DateTime Now = new(2026, 11, 10, 12, 0, 0, DateTimeKind.Utc);

    private class FakeStore : IEmailOutboxStore
    {
        public List<EmailOutboxMessage> Rows = [];
        public int Purged;
        public Task<List<EmailOutboxMessage>> ClaimDueAsync(int max, DateTime now, TimeSpan lease, CancellationToken ct)
        {
            var due = Rows.Where(r => (r.Status == EmailOutboxStatus.Pending && r.NextAttemptAt <= now)
                                      || (r.Status == EmailOutboxStatus.Sending && r.LeaseUntil < now)).Take(max).ToList();
            foreach (var r in due) r.RecordClaim(now, lease);
            return Task.FromResult(due);
        }
        public Task UpdateAsync(EmailOutboxMessage m, CancellationToken ct) => Task.CompletedTask;
        public Task<int> PurgeAsync(DateTime olderThan, CancellationToken ct) { Purged++; return Task.FromResult(0); }
    }

    private class FakeTransport(Func<EmailMessage, bool> result) : IEmailTransport
    {
        public List<EmailMessage> Sent = [];
        public Task<bool> DeliverAsync(EmailMessage m, CancellationToken ct = default) { Sent.Add(m); return Task.FromResult(result(m)); }
    }

    private static (EmailOutboxProcessor p, FakeStore s) Make(IEmailTransport t, params EmailOutboxMessage[] rows)
    {
        var s = new FakeStore { Rows = rows.ToList() };
        return (new EmailOutboxProcessor(s, t), s);
    }

    private static EmailOutboxMessage Row() => EmailOutboxMessage.Create("a@b.com", "Hi", "<p>x</p>", "x", Now);

    [Fact]
    public async Task Delivers_DueRows_AndMarksSent()
    {
        var t = new FakeTransport(_ => true);
        var (p, s) = Make(t, Row());
        await p.RunOnceAsync(Now, default);
        Assert.Single(t.Sent);
        Assert.Equal(EmailOutboxStatus.Sent, s.Rows[0].Status);
    }

    [Fact]
    public async Task FailedDelivery_IsRescheduled_NotLost_AndNotRetriedBeforeBackoff()
    {
        var t = new FakeTransport(_ => false);
        var (p, s) = Make(t, Row());
        await p.RunOnceAsync(Now, default);
        Assert.Equal(EmailOutboxStatus.Pending, s.Rows[0].Status);
        Assert.Equal(1, s.Rows[0].Attempts);

        await p.RunOnceAsync(Now.AddSeconds(5), default);
        Assert.Single(t.Sent);                       // still backing off
        await p.RunOnceAsync(Now.AddMinutes(2), default);
        Assert.Equal(2, t.Sent.Count);
    }

    [Fact]
    public async Task TransportThrowing_IsTreatedAsFailure()
    {
        var (p, s) = Make(new FakeTransport(_ => throw new InvalidOperationException("boom")), Row());
        await p.RunOnceAsync(Now, default);
        Assert.Equal(EmailOutboxStatus.Pending, s.Rows[0].Status);
        Assert.Equal("InvalidOperationException", s.Rows[0].LastError);
    }

    [Fact]
    public async Task GivesUpAfterTheCap()
    {
        var (p, s) = Make(new FakeTransport(_ => false), Row());
        var t = Now;
        for (var i = 0; i < 10; i++) { await p.RunOnceAsync(t, default); t = t.AddDays(1); }
        Assert.Equal(EmailOutboxStatus.Failed, s.Rows[0].Status);
        Assert.Equal(EmailOutboxMessage.MaxAttempts, s.Rows[0].Attempts);
    }

    [Fact]
    public async Task RowLeftSendingByACrashedInstance_IsReclaimedAfterTheLease()
    {
        var row = Row();
        row.RecordClaim(Now, TimeSpan.FromMinutes(2));           // an instance claimed it and died
        var t = new FakeTransport(_ => true);
        var (p, s) = Make(t, row);

        await p.RunOnceAsync(Now.AddMinutes(1), default);
        Assert.Empty(t.Sent);                                    // lease still held
        await p.RunOnceAsync(Now.AddMinutes(3), default);
        Assert.Single(t.Sent);
        Assert.Equal(EmailOutboxStatus.Sent, s.Rows[0].Status);
    }

    [Fact]
    public async Task SentAndFailedRows_AreNotPickedUpAgain()
    {
        var done = Row(); done.MarkSent(Now);
        var t = new FakeTransport(_ => true);
        var (p, _) = Make(t, done);
        await p.RunOnceAsync(Now.AddDays(1), default);
        Assert.Empty(t.Sent);
    }

    [Fact]
    public async Task Purge_IsRequestedForOldRows()
    {
        var (p, s) = Make(new FakeTransport(_ => true));
        await p.PurgeAsync(Now, default);
        Assert.Equal(1, s.Purged);
    }
}

public class UserSessionAndPreferenceTests
{
    [Fact]
    public void SetPasswordHash_BumpsTokenVersion_SoOldTokensStopWorking()
    {
        var u = User.CreateStaff("A", "a@b.com", "h1", "co", UserRole.Supervisor);
        Assert.True(u.IsTokenCurrent(u.TokenVersion));
        var old = u.TokenVersion;
        u.SetPasswordHash("h2");
        Assert.False(u.IsTokenCurrent(old));
        Assert.True(u.IsTokenCurrent(u.TokenVersion));
    }

    [Fact]
    public void TokenWithoutVersionClaim_IsOnlyValid_WhilePasswordNeverChanged()
    {
        var u = User.CreateStaff("A", "a@b.com", "h1", "co", UserRole.Supervisor);
        Assert.True(u.IsTokenCurrent(null));
        u.SetPasswordHash("h2");
        Assert.False(u.IsTokenCurrent(null));
    }

    [Fact]
    public void EmailNotifications_DefaultOn_AndCanBeToggled()
    {
        var u = User.CreateStaff("A", "a@b.com", "h1", "co", UserRole.Supervisor);
        Assert.True(u.EmailNotificationsEnabled);
        u.SetEmailNotifications(false);
        Assert.False(u.EmailNotificationsEnabled);
    }

    [Fact]
    public void JwtService_EmbedsTokenVersion()
    {
        var cfg = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:Key"] = "0123456789012345678901234567890123456789" }).Build();
        var u = User.CreateStaff("A", "a@b.com", "h1", "co", UserRole.Supervisor);
        u.SetPasswordHash("h2");
        var jwt = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler()
            .ReadJwtToken(new Ilevora.Infrastructure.Services.Auth.JwtService(cfg).GenerateToken(u));
        Assert.Equal(u.TokenVersion.ToString(), jwt.Claims.First(c => c.Type == "tv").Value);
    }
}
