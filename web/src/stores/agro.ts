import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import type {
  Farm, Block, Worker, WorkerRole, ActivityLog, Transaction,
  InventoryItem, OutstandingStock, InventoryCategory, ChemicalProduct, UpcomingTask, LowStockAlert, WeatherWidget, DashboardData,
  AuthUser, AuthResponse, FeatureFlag, FarmSummary, State,
  BoundarySubmissionResult, PickStockResponse, StockAdjustmentRequest, PriceChangeRequest, SatelliteOverview,
  Supplier, Order, CreateOrderLineItemRequest, Report
} from '@/types'
import {
  farmApi, blockApi, activityApi, transactionApi,
  workerApi, workerRoleApi, inventoryApi, inventoryCategoryApi, inventoryAdjustmentsApi, inventoryPriceChangesApi, chemicalApi,
  authApi, featuresApi, referenceApi, supplierApi, orderApi, reportApi,
  setFarmId,
  managersApi, satelliteApi
} from '@/services/api'
import { normalizeBlobError } from '@/utils/http'
import { normalizeActivity, normalizeTask, normalizeBlock, normalizeTransaction } from '@/utils/enums'

const ACTIVE_FARM_KEY = 'agro_active_farm_id'

export const useAgroStore = defineStore('agro', () => {

  // ── STATE ──
  const farm = ref<Farm | null>(null)
  const blocks = ref<Block[]>([])
  const workers = ref<Worker[]>([])
  const workerRoles = ref<WorkerRole[]>([])
  const activities = ref<ActivityLog[]>([])
  const transactions = ref<Transaction[]>([])
  const transactionsTotal = ref(0)
  const inventoryItems = ref<InventoryItem[]>([])
  const outstandingStock = ref<OutstandingStock[]>([])
  const inventoryCategories = ref<InventoryCategory[]>([])
  const pendingAdjustments = ref<StockAdjustmentRequest[]>([])
  const pendingPriceChanges = ref<PriceChangeRequest[]>([])
  const suppliers = ref<Supplier[]>([])
  const orders = ref<Order[]>([])
  const chemicals = ref<ChemicalProduct[]>([])
  const upcomingTasks = ref<UpcomingTask[]>([])
  const lowStockAlerts = ref<LowStockAlert[]>([])
  const weather = ref<WeatherWidget | null>(null)
  const dashboard = ref<DashboardData | null>(null)

  const isOnline = ref(true)
  const lastSynced = ref<string | null>(null)
  const loading = ref(false)
  const error = ref<string | null>(null)
  const needsOnboarding = ref(false)

  // ── AUTH STATE ──
  const currentUser = ref<AuthUser | null>(null)
  const token = ref<string | null>(localStorage.getItem('agro_token'))
  const enabledFeatures = ref<string[]>([])

  // ── MULTI-FARM STATE ──
  const companyId = ref<string | null>(null)
  const farms = ref<FarmSummary[]>([])
  const activeFarmId = ref<string | null>(localStorage.getItem(ACTIVE_FARM_KEY))

  // ── REFERENCE DATA ──
  const states = ref<State[]>([])

  // ── SETUP WIZARD (Blocks Setup → Review & Confirm handoff) ──
  // Real, server-confirmed blocks added during this wizard session — used to drive the
  // Review & Confirm summary.
  const wizardBlocks = ref<Block[]>([])

  // ── COMPUTED ──
  const isAuthenticated = computed(() => !!token.value)

  const activeFarmSummary = computed(() =>
    farms.value.find(f => f.id === activeFarmId.value) ?? null
  )

  const hasMultipleFarms = computed(() => farms.value.length > 1)

  // Every transaction nobody has reviewed yet, counted by the server across all dates. Until it has been read it falls
  // back to the dashboard's figure, and last of all to what is on screen (only the page that happens to be loaded).
  const pendingReviewCount = ref<number | null>(null)
  const pendingReviews = computed(() =>
    pendingReviewCount.value ?? dashboard.value?.pendingReviewCount ?? transactions.value.filter(t => !t.reviewedByUser).length
  )

  async function refreshPendingReviews() {
    try {
      pendingReviewCount.value = (await transactionApi.summary()).data.pendingReview
    } catch { /* keep the last known count */ }
  }

  // Crop condition per block from satellite (NDVI / NDMI); empty until the Copernicus keys are set on the API.
  const satellite = ref<SatelliteOverview | null>(null)
  async function fetchSatellite() {
    try { satellite.value = (await satelliteApi.get()).data } catch { /* keep what was shown */ }
  }
  async function refreshSatellite() {
    satellite.value = (await satelliteApi.refresh()).data
  }

  // Farm managers can supervise an activity too: they are accounts, not workers, so they are listed separately.
  const managers = ref<{ id: string; fullName: string; role?: string }[]>([])
  async function fetchManagers() {
    try { managers.value = (await managersApi.getAll()).data ?? [] } catch { /* the list stays as it was */ }
  }
  /** Who supervised: a supervisor worker, or a farm manager. */
  const supervisorName = (id: string) =>
    workers.value.find(w => w.id === id)?.name ?? managers.value.find(m => m.id === id)?.fullName ?? ''

  const supervisors = computed(() =>
    workers.value.filter(w => w.roleTitle.toLowerCase().includes('supervisor'))
  )

  // If inventory isn't loaded yet, fall back to lowStockAlerts (populated by dashboard fetch)
  const lowStockCount = computed(() =>
    inventoryItems.value.length > 0
      ? inventoryItems.value.filter(i => i.isLowStock).length
      : lowStockAlerts.value.length
  )

  // Things waiting for an admin on the stock approvals page: stock adjustments and price changes.
  const pendingAdjustmentsCount = computed(() => pendingAdjustments.value.length + pendingPriceChanges.value.length)

  // ── MULTI-FARM ACTIONS ──

  /** Sets the active farm pointer (localStorage + the shared FARM_ID used by farm-scoped API calls). */
  function setActiveFarm(farmId: string | null) {
    activeFarmId.value = farmId
    if (farmId) {
      localStorage.setItem(ACTIVE_FARM_KEY, farmId)
      setFarmId(farmId)
    } else {
      localStorage.removeItem(ACTIVE_FARM_KEY)
    }
  }

  /** Picks the active farm from the last-used id (if still valid) or defaults to the first farm. */
  function applyActiveFarm() {
    const saved = localStorage.getItem(ACTIVE_FARM_KEY)
    const match = saved ? farms.value.find(f => f.id === saved) : null
    setActiveFarm(match ? match.id : (farms.value[0]?.id ?? null))
  }

  /** Loads the farms belonging to the current user's company (server-side scoped via JWT). */
  async function fetchFarms(): Promise<FarmSummary[]> {
    const res = await farmApi.list()
    farms.value = res.data ?? []
    return farms.value
  }

  /** Switches the active farm and refreshes farm-scoped context (sidebar, dashboard, etc.). */
  async function switchFarm(farmId: string) {
    if (farmId === activeFarmId.value) return
    setActiveFarm(farmId)
    farm.value = null
    dashboard.value = null
    blocks.value = []
    await fetchFarm()
    await fetchFeatures(farmId)
  }

  /** Flow A2 — create a new, independent farm under the caller's existing company. */
  async function addFarm(body: { name: string; stateId: string; lgaFreetext?: string }) {
    const res = await farmApi.addFarm(body)
    const summary: FarmSummary = { id: res.data.id, name: res.data.name, setupStatus: res.data.setupStatus }
    farms.value.push(summary)
    setActiveFarm(summary.id)
    farm.value = res.data
    needsOnboarding.value = false
    return res.data as Farm
  }

  async function fetchStates(): Promise<State[]> {
    if (states.value.length) return states.value
    try {
      const res = await referenceApi.getStates()
      states.value = res.data ?? []
    } catch {
      states.value = []
    }
    return states.value
  }

  async function setFarmLocation(farmId: string, body: { stateId: string; lgaFreetext?: string }) {
    const res = await farmApi.setLocation(farmId, body)
    farm.value = res.data
    return res.data as Farm
  }

  async function completeFarmSetup(farmId: string) {
    const res = await farmApi.setSetupStatus(farmId)
    const idx = farms.value.findIndex(f => f.id === farmId)
    if (idx !== -1) farms.value[idx] = { ...farms.value[idx], setupStatus: res.data.setupStatus }
    if (farm.value?.id === farmId) farm.value = res.data
    return res.data as Farm
  }

  async function submitBoundaryPoints(
    blockId: string,
    body: { operationId: string; points: { lat: number; lng: number }[]; confirmImplausibleGeometry?: boolean }
  ): Promise<BoundarySubmissionResult> {
    const res = await blockApi.submitBoundaryPoints(blockId, body)
    return res.data
  }

  async function uploadBoundaryFile(
    blockId: string,
    body: { operationId: string; fileName: string; content: string; confirmImplausibleGeometry?: boolean }
  ): Promise<BoundarySubmissionResult> {
    const res = await blockApi.uploadBoundaryFile(blockId, body)
    return res.data
  }

  async function updateBlock(
    blockId: string,
    body: { name: string; crops: { cropType: number; percentage: number }[]; areaValue: number; treeCount: number | null; plantingYear?: number | null }
  ): Promise<Block> {
    const res = await blockApi.update(blockId, body)
    const block = normalizeBlock(res.data)
    const idx = blocks.value.findIndex(s => s.id === blockId)
    if (idx !== -1) blocks.value[idx] = block
    return block
  }

  // ── ACTIONS ──
  async function fetchFarm() {
    try {
      const res = await farmApi.get()
      farm.value = res.data
    } catch (e) {
      console.error('Failed to fetch farm', e)
    }
  }

  async function fetchReport(year: number, month: number): Promise<Report> {
    const res = await reportApi.get(year, month)
    return res.data as Report
  }

  async function fetchDashboard() {
    try {
      loading.value = true
      const res = await farmApi.getDashboard()
      dashboard.value = res.data
      upcomingTasks.value = (res.data.upcomingTasks ?? []).map(normalizeTask)
      dashboard.value!.upcomingTasks = upcomingTasks.value
      lowStockAlerts.value = res.data.lowStockAlerts ?? []
    } catch (e) {
      console.error('Dashboard fetch failed', e)
    } finally {
      loading.value = false
    }
  }

  async function fetchWeather() {
    try {
      const res = await farmApi.getWeather()
      weather.value = res.data
    } catch { /* non-critical */ }
  }

  async function fetchBlocks() {
    try {
      const res = await blockApi.getAll()
      blocks.value = (res.data as Block[]).map(normalizeBlock)
    } catch (e) {
      error.value = 'Failed to load blocks'
    }
  }

  async function fetchActivities(page = 1) {
    try {
      const res = await activityApi.getAll(page)
      activities.value = (res.data.items as ActivityLog[]).map(normalizeActivity)
    } catch (e) {
      error.value = 'Failed to load activities'
    }
  }

  /** Fetches a single activity (normalised) and refreshes its copy in the cached list. */
  async function fetchActivity(id: string): Promise<ActivityLog> {
    const res = await activityApi.get(id)
    const activity = normalizeActivity(res.data)
    const idx = activities.value.findIndex(a => a.id === id)
    if (idx !== -1) activities.value[idx] = activity
    return activity
  }

  /** Loads a page of transactions. `append` adds the page to what is shown (Load more); otherwise it replaces it. */
  async function fetchTransactions(page = 1, pendingReview?: boolean, append = false, kind?: 'Income' | 'Capital' | 'Expense', category?: string) {
    try {
      const res = await transactionApi.getAll(page, 20, pendingReview, kind, category)
      const items = (res.data.items as Transaction[]).map(normalizeTransaction)
      transactions.value = append ? [...transactions.value, ...items.filter(n => !transactions.value.some(t => t.id === n.id))] : items
      transactionsTotal.value = res.data.total ?? items.length
    } catch (e) {
      error.value = 'Failed to load transactions'
    }
  }

  async function fetchWorkers() {
    try {
      const res = await workerApi.getAll()
      workers.value = res.data
    } catch (e) {
      error.value = 'Failed to load workers'
    }
  }

  async function addWorker(body: { name: string; initials: string; workerRoleId: string; type: number }) {
    const res = await workerApi.add(body)
    workers.value.push(res.data)
    return res.data as Worker
  }

  async function createWorkerRole(body: { title: string; defaultType: number; payRate: number; payFrequency: number }) {
    const res = await workerRoleApi.create(body)
    workerRoles.value.push(res.data)
    return res.data as WorkerRole
  }

  async function updateWorkerRoleRate(roleId: string, payRate: number, payFrequency: number) {
    await workerRoleApi.updateRate(roleId, payRate, payFrequency)
    await Promise.all([fetchWorkerRoles(), fetchWorkers()]) // workers carry their band's rate
  }

  async function fetchWorkerRoles() {
    try {
      const res = await workerRoleApi.getAll()
      workerRoles.value = res.data
    } catch (e) {
      error.value = 'Failed to load worker roles'
    }
  }

  async function fetchInventory() {
    loading.value = true
    error.value = null
    try {
      const res = await inventoryApi.getAll()
      inventoryItems.value = res.data
      lowStockAlerts.value = res.data.filter((i: InventoryItem) => i.isLowStock).map((i: InventoryItem) => ({
        inventoryItemId: i.id,
        inventoryItemName: i.name,
        currentStock: i.currentStock,
        unit: i.unit,
        reorderThreshold: i.reorderThreshold
      }))
    } catch (e) {
      error.value = 'Failed to load inventory'
    } finally {
      loading.value = false
    }
  }

  async function fetchInventoryCategories() {
    try {
      const res = await inventoryCategoryApi.getAll()
      inventoryCategories.value = res.data
    } catch { /* non-critical — Add Item modal falls back to an empty category list */ }
  }

  async function createInventoryCategory(name: string): Promise<InventoryCategory> {
    const res = await inventoryCategoryApi.create({ name })
    inventoryCategories.value.push(res.data)
    return res.data as InventoryCategory
  }

  async function fetchChemicals() {
    try {
      const res = await chemicalApi.getAll()
      chemicals.value = res.data
    } catch { /* non-critical */ }
  }

  async function logActivity(body: unknown) {
    const res = await activityApi.log(body)
    const activity = normalizeActivity(res.data.activity)
    activities.value.unshift(activity)
    // Every input consumes stock (picked-out stock first, then the store) — refresh both views.
    await Promise.all([fetchInventory(), fetchOutstandingStock()])
    // A weeding or spraying changes when the next weeding / spraying is due: recalculate both views.
    await Promise.all([fetchBlocks(), fetchDashboard()])
    // The AI review runs in the background after a weeding / spraying is saved: pick it up shortly after.
    const type = (res.data.activity as { activityType: unknown }).activityType
    if (type === 'Weeding' || type === 'Spraying' || type === 0 || type === 1) {
      setTimeout(() => { fetchBlocks(); fetchDashboard() }, 8000)
    }
    return res.data
  }

  async function reviewTransaction(id: string, body: unknown) {
    const res = await transactionApi.review(id, body)
    const idx = transactions.value.findIndex(t => t.id === id)
    const updated = normalizeTransaction(res.data)
    if (idx !== -1) transactions.value[idx] = updated
    refreshPendingReviews()
    return updated
  }

  /**
   * Phase 3 — submitting an adjustment no longer applies immediately: it creates a Pending
   * StockAdjustmentRequest, so unlike the pre-Phase-3 version this does NOT call fetchInventory()
   * afterward (CurrentStock hasn't changed yet). It does refresh the pending-adjustments list so
   * the nav badge/queue reflect the new request right away.
   */
  async function adjustStock(itemId: string, body: { newQuantity: number; notes: string }): Promise<StockAdjustmentRequest> {
    const res = await inventoryApi.adjust(itemId, body)
    await fetchPendingAdjustments()
    return res.data as StockAdjustmentRequest
  }

  async function fetchPendingAdjustments() {
    try {
      const res = await inventoryAdjustmentsApi.getAll('Pending')
      pendingAdjustments.value = res.data ?? []
    } catch { /* non-critical — nav badge/queue just show nothing until the next successful fetch */ }
  }

  /** Approving applies the adjustment server-side — refresh both the queue and the item's stock. */
  async function approveStockAdjustment(requestId: string, reviewNotes?: string) {
    await inventoryAdjustmentsApi.approve(requestId, { reviewNotes })
    await Promise.all([fetchPendingAdjustments(), fetchInventory()])
  }

  /** Rejecting never touches stock — only the queue needs refreshing. */
  async function rejectStockAdjustment(requestId: string, reviewNotes?: string) {
    await inventoryAdjustmentsApi.reject(requestId, { reviewNotes })
    await fetchPendingAdjustments()
  }

  /**
   * Manual "pick / issue from stock" — a genuinely separate code path from activity-log-driven
   * auto-deduction. Refreshes the list the same way adjustStock does, but also returns the response
   * (item + lowStockWarning) so the caller can surface the warning inline without a second
   * round-trip.
   *
   * Who performed the pick is no longer client-supplied (see the removed `workerId` field) — the
   * backend auto-attributes the authenticated caller (ICurrentUserAccessor). `blockId`
   * records which farm area the stock is going to instead.
   */
  async function pickStock(
    itemId: string,
    body: { quantity: number; blockId: string; purpose: string }
  ): Promise<PickStockResponse> {
    const res = await inventoryApi.pick(itemId, body)
    await Promise.all([fetchInventory(), fetchOutstandingStock()])
    return res.data as PickStockResponse
  }

  async function fetchOutstandingStock() {
    try {
      const res = await inventoryApi.getOutstanding()
      outstandingStock.value = res.data
    } catch {
      /* non-fatal: the card just stays empty */
    }
  }

  /** Hands leftover stock back to the store; throws on a rejected quantity so the caller can show why. */
  /** Prices an item already on hand; optionally fills in past activities that had no input cost. */
  /** Asks for a price. It is NOT applied: an admin has to approve it first (see the stock approvals page). */
  async function setItemPricing(itemId: string, body: { packSize: number; packLabel: string; pricePerPack: number; backfillActivities: boolean }) {
    const res = await inventoryApi.setPricing(itemId, body)
    await fetchPendingPriceChanges()
    return res.data as PriceChangeRequest
  }

  async function fetchPendingPriceChanges() {
    try {
      pendingPriceChanges.value = (await inventoryPriceChangesApi.getAll('Pending')).data ?? []
    } catch { /* the queue just stays as it was */ }
  }

  async function approvePriceChange(requestId: string) {
    await inventoryPriceChangesApi.approve(requestId)
    await Promise.all([fetchPendingPriceChanges(), fetchInventory()])
  }

  async function rejectPriceChange(requestId: string) {
    await inventoryPriceChangesApi.reject(requestId)
    await fetchPendingPriceChanges()
  }

  async function returnStock(itemId: string, body: { quantity: number; blockId: string; notes?: string | null }) {
    await inventoryApi.returnStock(itemId, body)
    await Promise.all([fetchInventory(), fetchOutstandingStock()])
  }

  /** Adds an item to the catalogue (no stock: stock comes in through a received purchase order). */
  async function addOrRestockInventoryItem(body: {
    name: string
    unit: string
    categoryId: string
    quantity: number
    reorderThreshold: number
    chemicalProductId?: string | null
    existingItemId?: string | null
    packSize?: number | null
    packLabel?: string | null
    pricePerPack?: number | null
  }): Promise<InventoryItem> {
    const res = await inventoryApi.create(body)
    await fetchInventory()
    return res.data as InventoryItem
  }

  // ── Suppliers (Phase 4) ──

  async function fetchSuppliers() {
    try {
      const res = await supplierApi.getAll()
      suppliers.value = res.data ?? []
    } catch { /* non-critical — the create-order supplier picker falls back to an empty list */ }
  }

  async function createSupplier(body: { name: string; phone?: string | null; email?: string | null }): Promise<Supplier> {
    const res = await supplierApi.create(body)
    suppliers.value.push(res.data)
    return res.data as Supplier
  }

  // ── Orders (Phase 4) ──

  async function fetchOrders(status?: string) {
    try {
      loading.value = true
      const res = await orderApi.getAll(status)
      orders.value = res.data ?? []
    } catch (e) {
      error.value = 'Failed to load orders'
    } finally {
      loading.value = false
    }
  }

  async function createOrder(body: { supplierId: string; lineItems: CreateOrderLineItemRequest[] }): Promise<Order> {
    const res = await orderApi.create(body)
    orders.value.unshift(res.data)
    return res.data as Order
  }

  /** Placing has no stock side effects — only the order's own status changes. */
  async function placeOrder(orderId: string): Promise<Order> {
    const res = await orderApi.place(orderId)
    const idx = orders.value.findIndex(o => o.id === orderId)
    if (idx !== -1) orders.value[idx] = res.data
    return res.data as Order
  }

  /**
   * Receiving converts every line item into real stock server-side — refresh inventory afterward
   * the same "refetch the thing that changed" convention approveStockAdjustment already uses.
   */
  async function receiveOrder(orderId: string): Promise<Order> {
    const res = await orderApi.receive(orderId)
    const idx = orders.value.findIndex(o => o.id === orderId)
    if (idx !== -1) orders.value[idx] = res.data
    await fetchInventory()
    return res.data as Order
  }

  /**
   * Fetches the invoice PDF as a Blob (see orderApi.downloadInvoice's doc comment for why not
   * window.open) and triggers a synthetic anchor-click download — the standard pattern for an
   * authenticated (Bearer-token, non-cookie) file download in the browser. On failure, re-throws
   * via normalizeBlobError so the caller's extractErrorMessage(e, ...) still surfaces the backend's
   * real detail instead of falling back to a generic message (responseType: 'blob' means error
   * bodies come back as a Blob too, not parsed JSON).
   */
  async function downloadOrderInvoice(orderId: string): Promise<void> {
    try {
      const res = await orderApi.downloadInvoice(orderId)
      const blobUrl = URL.createObjectURL(new Blob([res.data], { type: 'application/pdf' }))
      const link = document.createElement('a')
      link.href = blobUrl
      link.download = `invoice-${orderId}.pdf`
      document.body.appendChild(link)
      link.click()
      document.body.removeChild(link)
      URL.revokeObjectURL(blobUrl)
    } catch (e) {
      throw await normalizeBlobError(e)
    }
  }

  // ── AUTH ACTIONS ──

  function restoreSession() {
    const saved = localStorage.getItem('agro_user')
    if (saved) {
      try { currentUser.value = JSON.parse(saved) } catch { /* ignore */ }
    }
  }

  async function login(email: string, password: string) {
    const res = await authApi.login({ email, password })
    const data = res.data as AuthResponse
    token.value = data.token
    currentUser.value = data.user
    companyId.value = data.companyId ?? data.user.companyId ?? null
    farms.value = data.farms ?? []
    localStorage.setItem('agro_token', data.token)
    localStorage.setItem('agro_user', JSON.stringify(data.user))
    applyActiveFarm()
    needsOnboarding.value = farms.value.length === 0
    if (activeFarmId.value) {
      await fetchFeatures(activeFarmId.value)
    }
    return data
  }

  async function register(body: { fullName: string; email: string; password: string; farmName: string }) {
    const res = await authApi.register(body)
    const data = res.data as AuthResponse
    token.value = data.token
    currentUser.value = data.user
    companyId.value = data.companyId ?? data.user.companyId ?? null
    farms.value = data.farms ?? []
    localStorage.setItem('agro_token', data.token)
    localStorage.setItem('agro_user', JSON.stringify(data.user))
    applyActiveFarm()
    needsOnboarding.value = true
    return data
  }

  function logout() {
    token.value = null
    currentUser.value = null
    enabledFeatures.value = []
    farm.value = null
    companyId.value = null
    farms.value = []
    setActiveFarm(null)
    localStorage.removeItem('agro_token')
    localStorage.removeItem('agro_user')
  }

  async function fetchFeatures(farmId: string) {
    try {
      const res = await featuresApi.getForFarm(farmId)
      enabledFeatures.value = (res.data as FeatureFlag[])
        .filter(f => f.enabled)
        .map(f => f.name)
    } catch {
      // API unavailable or no flags configured → enable everything
      enabledFeatures.value = ['finance','inventory','workers','reports','scheduling','weather','ai_categorization']
    }
  }

  /**
   * Ensures `farms`/`activeFarmId` are populated after a page reload (token restored from
   * localStorage but the in-memory farms array from login/register is gone). There's no
   * `/api/auth/me` endpoint, so this simply re-fetches `GET /api/farms` — which the backend now
   * scopes server-side to the caller's company — and trusts that array directly (no client-side
   * "which farm is really theirs" heuristic needed anymore).
   */
  async function discoverFarm(): Promise<boolean> {
    try {
      if (farms.value.length === 0) {
        await fetchFarms()
      }
      if (!farms.value.length) {
        needsOnboarding.value = true
        return false
      }
      if (!activeFarmId.value || !farms.value.some(f => f.id === activeFarmId.value)) {
        applyActiveFarm()
      } else {
        setFarmId(activeFarmId.value)
      }
      needsOnboarding.value = false
      return true
    } catch {
      needsOnboarding.value = true
      return false
    }
  }

  async function updateFarmProfile(opts: {
    farmId: string
    name: string
    areaValue: number
    areaUnit: 'Hectares' | 'Acres'
  }) {
    const res = await farmApi.update(opts.farmId, {
      name: opts.name,
      areaValue: opts.areaValue,
      areaUnit: opts.areaUnit === 'Acres' ? 1 : 0,
    })
    farm.value = res.data
    return res.data as Farm
  }

  async function initDashboard() {
    const hasFarm = await discoverFarm()
    if (!hasFarm) return
    await Promise.all([fetchFarm(), fetchDashboard(), fetchWeather(), refreshPendingReviews()])
    lastSynced.value = new Date().toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit' })
  }

  return {
    // state
    farm, blocks, workers, workerRoles, activities, transactions,
    inventoryItems, inventoryCategories, pendingAdjustments, suppliers, orders, chemicals, upcomingTasks, lowStockAlerts, weather, dashboard,
    isOnline, lastSynced, loading, error, needsOnboarding,
    // auth state
    currentUser, token, enabledFeatures,
    // multi-farm state
    companyId, farms, activeFarmId, states, wizardBlocks,
    // computed
    managers, fetchManagers, supervisorName,
    satellite, fetchSatellite, refreshSatellite,
    isAuthenticated, pendingReviews, refreshPendingReviews, supervisors, lowStockCount, pendingAdjustmentsCount,
    activeFarmSummary, hasMultipleFarms,
    // actions
    fetchFarm, fetchDashboard, fetchWeather, fetchBlocks, fetchActivities, fetchActivity,
    transactionsTotal, fetchTransactions, fetchWorkers, addWorker, createWorkerRole, updateWorkerRoleRate, fetchWorkerRoles, fetchInventory, fetchInventoryCategories,
    createInventoryCategory, fetchChemicals,
    fetchReport, logActivity, reviewTransaction, adjustStock, pickStock, setItemPricing, fetchOutstandingStock, returnStock, outstandingStock, addOrRestockInventoryItem,
    fetchPendingAdjustments, approveStockAdjustment, rejectStockAdjustment,
    pendingPriceChanges, fetchPendingPriceChanges, approvePriceChange, rejectPriceChange,
    fetchSuppliers, createSupplier, fetchOrders, createOrder, placeOrder, receiveOrder, downloadOrderInvoice,
    discoverFarm, updateFarmProfile, initDashboard,
    // multi-farm actions
    fetchFarms, switchFarm, addFarm, setActiveFarm, fetchStates, setFarmLocation,
    completeFarmSetup, submitBoundaryPoints, uploadBoundaryFile, updateBlock,
    // auth actions
    login, register, logout, fetchFeatures, restoreSession,
  }
})
