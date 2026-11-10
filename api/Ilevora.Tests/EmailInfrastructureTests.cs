using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text.Json;
using Ilevora.Application.Email;
using Ilevora.Application.Interfaces;
using Ilevora.Infrastructure.Services.Email;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Ilevora.Tests;

public class EmailTemplateTests
{
    [Fact]
    public void Invitation_HasLink_Brand_NoLogo_AndEncodesNames()
    {
        var m = EmailTemplates.Invitation("a@b.com", "<script>Ada</script>", "Green & Co", "Supervisor", "https://app.test/accept-invite?token=abc", 7);
        Assert.Equal("a@b.com", m.To);
        Assert.Contains("https://app.test/accept-invite?token=abc", m.Html);
        Assert.Contains("https://app.test/accept-invite?token=abc", m.Text);
        Assert.Contains("#138A44", m.Html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Plus Jakarta Sans", m.Html);
        Assert.DoesNotContain("<script>", m.Html);
        Assert.Contains("Green &amp; Co", m.Html);
        Assert.DoesNotContain("<img", m.Html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Green & Co", m.Subject);
    }

    [Fact]
    public void PasswordReset_Changed_Welcome_AreBuilt()
    {
        var reset = EmailTemplates.PasswordReset("a@b.com", "Ada", "https://x/r?token=t", 60);
        Assert.Contains("https://x/r?token=t", reset.Html);
        Assert.Contains("https://x/r?token=t", reset.Text);
        Assert.Contains("ignore", reset.Text, StringComparison.OrdinalIgnoreCase);

        var changed = EmailTemplates.PasswordChanged("a@b.com", "Ada");
        Assert.Contains("changed", changed.Subject, StringComparison.OrdinalIgnoreCase);

        var welcome = EmailTemplates.Welcome("a@b.com", "Ada", "https://app.test/");
        Assert.Contains("Welcome", welcome.Subject);
        Assert.Contains("https://app.test/", welcome.Html);
    }
}

public class EmailSenderTests
{
    private class StubHandler(HttpStatusCode status = HttpStatusCode.OK, bool throwOnSend = false) : HttpMessageHandler
    {
        public List<(HttpRequestMessage req, string body)> Calls = [];
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Calls.Add((request, request.Content is null ? "" : await request.Content.ReadAsStringAsync(ct)));
            if (throwOnSend) throw new HttpRequestException("boom");
            return new HttpResponseMessage(status) { Content = new StringContent("{}") };
        }
    }

    private static ResendEmailTransport Make(StubHandler h, ResendOptions o) =>
        new(new HttpClient(h), Options.Create(o), NullLogger<ResendEmailTransport>.Instance) { RetryDelay = TimeSpan.Zero };

    private static readonly EmailMessage Msg = new("ada@farm.com", "Hi", "<p>Hi</p>", "Hi");

    [Fact]
    public async Task Enabled_PostsToResend_WithBearerAndPayload()
    {
        var h = new StubHandler();
        var ok = await Make(h, new() { ApiKey = "re_secret", FromAddress = "Ilevora <no-reply@ilevora.com>", Enabled = true }).DeliverAsync(Msg);

        Assert.True(ok);
        var (req, body) = Assert.Single(h.Calls);
        Assert.Equal("https://api.resend.com/emails", req.RequestUri!.ToString());
        Assert.Equal("Bearer", req.Headers.Authorization!.Scheme);
        Assert.Equal("re_secret", req.Headers.Authorization.Parameter);
        var json = JsonDocument.Parse(body).RootElement;
        Assert.Equal("Ilevora <no-reply@ilevora.com>", json.GetProperty("from").GetString());
        Assert.Equal("ada@farm.com", json.GetProperty("to")[0].GetString());
        Assert.Equal("Hi", json.GetProperty("subject").GetString());
        Assert.Equal("<p>Hi</p>", json.GetProperty("html").GetString());
        Assert.Equal("Hi", json.GetProperty("text").GetString());
    }

    [Theory]
    [InlineData(false, "key")]
    [InlineData(true, "")]
    [InlineData(true, null)]
    public async Task DisabledOrKeyless_SkipsWithoutHttpCall(bool enabled, string? key)
    {
        var h = new StubHandler();
        var ok = await Make(h, new() { ApiKey = key, FromAddress = "a@b.com", Enabled = enabled }).DeliverAsync(Msg);
        Assert.False(ok);
        Assert.Empty(h.Calls);
    }

    [Fact]
    public async Task ServerError_RetriesOnce_ThenReturnsFalse_WithoutThrowing()
    {
        var h = new StubHandler(HttpStatusCode.InternalServerError);
        var ok = await Make(h, new() { ApiKey = "k", FromAddress = "a@b.com", Enabled = true }).DeliverAsync(Msg);
        Assert.False(ok);
        Assert.Equal(2, h.Calls.Count);
    }

    [Fact]
    public async Task ClientError_NoRetry_ReturnsFalse()
    {
        var h = new StubHandler(HttpStatusCode.UnprocessableEntity);
        Assert.False(await Make(h, new() { ApiKey = "k", FromAddress = "a@b.com", Enabled = true }).DeliverAsync(Msg));
        Assert.Single(h.Calls);
    }

    [Fact]
    public async Task NetworkException_IsSwallowed()
    {
        var h = new StubHandler(throwOnSend: true);
        Assert.False(await Make(h, new() { ApiKey = "k", FromAddress = "a@b.com", Enabled = true }).DeliverAsync(Msg));
    }

    [Fact]
    public async Task OutboxSender_WhenDisabled_SkipsWithoutTouchingTheDatabase()
    {
        var db = new Ilevora.Infrastructure.Persistence.IlevoraDbContext(
            new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<Ilevora.Infrastructure.Persistence.IlevoraDbContext>().UseNpgsql("Host=unreachable").Options);
        IEmailSender sender = new OutboxEmailSender(db, Options.Create(new ResendOptions { Enabled = false }), TimeProvider.System, NullLogger<OutboxEmailSender>.Instance);
        await sender.SendAsync(Msg);
        Assert.Empty(db.ChangeTracker.Entries());
    }

    [Fact]
    public void AppUrls_BuildLinks_FromBaseUrl_WithEncodedToken()
    {
        var urls = new AppUrls(Options.Create(new AppOptions { WebBaseUrl = "https://app.ilevora.com/" }));
        Assert.Equal("https://app.ilevora.com/accept-invite?token=a%2Bb", urls.AcceptInvite("a+b"));
        Assert.Equal("https://app.ilevora.com/reset-password?token=t", urls.ResetPassword("t"));
        Assert.Equal("https://app.ilevora.com/", urls.Login);
    }
}
