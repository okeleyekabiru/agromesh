using Ilevora.Domain.Entities;
using Ilevora.Domain.Enums;
using Ilevora.Domain.Exceptions;
using Ilevora.Domain.Utilities;

namespace Ilevora.Tests;

public class SecureTokenTests
{
    [Fact]
    public void Generate_IsUrlSafe_AndUnique()
    {
        var a = SecureToken.Generate();
        var b = SecureToken.Generate();
        Assert.NotEqual(a, b);
        Assert.True(a.Length >= 43);
        Assert.DoesNotContain('+', a);
        Assert.DoesNotContain('/', a);
        Assert.DoesNotContain('=', a);
    }

    [Fact]
    public void Hash_IsSha256Hex_AndNotTheToken()
    {
        var t = SecureToken.Generate();
        var h = SecureToken.Hash(t);
        Assert.Equal(64, h.Length);
        Assert.NotEqual(t, h);
        Assert.Equal(h, SecureToken.Hash(t));
    }

    [Fact]
    public void Matches_AcceptsRightToken_RejectsWrongOrEmpty()
    {
        var t = SecureToken.Generate();
        var h = SecureToken.Hash(t);
        Assert.True(SecureToken.Matches(t, h));
        Assert.False(SecureToken.Matches(SecureToken.Generate(), h));
        Assert.False(SecureToken.Matches("", h));
        Assert.False(SecureToken.Matches(t, "short"));
    }
}

public class InvitationTests
{
    private static readonly DateTime Now = new(2026, 11, 10, 12, 0, 0, DateTimeKind.Utc);
    private static Invitation New() =>
        Invitation.Create("co-1", "Ada@Farm.com", UserRole.Supervisor, "u1", "hash1", Now, TimeSpan.FromDays(7));

    [Fact]
    public void Create_NormalisesEmail_AndIsPending()
    {
        var i = New();
        Assert.Equal("ada@farm.com", i.Email);
        Assert.Equal(InvitationStatus.Pending, i.StatusAt(Now));
        Assert.Equal(Now.AddDays(7), i.ExpiresAt);
    }

    [Fact]
    public void Status_BecomesExpired_AfterTtl()
    {
        Assert.Equal(InvitationStatus.Expired, New().StatusAt(Now.AddDays(7).AddSeconds(1)));
    }

    [Fact]
    public void Accept_MarksAccepted_AndCannotBeReused()
    {
        var i = New();
        i.Accept(Now.AddHours(1));
        Assert.Equal(InvitationStatus.Accepted, i.StatusAt(Now.AddHours(2)));
        Assert.Throws<DomainException>(() => i.Accept(Now.AddHours(2)));
    }

    [Fact]
    public void Accept_WhenExpired_Throws()
    {
        Assert.Throws<DomainException>(() => New().Accept(Now.AddDays(8)));
    }

    [Fact]
    public void Revoke_BlocksAccept()
    {
        var i = New();
        i.Revoke(Now);
        Assert.Equal(InvitationStatus.Revoked, i.StatusAt(Now));
        Assert.Throws<DomainException>(() => i.Accept(Now));
    }

    [Fact]
    public void Reissue_RotatesHash_AndExtendsExpiry_EvenWhenExpired()
    {
        var i = New();
        var later = Now.AddDays(10);
        i.Reissue("hash2", later, TimeSpan.FromDays(7));
        Assert.Equal("hash2", i.TokenHash);
        Assert.Equal(InvitationStatus.Pending, i.StatusAt(later));
    }

    [Fact]
    public void Reissue_Throws_WhenAcceptedOrRevoked()
    {
        var accepted = New(); accepted.Accept(Now);
        Assert.Throws<DomainException>(() => accepted.Reissue("h", Now, TimeSpan.FromDays(1)));
        var revoked = New(); revoked.Revoke(Now);
        Assert.Throws<DomainException>(() => revoked.Reissue("h", Now, TimeSpan.FromDays(1)));
    }

    [Fact]
    public void Revoke_Throws_WhenAlreadyAccepted()
    {
        var i = New(); i.Accept(Now);
        Assert.Throws<DomainException>(() => i.Revoke(Now));
    }
}

public class PasswordResetTokenTests
{
    private static readonly DateTime Now = new(2026, 11, 10, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Usable_UntilExpiryOrUse()
    {
        var t = PasswordResetToken.Create("u1", "h", Now, TimeSpan.FromHours(1));
        Assert.True(t.IsUsable(Now.AddMinutes(59)));
        Assert.False(t.IsUsable(Now.AddMinutes(61)));
        t.MarkUsed(Now);
        Assert.False(t.IsUsable(Now));
    }
}
