using AgroMesh.Application.DTOs;
using AgroMesh.Domain.Entities;

namespace AgroMesh.Application.Interfaces;

/// <summary>
/// Generates a downloadable PDF invoice for an Order. Application defines the contract,
/// Infrastructure implements it against the concrete QuestPDF library — same "interface in
/// Application, external-library implementation in Infrastructure" shape as
/// IClaudeSchedulingService/IWeatherService. Kept synchronous (no I/O inside PDF layout/rendering
/// itself) — the async work (resolving the Order/Supplier/InventoryItem/Farm) happens in the query
/// handler before this is called.
///
/// Takes the already-resolved <see cref="OrderDto"/> rather than the raw Order entity: Order's own
/// LineItems value objects only carry InventoryItemId (no name/unit — see OrderLineItem), so a
/// human-readable invoice needs the same resolved item names/supplier name the API DTO already
/// carries. Avoids re-resolving InventoryItem/Supplier a second time inside the PDF layer.
/// </summary>
public interface IOrderInvoiceService
{
    byte[] Generate(OrderDto order, Farm farm);
}
