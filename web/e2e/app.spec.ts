import { test, expect, type Page, type Route } from '@playwright/test'
import {
  FARM, SECTIONS, WORKERS, WORKER_ROLES,
  ACTIVITIES, TRANSACTIONS, INVENTORY, CHEMICALS, DASHBOARD, WEATHER
} from './fixtures'

// ── Mock all API endpoints ────────────────────────────────────────────────────
async function mockApi(page: Page) {
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
  await intercept('**/api/farms/farm_001/inventory',              INVENTORY)
  await intercept('**/api/chemicals',                             CHEMICALS)
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
