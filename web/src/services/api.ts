import axios from 'axios'

const BASE = import.meta.env.VITE_API_URL || 'http://localhost:5000'
export let FARM_ID = import.meta.env.VITE_FARM_ID || 'farm_001'
export function setFarmId(id: string) { FARM_ID = id }

export const api = axios.create({
  baseURL: BASE,
  headers: { 'Content-Type': 'application/json' }
})

// Attach JWT on every request
api.interceptors.request.use(config => {
  const token = localStorage.getItem('agro_token')
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

// On 401: clear session and return to landing
api.interceptors.response.use(
  res => res,
  err => {
    if (err.response?.status === 401) {
      localStorage.removeItem('agro_token')
      localStorage.removeItem('agro_user')
      window.location.href = '/'
    }
    return Promise.reject(err)
  }
)

// ── Farm ──
export const farmApi = {
  list: () => api.get('/api/farms'),
  get: (farmId: string = FARM_ID) => api.get(`/api/farms/${farmId}`),
  create: (body: unknown) => api.post('/api/farms', body),
  addFarm: (body: { name: string; stateId: string; lgaFreetext?: string }) => api.post('/api/farms', body),
  update: (farmId: string, body: unknown) => api.patch(`/api/farms/${farmId}`, body),
  setLocation: (farmId: string, body: { stateId: string; lgaFreetext?: string }) =>
    api.patch(`/api/farms/${farmId}/location`, body),
  setSetupStatus: (farmId: string) => api.patch(`/api/farms/${farmId}/setup-status`),
  getOnboardingStatus: () => api.get(`/api/farms/${FARM_ID}/onboarding-status`),
  getWeather: () => api.get(`/api/farms/${FARM_ID}/weather`),
  getDashboard: () => api.get(`/api/farms/${FARM_ID}/dashboard`),
  setAllocationRules: (rules: unknown[]) => api.post(`/api/farms/${FARM_ID}/allocation-rules`, rules)
}

// ── Sections ──
export const sectionApi = {
  getAll: () => api.get(`/api/farms/${FARM_ID}/sections`),
  create: (body: unknown) => api.post(`/api/farms/${FARM_ID}/sections`, body),
  update: (sectionId: string, body: unknown) => api.patch(`/api/farms/${FARM_ID}/sections/${sectionId}`, body),
  createBlock: (sectionId: string, body: unknown) => api.post(`/api/farms/${FARM_ID}/sections/${sectionId}/blocks`, body),
  submitBoundaryPoints: (
    sectionId: string,
    body: { operationId: string; points: { lat: number; lng: number }[]; confirmImplausibleGeometry?: boolean }
  ) => api.post(`/api/sections/${sectionId}/boundary/points`, body),
  uploadBoundaryFile: (
    sectionId: string,
    body: { operationId: string; fileName: string; content: string; confirmImplausibleGeometry?: boolean }
  ) => api.post(`/api/sections/${sectionId}/boundary/upload`, body)
}

// ── Reference data ──
export const referenceApi = {
  getStates: () => api.get('/api/reference/states')
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
  updateRate: (roleId: string, payRate: number, payFrequency: number) =>
    api.patch(`/api/farms/${FARM_ID}/worker-roles/${roleId}/rate`, { payRate, payFrequency })
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

// ── Auth ──
export const authApi = {
  login:    (body: { email: string; password: string }) =>
    api.post('/api/auth/login', body),
  register: (body: { fullName: string; email: string; password: string; farmName: string }) =>
    api.post('/api/auth/register', body),
  me: () => api.get('/api/auth/me'),
}

// ── Feature Flags ──
export const featuresApi = {
  getForFarm: (farmId: string) => api.get(`/api/farms/${farmId}/features`),
}
