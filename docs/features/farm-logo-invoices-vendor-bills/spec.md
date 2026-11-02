# Farm logo, generated invoices, and vendor bills (API slice)

> Note: the product was named Ilevora when these docs were written; it is now Ilevora. Docs under `docs/features/` are kept as historical.

Scope of this document: the `api/` (.NET) slice only. Web and mobile are built separately against the contract in the final report.

## Goal

1. A farm manager/owner can upload, replace and delete the farm logo; after login every client can fetch it and show it in place of the Ilevora logo.
2. A farm manager/owner can create sales invoices (customer bill-to, line items, VAT, payment details, terms, signatory), with automatic per-farm invoice numbers, and download a branded A4 PDF that embeds the farm logo. Invoices render through a template; "NGF - Invoice" (Nature's Gold Farm) ships as the default and new templates are added by writing one renderer class.
3. A user can attach the invoice a vendor (supplier) issued to the farm to an order once that order is completed (Received): upload the file (pdf/png/jpg) with vendor name, invoice number, amount, dates and notes; list per farm and per order; download; mark paid/unpaid; delete.

## Context / existing code

- Orders and suppliers already exist ("vendor" = `Supplier`): `api/Ilevora.Domain/Entities/Order.cs`, `Supplier.cs`, `Enums/OrderStatus.cs` (Draft -> Placed -> Received). "Completed" = `Received`. Controllers `OrdersController`, `SuppliersController` (route `api/farms/{farmId}/...`, class-level `[Authorize]`).
- QuestPDF is already a dependency, licence set in `Program.cs`; an order PDF exists in `Ilevora.Infrastructure/Services/OrderInvoiceService.cs` (this is the purchase-order document and is left alone).
- MediatR command/query + hand-rolled-fake handler tests (`Ilevora.Tests/SupplierAndOrderHandlerTests.cs`, `StockAdjustmentApprovalTests.cs`). Roles via `ICurrentUserAccessor` (`UserRole`: Admin = company owner, FarmManager, Supervisor, Member), `ForbiddenException` -> 403, `NotFoundException` -> 404, `DomainException` -> 422, all as RFC 7807 in `ExceptionMiddleware`.
- Per-farm sequential numbering precedent: `IOrderRepository.GetNextNumberAsync`.
- jsonb value-object list precedent: `Order.LineItems` / `OrderConfiguration`.
- No file/blob storage exists anywhere. Render's disk is ephemeral and the DB is Supabase Postgres, so uploads are stored as `bytea` rows in a new `StoredFiles` table behind `IStoredFileRepository` (swappable for object storage later).
- Finance: bank `Transaction`s come from Mono; paying an order is matched by narration (`Order.MarkPaid`) and stock purchases are deliberately not counted as expenses until used (`Transaction.IsStockPurchase`). A vendor bill therefore must NOT create a ledger entry (double count). It only records the document and its own paid flag, and exposes the order's reference/total/paid state for comparison.
- Farm has no address/phone/email/website/bank-account-name fields (`BankAccountId` is the Mono link id). They are added.

## Requirements

### Farm logo
1. `PUT /api/farms/{farmId}/logo` (multipart `file`) stores the logo; replaces and deletes the previous stored file.
2. Only Admin or FarmManager may upload/replace/delete (403 otherwise). Any authenticated user of the same company may read.
3. Allowed: PNG, JPEG, WebP. Max 2 MB. Max 4096x4096 px. Empty file rejected.
4. Validated by magic bytes, not only the declared content type/extension; declared content type, if present, must agree with the bytes; the image must decode. SVG and everything else rejected (415).
5. `GET /api/farms/{farmId}/logo` returns the bytes with the sniffed content type, ETag and `Cache-Control: private`, 404 if none. `DELETE` removes it (204, idempotent).
6. Farms of other companies are 404 for every endpoint (fails closed when the caller has no company).
7. `FarmDto` exposes `hasLogo` and `logoUpdatedAt` (cache-bust key).

### Farm business profile (fields printed on invoices)
8. `GET/PUT /api/farms/{farmId}/business-profile`: address, phone, email, website, bankAccountName, bankAccountNumber, bankName. PUT is Admin/FarmManager only; blank -> null; length limits; email shape checked.

### Invoices (sales)
9. Invoice fields: number (auto, per farm, starts at 1, display `INV-0001`), date, dueDate, bill-to name/phone/email/address, line items (description, rate, quantity; S/N and amount are computed), subtotal, vatPercent, vatAmount, total, payment block (account name/number/bank name, defaulting from the farm profile when omitted), termsAndNotes, signatoryName, templateKey.
10. Validation: bill-to name required; 1..200 line items; description required; rate >= 0; quantity > 0; vatPercent 0..100; dueDate >= date; unknown templateKey rejected. Money is rounded to 2 dp (away from zero) per line; VAT on the subtotal.
11. CRUD: create (201), list (newest first, summary rows), get, update (full replace, number unchanged), delete (204). Admin/FarmManager write; same-company read. Invoice id from another farm is 404.
12. Numbers are unique per farm (unique index) and monotonically increasing; concurrent collision retries.
13. `GET /api/farms/{farmId}/invoices/{id}/pdf` returns a valid A4 PDF built by the renderer for the invoice's templateKey, embeds the farm logo when present, and still renders when there is no logo.
14. `GET /api/farms/{farmId}/invoice-templates` lists available templates (`key`, `name`). Default `ngf`.
15. Template design (NGF): A4 portrait; logo top right; BILLING TO block (yellow underline, P/E/A rows); INVOICE title with Invoice/Date/Due Date at right; item table (yellow parallelogram tab for S/N + ITEM DESCRIPTION, green bar for RATE/QTY/AMOUNT, at least 5 rows with thin grey grid); SUBTOTAL and TAX VAT (x%) right aligned, grey rule, TOTAL band; green PAYMENT METHOD and TERMS & CONDITIONS/NOTES at left; "Name & Signature" line at right with signatory; footer contact rows (location, phone, email, website) and three decorative green/yellow lines with circle ends; Plus Jakarta Sans (embedded). Colours are constants in the template.

### Vendor bills
16. `POST /api/farms/{farmId}/orders/{orderId}/vendor-invoices` (multipart: `file` + fields) creates a `VendorInvoice` for an order of this farm whose status is `Received`; any other status -> 422.
17. Allowed files: PDF, PNG, JPEG; max 10 MB; magic-byte validated (`%PDF-`, PNG, JPEG), declared type must agree; others 415; too large 413.
18. Fields: vendorName (defaults to the order's supplier name), invoiceNumber (required), amount > 0, invoiceDate (required), dueDate (optional, >= invoiceDate), notes (optional, <= 1000). Duplicate invoiceNumber for the same supplier on the same farm -> 422.
19. Status `Unpaid`/`Paid` (`PATCH .../status`), `paidAt` set when paid, cleared when unpaid; `isOverdue` computed.
20. List per farm (`GET /vendor-invoices?status=&orderId=`) and per order (`GET /orders/{orderId}/vendor-invoices`), get one, download file (`GET .../file`), delete (removes the stored file).
21. Roles: upload by Admin/FarmManager/Supervisor; status change and delete by Admin/FarmManager; read by any same-company user.
22. DTO carries order reference, order total and whether the order is paid (bank-matched), so a client can flag amount mismatches. No ledger/Transaction is created.

### Cross-cutting
23. Errors are RFC 7807 problem+json: 400 unreadable/missing multipart, 403, 404, 413 too large, 415 unsupported type, 422 rule violations. Kestrel body-size rejections map to 413 instead of 500.
24. Two EF migrations (or one) add the new tables/columns; model snapshot updated.

## Non-goals

- Web and mobile UI. Emailing/sending invoices. Invoice payment tracking/status on sales invoices, credit notes, multi-currency (PDF uses the naira sign like `OrderInvoiceService`), customer address book, per-invoice logo override.
- Editing vendor-bill metadata after upload (delete and re-upload), OCR of uploaded bills, linking a vendor bill to a bank transaction, creating expenses from vendor bills.
- Object-storage backend (bytea behind an interface instead). Antivirus scanning.
- Public (unauthenticated) logo URL: the logo is fetched with the bearer token.

## Acceptance criteria

Each maps to automated tests in `api/Ilevora.Tests` (xUnit, fakes; PDF tests use real QuestPDF).

- [x] AC1 Logo upload stores a valid PNG/JPEG/WebP, sets `LogoFileId/LogoUpdatedAt`, returns info (LogoTests)
- [x] AC2 Replace deletes the old stored file; delete clears the farm and the file; delete with no logo is a no-op (LogoTests)
- [x] AC3 Logo rejects: empty, > 2 MB, SVG, text renamed `.png`, declared type that disagrees with bytes, undecodable image, oversize dimensions (FileSniffer + LogoTests)
- [x] AC4 Logo write is 403 for Supervisor/Member, allowed for Admin/FarmManager; other company's farm is 404; no company -> 404 (LogoTests)
- [x] AC5 Get logo returns bytes/content type; 404 when none (LogoTests)
- [x] AC6 Business profile get/update, blank->null, bad email rejected, role gate (BusinessProfileTests)
- [x] AC7 Invoice totals: line amount, subtotal, VAT, total, rounding, S/N (InvoiceTests)
- [x] AC8 Invoice validation rules of req 10 (InvoiceTests)
- [x] AC9 Create assigns next per-farm number independent per farm, payment block defaults from farm, template validated (InvoiceHandlerTests)
- [x] AC10 Update/get/list/delete incl. farm scoping, role gate, number kept on update (InvoiceHandlerTests)
- [x] AC11 Number collision retry (repository contract covered by handler/fake; unique index in config test)
- [x] AC12 NGF renderer produces a PDF (`%PDF`), with and without logo, with 1 and 40 items (multi-page), long text (InvoiceRendererTests)
- [x] AC13 Render handler loads farm logo into the render model and picks the renderer by key; unknown key fails; templates list contains `ngf` (InvoiceHandlerTests)
- [x] AC14 Vendor bill requires a Received order; Draft/Placed rejected; other farm's order 404 (VendorInvoiceTests)
- [x] AC15 Vendor bill file validation (pdf/png/jpg ok; exe/html/gif/oversize/empty rejected; type mismatch) (FileSniffer + VendorInvoiceTests)
- [x] AC16 Vendor bill fields: vendor default from supplier, amount > 0, dueDate >= invoiceDate, duplicate number rejected, notes length (VendorInvoiceTests)
- [x] AC17 Mark paid/unpaid sets/clears paidAt; overdue computed; role gates (VendorInvoiceTests)
- [x] AC18 List per farm/per order with filters; download returns the stored bytes; delete removes record and stored file; scoping (VendorInvoiceTests)
- [x] AC19 Creating a vendor bill writes no Transaction/ledger entry (VendorInvoiceTests, repository surface of handler has no transaction dependency)
- [x] AC20 Controllers are `[Authorize]`; write actions' size limits are set; middleware maps 413/415/Kestrel body-too-large (ApiContractTests)
- [x] AC21 Migration compiles and model snapshot matches (`dotnet ef migrations has-pending-model-changes` / build)

## Test plan

- Domain unit: `Invoice` totals/validation, `VendorInvoice` rules, `Farm` profile + logo methods, `Order` untouched.
- Application services unit: `UploadedFileValidator` magic-byte matrix.
- Handler tests with fakes (repo conventions as the existing tests): logo, business profile, invoice CRUD/render, vendor bill CRUD.
- Infrastructure: real QuestPDF render of the NGF template (bytes start with `%PDF`, page count via simple `/Type /Page` scan > 1 for the long list), real image inspector on generated PNG/JPEG/WebP fixtures.
- API: reflection `[Authorize]`/size-limit checks; `ExceptionMiddleware` driven with `DefaultHttpContext` for 413/415.
- Edge/negative cases: zero/negative amounts and quantities, VAT 0 and 100, empty items, other tenant, role denied, wrong order status, duplicate number, polyglot/renamed files.
- Not tested automatically: EF against a real Postgres (no DB in the test setup); migration is verified by build + `has-pending-model-changes`.

## Outcome notes (as built)

- Final suite: 717 xUnit tests green (`dotnet test api/Ilevora.slnx`). Beyond the unit tests, the API was run against a throwaway Postgres 15 container (migration applied, all endpoints exercised with curl, 8 parallel invoice creates gave unique numbers); the container was removed afterwards.
- AC11: the unique index `IX_Invoices_FarmId_Number` plus the retry in `InvoiceRepository.AddAsync` is only verified by the smoke run, not by an automated test (no DB in the test setup).
- AC21: `dotnet ef migrations has-pending-model-changes` reports no changes after `AddLogoInvoicesAndVendorBills`.
- Plus Jakarta Sans ships as four static weights (400/600/700/800) cut from the variable font with fonttools, because QuestPDF picks weights by file and cannot choose inside a variable font.
- A `UploadSizeLimit` resource filter was added so a too-large upload answers 413 instead of the 400 the framework gives when model binding hits the server cap.
- The "completed" order status is `Received` (the existing model has Draft, Placed, Received). Vendors are the existing `Supplier`s.

## QA fixes (second pass)

- QA-1: the image inspector now draws with fit-area (portrait images no longer throw) and also rejects structurally incomplete files (PNG must reach IEND through well-formed chunks, JPEG must end with EOI, WebP must be as long as its RIFF header says). 100x4097 now gives 422 "too large in pixels"; a truncated PNG with a valid 4096x4096 header gives 415.
- QA-2/QA-3: the signatory name wraps (no fixed height); short terms stay beside the payment details, long terms (over 450 chars or 6+ lines) print full width below and flow across pages. Multi-line item descriptions are kept together on one page.
- QA-4: vendor bill `amount` is bound as text and parsed strictly (`StrictDecimal`: digits, optional single `.` fraction of 1-6 digits, max 15 integer digits); anything else is 400 with `errors.Amount`.
- QA-5: `VendorInvoices.InvoiceNumberKey` (trimmed, upper-cased number) with a UNIQUE index on (FarmId, SupplierId, InvoiceNumberKey); a unique violation becomes the same 422 and the stored file is deleted. SupplierId is never null (it is the received order's supplier), so there are no supplier-less rows. Migration `VendorBillUniqueKeyAndInvoiceCounters` back-fills keys; legacy duplicates keep the key for the oldest and get `#DUP-<n>` on their internal key (nothing deleted, shown number unchanged).
- QA-6/QA-7: invoice numbers come from a per-farm counter table (`InvoiceCounters`) advanced by one atomic upsert, seeded from each farm's existing maximum. A number is never handed out twice, even after the invoice is deleted; a rejected invoice does not spend a number; gaps are possible only if saving fails after the number was reserved. The old MAX+1 retry loop is gone. 32 parallel creates: 32 x 201, unique numbers.
- QA-8 and limits: invoice and due dates must be in 2000..2100 (vendor bill dates too). Rate < 100,000,000; quantity < 1,000,000; line amount < 1,000,000,000; subtotal < 10,000,000,000 (all 422 otherwise).
- Role is checked before file size/type/content in the handlers, so a caller who may not upload gets 403 (the controllers no longer pre-validate; only the Content-Length filter, 413 above 3 MB / 11 MB, runs first).

## Third pass: brand colours, ten templates, preview

### Goal
A farm can make its invoices look like its brand and pick one of ten modern layouts. Default look is unchanged: `ngf` stays the default template with green `#138A44` and yellow `#F5C90C`.

### Requirements (all API, all backward compatible)
1. Colours are strict `#RRGGBB` (3-digit, names, alpha, missing `#` are 422), normalised to upper case (`BrandColor`, `Domain/Utilities/BrandColor.cs`).
2. `GET/PUT /api/farms/{id}/business-profile` carry optional `brandPrimaryColor` / `brandAccentColor` (null or blank clears). Invoice create/update/response carry optional `primaryColor` / `accentColor`; the response also has `resolvedPrimaryColor`, `resolvedAccentColor` and `primaryColorSource` / `accentColorSource` (`invoice` | `farm` | `default`).
3. Resolution per colour: invoice colour, else farm brand colour, else template default (`InvoiceThemeResolver`). A PUT of an invoice is a full replace, so omitting a colour clears it (back to farm/default).
4. Text on a coloured area is black or white by WCAG luminance contrast; very light colours used as text/lines on white are darkened until legible (`InvoicePalette` / `BrandColor`).
5. The registry holds at most 10 templates (startup fails above that, also on repeated keys or bad default colours). The app registers exactly 10: `ngf` plus `band`, `sidebar`, `minimal`, `callout`, `cards`, `ledger`, `angle`, `anchor`, `duo`. All embed Plus Jakarta Sans only. Existing invoices that use `ngf` keep rendering; unknown key on create/update stays 422 listing the valid keys.
6. `GET /api/farms/{farmId}/invoice-templates` returns a stable order (ngf first, then by name) of `key, name, description, isDefault, defaultPrimaryColor, defaultAccentColor, previewUrl`.
7. `GET /api/farms/{farmId}/invoice-templates/{key}/preview?primaryColor=%23RRGGBB&accentColor=%23RRGGBB` returns an inline `application/pdf` with sample data, the farm logo and business profile; any user of the company; other company 404; invalid colour 422; unknown key 422; nothing is persisted (no invoice number is spent). Without query colours the farm brand colours then template defaults apply.
8. Migration `InvoiceBrandColours`: nullable `varchar(7)` `PrimaryColor`/`AccentColor` on `Invoices`, `BrandPrimaryColor`/`BrandAccentColor` on `Farms`; reversible.

### Acceptance criteria (each backed by tests)
- [x] Colour validation and normalisation: `BrandColorTests`, `InvoiceColourHandlerTests`.
- [x] Luminance text colour and light-colour legibility: `BrandColorTests`, `InvoiceThemeResolverTests`.
- [x] Resolution order invoice > farm > default: `InvoiceThemeResolverTests`, `InvoiceColourHandlerTests`.
- [x] Registry cap of 10 and bad-registration failures; exactly the ten keys; stable order; ngf default colours unchanged: `InvoiceTemplateCatalogTests`.
- [x] Every template renders (single page where expected) for no logo, landscape and portrait logo, 1/2/6 items, 200 items (multi page), max-length signatory and terms, max amounts with 100% VAT, long descriptions, no optional text, very light, very dark, mid, light-primary/dark-accent and saturated colours: `InvoiceTemplateCatalogTests.EveryTemplate_Renders_EveryScenario` (160 cases).
- [x] Colours reach the PDF; Plus Jakarta Sans embedded; logo embedded only when present: `InvoiceTemplateCatalogTests`.
- [x] Preview endpoint auth, tenancy, colour validation, unknown key, nothing persisted, inline PDF: `InvoicePreviewControllerTests`.
- [x] Earlier QA regression tests (`InvoiceRendererTests`, `InvoiceHandlerTests`, `InvoiceTests`) still pass.

### Notes
- Verified visually: the real API on a throwaway Postgres rendered all ten templates (default colours, custom colours `#B91C1C`/`#FACC15`, and a stress invoice with max amounts, 100% VAT, 150-character signatory and 2000-character terms) and the pages were inspected as images.
