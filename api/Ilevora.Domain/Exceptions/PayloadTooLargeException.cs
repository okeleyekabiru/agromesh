namespace Ilevora.Domain.Exceptions;

/// <summary>An upload bigger than the limit for that kind of file. Mapped to HTTP 413 in ExceptionMiddleware.</summary>
public class PayloadTooLargeException : DomainException
{
    public PayloadTooLargeException(string message) : base(message) { }
}
