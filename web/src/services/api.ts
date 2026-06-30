import axios from 'axios'

const BASE = import.meta.env.VITE_API_URL || 'http://localhost:5000'
export const FARM_ID = import.meta.env.VITE_FARM_ID || 'farm_001'

export const api = axios.create({
  baseURL: BASE,
  headers: { 'Content-Type': 'application/json' }
})

// ── Farm ──
export const farmApi = {
  get: () => api.get(`/api/farms/${FARM_ID}`),
  getOnboardingStatus: () => api.get(`/api/farms/${FARM_ID}/onboarding-status`),
  getWeather: () => api.get(`/api/farms/${FARM_ID}/weather`),
  getDashboard: () => api.get(`/api/farms/${FARM_ID}/dashboard`),
  setAllocationRules: (rules: unknown[]) => api.post(`/api/farms/${FARM_ID}/allocation-rules`, rules)
}

// ── Sections ──
export const sectionApi = {
  getAll: () => api.get(`/api/farms/${FARM_ID}/sections`),
  create: (body: unknown) => api.post(`/api/farms/${FARM_ID}/sections`, body),
  createBlock: (sectionId: string, body: unknown) => api.post(`/api/farms/${FARM_ID}/sections/${sectionId}/blocks`, body)
}

// ── Activities ──
export const activityApi = {
  getAll: (page = 1, pageSize = 20) => api.get(`/api/farms/${FARM_ID}/activities?page=${page}&pageSize=${pageSize}`),
  log: (body: unknown) => api.post(`/api/farms/${FARM_ID}/activities`, body)
}

// ── Transactions ──
export const transactionApi = {
  getAll: (page = 1, pageSize = 20, pendingReview?: boolean) => {
    const q = pendingReview !== undefined ? `&pendingReview=${pendingReview}` : ''
    return api.get(`/api/farms/${FARM_ID}/transactions?page=${page}&pageSize=${pageSize}${q}`)
  },
  review: (id: string, body: unknown) => api.patch(`/api/farms/${FARM_ID}/transactions/${id}/review`, body),
  recordReceipt: (id: string, body: unknown) => api.post(`/api/farms/${FARM_ID}/transactions/${id}/inventory-receipt`, body),
  sync: (monoAccountId: string) => api.post(`/api/farms/${FARM_ID}/transactions/sync?monoAccountId=${monoAccountId}`)
}

// ── Workers ──
export const workerApi = {
  getAll: (activeOnly = true) => api.get(`/api/farms/${FARM_ID}/workers?activeOnly=${activeOnly}`),
  add: (body: unknown) => api.post(`/api/farms/${FARM_ID}/workers`, body)
}

// ── Worker Roles ──
export const workerRoleApi = {
  getAll: () => api.get(`/api/farms/${FARM_ID}/worker-roles`),
  create: (body: unknown) => api.post(`/api/farms/${FARM_ID}/worker-roles`, body),
  updateRate: (roleId: string, dailyRate: number) => api.patch(`/api/farms/${FARM_ID}/worker-roles/${roleId}/rate`, { dailyRate })
}

// ── Inventory ──
export const inventoryApi = {
  getAll: () => api.get(`/api/farms/${FARM_ID}/inventory`),
  getLowStock: () => api.get(`/api/farms/${FARM_ID}/inventory/low-stock`),
  create: (body: unknown) => api.post(`/api/farms/${FARM_ID}/inventory`, body),
  adjust: (itemId: string, body: unknown) => api.patch(`/api/farms/${FARM_ID}/inventory/${itemId}/adjust`, body),
  getMovements: (itemId: string) => api.get(`/api/farms/${FARM_ID}/inventory/${itemId}/movements`)
}

// ── Chemicals ──
export const chemicalApi = {
  getAll: () => api.get('/api/chemicals')
}
