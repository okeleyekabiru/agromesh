using AgroMesh.Application.DTOs;
using AgroMesh.Domain.Entities;
using AgroMesh.Infrastructure.Services;
using QuestPDF.Infrastructure;

namespace AgroMesh.Tests;

/// <summary>
/// The first test in this repo that needs AgroMesh.Infrastructure directly (see
/// AgroMesh.Tests.csproj's new ProjectReference) — a real QuestPDF call, not a fake, since this is
/// genuinely new, non-DB-dependent infrastructure worth proving actually works (the library throws
/// at runtime if QuestPDF.Settings.License is never set — Program.cs sets it for the running app,
/// but Program.cs never runs under xUnit, so this class sets it itself in a static constructor).
/// </summary>
public class OrderInvoiceServiceTests
{
    static OrderInvoiceServiceTests()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    private static OrderDto BuildOrderDto() => new(
        Id: "order-1",
        FarmId: "farm-1",
        SupplierId: "sup-1",
        SupplierName: "Saro Agro Sciences",
        Status: "Placed",
        LineItems:
        [
            new OrderLineItemDto("item-1", "Glyphosate 360", "L", 10m, 500m, 5000m),
            new OrderLineItemDto("item-2", "NPK 15-15-15", "kg", 5m, 1200m, 6000m)
        ],
        Total: 11000m,
        CreatedAt: DateTime.UtcNow,
        UpdatedAt: DateTime.UtcNow);

    [Fact]
    public void Generate_RealOrder_ReturnsNonEmptyPdfBytes()
    {
        var farm = Farm.Create("Kabiru Farms Ltd.", "company-1", "owner-1", id: "farm-1");
        var order = BuildOrderDto();
        var service = new OrderInvoiceService();

        var bytes = service.Generate(order, farm);

        Assert.NotEmpty(bytes);
        Assert.True(bytes.Length > 100, "Expected a real, non-trivial PDF document.");
        // %PDF magic number — the first 4 bytes of every valid PDF file.
        Assert.Equal(0x25, bytes[0]); // '%'
        Assert.Equal(0x50, bytes[1]); // 'P'
        Assert.Equal(0x44, bytes[2]); // 'D'
        Assert.Equal(0x46, bytes[3]); // 'F'
    }
}
