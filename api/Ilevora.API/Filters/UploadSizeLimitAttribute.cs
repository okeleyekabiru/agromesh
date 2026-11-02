using Ilevora.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Ilevora.API.Filters;

/// <summary>
/// Refuses an upload whose declared Content-Length is over the limit with 413 and a problem body, before the form is read.
/// Without it a too-large multipart body is stopped by the server's body-size cap while model binding reads the form, and
/// the client gets a 400 validation error instead. The cap (RequestSizeLimit) stays as the backstop for requests that do not
/// declare a length.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class UploadSizeLimitAttribute : Attribute, IResourceFilter
{
    public long MaxBytes { get; }

    public UploadSizeLimitAttribute(long maxBytes) => MaxBytes = maxBytes;

    public void OnResourceExecuting(ResourceExecutingContext context)
    {
        var declared = context.HttpContext.Request.ContentLength;
        if (declared > MaxBytes)
            throw new PayloadTooLargeException("The upload is too large.");
    }

    public void OnResourceExecuted(ResourceExecutedContext context) { }
}
