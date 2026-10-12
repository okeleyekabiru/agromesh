<template>
  <div class="modal-overlay" @click.self="$emit('close')">
    <div class="modal" role="dialog" aria-modal="true" aria-label="Activity details">
      <div class="modal-header">
        <div>
          <div class="modal-title">{{ activityTypeLabel(activity.activityType) }}</div>
          <div class="modal-sub">{{ formatLongDate(activity.date) }} · {{ blockName }}</div>
        </div>
        <button class="modal-close" @click="$emit('close')" aria-label="Close">×</button>
      </div>

      <div class="modal-body">
        <div v-if="loadError" class="error-msg">{{ loadError }}</div>

        <div class="cost-grid">
          <div class="cost-cell"><div class="cost-label">Labour</div><div class="cost-val">₦{{ naira(activity.labourCost) }}</div></div>
          <div class="cost-cell"><div class="cost-label">Inputs</div><div class="cost-val">₦{{ naira(activity.inputCost) }}</div></div>
          <div class="cost-cell total"><div class="cost-label">Total</div><div class="cost-val">₦{{ naira(activity.labourCost + activity.inputCost) }}</div></div>
        </div>

        <div class="detail-row"><span class="k">Block</span><span class="v">{{ blockName }}</span></div>
        <div class="detail-row"><span class="k">Supervisor</span><span class="v">{{ supervisorName }}</span></div>
        <div v-if="activity.workedArea" class="detail-row">
          <span class="k">Area worked</span>
          <span class="v">{{ activity.workedArea.displayValue }} {{ activity.workedArea.displayUnit }}</span>
        </div>
        <div v-if="activity.chemicalProductName" class="detail-row"><span class="k">Chemical</span><span class="v">{{ activity.chemicalProductName }}</span></div>
        <div class="detail-row">
          <span class="k">Status</span>
          <span class="v">{{ activity.syncedAt ? `Synced ${formatLongDate(activity.syncedAt)}` : activity.createdOffline ? 'Logged offline — awaiting sync' : 'Saved' }}</span>
        </div>

        <div class="section-title">Workers ({{ activity.workers.length }})</div>
        <div v-if="!activity.workers.length" class="empty">No workers recorded</div>
        <div v-for="w in activity.workers" :key="w.workerId" class="line">
          <div class="line-main">
            <div class="line-name">{{ w.workerName }}</div>
            <div class="line-sub">
              {{ w.workerType }} · {{ w.hoursWorked }}h ·
              ₦{{ naira(w.payRate) }} / {{ PAY_FREQUENCY_LABELS[w.payFrequency]?.toLowerCase() ?? 'day' }}
            </div>
          </div>
          <div class="line-amt">₦{{ naira(workerCost(w)) }}</div>
        </div>

        <div class="section-title">Inputs used ({{ activity.inputs.length }})</div>
        <div v-if="!activity.inputs.length" class="empty">No inputs recorded</div>
        <div v-for="(i, idx) in activity.inputs" :key="idx" class="line">
          <div class="line-main"><div class="line-name">{{ i.item }}</div></div>
          <div class="line-amt">{{ i.quantity }} {{ i.unit }}</div>
        </div>

        <template v-if="activity.notes">
          <div class="section-title">Notes</div>
          <div class="notes">{{ activity.notes }}</div>
        </template>
      </div>

      <div class="modal-footer">
        <button class="btn btn-ghost" @click="$emit('close')">Close</button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useAgroStore } from '@/stores/agro'
import { PAY_FREQUENCY_LABELS, type ActivityLog, type WorkerLogDto } from '@/types'
import { activityTypeLabel } from '@/utils/enums'
import { extractErrorMessage } from '@/utils/http'

const props = defineProps<{ activity: ActivityLog }>()
defineEmits(['close'])
const store = useAgroStore()

// Show the row's data immediately, then swap in the freshly fetched copy.
const activity = ref<ActivityLog>(props.activity)
const loadError = ref('')

onMounted(async () => {
  try {
    activity.value = await store.fetchActivity(props.activity.id)
  } catch (e) {
    loadError.value = extractErrorMessage(e, 'Could not refresh this activity — showing the saved copy.')
  }
  if (!store.blocks.length) store.fetchBlocks()
  if (!store.workers.length) store.fetchWorkers()
  store.fetchManagers()
})

const blockName = computed(() => store.blocks.find(b => b.id === activity.value.blockId)?.name ?? 'Unknown block')
const supervisorName = computed(() => store.supervisorName(activity.value.supervisorId) || '—')

// Mirrors the backend's PayRateConverter.ToDailyEquivalent (6-day week, 26-day month).
function dailyEquivalent(rate: number, freq: number) {
  if (freq === 1) return Math.round((rate / 6) * 100) / 100
  if (freq === 2) return Math.round((rate / 26) * 100) / 100
  return rate
}
function workerCost(w: WorkerLogDto) {
  return dailyEquivalent(w.payRate, w.payFrequency) * (w.hoursWorked / 8)
}

function naira(n: number) {
  return Math.round(n).toLocaleString()
}
function formatLongDate(iso: string) {
  return new Date(iso).toLocaleDateString('en-NG', { day: 'numeric', month: 'short', year: 'numeric' })
}
</script>

<style scoped>
.modal-overlay { position: fixed; inset: 0; background: rgba(10,26,16,0.6); backdrop-filter: blur(4px); z-index: 200; display: flex; align-items: center; justify-content: center; padding: 16px; }
.modal { background: white; border-radius: 16px; width: 480px; max-width: 100%; max-height: calc(100vh - 32px); display: flex; flex-direction: column; box-shadow: 0 24px 64px rgba(10,26,16,0.2); }
.modal-header { padding: 20px 24px 16px; border-bottom: 1px solid var(--fog); display: flex; align-items: flex-start; justify-content: space-between; gap: 12px; }
.modal-title { font-family: 'Syne', sans-serif; font-weight: 700; font-size: 16px; color: var(--forest); }
.modal-sub { font-family: 'JetBrains Mono', monospace; font-size: 10px; color: var(--stone); margin-top: 3px; }
.modal-close { width: 28px; height: 28px; border-radius: 6px; border: 1px solid var(--fog); background: transparent; cursor: pointer; font-size: 18px; color: var(--stone); flex-shrink: 0; }
.modal-body { padding: 16px 24px; overflow-y: auto; }
.modal-footer { padding: 14px 24px; border-top: 1px solid var(--fog); display: flex; justify-content: flex-end; }
.error-msg { font-size: 11.5px; color: var(--alert); margin-bottom: 10px; }
.cost-grid { display: grid; grid-template-columns: repeat(3, 1fr); gap: 8px; margin-bottom: 14px; }
.cost-cell { background: var(--mist); border-radius: 8px; padding: 10px 12px; }
.cost-cell.total { background: var(--canopy); }
.cost-label { font-family: 'JetBrains Mono', monospace; font-size: 9px; letter-spacing: 0.08em; text-transform: uppercase; color: var(--stone); }
.cost-val { font-family: 'Syne', sans-serif; font-weight: 700; font-size: 14px; color: var(--forest); margin-top: 3px; }
.detail-row { display: flex; justify-content: space-between; gap: 12px; padding: 7px 0; border-bottom: 1px solid var(--fog); font-size: 12px; }
.k { font-family: 'JetBrains Mono', monospace; font-size: 9px; letter-spacing: 0.08em; text-transform: uppercase; color: var(--stone); align-self: center; }
.v { color: var(--forest); font-weight: 500; text-align: right; }
.section-title { font-family: 'Syne', sans-serif; font-weight: 700; font-size: 12px; color: var(--forest); margin: 16px 0 6px; }
.line { display: flex; justify-content: space-between; align-items: center; gap: 12px; padding: 8px 0; border-bottom: 1px solid var(--fog); }
.line:last-of-type { border-bottom: none; }
.line-name { font-size: 12px; font-weight: 600; color: var(--forest); }
.line-sub { font-family: 'JetBrains Mono', monospace; font-size: 9px; color: var(--stone); margin-top: 2px; }
.line-amt { font-family: 'JetBrains Mono', monospace; font-size: 12px; font-weight: 600; color: var(--forest); white-space: nowrap; }
.empty { font-size: 11.5px; color: var(--stone); padding: 4px 0; }
.notes { font-size: 12px; color: var(--forest); line-height: 1.55; white-space: pre-wrap; background: var(--mist); border-radius: 8px; padding: 10px 12px; }
</style>
