using Ilevora.Application.Commands.Auth;
using Ilevora.Application.DTOs;
using Ilevora.Application.Interfaces;
using Ilevora.Domain.Entities;
using Ilevora.Domain.Enums;
using Ilevora.Domain.Exceptions;
using Ilevora.Domain.Utilities;

namespace Ilevora.Tests;

public class PasswordResetTests
{
    private const string Generic = "This password reset link is invalid or has expired.";

    private class Fx
    {
        public InMemoryUsers Users = new();
        public InMemoryResetTokens Tokens = new();
        public FakeEmailSender Email = new();
        public TestClock Clock = new(new DateTimeOffset(2026, 11, 10, 12, 0, 0, TimeSpan.Zero));
        public User User = User.CreateStaff("Ada Okafor", "ada@farm.com", "hash:oldpassword", "co-1", UserRole.Supervisor);

        public Fx() => Users.Users.Add(User);
        public ForgotPasswordCommandHandler Forgot() => new(Users, Tokens, Email, new FakeAppUrls(), Clock);
        public ResetPasswordCommandHandler Reset() => new(Users, Tokens, new FakeHasher2(), Email, new FakeAppUrls(), Clock);
        public static string Raw(EmailMessage m) => m.Text.Split("token=")[1].Split(['\n', ' ', '\r'])[0];
    }

    [Fact]
    public async Task Forgot_KnownUser_StoresHashOnly_AndEmailsLink()
    {
        var f = new Fx();
        await f.Forgot().Handle(new(new("ADA@farm.com")), default);

        var mail = Assert.Single(f.Email.Sent);
        var raw = Fx.Raw(mail);
        var token = Assert.Single(f.Tokens.Items);
        Assert.Equal(SecureToken.Hash(raw), token.TokenHash);
        Assert.NotEqual(raw, token.TokenHash);
        Assert.Equal("ada@farm.com", mail.To);
    }

    [Fact]
    public async Task Forgot_UnknownEmail_SendsNothing_AndDoesNotThrow()
    {
        var f = new Fx();
        await f.Forgot().Handle(new(new("ghost@nowhere.com")), default);
        Assert.Empty(f.Email.Sent);
        Assert.Empty(f.Tokens.Items);
    }

    [Fact]
    public async Task Forgot_DisabledUser_SendsNothing()
    {
        var f = new Fx();
        f.User.SetActive(false);
        await f.Forgot().Handle(new(new("ada@farm.com")), default);
        Assert.Empty(f.Email.Sent);
    }

    [Fact]
    public async Task Forgot_Twice_WithinAMinute_IsThrottled_ThenAllowed()
    {
        var f = new Fx();
        await f.Forgot().Handle(new(new("ada@farm.com")), default);
        await f.Forgot().Handle(new(new("ada@farm.com")), default);
        Assert.Single(f.Email.Sent);

        f.Clock.Advance(TimeSpan.FromSeconds(61));
        await f.Forgot().Handle(new(new("ada@farm.com")), default);
        Assert.Equal(2, f.Email.Sent.Count);
    }

    [Fact]
    public async Task Forgot_FailingSender_DoesNotThrow()
    {
        var f = new Fx { Email = { Throw = true } };
        await f.Forgot().Handle(new(new("ada@farm.com")), default);
    }

    [Fact]
    public async Task Reset_SetsPassword_SingleUse_AndNotifies()
    {
        var f = new Fx();
        await f.Forgot().Handle(new(new("ada@farm.com")), default);
        var raw = Fx.Raw(f.Email.Sent[0]);
        f.Email.Sent.Clear();

        await f.Reset().Handle(new(new(raw, "brandnewpass")), default);
        Assert.Equal("hash:brandnewpass", f.User.PasswordHash);
        var changed = Assert.Single(f.Email.Sent);
        Assert.Contains("changed", changed.Subject, StringComparison.OrdinalIgnoreCase);

        var ex = await Assert.ThrowsAsync<DomainException>(() => f.Reset().Handle(new(new(raw, "another1234")), default));
        Assert.Equal(Generic, ex.Message);
        Assert.Equal("hash:brandnewpass", f.User.PasswordHash);
    }

    [Fact]
    public async Task Reset_Expired_Fails()
    {
        var f = new Fx();
        await f.Forgot().Handle(new(new("ada@farm.com")), default);
        var raw = Fx.Raw(f.Email.Sent[0]);
        f.Clock.Advance(TimeSpan.FromMinutes(61));
        var ex = await Assert.ThrowsAsync<DomainException>(() => f.Reset().Handle(new(new(raw, "brandnewpass")), default));
        Assert.Equal(Generic, ex.Message);
        Assert.Equal("hash:oldpassword", f.User.PasswordHash);
    }

    [Fact]
    public async Task Reset_UnknownToken_Fails_WithSameMessage()
    {
        var f = new Fx();
        var ex = await Assert.ThrowsAsync<DomainException>(() => f.Reset().Handle(new(new("nope", "brandnewpass")), default));
        Assert.Equal(Generic, ex.Message);
    }

    [Fact]
    public async Task Reset_ShortPassword_Rejected_TokenStillUsable()
    {
        var f = new Fx();
        await f.Forgot().Handle(new(new("ada@farm.com")), default);
        var raw = Fx.Raw(f.Email.Sent[0]);
        await Assert.ThrowsAsync<DomainException>(() => f.Reset().Handle(new(new(raw, "short")), default));
        await f.Reset().Handle(new(new(raw, "longenough1")), default);
    }

    [Fact]
    public async Task Reset_VoidsOtherOutstandingTokens()
    {
        var f = new Fx();
        await f.Forgot().Handle(new(new("ada@farm.com")), default);
        var first = Fx.Raw(f.Email.Sent[0]);
        f.Clock.Advance(TimeSpan.FromMinutes(5));
        await f.Forgot().Handle(new(new("ada@farm.com")), default);
        var second = Fx.Raw(f.Email.Sent[1]);

        await f.Reset().Handle(new(new(second, "longenough1")), default);
        await Assert.ThrowsAsync<DomainException>(() => f.Reset().Handle(new(new(first, "otherpass12")), default));
    }
}

public class RegisterWelcomeTests
{
    [Fact]
    public async Task Register_SendsWelcome_AndSurvivesFailingSender()
    {
        foreach (var fail in new[] { false, true })
        {
            var users = new InMemoryUsers();
            var email = new FakeEmailSender { Throw = fail };
            var h = new RegisterCommandHandler(users, new InMemoryCompanies(), new StubFarms(), new NoopCategories(),
                new FakeHasher2(), new FakeJwt(), email, new FakeAppUrls());
            var res = await h.Handle(new(new RegisterRequest("Ada Okafor", "ada@farm.com", "password123", "Ada Farm")), default);
            Assert.NotNull(res.Token);
            if (!fail)
            {
                var mail = Assert.Single(email.Sent);
                Assert.Equal("ada@farm.com", mail.To);
                Assert.Contains("Welcome", mail.Subject);
            }
        }
    }

    private class NoopCategories : Ilevora.Application.Interfaces.IInventoryCategoryRepository
    {
        public Task<InventoryCategory?> GetByIdAsync(string id, CancellationToken ct = default) => Task.FromResult<InventoryCategory?>(null);
        public Task<List<InventoryCategory>> GetByFarmIdAsync(string farmId, CancellationToken ct = default) => Task.FromResult(new List<InventoryCategory>());
        public Task<InventoryCategory?> FindByNameAsync(string farmId, string name, CancellationToken ct = default) => Task.FromResult<InventoryCategory?>(null);
        public Task<InventoryCategory> AddAsync(InventoryCategory c, CancellationToken ct = default) => Task.FromResult(c);
        public Task AddRangeAsync(IEnumerable<InventoryCategory> c, CancellationToken ct = default) => Task.CompletedTask;
    }
}
