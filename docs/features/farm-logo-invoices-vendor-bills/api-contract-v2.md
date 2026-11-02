# Ilevora API contract v2: invoice brand colours, 10 templates, preview

All routes need `Authorization: Bearer <jwt>`. JSON is camelCase. All additions are optional/nullable, so old clients keep working.

## Colour rules
- Format: exactly `#RRGGBB` (6 hex digits, leading `#`). Server accepts any case and returns UPPER case. Rejected with **422** (`detail`: "primaryColor must be a colour in the form #RRGGBB (for example #138A44)."): `red`, `#FFF`, `FFFFFF`, `#12345G`, `#11223344`, whitespace-only is treated as "not set".
- `null`/omitted/blank = "not set" (use the next level). There is no separate "reset" call: set to null.
- Resolution, per colour independently: invoice colour -> farm brand colour -> template default.
- Text on coloured areas is chosen automatically (black or white by luminance); very light colours used as lines/text on white are darkened automatically so the invoice stays legible. Clients do not need to do anything; just show a colour picker and the preview.
- "default" = nothing chosen, template default used (ngf: primary `#138A44`, accent `#F5C90C`, unchanged). "custom" = an invoice colour or a farm brand colour was chosen.

## Farm business profile
`GET /api/farms/{farmId}/business-profile` and `PUT /api/farms/{farmId}/business-profile` (PUT is Admin only, as before)

Response (and PUT body adds the two new optional fields):
```json
{ "address": "...", "phone": "...", "email": "...", "website": "...",
  "bankAccountName": null, "bankAccountNumber": null, "bankName": null,
  "brandPrimaryColor": "#B91C1C", "brandAccentColor": null }
```
PUT: `brandPrimaryColor` / `brandAccentColor` optional; null or blank clears; invalid -> 422. Existing PUT bodies without them clear nothing new (they just stay unset/cleared as part of the full replace like every other field).

## Invoices
`POST /api/farms/{farmId}/invoices`, `PUT /api/farms/{farmId}/invoices/{id}` (full replace), `GET .../invoices/{id}`
Request adds optional `primaryColor`, `accentColor` (`#RRGGBB` or null). PUT without them clears the invoice colours (falls back to farm brand colour/default).
Response (`InvoiceDto`) adds:
```json
{ "primaryColor": "#B91C1C", "accentColor": null,
  "resolvedPrimaryColor": "#B91C1C", "resolvedAccentColor": "#F5C90C",
  "primaryColorSource": "invoice", "accentColorSource": "default" }
```
- `primaryColor`/`accentColor`: what is stored on the invoice (null = inherit).
- `resolved*`: what the PDF will actually use now. `*Source`: `"invoice"` | `"farm"` | `"default"`.
- `GET .../invoices/{id}/pdf` renders with the resolved colours. Changing the farm brand colour changes the look of every invoice that has no colour of its own (the PDF is rendered on demand).
- `templateKey` unchanged: default `ngf`; unknown key -> 422 listing the valid keys. Invoices saved with any key keep rendering.
- Existing limits unchanged (rate < 1e8, qty < 1e6, line amount < 1e9, subtotal < 1e10, dates 2000..2100).

## Templates
`GET /api/farms/{farmId}/invoice-templates` -> stable order (ngf first, then by name):
```json
[{ "key": "ngf", "name": "NGF - Invoice", "description": "...", "isDefault": true,
   "defaultPrimaryColor": "#138A44", "defaultAccentColor": "#F5C90C",
   "previewUrl": "/api/farms/{farmId}/invoice-templates/ngf/preview" }, ...]
```
| key | name | format | default primary / accent |
|---|---|---|---|
| ngf (default) | NGF - Invoice | the original Nature's Gold layout, slanted table header, TOTAL band | #138A44 / #F5C90C |
| anchor | Anchor | airy page ending in a full-width colour totals bar | #065F46 / #A3E635 |
| angle | Angle | diagonal colour header with accent stripe | #DB2777 / #FBBF24 |
| band | Band | full-width colour header band, zebra rows | #4F46E5 / #F59E0B |
| callout | Callout | big "amount due" figure beside the customer, whitespace and type | #7C3AED / #FB7185 |
| cards | Cards | rounded tinted cards, pill table header, solid total card | #0D9488 / #FDBA74 |
| duo | Duo | two colour panels (customer+dates / amount due), zebra rows | #1D4ED8 / #F97316 |
| ledger | Ledger | compact centred receipt-style, dashed separators | #1F2937 / #9CA3AF |
| minimal | Minimal | oversized invoice number, hairlines, no decoration | #111827 / #EF4444 |
| sidebar | Sidebar | coloured side column with logo, total and payment details | #0F172A / #22D3EE |

Order returned: ngf, Anchor, Angle, Band, Callout, Cards, Duo, Ledger, Minimal, Sidebar. At most 10 templates can exist (the app refuses to start with more).

## Preview
`GET /api/farms/{farmId}/invoice-templates/{key}/preview?primaryColor=%23RRGGBB&accentColor=%23RRGGBB`
- 200 `application/pdf`, `Content-Disposition: inline`. Sample customer, 6 sample items, sample payment block (if the farm has no bank details) and terms; the farm's real logo, name and contacts. Nothing is saved and no invoice number is used.
- Colours optional: query colour -> farm brand colour -> template default. `#` must be URL-encoded as `%23`.
- Any user of the farm's company (Admin or Member). 401 no token; 404 farm of another company/unknown farm; 404 unknown template key (unlike create/update where it is 422); 422 invalid colour.
- Use it as `<iframe>`/`<object>` source or fetch as blob with the bearer token (an `<img>`/plain link cannot send the header).

## Errors (unchanged shape)
`{"type":..., "title":"Unprocessable Request","status":422,"detail":"...","traceId":"..."}`

## Migration
`20261101110452_InvoiceBrandColours`: nullable varchar(7) `Invoices.PrimaryColor/AccentColor`, `Farms.BrandPrimaryColor/BrandAccentColor`. Reversible; the API applies it on startup.
