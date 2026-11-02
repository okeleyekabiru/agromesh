namespace Ilevora.Domain.Exceptions;

/// <summary>An upload whose real content (or declared type) is not one of the allowed file types. Mapped to HTTP 415 in ExceptionMiddleware.</summary>
public class UnsupportedFileTypeException : DomainException
{
    public UnsupportedFileTypeException(string message) : base(message) { }
}
