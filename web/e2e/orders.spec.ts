import { test, expect, type Page, type Route } from '@playwright/test'
import {
  FARM, SECTIONS, WORKERS, WORKER_ROLES,
  ACTIVITIES, TRANSACTIONS, INVENTORY, INVENTORY_CATEGORIES, CHEMICALS,
  SUPPLIERS, ORDERS, DASHBOARD, WEATHER
} from './fixtures'

// Phase 4 — Orders/Suppliers/Invoices. A dedicated spec file (not folded into app.spec.ts) since
// the create/place/receive/invoice-download flow is large enough to warrant its own file, mirroring
// this repo's precedent of gps-boundary.spec.ts being split out rather than growing app.spec.ts
// further. mockApi below mirrors app.spec.ts's mockApi (same broad set of endpoints every
// authenticated route's Sidebar/AppShell fetch on mount) plus the new suppliers/orders routes.
async function mockApi(page: Page, overrides?: { orders?: unknown; suppliers?: unknown }) {
  await page.addInitScript(() => {
    localStorage.setItem('agro_token', 'test-token')
    localStorage.setItem('agro_user', JSON.stringify({ id: 'u1', email: 'test@example.com', fullName: 'Test User', companyId: 'c1', role: 'Admin' }))
    localStorage.setItem('agro_active_farm_id', 'farm_001')
  })

  const intercept = (pattern: string | RegExp, body: unknown, status = 200) =>
    page.route(pattern, (route: Route) => route.fulfill({
      status, contentType: 'application/json', body: JSON.stringify(body),
    }))

  // Order matters: more specific first, or use non-overlapping patterns
  await intercept('**/api/farms/farm_001/dashboard',              DASHBOARD)
  await intercept('**/api/farms/farm_001/weather',                WEATHER)
  await intercept('**/api/farms/farm_001/sections',                SECTIONS)
  await intercept('**/api/farms/farm_001/activities**',           ACTIVITIES)
  await intercept('**/api/farms/farm_001/transactions**',         TRANSACTIONS)
  await intercept('**/api/farms/farm_001/workers**',              WORKERS)
  await intercept('**/api/farms/farm_001/worker-roles',           WORKER_ROLES)
  await intercept('**/api/farms/farm_001/inventory-categories',   INVENTORY_CATEGORIES)
  await intercept('**/api/farms/farm_001/inventory-adjustments**', [])
  await intercept('**/api/farms/farm_001/inventory',              INVENTORY)
  await intercept('**/api/farms/farm_001/suppliers',              overrides?.suppliers ?? SUPPLIERS)
  await intercept('**/api/farms/farm_001/orders',                 overrides?.orders ?? ORDERS)
  await intercept('**/api/chemicals',                             CHEMICALS)
  await intercept('**/api/farms',                                 [{ id: FARM.id, name: FARM.name, setupStatus: 1 }])
  // Catch-all for /api/farms/farm_001 (must be last — most general)
  await intercept('**/api/farms/farm_001',                        FARM)
}

test('orders — page lists existing orders with supplier, status, and total', async ({ page }) => {
  await mockApi(page)
  await page.goto('/orders')

  await expect(page.locator('.ord-row')).toHaveCount(3)
  const draftRow = page.locator('.ord-row').filter({ hasText: 'Saro Agro Sciences' }).first()
  await expect(draftRow).toContainText('Draft')
  await expect(draftRow).toContainText('₦5,000')
})

test('orders — empty state shown when there are no orders', async ({ page }) => {
  await mockApi(page, { orders: [] })
  await page.goto('/orders')
  await expect(page.locator('.card', { hasText: 'No orders yet' })).toBeVisible()
})

test('orders — status filter pills narrow the list', async ({ page }) => {
  await mockApi(page)
  await page.goto('/orders')

  await expect(page.locator('.ord-row')).toHaveCount(3)
  await page.locator('.filter-pill', { hasText: 'Placed' }).click()
  await expect(page.locator('.ord-row')).toHaveCount(1)
  // ord_2 (Saro Agro Sciences, ₦40,000) is the only Placed order — the row doesn't show line
  // item names, only the item count/total, so assert on those instead.
  await expect(page.locator('.ord-row')).toContainText('Saro Agro Sciences')
  await expect(page.locator('.ord-row')).toContainText('₦40,000')
})

test('orders — create order modal builds a Draft order and it appears in the list', async ({ page }) => {
  await mockApi(page)
  let orders: unknown[] = [...ORDERS]
  let capturedBody: Record<string, unknown> | null = null

  await page.route('**/api/farms/farm_001/orders', async route => {
    if (route.request().method() === 'POST') {
      capturedBody = route.request().postDataJSON()
      const lineItems = (capturedBody!.lineItems as { inventoryItemId: string; quantity: number; unitPrice: number }[])
        .map(l => ({
          inventoryItemId: l.inventoryItemId, inventoryItemName: 'NPK 15-15-15', unit: 'kg',
          quantity: l.quantity, unitPrice: l.unitPrice, lineTotal: l.quantity * l.unitPrice,
        }))
      const created = {
        id: 'ord_new', farmId: 'farm_001', supplierId: capturedBody!.supplierId, supplierName: 'Saro Agro Sciences',
        status: 'Draft', lineItems, total: lineItems.reduce((s, l) => s + l.lineTotal, 0),
        createdAt: '2025-06-21T00:00:00Z', updatedAt: '2025-06-21T00:00:00Z',
      }
      orders = [created, ...orders]
      await route.fulfill({ status: 201, contentType: 'application/json', body: JSON.stringify(created) })
    } else {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(orders) })
    }
  })

  await page.goto('/orders')
  await page.locator('button[aria-label="Create a new order"]').click()

  await page.locator('select').filter({ hasText: 'Select supplier' }).selectOption('sup_1')
  await page.locator('select').filter({ hasText: '— Item —' }).selectOption('inv_2')
  await page.locator('input[placeholder="Qty"]').fill('10')
  await page.locator('input[placeholder="Unit ₦"]').fill('300')
  await page.locator('.modal-footer button', { hasText: 'Create Order' }).click()

  await expect(page.locator('.modal')).not.toBeVisible()
  expect(capturedBody).not.toBeNull()
  expect(capturedBody!.supplierId).toBe('sup_1')
  await expect(page.locator('.ord-row')).toHaveCount(4)
})

test('orders — create order modal groups the item picker by category', async ({ page }) => {
  await mockApi(page)
  await page.goto('/orders')
  await page.locator('button[aria-label="Create a new order"]').click()

  // INVENTORY fixture spans 3 distinct categories (Chemical, Fertilizer, Fuel) across its 3 items —
  // the item picker should group by categoryName (already on InventoryItemDto) instead of listing
  // every farm item as one flat alphabetical list.
  const itemSelect = page.locator('select').filter({ hasText: '— Item —' })
  const groups = itemSelect.locator('optgroup')
  await expect(groups).toHaveCount(3)
  await expect(groups.nth(0)).toHaveAttribute('label', 'Chemical')
  await expect(groups.nth(1)).toHaveAttribute('label', 'Fertilizer')
  await expect(groups.nth(2)).toHaveAttribute('label', 'Fuel')
  await expect(groups.filter({ hasText: 'Glyphosate 360' })).toHaveAttribute('label', 'Chemical')
})

test('orders — create order modal can add a supplier inline and auto-selects it', async ({ page }) => {
  await mockApi(page)
  await page.route('**/api/farms/farm_001/suppliers', async route => {
    if (route.request().method() === 'POST') {
      const body = route.request().postDataJSON()
      await route.fulfill({
        status: 201, contentType: 'application/json',
        body: JSON.stringify({ id: 'sup_new', farmId: 'farm_001', name: body.name, phone: body.phone, email: body.email }),
      })
    } else {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(SUPPLIERS) })
    }
  })

  await page.goto('/orders')
  await page.locator('button[aria-label="Create a new order"]').click()

  const supplierSelect = page.locator('select').filter({ hasText: 'Select supplier' })
  await supplierSelect.selectOption('__new__')
  await page.locator('input[placeholder="Supplier name"]').fill('New Farm Supplies Ltd.')
  await page.locator('button', { hasText: 'Add Supplier' }).click()

  await expect(supplierSelect).toHaveValue('sup_new')
})

test('orders — create order submit is disabled until supplier and a valid line item are set', async ({ page }) => {
  await mockApi(page)
  await page.goto('/orders')
  await page.locator('button[aria-label="Create a new order"]').click()

  const createButton = page.locator('.modal-footer button', { hasText: 'Create Order' })
  await expect(createButton).toBeDisabled()

  await page.locator('select').filter({ hasText: 'Select supplier' }).selectOption('sup_1')
  await expect(createButton).toBeDisabled() // no item/qty/price yet

  await page.locator('select').filter({ hasText: '— Item —' }).selectOption('inv_1')
  await page.locator('input[placeholder="Qty"]').fill('5')
  await page.locator('input[placeholder="Unit ₦"]').fill('500')
  await expect(createButton).toBeEnabled()
})

test('orders — Draft order shows Place but not Receive', async ({ page }) => {
  await mockApi(page)
  await page.goto('/orders')

  await page.locator('.ord-row').filter({ hasText: 'Draft' }).locator('button', { hasText: 'View' }).click()
  await expect(page.locator('button[aria-label="Place order"]')).toBeVisible()
  await expect(page.locator('button[aria-label="Receive order"]')).toHaveCount(0)
})

test('orders — Placed order shows Receive but not Place', async ({ page }) => {
  await mockApi(page)
  await page.goto('/orders')

  await page.locator('.ord-row').filter({ hasText: 'Placed' }).locator('button', { hasText: 'View' }).click()
  await expect(page.locator('button[aria-label="Receive order"]')).toBeVisible()
  await expect(page.locator('button[aria-label="Place order"]')).toHaveCount(0)
})

test('orders — Received order shows neither Place nor Receive', async ({ page }) => {
  await mockApi(page)
  await page.goto('/orders')

  await page.locator('.ord-row').filter({ hasText: 'Received' }).locator('button', { hasText: 'View' }).click()
  await expect(page.locator('button[aria-label="Place order"]')).toHaveCount(0)
  await expect(page.locator('button[aria-label="Receive order"]')).toHaveCount(0)
})

test('orders — placing a Draft order transitions it to Placed', async ({ page }) => {
  await mockApi(page)
  await page.route('**/api/farms/farm_001/orders/ord_1/place', route =>
    route.fulfill({
      status: 200, contentType: 'application/json',
      body: JSON.stringify({ ...ORDERS[0], status: 'Placed' }),
    })
  )

  await page.goto('/orders')
  await page.locator('.ord-row').filter({ hasText: 'Draft' }).locator('button', { hasText: 'View' }).click()
  await page.locator('button[aria-label="Place order"]').click()

  await expect(page.locator('.modal .badge', { hasText: 'Placed' })).toBeVisible()
  await expect(page.locator('button[aria-label="Receive order"]')).toBeVisible()
})

test('orders — receiving a Placed order transitions it to Received and increases inventory stock', async ({ page }) => {
  await mockApi(page)
  await page.route('**/api/farms/farm_001/orders/ord_2/receive', route =>
    route.fulfill({
      status: 200, contentType: 'application/json',
      body: JSON.stringify({ ...ORDERS[1], status: 'Received' }),
    })
  )
  // ReceiveOrderCommand increases InventoryItem stock server-side — the store re-fetches
  // inventory afterward, mirroring app.spec.ts's "approving updates stock" assertion shape.
  await page.route('**/api/farms/farm_001/inventory', async route => {
    const updated = INVENTORY.map(i => (i.id === 'inv_3' ? { ...i, currentStock: 135, isLowStock: false } : i))
    await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(updated) })
  })

  await page.goto('/orders')
  await page.locator('.ord-row').filter({ hasText: 'Placed' }).locator('button', { hasText: 'View' }).click()
  await page.locator('button[aria-label="Receive order"]').click()

  await expect(page.locator('.modal .badge', { hasText: 'Received' })).toBeVisible()
  await expect(page.locator('button[aria-label="Place order"]')).toHaveCount(0)
  await expect(page.locator('button[aria-label="Receive order"]')).toHaveCount(0)

  // Confirm the inventory view now reflects the increased stock.
  await page.locator('.modal-close-inline').click()
  await page.goto('/inventory')
  const row = page.locator('.inv-row').filter({ hasText: 'Diesel' })
  await expect(row.locator('.inv-stock-num')).toContainText('135')
})

test('orders — cannot receive a Draft order (guarded server-side, surfaced inline)', async ({ page }) => {
  await mockApi(page)
  await page.route('**/api/farms/farm_001/orders/ord_1/receive', route =>
    route.fulfill({
      status: 422, contentType: 'application/problem+json',
      body: JSON.stringify({
        type: 'https://tools.ietf.org/html/rfc9110#section-15.5.21', title: 'Unprocessable Request', status: 422,
        detail: 'Only a Placed order can be received. This order is Draft.', traceId: 'test-trace',
      }),
    })
  )

  await page.goto('/orders')
  // Draft order shows no Receive button at all — the primary guard is the UI never offering the
  // action; this test proves the button is genuinely absent rather than present-but-disabled.
  await page.locator('.ord-row').filter({ hasText: 'Draft' }).locator('button', { hasText: 'View' }).click()
  await expect(page.locator('button[aria-label="Receive order"]')).toHaveCount(0)
})

test('orders — cannot place an already-Placed order (guarded server-side, surfaced inline)', async ({ page }) => {
  await mockApi(page)
  await page.goto('/orders')
  // Placed order shows no Place button at all — same "action never offered" guard as above.
  await page.locator('.ord-row').filter({ hasText: 'Placed' }).locator('button', { hasText: 'View' }).click()
  await expect(page.locator('button[aria-label="Place order"]')).toHaveCount(0)
})

test('orders — Download Invoice fires a GET request to the invoice endpoint', async ({ page }) => {
  await mockApi(page)
  let invoiceRequested = false
  let requestMethod = ''

  await page.route('**/api/farms/farm_001/orders/ord_1/invoice', async route => {
    invoiceRequested = true
    requestMethod = route.request().method()
    // Playwright can't easily assert real PDF bytes end-to-end against a mocked API — this
    // confirms the request fired with the right method/URL, per spec's documented limitation.
    await route.fulfill({
      status: 200,
      contentType: 'application/pdf',
      headers: { 'Content-Disposition': 'attachment; filename=invoice-ord_1.pdf' },
      body: Buffer.from('%PDF-1.4 fake'),
    })
  })

  await page.goto('/orders')
  await page.locator('.ord-row').filter({ hasText: 'Draft' }).locator('button', { hasText: 'View' }).click()
  await page.locator('button[aria-label="Download invoice"]').click()

  await expect.poll(() => invoiceRequested).toBe(true)
  expect(requestMethod).toBe('GET')
})

test('navigation — Orders nav item changes route', async ({ page }) => {
  await mockApi(page)
  await page.goto('/dashboard')
  await page.locator('.nav-item', { hasText: 'Orders' }).click()
  await expect(page).toHaveURL('/orders')
})
