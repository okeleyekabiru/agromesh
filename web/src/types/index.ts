// ── CORE DOMAIN TYPES ──

export type CropType = 'OilPalm' | 'Maize' | 'Mixed' | 'Cocoa' | 'Cassava'

export type SectionStatus = 'Active' | 'Idle' | 'HarvestSoon'

export type ActivityType =
  | 'Weeding'
  | 'Spraying'
  | 'FertilizerApplication'
  | 'Maintenance'
  | 'Harvesting'
  | 'Clearing'
  | 'Pruning'
  | 'PestControl'

export type WorkerType = 'Employee' | 'Contractor'

// Enums serialize as raw integers on the wire (no JsonStringEnumConverter configured
// backend-side) — 0=Daily, 1=Weekly, 2=Monthly. Convert at the API boundary; don't
// compare this against string literals.
export const PAY_FREQUENCY_LABELS = ['Daily', 'Weekly', 'Monthly'] as const

export type TransactionCategory =
  | 'Labour'
  | 'Fertilizer'
  | 'Chemicals'
  | 'Fuel'
  | 'Equipment'
  | 'Maintenance'
  | 'Transportation'
  | 'Seeds'
  | 'Sales'
  | 'Other'

export type CategorizationSource = 'Dictionary' | 'Pattern' | 'Llm' | 'Manual'

export type ConfidenceLevel = 'High' | 'Medium' | 'Low'

export type AreaUnit = 'Hectares' | 'Acres'

/** FarmSetupStatus enum ordinal as returned by the API: 0 = Draft, 1 = Configured. */
export type FarmSetupStatus = 0 | 1

export type InventoryCategory = 'Chemical' | 'Fertilizer' | 'Seed' | 'Fuel' | 'Equipment' | 'SpareParts' | 'Other'

export type ChemicalCategory = 'Herbicide' | 'Insecticide' | 'Fungicide' | 'Acaricide' | 'Nematicide'

// ── VALUE OBJECTS ──

export interface AreaDto {
  hectares: number
  acres: number
  displayValue: string
  displayUnit: string
}

export interface SectionCrop {
  cropType: CropType
  area: AreaDto
  percentage: number
}

// ── ENTITIES ──

export interface Farm {
  id: string
  name: string
  location: string
  area: AreaDto
  ownerId: string
  companyId: string
  countryCode: string
  stateId: string | null
  lgaFreetext: string | null
  setupStatus: FarmSetupStatus
  preferredAreaUnit: AreaUnit
  centroidLat: number | null
  centroidLng: number | null
  sectionCount: number
  workerCount: number
}

/** Lightweight farm summary returned inline from login/register (Farms[]) and GET /api/farms. */
export interface FarmSummary {
  id: string
  name: string
  setupStatus: FarmSetupStatus
}

/** A Nigerian state (or FCT) from GET /api/reference/states. */
export interface State {
  id: string
  name: string
  code: string
}

/** Response shape from POST /api/sections/{id}/boundary/points and /upload. */
export interface BoundarySubmissionResult {
  sectionId: string
  operationId: string
  submissionType: 0 | 1 // 0 = GpsPoints, 1 = FileUpload
  computedGeoJson: string
  computedHectares: number
  computedCentroidLat: number | null
  computedCentroidLng: number | null
}

/**
 * How a section's boundary was captured during the setup wizard. Tracked client-side only —
 * SectionDto doesn't surface a boundary source field, so this lives for the lifetime of the
 * wizard session (see agro store's `wizardSections`) purely to drive the Review & Confirm summary.
 */
export type BoundarySource = 'gps' | 'upload' | 'manual'

export interface WizardSectionSummary {
  id: string
  name: string
  areaValue: number
  boundarySource: BoundarySource
  boundaryHectares: number | null
}

export interface Section {
  id: string
  farmId: string
  name: string
  crops: SectionCrop[]
  area: AreaDto
  status: SectionStatus
  lastActivityDate: string | null
  boundaryGeoJson: string | null
  centroidLat: number | null
  centroidLng: number | null
  blocks: Block[]
  nextWeedingPrediction: UpcomingTask | null
  nextSprayingPrediction: UpcomingTask | null
}

export interface Block {
  id: string
  sectionId: string
  name: string
  area: AreaDto
  treeCount: number | null
}

export interface WorkerRole {
  id: string
  farmId: string
  title: string
  defaultType: WorkerType
  payRate: number
  payFrequency: number // index into PAY_FREQUENCY_LABELS
  workerCount: number
}

export interface Worker {
  id: string
  farmId: string
  name: string
  initials: string
  workerRoleId: string
  roleTitle: string
  payRate: number
  payFrequency: number // index into PAY_FREQUENCY_LABELS
  type: WorkerType
  active: boolean
  daysLoggedThisMonth: number
}

export interface WorkerLogDto {
  workerId: string
  workerName: string
  workerType: WorkerType
  hoursWorked: number
  payRate: number
  payFrequency: number // index into PAY_FREQUENCY_LABELS
}

export interface InputUsedDto {
  item: string
  quantity: number
  unit: string
}

export interface ActivityLog {
  id: string
  farmId: string
  sectionId: string
  blockId: string
  activityType: ActivityType
  date: string
  notes: string | null
  supervisorId: string
  labourCost: number
  inputCost: number
  createdOffline: boolean
  syncedAt: string | null
  workedAreaGeoJson: string | null
  workedArea: AreaDto | null
  chemicalProductId: string | null
  chemicalProductName: string | null
  workers: WorkerLogDto[]
  inputs: InputUsedDto[]
}

export interface CropAllocation {
  [crop: string]: number
}

export interface Transaction {
  id: string
  farmId: string
  monoId: string
  narration: string
  amount: number
  date: string
  category: TransactionCategory
  cropAllocation: CropAllocation
  confidence: ConfidenceLevel
  source: CategorizationSource
  reviewedByUser: boolean
  reviewNotes: string | null
}

export interface ChemicalProduct {
  id: string
  name: string
  manufacturer: string | null
  chemType: ChemicalCategory
  typicalIntervalDays: number
  active: boolean
}

export interface InventoryItem {
  id: string
  farmId: string
  name: string
  unit: string
  category: InventoryCategory
  currentStock: number
  reorderThreshold: number
  isLowStock: boolean
  chemicalProductId: string | null
  chemicalProductName: string | null
  lastMovementDate: string | null
}

export interface StockMovement {
  id: string
  inventoryItemId: string
  type: 'In' | 'Out' | 'Adjustment'
  quantity: number
  activityLogId: string | null
  transactionId: string | null
  notes: string | null
  date: string
}

export interface UpcomingTask {
  sectionId: string
  sectionName: string
  activityType: ActivityType
  lastDone: string | null
  predictedNextDue: string
  daysUntilDue: number
  reasoning: string
  recommendedChemical: string | null
  recommendedSprayDate: string | null
  confidence: ConfidenceLevel
  stockAvailable: number | null
  stockUnit: string | null
  isStale: boolean
  weatherDataUsed: boolean
}

export interface LowStockAlert {
  inventoryItemId: string
  inventoryItemName: string
  currentStock: number
  unit: string
  reorderThreshold: number
}

export interface WeatherWidget {
  latitude: number
  longitude: number
  currentSeason: string
  todayPrecipitationMm: number
  todayTempMaxC: number
  nextDryWindowStart: string | null
  nextDryWindowEnd: string | null
  rainfallLast30Days: number
}

export interface DashboardData {
  farmId: string
  totalSpendMtd: number
  labourCostMtd: number
  activitiesThisMonth: number
  pendingReviewCount: number
  sectionsMtd: SectionMtdDto[]
  cropPnl: CropPnlDto[]
  recentActivities: ActivityLog[]
  upcomingTasks: UpcomingTask[]
  lowStockAlerts: LowStockAlert[]
}

export interface SectionMtdDto {
  sectionId: string
  sectionName: string
  cost: number
  activityCount: number
}

export interface CropPnlDto {
  cropType: string
  revenue: number
  cost: number
  netProfit: number
}

// ── UI TYPES ──

export interface NavItem {
  id: string
  label: string
  icon: string
}

export interface KpiCard {
  label: string
  value: string
  delta: string
  deltaType: 'up' | 'down' | 'warn' | 'mute'
  dark?: boolean
}

export interface PagedResult<T> {
  items: T[]
  total: number
  page: number
  pageSize: number
}

// ── AUTH ──

export interface AuthUser {
  id: string
  email: string
  fullName: string
  companyId: string | null
}

export interface AuthResponse {
  token: string
  user: AuthUser
  companyId: string | null
  farms: FarmSummary[]
}

// ── FEATURE FLAGS ──

export type FeatureName =
  | 'finance'
  | 'inventory'
  | 'workers'
  | 'reports'
  | 'scheduling'
  | 'weather'
  | 'ai_categorization'

export interface FeatureFlag {
  name: FeatureName
  enabled: boolean
}
