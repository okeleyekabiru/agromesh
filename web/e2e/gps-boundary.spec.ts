import { test, expect, type Page } from '@playwright/test'
import { FARM, WORKER_ROLES, WORKERS, ACTIVITIES, TRANSACTIONS, INVENTORY, CHEMICALS, DASHBOARD, WEATHER } from './fixtures'

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
  status: 'Active', lastActivityDate: null, boundaryGeoJson: null, boundarySource: null,
  centroidLat: null, centroidLng: null,
  blocks: [], nextWeedingPrediction: null, nextSprayingPrediction: null,
}

// A simple, non-self-intersecting square — walking order matches the perimeter.
const SQUARE_CORNERS = [
  { latitude: 7.3775, longitude: 3.9470 },
  { latitude: 7.3775, longitude: 3.9560 },
  { latitude: 7.3865, longitude: 3.9560 },
  { latitude: 7.3865, longitude: 3.9470 },
]

async function addPlotAndReachBoundaryStep(page: Page, name = 'GPS Test Plot') {
  await page.goto('/sections-setup')
  await page.locator('input[placeholder*="North Plot"]').fill(name)
  await page.locator('button', { hasText: '+ Add this plot' }).click()
  await expect(page.locator('.work-card-title')).toContainText('Mark the boundary')
}

async function tapCorners(page: Page, context: import('@playwright/test').BrowserContext, points: { latitude: number; longitude: number }[]) {
  for (const point of points) {
    await context.setGeolocation(point)
    await page.locator('button', { hasText: 'Mark Corner Here' }).click()
  }
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

test('GPS boundary capture — Finish boundary stays disabled until 4 corners are marked', async ({ page, context }) => {
  await context.grantPermissions(['geolocation'])
  await context.setGeolocation(LOCATION_A)

  await page.route('**/api/farms/farm_001/sections', route =>
    route.fulfill({ status: 201, contentType: 'application/json', body: JSON.stringify(SECTION_RESPONSE) })
  )

  await page.goto('/sections-setup')
  await page.locator('input[placeholder*="North Plot"]').fill('GPS Test Plot')
  await page.locator('button', { hasText: '+ Add this plot' }).click()

  const finishButton = page.locator('button', { hasText: 'Finish boundary' })

  await tapCorners(page, context, SQUARE_CORNERS.slice(0, 3))
  await expect(page.locator('.corner-list-row')).toHaveCount(3)
  await expect(finishButton).toBeDisabled()
  await expect(page.locator('.hint-txt')).toContainText('Add at least 4 points')

  await tapCorners(page, context, SQUARE_CORNERS.slice(3, 4))
  await expect(page.locator('.corner-list-row')).toHaveCount(4)
  await expect(finishButton).toBeEnabled()
})

test('GPS boundary capture — finishing a valid 4-corner boundary saves and adds the plot', async ({ page, context }) => {
  await context.grantPermissions(['geolocation'])

  const BOUNDARY_RESULT = {
    sectionId: 'sec_new', operationId: 'op-test', submissionType: 0,
    computedGeoJson: '{"type":"Polygon","coordinates":[[[3.9470,7.3775],[3.9560,7.3775],[3.9560,7.3865],[3.9470,7.3865],[3.9470,7.3775]]]}',
    computedHectares: 99.21, computedCentroidLat: 7.382, computedCentroidLng: 3.9515,
  }
  const SECTION_WITH_BOUNDARY = {
    ...SECTION_RESPONSE,
    boundaryGeoJson: BOUNDARY_RESULT.computedGeoJson, boundarySource: 0,
    area: { hectares: 99.21, acres: 245.2, displayValue: '99.21', displayUnit: 'ha' },
  }

  await page.route('**/api/farms/farm_001/sections', route => {
    if (route.request().method() === 'GET') {
      return route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify([SECTION_WITH_BOUNDARY]) })
    }
    return route.fulfill({ status: 201, contentType: 'application/json', body: JSON.stringify(SECTION_RESPONSE) })
  })
  await page.route('**/api/sections/sec_new/boundary/points', route =>
    route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(BOUNDARY_RESULT) })
  )

  await addPlotAndReachBoundaryStep(page)
  await tapCorners(page, context, SQUARE_CORNERS)
  await page.locator('button', { hasText: 'Finish boundary' }).click()

  // Back to a fresh "New plot" form, and the finished plot now shows in the left panel.
  await expect(page.locator('.work-card-title')).toContainText('New plot')
  await expect(page.locator('.section-item-name')).toContainText('GPS Test Plot')
  await expect(page.locator('.section-item-meta')).toContainText('99.21')
  await expect(page.locator('.boundary-src-badge')).toContainText('GPS capture')
})

test('GPS boundary capture — self-intersecting corners require confirmation before saving', async ({ page, context }) => {
  await context.grantPermissions(['geolocation'])

  // Same four corners as SQUARE_CORNERS but walked out of perimeter order (middle two
  // swapped) — traces a self-intersecting "bowtie" instead of the plot's real boundary.
  const BOWTIE_CORNERS = [SQUARE_CORNERS[0], SQUARE_CORNERS[2], SQUARE_CORNERS[1], SQUARE_CORNERS[3]]

  await page.route('**/api/farms/farm_001/sections', route => {
    if (route.request().method() === 'GET') {
      return route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify([SECTION_RESPONSE]) })
    }
    return route.fulfill({ status: 201, contentType: 'application/json', body: JSON.stringify(SECTION_RESPONSE) })
  })
  await page.route('**/api/sections/sec_new/boundary/points', route => {
    const body = route.request().postDataJSON()
    if (!body.confirmImplausibleGeometry) {
      return route.fulfill({
        status: 422, contentType: 'application/problem+json',
        body: JSON.stringify({ detail: 'This boundary\'s computed area (0.0015 ha) is far smaller than the spread of your points suggests — the corners may have been captured out of walking order, producing a self-intersecting shape. Confirm to proceed anyway.' }),
      })
    }
    return route.fulfill({
      status: 200, contentType: 'application/json',
      body: JSON.stringify({ sectionId: 'sec_new', operationId: 'op-test', submissionType: 0, computedGeoJson: '{}', computedHectares: 0.0015, computedCentroidLat: 7.382, computedCentroidLng: 3.9515 }),
    })
  })

  await addPlotAndReachBoundaryStep(page)
  await tapCorners(page, context, BOWTIE_CORNERS)
  await page.locator('button', { hasText: 'Finish boundary' }).click()

  await expect(page.locator('.distance-confirm-msg')).toContainText('self-intersecting shape')
  await page.locator('button', { hasText: 'Yes, these are correct' }).click()

  await expect(page.locator('.work-card-title')).toContainText('New plot')
})

test('Edit section — pre-fills existing corners and allows re-marking the boundary', async ({ page, context }) => {
  await context.grantPermissions(['geolocation'])
  await context.setGeolocation(LOCATION_A)

  const EXISTING_SECTION = {
    id: 'sec_existing', farmId: 'farm_001', name: 'Old Plot',
    crops: [{ cropType: 'OilPalm', area: { hectares: 5, acres: 12.4, displayValue: '5.00', displayUnit: 'ha' }, percentage: 100 }],
    area: { hectares: 5, acres: 12.4, displayValue: '5.00', displayUnit: 'ha' },
    status: 'Active', lastActivityDate: null,
    boundaryGeoJson: '{"type":"Polygon","coordinates":[[[3.9470,7.3775],[3.9560,7.3775],[3.9560,7.3865],[3.9470,7.3865],[3.9470,7.3775]]]}',
    boundarySource: 0, centroidLat: 7.382, centroidLng: 3.9515,
    blocks: [], nextWeedingPrediction: null, nextSprayingPrediction: null,
  }

  // Seed an authenticated session — /sections requires auth, unlike the public /sections-setup.
  await page.addInitScript(() => {
    localStorage.setItem('agro_token', 'test-token')
    localStorage.setItem('agro_user', JSON.stringify({ id: 'u1', email: 'test@example.com', fullName: 'Test User', companyId: 'c1' }))
    localStorage.setItem('agro_active_farm_id', 'farm_001')
  })

  // AppShell/Sidebar fetch several other endpoints on mount for any authenticated route —
  // mock the same broad set app.spec.ts already relies on so nothing falls through to the
  // real (unauthenticated-for-this-fake-token) backend and 401-reload-loops.
  const intercept = (pattern: string, body: unknown) =>
    page.route(pattern, route => route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(body) }))
  await intercept('**/api/farms/farm_001/dashboard', DASHBOARD)
  await intercept('**/api/farms/farm_001/weather', WEATHER)
  await intercept('**/api/farms/farm_001/activities**', ACTIVITIES)
  await intercept('**/api/farms/farm_001/transactions**', TRANSACTIONS)
  await intercept('**/api/farms/farm_001/workers**', WORKERS)
  await intercept('**/api/farms/farm_001/worker-roles', WORKER_ROLES)
  await intercept('**/api/farms/farm_001/inventory', INVENTORY)
  // Sidebar now self-fetches the pending-adjustments count on mount for every authenticated
  // route (Phase 3 nav badge) — mock it here too, same reasoning as the rest of this list.
  await intercept('**/api/farms/farm_001/inventory-adjustments**', [])
  await intercept('**/api/chemicals', CHEMICALS)
  await intercept('**/api/farms', [FARM])
  await page.route('**/api/farms/farm_001/sections', route =>
    route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify([EXISTING_SECTION]) })
  )
  await page.route('**/api/farms/farm_001/sections/sec_existing', route =>
    route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ ...EXISTING_SECTION, name: 'Renamed Plot' }) })
  )
  // Catch-all for the plain /api/farms/farm_001 fetch — must be registered last (most general).
  await intercept('**/api/farms/farm_001', FARM)

  await page.goto('/sections')
  await expect(page.locator('.section-card-name')).toContainText('Old Plot')

  await page.locator('.btn-edit').click()
  await expect(page.locator('.work-card-title')).toContainText('Edit — Old Plot')
  await expect(page.locator('input[placeholder*="North Plot"]')).toHaveValue('Old Plot')

  await page.locator('input[placeholder*="North Plot"]').fill('Renamed Plot')
  await page.locator('button', { hasText: 'Save & continue to boundary' }).click()

  // Existing boundary's 4 corners are pre-populated on the boundary step.
  await expect(page.locator('.work-card-title')).toContainText('Mark the boundary')
  await expect(page.locator('.corner-list-row')).toHaveCount(4)
  await expect(page.locator('button', { hasText: 'Keep existing boundary' })).toBeVisible()
})
