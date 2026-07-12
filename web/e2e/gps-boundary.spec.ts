import { test, expect } from '@playwright/test'

// Two genuinely distinct physical locations (~15km apart) — proves the GPS corner-capture
// flow stores an independent coordinate pair per tap rather than reusing/caching a stale
// fix (the reported bug: "different physical locations return the same latitude and
// longitude"). Uses Playwright's real browser Geolocation API (not a JS-level stub), so
// this exercises the exact `navigator.geolocation.getCurrentPosition` path the app calls.
const LOCATION_A = { latitude: 7.3775, longitude: 3.9470 } // Ibadan, Oyo State
const LOCATION_B = { latitude: 7.4675, longitude: 4.0470 }

const SECTION_RESPONSE = {
  id: 'sec_new', farmId: 'farm_001', name: 'GPS Test Plot',
  crops: [{ cropType: 'OilPalm', area: { hectares: 10, acres: 24.7, displayValue: '10.00', displayUnit: 'ha' }, percentage: 100 }],
  area: { hectares: 10, acres: 24.7, displayValue: '10.00', displayUnit: 'ha' },
  status: 'Active', lastActivityDate: null, boundaryGeoJson: null, centroidLat: null, centroidLng: null,
  blocks: [], nextWeedingPrediction: null, nextSprayingPrediction: null,
}

test('GPS boundary capture — two different locations store two different coordinates', async ({ page, context }) => {
  await context.grantPermissions(['geolocation'])
  await context.setGeolocation(LOCATION_A)

  await page.route('**/api/farms/farm_001/sections', route =>
    route.fulfill({ status: 201, contentType: 'application/json', body: JSON.stringify(SECTION_RESPONSE) })
  )

  await page.goto('/sections-setup')

  await page.locator('input[placeholder*="North Plot"]').fill('GPS Test Plot')
  await page.locator('button', { hasText: '+ Add this plot' }).click()
  await expect(page.locator('.work-card-title')).toContainText('Mark the boundary')

  // First corner at location A.
  await page.locator('button', { hasText: 'Mark Corner Here' }).click()
  await expect(page.locator('.corner-list-row')).toHaveCount(1)
  const firstCoords = await page.locator('.corner-list-row').nth(0).locator('.corner-list-coords').textContent()

  // Move to a genuinely different physical location, mark a second corner.
  await context.setGeolocation(LOCATION_B)
  await page.locator('button', { hasText: 'Mark Corner Here' }).click()
  await expect(page.locator('.corner-list-row')).toHaveCount(2)
  const secondCoords = await page.locator('.corner-list-row').nth(1).locator('.corner-list-coords').textContent()

  // The actual confirmation requested: two distinct physical locations must produce two
  // distinct stored coordinate pairs, not the same value repeated.
  expect(firstCoords).toContain('7.37750')
  expect(secondCoords).toContain('7.46750')
  expect(firstCoords).not.toEqual(secondCoords)

  // No stale-fix warning should fire for two points this far apart.
  await expect(page.locator('.gps-warning')).toHaveCount(0)
})

test('GPS boundary capture — near-identical consecutive points surface a stale-fix warning', async ({ page, context }) => {
  await context.grantPermissions(['geolocation'])
  await context.setGeolocation(LOCATION_A)

  await page.route('**/api/farms/farm_001/sections', route =>
    route.fulfill({ status: 201, contentType: 'application/json', body: JSON.stringify(SECTION_RESPONSE) })
  )

  await page.goto('/sections-setup')
  await page.locator('input[placeholder*="North Plot"]').fill('GPS Test Plot')
  await page.locator('button', { hasText: '+ Add this plot' }).click()

  await page.locator('button', { hasText: 'Mark Corner Here' }).click()
  await expect(page.locator('.corner-list-row')).toHaveCount(1)

  // Same location again — simulates a cached/stale fix being returned for a genuinely
  // different tap, which is exactly the failure mode reported.
  await page.locator('button', { hasText: 'Mark Corner Here' }).click()
  await expect(page.locator('.corner-list-row')).toHaveCount(2)

  await expect(page.locator('.gps-warning')).toBeVisible()
  await expect(page.locator('.gps-warning')).toContainText('almost exactly where the last one was')
})
