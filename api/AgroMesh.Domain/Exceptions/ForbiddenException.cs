namespace AgroMesh.Domain.Exceptions;

/// <summary>
/// A well-formed request rejected purely because of who's asking (e.g. a non-admin trying to
/// approve/reject a stock adjustment) — distinct from <see cref="DomainException"/>, which covers
/// business-rule violations that would be meaningful regardless of caller. Mapped to HTTP 403 in
/// ExceptionMiddleware, so a client can tell "you're not allowed" apart from "this data is invalid"
/// without string-matching the error body.
/// </summary>
public class ForbiddenException : Exception
{
    public ForbiddenException(string message) : base(message) { }
}
