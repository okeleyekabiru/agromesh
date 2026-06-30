import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import type {
  Farm, Section, Worker, WorkerRole, ActivityLog, Transaction,
  InventoryItem, ChemicalProduct, UpcomingTask, LowStockAlert, WeatherWidget, DashboardData
} from '@/types'
import {
  farmApi, sectionApi, activityApi, transactionApi,
  workerApi, workerRoleApi, inventoryApi, chemicalApi,
  setFarmId
} from '@/services/api'

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

  // ── COMPUTED ──
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

  async function discoverFarm(): Promise<boolean> {
    try {
      const res = await farmApi.list()
      const farms: typeof res.data = res.data
      if (!farms || farms.length === 0) {
        needsOnboarding.value = true
        return false
      }
      setFarmId(farms[0].id)
      farm.value = farms[0]
      needsOnboarding.value = false
      return true
    } catch {
      needsOnboarding.value = true
      return false
    }
  }

  async function createFarm(body: unknown) {
    const res = await farmApi.create(body)
    setFarmId(res.data.id)
    farm.value = res.data
    needsOnboarding.value = false
    return res.data
  }

  async function initDashboard() {
    const hasFarm = await discoverFarm()
    if (!hasFarm) return
    await Promise.all([fetchDashboard(), fetchWeather()])
    lastSynced.value = new Date().toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit' })
  }

  return {
    // state
    farm, sections, workers, workerRoles, activities, transactions,
    inventoryItems, chemicals, upcomingTasks, lowStockAlerts, weather, dashboard,
    isOnline, lastSynced, loading, error, needsOnboarding,
    // computed
    pendingReviews, supervisors, lowStockCount,
    // actions
    fetchFarm, fetchDashboard, fetchWeather, fetchSections, fetchActivities,
    fetchTransactions, fetchWorkers, fetchWorkerRoles, fetchInventory, fetchChemicals,
    logActivity, reviewTransaction, adjustStock, addStockFromReceipt,
    discoverFarm, createFarm, initDashboard
  }
})
