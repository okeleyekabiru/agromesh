using Ilevora.Application.Commands.Invitations;
using Ilevora.Application.DTOs;
using Ilevora.Application.Interfaces;
using Ilevora.Domain.Entities;
using Ilevora.Domain.Enums;
using Ilevora.Domain.Exceptions;
using Ilevora.Domain.Utilities;

namespace Ilevora.Tests;

internal class InvitationHandlerFixture
{
    public InMemoryUsers Users = new();
    public InMemoryInvitations Invites = new();
    public InMemoryCompanies Companies = new();
    public StubFarms Farms = new();
    public FakeEmailSender Email = new();
    public TestClock Clock = new(new DateTimeOffset(2026, 11, 10, 12, 0, 0, TimeSpan.Zero));
    public Company Company;
    public User Inviter;

    public InvitationHandlerFixture()
    {
        Company = Company.Create("Green Acres", "owner", true);
        Companies.Items.Add(Company);
        Inviter = User.CreateStaff("Owner Person", "owner@farm.com", "x", Company.Id, UserRole.Admin);
        Users.Users.Add(Inviter);
    }

    public CreateInvitationCommandHandler Create(UserRole role) =>
        new(Invites, Users, Companies, new FakeCaller(role, Company.Id, Inviter.Id), Email, new FakeAppUrls(), Clock);

    public static string TokenFromLink(EmailMessage m) => m.Text.Split("token=")[1].Split(['\n', ' ', '\r'])[0];
}

public class CreateInvitationTests
{
    [Fact]
    public async Task Creates_StoresOnlyHash_AndEmailsLinkWithRawToken()
    {
        var f = new InvitationHandlerFixture();
        var dto = await f.Create(UserRole.Admin).Handle(new(new("Ada@Farm.com", "Supervisor")), default);

        var inv = Assert.Single(f.Invites.Items);
        var mail = Assert.Single(f.Email.Sent);
        var raw = InvitationHandlerFixture.TokenFromLink(mail);
        Assert.Equal("ada@farm.com", mail.To);
        Assert.Contains("Green Acres", mail.Html);
        Assert.NotEqual(raw, inv.TokenHash);
        Assert.Equal(SecureToken.Hash(raw), inv.TokenHash);
        Assert.Equal("Pending", dto.Status);
        Assert.Equal(f.Company.Id, inv.CompanyId);
    }

    [Theory]
    [InlineData(UserRole.Admin, "FarmManager", true)]
    [InlineData(UserRole.Admin, "Supervisor", true)]
    [InlineData(UserRole.FarmManager, "Supervisor", true)]
    [InlineData(UserRole.FarmManager, "FarmManager", false)]
    [InlineData(UserRole.Supervisor, "Supervisor", false)]
    [InlineData(UserRole.Member, "Supervisor", false)]
    public async Task RoleMatrix(UserRole caller, string target, bool allowed)
    {
        var f = new InvitationHandlerFixture();
        var act = () => f.Create(caller).Handle(new(new("a@b.com", target)), default);
        if (allowed) { await act(); Assert.Single(f.Email.Sent); }
        else { await Assert.ThrowsAsync<ForbiddenException>(act); Assert.Empty(f.Invites.Items); Assert.Empty(f.Email.Sent); }
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("nonsense")]
    public async Task UnassignableRole_Rejected(string role)
    {
        var f = new InvitationHandlerFixture();
        await Assert.ThrowsAsync<DomainException>(() => f.Create(UserRole.Admin).Handle(new(new("a@b.com", role)), default));
        Assert.Empty(f.Email.Sent);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public async Task InvalidEmail_Rejected(string email)
    {
        var f = new InvitationHandlerFixture();
        await Assert.ThrowsAsync<DomainException>(() => f.Create(UserRole.Admin).Handle(new(new(email, "Supervisor")), default));
        Assert.Empty(f.Email.Sent);
    }

    [Fact]
    public async Task AlreadyRegisteredEmail_Rejected()
    {
        var f = new InvitationHandlerFixture();
        await Assert.ThrowsAsync<DomainException>(() => f.Create(UserRole.Admin).Handle(new(new("OWNER@farm.com", "Supervisor")), default));
        Assert.Empty(f.Email.Sent);
    }

    [Fact]
    public async Task SecondPendingInviteForSameEmail_Rejected_ButAllowedAfterRevoke()
    {
        var f = new InvitationHandlerFixture();
        var h = f.Create(UserRole.Admin);
        var first = await h.Handle(new(new("a@b.com", "Supervisor")), default);
        await Assert.ThrowsAsync<DomainException>(() => h.Handle(new(new("a@b.com", "Supervisor")), default));

        f.Invites.Items.Single(i => i.Id == first.Id).Revoke(f.Clock.GetUtcNow().UtcDateTime);
        await h.Handle(new(new("a@b.com", "Supervisor")), default);
        Assert.Equal(2, f.Invites.Items.Count);
    }

    [Fact]
    public async Task FailingEmailSender_DoesNotFailTheRequest()
    {
        var f = new InvitationHandlerFixture { Email = { Throw = true } };
        var dto = await f.Create(UserRole.Admin).Handle(new(new("a@b.com", "Supervisor")), default);
        Assert.Equal("Pending", dto.Status);
        Assert.Single(f.Invites.Items);
    }
}

public class InvitationManageTests
{
    private static async Task<(InvitationHandlerFixture f, InvitationDto dto, string raw)> Seed(string role = "Supervisor")
    {
        var f = new InvitationHandlerFixture();
        var dto = await f.Create(UserRole.Admin).Handle(new(new("a@b.com", role)), default);
        return (f, dto, InvitationHandlerFixture.TokenFromLink(f.Email.Sent[0]));
    }

    private static ListInvitationsQueryHandler List(InvitationHandlerFixture f, UserRole role, string? company = null) =>
        new(f.Invites, new FakeCaller(role, company ?? f.Company.Id), f.Clock);

    [Fact]
    public async Task List_ShowsStatuses_ScopedToCompany()
    {
        var (f, dto, _) = await Seed();
        f.Invites.Items.Add(Invitation.Create("other-co", "x@y.com", UserRole.Supervisor, "u", "h", DateTime.UtcNow, TimeSpan.FromDays(7)));
        f.Clock.Advance(TimeSpan.FromDays(8));

        var list = await List(f, UserRole.Admin).Handle(new(), default);
        var row = Assert.Single(list);
        Assert.Equal(dto.Id, row.Id);
        Assert.Equal("Expired", row.Status);
    }

    [Theory]
    [InlineData(UserRole.Supervisor)]
    [InlineData(UserRole.Member)]
    public async Task List_Forbidden_ForNonManagers(UserRole role)
    {
        var (f, _, _) = await Seed();
        await Assert.ThrowsAsync<ForbiddenException>(() => List(f, role).Handle(new(), default));
    }

    [Fact]
    public async Task Resend_RotatesToken_OldLinkDies_NewEmailSent()
    {
        var (f, dto, oldRaw) = await Seed();
        var h = new ResendInvitationCommandHandler(f.Invites, f.Users, f.Companies, new FakeCaller(UserRole.Admin, f.Company.Id, f.Inviter.Id), f.Email, new FakeAppUrls(), f.Clock);
        await h.Handle(new(dto.Id), default);

        Assert.Equal(2, f.Email.Sent.Count);
        var newRaw = InvitationHandlerFixture.TokenFromLink(f.Email.Sent[1]);
        Assert.NotEqual(oldRaw, newRaw);
        var inv = f.Invites.Items.Single();
        Assert.False(SecureToken.Matches(oldRaw, inv.TokenHash));
        Assert.True(SecureToken.Matches(newRaw, inv.TokenHash));
    }

    [Fact]
    public async Task Resend_OtherCompany_NotFound()
    {
        var (f, dto, _) = await Seed();
        var h = new ResendInvitationCommandHandler(f.Invites, f.Users, f.Companies, new FakeCaller(UserRole.Admin, "other", f.Inviter.Id), f.Email, new FakeAppUrls(), f.Clock);
        await Assert.ThrowsAsync<DomainException>(() => h.Handle(new(dto.Id), default));
        Assert.Single(f.Email.Sent);
    }

    [Fact]
    public async Task FarmManager_CannotManageFarmManagerInvites()
    {
        var (f, dto, _) = await Seed("FarmManager");
        var h = new RevokeInvitationCommandHandler(f.Invites, new FakeCaller(UserRole.FarmManager, f.Company.Id), f.Clock);
        await Assert.ThrowsAsync<ForbiddenException>(() => h.Handle(new(dto.Id), default));
    }

    [Fact]
    public async Task Revoke_KillsTheLink()
    {
        var (f, dto, raw) = await Seed();
        await new RevokeInvitationCommandHandler(f.Invites, new FakeCaller(UserRole.Admin, f.Company.Id), f.Clock).Handle(new(dto.Id), default);

        var accept = new AcceptInvitationCommandHandler(f.Invites, f.Users, f.Companies, f.Farms, new FakeHasher2(), new FakeJwt(), f.Email, new FakeAppUrls(), f.Clock);
        await Assert.ThrowsAsync<DomainException>(() => accept.Handle(new(new(raw, "Ada", "password123")), default));
        Assert.DoesNotContain(f.Users.Users, u => u.Email == "a@b.com");
    }
}

public class AcceptInvitationTests
{
    private const string Generic = "This invitation link is invalid or has expired.";

    private static async Task<(InvitationHandlerFixture f, string raw, AcceptInvitationCommandHandler accept)> Seed()
    {
        var f = new InvitationHandlerFixture();
        f.Farms.Farms.Add(Farm.Create("Green Acres Farm", f.Company.Id, f.Inviter.Id));
        await f.Create(UserRole.Admin).Handle(new(new("a@b.com", "FarmManager")), default);
        var raw = InvitationHandlerFixture.TokenFromLink(f.Email.Sent[0]);
        f.Email.Sent.Clear();
        return (f, raw, new AcceptInvitationCommandHandler(f.Invites, f.Users, f.Companies, f.Farms, new FakeHasher2(), new FakeJwt(), f.Email, new FakeAppUrls(), f.Clock));
    }

    [Fact]
    public async Task Accept_CreatesUser_InCompany_WithRole_ReturnsAuth_SendsWelcome()
    {
        var (f, raw, accept) = await Seed();
        var res = await accept.Handle(new(new(raw, "Ada Okafor", "password123")), default);

        var user = f.Users.Users.Single(u => u.Email == "a@b.com");
        Assert.Equal(f.Company.Id, user.CompanyId);
        Assert.Equal(UserRole.FarmManager, user.Role);
        Assert.Equal("hash:password123", user.PasswordHash);
        Assert.Equal("jwt-for-" + user.Id, res.Token);
        Assert.Equal("FarmManager", res.User.Role);
        Assert.Single(res.Farms);
        Assert.Equal(InvitationStatus.Accepted, f.Invites.Items.Single().StatusAt(f.Clock.GetUtcNow().UtcDateTime));
        var mail = Assert.Single(f.Email.Sent, m => m.To == "a@b.com");
        Assert.Contains("Welcome", mail.Subject);
    }

    [Fact]
    public async Task Accept_TellsTheInviter_UnlessTheyOptedOut()
    {
        var (f, raw, accept) = await Seed();
        await accept.Handle(new(new(raw, "Ada Okafor", "password123")), default);
        var note = Assert.Single(f.Email.Sent, m => m.To == "owner@farm.com");
        Assert.Contains("Ada Okafor", note.Subject);

        var (f2, raw2, accept2) = await Seed();
        f2.Inviter.SetEmailNotifications(false);
        await accept2.Handle(new(new(raw2, "Ada Okafor", "password123")), default);
        Assert.DoesNotContain(f2.Email.Sent, m => m.To == "owner@farm.com");
        Assert.Contains(f2.Email.Sent, m => m.To == "a@b.com");   // the welcome is not optional yet
    }

    [Fact]
    public async Task Accept_Twice_Fails_AndCreatesNoSecondUser()
    {
        var (f, raw, accept) = await Seed();
        await accept.Handle(new(new(raw, "Ada", "password123")), default);
        var ex = await Assert.ThrowsAsync<DomainException>(() => accept.Handle(new(new(raw, "Ada", "password123")), default));
        Assert.Equal(Generic, ex.Message);
        Assert.Single(f.Users.Users, u => u.Email == "a@b.com");
    }

    [Fact]
    public async Task Accept_Expired_Fails()
    {
        var (f, raw, accept) = await Seed();
        f.Clock.Advance(TimeSpan.FromDays(8));
        var ex = await Assert.ThrowsAsync<DomainException>(() => accept.Handle(new(new(raw, "Ada", "password123")), default));
        Assert.Equal(Generic, ex.Message);
    }

    [Fact]
    public async Task Accept_UnknownToken_SameMessage()
    {
        var (_, _, accept) = await Seed();
        var ex = await Assert.ThrowsAsync<DomainException>(() => accept.Handle(new(new("nope", "Ada", "password123")), default));
        Assert.Equal(Generic, ex.Message);
    }

    [Theory]
    [InlineData("short", "Ada")]
    [InlineData("password123", " ")]
    public async Task Accept_InvalidInput_Rejected_AndInviteStaysUsable(string password, string name)
    {
        var (f, raw, accept) = await Seed();
        await Assert.ThrowsAsync<DomainException>(() => accept.Handle(new(new(raw, name, password)), default));
        Assert.Equal(InvitationStatus.Pending, f.Invites.Items.Single().StatusAt(f.Clock.GetUtcNow().UtcDateTime));
    }

    [Fact]
    public async Task Accept_FailingEmailSender_StillSucceeds()
    {
        var (f, raw, accept) = await Seed();
        f.Email.Throw = true;
        var res = await accept.Handle(new(new(raw, "Ada", "password123")), default);
        Assert.NotNull(res.Token);
    }

    [Fact]
    public async Task Preview_ReturnsFarmAndRole_ForValidToken_AndGenericErrorOtherwise()
    {
        var (f, raw, _) = await Seed();
        var h = new GetInvitationPreviewQueryHandler(f.Invites, f.Companies, f.Clock);
        var p = await h.Handle(new(raw), default);
        Assert.Equal("a@b.com", p.Email);
        Assert.Equal("Green Acres", p.FarmName);
        Assert.Equal("FarmManager", p.Role);
        var ex = await Assert.ThrowsAsync<DomainException>(() => h.Handle(new("bad"), default));
        Assert.Equal(Generic, ex.Message);
    }
}
