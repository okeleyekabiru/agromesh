namespace Ilevora.Domain.Exceptions;

public class InvalidBoundaryFileException : DomainException
{
    public InvalidBoundaryFileException(string message) : base(message) { }
}
