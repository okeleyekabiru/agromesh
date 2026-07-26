namespace AgroMesh.Application.DTOs;

public record SupplierDto(string Id, string FarmId, string Name, string? Phone, string? Email);

public record CreateSupplierRequest(string Name, string? Phone, string? Email);
