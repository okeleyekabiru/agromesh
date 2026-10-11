<template>
  <div v-if="store.outstandingStock.length" class="card out-card">
    <div class="card-header">
      <div class="card-title">Out on the farm</div>
      <div class="card-label">PICKED, NOT YET USED OR RETURNED</div>
    </div>

    <div v-for="o in store.outstandingStock" :key="o.pickId" class="out-row">
      <div class="out-main">
        <div class="out-name">{{ o.inventoryItemName }}</div>
        <div class="out-sub">
          {{ blockName(o.blockId) }} · picked {{ fmt(o.quantityPicked) }} {{ o.unit }} · {{ formatDate(o.pickedAt) }}
          <span v-if="o.quantityUsed > 0"> · used {{ fmt(o.quantityUsed) }}</span>
          <span v-if="o.quantityReturned > 0"> · returned {{ fmt(o.quantityReturned) }}</span>
        </div>
      </div>
      <div class="out-qty">{{ fmt(o.quantityOutstanding) }} <span class="out-unit">{{ o.unit }} left</span></div>
      <button class="btn btn-ghost btn-sm" @click="openReturn(o)">Return leftover</button>
    </div>
  </div>

  <!-- Return leftover modal -->
  <div v-if="target" class="modal-overlay" @click.self="close">
    <div class="modal" role="dialog" aria-modal="true" aria-label="Return leftover stock">
      <div class="modal-header">
        <div class="modal-title">Return leftover — {{ target.inventoryItemName }}</div>
        <button class="modal-close" @click="close" aria-label="Close">×</button>
      </div>
      <div class="modal-body">
        <div class="info">
          {{ blockName(target.blockId) }}: <strong>{{ fmt(blockOutstanding) }} {{ target.unit }}</strong> of
          {{ target.inventoryItemName }} is still out on the farm.
        </div>
        <div class="form-group">
          <label class="form-label">Quantity returning ({{ target.unit }})</label>
          <input class="form-input" type="number" min="0" :max="blockOutstanding" step="any" v-model.number="qty" @wheel.prevent />
          <button class="link-btn" type="button" @click="qty = blockOutstanding">Return everything left ({{ fmt(blockOutstanding) }} {{ target.unit }})</button>
        </div>
        <div class="form-group">
          <label class="form-label">Note <span class="opt">optional</span></label>
          <input class="form-input" v-model="notes" placeholder="e.g. Unopened bag back on the shelf" />
        </div>
        <div v-if="error" class="error-msg">{{ error }}</div>
      </div>
      <div class="modal-footer">
        <button class="btn btn-ghost" @click="close" :disabled="saving">Cancel</button>
        <button class="btn btn-primary" @click="submit" :disabled="!canSubmit || saving">
          {{ saving ? 'Returning…' : 'Return to stock' }}
        </button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useAgroStore } from '@/stores/agro'
import type { OutstandingStock } from '@/types'
import { extractErrorMessage } from '@/utils/http'

const store = useAgroStore()

onMounted(() => {
  store.fetchOutstandingStock()
  if (!store.blocks.length) store.fetchBlocks()
})

const target = ref<OutstandingStock | null>(null)
const qty = ref<number | ''>('')
const notes = ref('')
const saving = ref(false)
const error = ref('')

// A return settles by item + block, so the cap is everything still out for that pair.
const blockOutstanding = computed(() => {
  const t = target.value
  if (!t) return 0
  return store.outstandingStock
    .filter(o => o.inventoryItemId === t.inventoryItemId && o.blockId === t.blockId)
    .reduce((sum, o) => sum + o.quantityOutstanding, 0)
})

const canSubmit = computed(() => Number(qty.value) > 0 && Number(qty.value) <= blockOutstanding.value)

function openReturn(o: OutstandingStock) {
  target.value = o
  qty.value = ''
  notes.value = ''
  error.value = ''
}
function close() {
  target.value = null
}

async function submit() {
  if (!canSubmit.value || !target.value) return
  saving.value = true
  error.value = ''
  try {
    await store.returnStock(target.value.inventoryItemId, {
      quantity: Number(qty.value),
      blockId: target.value.blockId,
      notes: notes.value.trim() || null,
    })
    target.value = null
  } catch (e) {
    error.value = extractErrorMessage(e, 'Could not return this stock. Please try again.')
  } finally {
    saving.value = false
  }
}

function blockName(id: string) {
  return store.blocks.find(b => b.id === id)?.name ?? 'Unknown block'
}
function fmt(n: number) {
  return Number(n.toFixed(3)).toLocaleString()
}
function formatDate(iso: string) {
  return new Date(iso).toLocaleDateString('en-NG', { day: 'numeric', month: 'short' })
}
</script>

<style scoped>
.out-card { margin-bottom: 16px; }
.out-row { display: flex; align-items: center; gap: 12px; padding: 11px 16px; border-bottom: 1px solid var(--fog); }
.out-row:last-child { border-bottom: none; }
.out-main { flex: 1; min-width: 0; }
.out-name { font-size: 12px; font-weight: 600; color: var(--forest); }
.out-sub { font-family: 'JetBrains Mono', monospace; font-size: 9px; color: var(--stone); margin-top: 2px; }
.out-qty { font-family: 'Syne', sans-serif; font-weight: 700; font-size: 14px; color: var(--forest); white-space: nowrap; }
.out-unit { font-family: 'JetBrains Mono', monospace; font-weight: 400; font-size: 9px; color: var(--stone); }
.modal-overlay { position: fixed; inset: 0; background: rgba(10,26,16,0.6); backdrop-filter: blur(4px); z-index: 200; display: flex; align-items: center; justify-content: center; padding: 16px; }
.modal { background: white; border-radius: 16px; width: 420px; max-width: 100%; box-shadow: 0 24px 64px rgba(10,26,16,0.2); }
.modal-header { padding: 20px 24px 16px; border-bottom: 1px solid var(--fog); display: flex; align-items: center; justify-content: space-between; gap: 12px; }
.modal-title { font-family: 'Syne', sans-serif; font-weight: 700; font-size: 15px; color: var(--forest); }
.modal-close { width: 28px; height: 28px; border-radius: 6px; border: 1px solid var(--fog); background: transparent; cursor: pointer; font-size: 18px; color: var(--stone); flex-shrink: 0; }
.modal-body { padding: 20px 24px; }
.modal-footer { padding: 16px 24px; border-top: 1px solid var(--fog); display: flex; justify-content: space-between; }
.form-group { margin-bottom: 16px; }
.info { font-size: 12px; color: var(--forest); background: var(--mist); border-radius: 8px; padding: 10px 12px; margin-bottom: 16px; line-height: 1.5; }
.link-btn { background: none; border: none; padding: 0; margin-top: 6px; font-size: 11px; color: var(--leaf, #2d7a4f); cursor: pointer; text-decoration: underline; }
.opt { font-weight: 400; text-transform: none; letter-spacing: 0; color: var(--stone); }
.error-msg { font-size: 11.5px; color: var(--alert); line-height: 1.5; }
</style>
