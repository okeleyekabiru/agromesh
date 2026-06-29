import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import type { Section, Worker, ActivityLog, Transaction, ActivityFormData } from '@/types'
import { mockSections, mockWorkers, mockActivities, mockTransactions, mockFarm } from '@/data/mock'

export const useAgroStore = defineStore('agro', () => {

  // ── STATE ──
  const farm = ref(mockFarm)
  const sections = ref<Section[]>(mockSections)
  const workers = ref<Worker[]>(mockWorkers)
  const activities = ref<ActivityLog[]>(mockActivities)
  const transactions = ref<Transaction[]>(mockTransactions)
  const isOnline = ref(true)
  const lastSynced = ref('06:02 AM')
  const pendingSync = ref(0)

  // ── COMPUTED ──
  const activeSectionsToday = computed(() =>
    sections.value.filter(s => s.lastActivityDate === 'Today').length
  )

  const totalMTDSpend = computed(() =>
    transactions.value.reduce((sum, t) => sum + t.amount, 0)
  )

  const pendingReviews = computed(() =>
    transactions.value.filter(t => t.confidence !== 'high' && !t.reviewedByUser).length
  )

  const cropPnL = computed(() => {
    const totals: Record<string, number> = {}
    const total = totalMTDSpend.value

    transactions.value.forEach(t => {
      Object.entries(t.cropAllocation).forEach(([crop, pct]) => {
        if (!totals[crop]) totals[crop] = 0
        totals[crop] += (t.amount * pct) / 100
      })
    })

    return Object.entries(totals).map(([crop, amount]) => ({
      crop,
      amount,
      percentage: Math.round((amount / total) * 100)
    }))
  })

  const sectionMTD = computed(() =>
    sections.value.map(s => {
      // In production this comes from actual transaction allocation
      const spend = Math.round(Math.random() * 500000 + 50000)
      return {
        ...s,
        mtdSpend: spend,
        costPerHectare: Math.round(spend / s.hectares),
        percentage: Math.round((spend / 612400) * 100)
      }
    })
  )

  const supervisors = computed(() =>
    workers.value.filter(w => w.role.toLowerCase().includes('supervisor'))
  )

  // ── ACTIONS ──
  function logActivity(formData: ActivityFormData) {
    const section = sections.value.find(s => s.id === formData.sectionId)
    const supervisor = workers.value.find(w => w.id === formData.supervisorId)

    if (!section || !formData.activityType) return

    const newActivity: ActivityLog = {
      id: `a${Date.now()}`,
      farmId: farm.value.id,
      sectionId: formData.sectionId,
      blockId: formData.blockId,
      sectionName: section.name,
      blockName: formData.blockId,
      activityType: formData.activityType,
      date: 'Today',
      workers: [],
      inputs: [],
      notes: formData.activityNote,
      supervisorId: formData.supervisorId,
      supervisorName: supervisor?.name || 'Unknown',
      labourCost: 0,
      inputCost: 0,
      totalCost: 0,
      createdOffline: !isOnline.value,
      syncedAt: isOnline.value ? new Date().toISOString() : null,
      icon: getActivityIcon(formData.activityType)
    }

    activities.value.unshift(newActivity)

    // Update section last activity
    const sectionIndex = sections.value.findIndex(s => s.id === formData.sectionId)
    if (sectionIndex !== -1) {
      sections.value[sectionIndex].lastActivityDate = 'Today'
      sections.value[sectionIndex].status = 'Active'
    }
  }

  function reviewTransaction(id: string, category: string) {
    const txn = transactions.value.find(t => t.id === id)
    if (txn) {
      txn.reviewedByUser = true
      txn.confidence = 'high'
    }
  }

  function getActivityIcon(type: string): string {
    const icons: Record<string, string> = {
      'Weeding': '🌿', 'Spraying': '💧', 'Fertilizer Application': '🌱',
      'Maintenance': '🔧', 'Harvesting': '🌾', 'Clearing': '🪚',
      'Pruning': '✂️', 'Pest Control': '🛡️'
    }
    return icons[type] || '📋'
  }

  return {
    farm, sections, workers, activities, transactions,
    isOnline, lastSynced, pendingSync,
    activeSectionsToday, totalMTDSpend, pendingReviews,
    cropPnL, sectionMTD, supervisors,
    logActivity, reviewTransaction
  }
})
