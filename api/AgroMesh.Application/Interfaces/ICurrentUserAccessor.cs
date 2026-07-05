namespace AgroMesh.Application.Interfaces;

public interface ICurrentUserAccessor
{
    string? UserId { get; }
    string? CompanyId { get; }
}
