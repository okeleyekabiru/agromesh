import type { ActivityLog, ActivityType, Block, Transaction, UpcomingTask, WorkerType } from '@/types'

// The API serialises enums as ordinals (no string-enum converter is configured), but the UI
// compares and displays them by name. These must mirror the backend enum declaration order.
const ACTIVITY_TYPES: ActivityType[] = [
  'Weeding', 'Spraying', 'FertilizerApplication', 'Maintenance',
  'Harvesting', 'Clearing', 'Pruning', 'PestControl',
]
const WORKER_TYPES: WorkerType[] = ['Employee', 'Contractor']

export const TRANSACTION_CATEGORIES = [
  'Labour', 'Fertilizer', 'Chemicals', 'Fuel', 'Equipment', 'Maintenance', 'Transportation', 'Seeds', 'Sales', 'Other',
] as const
const CONFIDENCE = ['High', 'Medium', 'Low']
const SOURCES = ['Dictionary', 'Pattern', 'Llm', 'Manual']

function nameOf<T extends string>(value: unknown, names: T[]): T {
  if (typeof value === 'number') return names[value] ?? (String(value) as T)
  return value as T
}

/** Accepts an activity as sent by the API (ordinal enums) or already normalised. */
export function normalizeActivity(a: ActivityLog): ActivityLog {
  return {
    ...a,
    activityType: nameOf(a.activityType, ACTIVITY_TYPES),
    workers: (a.workers ?? []).map(w => ({ ...w, workerType: nameOf(w.workerType, WORKER_TYPES) })),
  }
}

const CROP_NAMES = ['OilPalm', 'Maize', 'Mixed', 'Cocoa', 'Cassava']

/** A block as sent by the API (ordinal crop types and task activity types) with names the UI can compare. */
export function normalizeBlock(b: Block): Block {
  return {
    ...b,
    crops: (b.crops ?? []).map(c => ({ ...c, cropType: nameOf(c.cropType, CROP_NAMES) as Block['crops'][number]['cropType'] })),
    nextWeedingPrediction: normalizeTask(b.nextWeedingPrediction),
    nextSprayingPrediction: normalizeTask(b.nextSprayingPrediction),
    nextFertilizingPrediction: normalizeTask(b.nextFertilizingPrediction),
  }
}

/** Upcoming tasks also carry the activity type as an ordinal. */
export function normalizeTask<T extends UpcomingTask | null | undefined>(t: T): T {
  if (!t) return t
  return { ...t, activityType: nameOf(t.activityType, ACTIVITY_TYPES) } as T
}

/** Index of a category name, which is what the API expects when a transaction is categorised. */
export function categoryOrdinal(name: string): number {
  return (TRANSACTION_CATEGORIES as readonly string[]).indexOf(name)
}

/** Accepts a transaction as sent by the API (ordinal enums) or already normalised. */
export function normalizeTransaction(t: Transaction): Transaction {
  return {
    ...t,
    category: nameOf(t.category, [...TRANSACTION_CATEGORIES]),
    confidence: nameOf(t.confidence, CONFIDENCE as Transaction['confidence'][]),
    source: nameOf(t.source, SOURCES as Transaction['source'][]),
    kind: nameOf(t.kind as unknown, ['Expense', 'Income', 'Capital']),
    reconciliation: nameOf(t.reconciliation as unknown, ['None', 'Matched', 'NeedsReview', 'Dismissed']),
  }
}

const SPLIT_LABELS: Record<string, string> = {
  FertilizerApplication: 'Fertilizer Application',
  PestControl: 'Pest Control',
}
export function activityTypeLabel(t: string): string {
  return SPLIT_LABELS[t] ?? t
}
