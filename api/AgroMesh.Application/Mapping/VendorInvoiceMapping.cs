using AgroMesh.Application.DTOs;
using AgroMesh.Domain.Entities;
using AgroMesh.Domain.Exceptions;

namespace AgroMesh.Application.Mapping;

public static class VendorInvoiceMapping
{
    public static VendorInvoice EnsureFarmScoped(VendorInvoice? invoice, string farmId, string id)
    {
        if (invoice is null || invoice.FarmId != farmId) throw new NotFoundException("Vendor invoice", id);
        return invoice;
    }

    /// <summary>An order of this farm, or 404. A different farm's order is reported as missing.</summary>
    public static Order EnsureOrderOfFarm(Order? order, string farmId, string orderId)
    {
        if (order is null || order.FarmId != farmId) throw new NotFoundException("Order", orderId);
        return order;
    }

    public static VendorInvoiceDto ToDto(VendorInvoice v, Order? order, DateOnly today) => new(
        v.Id, v.FarmId, v.OrderId,
        order?.Reference ?? string.Empty, order?.Total ?? 0m, order?.IsPaid ?? false,
        v.SupplierId, v.VendorName, v.InvoiceNumber, v.Amount, v.InvoiceDate, v.DueDate,
        v.Status.ToString(), v.PaidAt, v.IsOverdue(today), v.Notes,
        v.FileName, v.ContentType, v.FileSize, v.UploadedByUserId, v.CreatedAt);

    public static DateOnly Today() => DateOnly.FromDateTime(DateTime.UtcNow);
}
