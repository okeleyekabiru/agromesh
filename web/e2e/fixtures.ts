// Shared API response fixtures used across all tests

export const FARM = {
  id: 'farm_001',
  name: 'Kabiru Farms Ltd.',
  location: 'Oyo State, Nigeria',
  area: { hectares: 45, acres: 111.2, displayValue: '111.2', displayUnit: 'ac' },
  ownerId: 'owner_001',
  preferredAreaUnit: 'Acres',
  centroidLat: 7.9,
  centroidLng: 3.9,
  sectionCount: 4,
  workerCount: 5,
}

export const SECTIONS = [
  {
    id: 'sec_a', farmId: 'farm_001', name: 'Section A',
    crops: [{ cropType: 'OilPalm', area: { hectares: 18, acres: 44.5, displayValue: '44.5', displayUnit: 'ac' }, percentage: 100 }],
    area: { hectares: 18, acres: 44.5, displayValue: '44.5', displayUnit: 'ac' },
    status: 'Active', lastActivityDate: '2025-06-18T00:00:00Z',
    boundaryGeoJson: null, centroidLat: 7.9, centroidLng: 3.9,
    blocks: [
      { id: 'blk_a1', sectionId: 'sec_a', name: 'Block A', area: { hectares: 4.5, acres: 11.1, displayValue: '11.1', displayUnit: 'ac' }, treeCount: 200 },
      { id: 'blk_a2', sectionId: 'sec_a', name: 'Block B', area: { hectares: 4.5, acres: 11.1, displayValue: '11.1', displayUnit: 'ac' }, treeCount: 200 },
    ],
    nextWeedingPrediction: { sectionId: 'sec_a', sectionName: 'Section A', activityType: 'Weeding', lastDone: '2025-06-01T00:00:00Z', predictedNextDue: '2025-07-05T00:00:00Z', daysUntilDue: 5, reasoning: 'Peak rains season, weeds growing fast', recommendedChemical: null, recommendedSprayDate: null, confidence: 'High', stockAvailable: null, stockUnit: null, isStale: false, weatherDataUsed: true },
    nextSprayingPrediction: null,
  },
  {
    id: 'sec_d', farmId: 'farm_001', name: 'Section D',
    crops: [
      { cropType: 'OilPalm', area: { hectares: 10, acres: 24.7, displayValue: '24.7', displayUnit: 'ac' }, percentage: 50 },
      { cropType: 'Maize',   area: { hectares: 10, acres: 24.7, displayValue: '24.7', displayUnit: 'ac' }, percentage: 50 },
    ],
    area: { hectares: 20, acres: 49.4, displayValue: '49.4', displayUnit: 'ac' },
    status: 'Idle', lastActivityDate: null,
    boundaryGeoJson: null, centroidLat: 7.91, centroidLng: 3.91,
    blocks: [
      { id: 'blk_d1', sectionId: 'sec_d', name: 'Block A', area: { hectares: 5, acres: 12.4, displayValue: '12.4', displayUnit: 'ac' }, treeCount: null },
    ],
    nextWeedingPrediction: null,
    nextSprayingPrediction: null,
  },
]

export const WORKERS = [
  { id: 'w1', farmId: 'farm_001', name: 'Ibrahim Musa',  initials: 'IM', workerRoleId: 'r1', roleTitle: 'Field Supervisor', dailyRate: 3500, type: 'Employee', active: true, daysLoggedThisMonth: 22 },
  { id: 'w2', farmId: 'farm_001', name: 'Tunde Adeyemi', initials: 'TA', workerRoleId: 'r2', roleTitle: 'Farm Worker',       dailyRate: 2200, type: 'Employee', active: true, daysLoggedThisMonth: 20 },
]

export const WORKER_ROLES = [
  { id: 'r1', farmId: 'farm_001', title: 'Field Supervisor', defaultType: 'Employee', dailyRate: 3500, workerCount: 1 },
  { id: 'r2', farmId: 'farm_001', title: 'Farm Worker',       defaultType: 'Employee', dailyRate: 2200, workerCount: 3 },
  { id: 'r3', farmId: 'farm_001', title: 'Contractor',        defaultType: 'Contractor', dailyRate: 4000, workerCount: 1 },
]

export const ACTIVITIES = {
  items: [
    {
      id: 'a1', farmId: 'farm_001', sectionId: 'sec_a', blockId: 'blk_a1',
      activityType: 'Weeding', date: '2025-06-18T08:00:00Z', notes: null,
      supervisorId: 'w1', labourCost: 28000, inputCost: 4800,
      createdOffline: false, syncedAt: '2025-06-18T08:30:00Z',
      workedAreaGeoJson: null, workedArea: null,
      chemicalProductId: null, chemicalProductName: null,
      workers: [{ workerId: 'w1', workerName: 'Ibrahim Musa', workerType: 'Employee', hoursWorked: 8, dailyRate: 3500 }],
      inputs: [{ item: 'Diesel', quantity: 12, unit: 'L' }],
    },
    {
      id: 'a2', farmId: 'farm_001', sectionId: 'sec_a', blockId: 'blk_a2',
      activityType: 'Spraying', date: '2025-06-17T07:00:00Z', notes: 'Applied glyphosate',
      supervisorId: 'w2', labourCost: 15400, inputCost: 16200,
      createdOffline: false, syncedAt: '2025-06-17T09:00:00Z',
      workedAreaGeoJson: null, workedArea: null,
      chemicalProductId: 'chem_1', chemicalProductName: 'Glyphosate 360',
      workers: [{ workerId: 'w2', workerName: 'Tunde Adeyemi', workerType: 'Employee', hoursWorked: 7, dailyRate: 2200 }],
      inputs: [{ item: 'Glyphosate 360', quantity: 15, unit: 'L' }],
    },
  ],
  total: 2, page: 1, pageSize: 20,
}

export const TRANSACTIONS = {
  items: [
    { id: 't1', farmId: 'farm_001', monoId: 'mono_001', narration: 'SARO AGRO NIG LTD', amount: 185000, date: '2025-06-18T00:00:00Z', category: 'Fertilizer', cropAllocation: { 'OilPalm': 70, 'Maize': 30 }, confidence: 'High', source: 'Dictionary', reviewedByUser: false, reviewNotes: null },
    { id: 't2', farmId: 'farm_001', monoId: 'mono_002', narration: 'CONOIL PLC', amount: 64500, date: '2025-06-17T00:00:00Z', category: 'Fuel', cropAllocation: { 'OilPalm': 60, 'Maize': 40 }, confidence: 'High', source: 'Pattern', reviewedByUser: true, reviewNotes: null },
  ],
  total: 2, page: 1, pageSize: 20,
}

export const INVENTORY = [
  { id: 'inv_1', farmId: 'farm_001', name: 'Glyphosate 360', unit: 'L', category: 'Chemical', currentStock: 105, reorderThreshold: 30, isLowStock: false, chemicalProductId: 'chem_1', chemicalProductName: 'Glyphosate 360', lastMovementDate: '2025-06-17T09:00:00Z' },
  { id: 'inv_2', farmId: 'farm_001', name: 'NPK 15-15-15',   unit: 'kg', category: 'Fertilizer', currentStock: 2500, reorderThreshold: 500, isLowStock: false, chemicalProductId: null, chemicalProductName: null, lastMovementDate: '2025-06-10T00:00:00Z' },
  { id: 'inv_3', farmId: 'farm_001', name: 'Diesel',         unit: 'L', category: 'Fuel', currentStock: 85, reorderThreshold: 100, isLowStock: true, chemicalProductId: null, chemicalProductName: null, lastMovementDate: '2025-06-18T08:30:00Z' },
]

export const CHEMICALS = [
  { id: 'chem_1', name: 'Glyphosate 360', manufacturer: 'Saro Agro', chemType: 'Herbicide', typicalIntervalDays: 21, active: true },
  { id: 'chem_2', name: 'Atrazine 500SC', manufacturer: null, chemType: 'Herbicide', typicalIntervalDays: 30, active: true },
]

export const DASHBOARD = {
  farmId: 'farm_001',
  totalSpendMtd: 1540000,
  labourCostMtd: 677600,
  activitiesThisMonth: 12,
  pendingReviewCount: 1,
  sectionsMtd: [
    { sectionId: 'sec_a', sectionName: 'Section A', cost: 612400, activityCount: 8 },
    { sectionId: 'sec_d', sectionName: 'Section D', cost: 284500, activityCount: 4 },
  ],
  cropPnl: [
    { cropType: 'OilPalm', revenue: 500000, cost: 1120000, netProfit: -620000 },
    { cropType: 'Maize',   revenue: 200000, cost: 420000,  netProfit: -220000 },
  ],
  recentActivities: ACTIVITIES.items,
  upcomingTasks: [
    { sectionId: 'sec_a', sectionName: 'Section A', activityType: 'Weeding', lastDone: '2025-06-01T00:00:00Z', predictedNextDue: '2025-07-05T00:00:00Z', daysUntilDue: 5, reasoning: 'Peak rains season, high weed pressure', recommendedChemical: null, recommendedSprayDate: null, confidence: 'High', stockAvailable: null, stockUnit: null, isStale: false, weatherDataUsed: true },
    { sectionId: 'sec_d', sectionName: 'Section D', activityType: 'Spraying', lastDone: null, predictedNextDue: '2025-07-08T00:00:00Z', daysUntilDue: 8, reasoning: 'No recent spray, dry window approaching', recommendedChemical: 'Glyphosate 360', recommendedSprayDate: '2025-07-06T00:00:00Z', confidence: 'Medium', stockAvailable: 105, stockUnit: 'L', isStale: false, weatherDataUsed: true },
  ],
  lowStockAlerts: [
    { inventoryItemId: 'inv_3', inventoryItemName: 'Diesel', currentStock: 85, unit: 'L', reorderThreshold: 100 },
  ],
}

export const WEATHER = {
  latitude: 7.9, longitude: 3.9,
  currentSeason: 'Peak Rains',
  todayPrecipitationMm: 4.2,
  todayTempMaxC: 32,
  nextDryWindowStart: '2025-07-06T00:00:00Z',
  nextDryWindowEnd: '2025-07-08T00:00:00Z',
  rainfallLast30Days: 186,
}
