# Ilevora Brand Strategy and Identity Guide

Status: draft v1 for owner review (2026-11-01). Nothing in this guide has been applied to code, assets or store listings. Items marked **VERIFY** are assumptions or unchecked facts, not confirmed.

Companion files: `rename-checklist.md` (every place "AgroMesh" appears), `logo-sketches/` (SVG concept sketches).

---

## 1. Positioning

### What the product is today (from the code and docs)
Ilevora (currently AgroMesh) is a farm operating system: web dashboard for owners and managers, Flutter app for field supervisors, a .NET API behind both. Evidence in the repo:

- Landing copy: "Track every naira spent on your farm", "Purpose-built for Nigerian agriculture", Nigerian seasons, daily wage rates in naira (`web/src/views/LandingView.vue`).
- Bank sync via Mono, AI categorisation of transactions (Labour, Chemicals, Fuel), reconciliation against orders.
- Blocks with GPS/polygon boundaries, satellite readings, work-coverage tracking, 16-day weather.
- Inventory with approval flows, suppliers and orders, vendor bills, 10 invoice templates with farm branding.
- Workers and role-based daily rates; offline activity logging on mobile with sync.

### Target customers
| Segment | Who | What they care about | Priority |
|---|---|---|---|
| Primary | Owner-operators and managers of commercial farms, 20 to 500 ha, tree crops, vegetables, grains (Nigeria first) | Knowing real cost and profit per block; stopping leakage in inputs and labour; proof for banks and buyers | Launch |
| Primary | Absentee and diaspora owners, "weekend farmers" with a hired manager | Trust and visibility without being on site | Launch |
| Secondary | Field supervisors | Fast, offline-friendly logging in a few taps | Adoption driver, not the buyer |
| Secondary | Agribusiness outgrower schemes, cooperatives, farm-management firms | Many farms in one view | Later |
| Later | Ghana, Cote d'Ivoire, Senegal, Kenya | Currency, language, regulatory fit | Phase 2 (**VERIFY** demand) |

The buyer is the owner or manager; the daily user is often the supervisor. Brand must reassure the first and be effortless for the second.

### Value proposition
**For farm owners and managers who run real money through their land, Ilevora is the farm operating system that shows what every block earns and costs, in naira, from the field to the invoice.**

Three proof pillars (use these everywhere, in this order):
1. **Know your numbers.** Bank transactions auto-sorted to Labour, Chemicals, Fuel and allocated to crops, so profit per block is not a spreadsheet job.
2. **See your land.** Mapped blocks, satellite and weather, and a record of who did what, where, and when (works offline in the field).
3. **Run it properly.** Stock with approvals, orders and suppliers, workers and pay rates, and invoices that carry the farm's own name and colours.

### Competitor angles and how to differentiate
Competitor claims below are general market knowledge and must be re-checked before use in any public material (**VERIFY**).

| Alternative | Their angle | Weakness for our buyer | Our answer |
|---|---|---|---|
| Notebooks, WhatsApp, Excel | Free, familiar | No audit trail, errors, theft hides in the gaps, nothing reconciles to the bank | "Same simplicity, but it checks itself against your bank." |
| Global farm software (e.g. Farmbrite, Granular, FarmLogs lineage, Cropio-type satellite tools) | Feature depth, agronomy, large-scale mechanised farming | Priced in USD, built for different crops, currencies and rainfall seasons, weak offline, no Nigerian bank link | "Built for how Nigerian farms actually spend and pay." |
| African agritech (marketplaces, input credit, advisory, e.g. Farmcrowdy/Thrive Agric-style, Hello Tractor, FarmDrive-type) | Access to market, credit, equipment | Mostly sell to the farmer or fund the farmer; few give the owner a management system | "We are the record that makes you bankable, not another middleman." |
| Generic accounting tools | Books and tax | Do not know blocks, crops, labour days or stock | "Accounting that understands a field." |

**Differentiators to own:** (1) naira and Nigerian bank reality (Mono sync, narration matching); (2) block-level profit; (3) field-first offline mobile; (4) farm-branded invoices that make the owner look professional; (5) plain-language AI that explains, not just predicts.

**Positioning statement:** *For commercial farm owners and managers in West Africa, Ilevora is the farm operating system that turns every naira and every day of work into a clear picture of what each block earns. Unlike spreadsheets or foreign farm software, it is built around Nigerian banks, seasons and field reality.*

**Brand pillars:** Clarity, Ground truth, Growth, Dignity of the farmer.

---

## 2. Name story

### What Ilevora can evoke
"Ilevora" is a coined word, so meaning is something we give it. Possible threads (the owner should confirm which, if any, was intended):

- **Ilẹ̀ (Yoruba: land, ground, earth).** The opening "Ile" can be heard as a quiet nod to land and home (*ilé* means home/house in Yoruba). This is a plausible, authentic root for a Nigerian audience. **VERIFY** with Yoruba speakers that tone and meaning land well, and that no unfortunate reading exists in Hausa, Igbo, Pidgin, French, Portuguese.
- **"-vora" / "evor-"** carries rhythm of *flora*, *evolve*, *forever*, *vora* (Latin root, devouring, so avoid leaning on it). Keep the story to: land that evolves, growth that lasts.
- Sounds like a modern, premium, West African brand: four syllables, vowel-rich, easy to say in English, Yoruba, Hausa and French.

Suggested story line (draft): *"Ilevora starts from the land. Ilẹ̀ is where every farm begins; 'evora' is where it goes: evolving, thriving, lasting. We help farms grow from ground to good business."*

### Pronunciation
Recommended standard: **ih-LEV-or-ah** (/ɪˈlɛvɔːrə/). Expect **ee-leh-VOR-ah** from Yoruba speakers; accept both and do not correct customers. Put a phonetic spelling on the website About page and in the first sales deck. Decide one official version before launch and use it in any voice/audio ad.

### Tagline options
1. **From ground to growth.** (Land plus progress; works with the name story.)
2. **Know your farm. Grow your farm.** (Plain, benefit-led, translates well.)
3. **Every naira. Every block.** (Sharply specific to the money angle; Nigeria-first.)
4. **Run the farm. Own the numbers.** (Owner-confident, slightly bolder.)
5. **Your land, in order.** (Calm, short, suits the manager persona.)

Recommendation: lead with #2 as the public tagline (reads in one pass, no jargon) and keep #3 as a campaign line for finance-focused ads.

### Trademark, domain and handle risks (flag, do not assume available)
No availability check has been done. Treat every item as **VERIFY before spending on branding**.

| Area | What to check | Notes |
|---|---|---|
| Trademark, Nigeria | Search the Trademarks Registry (Federal Ministry of Industry, Trade and Investment) for "Ilevora", "Elevora", "Ilevera", "Elevara", phonetic variants | Classes to cover: 9 (software/apps), 42 (SaaS), 35 (business management services), 36 (financial/bookkeeping tools) if relevant, 31/44 only if adjacent |
| Trademark, regional and international | WIPO Global Brand Database; EUIPO/USPTO for look-alikes; check OAPI (francophone West Africa) and ARIPO membership for the target countries | Madrid Protocol filing can extend a Nigerian base mark. Ask an IP attorney which route applies |
| Product name registration | Trademark filing for the product name (see rows above); CAC business-name search only if the legal entity will trade under it | Name is a product brand, not a company; reserve early, reservations lapse |
| Existing meanings | "Evora/Évora" is a well-known Portuguese city and a surname; "Elevora" style names are common in SaaS | Likelihood-of-confusion risk is highest with near-spellings in software and agri |
| Domains | ilevora.com, ilevora.ng, ilevora.com.ng, ilevora.africa, ilevora.io, ilevora.app | CLAUDE.md/README currently reference agromesh.io. Get .com and .ng at minimum; add common typo domains |
| Social handles | Instagram, X, LinkedIn, Facebook, TikTok, YouTube, WhatsApp Business: aim for one consistent handle (e.g. @ilevora or @ilevoraapp) | Check before announcing |
| App stores | Google Play and Apple App Store name collision and package id; Play rejects `com.example.*` ids, so a real id is needed anyway (see rename checklist) | Suggested id: `com.ilevora.app` (**VERIFY** domain ownership first, reverse-DNS should match a domain you own) |
| Email and brand risk | Lookalike phishing domains once invoices go out under the brand | Register the main typos; set SPF/DKIM/DMARC on the sending domain |

---

## 3. Voice and tone

### Voice in four words
**Clear. Grounded. Respectful. Confident.**

- **Clear:** short sentences, plain words, numbers over adjectives. Our users read between field visits and meetings.
- **Grounded:** talk about blocks, labour, inputs, harvest, naira. Real farm vocabulary, no startup buzzwords.
- **Respectful:** the farmer is the expert; we are the tool. Never condescending, never "empowering smallholders" fluff.
- **Confident:** state what the product does. No hedging, no hype.

### Tone shifts by moment
| Moment | Tone | Example |
|---|---|---|
| Marketing / landing | Warm, assured | "Know what every block earns." |
| Onboarding | Encouraging, step by step | "Draw your first block. Ten minutes is enough." |
| Daily use | Quiet, efficient | "Logged. 3 workers, Block 4." |
| Errors | Calm, specific, tells next step | "We could not reach your bank. Your data is safe; try again in a few minutes." |
| Finance / money | Exact and neutral | "₦184,500 spent on chemicals this month." |
| Warnings / approvals | Direct, no blame | "This stock change needs a manager's approval." |

### Do and don't
| Do | Don't |
|---|---|
| "Profit by block, updated as you work." | "Leverage AI-driven synergies to optimise your agri-value chain." |
| "Ilevora" (capital I, one word) | "ILEVORA" in running text, "Ile Vora", "iLevora", "Ilevora App" as the name |
| "Your farm's invoices carry your logo." | "Rebrand your invoices with our powerful white-label suite." |
| Use ₦ and Nigerian formats (₦3,500, 12 Nov 2026) | Use $ or US date format by default |
| "Something went wrong saving. Try again." | "Oops! Our bad! 😬" (jokey errors on money screens) |
| "Worker", "supervisor", "block" (the product's own terms) | Mixing "section", "plot", "field", "zone" for the same thing |
| Say what AI did: "Sorted automatically, check it." | Pretend AI is infallible, or hide that it guessed |
| Short Nigerian English, plain; Pidgin only in clearly informal social posts | Slang or Pidgin in invoices, errors, legal or finance copy |

Writing rules: sentence case for headings and buttons; verbs first on buttons ("Add worker", "Send invoice"); no exclamation marks in product UI; avoid emoji in product copy; spell out numbers over a few words once only in marketing.

---

## 4. Visual identity

### Principles
Rooted, precise, bright. A deep green for trust and land, a sun-yellow used sparingly as the "harvest" signal, generous white space, data shown clearly.

### Colour: what is weak today and what changes
Current brand colours exist in three inconsistent sets: web/mobile tokens (Forest `#0D2B1F`, Canopy `#1A4A32`, Leaf `#2D7A4F`, Palm `#3DAA6A`, Harvest `#E8A838`, in `web/src/assets/main.css` and `mobile/lib/theme/app_theme.dart`), the invoice defaults (`#138A44` green, `#F5C90C` yellow), and the old README (Syne/Inter, now obsolete).

Findings (contrast ratios computed against WCAG 2.x):
- `#138A44` with white text is **4.42:1, just below the 4.5:1 AA threshold for normal text.** It is fine for large text, icons and graphics, and for invoice bands, but not for small button labels.
- `#F5C90C` on white is **1.59:1.** Yellow must never be text or a thin line on a light background; use it as a fill with dark text on top (9.58:1 with Forest).
- Two different yellows (`#E8A838` in the app, `#F5C90C` on invoices) dilute recognition. Standardise on `#F5C90C`.
- Two near-identical greens (`#138A44` heritage and the app's `#2D7A4F`/`#3DAA6A`) do not need to coexist as primaries.

**Decision proposed:** keep `#138A44` and `#F5C90C` as the heritage pair (they are already the invoice default and the ngf template), but add an accessible UI primary and a clear usage rule.

### Palette (strict #RRGGBB)

**Core**
| Token | Hex | Role |
|---|---|---|
| Ilevora Green | `#0F7A3A` | Primary UI colour: buttons, links, active states (white text 5.43:1) |
| Brand Green (heritage) | `#138A44` | Logo, large graphics, invoice default band (large text/graphics only; not small text) |
| Forest | `#0D2B1F` | Text, dark surfaces, sidebar, logo wordmark (15.19:1 on white) |
| Harvest | `#F5C90C` | Accent fill, highlights, logo dot; always with Forest text on it (9.58:1) |

**Support**
| Token | Hex | Role |
|---|---|---|
| Deep Green | `#0B5D2C` | Hover/pressed for primary, headings on tint (white 8.03:1) |
| Canopy | `#1A4A32` | Dark card surfaces (existing) |
| Sprout | `#8FD9A8` | Highlights on Forest surfaces (9.16:1 on Forest) |
| Mist | `#F0F4F1` | App background (existing) |
| Fog | `#E2EBE5` | Borders and dividers (existing) |
| Stone | `#4F5F56` | Secondary text (6.77:1 on white, 6.09:1 on Mist) |
| Stone on dark | `#A9BDB0` | Secondary text on Forest (7.66:1) |
| White | `#FFFFFF` | Surfaces |

**Status** (keep semantic, never rely on colour alone)
| Token | Hex | Notes |
|---|---|---|
| Alert | `#B42318` | Errors; replaces `#DC3C3C` for text (6.57:1 on white). `#DC3C3C` may stay for fills/icons |
| Warning text | `#8A5A00` on `#FFF4CC` | 5.38:1 |
| Success | `#0F7A3A` | Same as primary |
| Info | `#1D4ED8` | 6.70:1 on white |

**Approved text/background pairs**
| Foreground | Background | Ratio | Use |
|---|---|---|---|
| `#0D2B1F` | `#FFFFFF` | 15.19 | Body text |
| `#0D2B1F` | `#F0F4F1` | 13.68 | Body text on app background |
| `#4F5F56` | `#FFFFFF` | 6.77 | Secondary text |
| `#4F5F56` | `#F0F4F1` | 6.09 | Secondary text on Mist |
| `#FFFFFF` | `#0F7A3A` | 5.43 | Primary button |
| `#FFFFFF` | `#0B5D2C` | 8.03 | Hover button, strong bands |
| `#0F7A3A` | `#FFFFFF` | 5.43 | Links on white |
| `#0F7A3A` | `#F0F4F1` | 4.89 | Links on Mist (passes, tight) |
| `#0D2B1F` | `#F5C90C` | 9.58 | Text on yellow |
| `#F5C90C` | `#0D2B1F` | 9.58 | Yellow accent on Forest |
| `#FFFFFF` | `#0D2B1F` | 15.19 | Text on Forest |
| `#A9BDB0` | `#0D2B1F` | 7.66 | Muted text on Forest |
| `#8FD9A8` | `#0D2B1F` | 9.16 | Highlights on Forest |

**Banned pairs:** `#F5C90C` on white or Mist as text; `#138A44` as small text on white (4.42) or on Mist (4.09); `#3DAA6A` as text on light backgrounds (below AA).

Data visualisation keeps the existing colour-blind-safe Okabe-Ito set (`#0072B2`, `#E69F00`, and the section palette in `app_theme.dart`); do not recolour maps with brand greens.

Invoice rule (unchanged, from CLAUDE.md): brand colours resolve invoice, then farm, then template default (`#138A44` and `#F5C90C`), strict `#RRGGBB`. This guide does not alter that; the Ilevora palette governs the app and marketing, the farm's colours govern its invoices.

### Typography
Plus Jakarta Sans everywhere (web via fontsource, mobile bundled, API static weights for PDFs). No change.

| Level | Weight | Size (web) | Notes |
|---|---|---|---|
| Display / hero | 800 | 40 to 56 | Tight tracking (-1%), sentence case |
| H1 | 700 | 28 to 32 | |
| H2 | 700 | 22 | |
| H3 | 600 | 18 | |
| Body | 400 to 500 | 15 to 16 | Line height 1.5 |
| Caption / label | 600 | 12 to 13 | Never below 12 |
| Numbers / money | 700, tabular figures | match context | Enable `font-variant-numeric: tabular-nums` for tables |

The wordmark is set in Plus Jakarta Sans 800, lowercase or title case "Ilevora" (title case recommended), with optical kerning adjusted when the vector is drawn. Never typeset the wordmark live in running UI; use the logo asset.

### Logo concept directions
Current mark: a rounded green square with a four-node grid joined to a yellow centre dot (a "mesh"). That idea belongs to the old name and does not carry to Ilevora, so a new mark is recommended. SVG sketches are in `logo-sketches/` (rough, for direction only, not production artwork).

**Direction A: "Furrow I" (recommended).** A bold capital I built as a rounded vertical stem that rises out of three horizontal furrow lines, with a Harvest-yellow dot above it (sun, seed, and the dot of an "i" in one). Reads at 16 px, works as an app icon, ties to land and growth, and ownable as the letter. Sketch: `logo-sketches/ilevora-mark-a-furrow-i.svg`.

**Direction B: "Rising rows".** Three curved field rows that open upward into a leaf or horizon, in Brand Green with one Harvest row. More illustrative and warmer; riskier at small sizes and more generic. Sketch: `logo-sketches/ilevora-mark-b-rising-rows.svg`.

**Direction C: "Land tile".** A square plot with an inner rotated square (a field seen from above) and a yellow corner, nodding to blocks and mapped boundaries. Product-true (the app is built around blocks) and geometric; closest to the old grid mark so easiest to transition. Not sketched; can be drawn if chosen.

Logo system (to produce once a direction is chosen): primary horizontal lockup (mark plus wordmark), stacked lockup, icon-only (app icon, favicon, sidebar), single-colour Forest and single-colour white versions.

- **Clear space:** at least the height of the "I" stem on all sides.
- **Minimum size:** icon 16 px (favicon) / 24 px in app chrome; horizontal lockup 96 px wide on screen, 25 mm in print.
- **Backgrounds:** full-colour on white, Mist, Forest; white version on Brand Green or photography with enough darkness.
- **Do not:** recolour outside the palette, stretch, rotate, add shadows or outlines, place on busy photos without a scrim, redraw the mark, or place the yellow dot on a light background with low contrast (use Forest dot version).

---

## 5. Logo usage rules (including the farm logo rule)

These come from the product rules in CLAUDE.md and are binding.

1. **The Ilevora logo is the only logo in the app chrome.** Web sidebar, top bar, login/landing, onboarding, mobile header and login screen, empty states and loaders always show Ilevora.
2. **The farm's own logo appears ONLY on invoices (PDF) and in its preview in the farm settings screen.** It must never replace the Ilevora logo in the app chrome, in notifications, in emails sent by the product, or on any marketing surface.
3. Where a farm logo cannot load, the fallback is the Ilevora mark (the existing `FarmLogo.vue` behaviour), never a blank or a name-initial badge.
4. The farm name in text (for example the dashboard hero greeting) is allowed; its logo image is not.
5. Invoices carry the farm's brand colours and logo. **Decided:** a small "Powered by Ilevora" footer on invoices (tiny, Stone grey), removable on paid plans.
6. Co-branding on partner material: Ilevora logo and partner logo get equal clear space and equal visual weight, separated by a thin divider.
7. Never use a farm's logo in Ilevora marketing or case studies without written consent.

---

## 6. Rename checklist (AgroMesh to Ilevora)

The full file-by-file inventory with risk notes is in **`docs/brand/rename-checklist.md`**. Summary of the scale: about 2,980 occurrences across about 520 files. Roughly 95 percent are C# namespaces and `using` lines in `api/` that users never see. The user-facing surface is small (about 25 files) and can be done first.

Highest risks (details in the checklist):
- Android/iOS application id `com.example.mobile` change makes a new app for the stores (existing installs will not upgrade); Play rejects `com.example.*` anyway.
- Mobile local storage keys (`agromesh_token`, `agromesh.db`) are tied to existing installs; renaming without migration logs people out and can orphan unsynced offline data.
- JWT issuer `AgroMesh` change invalidates all issued tokens.
- Render service rename changes the `agromesh-api.onrender.com` URL that shipped APKs hard-code.
- Docker volume/db names and `agromesh_*` containers: renaming risks the local database; CLAUDE.md forbids recreating them.
- EF Core migrations reference the `AgroMesh.*` namespaces and `AgroMeshDbContext`; renaming the C# namespace needs care and is optional.

Recommended order: (1) user-visible strings and logo, (2) web/mobile display names, (3) domain and URL migration with redirects, (4) store identity, (5) internals last or never.

---

## 7. Launch checklist for the brand (next steps)

1. Owner confirms name meaning, pronunciation and tagline.
2. Run the trademark, CAC, domain, handle and store checks in section 2 before any spend.
3. Choose logo direction; commission final vector artwork (mark, lockups, favicon, app icon, single-colour versions).
4. Confirm palette adoption (Ilevora Green `#0F7A3A` as UI primary).
5. Build a small asset kit: logo files, palette tokens, type scale, one-page voice guide, deck and social templates.
6. Execute the rename in the order above, with domain redirects ready before the API URL changes.

## 8. Open questions for the owner
1. Is the name derived from a specific meaning (Yoruba *ilẹ̀*, or something else), and is the owner happy with the pronunciation proposed?
2. Which tagline, and does launch start Nigeria-only before expanding across West Africa (affects language and currency messaging)?
3. Is `#0F7A3A` accepted as the UI primary while `#138A44` stays as the heritage and invoice default?
4. ~~Logo direction~~ **Decided:** Direction B, "Rising rows" (`logo-sketches/ilevora-mark-b-rising-rows.svg`). Note: it is weaker at small sizes, so the favicon and app icon need a simplified single-row version.
5. ~~Invoice footer~~ **Decided:** invoices carry a "Powered by Ilevora" footer, removable on paid plans.
6. ~~Primary domain and store accounts~~ **Decided:** `ilevora.com`. VisionaryByte Solutions owns the Google Play and Apple Developer accounts.
7. ~~Existing users and APKs~~ **Decided:** not a concern, so a clean breaking rename is fine and no old-URL or storage-key compatibility layer is needed.
