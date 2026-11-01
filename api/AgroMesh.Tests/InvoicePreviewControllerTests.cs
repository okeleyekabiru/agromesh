using System.Reflection;
using AgroMesh.API.Controllers;
using AgroMesh.Application.DTOs;
using AgroMesh.Application.Queries.Invoice;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace AgroMesh.Tests;

public class InvoicePreviewControllerTests
{
    private class Mediator : IMediator
    {
        public object? Last;
        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            Last = request;
            return Task.FromResult((TResponse)(object)new InvoicePdfFile("%PDF-sample"u8.ToArray(), "ngf-preview.pdf"));
        }
        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest => throw new NotImplementedException();
        public Task<object?> Send(object request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task Publish(object notification, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification => throw new NotImplementedException();
    }

    [Fact]
    public void PreviewRoute_IsAGet_UnderTheFarmsTemplates_AndTheControllerIsAuthorized()
    {
        var method = typeof(InvoicesController).GetMethod(nameof(InvoicesController.Preview))!;
        var http = method.GetCustomAttributes<HttpMethodAttribute>().Single();
        Assert.Equal("GET", http.HttpMethods.Single());
        Assert.Equal("~/api/farms/{farmId}/invoice-templates/{key}/preview", http.Template);
        Assert.Empty(method.GetCustomAttributes<Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute>());
        Assert.NotEmpty(typeof(InvoicesController).GetCustomAttributes<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>());
    }

    [Fact]
    public async Task Preview_PassesTheColoursThrough_AndReturnsAnInlinePdf()
    {
        var mediator = new Mediator();
        var controller = new InvoicesController(mediator) { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };

        var result = await controller.Preview("farm-1", "ngf", "#1B2A6B", "#E67E22", default);

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/pdf", file.ContentType);
        Assert.True(string.IsNullOrEmpty(file.FileDownloadName));
        Assert.Equal("inline", controller.Response.Headers.ContentDisposition.ToString());
        Assert.Equal(new GenerateInvoicePreviewQuery("farm-1", "ngf", "#1B2A6B", "#E67E22"), mediator.Last);
    }
}
