# Plan

Order is red-green per step; run `dotnet test api/AgroMesh.slnx` after each.

1. **Upload validation service** (`Application/Services/UploadedFileValidator.cs`, `UploadRules`): magic bytes, size, declared type, allowed sets. New exceptions `PayloadTooLargeException` (413), `UnsupportedFileTypeException` (415) deriving from `DomainException`. Tests: `UploadedFileValidatorTests`. Also `ExceptionMiddleware` branches.
2. **Stored files**: `Domain/Entities/StoredFile.cs`, `IStoredFileRepository`, `StoredFileRepository`, `StoredFileConfiguration`. Tests via fakes in the later steps.
3. **Farm**: logo (`LogoFileId`, `LogoUpdatedAt`, `SetLogo/ClearLogo`) and business profile (`Address, Phone, Email, Website, BankAccountName, BankAccountNumber, BankName`, `SetBusinessProfile`). `FarmDto` gains `HasLogo`, `LogoUpdatedAt`. `FarmAccess` helper (tenant + role). Tests: `FarmBusinessProfileTests`.
4. **Logo handlers**: `UploadFarmLogoCommand`, `DeleteFarmLogoCommand`, `GetFarmLogoQuery`, `IImageInspector` (+ Infrastructure QuestPDF impl). Tests: `FarmLogoTests`, `ImageInspectorTests`.
5. **Business profile handlers** `GetFarmBusinessProfileQuery`, `UpdateFarmBusinessProfileCommand`.
6. **Invoice domain**: `Invoice`, `InvoiceLineItem`, `InvoiceTemplateKeys`. Tests: `InvoiceTests`.
7. **Invoice handlers**: Create/Update/Delete/Get/List, `IInvoiceRepository`, `IInvoiceTemplateRegistry`, `GenerateInvoicePdfQuery`, `GetInvoiceTemplatesQuery`. Tests: `InvoiceHandlerTests`.
8. **Renderer**: `IInvoiceTemplateRenderer`, `InvoiceRenderModel`, `NgfInvoiceTemplate` (+ embedded Plus Jakarta Sans), `InvoiceTemplateRegistry`. Tests: `InvoiceRendererTests`; visually checked by rasterising the PDF.
9. **Vendor bills**: `VendorInvoice`, `VendorInvoiceStatus`, `IVendorInvoiceRepository`, commands/queries, DTOs. Tests: `VendorInvoiceTests`.
10. **Controllers**: `FarmLogoController`, `FarmBusinessProfileController`, `InvoicesController`, `VendorInvoicesController` (+ order-nested list/upload routes). Tests: `ApiContractTests`.
11. **Persistence**: DbContext sets, configurations, repositories, DI, migration `AddLogoInvoicesAndVendorBills`.
12. Self-review, fix, full suite.

Migration/data model: tables `StoredFiles`, `Invoices`, `VendorInvoices`; columns on `Farms`: `LogoFileId`, `LogoUpdatedAt`, `Address`, `Phone`, `Email`, `Website`, `BankAccountName`, `BankAccountNumber`, `BankName`. Unique (FarmId, Number) on Invoices.

Risks: variable Plus Jakarta Sans in QuestPDF (verify visually, fall back to static instance); bytea size on the free DB tier (limits kept low); concurrent invoice numbering (unique index + retry); `FarmDto` positional record extended only with defaulted trailing parameters.
