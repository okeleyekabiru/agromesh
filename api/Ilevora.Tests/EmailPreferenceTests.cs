using Ilevora.Application.Commands.Staff;
using Ilevora.Domain.Entities;
using Ilevora.Domain.Enums;

namespace Ilevora.Tests;

public class EmailPreferenceTests
{
    private static (InMemoryUsers users, User user) Seed()
    {
        var users = new InMemoryUsers();
        var user = User.CreateStaff("Ada", "ada@farm.com", "h", "co-1", UserRole.FarmManager);
        users.Users.Add(user);
        return (users, user);
    }

    [Fact]
    public async Task Get_ReturnsCurrentSetting_ForTheCaller()
    {
        var (users, user) = Seed();
        var h = new GetEmailPreferenceQueryHandler(users, new FakeCaller(UserRole.FarmManager, "co-1", user.Id));
        Assert.True((await h.Handle(new(), default)).Enabled);
        user.SetEmailNotifications(false);
        Assert.False((await h.Handle(new(), default)).Enabled);
    }

    [Fact]
    public async Task Set_ChangesOnlyTheCallersOwnSetting()
    {
        var (users, user) = Seed();
        var other = User.CreateStaff("Bo", "bo@farm.com", "h", "co-1", UserRole.Supervisor);
        users.Users.Add(other);

        var h = new SetEmailPreferenceCommandHandler(users, new FakeCaller(UserRole.FarmManager, "co-1", user.Id));
        var dto = await h.Handle(new(false), default);

        Assert.False(dto.Enabled);
        Assert.False(user.EmailNotificationsEnabled);
        Assert.True(other.EmailNotificationsEnabled);
    }
}
