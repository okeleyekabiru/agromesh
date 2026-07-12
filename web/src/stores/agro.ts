import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import type {
  Farm, Section, Worker, WorkerRole, ActivityLog, Transaction,
  InventoryItem, ChemicalProduct, UpcomingTask, LowStockAlert, WeatherWidget, DashboardData,
  AuthUser, AuthResponse, FeatureFlag, FarmSummary, State,
  BoundarySubmissionResult, WizardSectionSummary
} from '@/types'
import {
  farmApi, sectionApi, activityApi, transactionApi,
  workerApi, workerRoleApi, inventoryApi, chemicalApi,
  authApi, featuresApi, referenceApi,
  setFarmId
} from '@/services/api'

const ACTIVE_FARM_KEY = 'agro_active_farm_id'

export const useAgroStore = defineStore('agro', () => {

  // ── STATE ──
  const farm = ref<Farm | null>(null)
  const sections = ref<Section[]>([])
  const workers = ref<Worker[]>([])
  const workerRoles = ref<WorkerRole[]>([])
  const activities = ref<ActivityLog[]>([])
  const transactions = ref<Transaction[]>([])
  const inventoryItems = ref<InventoryItem[]>([])
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

  // ── SETUP WIZARD (Sections Setup → Review & Confirm handoff) ──
  // SectionDto doesn't expose a boundary-source field, so we track it client-side for the
  // lifetime of the wizard session just to render it in the Review & Confirm summary.
  const wizardSections = ref<WizardSectionSummary[]>([])

  // ── COMPUTED ──
  const isAuthenticated = computed(() => !!token.value)

  const activeFarmSummary = computed(() =>
    farms.value.find(f => f.id === activeFarmId.value) ?? null
  )

  const hasMultipleFarms = computed(() => farms.value.length > 1)

  const pendingReviews = computed(() =>
    transactions.value.filter(t => !t.reviewedByUser).length
  )

  const supervisors = computed(() =>
    workers.value.filter(w => w.roleTitle.toLowerCase().includes('supervisor'))
  )

  // If inventory isn't loaded yet, fall back to lowStockAlerts (populated by dashboard fetch)
  const lowStockCount = computed(() =>
    inventoryItems.value.length > 0
      ? inventoryItems.value.filter(i => i.isLowStock).length
      : lowStockAlerts.value.length
  )

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
    sections.value = []
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
    sectionId: string,
    body: { operationId: string; points: { lat: number; lng: number }[]; confirmImplausibleDistance?: boolean }
  ): Promise<BoundarySubmissionResult> {
    const res = await sectionApi.submitBoundaryPoints(sectionId, body)
    return res.data
  }

  async function uploadBoundaryFile(
    sectionId: string,
    body: { operationId: string; fileName: string; content: string }
  ): Promise<BoundarySubmissionResult> {
    const res = await sectionApi.uploadBoundaryFile(sectionId, body)
    return res.data
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

  async function fetchDashboard() {
    try {
      loading.value = true
      const res = await farmApi.getDashboard()
      dashboard.value = res.data
      upcomingTasks.value = res.data.upcomingTasks ?? []
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

  async function fetchSections() {
    try {
      const res = await sectionApi.getAll()
      sections.value = res.data
    } catch (e) {
      error.value = 'Failed to load sections'
    }
  }

  async function fetchActivities(page = 1) {
    try {
      const res = await activityApi.getAll(page)
      activities.value = res.data.items
    } catch (e) {
      error.value = 'Failed to load activities'
    }
  }

  async function fetchTransactions(page = 1, pendingReview?: boolean) {
    try {
      const res = await transactionApi.getAll(page, 20, pendingReview)
      transactions.value = res.data.items
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

  async function fetchWorkerRoles() {
    try {
      const res = await workerRoleApi.getAll()
      workerRoles.value = res.data
    } catch (e) {
      error.value = 'Failed to load worker roles'
    }
  }

  async function fetchInventory() {
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
    }
  }

  async function fetchChemicals() {
    try {
      const res = await chemicalApi.getAll()
      chemicals.value = res.data
    } catch { /* non-critical */ }
  }

  async function logActivity(body: unknown) {
    const res = await activityApi.log(body)
    const { activity, lowStockWarnings } = res.data
    activities.value.unshift(activity)
    if (lowStockWarnings?.length) {
      await fetchInventory() // refresh stock levels
    }
    return res.data
  }

  async function reviewTransaction(id: string, body: unknown) {
    const res = await transactionApi.review(id, body)
    const idx = transactions.value.findIndex(t => t.id === id)
    if (idx !== -1) transactions.value[idx] = res.data
    return res.data
  }

  async function adjustStock(itemId: string, body: unknown) {
    await inventoryApi.adjust(itemId, body)
    await fetchInventory()
  }

  async function addStockFromReceipt(transactionId: string, body: unknown) {
    await transactionApi.recordReceipt(transactionId, body)
    await fetchInventory()
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
    await Promise.all([fetchFarm(), fetchDashboard(), fetchWeather()])
    lastSynced.value = new Date().toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit' })
  }

  return {
    // state
    farm, sections, workers, workerRoles, activities, transactions,
    inventoryItems, chemicals, upcomingTasks, lowStockAlerts, weather, dashboard,
    isOnline, lastSynced, loading, error, needsOnboarding,
    // auth state
    currentUser, token, enabledFeatures,
    // multi-farm state
    companyId, farms, activeFarmId, states, wizardSections,
    // computed
    isAuthenticated, pendingReviews, supervisors, lowStockCount,
    activeFarmSummary, hasMultipleFarms,
    // actions
    fetchFarm, fetchDashboard, fetchWeather, fetchSections, fetchActivities,
    fetchTransactions, fetchWorkers, fetchWorkerRoles, fetchInventory, fetchChemicals,
    logActivity, reviewTransaction, adjustStock, addStockFromReceipt,
    discoverFarm, updateFarmProfile, initDashboard,
    // multi-farm actions
    fetchFarms, switchFarm, addFarm, setActiveFarm, fetchStates, setFarmLocation,
    completeFarmSetup, submitBoundaryPoints, uploadBoundaryFile,
    // auth actions
    login, register, logout, fetchFeatures, restoreSession,
  }
})
