import { test, expect, type Page, type Route } from '@playwright/test'
import { FARM, DASHBOARD, WEATHER } from './fixtures'

// QA regression specs for confirmed defects in the farm-logo / sales-invoice / vendor-invoice web
// slice. Each test describes the CORRECT behaviour and currently FAILS against the implementation
// under review (see the QA report). They use the same mocked-API approach as app.spec.ts.

async function mockSession(page: Page, farmOverrides: Record<string, unknown> = {}) {
  await page.addInitScript(() => {
    localStorage.setItem('agro_token', 'test-token')
    localStorage.setItem('agro_user', JSON.stringify({ id: 'u1', email: 'test@example.com', fullName: 'Test User', companyId: 'c1', role: 'Admin' }))
    localStorage.setItem('agro_active_farm_id', 'farm_001')
  })
  const json = (pattern: string | RegExp, body: unknown, status = 200) =>
    page.route(pattern, (route: Route) => route.fulfill({ status, contentType: 'application/json', body: JSON.stringify(body) }))
  await json('**/api/farms/farm_001/dashboard', DASHBOARD)
  await json('**/api/farms/farm_001/weather', WEATHER)
  await json('**/api/farms/farm_001/features', [])
  await json('**/api/farms/farm_001/inventory-adjustments**', [])
  await json('**/api/farms/farm_001/activities**', { items: [], total: 0 })
  await json('**/api/farms/farm_001/transactions**', { items: [], total: 0 })
  await json('**/api/farms/farm_001/blocks', [])
  await json('**/api/farms', [{ id: FARM.id, name: FARM.name, setupStatus: 1, hasLogo: !!farmOverrides.hasLogo, logoUpdatedAt: farmOverrides.logoUpdatedAt ?? null }])
  await json('**/api/farms/farm_001', { ...FARM, ...farmOverrides })
}

test('QA-A11Y-1: closing the invoice delete dialog returns focus to the Delete button that opened it', async ({ page }) => {
  await mockSession(page)
  await page.route('**/api/farms/farm_001/invoices', route => route.fulfill({
    status: 200, contentType: 'application/json',
    body: JSON.stringify([{ id: 'i1', number: 1, reference: 'INV-0001', date: '2026-11-01', dueDate: '2026-11-15', billToName: 'Mama Put', total: 100, templateKey: 'ngf', createdAt: '2026-11-01T10:00:00Z' }]),
  }))
  await page.goto('/invoices')
  const del = page.getByRole('button', { name: 'Delete INV-0001' })
  await del.focus()
  await page.keyboard.press('Enter')
  await expect(page.getByRole('alertdialog')).toBeVisible()
  await page.keyboard.press('Escape')
  await expect(page.getByRole('alertdialog')).toHaveCount(0)
  // Expected: keyboard users land back where they were. Actual: focus falls to <body>.
  await expect(del).toBeFocused()
})

test('QA-TZ-1: a vendor invoice marked paid at 23:30 UTC shows the Lagos calendar day (1 Nov), not 31 Oct', async ({ browser }) => {
  const ctx = await browser.newContext({ timezoneId: 'Africa/Lagos' })
  const page = await ctx.newPage()
  await mockSession(page)
  await page.route('**/api/farms/farm_001/vendor-invoices**', route => route.fulfill({
    status: 200, contentType: 'application/json',
    body: JSON.stringify([{
      id: 'v1', farmId: 'farm_001', orderId: 'o1', orderReference: 'ORD-1001', orderTotal: 15000, orderIsPaid: false,
      supplierId: 's1', vendorName: 'Greenfield', invoiceNumber: 'SUP-1', amount: 15000, invoiceDate: '2026-10-01', dueDate: null,
      status: 'Paid', paidAt: '2026-10-31T23:30:00Z', isOverdue: false, notes: null, fileName: 'b.pdf', contentType: 'application/pdf', fileSize: 193,
      uploadedByUserId: 'u1', createdAt: '2026-10-01T10:00:00Z',
    }]),
  }))
  await page.goto('/vendor-invoices')
  // 23:30 UTC on 31 Oct is 00:30 on 1 Nov in Lagos (UTC+1). Actual: "Paid 31 Oct 2026" (the UTC date is read off the string).
  await expect(page.locator('.vi-paid-on')).toHaveText('Paid 1 Nov 2026')
  await ctx.close()
})

test('LOGO-SIDEBAR: the farm logo never replaces the AgroMesh logo in the sidebar (it is only used on invoices)', async ({ page }) => {
  await mockSession(page, { hasLogo: true, logoUpdatedAt: '2026-11-01T10:00:00Z' })
  let logoRequested = false
  await page.route('**/api/farms/farm_001/logo', route => { logoRequested = true; return route.fulfill({ status: 200, contentType: 'image/png', body: 'x' }) })
  await page.goto('/')
  await expect(page.locator('.sidebar-logo .logo-text')).toBeVisible()
  await expect(page.locator('.sidebar-logo img')).toHaveCount(0)
  expect(logoRequested).toBe(false) // the sidebar does not even fetch the farm logo
})
