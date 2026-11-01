# API contract: farm logo, invoices, vendor invoices

Base `/api`. All routes need `Authorization: Bearer <jwt>`. JSON camelCase, enums as strings. Dates `yyyy-MM-dd`, timestamps ISO-8601 UTC. IDs are strings. Errors are RFC 7807 problem+json `{type,title,status,detail,traceId}`; 422 `detail` is a human message; 400 uses `errors:{Field:[msgs]}`.

Statuses: 400 bad/missing field, 401 no token, 403 role, 404 not found or outside company/farm, 413 too large, 415 wrong file type, 422 rule violation.
Roles: read = any company user (incl. Member). Write = Admin or FarmManager, except vendor-bill upload (Admin, FarmManager, Supervisor).

## 1. Farm logo
- PUT /farms/{farmId}/logo  multipart/form-data, one part `file`. PNG/JPEG/WebP, max 2 MB, max 4096x4096. 200 `{hasLogo:true, contentType, size, updatedAt}`. Errors 400/403/413/415/422.
- GET /farms/{farmId}/logo  image bytes. ETag, Cache-Control private max-age=300, supports If-None-Match -> 304. 404 if no logo. REQUIRES bearer token: web cannot use <img src>; fetch bytes with axios (responseType blob) and use a blob URL. Flutter: Image.network(url, headers: {...}) or http bytes + Image.memory. Cache-bust with farm.logoUpdatedAt.
- DELETE /farms/{farmId}/logo -> 204 (even if none).
- GET /farms and GET /farms/{id} now include `hasLogo: bool`, `logoUpdatedAt: string|null`.

## 1b. Farm business profile (printed on invoices)
GET and PUT /farms/{farmId}/business-profile (PUT full replace; missing/null/blank clears):
`{address, phone, email, website, bankAccountName, bankAccountNumber, bankName}` all optional strings; max lengths 300, 60, 200, 200, 150, 40, 100. email shape-checked, violations 422.

## 2. Sales invoices
- GET /farms/{farmId}/invoice-templates -> `[{key:"ngf", name:"NGF - Invoice", description, isDefault:true}]`
- GET /farms/{farmId}/invoices -> `[{id, number, reference:"INV-0002", date, dueDate, billToName, total, templateKey, createdAt}]` newest first
- POST /farms/{farmId}/invoices -> 201 full invoice
- PUT /farms/{farmId}/invoices/{id} -> full invoice (same body; number never changes)
- GET /farms/{farmId}/invoices/{id} -> full invoice
- DELETE /farms/{farmId}/invoices/{id} -> 204
- GET /farms/{farmId}/invoices/{id}/pdf -> application/pdf attachment INV-0001.pdf (any company user). Embeds the farm logo, else prints farm name.

Request body (POST/PUT):
```
{ date?: "yyyy-MM-dd"  (create default today UTC; send it explicitly),
  dueDate?: (create default date+14d; must be >= date),
  billToName: required max 200, billToPhone? (60), billToEmail? (200), billToAddress? (500),
  lineItems: [{description (required, max 500), rate (>=0), quantity (>0)}]  1..200 items,
  vatPercent?: 0..100 default 0,
  paymentAccountName?, paymentAccountNumber?, paymentBankName?   (on create, if all blank copied from farm business profile; on update blank clears),
  termsAndNotes? max 2000, signatoryName? max 150,
  templateKey?: default "ngf"; unknown -> 422 }
```
Response: `{id, farmId, number, reference, date, dueDate, billToName, billToPhone, billToEmail, billToAddress, lineItems:[{serialNumber, description, rate, quantity, amount}], subtotal, vatPercent, vatAmount, total, paymentAccountName, paymentAccountNumber, paymentBankName, termsAndNotes, signatoryName, templateKey, createdAt, updatedAt}`
Amounts computed server-side (rate x qty, 2dp). Show a live preview client-side but treat server values as truth.

## 3. Vendor invoices (bills received from suppliers; vendor = Supplier; "completed order" = OrderStatus.Received)
- POST /farms/{farmId}/orders/{orderId}/vendor-invoices  multipart: `file` (required, PDF/PNG/JPEG, max 10 MB), `invoiceNumber` (required, max 100), `amount` (required decimal with '.', >0), `invoiceDate` (required yyyy-MM-dd), `dueDate?` (>= invoiceDate), `vendorName?` (defaults to order supplier name, max 200), `notes?` (max 1000). Roles Admin/FarmManager/Supervisor (Member 403). 201 VendorInvoice. 422 if order not Received (message names status) or duplicate invoice number for that supplier. 404 unknown order. 413/415 file problems.
- GET /farms/{farmId}/orders/{orderId}/vendor-invoices -> VendorInvoice[]
- GET /farms/{farmId}/vendor-invoices?status=Paid|Unpaid&orderId=... -> VendorInvoice[]
- GET /farms/{farmId}/vendor-invoices/{id} -> VendorInvoice
- GET /farms/{farmId}/vendor-invoices/{id}/file  download (attachment); `?inline=true` to display. Needs bearer token (fetch as blob / bytes).
- PATCH /farms/{farmId}/vendor-invoices/{id}/status body `{status:"Paid"|"Unpaid", paidAt?: ISO}` -> VendorInvoice (Admin/FarmManager)
- DELETE /farms/{farmId}/vendor-invoices/{id} -> 204 (Admin/FarmManager)

VendorInvoice: `{id, farmId, orderId, orderReference, orderTotal, orderIsPaid, supplierId, vendorName, invoiceNumber, amount, invoiceDate, dueDate|null, status:"Unpaid"|"Paid", paidAt|null, isOverdue, notes, fileName, contentType, fileSize, uploadedByUserId, createdAt}`
Show a mismatch hint when amount != orderTotal. No metadata edit endpoint: delete and re-upload.

Full spec: docs/features/farm-logo-invoices-vendor-bills/spec.md
