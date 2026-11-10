using System.Reflection;
using Ilevora.API.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;

namespace Ilevora.Tests;

public class InvitationControllerTests
{
    private static MethodInfo M(Type t, string name) => t.GetMethod(name) ?? throw new InvalidOperationException(name);
    private static bool Anonymous(MethodInfo m) => m.GetCustomAttribute<AllowAnonymousAttribute>() is not null;
    private static string? Limiter(MethodInfo m) => m.GetCustomAttribute<EnableRateLimitingAttribute>()?.PolicyName;

    [Fact]
    public void Management_RequiresAuthorize_AtClassLevel()
    {
        Assert.NotEmpty(typeof(InvitationsController).GetCustomAttributes(typeof(AuthorizeAttribute), false));
        foreach (var name in new[] { "List", "Create", "Resend", "Revoke" })
            Assert.False(Anonymous(M(typeof(InvitationsController), name)), name);
    }

    [Theory]
    [InlineData("Create")]
    [InlineData("Resend")]
    public void CreateAndResend_AreRateLimited(string action) =>
        Assert.Equal("invite", Limiter(M(typeof(InvitationsController), action)));

    [Theory]
    [InlineData("Preview")]
    [InlineData("Accept")]
    public void PreviewAndAccept_AreAnonymous_AndRateLimited(string action)
    {
        var m = M(typeof(InvitationsController), action);
        Assert.True(Anonymous(m));
        Assert.Equal("token-endpoints", Limiter(m));
    }

    [Fact]
    public void ForgotAndReset_AreRateLimited_AuthControllerHasNoClassAuthorize()
    {
        Assert.Equal("forgot-password", Limiter(M(typeof(AuthController), "ForgotPassword")));
        Assert.Equal("token-endpoints", Limiter(M(typeof(AuthController), "ResetPassword")));
        Assert.Empty(typeof(AuthController).GetCustomAttributes(typeof(AuthorizeAttribute), false));
    }
}
