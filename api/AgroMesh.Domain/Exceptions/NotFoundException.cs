namespace AgroMesh.Domain.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string entity, string id)
        : base($"{entity} '{id}' was not found.") { }
}
