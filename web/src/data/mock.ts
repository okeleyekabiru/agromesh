import type {
  Section, Worker, ActivityLog, Transaction, Farm
} from '@/types'

export const mockFarm: Farm = {
  id: 'farm_001',
  name: 'Kabiru Farms Ltd.',
  location: 'Oyo State, Nigeria',
  totalHectares: 100,
  crops: ['Oil Palm', 'Maize'],
  createdAt: '2024-01-01'
}

export const mockSections: Section[] = [
  { id: 's1', farmId: 'farm_001', name: 'Section A', crop: 'Oil Palm', hectares: 18, color: '#2D7A4F', status: 'Active', lastActivityDate: 'Today', blockCount: 4 },
  { id: 's2', farmId: 'farm_001', name: 'Section B', crop: 'Oil Palm', hectares: 16, color: '#3DAA6A', status: 'Active', lastActivityDate: 'Today', blockCount: 4 },
  { id: 's3', farmId: 'farm_001', name: 'Section C', crop: 'Maize',    hectares: 14, color: '#E8A838', status: 'Harvest Soon', lastActivityDate: 'Yesterday', blockCount: 3 },
  { id: 's4', farmId: 'farm_001', name: 'Section D', crop: 'Mixed',    hectares: 20, color: '#6B7C72', status: 'Idle', lastActivityDate: '3 days ago', blockCount: 5 },
  { id: 's5', farmId: 'farm_001', name: 'Section E', crop: 'Oil Palm', hectares: 16, color: '#1A4A32', status: 'Active', lastActivityDate: 'Today', blockCount: 4 },
  { id: 's6', farmId: 'farm_001', name: 'Section F', crop: 'Maize',    hectares: 16, color: '#8B4513', status: 'Idle', lastActivityDate: '5 days ago', blockCount: 4 },
]

export const mockWorkers: Worker[] = [
  { id: 'w1', farmId: 'farm_001', name: 'Ibrahim Musa',    initials: 'IM', role: 'Field Supervisor',       type: 'Employee',   active: true, dailyRate: 3500 },
  { id: 'w2', farmId: 'farm_001', name: 'Tunde Adeyemi',   initials: 'TA', role: 'Assistant Supervisor',   type: 'Employee',   active: true, dailyRate: 2800 },
  { id: 'w3', farmId: 'farm_001', name: 'Emeka Johnson',   initials: 'EJ', role: 'Farm Worker',            type: 'Employee',   active: true, dailyRate: 2200 },
  { id: 'w4', farmId: 'farm_001', name: 'Fatima Bello',    initials: 'FB', role: 'Farm Worker',            type: 'Employee',   active: true, dailyRate: 2200 },
  { id: 'w5', farmId: 'farm_001', name: 'Chukwu Obi',      initials: 'CO', role: 'Contractor',             type: 'Contractor', active: true, dailyRate: 4000 },
  { id: 'w6', farmId: 'farm_001', name: 'Musa Garba',      initials: 'MG', role: 'Contractor',             type: 'Contractor', active: true, dailyRate: 3800 },
]

export const mockActivities: ActivityLog[] = [
  {
    id: 'a1', farmId: 'farm_001', sectionId: 's1', blockId: 'b2',
    sectionName: 'Section A', blockName: 'Block B',
    activityType: 'Weeding', date: 'Today',
    workers: [{ workerId: 'w1', name: 'Ibrahim Musa', type: 'Employee', hoursWorked: 8 }],
    inputs: [{ item: 'Brushcutter', quantity: 4, unit: 'units' }, { item: 'Diesel', quantity: 12, unit: 'L' }],
    notes: '', supervisorId: 'w1', supervisorName: 'Ibrahim M.',
    labourCost: 28000, inputCost: 19200, totalCost: 47200,
    createdOffline: false, syncedAt: '2025-06-18T08:30:00Z', icon: '🌿'
  },
  {
    id: 'a2', farmId: 'farm_001', sectionId: 's2', blockId: 'b1',
    sectionName: 'Section B', blockName: 'Block A',
    activityType: 'Fertilizer Application', date: 'Today',
    workers: [{ workerId: 'w2', name: 'Tunde Adeyemi', type: 'Employee', hoursWorked: 7 }],
    inputs: [{ item: 'NPK 15-15-15', quantity: 200, unit: 'kg' }],
    notes: 'Applied to northern blocks only', supervisorId: 'w2', supervisorName: 'Tunde A.',
    labourCost: 14000, inputCost: 24500, totalCost: 38500,
    createdOffline: false, syncedAt: '2025-06-18T07:15:00Z', icon: '🌱'
  },
  {
    id: 'a3', farmId: 'farm_001', sectionId: 's3', blockId: 'b3',
    sectionName: 'Section C', blockName: 'Block C',
    activityType: 'Maintenance', date: 'Yesterday',
    workers: [{ workerId: 'w3', name: 'Emeka Johnson', type: 'Employee', hoursWorked: 6 }],
    inputs: [{ item: 'Diesel', quantity: 12, unit: 'L' }, { item: 'Spare Parts', quantity: 1, unit: 'units' }],
    notes: '', supervisorId: 'w1', supervisorName: 'Ibrahim M.',
    labourCost: 13200, inputCost: 8800, totalCost: 22000,
    createdOffline: false, syncedAt: '2025-06-17T14:00:00Z', icon: '🔧'
  },
  {
    id: 'a4', farmId: 'farm_001', sectionId: 's1', blockId: 'b4',
    sectionName: 'Section A', blockName: 'Block D',
    activityType: 'Spraying', date: 'Yesterday',
    workers: [{ workerId: 'w4', name: 'Fatima Bello', type: 'Employee', hoursWorked: 7 }],
    inputs: [{ item: 'Glyphosate', quantity: 8, unit: 'L' }],
    notes: '', supervisorId: 'w2', supervisorName: 'Tunde A.',
    labourCost: 15400, inputCost: 16200, totalCost: 31600,
    createdOffline: false, syncedAt: '2025-06-17T09:00:00Z', icon: '💧'
  },
  {
    id: 'a5', farmId: 'farm_001', sectionId: 's3', blockId: 'b1',
    sectionName: 'Section C', blockName: 'Block A',
    activityType: 'Harvesting', date: 'Jun 17',
    workers: [
      { workerId: 'w1', name: 'Ibrahim Musa', type: 'Employee', hoursWorked: 8 },
      { workerId: 'w5', name: 'Chukwu Obi', type: 'Contractor', hoursWorked: 8 },
    ],
    inputs: [{ item: 'Harvesting Tools', quantity: 12, unit: 'units' }],
    notes: '', supervisorId: 'w1', supervisorName: 'Ibrahim M.',
    labourCost: 60000, inputCost: 24000, totalCost: 84000,
    createdOffline: false, syncedAt: '2025-06-17T07:30:00Z', icon: '🌾'
  },
]

export const mockTransactions: Transaction[] = [
  {
    id: 't1', farmId: 'farm_001', monoId: 'mono_001',
    narration: 'SARO AGRO NIG LTD', displayName: 'SARO AGRO CHEMICALS',
    amount: 185000, date: 'Jun 18',
    category: 'Fertilizer', cropAllocation: { 'Oil Palm': 70, 'Maize': 30 },
    confidence: 'high', categorizationSource: 'dictionary',
    reviewedByUser: false, icon: '🌿', iconBg: 'rgba(45,122,79,0.1)'
  },
  {
    id: 't2', farmId: 'farm_001', monoId: 'mono_002',
    narration: 'CONOIL PLC', displayName: 'CONOIL FUEL PAYMENT',
    amount: 64500, date: 'Jun 17',
    category: 'Fuel', cropAllocation: { 'Oil Palm': 60, 'Maize': 40 },
    confidence: 'high', categorizationSource: 'dictionary',
    reviewedByUser: false, icon: '⛽', iconBg: 'rgba(232,168,56,0.1)'
  },
  {
    id: 't3', farmId: 'farm_001', monoId: 'mono_003',
    narration: 'TRF TO EMEKA J', displayName: 'PAYMENT TO EMEKA',
    amount: 45000, date: 'Jun 17',
    category: 'Labour', cropAllocation: { 'Oil Palm': 60, 'Maize': 40 },
    confidence: 'medium', categorizationSource: 'llm',
    reviewedByUser: false, icon: '👤', iconBg: 'rgba(59,130,246,0.1)'
  },
  {
    id: 't4', farmId: 'farm_001', monoId: 'mono_004',
    narration: 'KINGSWAY STORES LTD', displayName: 'KINGSWAY STORES',
    amount: 28000, date: 'Jun 16',
    category: 'Other', cropAllocation: {},
    confidence: 'low', categorizationSource: 'llm',
    reviewedByUser: false, icon: '❓', iconBg: 'rgba(220,60,60,0.08)'
  },
  {
    id: 't5', farmId: 'farm_001', monoId: 'mono_005',
    narration: 'NOTORE CHEM NIG', displayName: 'NOTORE CHEMICAL IND',
    amount: 220000, date: 'Jun 15',
    category: 'Fertilizer', cropAllocation: { 'Oil Palm': 70, 'Maize': 30 },
    confidence: 'high', categorizationSource: 'dictionary',
    reviewedByUser: false, icon: '🌿', iconBg: 'rgba(45,122,79,0.1)'
  },
  {
    id: 't6', farmId: 'farm_001', monoId: 'mono_006',
    narration: 'PAYROLL BATCH JUN', displayName: 'LABOUR PAYMENT JUN W2',
    amount: 312000, date: 'Jun 14',
    category: 'Labour', cropAllocation: { 'Oil Palm': 65, 'Maize': 35 },
    confidence: 'high', categorizationSource: 'dictionary',
    reviewedByUser: false, icon: '👥', iconBg: 'rgba(59,130,246,0.1)'
  },
  {
    id: 't7', farmId: 'farm_001', monoId: 'mono_007',
    narration: 'JOHN HOLT PLC', displayName: 'JOHN HOLT EQUIPMENT',
    amount: 89500, date: 'Jun 13',
    category: 'Equipment', cropAllocation: { 'Oil Palm': 100, 'Maize': 0 },
    confidence: 'high', categorizationSource: 'dictionary',
    reviewedByUser: false, icon: '🔧', iconBg: 'rgba(107,124,114,0.1)'
  },
  {
    id: 't8', farmId: 'farm_001', monoId: 'mono_008',
    narration: 'TRF IBRAHIM MUSA', displayName: 'TRANSFER TO IBRAHIM',
    amount: 77000, date: 'Jun 12',
    category: 'Labour', cropAllocation: { 'Oil Palm': 65, 'Maize': 35 },
    confidence: 'medium', categorizationSource: 'llm',
    reviewedByUser: false, icon: '👤', iconBg: 'rgba(59,130,246,0.1)'
  },
]
