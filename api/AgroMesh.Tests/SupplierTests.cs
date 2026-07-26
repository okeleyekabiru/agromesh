using AgroMesh.Domain.Entities;
using AgroMesh.Domain.Exceptions;

namespace AgroMesh.Tests;

public class SupplierTests
{
    [Fact]
    public void Create_ValidName_SetsFields()
    {
        var supplier = Supplier.Create("farm-1", "Saro Agro Sciences", "08012345678", "sales@saroagro.com");

        Assert.Equal("farm-1", supplier.FarmId);
        Assert.Equal("Saro Agro Sciences", supplier.Name);
        Assert.Equal("08012345678", supplier.Phone);
        Assert.Equal("sales@saroagro.com", supplier.Email);
    }

    [Fact]
    public void Create_BlankName_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => Supplier.Create("farm-1", "   "));
    }

    [Fact]
    public void Create_NoContactInfo_LeavesPhoneEmailNull()
    {
        var supplier = Supplier.Create("farm-1", "Local Agro Depot");

        Assert.Null(supplier.Phone);
        Assert.Null(supplier.Email);
    }

    [Fact]
    public void Create_TrimsName()
    {
        var supplier = Supplier.Create("farm-1", "  Saro Agro  ");
        Assert.Equal("Saro Agro", supplier.Name);
    }
}
