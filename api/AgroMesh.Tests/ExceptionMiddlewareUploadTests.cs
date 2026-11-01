using System.Text.Json;
using AgroMesh.API.Middleware;
using AgroMesh.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace AgroMesh.Tests;

public class ExceptionMiddlewareUploadTests
{
    private static async Task<(int Status, string ContentType, JsonElement Body)> Run(Exception toThrow)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var middleware = new ExceptionMiddleware(_ => throw toThrow, NullLogger<ExceptionMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        var body = await JsonDocument.ParseAsync(context.Response.Body);
        return (context.Response.StatusCode, context.Response.ContentType!, body.RootElement.Clone());
    }

    [Fact]
    public async Task PayloadTooLarge_Is413ProblemJson()
    {
        var (status, contentType, body) = await Run(new PayloadTooLargeException("too big"));
        Assert.Equal(413, status);
        Assert.Equal("application/problem+json", contentType);
        Assert.Equal(413, body.GetProperty("status").GetInt32());
        Assert.Equal("Payload Too Large", body.GetProperty("title").GetString());
        Assert.Equal("too big", body.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task UnsupportedFileType_Is415ProblemJson()
    {
        var (status, contentType, body) = await Run(new UnsupportedFileTypeException("nope"));
        Assert.Equal(415, status);
        Assert.Equal("application/problem+json", contentType);
        Assert.Equal("Unsupported Media Type", body.GetProperty("title").GetString());
    }

    [Fact]
    public async Task KestrelBodyTooLarge_Is413NotA500()
    {
        var (status, _, body) = await Run(new BadHttpRequestException("Request body too large.", 413));
        Assert.Equal(413, status);
        Assert.Equal("Payload Too Large", body.GetProperty("title").GetString());
    }

    [Fact]
    public async Task OtherBadRequests_KeepTheirStatusAs400()
    {
        var (status, _, body) = await Run(new BadHttpRequestException("Malformed", 400));
        Assert.Equal(400, status);
        Assert.Equal("Bad Request", body.GetProperty("title").GetString());
    }

    [Fact]
    public async Task PlainDomainException_StillReturns422()
    {
        var (status, _, _) = await Run(new DomainException("rule"));
        Assert.Equal(422, status);
    }

    [Fact]
    public async Task UnhandledException_StillReturns500WithoutLeakingMessage()
    {
        var (status, _, body) = await Run(new InvalidOperationException("secret detail"));
        Assert.Equal(500, status);
        Assert.DoesNotContain("secret", body.GetProperty("detail").GetString());
    }
}
