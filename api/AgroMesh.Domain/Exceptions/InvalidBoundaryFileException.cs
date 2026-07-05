namespace AgroMesh.Domain.Exceptions;

public class InvalidBoundaryFileException : DomainException
{
    public InvalidBoundaryFileException(string message) : base(message) { }
}
