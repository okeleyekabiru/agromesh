using System.Reflection;
using Ilevora.API.Controllers;
using Ilevora.API.Filters;
using Ilevora.Application.Commands.Farm;
using Ilevora.Application.Commands.Invoice;
using Ilevora.Application.Commands.VendorInvoice;
using Ilevora.Application.DTOs;
using Ilevora.Application.Queries.Farm;
using Ilevora.Domain.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Ilevora.Tests;

/// <summary>
/// The controllers are thin; what is worth proving here is the HTTP contract: who needs a token, the upload size limits,
/// the verbs and routes clients are told about, and how a multipart form becomes a command. No TestServer exists in this
/// repo, so the controllers are called directly with a recording IMediator.
/// </summary>
public class UploadControllersTests
{
    private class RecordingMediator : IMediator
    {
        public object? LastRequest;
        public object? Response;

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return Task.FromResult((TResponse)Response!);
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest
        {
            LastRequest = request;
            return Task.CompletedTask;
        }

        public Task<object?> Send(object request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task Publish(object notification, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification => throw new NotImplementedException();
    }

    private static IFormFile FormFile(byte[] bytes, string name = "logo.png", string type = "image/png")
    {
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "file", name) { Headers = new HeaderDictionary(), ContentType = type };
    }

    private static T WithHttp<T>(T controller) where T : ControllerBase
    {
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        return controller;
    }

    // ---- authentication & contract -------------------------------------------------------------------------------

    [Theory]
    [InlineData(typeof(FarmLogoController))]
    [InlineData(typeof(FarmBusinessProfileController))]
    [InlineData(typeof(InvoicesController))]
    [InlineData(typeof(VendorInvoicesController))]
    public void EveryNewController_RequiresAnAuthenticatedUser_AndNoActionOptsOut(Type controller)
    {
        Assert.NotEmpty(controller.GetCustomAttributes<AuthorizeAttribute>(inherit: false));
        foreach (var action in controller.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            Assert.Empty(action.GetCustomAttributes<AllowAnonymousAttribute>());
    }

    [Theory]
    [InlineData(typeof(FarmLogoController), nameof(FarmLogoController.Upload), 3L * 1024 * 1024)]
    [InlineData(typeof(VendorInvoicesController), nameof(VendorInvoicesController.Upload), 11L * 1024 * 1024)]
    public void UploadActions_CapTheRequestSize_AboveTheFileLimit(Type controller, string action, long limit)
    {
        var attribute = controller.GetMethod(action)!.GetCustomAttribute<RequestSizeLimitAttribute>();
        Assert.NotNull(attribute);
        Assert.Equal(limit, ((Microsoft.AspNetCore.Http.Metadata.IRequestSizeLimitMetadata)attribute!).MaxRequestBodySize);
        Assert.NotNull(controller.GetMethod(action)!.GetCustomAttribute<ConsumesAttribute>());
    }

    [Fact]
    public void RequestLimits_AreBiggerThanTheFileLimits_ButOnlyJustSo()
    {
        Assert.True(3L * 1024 * 1024 > Application.Services.UploadRules.Logo.MaxBytes);
        Assert.True(11L * 1024 * 1024 > Application.Services.UploadRules.VendorInvoice.MaxBytes);
    }

    private static (string Verb, string Template) Route(Type controller, string action)
    {
        var method = controller.GetMethod(action)!;
        var http = method.GetCustomAttributes<HttpMethodAttribute>().Single();
        return (http.HttpMethods.Single(), http.Template ?? "");
    }

    [Theory]
    [InlineData(typeof(FarmLogoController), nameof(FarmLogoController.Upload), "PUT", "")]
    [InlineData(typeof(FarmLogoController), nameof(FarmLogoController.Get), "GET", "")]
    [InlineData(typeof(FarmLogoController), nameof(FarmLogoController.Delete), "DELETE", "")]
    [InlineData(typeof(FarmBusinessProfileController), nameof(FarmBusinessProfileController.Get), "GET", "")]
    [InlineData(typeof(FarmBusinessProfileController), nameof(FarmBusinessProfileController.Update), "PUT", "")]
    [InlineData(typeof(InvoicesController), nameof(InvoicesController.GetAll), "GET", "")]
    [InlineData(typeof(InvoicesController), nameof(InvoicesController.Create), "POST", "")]
    [InlineData(typeof(InvoicesController), nameof(InvoicesController.GetById), "GET", "{invoiceId}")]
    [InlineData(typeof(InvoicesController), nameof(InvoicesController.Update), "PUT", "{invoiceId}")]
    [InlineData(typeof(InvoicesController), nameof(InvoicesController.Delete), "DELETE", "{invoiceId}")]
    [InlineData(typeof(InvoicesController), nameof(InvoicesController.GetPdf), "GET", "{invoiceId}/pdf")]
    [InlineData(typeof(InvoicesController), nameof(InvoicesController.GetTemplates), "GET", "~/api/farms/{farmId}/invoice-templates")]
    [InlineData(typeof(VendorInvoicesController), nameof(VendorInvoicesController.Upload), "POST", "orders/{orderId}/vendor-invoices")]
    [InlineData(typeof(VendorInvoicesController), nameof(VendorInvoicesController.GetForOrder), "GET", "orders/{orderId}/vendor-invoices")]
    [InlineData(typeof(VendorInvoicesController), nameof(VendorInvoicesController.GetAll), "GET", "vendor-invoices")]
    [InlineData(typeof(VendorInvoicesController), nameof(VendorInvoicesController.GetById), "GET", "vendor-invoices/{vendorInvoiceId}")]
    [InlineData(typeof(VendorInvoicesController), nameof(VendorInvoicesController.Download), "GET", "vendor-invoices/{vendorInvoiceId}/file")]
    [InlineData(typeof(VendorInvoicesController), nameof(VendorInvoicesController.SetStatus), "PATCH", "vendor-invoices/{vendorInvoiceId}/status")]
    [InlineData(typeof(VendorInvoicesController), nameof(VendorInvoicesController.Delete), "DELETE", "vendor-invoices/{vendorInvoiceId}")]
    public void Routes_AreWhatTheContractSays(Type controller, string action, string verb, string template)
    {
        Assert.Equal((verb, template), Route(controller, action));
    }

    [Theory]
    [InlineData(typeof(FarmLogoController), "api/farms/{farmId}/logo")]
    [InlineData(typeof(FarmBusinessProfileController), "api/farms/{farmId}/business-profile")]
    [InlineData(typeof(InvoicesController), "api/farms/{farmId}/invoices")]
    [InlineData(typeof(VendorInvoicesController), "api/farms/{farmId}")]
    public void ControllerRoots_AreFarmScoped(Type controller, string root)
    {
        Assert.Equal(root, controller.GetCustomAttribute<RouteAttribute>()!.Template);
    }

    // ---- logo -----------------------------------------------------------------------------------------------------

    [Fact]
    public async Task LogoUpload_PassesTheBytesNameAndTypeToTheCommand()
    {
        var mediator = new RecordingMediator { Response = new FarmLogoDto(true, "image/png", 12, DateTime.UtcNow) };
        var controller = WithHttp(new FarmLogoController(mediator));

        var result = await controller.Upload("farm-1", FormFile(UploadedFileValidatorTests.Png, "my logo.png"), default);

        Assert.IsType<OkObjectResult>(result);
        var command = Assert.IsType<UploadFarmLogoCommand>(mediator.LastRequest);
        Assert.Equal("farm-1", command.FarmId);
        Assert.Equal(UploadedFileValidatorTests.Png, command.Content);
        Assert.Equal("my logo.png", command.FileName);
        Assert.Equal("image/png", command.ContentType);
    }

    [Fact]
    public async Task LogoUpload_WithoutAFilePart_Is400()
    {
        var controller = WithHttp(new FarmLogoController(new RecordingMediator()));
        var result = await controller.Upload("farm-1", null, default);
        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, Assert.IsType<ProblemDetails>(bad.Value).Status);
    }

    [Fact]
    public async Task LogoUpload_LeavesSizeAndTypeChecksToTheHandler_SoTheRoleIsCheckedFirst()
    {
        // A caller who may not change the logo must get 403 from the handler even when the file is also bad.
        var mediator = new RecordingMediator { Response = new FarmLogoDto(false, null, null, null) };
        var controller = WithHttp(new FarmLogoController(mediator));

        await controller.Upload("farm-1", FormFile(new byte[2 * 1024 * 1024 + 1]), default);

        Assert.IsType<UploadFarmLogoCommand>(mediator.LastRequest);
    }

    [Fact]
    public async Task LogoGet_SendsBytesWithTheStoredContentType_AndAnETag()
    {
        var updated = new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);
        var mediator = new RecordingMediator { Response = new FarmLogoFile(UploadedFileValidatorTests.Png, "image/png", "logo.png", updated) };
        var controller = WithHttp(new FarmLogoController(mediator));

        var result = await controller.Get("farm-1", default);

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("image/png", file.ContentType);
        Assert.Equal(UploadedFileValidatorTests.Png, file.FileContents);
        Assert.NotNull(file.EntityTag);
        Assert.Contains(updated.Ticks.ToString(), file.EntityTag!.Tag.Value);
        Assert.StartsWith("private", controller.Response.Headers.CacheControl.ToString());
        Assert.Equal("nosniff", controller.Response.Headers["X-Content-Type-Options"].ToString());
        Assert.IsType<GetFarmLogoQuery>(mediator.LastRequest);
    }

    [Fact]
    public async Task LogoDelete_Is204()
    {
        var mediator = new RecordingMediator();
        var result = await WithHttp(new FarmLogoController(mediator)).Delete("farm-1", default);
        Assert.IsType<NoContentResult>(result);
        Assert.IsType<DeleteFarmLogoCommand>(mediator.LastRequest);
    }

    // ---- invoices -------------------------------------------------------------------------------------------------

    [Fact]
    public async Task InvoiceCreate_Is201WithALocation()
    {
        var dto = new InvoiceDto("inv-1", "farm-1", 1, "INV-0001", new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 15), "B", null, null, null, [], 0, 0, 0, 0, null, null, null, null, null, "ngf", DateTime.UtcNow, DateTime.UtcNow);
        var mediator = new RecordingMediator { Response = dto };
        var request = new SaveInvoiceRequest(null, null, "B", null, null, null, [new InvoiceLineItemRequest("x", 1, 1)], null, null, null, null, null, null, null);

        var result = await WithHttp(new InvoicesController(mediator)).Create("farm-1", request, default);

        var created = Assert.IsType<CreatedResult>(result);
        Assert.Equal("api/farms/farm-1/invoices/inv-1", created.Location);
        Assert.IsType<CreateInvoiceCommand>(mediator.LastRequest);
    }

    [Fact]
    public async Task InvoiceDelete_Is204()
    {
        var result = await WithHttp(new InvoicesController(new RecordingMediator())).Delete("farm-1", "inv-1", default);
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task InvoicePdf_IsSentAsAPdfDownloadNamedAfterTheInvoice()
    {
        var mediator = new RecordingMediator { Response = new InvoicePdfFile("%PDF-x"u8.ToArray(), "INV-0001.pdf") };
        var result = await WithHttp(new InvoicesController(mediator)).GetPdf("farm-1", "inv-1", default);

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/pdf", file.ContentType);
        Assert.Equal("INV-0001.pdf", file.FileDownloadName);
    }

    // ---- vendor invoices --------------------------------------------------------------------------------------------

    private static UploadVendorInvoiceForm Form(byte[] bytes, string type = "application/pdf") => new()
    {
        File = FormFile(bytes, "saro.pdf", type),
        VendorName = "Saro",
        InvoiceNumber = "S-1",
        Amount = "1234.5",
        InvoiceDate = new DateOnly(2026, 10, 5),
        DueDate = new DateOnly(2026, 11, 4),
        Notes = "n",
    };

    private static VendorInvoiceDto BillDto() => new("v1", "farm-1", "o1", "ORD-1001", 100, false, "s1", "Saro", "S-1", 1234.5m,
        new DateOnly(2026, 10, 5), null, "Unpaid", null, false, null, "saro.pdf", "application/pdf", 10, "u", DateTime.UtcNow);

    [Fact]
    public async Task VendorUpload_MapsTheFormToTheCommand_AndReturns201()
    {
        var mediator = new RecordingMediator { Response = BillDto() };
        var controller = WithHttp(new VendorInvoicesController(mediator));

        var result = await controller.Upload("farm-1", "o1", Form(UploadedFileValidatorTests.Pdf), default);

        var created = Assert.IsType<CreatedResult>(result);
        Assert.Equal("api/farms/farm-1/vendor-invoices/v1", created.Location);
        var command = Assert.IsType<UploadVendorInvoiceCommand>(mediator.LastRequest);
        Assert.Equal("o1", command.OrderId);
        Assert.Equal(UploadedFileValidatorTests.Pdf, command.Content);
        Assert.Equal("saro.pdf", command.FileName);
        Assert.Equal("application/pdf", command.ContentType);
        Assert.Equal(new UploadVendorInvoiceFields("Saro", "S-1", 1234.5m, new DateOnly(2026, 10, 5), new DateOnly(2026, 11, 4), "n"), command.Fields);
    }

    [Fact]
    public async Task VendorUpload_LeavesSizeAndTypeChecksToTheHandler_SoTheRoleIsCheckedFirst()
    {
        var mediator = new RecordingMediator { Response = BillDto() };
        var controller = WithHttp(new VendorInvoicesController(mediator));

        await controller.Upload("farm-1", "o1", Form(new byte[10 * 1024 * 1024 + 1]), default);

        Assert.IsType<UploadVendorInvoiceCommand>(mediator.LastRequest);
    }

    [Theory]
    [InlineData("1,5")]
    [InlineData("1,2,3")]
    [InlineData("12,")]
    [InlineData("1e3")]
    [InlineData(" 12 ")]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("-3")]
    public async Task VendorUpload_MalformedAmount_Is400_AndNothingIsSent(string? amount)
    {
        var mediator = new RecordingMediator { Response = BillDto() };
        var controller = WithHttp(new VendorInvoicesController(mediator));
        var form = Form(UploadedFileValidatorTests.Pdf);
        form.Amount = amount;

        var result = await controller.Upload("farm-1", "o1", form, default);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        var problem = Assert.IsType<ValidationProblemDetails>(bad.Value);
        Assert.Equal(400, problem.Status);
        Assert.Contains("Amount", problem.Errors.Keys);
        Assert.Null(mediator.LastRequest);
    }

    [Fact]
    public void VendorUploadForm_MarksTheMandatoryFieldsRequired()
    {
        foreach (var name in new[] { nameof(UploadVendorInvoiceForm.File), nameof(UploadVendorInvoiceForm.InvoiceNumber), nameof(UploadVendorInvoiceForm.Amount), nameof(UploadVendorInvoiceForm.InvoiceDate) })
            Assert.NotNull(typeof(UploadVendorInvoiceForm).GetProperty(name)!.GetCustomAttribute<System.ComponentModel.DataAnnotations.RequiredAttribute>());
        foreach (var name in new[] { nameof(UploadVendorInvoiceForm.VendorName), nameof(UploadVendorInvoiceForm.DueDate), nameof(UploadVendorInvoiceForm.Notes) })
            Assert.Null(typeof(UploadVendorInvoiceForm).GetProperty(name)!.GetCustomAttribute<System.ComponentModel.DataAnnotations.RequiredAttribute>());
    }

    [Fact]
    public async Task VendorDownload_IsAnAttachmentByDefault_InlineOnRequest()
    {
        var mediator = new RecordingMediator { Response = new VendorInvoiceFile(UploadedFileValidatorTests.Pdf, "application/pdf", "Saro Oct.pdf") };

        var attachment = Assert.IsType<FileContentResult>(await WithHttp(new VendorInvoicesController(mediator)).Download("farm-1", "v1", false, default));
        Assert.Equal("Saro Oct.pdf", attachment.FileDownloadName);
        Assert.Equal("application/pdf", attachment.ContentType);

        var inlineController = WithHttp(new VendorInvoicesController(mediator));
        var inline = Assert.IsType<FileContentResult>(await inlineController.Download("farm-1", "v1", true, default));
        Assert.True(string.IsNullOrEmpty(inline.FileDownloadName));
        Assert.StartsWith("inline", inlineController.Response.Headers.ContentDisposition.ToString());
        Assert.Equal("nosniff", inlineController.Response.Headers["X-Content-Type-Options"].ToString());
    }

    [Fact]
    public async Task VendorList_PassesFiltersThrough()
    {
        var mediator = new RecordingMediator { Response = new List<VendorInvoiceDto>() };
        await WithHttp(new VendorInvoicesController(mediator)).GetAll("farm-1", "paid", "o1", default);
        Assert.Equal(new Ilevora.Application.Queries.VendorInvoice.GetVendorInvoicesQuery("farm-1", "o1", "paid"), mediator.LastRequest);

        await WithHttp(new VendorInvoicesController(mediator)).GetForOrder("farm-1", "o2", default);
        Assert.Equal(new Ilevora.Application.Queries.VendorInvoice.GetVendorInvoicesQuery("farm-1", "o2", null), mediator.LastRequest);
    }

    [Fact]
    public async Task VendorDelete_Is204_AndStatusReturnsTheBill()
    {
        var mediator = new RecordingMediator { Response = BillDto() };
        var controller = WithHttp(new VendorInvoicesController(mediator));

        Assert.IsType<OkObjectResult>(await controller.SetStatus("farm-1", "v1", new SetVendorInvoiceStatusRequest("Paid", null), default));
        Assert.IsType<NoContentResult>(await controller.Delete("farm-1", "v1", default));
    }
}

public class UploadSizeLimitFilterTests
{
    private static ResourceExecutingContext Context(long? contentLength)
    {
        var http = new DefaultHttpContext();
        http.Request.ContentLength = contentLength;
        var action = new ActionContext(http, new Microsoft.AspNetCore.Routing.RouteData(), new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor());
        return new ResourceExecutingContext(action, new List<Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata>(), new List<Microsoft.AspNetCore.Mvc.ModelBinding.IValueProviderFactory>());
    }

    [Fact]
    public void DeclaredBodyOverTheLimit_IsRefusedWith413_BeforeModelBinding()
    {
        var filter = new UploadSizeLimitAttribute(1000);
        Assert.Throws<Ilevora.Domain.Exceptions.PayloadTooLargeException>(() => filter.OnResourceExecuting(Context(1001)));
    }

    [Theory]
    [InlineData(1000L)]
    [InlineData(1L)]
    [InlineData(null)]
    public void DeclaredBodyWithinTheLimit_OrUnknown_PassesThrough(long? length)
    {
        new UploadSizeLimitAttribute(1000).OnResourceExecuting(Context(length));
    }

    [Theory]
    [InlineData(typeof(FarmLogoController), nameof(FarmLogoController.Upload), 3L * 1024 * 1024)]
    [InlineData(typeof(VendorInvoicesController), nameof(VendorInvoicesController.Upload), 11L * 1024 * 1024)]
    public void UploadActions_UseTheFilter_WithTheSameLimitAsTheServerCap(Type controller, string action, long limit)
    {
        var attribute = controller.GetMethod(action)!.GetCustomAttribute<UploadSizeLimitAttribute>();
        Assert.NotNull(attribute);
        Assert.Equal(limit, attribute!.MaxBytes);
    }
}
