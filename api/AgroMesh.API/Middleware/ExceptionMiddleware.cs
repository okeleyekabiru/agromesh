using AgroMesh.Domain.Exceptions;
using System.Text.Json;
#pragma warning disable CS8604

namespace AgroMesh.API.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning(ex, "Not found: {Message}", ex.Message);
            context.Response.StatusCode = 404;
            context.Response.ContentType = "application/problem+json";
            var notFound = new
            {
                type = "https://tools.ietf.org/html/rfc9110#section-15.5.5",
                title = "Not Found",
                status = 404,
                detail = ex.Message,
                traceId = context.TraceIdentifier
            };
            await context.Response.WriteAsync(JsonSerializer.Serialize(notFound));
            return;
        }
        catch (ForbiddenException ex)
        {
            _logger.LogWarning(ex, "Forbidden: {Message}", ex.Message);
            context.Response.StatusCode = 403;
            context.Response.ContentType = "application/problem+json";
            var forbidden = new
            {
                type = "https://tools.ietf.org/html/rfc9110#section-15.5.4",
                title = "Forbidden",
                status = 403,
                detail = ex.Message,
                traceId = context.TraceIdentifier
            };
            await context.Response.WriteAsync(JsonSerializer.Serialize(forbidden));
            return;
        }
        catch (PayloadTooLargeException ex)
        {
            _logger.LogWarning(ex, "Payload too large: {Message}", ex.Message);
            await WriteProblem(context, 413, "Payload Too Large", "https://tools.ietf.org/html/rfc9110#section-15.5.14", ex.Message);
        }
        catch (UnsupportedFileTypeException ex)
        {
            _logger.LogWarning(ex, "Unsupported file type: {Message}", ex.Message);
            await WriteProblem(context, 415, "Unsupported Media Type", "https://tools.ietf.org/html/rfc9110#section-15.5.16", ex.Message);
        }
        catch (BadHttpRequestException ex) when (ex.StatusCode is 400 or 413)
        {
            // Kestrel rejects an oversized upload (or a malformed body) before any handler runs.
            _logger.LogWarning(ex, "Bad request: {Message}", ex.Message);
            if (ex.StatusCode == 413)
                await WriteProblem(context, 413, "Payload Too Large", "https://tools.ietf.org/html/rfc9110#section-15.5.14", "The request is too large.");
            else
                await WriteProblem(context, 400, "Bad Request", "https://tools.ietf.org/html/rfc9110#section-15.5.1", "The request could not be read.");
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation: {Message}", ex.Message);
            context.Response.StatusCode = 422;
            context.Response.ContentType = "application/problem+json";
            var problem = new
            {
                type = "https://tools.ietf.org/html/rfc9110#section-15.5.21",
                title = "Unprocessable Request",
                status = 422,
                detail = ex.Message,
                traceId = context.TraceIdentifier
            };
            await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/problem+json";
            var problem = new
            {
                type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                title = "Internal Server Error",
                status = 500,
                detail = "Something went wrong on our end. Please try again.",
                traceId = context.TraceIdentifier
            };
            await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
        }
    }

    private static async Task WriteProblem(HttpContext context, int status, string title, string type, string detail)
    {
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(new { type, title, status, detail, traceId = context.TraceIdentifier }));
    }
}
