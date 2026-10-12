<template>
  <div class="modal-overlay" @click.self="$emit('close')">
    <div class="modal-wrap">
      <div class="modal">
        <div class="modal-header">
          <div class="modal-title">Transaction details</div>
          <button class="modal-close-inline" aria-label="Close" @click="$emit('close')">×</button>
        </div>

        <div class="modal-body">
          <div class="amount-row">
            <div class="amount" :class="current.amount < 0 ? 'out' : 'in'">
              {{ current.amount < 0 ? '−' : '+' }}₦{{ Math.abs(current.amount).toLocaleString() }}
            </div>
            <StatBadge :variant="kindVariant">{{ kindLabel }}</StatBadge>
          </div>

          <!-- Details first: everything we know about the transfer, read-only -->
          <dl class="details">
            <dt>Narration</dt><dd class="narration">{{ current.narration }}</dd>
            <dt>Date</dt><dd>{{ formatDateTime(current.date) }}</dd>
            <dt>Bank reference</dt><dd class="mono">{{ current.monoId }}</dd>
            <template v-if="blockName">
              <dt>Block</dt><dd>{{ blockName }}</dd>
            </template>
            <dt>Category</dt><dd>{{ categoryLabel(current.category) }}</dd>
            <dt>Categorised by</dt>
            <dd>
              {{ current.source }} · {{ current.confidence }} confidence
              <StatBadge v-if="current.reviewedByUser" variant="green">Reviewed</StatBadge>
              <StatBadge v-else variant="harvest">Not reviewed</StatBadge>
            </dd>
            <template v-if="allocations.length">
              <dt>Crop split</dt>
              <dd><span v-for="[crop, pct] in allocations" :key="crop" class="alloc">{{ crop }} {{ pct }}%</span></dd>
            </template>
            <template v-if="current.reviewNotes">
              <dt>Notes</dt><dd>{{ current.reviewNotes }}</dd>
            </template>
          </dl>

          <div v-if="orderRef || reconciliation !== 'None'" class="order-box" :class="{ warn: reconciliation === 'NeedsReview' }">
            <div class="order-title">Order payment</div>
            <div v-if="reconciliation === 'Matched'">Paid <b>{{ orderRef }}</b>. This transfer matched the order's number and total.</div>
            <div v-else-if="reconciliation === 'NeedsReview'">{{ current.reconciliationNote }}</div>
            <div v-else-if="reconciliation === 'Dismissed'">Reviewed: not an order payment.</div>
          </div>

          <!-- Editing is a separate, deliberate step -->
          <div v-if="!editing" class="edit-bar">
            <button class="btn-secondary" @click="startEdit">Edit</button>
          </div>

          <div v-else class="edit-box">
            <div class="form-group">
              <label class="form-label">Category</label>
              <select class="form-select" v-model="draft.category">
                <option v-for="c in TRANSACTION_CATEGORIES" :key="c" :value="c">{{ categoryLabel(c) }}</option>
              </select>
            </div>

            <div v-if="current.amount >= 0" class="form-group">
              <label class="form-label">Money in is</label>
              <div class="kind-toggle">
                <button type="button" :class="{ on: draft.kind === 'Income' }" @click="draft.kind = 'Income'">Income</button>
                <button type="button" :class="{ on: draft.kind === 'Capital' }" @click="draft.kind = 'Capital'">Capital (owner's investment)</button>
              </div>
            </div>

            <div class="form-group">
              <label class="form-label">Notes</label>
              <input class="form-input" v-model="draft.notes" maxlength="500" placeholder="Optional" />
            </div>

            <div v-if="error" class="error-msg">{{ error }}</div>
            <div class="edit-actions">
              <button class="btn-secondary" :disabled="saving" @click="editing = false">Cancel</button>
              <button class="btn-primary" :disabled="saving" @click="save">{{ saving ? 'Saving…' : 'Save' }}</button>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { useAgroStore } from '@/stores/agro'
import { transactionApi } from '@/services/api'
import { TRANSACTION_CATEGORIES, categoryLabel, categoryOrdinal, normalizeTransaction } from '@/utils/enums'
import { extractErrorMessage } from '@/utils/http'
import StatBadge from '@/components/ui/StatBadge.vue'
import type { Transaction } from '@/types'

const props = defineProps<{ txn: Transaction }>()
const emit = defineEmits<{ close: []; saved: [] }>()
const store = useAgroStore()

const current = ref<Transaction>(props.txn)
const editing = ref(false)
const saving = ref(false)
const error = ref('')
const draft = ref({ category: props.txn.category as string, kind: 'Income', notes: '' })

const kindLabel = computed(() => String(current.value.kind ?? (current.value.amount < 0 ? 'Expense' : 'Income')))
const kindVariant = computed(() => kindLabel.value === 'Capital' ? 'harvest' : kindLabel.value === 'Income' ? 'green' : 'stone')
const reconciliation = computed(() => String(current.value.reconciliation ?? 'None'))
const blockName = computed(() => (current.value.blockId ? store.blocks.find(b => b.id === current.value.blockId)?.name ?? '' : ''))
const allocations = computed(() => Object.entries(current.value.cropAllocation ?? {}) as [string, number][])

// The order's number (ORD-1042) is looked up from the orders list when we have it.
const orderRef = computed(() => {
  const id = current.value.orderId
  if (!id) return ''
  return store.orders.find(o => o.id === id)?.reference ?? 'the order'
})

function formatDateTime(iso: string) {
  return new Date(iso).toLocaleString('en-GB', { day: 'numeric', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit' })
}

function startEdit() {
  draft.value = {
    category: current.value.category as string,
    kind: kindLabel.value === 'Capital' ? 'Capital' : 'Income',
    notes: current.value.reviewNotes ?? ''
  }
  error.value = ''
  editing.value = true
}

async function save() {
  saving.value = true
  error.value = ''
  try {
    const updated = await store.reviewTransaction(current.value.id, {
      category: categoryOrdinal(draft.value.category),
      cropAllocation: null,
      notes: draft.value.notes || null
    })
    let result = updated
    // Only money in can move between income and capital.
    if (current.value.amount >= 0 && draft.value.kind !== kindLabel.value) {
      const res = await transactionApi.setKind(current.value.id, draft.value.kind === 'Capital' ? 2 : 1)
      result = normalizeTransaction(res.data)
    }
    current.value = result
    editing.value = false
    emit('saved')
  } catch (e) {
    error.value = extractErrorMessage(e, 'Could not save the changes.')
  } finally {
    saving.value = false
  }
}
</script>

<style scoped>
.modal-overlay {
  position: fixed; inset: 0; background: rgba(10,26,16,0.6); backdrop-filter: blur(4px);
  z-index: 200; display: flex; align-items: flex-start; justify-content: center;
  padding: 48px 24px; overflow-y: auto;
}
.modal-wrap { position: relative; }
.modal { background: white; border-radius: 16px; width: 480px; max-width: 100%; box-shadow: 0 24px 64px rgba(10,26,16,0.2); }
.modal-header { padding: 20px 24px 16px; border-bottom: 1px solid var(--fog); display: flex; align-items: center; justify-content: space-between; }
.modal-title { font-family: 'Syne', sans-serif; font-weight: 700; font-size: 15px; color: var(--forest); }
.modal-close-inline { width: 28px; height: 28px; border-radius: 6px; border: 1px solid var(--fog); background: transparent; cursor: pointer; font-size: 18px; color: var(--stone); }
.modal-body { padding: 20px 24px; }

.amount-row { display: flex; align-items: center; justify-content: space-between; margin-bottom: 14px; }
.amount { font-family: 'JetBrains Mono', monospace; font-size: 22px; font-weight: 600; }
.amount.out { color: var(--alert); }
.amount.in { color: var(--palm); }

.details { display: grid; grid-template-columns: 120px 1fr; gap: 8px 12px; font-size: 12.5px; margin: 0 0 14px; }
.details dt { color: var(--stone); font-size: 11px; padding-top: 1px; }
.details dd { margin: 0; color: var(--forest); word-break: break-word; }
.narration { font-weight: 600; }
.mono { font-family: 'JetBrains Mono', monospace; font-size: 11px; }
.alloc { display: inline-block; margin-right: 8px; }

.order-box { background: rgba(45,122,79,0.08); border-radius: 8px; padding: 10px 12px; font-size: 12px; color: var(--forest); margin-bottom: 14px; }
.order-box.warn { background: rgba(183,121,31,0.12); }
.order-title { font-weight: 600; margin-bottom: 2px; }

.edit-bar { border-top: 1px solid var(--fog); padding-top: 14px; display: flex; justify-content: flex-end; }
.edit-box { border-top: 1px solid var(--fog); padding-top: 14px; }
.edit-actions { display: flex; justify-content: flex-end; gap: 8px; margin-top: 12px; }
.kind-toggle { display: inline-flex; border: 1.5px solid var(--fog); border-radius: 8px; overflow: hidden; }
.kind-toggle button { border: none; background: transparent; padding: 7px 14px; font-size: 12px; cursor: pointer; color: var(--stone); }
.kind-toggle button.on { background: var(--forest); color: #fff; }
.error-msg { font-size: 11.5px; color: var(--alert); line-height: 1.5; margin-top: 8px; }
</style>
