using AgroMesh.Domain.Entities;
using AgroMesh.Domain.Exceptions;

namespace AgroMesh.Tests;

public class FarmBusinessProfileTests
{
    private static Farm NewFarm() => Farm.Create("Nature's Gold Farm", "company-1", "owner-1", id: "farm-1");

    [Fact]
    public void NewFarm_HasNoProfileAndNoLogo()
    {
        var farm = NewFarm();
        Assert.Null(farm.Address);
        Assert.Null(farm.BankAccountNumber);
        Assert.Null(farm.LogoFileId);
        Assert.False(farm.HasLogo);
    }

    [Fact]
    public void SetBusinessProfile_TrimsAndStoresEveryField()
    {
        var farm = NewFarm();
        farm.SetBusinessProfile("  12 Farm Road, Ogun ", " +234 801 234 5678 ", " hello@ngf.ng ", " www.ngf.ng ", " Nature's Gold Farm ", " 0123456789 ", " GTBank ");

        Assert.Equal("12 Farm Road, Ogun", farm.Address);
        Assert.Equal("+234 801 234 5678", farm.Phone);
        Assert.Equal("hello@ngf.ng", farm.Email);
        Assert.Equal("www.ngf.ng", farm.Website);
        Assert.Equal("Nature's Gold Farm", farm.BankAccountName);
        Assert.Equal("0123456789", farm.BankAccountNumber);
        Assert.Equal("GTBank", farm.BankName);
    }

    [Fact]
    public void SetBusinessProfile_BlankValuesBecomeNull()
    {
        var farm = NewFarm();
        farm.SetBusinessProfile("x", "x", "a@b.co", "x", "x", "x", "x");
        farm.SetBusinessProfile("  ", "", null, " ", null, "", "   ");

        Assert.Null(farm.Address);
        Assert.Null(farm.Phone);
        Assert.Null(farm.Email);
        Assert.Null(farm.Website);
        Assert.Null(farm.BankAccountName);
        Assert.Null(farm.BankAccountNumber);
        Assert.Null(farm.BankName);
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("a@b")]
    [InlineData("@b.com")]
    [InlineData("a b@c.com")]
    public void SetBusinessProfile_RejectsMalformedEmail(string email)
    {
        var ex = Assert.Throws<DomainException>(() => NewFarm().SetBusinessProfile(null, null, email, null, null, null, null));
        Assert.Contains("email", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(301, "address")]
    [InlineData(61, "phone")]
    [InlineData(201, "website")]
    [InlineData(151, "account name")]
    [InlineData(41, "account number")]
    [InlineData(101, "bank name")]
    public void SetBusinessProfile_RejectsOverlongFields(int length, string which)
    {
        var text = new string('a', length);
        var farm = NewFarm();
        Action act = which switch
        {
            "address" => () => farm.SetBusinessProfile(text, null, null, null, null, null, null),
            "phone" => () => farm.SetBusinessProfile(null, text, null, null, null, null, null),
            "website" => () => farm.SetBusinessProfile(null, null, null, text, null, null, null),
            "account name" => () => farm.SetBusinessProfile(null, null, null, null, text, null, null),
            "account number" => () => farm.SetBusinessProfile(null, null, null, null, null, text, null),
            _ => () => farm.SetBusinessProfile(null, null, null, null, null, null, text),
        };
        var ex = Assert.Throws<DomainException>(act);
        Assert.Contains(which, ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SetLogo_RecordsFileAndTimestamp_ClearLogoRemovesThem()
    {
        var farm = NewFarm();
        farm.SetLogo("file-1");
        Assert.True(farm.HasLogo);
        Assert.Equal("file-1", farm.LogoFileId);
        Assert.NotNull(farm.LogoUpdatedAt);

        farm.ClearLogo();
        Assert.False(farm.HasLogo);
        Assert.Null(farm.LogoFileId);
        Assert.Null(farm.LogoUpdatedAt);
    }

    [Fact]
    public void SetLogo_RejectsBlankFileId()
    {
        Assert.Throws<DomainException>(() => NewFarm().SetLogo(" "));
    }

    [Fact]
    public void StoredFile_Create_CapturesBytesAndSize()
    {
        var file = StoredFile.Create("farm-1", "logo.png", "image/png", [1, 2, 3, 4]);
        Assert.Equal("farm-1", file.FarmId);
        Assert.Equal("logo.png", file.FileName);
        Assert.Equal("image/png", file.ContentType);
        Assert.Equal(4, file.Size);
        Assert.Equal(new byte[] { 1, 2, 3, 4 }, file.Content);
        Assert.False(string.IsNullOrEmpty(file.Id));
    }

    [Fact]
    public void StoredFile_Create_RejectsEmptyContent()
    {
        Assert.Throws<DomainException>(() => StoredFile.Create("farm-1", "a.png", "image/png", []));
    }
}
