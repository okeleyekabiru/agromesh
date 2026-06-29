// ── CORE DOMAIN TYPES ──

export type CropType = 'Oil Palm' | 'Maize' | 'Mixed'

export type SectionStatus = 'Active' | 'Idle' | 'Harvest Soon'

export type ActivityType =
  | 'Weeding'
  | 'Spraying'
  | 'Fertilizer Application'
  | 'Maintenance'
  | 'Harvesting'
  | 'Clearing'
  | 'Pruning'
  | 'Pest Control'

export type WorkerType = 'Employee' | 'Contractor'

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

export type CategorizationSource = 'dictionary' | 'pattern' | 'llm' | 'manual'

export type ConfidenceLevel = 'high' | 'medium' | 'low'

// ── ENTITIES ──

export interface Farm {
  id: string
  name: string
  location: string
  totalHectares: number
  crops: CropType[]
  createdAt: string
}

export interface Section {
  id: string
  farmId: string
  name: string
  crop: CropType
  hectares: number
  color: string
  status: SectionStatus
  lastActivityDate: string | null
  blockCount: number
}

export interface Block {
  id: string
  sectionId: string
  name: string
  hectares: number
  treeCount?: number
}

export interface Worker {
  id: string
  farmId: string
  name: string
  initials: string
  role: string
  type: WorkerType
  active: boolean
  dailyRate: number
}

export interface WorkerLog {
  workerId: string
  name: string
  type: WorkerType
  hoursWorked: number
}

export interface InputUsed {
  item: string
  quantity: number
  unit: string
}

export interface ActivityLog {
  id: string
  farmId: string
  sectionId: string
  blockId: string
  sectionName: string
  blockName: string
  activityType: ActivityType
  date: string
  workers: WorkerLog[]
  inputs: InputUsed[]
  notes: string
  supervisorId: string
  supervisorName: string
  labourCost: number
  inputCost: number
  totalCost: number
  createdOffline: boolean
  syncedAt: string | null
  icon: string
}

export interface CropAllocation {
  [crop: string]: number // percentage 0-100
}

export interface Transaction {
  id: string
  farmId: string
  monoId: string
  narration: string
  displayName: string
  amount: number
  date: string
  category: TransactionCategory
  cropAllocation: CropAllocation
  confidence: ConfidenceLevel
  categorizationSource: CategorizationSource
  reviewedByUser: boolean
  icon: string
  iconBg: string
}

export interface CropPnL {
  crop: CropType
  totalCost: number
  percentage: number
}

// ── UI / FORM TYPES ──

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

export interface BarChartData {
  label: string
  value: number
  height: number
  dim?: boolean
  current?: boolean
  displayValue?: string
}

export interface ActivityFormData {
  date: string
  sectionId: string
  blockId: string
  activityType: ActivityType | ''
  activityNote: string
  workers: string[]
  inputs: string[]
  supervisorId: string
}

export interface SectionMTDSummary {
  sectionId: string
  sectionName: string
  crop: CropType
  color: string
  mtdSpend: number
  percentage: number
  status: SectionStatus
  hectares: number
  costPerHectare: number
  lastActivity: string
}
