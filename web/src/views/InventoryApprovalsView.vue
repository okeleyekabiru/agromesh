<template>
  <div>
    <!-- KPI -->
    <div class="kpi-grid">
      <KpiCard
        label="Awaiting Approval"
        :value="`${store.pendingAdjustmentsCount}`"
        :delta="store.pendingAdjustmentsCount > 0 ? 'Adjustments and prices' : 'All clear'"
        :deltaType="store.pendingAdjustmentsCount > 0 ? 'warn' : 'up'"
      />
    </div>

    <AlertBanner v-if="!isAdmin">
      You can view what is waiting, but only an admin can approve or reject stock adjustments and price changes.
    </AlertBanner>

    <!-- Price changes: a price decides the cost of every activity that uses the item -->
    <div v-if="store.pendingPriceChanges.length" class="card" style="margin-bottom:16px;">
      <div class="inv-header">
        <div class="card-title">Pending Price Changes</div>
        <div class="card-label">{{ store.pendingPriceChanges.length }} awaiting review</div>
      </div>
      <div class="approval-row" v-for="req in store.pendingPriceChanges" :key="req.id">
        <div class="approval-info">
          <div class="approval-item-name">{{ req.inventoryItemName }}</div>
          <div class="approval-meta">
            <span class="approval-qty">
              {{ req.currentUnitCost ? `₦${fmt(req.currentUnitCost * req.currentPackSize)} / ${req.currentPackLabel}` : 'no price' }}
              → ₦{{ fmt(req.requestedPricePerPack) }} / {{ req.requestedPackLabel }}
            </span>
          </div>
          <div class="approval-notes">
            ₦{{ fmt(req.requestedPricePerUnit) }} per {{ req.unit }} · {{ req.requestedPackSize }} {{ req.unit }} per {{ req.requestedPackLabel }}
            <template v-if="req.backfillActivities"> · will also cost past activities that used it</template>
          </div>
        </div>
        <div v-if="actionError[req.id]" class="error-msg">{{ actionError[req.id] }}</div>
        <div v-if="isAdmin" class="approval-actions">
          <button class="btn btn-ghost btn-sm" @click="rejectPrice(req.id)" :disabled="!!busy[req.id]">
            {{ busy[req.id] === 'reject' ? 'Rejecting…' : 'Reject' }}
          </button>
          <button class="btn btn-primary btn-sm" @click="approvePrice(req.id)" :disabled="!!busy[req.id]">
            {{ busy[req.id] === 'approve' ? 'Approving…' : 'Approve' }}
          </button>
        </div>
      </div>
    </div>

    <!-- Queue -->
    <div v-if="loading" class="loading-state">Loading pending adjustments…</div>
    <div v-else-if="!store.pendingAdjustments.length && !store.pendingPriceChanges.length" class="card" style="padding:40px;text-align:center;">
      <div style="font-size:32px;margin-bottom:12px;">✅</div>
      <div style="font-family:'Syne',sans-serif;font-weight:700;font-size:16px;color:var(--forest);margin-bottom:4px;">No pending adjustments</div>
      <div style="font-size:12px;color:var(--stone);">Stock adjustment requests will show up here for review.</div>
    </div>
    <div v-else-if="store.pendingAdjustments.length" class="card">
      <div class="inv-header">
        <div class="card-title">Pending Stock Adjustments</div>
        <div class="card-label">{{ store.pendingAdjustments.length }} awaiting review</div>
      </div>
      <div class="approval-row" v-for="req in store.pendingAdjustments" :key="req.id">
        <div class="approval-info">
          <div class="approval-item-name">{{ req.inventoryItemName }}</div>
          <div class="approval-meta">
            <span class="approval-qty">{{ req.currentStock }} {{ req.unit }} → {{ req.requestedQuantity }} {{ req.unit }}</span>
          </div>
          <div class="approval-notes">"{{ req.notes }}"</div>
        </div>
        <div v-if="actionError[req.id]" class="error-msg">{{ actionError[req.id] }}</div>
        <div v-if="isAdmin" class="approval-actions">
          <button class="btn btn-ghost btn-sm" @click="reject(req.id)" :disabled="!!busy[req.id]">
            {{ busy[req.id] === 'reject' ? 'Rejecting…' : 'Reject' }}
          </button>
          <button class="btn btn-primary btn-sm" @click="approve(req.id)" :disabled="!!busy[req.id]">
            {{ busy[req.id] === 'approve' ? 'Approving…' : 'Approve' }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { useAgroStore } from '@/stores/agro'
import KpiCard from '@/components/ui/KpiCard.vue'
import AlertBanner from '@/components/ui/AlertBanner.vue'
import { extractErrorMessage } from '@/utils/http'

const store = useAgroStore()
const loading = ref(false)

// UX hint only — hides the buttons for a non-admin session. The real security boundary is the
// backend's own role check (403 on approve/reject); this is not what makes the app safe.
const isAdmin = computed(() => store.currentUser?.role === 'Admin')

const busy = reactive<Record<string, 'approve' | 'reject' | undefined>>({})
const actionError = reactive<Record<string, string>>({})

async function approve(requestId: string) {
  busy[requestId] = 'approve'
  actionError[requestId] = ''
  try {
    await store.approveStockAdjustment(requestId)
  } catch (e) {
    actionError[requestId] = extractErrorMessage(e, 'Could not approve this request. Please try again.')
  } finally {
    busy[requestId] = undefined
  }
}

const fmt = (n: number) => n.toLocaleString('en-NG', { maximumFractionDigits: 2 })

async function approvePrice(requestId: string) {
  busy[requestId] = 'approve'
  actionError[requestId] = ''
  try {
    await store.approvePriceChange(requestId)
  } catch (e) {
    actionError[requestId] = extractErrorMessage(e, 'Could not approve this price. Please try again.')
  } finally {
    busy[requestId] = undefined
  }
}

async function rejectPrice(requestId: string) {
  busy[requestId] = 'reject'
  actionError[requestId] = ''
  try {
    await store.rejectPriceChange(requestId)
  } catch (e) {
    actionError[requestId] = extractErrorMessage(e, 'Could not reject this price. Please try again.')
  } finally {
    busy[requestId] = undefined
  }
}

async function reject(requestId: string) {
  busy[requestId] = 'reject'
  actionError[requestId] = ''
  try {
    await store.rejectStockAdjustment(requestId)
  } catch (e) {
    actionError[requestId] = extractErrorMessage(e, 'Could not reject this request. Please try again.')
  } finally {
    busy[requestId] = undefined
  }
}

onMounted(async () => {
  loading.value = true
  try {
    await Promise.all([store.fetchPendingAdjustments(), store.fetchPendingPriceChanges()])
  } finally {
    loading.value = false
  }
})
</script>

<style scoped>
.kpi-grid { display: grid; grid-template-columns: repeat(1,1fr); gap: 12px; margin-bottom: 16px; max-width: 280px; }
.loading-state { text-align: center; padding: 60px; color: var(--stone); font-size: 13px; }
.inv-header { padding: 16px 20px 12px; border-bottom: 1px solid var(--fog); display: flex; align-items: center; justify-content: space-between; }
.approval-row { display: flex; align-items: center; gap: 14px; padding: 14px 20px; border-bottom: 1px solid var(--fog); flex-wrap: wrap; }
.approval-row:last-child { border-bottom: none; }
.approval-info { flex: 1; min-width: 220px; }
.approval-item-name { font-size: 13px; font-weight: 600; color: var(--forest); }
.approval-meta { margin-top: 2px; }
.approval-qty { font-family: 'JetBrains Mono', monospace; font-size: 12px; color: var(--forest); }
.approval-notes { margin-top: 4px; font-size: 11.5px; color: var(--stone); font-style: italic; }
.approval-actions { display: flex; gap: 6px; flex-shrink: 0; }
.error-msg { font-size: 11.5px; color: var(--alert); line-height: 1.5; }
</style>
