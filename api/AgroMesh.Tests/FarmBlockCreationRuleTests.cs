using AgroMesh.Domain.Entities;
using AgroMesh.Domain.Enums;

namespace AgroMesh.Tests;

public class FarmBlockCreationRuleTests
{
    [Fact]
    public void Block_HasNoStateField()
    {
        // A Farm's Blocks must never carry independent state — state lives on Farm only.
        var stateProperty = typeof(Block).GetProperty("StateId") ?? typeof(Block).GetProperty("State");
        Assert.Null(stateProperty);
    }

    [Fact]
    public void Farm_Create_RequiresCompanyIdAndOwnerId()
    {
        var farm = Farm.Create("Test Farm", companyId: "company-1", ownerId: "user-1");

        Assert.Equal("company-1", farm.CompanyId);
        Assert.Equal("user-1", farm.OwnerId);
        Assert.Equal(FarmSetupStatus.Draft, farm.SetupStatus);
        Assert.Equal("NG", farm.CountryCode);
        Assert.Null(farm.StateId);
    }

    [Fact]
    public void Farm_MarkConfigured_TransitionsSetupStatus()
    {
        var farm = Farm.Create("Test Farm", "company-1", "user-1");

        farm.MarkConfigured();

        Assert.Equal(FarmSetupStatus.Configured, farm.SetupStatus);
    }

    [Fact]
    public void Farm_SetLocation_AssignsStateOnceToWholeFarm()
    {
        var farm = Farm.Create("Test Farm", "company-1", "user-1");

        farm.SetLocation("state-oyo", "Ibadan North");

        Assert.Equal("state-oyo", farm.StateId);
        Assert.Equal("Ibadan North", farm.LgaFreetext);
    }
}
