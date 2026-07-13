import { test, expect, type Page, type Route } from '@playwright/test'
import {
  FARM, SECTIONS, WORKERS, WORKER_ROLES,
  ACTIVITIES, TRANSACTIONS, INVENTORY, INVENTORY_CATEGORIES, STOCK_ADJUSTMENTS, CHEMICALS, DASHBOARD, WEATHER
} from './fixtures'

// ── Mock all API endpoints ────────────────────────────────────────────────────
async function mockApi(page: Page, overrides?: { inventory?: unknown; stockAdjustments?: unknown; role?: 'Admin' | 'Member' }) {
  // Seed an authenticated session before any app script runs. The router guard
  // (router/index.ts) redirects any non-public route to /landing unless store.isAuthenticated
  // is true, so every test in this file that navigates straight to an app route (dashboard,
  // sections, inventory, etc. — anything without meta.public) needs a session already in
  // localStorage. Mirrors the one place this is already done correctly:
  // gps-boundary.spec.ts's "Edit section" test.
  const role = overrides?.role ?? 'Admin'
  await page.addInitScript((r: string) => {
    localStorage.setItem('agro_token', 'test-token')
    localStorage.setItem('agro_user', JSON.stringify({ id: 'u1', email: 'test@example.com', fullName: 'Test User', companyId: 'c1', role: r }))
    localStorage.setItem('agro_active_farm_id', 'farm_001')
  }, role)

  const intercept = (pattern: string | RegExp, body: unknown, status = 200) =>
    page.route(pattern, (route: Route) => route.fulfill({
      status, contentType: 'application/json', body: JSON.stringify(body),
    }))

  // Order matters: more specific first, or use non-overlapping patterns
  await intercept('**/api/farms/farm_001/dashboard',              DASHBOARD)
  await intercept('**/api/farms/farm_001/weather',                WEATHER)
  await intercept('**/api/farms/farm_001/sections',               SECTIONS)
  await intercept('**/api/farms/farm_001/activities**',           ACTIVITIES)
  await intercept('**/api/farms/farm_001/transactions**',         TRANSACTIONS)
  await intercept('**/api/farms/farm_001/workers**',              WORKERS)
  await intercept('**/api/farms/farm_001/worker-roles',           WORKER_ROLES)
  await intercept('**/api/farms/farm_001/inventory-categories',   INVENTORY_CATEGORIES)
  await intercept('**/api/farms/farm_001/inventory-adjustments**', overrides?.stockAdjustments ?? STOCK_ADJUSTMENTS)
  await intercept('**/api/farms/farm_001/inventory',              overrides?.inventory ?? INVENTORY)
  await intercept('**/api/chemicals',                             CHEMICALS)
  // Sidebar fetches the bare farm list on mount for the farm switcher.
  await intercept('**/api/farms',                                 [{ id: FARM.id, name: FARM.name, setupStatus: 1 }])
  // Catch-all for /api/farms/farm_001 (must be last — most general)
  await intercept('**/api/farms/farm_001',                        FARM)
}

// ── App shell ─────────────────────────────────────────────────────────────────
test('app shell — sidebar logo visible', async ({ page }) => {
  await mockApi(page)
  await page.goto('/')
  // Use class selector to avoid strict mode on nested span text
  await expect(page.locator('.logo-text')).toBeVisible()
  await expect(page.locator('.logo-text')).toContainText('AgroMesh')
})

test('app shell — all nav items present', async ({ page }) => {
  await mockApi(page)
  await page.goto('/')
  const navItems = ['Dashboard', 'Activities', 'Sections', 'Finance', 'Workers', 'Inventory', 'Reports']
  for (const label of navItems) {
    await expect(page.locator('.nav-item', { hasText: label })).toBeVisible()
  }
})

test('sidebar — farm name and area shown', async ({ page }) => {
  await mockApi(page)
  await page.goto('/')
  await expect(page.locator('.farm-name')).toContainText('Kabiru Farms Ltd.')
  await expect(page.locator('.farm-sub')).toContainText('111.2')
})

// ── Dashboard ─────────────────────────────────────────────────────────────────
test('dashboard — total spend KPI renders', async ({ page }) => {
  await mockApi(page)
  await page.goto('/')
  // 1,540,000 / 1000 = 1,540 → "₦1,540K"
  await expect(page.locator('.kpi-grid')).toContainText('₦1,540K')
})

test('dashboard — activities this month KPI renders', async ({ page }) => {
  await mockApi(page)
  await page.goto('/')
  // activities KPI card value is "12"
  await expect(page.locator('.kpi-grid')).toContainText('12')
})

test('dashboard — weather widget shows season badge', async ({ page }) => {
  await mockApi(page)
  await page.goto('/')
  await expect(page.locator('.season-badge')).toBeVisible()
  await expect(page.locator('.season-badge')).toHaveText('Peak Rains')
})

test('dashboard — weather widget shows temperature', async ({ page }) => {
  await mockApi(page)
  await page.goto('/')
  await expect(page.locator('.weather-temp')).toContainText('32°C')
})

test('dashboard — weather widget shows rain stat', async ({ page }) => {
  await mockApi(page)
  await page.goto('/')
  await expect(page.locator('.weather-stats')).toContainText('4.2 mm')
})

test('dashboard — weather widget shows best spray window', async ({ page }) => {
  await mockApi(page)
  await page.goto('/')
  await expect(page.locator('.spray-window')).toBeVisible()
  await expect(page.locator('.spray-label')).toHaveText('BEST SPRAY WINDOW')
})

test('dashboard — upcoming tasks panel visible', async ({ page }) => {
  await mockApi(page)
  await page.goto('/')
  await expect(page.locator('.card-title', { hasText: 'Upcoming Tasks' })).toBeVisible()
})

test('dashboard — upcoming task shows section name', async ({ page }) => {
  await mockApi(page)
  await page.goto('/')
  // .task-section is specific to the tasks panel
  await expect(page.locator('.task-section').first()).toContainText('Section A')
})

test('dashboard — upcoming task shows activity type', async ({ page }) => {
  await mockApi(page)
  await page.goto('/')
  await expect(page.locator('.task-type').first()).toContainText('Weeding')
})

test('dashboard — upcoming task shows due-days badge', async ({ page }) => {
  await mockApi(page)
  await page.goto('/')
  await expect(page.locator('.task-due').first()).toContainText('in 5d')
})

test('dashboard — upcoming task shows Claude reasoning', async ({ page }) => {
  await mockApi(page)
  await page.goto('/')
  await expect(page.locator('.task-reasoning').first()).toContainText('Peak rains season')
})

test('dashboard — low stock alert banner shown', async ({ page }) => {
  await mockApi(page)
  await page.goto('/')
  // Banner text — store.lowStockCount comes from lowStockAlerts when inventory not loaded
  await expect(page.locator('.alert-banner').filter({ hasText: /below reorder threshold/i })).toBeVisible()
})

test('dashboard — pending review alert banner shown', async ({ page }) => {
  await mockApi(page)
  await page.goto('/')
  await expect(page.locator('.alert-banner').filter({ hasText: /transactions/i })).toBeVisible()
})

test('dashboard — recent activities section visible', async ({ page }) => {
  await mockApi(page)
  await page.goto('/')
  await expect(page.locator('.card-title', { hasText: 'Recent Activities' })).toBeVisible()
})

test('dashboard — recent activity rows shown', async ({ page }) => {
  await mockApi(page)
  await page.goto('/')
  // Act titles in recent activities rows
  await expect(page.locator('.act-title').first()).toBeVisible()
})

test('dashboard — section MTD card visible', async ({ page }) => {
  await mockApi(page)
  await page.goto('/')
  await expect(page.locator('.card-title', { hasText: 'Section Costs' })).toBeVisible()
})

// ── Sections ──────────────────────────────────────────────────────────────────
test('sections — cards render for each section', async ({ page }) => {
  await mockApi(page)
  await page.goto('/sections')
  await expect(page.locator('.section-card')).toHaveCount(2)
})

test('sections — Section A card visible', async ({ page }) => {
  await mockApi(page)
  await page.goto('/sections')
  await expect(page.locator('.section-card-name', { hasText: 'Section A' })).toBeVisible()
})

test('sections — multi-crop section D shows both crops', async ({ page }) => {
  await mockApi(page)
  await page.goto('/sections')
  const sectionDCard = page.locator('.section-card').filter({ hasText: 'Section D' })
  await expect(sectionDCard.locator('.crop-pill', { hasText: 'OilPalm' })).toBeVisible()
  await expect(sectionDCard.locator('.crop-pill', { hasText: 'Maize' })).toBeVisible()
})

test('sections — crop percentage shown', async ({ page }) => {
  await mockApi(page)
  await page.goto('/sections')
  const sectionDCard = page.locator('.section-card').filter({ hasText: 'Section D' })
  await expect(sectionDCard.locator('.crop-pill').first()).toContainText('50%')
})

test('sections — section area shown in preferred unit', async ({ page }) => {
  await mockApi(page)
  await page.goto('/sections')
  const sectionACard = page.locator('.section-card').filter({ hasText: 'Section A' })
  await expect(sectionACard).toContainText('44.5')
})

test('sections — Section A shows upcoming weeding prediction', async ({ page }) => {
  await mockApi(page)
  await page.goto('/sections')
  const sectionACard = page.locator('.section-card').filter({ hasText: 'Section A' })
  await expect(sectionACard).toContainText('in 5d')
})

test('sections — Active status badge shown on Section A', async ({ page }) => {
  await mockApi(page)
  await page.goto('/sections')
  const sectionACard = page.locator('.section-card').filter({ hasText: 'Section A' })
  await expect(sectionACard.locator('.badge')).toContainText('Active')
})

test('sections — block count shown', async ({ page }) => {
  await mockApi(page)
  await page.goto('/sections')
  const sectionACard = page.locator('.section-card').filter({ hasText: 'Section A' })
  // Section A has 2 blocks
  await expect(sectionACard.locator('.stat-val').filter({ hasText: '2' })).toBeVisible()
})

// ── Activities ────────────────────────────────────────────────────────────────
test('activities — All Activities card title shown', async ({ page }) => {
  await mockApi(page)
  await page.goto('/activities')
  await expect(page.locator('.card-title', { hasText: 'All Activities' })).toBeVisible()
})

test('activities — activity rows render', async ({ page }) => {
  await mockApi(page)
  await page.goto('/activities')
  await expect(page.locator('.activity-row')).toHaveCount(2)
})

test('activities — first activity is Weeding', async ({ page }) => {
  await mockApi(page)
  await page.goto('/activities')
  // Use .act-title class (not getByText) to avoid matching the dropdown option
  await expect(page.locator('.act-title').first()).toHaveText('Weeding')
})

test('activities — second activity is Spraying', async ({ page }) => {
  await mockApi(page)
  await page.goto('/activities')
  await expect(page.locator('.act-title').nth(1)).toHaveText('Spraying')
})

test('activities — labour + input cost shown', async ({ page }) => {
  await mockApi(page)
  await page.goto('/activities')
  // Activity 1: labourCost 28000 + inputCost 4800 = 32800
  await expect(page.locator('.act-cost').first()).toContainText('32,800')
})

test('activities — chemical product name shown for spray', async ({ page }) => {
  await mockApi(page)
  await page.goto('/activities')
  // Spraying activity (second row) has chemicalProductName
  await expect(page.locator('.act-detail').nth(1)).toContainText('Glyphosate 360')
})

test('activities — section name resolved from store', async ({ page }) => {
  await mockApi(page)
  await page.goto('/activities')
  // Section A resolves from sectionId: 'sec_a'
  await expect(page.locator('.act-detail').first()).toContainText('Section A')
})

test('activities — section filter dropdown has section options', async ({ page }) => {
  await mockApi(page)
  await page.goto('/activities')
  const select = page.locator('select').first()
  await expect(select).toBeVisible()
  // Options should include "Section A" (loaded from sections mock via ActivitiesView onMount)
  await select.selectOption('sec_a')
  // After filtering, only activities for sec_a should remain (both are sec_a in fixtures)
  await expect(page.locator('.activity-row')).toHaveCount(2)
})

// ── Finance ───────────────────────────────────────────────────────────────────
test('finance — transaction rows render', async ({ page }) => {
  await mockApi(page)
  await page.goto('/finance')
  await expect(page.locator('.txn-row')).toHaveCount(2)
})

test('finance — first transaction narration shown', async ({ page }) => {
  await mockApi(page)
  await page.goto('/finance')
  await expect(page.locator('.txn-name').first()).toHaveText('SARO AGRO NIG LTD')
})

test('finance — category badge shown on transactions', async ({ page }) => {
  await mockApi(page)
  await page.goto('/finance')
  // First transaction: Fertilizer category badge (in .txn-badges not in filter pills)
  await expect(page.locator('.txn-badges').first()).toContainText('Fertilizer')
})

test('finance — reviewed badge shown for reviewed transaction', async ({ page }) => {
  await mockApi(page)
  await page.goto('/finance')
  // Second transaction is reviewedByUser: true
  await expect(page.locator('.txn-badges').nth(1)).toContainText('Reviewed')
})

test('finance — transaction amount shown', async ({ page }) => {
  await mockApi(page)
  await page.goto('/finance')
  await expect(page.locator('.txn-amount').first()).toContainText('185,000')
})

test('finance — filter pill Fuel filters transactions', async ({ page }) => {
  await mockApi(page)
  await page.goto('/finance')
  await page.locator('.filter-pill', { hasText: 'Fuel' }).click()
  // Only CONOIL (Fuel) should remain
  await expect(page.locator('.txn-row')).toHaveCount(1)
  await expect(page.locator('.txn-name')).toHaveText('CONOIL PLC')
})

test('finance — Pending Review filter shows unreviewed only', async ({ page }) => {
  await mockApi(page)
  await page.goto('/finance')
  await page.locator('.filter-pill', { hasText: 'Pending Review' }).click()
  // SARO AGRO (not reviewed) should show, CONOIL (reviewed) should not
  await expect(page.locator('.txn-row')).toHaveCount(1)
  await expect(page.locator('.txn-name')).toHaveText('SARO AGRO NIG LTD')
})

// ── Workers ───────────────────────────────────────────────────────────────────
test('workers — worker rows render', async ({ page }) => {
  await mockApi(page)
  await page.goto('/workers')
  await expect(page.locator('.worker-row')).toHaveCount(2)
})

test('workers — worker name shown', async ({ page }) => {
  await mockApi(page)
  await page.goto('/workers')
  await expect(page.locator('.w-name').first()).toHaveText('Ibrahim Musa')
})

test('workers — worker role title shown', async ({ page }) => {
  await mockApi(page)
  await page.goto('/workers')
  await expect(page.locator('.w-role').first()).toHaveText('Field Supervisor')
})

test('workers — daily rate shown from role', async ({ page }) => {
  await mockApi(page)
  await page.goto('/workers')
  await expect(page.locator('.w-pay').first()).toContainText('₦3,500 / day')
})

test('workers — days logged this month shown', async ({ page }) => {
  await mockApi(page)
  await page.goto('/workers')
  await expect(page.locator('.w-days').first()).toContainText('22')
})

test('workers — wage bands card renders', async ({ page }) => {
  await mockApi(page)
  await page.goto('/workers')
  await expect(page.locator('.card-title', { hasText: 'Wage Bands' })).toBeVisible()
  await expect(page.locator('.role-row')).toHaveCount(3)
})

test('workers — role daily rate shown in wage bands', async ({ page }) => {
  await mockApi(page)
  await page.goto('/workers')
  await expect(page.locator('.role-rate').first()).toContainText('₦3,500 / day')
})

test('workers — KPI shows employee count', async ({ page }) => {
  await mockApi(page)
  await page.goto('/workers')
  // "2 Employees" badge in card header
  await expect(page.locator('.badge', { hasText: 'Employees' })).toBeVisible()
})

// ── Inventory ─────────────────────────────────────────────────────────────────
test('inventory — item rows render', async ({ page }) => {
  await mockApi(page)
  await page.goto('/inventory')
  await expect(page.locator('.inv-row')).toHaveCount(3)
})

test('inventory — item names shown', async ({ page }) => {
  await mockApi(page)
  await page.goto('/inventory')
  await expect(page.locator('.inv-name').nth(0)).toHaveText('Glyphosate 360')
  await expect(page.locator('.inv-name').nth(1)).toHaveText('NPK 15-15-15')
  await expect(page.locator('.inv-name').nth(2)).toHaveText('Diesel')
})

test('inventory — Diesel shows LOW STOCK badge', async ({ page }) => {
  await mockApi(page)
  await page.goto('/inventory')
  const dieselRow = page.locator('.inv-row').filter({ hasText: 'Diesel' })
  await expect(dieselRow.locator('.badge-warn')).toHaveText('LOW STOCK')
})

test('inventory — low stock alert banner shown', async ({ page }) => {
  await mockApi(page)
  await page.goto('/inventory')
  await expect(page.locator('.alert-banner').filter({ hasText: /below reorder threshold/i })).toBeVisible()
})

test('inventory — KPI shows total item count', async ({ page }) => {
  await mockApi(page)
  await page.goto('/inventory')
  await expect(page.locator('.kpi-grid')).toContainText('3')
})

test('inventory — category filter Chemical shows only chemicals', async ({ page }) => {
  await mockApi(page)
  await page.goto('/inventory')
  await page.locator('.filter-pill', { hasText: 'Chemical' }).click()
  // Only Glyphosate (Chemical) should remain
  await expect(page.locator('.inv-row')).toHaveCount(1)
  await expect(page.locator('.inv-name')).toHaveText('Glyphosate 360')
})

test('inventory — category filter Fuel shows only fuel', async ({ page }) => {
  await mockApi(page)
  await page.goto('/inventory')
  await page.locator('.filter-pill', { hasText: 'Fuel' }).click()
  await expect(page.locator('.inv-row')).toHaveCount(1)
  await expect(page.locator('.inv-name')).toHaveText('Diesel')
})

test('inventory — adjust button opens modal', async ({ page }) => {
  await mockApi(page)
  await page.goto('/inventory')
  await page.locator('button', { hasText: 'Adjust' }).first().click()
  await expect(page.locator('.modal')).toBeVisible()
  await expect(page.locator('.modal-title')).toContainText('Adjust Stock')
  await expect(page.locator('label', { hasText: 'New Quantity' })).toBeVisible()
})

test('inventory — adjust modal shows current stock', async ({ page }) => {
  await mockApi(page)
  await page.goto('/inventory')
  await page.locator('button', { hasText: 'Adjust' }).first().click()
  // Glyphosate current stock: 105
  await expect(page.locator('.modal-body')).toContainText('105')
})

test('inventory — adjust modal can be closed', async ({ page }) => {
  await mockApi(page)
  await page.goto('/inventory')
  await page.locator('button', { hasText: 'Adjust' }).first().click()
  await expect(page.locator('.modal')).toBeVisible()
  await page.locator('.modal button', { hasText: 'Cancel' }).click()
  await expect(page.locator('.modal')).not.toBeVisible()
})

// ── Inventory: dynamic categories + Add Item / Add Stock (Phase 1) ─────────────
test('inventory — category filter pills are sourced from fetched categories, not hardcoded', async ({ page }) => {
  await mockApi(page)
  await page.goto('/inventory')
  // "Farm Tools" is not in the old hardcoded filter-pill array — its presence proves the pills
  // now come from the fetched INVENTORY_CATEGORIES fixture instead.
  await expect(page.locator('.filter-pill', { hasText: 'Farm Tools' })).toBeVisible()
})

test('inventory — loading state shown while the inventory list is in flight', async ({ page }) => {
  await mockApi(page)
  await page.route('**/api/farms/farm_001/inventory', async route => {
    // Generous delay — the first navigation in a fresh page also pays for router/app bootstrap
    // (async router guard, Sidebar's farm fetch, etc.), so a short delay leaves too little
    // margin to reliably observe the loading state before it flips back to loaded.
    await new Promise(resolve => setTimeout(resolve, 1000))
    await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(INVENTORY) })
  })

  await page.goto('/inventory')
  await expect(page.locator('.loading-state')).toContainText('Loading inventory')
  await expect(page.locator('.inv-row')).toHaveCount(3)
})

test('inventory — empty state shown when there are no items', async ({ page }) => {
  await mockApi(page, { inventory: [] })
  await page.goto('/inventory')
  await expect(page.locator('.card', { hasText: 'No inventory items' })).toBeVisible()
})

test('inventory — Add Item button opens the Add Item modal', async ({ page }) => {
  await mockApi(page)
  await page.goto('/inventory')
  await page.locator('button[aria-label="Add inventory item or stock"]').click()
  await expect(page.locator('.modal')).toBeVisible()
  await expect(page.locator('.modal-title')).toContainText('Add Item')
})

test('inventory — add item modal creates a brand-new item and it appears in the list', async ({ page }) => {
  await mockApi(page)
  let items: unknown[] = [...INVENTORY]
  let capturedBody: Record<string, unknown> | null = null

  await page.route('**/api/farms/farm_001/inventory', async route => {
    if (route.request().method() === 'POST') {
      capturedBody = route.request().postDataJSON()
      const created = {
        id: 'inv_new', farmId: 'farm_001', name: capturedBody!.name, unit: capturedBody!.unit,
        categoryId: capturedBody!.categoryId, categoryName: 'Fertilizer', currentStock: capturedBody!.quantity,
        reorderThreshold: capturedBody!.reorderThreshold, isLowStock: false,
        chemicalProductId: null, chemicalProductName: null, lastMovementDate: null,
      }
      items = [...items, created]
      await route.fulfill({ status: 201, contentType: 'application/json', body: JSON.stringify(created) })
    } else {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(items) })
    }
  })

  await page.goto('/inventory')
  await page.locator('button[aria-label="Add inventory item or stock"]').click()

  await page.locator('input[placeholder="e.g. Glyphosate 360"]').fill('NPK 20-10-10')
  await page.locator('select').filter({ hasText: 'Select category' }).selectOption('cat_fertilizer')
  await page.locator('input[placeholder="e.g. L, kg, bags"]').fill('kg')
  await page.locator('input[placeholder="e.g. 50"]').fill('200')
  await page.locator('input[placeholder="e.g. 10"]').fill('50')
  await page.locator('.modal-footer button', { hasText: 'Add Item' }).click()

  await expect(page.locator('.modal')).not.toBeVisible()
  await expect(page.locator('.inv-row')).toHaveCount(4)
  await expect(page.locator('.inv-name', { hasText: 'NPK 20-10-10' })).toBeVisible()
  expect(capturedBody).not.toBeNull()
  expect(capturedBody!.name).toBe('NPK 20-10-10')
  expect(capturedBody!.existingItemId ?? null).toBeNull()
})

test('inventory — add item modal restocks an existing item via the item picker', async ({ page }) => {
  await mockApi(page)
  let stock = 105
  let capturedBody: Record<string, unknown> | null = null

  await page.route('**/api/farms/farm_001/inventory', async route => {
    if (route.request().method() === 'POST') {
      capturedBody = route.request().postDataJSON()
      stock += Number(capturedBody!.quantity)
      await route.fulfill({
        status: 200, contentType: 'application/json',
        body: JSON.stringify({
          id: 'inv_1', farmId: 'farm_001', name: 'Glyphosate 360', unit: 'L',
          categoryId: 'cat_chemical', categoryName: 'Chemical', currentStock: stock,
          reorderThreshold: 30, isLowStock: false, chemicalProductId: 'chem_1',
          chemicalProductName: 'Glyphosate 360', lastMovementDate: '2025-06-17T09:00:00Z',
        }),
      })
    } else {
      const items = INVENTORY.map(i => (i.id === 'inv_1' ? { ...i, currentStock: stock } : i))
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(items) })
    }
  })

  await page.goto('/inventory')
  await page.locator('button[aria-label="Add inventory item or stock"]').click()

  // Explicit item picker — the "by name/category match" path is covered by the next test.
  await page.locator('select').filter({ hasText: 'Create new item' }).selectOption('inv_1')
  await page.locator('input[placeholder="e.g. 50"]').fill('25')
  await page.locator('.modal-footer button', { hasText: 'Add Stock' }).click()

  await expect(page.locator('.modal')).not.toBeVisible()
  expect(capturedBody).not.toBeNull()
  expect(capturedBody!.existingItemId).toBe('inv_1')
  expect(capturedBody!.quantity).toBe(25)

  const row = page.locator('.inv-row').filter({ hasText: 'Glyphosate 360' })
  await expect(row.locator('.inv-stock-num')).toContainText('130')
})

test('inventory — add item modal can create a new category inline and auto-selects it', async ({ page }) => {
  await mockApi(page)
  await page.route('**/api/farms/farm_001/inventory-categories', async route => {
    if (route.request().method() === 'POST') {
      const body = route.request().postDataJSON()
      await route.fulfill({
        status: 201, contentType: 'application/json',
        body: JSON.stringify({ id: 'cat_new', farmId: 'farm_001', name: body.name, isDefault: false }),
      })
    } else {
      await route.fallback()
    }
  })

  await page.goto('/inventory')
  await page.locator('button[aria-label="Add inventory item or stock"]').click()

  const categorySelect = page.locator('select').filter({ hasText: 'Select category' })
  await categorySelect.selectOption('__new__')
  await page.locator('input[placeholder="New category name"]').fill('Irrigation')
  await page.locator('.modal button', { hasText: /^Add$/ }).click()

  // The new category becomes the selected value without the user re-opening the dropdown.
  await expect(categorySelect).toHaveValue('cat_new')
})

test('inventory — add item modal shows an inline error on a failed submit and stays open', async ({ page }) => {
  await mockApi(page)
  await page.route('**/api/farms/farm_001/inventory', async route => {
    if (route.request().method() === 'POST') {
      await route.fulfill({
        status: 422, contentType: 'application/problem+json',
        body: JSON.stringify({
          type: 'https://tools.ietf.org/html/rfc9110#section-15.5.21',
          title: 'Unprocessable Request', status: 422,
          detail: "'Glyphosate 360' is tracked in L, not kg. Enter the quantity in L.",
          traceId: 'test-trace',
        }),
      })
    } else {
      await route.fallback()
    }
  })

  await page.goto('/inventory')
  await page.locator('button[aria-label="Add inventory item or stock"]').click()

  await page.locator('input[placeholder="e.g. Glyphosate 360"]').fill('Glyphosate 360')
  await page.locator('select').filter({ hasText: 'Select category' }).selectOption('cat_chemical')
  await page.locator('input[placeholder="e.g. L, kg, bags"]').fill('kg')
  await page.locator('input[placeholder="e.g. 50"]').fill('5')
  await page.locator('input[placeholder="e.g. 10"]').fill('10')
  await page.locator('.modal-footer button', { hasText: 'Add Item' }).click()

  await expect(page.locator('.error-msg')).toContainText('tracked in L')
  await expect(page.locator('.modal')).toBeVisible()
})

// ── Inventory: Pick / Issue Stock (Phase 2) ─────────────────────────────────────
test('inventory — Pick Stock button opens the pick modal with worker options', async ({ page }) => {
  await mockApi(page)
  await page.goto('/inventory')
  await page.locator('button', { hasText: 'Pick Stock' }).first().click()
  await expect(page.locator('.modal')).toBeVisible()
  await expect(page.locator('.modal-title')).toContainText('Pick Stock')
  await expect(page.locator('.modal-title')).toContainText('Glyphosate 360')
  // Available stock shown read-only
  await expect(page.locator('.modal-body')).toContainText('105')
  // Worker select sourced from store.workers (existing Workers feature), not a new endpoint
  const workerSelect = page.locator('select').filter({ hasText: 'Select worker' })
  await expect(workerSelect).toContainText('Ibrahim Musa')
  await expect(workerSelect).toContainText('Tunde Adeyemi')
})

test('inventory — Pick Stock submit is disabled until quantity, worker, and purpose are all filled', async ({ page }) => {
  await mockApi(page)
  await page.goto('/inventory')
  await page.locator('button', { hasText: 'Pick Stock' }).first().click()

  const submit = page.locator('.modal-footer button', { hasText: 'Pick Stock' })
  await expect(submit).toBeDisabled()

  await page.locator('input[placeholder="e.g. Spraying Section A"]').fill('Spraying Section A')
  await expect(submit).toBeDisabled() // still missing quantity + worker

  await page.locator('.modal input[type="number"]').fill('10')
  await expect(submit).toBeDisabled() // still missing worker

  await page.locator('select').filter({ hasText: 'Select worker' }).selectOption('w1')
  await expect(submit).toBeEnabled()
})

test('inventory — a valid pick reduces stock, records who/quantity/purpose, and closes the modal', async ({ page }) => {
  await mockApi(page)
  let stock = 105
  let capturedBody: Record<string, unknown> | null = null

  await page.route('**/api/farms/farm_001/inventory/inv_1/pick', async route => {
    capturedBody = route.request().postDataJSON()
    stock -= Number(capturedBody!.quantity)
    await route.fulfill({
      status: 200, contentType: 'application/json',
      body: JSON.stringify({
        item: {
          id: 'inv_1', farmId: 'farm_001', name: 'Glyphosate 360', unit: 'L',
          categoryId: 'cat_chemical', categoryName: 'Chemical', currentStock: stock,
          reorderThreshold: 30, isLowStock: stock <= 30, chemicalProductId: 'chem_1',
          chemicalProductName: 'Glyphosate 360', lastMovementDate: '2025-06-19T00:00:00Z',
        },
        lowStockWarning: null,
      }),
    })
  })
  await page.route('**/api/farms/farm_001/inventory', async route => {
    if (route.request().method() === 'GET') {
      const items = INVENTORY.map(i => (i.id === 'inv_1' ? { ...i, currentStock: stock, isLowStock: stock <= 30 } : i))
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(items) })
    } else {
      await route.fallback()
    }
  })

  await page.goto('/inventory')
  await page.locator('button', { hasText: 'Pick Stock' }).first().click()
  await page.locator('.modal input[type="number"]').fill('20')
  await page.locator('select').filter({ hasText: 'Select worker' }).selectOption('w1')
  await page.locator('input[placeholder="e.g. Spraying Section A"]').fill('Spraying Section A')
  await page.locator('.modal-footer button', { hasText: 'Pick Stock' }).click()

  await expect(page.locator('.modal')).not.toBeVisible()
  expect(capturedBody).not.toBeNull()
  expect(capturedBody!.quantity).toBe(20)
  expect(capturedBody!.workerId).toBe('w1')
  expect(capturedBody!.purpose).toBe('Spraying Section A')

  const row = page.locator('.inv-row').filter({ hasText: 'Glyphosate 360' })
  await expect(row.locator('.inv-stock-num')).toContainText('85')
})

test('inventory — Pick Stock shows an inline error and stays open when quantity exceeds current stock', async ({ page }) => {
  await mockApi(page)
  await page.route('**/api/farms/farm_001/inventory/inv_1/pick', async route => {
    await route.fulfill({
      status: 422, contentType: 'application/problem+json',
      body: JSON.stringify({
        type: 'https://tools.ietf.org/html/rfc9110#section-15.5.21',
        title: 'Unprocessable Request', status: 422,
        detail: "Insufficient stock for 'Glyphosate 360'. Available: 105 L, requested: 500 L.",
        traceId: 'test-trace',
      }),
    })
  })

  await page.goto('/inventory')
  await page.locator('button', { hasText: 'Pick Stock' }).first().click()
  await page.locator('.modal input[type="number"]').fill('500')
  await page.locator('select').filter({ hasText: 'Select worker' }).selectOption('w1')
  await page.locator('input[placeholder="e.g. Spraying Section A"]').fill('Spraying Section A')
  await page.locator('.modal-footer button', { hasText: 'Pick Stock' }).click()

  await expect(page.locator('.error-msg')).toContainText('Insufficient stock')
  await expect(page.locator('.modal')).toBeVisible()
})

test('inventory — Pick Stock shows a low-stock warning (not a block) when stock crosses the threshold', async ({ page }) => {
  await mockApi(page)
  await page.route('**/api/farms/farm_001/inventory/inv_3/pick', async route => {
    await route.fulfill({
      status: 200, contentType: 'application/json',
      body: JSON.stringify({
        item: {
          id: 'inv_3', farmId: 'farm_001', name: 'Diesel', unit: 'L',
          categoryId: 'cat_fuel', categoryName: 'Fuel', currentStock: 70,
          reorderThreshold: 100, isLowStock: true, chemicalProductId: null,
          chemicalProductName: null, lastMovementDate: '2025-06-19T00:00:00Z',
        },
        lowStockWarning: 'Diesel is running low (70 L remaining — reorder threshold: 100)',
      }),
    })
  })
  await page.route('**/api/farms/farm_001/inventory', async route => {
    if (route.request().method() === 'GET') {
      const items = INVENTORY.map(i => (i.id === 'inv_3' ? { ...i, currentStock: 70 } : i))
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(items) })
    } else {
      await route.fallback()
    }
  })

  await page.goto('/inventory')
  const dieselRow = page.locator('.inv-row').filter({ hasText: 'Diesel' })
  await dieselRow.locator('button', { hasText: 'Pick Stock' }).click()
  await page.locator('.modal input[type="number"]').fill('15')
  await page.locator('select').filter({ hasText: 'Select worker' }).selectOption('w1')
  await page.locator('input[placeholder="e.g. Spraying Section A"]').fill('Generator top-up')
  await page.locator('.modal-footer button', { hasText: 'Pick Stock' }).click()

  // Distinct from the error case: a warning, the pick already succeeded, and the modal doesn't
  // auto-close — the user must explicitly dismiss it via "Done".
  await expect(page.locator('.warn-msg')).toContainText('running low')
  await expect(page.locator('.modal')).toBeVisible()
  await page.locator('.modal-footer button', { hasText: 'Done' }).click()
  await expect(page.locator('.modal')).not.toBeVisible()
})

// ── Inventory: Stock Adjustment Approval (Phase 3) ──────────────────────────────
test('inventory — submitting an adjustment does not change the displayed stock immediately and shows submitted-for-approval messaging', async ({ page }) => {
  await mockApi(page)
  let adjustCaptured: Record<string, unknown> | null = null
  await page.route('**/api/farms/farm_001/inventory/inv_1/adjust', async route => {
    adjustCaptured = route.request().postDataJSON()
    await route.fulfill({
      status: 201, contentType: 'application/json',
      body: JSON.stringify({
        id: 'adj_new', farmId: 'farm_001', inventoryItemId: 'inv_1', inventoryItemName: 'Glyphosate 360', unit: 'L',
        currentStock: 105, requestedQuantity: adjustCaptured!.newQuantity, status: 'Pending', notes: adjustCaptured!.notes,
        requestedByUserId: 'u1', reviewedByUserId: null, reviewedAt: null, reviewNotes: null,
        createdAt: '2025-06-20T00:00:00Z',
      }),
    })
  })

  await page.goto('/inventory')
  await page.locator('button', { hasText: 'Adjust' }).first().click()
  await page.locator('input[type="number"]').fill('90')
  await page.locator('input[placeholder*="recount"]').fill('Physical recount')
  await page.locator('.modal-footer button', { hasText: 'Submit for Approval' }).click()

  await expect(page.locator('.success-msg')).toContainText('submitted for approval')
  expect(adjustCaptured).not.toBeNull()
  expect(adjustCaptured!.newQuantity).toBe(90)

  // Modal stays open with the confirmation until dismissed (mirrors the Pick Stock warning UX).
  await page.locator('.modal-footer button', { hasText: 'Done' }).click()
  await expect(page.locator('.modal')).not.toBeVisible()

  // No optimistic/refetch-driven stock change — the backend never touched CurrentStock.
  const row = page.locator('.inv-row').filter({ hasText: 'Glyphosate 360' })
  await expect(row.locator('.inv-stock-num')).toContainText('105')
})

test('inventory — adjust modal shows an inline error and stays open on a failed submit', async ({ page }) => {
  await mockApi(page)
  await page.route('**/api/farms/farm_001/inventory/inv_1/adjust', async route => {
    await route.fulfill({
      status: 422, contentType: 'application/problem+json',
      body: JSON.stringify({
        type: 'https://tools.ietf.org/html/rfc9110#section-15.5.21',
        title: 'Unprocessable Request', status: 422,
        detail: 'A reason is required for a stock adjustment request.',
        traceId: 'test-trace',
      }),
    })
  })

  await page.goto('/inventory')
  await page.locator('button', { hasText: 'Adjust' }).first().click()
  await page.locator('input[type="number"]').fill('90')
  await page.locator('input[placeholder*="recount"]').fill('x')
  await page.locator('.modal-footer button', { hasText: 'Submit for Approval' }).click()

  await expect(page.locator('.error-msg')).toContainText('reason is required')
  await expect(page.locator('.modal')).toBeVisible()
})

test('inventory approvals — queue lists pending requests with item name, quantity, and requester notes', async ({ page }) => {
  await mockApi(page)
  await page.goto('/inventory-approvals')

  await expect(page.locator('.approval-row')).toHaveCount(1)
  const row = page.locator('.approval-row').first()
  await expect(row).toContainText('Glyphosate 360')
  await expect(row).toContainText('90')
  await expect(row).toContainText('Physical recount')
})

test('inventory approvals — empty state shown when there are no pending requests', async ({ page }) => {
  await mockApi(page, { stockAdjustments: [] })
  await page.goto('/inventory-approvals')
  await expect(page.locator('.card', { hasText: /no pending/i })).toBeVisible()
})

test('inventory approvals — approving updates stock and removes the request from the queue', async ({ page }) => {
  await mockApi(page)
  let approveCaptured: Record<string, unknown> | null = null
  let queue: unknown[] = [...STOCK_ADJUSTMENTS]
  // Single dispatching handler (mirrors the Pick Stock/Add Item tests' pattern) — a second,
  // broader route for the same base path would shadow the specific one and also intercept the
  // initial GET, which is exactly the bug this single-handler shape avoids.
  await page.route('**/api/farms/farm_001/inventory-adjustments**', async route => {
    const req = route.request()
    if (req.method() === 'PATCH' && req.url().includes('/adj_1/approve')) {
      approveCaptured = req.postDataJSON()
      queue = []
      await route.fulfill({
        status: 200, contentType: 'application/json',
        body: JSON.stringify({
          id: 'adj_1', farmId: 'farm_001', inventoryItemId: 'inv_1', inventoryItemName: 'Glyphosate 360', unit: 'L',
          currentStock: 90, requestedQuantity: 90, status: 'Approved', notes: 'Physical recount — 15L short',
          requestedByUserId: 'u1', reviewedByUserId: 'u1', reviewedAt: '2025-06-20T00:00:00Z', reviewNotes: null,
          createdAt: '2025-06-19T00:00:00Z',
        }),
      })
    } else {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(queue) })
    }
  })
  // Inventory re-fetch after approve reflects the new stock.
  await page.route('**/api/farms/farm_001/inventory', async route => {
    if (route.request().method() === 'GET') {
      const items = INVENTORY.map(i => (i.id === 'inv_1' ? { ...i, currentStock: 90 } : i))
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(items) })
    } else {
      await route.fallback()
    }
  })

  await page.goto('/inventory-approvals')
  await expect(page.locator('.approval-row')).toHaveCount(1)
  await page.locator('.approval-row button', { hasText: 'Approve' }).click()

  expect(approveCaptured).not.toBeNull()
  await expect(page.locator('.approval-row')).toHaveCount(0)

  await page.goto('/inventory')
  const row = page.locator('.inv-row').filter({ hasText: 'Glyphosate 360' })
  await expect(row.locator('.inv-stock-num')).toContainText('90')
})

test('inventory approvals — rejecting removes the request from the queue without changing stock', async ({ page }) => {
  await mockApi(page)
  let rejectCaptured: Record<string, unknown> | null = null
  let queue: unknown[] = [...STOCK_ADJUSTMENTS]
  await page.route('**/api/farms/farm_001/inventory-adjustments**', async route => {
    const req = route.request()
    if (req.method() === 'PATCH' && req.url().includes('/adj_1/reject')) {
      rejectCaptured = req.postDataJSON()
      queue = []
      await route.fulfill({
        status: 200, contentType: 'application/json',
        body: JSON.stringify({
          id: 'adj_1', farmId: 'farm_001', inventoryItemId: 'inv_1', inventoryItemName: 'Glyphosate 360', unit: 'L',
          currentStock: 105, requestedQuantity: 90, status: 'Rejected', notes: 'Physical recount — 15L short',
          requestedByUserId: 'u1', reviewedByUserId: 'u1', reviewedAt: '2025-06-20T00:00:00Z', reviewNotes: null,
          createdAt: '2025-06-19T00:00:00Z',
        }),
      })
    } else {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(queue) })
    }
  })

  await page.goto('/inventory-approvals')
  await page.locator('.approval-row button', { hasText: 'Reject' }).click()

  expect(rejectCaptured).not.toBeNull()
  await expect(page.locator('.approval-row')).toHaveCount(0)

  // Reject never touches inventory — the row's stock in the main Inventory view is unchanged.
  await page.goto('/inventory')
  const row = page.locator('.inv-row').filter({ hasText: 'Glyphosate 360' })
  await expect(row.locator('.inv-stock-num')).toContainText('105')
})

test('inventory approvals — non-admin session does not show approve/reject buttons', async ({ page }) => {
  await mockApi(page, { role: 'Member' })
  await page.goto('/inventory-approvals')
  await expect(page.locator('.approval-row')).toHaveCount(1)
  await expect(page.locator('.approval-row button', { hasText: 'Approve' })).toHaveCount(0)
  await expect(page.locator('.approval-row button', { hasText: 'Reject' })).toHaveCount(0)
})

test('sidebar — pending-adjustments badge reflects queue size', async ({ page }) => {
  await mockApi(page)
  await page.goto('/')
  await expect(page.locator('.nav-item', { hasText: 'Approvals' }).locator('.nav-badge')).toHaveText('1')
})

test('sidebar — pending-adjustments badge hidden when queue is empty', async ({ page }) => {
  await mockApi(page, { stockAdjustments: [] })
  await page.goto('/')
  await expect(page.locator('.nav-item', { hasText: 'Approvals' }).locator('.nav-badge')).toHaveCount(0)
})

// ── Log Activity Modal ────────────────────────────────────────────────────────
test('log activity modal — opens from topbar', async ({ page }) => {
  await mockApi(page)
  await page.goto('/')
  await page.locator('button', { hasText: /Log Activity/ }).click()
  await expect(page.locator('.modal')).toBeVisible()
  await expect(page.locator('.modal-title')).toContainText('Log Activity')
})

test('log activity modal — step 1 shows date and section fields', async ({ page }) => {
  await mockApi(page)
  await page.goto('/')
  await page.locator('button', { hasText: /Log Activity/ }).click()
  await expect(page.locator('input[type="date"]')).toBeVisible()
  await expect(page.locator('select').filter({ hasText: 'Choose section' })).toBeVisible()
})

test('log activity modal — selecting section shows area hint', async ({ page }) => {
  await mockApi(page)
  await page.goto('/')
  await page.locator('button', { hasText: /Log Activity/ }).click()
  await page.locator('select').filter({ hasText: 'Choose section' }).selectOption('sec_a')
  // Hint shows area info
  await expect(page.locator('.form-hint')).toBeVisible()
  await expect(page.locator('.form-hint')).toContainText('44.5')
})

test('log activity modal — selecting section enables block dropdown', async ({ page }) => {
  await mockApi(page)
  await page.goto('/')
  await page.locator('button', { hasText: /Log Activity/ }).click()
  const blockSelect = page.locator('select').filter({ hasText: 'Choose block' })
  await expect(blockSelect).toBeDisabled()
  await page.locator('select').filter({ hasText: 'Choose section' }).selectOption('sec_a')
  await expect(blockSelect).not.toBeDisabled()
})

test('log activity modal — block dropdown has Section A blocks', async ({ page }) => {
  await mockApi(page)
  await page.goto('/')
  await page.locator('button', { hasText: /Log Activity/ }).click()
  await page.locator('select').filter({ hasText: 'Choose section' }).selectOption('sec_a')
  const blockSelect = page.locator('select').filter({ hasText: 'Choose block' })
  await blockSelect.selectOption('blk_a1')
  await expect(blockSelect).toHaveValue('blk_a1')
})

test('log activity modal — proceeds to step 2 with location filled', async ({ page }) => {
  await mockApi(page)
  await page.goto('/')
  await page.locator('button', { hasText: /Log Activity/ }).click()
  await page.locator('select').filter({ hasText: 'Choose section' }).selectOption('sec_a')
  await page.locator('select').filter({ hasText: 'Choose block' }).selectOption('blk_a1')
  await page.locator('button', { hasText: 'Continue' }).click()
  // Step 2 — activity grid
  await expect(page.locator('.activity-grid')).toBeVisible()
  await expect(page.locator('.activity-btn', { hasText: 'Weeding' })).toBeVisible()
  await expect(page.locator('.activity-btn', { hasText: 'Spraying' })).toBeVisible()
})

test('log activity modal — Spraying shows chemical dropdown', async ({ page }) => {
  await mockApi(page)
  await page.goto('/')
  await page.locator('button', { hasText: /Log Activity/ }).click()
  await page.locator('select').filter({ hasText: 'Choose section' }).selectOption('sec_a')
  await page.locator('select').filter({ hasText: 'Choose block' }).selectOption('blk_a1')
  await page.locator('button', { hasText: 'Continue' }).click()
  await page.locator('.activity-btn', { hasText: 'Spraying' }).click()
  await expect(page.locator('label', { hasText: 'Chemical Product' })).toBeVisible()
  await expect(page.locator('select').filter({ hasText: 'None / not tracked' })).toBeVisible()
})

test('log activity modal — chemical dropdown contains chemicals from API', async ({ page }) => {
  await mockApi(page)
  await page.goto('/')
  await page.locator('button', { hasText: /Log Activity/ }).click()
  await page.locator('select').filter({ hasText: 'Choose section' }).selectOption('sec_a')
  await page.locator('select').filter({ hasText: 'Choose block' }).selectOption('blk_a1')
  await page.locator('button', { hasText: 'Continue' }).click()
  await page.locator('.activity-btn', { hasText: 'Spraying' }).click()
  const chemSelect = page.locator('select').filter({ hasText: 'None / not tracked' })
  await chemSelect.selectOption('chem_1')
  await expect(chemSelect).toHaveValue('chem_1')
})

test('log activity modal — Weeding does NOT show chemical dropdown', async ({ page }) => {
  await mockApi(page)
  await page.goto('/')
  await page.locator('button', { hasText: /Log Activity/ }).click()
  await page.locator('select').filter({ hasText: 'Choose section' }).selectOption('sec_a')
  await page.locator('select').filter({ hasText: 'Choose block' }).selectOption('blk_a1')
  await page.locator('button', { hasText: 'Continue' }).click()
  await page.locator('.activity-btn', { hasText: 'Weeding' }).click()
  await expect(page.locator('label', { hasText: 'Chemical Product' })).not.toBeVisible()
})

test('log activity modal — step 3 shows worker picker', async ({ page }) => {
  await mockApi(page)
  await page.goto('/')
  await page.locator('button', { hasText: /Log Activity/ }).click()
  await page.locator('select').filter({ hasText: 'Choose section' }).selectOption('sec_a')
  await page.locator('select').filter({ hasText: 'Choose block' }).selectOption('blk_a1')
  await page.locator('button', { hasText: 'Continue' }).click()
  await page.locator('.activity-btn', { hasText: 'Weeding' }).click()
  await page.locator('button', { hasText: 'Continue' }).click()
  await expect(page.locator('select').filter({ hasText: /Pick worker/ })).toBeVisible()
})

test('log activity modal — worker picker shows rates', async ({ page }) => {
  await mockApi(page)
  await page.goto('/')
  await page.locator('button', { hasText: /Log Activity/ }).click()
  await page.locator('select').filter({ hasText: 'Choose section' }).selectOption('sec_a')
  await page.locator('select').filter({ hasText: 'Choose block' }).selectOption('blk_a1')
  await page.locator('button', { hasText: 'Continue' }).click()
  await page.locator('.activity-btn', { hasText: 'Weeding' }).click()
  await page.locator('button', { hasText: 'Continue' }).click()
  // Dropdown should show worker with rate
  await expect(page.locator('select').filter({ hasText: /Pick worker/ })).toContainText('₦3,500')
})

test('log activity modal — cancel closes modal', async ({ page }) => {
  await mockApi(page)
  await page.goto('/')
  await page.locator('button', { hasText: /Log Activity/ }).click()
  await expect(page.locator('.modal')).toBeVisible()
  await page.locator('.modal button', { hasText: 'Cancel' }).click()
  await expect(page.locator('.modal')).not.toBeVisible()
})

test('log activity modal — step indicator shows 1/3', async ({ page }) => {
  await mockApi(page)
  await page.goto('/')
  await page.locator('button', { hasText: /Log Activity/ }).click()
  await expect(page.locator('.modal-footer')).toContainText('1 / 3')
})

// ── Navigation ────────────────────────────────────────────────────────────────
test('navigation — activities nav item changes route', async ({ page }) => {
  await mockApi(page)
  await page.goto('/')
  await page.locator('.nav-item', { hasText: 'Activities' }).click()
  await expect(page).toHaveURL('/activities')
})

test('navigation — inventory nav item changes route', async ({ page }) => {
  await mockApi(page)
  await page.goto('/')
  await page.locator('.nav-item', { hasText: 'Inventory' }).click()
  await expect(page).toHaveURL('/inventory')
})

test('navigation — active nav item is highlighted', async ({ page }) => {
  await mockApi(page)
  await page.goto('/sections')
  await expect(page.locator('.nav-item', { hasText: 'Sections' })).toHaveClass(/active/)
})

test('navigation — back navigation from activities to dashboard', async ({ page }) => {
  await mockApi(page)
  await page.goto('/activities')
  await page.locator('.nav-item', { hasText: 'Dashboard' }).click()
  await expect(page).toHaveURL('/')
})

// ── Reports ───────────────────────────────────────────────────────────────────
test('reports — monthly summary card visible', async ({ page }) => {
  await mockApi(page)
  await page.goto('/reports')
  await expect(page.locator('.card-title', { hasText: 'Monthly Summary' })).toBeVisible()
})

test('reports — 6 month spend trend card visible', async ({ page }) => {
  await mockApi(page)
  await page.goto('/reports')
  await expect(page.locator('.card-title', { hasText: '6-Month Spend Trend' })).toBeVisible()
})

test('reports — cost per hectare shows section names', async ({ page }) => {
  await mockApi(page)
  await page.goto('/reports')
  await expect(page.locator('.card-title', { hasText: 'Cost per Hectare' })).toBeVisible()
  await expect(page.locator('.report-label', { hasText: 'Section A' })).toBeVisible()
})

test('reports — cost by category card visible', async ({ page }) => {
  await mockApi(page)
  await page.goto('/reports')
  await expect(page.locator('.card-title', { hasText: 'Cost by Category' })).toBeVisible()
  await expect(page.locator('.report-label', { hasText: /^Labour$/ })).toBeVisible()
})
