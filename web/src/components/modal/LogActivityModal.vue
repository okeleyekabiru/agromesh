<template>
  <Transition name="modal">
  <div class="modal-overlay" @click.self="$emit('close')">
    <div class="modal">
      <div class="modal-header">
        <div class="modal-title">Log Activity</div>
        <button class="modal-close" @click="$emit('close')">×</button>
      </div>

      <!-- Stepper -->
      <div class="modal-steps">
        <div v-for="(s, i) in steps" :key="s" class="step" :class="{ active: step === i+1, done: step > i+1 }">
          <div class="step-num">{{ step > i+1 ? '✓' : i+1 }}</div>
          <div class="step-label">{{ s }}</div>
        </div>
      </div>

      <!-- Low-stock warning banner -->
      <div v-if="lowStockWarnings.length" class="warning-banner" role="alert">
        <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="#8A6000" stroke-width="2" stroke-linecap="round"><path d="M10.29 3.86L1.82 18a2 2 0 001.71 3h16.94a2 2 0 001.71-3L13.71 3.86a2 2 0 00-3.42 0z"/><line x1="12" y1="9" x2="12" y2="13"/><line x1="12" y1="17" x2="12.01" y2="17"/></svg>
        {{ lowStockWarnings.join(' · ') }}
      </div>

      <div v-if="submitError" class="error-banner" role="alert">{{ submitError }}</div>

      <div class="modal-body">
        <!-- STEP 1: Location -->
        <template v-if="step === 1">
          <div class="form-group">
            <label class="form-label">Date</label>
            <input type="date" class="form-input" v-model="form.date">
          </div>
          <div class="form-group">
            <label class="form-label">Block</label>
            <select class="form-select" v-model="form.blockId">
              <option value="">Choose block...</option>
              <option v-for="s in store.blocks" :key="s.id" :value="s.id">
                {{ s.name }} — {{ s.crops.map(c => c.cropType).join(' / ') }}
              </option>
            </select>
            <div v-if="selectedBlock" class="form-hint">
              {{ selectedBlock.area.displayValue }} {{ selectedBlock.area.displayUnit }} ·
              Last activity: {{ selectedBlock.lastActivityDate ?? 'never' }}
            </div>
          </div>
        </template>

        <!-- STEP 2: Activity -->
        <template v-if="step === 2">
          <div class="form-group">
            <label class="form-label">Activity Type</label>
            <div class="activity-grid">
              <button
                v-for="a in activityTypes" :key="a.name"
                class="activity-btn"
                :class="{ selected: form.activityType === a.key }"
                @click="form.activityType = a.key as any"
              >
                <div class="activity-btn-icon">
                  <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor"
                       stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"
                       v-html="a.icon" />
                </div>
                <span class="activity-btn-name">{{ a.name }}</span>
              </button>
            </div>
          </div>
          <div v-if="form.activityType" class="form-group">
            <label class="form-label">Notes (optional)</label>
            <input class="form-input" placeholder="Any details about this activity..." v-model="form.notes">
          </div>
          <!-- Chemical selection for Spraying / PestControl -->
          <div v-if="isChemicalActivity && !store.chemicals.length" class="form-hint">
            The chemical used is recorded from the Chemical stock items you add in the next step.
          </div>
          <div v-if="isChemicalActivity && store.chemicals.length" class="form-group">
            <label class="form-label">Chemical Product</label>
            <select class="form-select" v-model="form.chemicalProductId">
              <option value="">None / not tracked</option>
              <option v-for="c in store.chemicals" :key="c.id" :value="c.id">
                {{ c.name }}
              </option>
            </select>
          </div>
        </template>

        <!-- STEP 3: Workers & Inputs -->
        <template v-if="step === 3">
          <div class="form-group">
            <label class="form-label">Supervisor</label>
            <select class="form-select" v-model="form.supervisorId">
              <option value="">Select supervisor...</option>
              <option v-for="w in store.supervisors" :key="w.id" :value="w.id">
                {{ w.name }} — {{ w.roleTitle }}
              </option>
            </select>
          </div>

          <div class="form-group">
            <label class="form-label">Workers ({{ workerEntries.length }} added)</label>
            <div class="chip-row">
              <span v-for="(we, i) in workerEntries" :key="i" class="chip">
                👤 {{ we.label }}
                <button class="chip-remove" @click="workerEntries.splice(i,1)">×</button>
              </span>
              <span v-if="!workerEntries.length" class="empty-chips">No workers added yet</span>
            </div>
            <div class="add-row">
              <select class="form-select" v-model="workerPickId" style="flex:1">
                <option value="">Pick worker...</option>
                <option v-for="w in store.workers" :key="w.id" :value="w.id">
                  {{ w.name }} ({{ w.roleTitle }} · ₦{{ w.payRate.toLocaleString() }}/{{ PAY_FREQUENCY_LABELS[w.payFrequency].toLowerCase() }})
                </option>
              </select>
              <input class="form-input" placeholder="Hours" v-model.number="workerHours" type="number" min="1" max="12" style="width:80px" @wheel.prevent>
              <button class="btn btn-primary" @click="addWorker">+ Add</button>
            </div>
          </div>

          <div class="form-group">
            <label class="form-label">Inputs Used ({{ form.inputs.length }} added)</label>
            <div class="chip-row">
              <span v-for="(inp, i) in form.inputs" :key="i" class="chip">
                📦 {{ inp.quantity }}{{ inp.unit }} {{ inp.item }}
                <button class="chip-remove" @click="form.inputs.splice(i,1)">×</button>
              </span>
              <span v-if="!form.inputs.length" class="empty-chips">No inputs added yet</span>
            </div>
            <div class="add-row">
              <select class="form-select" v-model="inputItemId" style="flex:1">
                <option value="" disabled>Choose from stock…</option>
                <option v-for="i in usableItems" :key="i.id" :value="i.id" :disabled="availableFor(i.id) <= 0">
                  {{ i.name }} — {{ fmt(availableFor(i.id)) }} {{ i.unit }} available
                </option>
              </select>
              <input class="form-input" placeholder="Qty" v-model.number="inputQty" type="number" min="0" step="any" style="width:84px" @wheel.prevent>
              <span v-if="selectedInputItem" class="unit-tag">{{ selectedInputItem.unit }}</span>
              <button class="btn btn-primary" @click="addInput" :disabled="!canAddInput">+ Add</button>
            </div>
            <div v-if="selectedInputItem" class="form-hint">
              {{ fmt(pickedFor(selectedInputItem.id)) }} {{ selectedInputItem.unit }} picked out for this block ·
              {{ fmt(selectedInputItem.currentStock) }} {{ selectedInputItem.unit }} in the store.
              Picked stock is used first, then the store.
            </div>
            <div v-if="inputOverLimit" class="error-msg">Only {{ fmt(availableFor(inputItemId)) }} {{ selectedInputItem?.unit }} available.</div>
            <div v-if="!workerEntries.length || !form.inputs.length" class="form-hint warn">
              Add at least one worker and one stock item — both are required.
            </div>
            <div v-if="isChemicalActivity && !hasChemicalInput" class="form-hint warn">
              Add the chemical you are spraying with — it is required for this activity.
            </div>
            <div v-if="selectedInputItem && !selectedInputItem.unitCost" class="form-hint warn">
              No price set for {{ selectedInputItem.name }} — this will cost ₦0. Set its price on the Inventory page.
            </div>
            <div v-if="!store.inventoryItems.length" class="form-hint">No stock items yet — add items on the Inventory page first.</div>
            <div v-else-if="form.activityType && !usableItems.length" class="form-hint">
              No {{ allowedLabel }} in stock for this activity.
            </div>
          </div>
        </template>
      </div>

      <div class="modal-footer">
        <button class="btn btn-ghost" @click="step > 1 ? step-- : $emit('close')">
          {{ step === 1 ? 'Cancel' : '← Back' }}
        </button>
        <div style="display:flex;align-items:center;gap:10px;">
          <span style="font-family:'JetBrains Mono',monospace;font-size:9px;color:var(--stone);">{{ step }} / {{ steps.length }}</span>
          <button class="btn btn-primary" @click="next" :disabled="!canProceed || submitting">
            {{ step === steps.length ? (submitting ? 'Saving…' : '✓ Submit Activity') : 'Continue →' }}
          </button>
        </div>
      </div>
    </div>
  </div>
  </Transition>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { useAgroStore } from '@/stores/agro'
import { extractErrorMessage } from '@/utils/http'
import type { ActivityType } from '@/types'
import { PAY_FREQUENCY_LABELS } from '@/types'

const emit = defineEmits(['close'])
const store = useAgroStore()

onMounted(() => {
  if (!store.workers.length) store.fetchWorkers()
  if (!store.chemicals.length) store.fetchChemicals()
  if (!store.blocks.length) store.fetchBlocks()
  store.fetchInventory()
  store.fetchOutstandingStock()
})

const step = ref(1)
const steps = ['Location', 'Activity', 'Workers & Inputs']
const submitting = ref(false)
const lowStockWarnings = ref<string[]>([])
const submitError = ref('')

const workerPickId = ref('')
const workerHours = ref<number>(8)
const workerEntries = ref<{ workerId: string; workerName: string; workerType: string; hoursWorked: number; payRate: number; label: string }[]>([])

const inputItemId = ref('')
const inputQty = ref<number | ''>('')

// Inputs must be real stock items; usage reduces stock (picked-out stock first, then the store).
const selectedInputItem = computed(() => store.inventoryItems.find(i => i.id === inputItemId.value))

function pickedFor(itemId: string) {
  return store.outstandingStock
    .filter(o => o.inventoryItemId === itemId && o.blockId === form.value.blockId)
    .reduce((sum, o) => sum + o.quantityOutstanding, 0)
}
// What's still usable for this item on this block, after what's already been added in this form.
function availableFor(itemId: string) {
  const item = store.inventoryItems.find(i => i.id === itemId)
  if (!item) return 0
  const alreadyAdded = form.value.inputs.filter(i => i.inventoryItemId === itemId).reduce((s, i) => s + i.quantity, 0)
  return Math.max(0, pickedFor(itemId) + item.currentStock - alreadyAdded)
}
const inputOverLimit = computed(() => !!inputItemId.value && Number(inputQty.value) > availableFor(inputItemId.value))
const canAddInput = computed(() => !!inputItemId.value && Number(inputQty.value) > 0 && !inputOverLimit.value)
function fmt(n: number) {
  return Number(n.toFixed(3)).toLocaleString()
}

const form = ref({
  date: new Date().toISOString().split('T')[0],
  blockId: '',
  activityType: '' as ActivityType | '',
  notes: '',
  supervisorId: '',
  chemicalProductId: '',
  inputs: [] as { inventoryItemId: string; item: string; quantity: number; unit: string }[]
})

const activityTypes = [
  { key: 'Weeding',               name: 'Weeding',     icon: '<path d="M12 22V12M12 12C12 7 7 4 3 5c2 4 5 7 9 7z"/><path d="M12 12c0-5 5-8 9-7-2 4-5 7-9 7z"/>' },
  { key: 'Spraying',              name: 'Spraying',    icon: '<path d="M12 2.69l5.66 5.66a8 8 0 11-11.31 0L12 2.69z"/>' },
  { key: 'FertilizerApplication', name: 'Fertilizer',  icon: '<path d="M5 8h14l-1.5 8H6.5L5 8z"/><path d="M8 8V6a4 4 0 018 0v2"/>' },
  { key: 'Maintenance',           name: 'Maintenance', icon: '<path d="M14.7 6.3a1 1 0 000 1.4l1.6 1.6a1 1 0 001.4 0l3.77-3.77a6 6 0 01-7.94 7.94l-6.91 6.91a2.12 2.12 0 01-3-3l6.91-6.91a6 6 0 017.94-7.94l-3.76 3.76z"/>' },
  { key: 'Harvesting',            name: 'Harvesting',  icon: '<path d="M3 17l4-8 4 4 4-8 4 8M3 21h18M12 3v4"/>' },
  { key: 'Clearing',              name: 'Clearing',    icon: '<circle cx="6" cy="6" r="3"/><circle cx="6" cy="18" r="3"/><path d="M20 4L8.12 15.88M14.47 14.48L20 20M8.12 8.12L12 12"/>' },
  { key: 'Pruning',               name: 'Pruning',     icon: '<line x1="4" y1="20" x2="20" y2="4"/><path d="M8.5 5.5L3 11l1.5 1.5M15.5 18.5L21 13l-1.5-1.5"/>' },
  { key: 'PestControl',           name: 'Pest Control',icon: '<path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z"/>' },
]

const CHEMICAL_TYPES = new Set(['Spraying', 'PestControl'])

// Same rule as the API: chemicals only for spraying / pest control, fertilizer only for fertilizer
// application (weeding may use chemicals), and neither for the rest.
function allowsCategory(type: string, categoryName: string): boolean {
  const cat = (categoryName || '').trim().toLowerCase()
  if (!cat) return true
  const chemical = cat === 'chemical'
  const fertilizer = cat === 'fertilizer'
  if (type === 'Spraying' || type === 'PestControl') return chemical
  if (type === 'FertilizerApplication') return fertilizer
  if (type === 'Weeding') return !fertilizer
  return !chemical && !fertilizer
}

const usableItems = computed(() =>
  store.inventoryItems.filter(i => !form.value.activityType || allowsCategory(form.value.activityType, i.categoryName)))

const allowedLabel = computed(() => {
  const t = form.value.activityType
  if (t === 'Spraying' || t === 'PestControl') return 'chemicals'
  if (t === 'FertilizerApplication') return 'fertilizer'
  return 'items'
})

// Switching activity type drops any picked inputs that no longer fit it.
watch(() => form.value.activityType, (t) => {
  if (!t) return
  form.value.inputs = form.value.inputs.filter(inp => {
    const item = store.inventoryItems.find(i => i.id === inp.inventoryItemId)
    return !item || allowsCategory(t, item.categoryName)
  })
  const sel = store.inventoryItems.find(i => i.id === inputItemId.value)
  if (sel && !allowsCategory(t, sel.categoryName)) inputItemId.value = ''
})
const isChemicalActivity = computed(() => CHEMICAL_TYPES.has(form.value.activityType as string))

const selectedBlock = computed(() => store.blocks.find(s => s.id === form.value.blockId))

const canProceed = computed(() => {
  if (step.value === 1) return !!form.value.blockId
  if (step.value === 2) return !!form.value.activityType
  // Who did the work and what it used are both required.
  return !!form.value.supervisorId && workerEntries.value.length > 0 && form.value.inputs.length > 0 &&
    (!isChemicalActivity.value || hasChemicalInput.value)
})

// Spraying / pest control must use a chemical from stock.
const hasChemicalInput = computed(() => form.value.inputs.some(inp => {
  const item = store.inventoryItems.find(i => i.id === inp.inventoryItemId)
  return item?.categoryName?.trim().toLowerCase() === 'chemical'
}))

async function next() {
  if (step.value < steps.length) {
    step.value++
    return
  }
  submitting.value = true
  lowStockWarnings.value = []
  submitError.value = ''
  try {
    const payload = {
      date: form.value.date,
      blockId: form.value.blockId,
      activityType: form.value.activityType,
      notes: form.value.notes || null,
      supervisorId: form.value.supervisorId,
      chemicalProductId: form.value.chemicalProductId || null,
      workers: workerEntries.value.map(we => ({
        workerId: we.workerId,
        workerName: we.workerName,
        workerType: we.workerType,
        hoursWorked: we.hoursWorked,
        payRate: we.payRate
      })),
      inputs: form.value.inputs.map(i => ({ inventoryItemId: i.inventoryItemId, quantity: i.quantity }))
    }
    const result = await store.logActivity(payload)
    if (result.lowStockWarnings?.length) {
      lowStockWarnings.value = result.lowStockWarnings
      setTimeout(() => emit('close'), 3000)
    } else {
      emit('close')
    }
  } catch (e) {
    console.error('Failed to log activity', e)
    submitError.value = extractErrorMessage(e, 'Could not submit this activity. Please try again.')
  } finally {
    submitting.value = false
  }
}

function addWorker() {
  const w = store.workers.find(x => x.id === workerPickId.value)
  if (!w || !workerHours.value) return
  workerEntries.value.push({
    workerId: w.id,
    workerName: w.name,
    workerType: w.type,
    hoursWorked: workerHours.value,
    payRate: w.payRate,
    label: `${w.name} · ${workerHours.value}h`
  })
  workerPickId.value = ''
  workerHours.value = 8
}

function addInput() {
  const item = selectedInputItem.value
  if (!item || !canAddInput.value) return
  form.value.inputs.push({ inventoryItemId: item.id, item: item.name, quantity: Number(inputQty.value), unit: item.unit })
  inputItemId.value = ''
  inputQty.value = ''
}
</script>

<style scoped>
/* Modal entrance / exit */
.modal-enter-active .modal-overlay { animation: fade-in var(--dur-base) var(--ease-out) both; }
.modal-leave-active .modal-overlay { animation: fade-in var(--dur-fast) var(--ease-in) reverse both; }
.modal-enter-active .modal  { animation: scale-in var(--dur-enter) var(--ease-out) both; }
.modal-leave-active .modal  { animation: scale-in var(--dur-fast) var(--ease-in) reverse both; }

.modal-overlay { position: fixed; inset: 0; background: rgba(10,26,16,0.55); backdrop-filter: blur(6px); z-index: 100; display: flex; align-items: center; justify-content: center; animation: fade-in var(--dur-base) var(--ease-out) both; }
.modal { background: white; border-radius: 16px; width: 520px; max-height: 88vh; overflow-y: auto; box-shadow: var(--shadow-xl); animation: scale-in var(--dur-enter) var(--ease-out) both; }
.modal-header { padding: 20px 24px 16px; border-bottom: 1px solid var(--fog); display: flex; align-items: center; justify-content: space-between; }
.modal-title { font-family: 'Syne', sans-serif; font-weight: 700; font-size: 16px; color: var(--forest); letter-spacing: -0.01em; }
.modal-close { width: 28px; height: 28px; border-radius: 6px; border: 1px solid var(--fog); background: transparent; cursor: pointer; font-size: 18px; color: var(--stone); display: flex; align-items: center; justify-content: center; }
.modal-close:hover { background: var(--mist); }
.modal-steps { display: flex; padding: 16px 24px; border-bottom: 1px solid var(--fog); }
.step { flex: 1; display: flex; flex-direction: column; align-items: center; position: relative; }
.step::after { content: ''; position: absolute; top: 12px; left: 50%; width: 100%; height: 1px; background: var(--fog); }
.step:last-child::after { display: none; }
.step-num { width: 24px; height: 24px; border-radius: 50%; border: 1.5px solid var(--fog); display: flex; align-items: center; justify-content: center; font-family: 'JetBrains Mono', monospace; font-size: 10px; color: var(--stone); background: white; position: relative; z-index: 1; }
.step.active .step-num { background: var(--leaf); border-color: var(--leaf); color: white; }
.step.done .step-num { background: var(--palm); border-color: var(--palm); color: white; }
.step-label { font-family: 'JetBrains Mono', monospace; font-size: 8px; letter-spacing: 0.1em; text-transform: uppercase; color: var(--stone); margin-top: 4px; text-align: center; }
.step.active .step-label { color: var(--leaf); }
.unit-tag { font-family: 'JetBrains Mono', monospace; font-size: 10px; color: var(--stone); align-self: center; }
.error-msg { font-size: 11.5px; color: var(--alert); margin-top: 6px; }
.error-banner { background: #FDECEC; border-bottom: 1px solid #F3B5B5; padding: 10px 24px; font-size: 11px; color: #A32020; font-weight: 600; }
.warning-banner { background: #FFF8E7; border-bottom: 1px solid #F5D77B; padding: 10px 24px; font-size: 11px; color: #8A6000; font-weight: 600; }
.modal-body { padding: 20px 24px; }
.modal-footer { padding: 16px 24px; border-top: 1px solid var(--fog); display: flex; justify-content: space-between; align-items: center; }
.form-group { margin-bottom: 16px; }
.activity-grid { display: grid; grid-template-columns: repeat(2,1fr); gap: 8px; }
.activity-btn {
  padding: 10px 12px;
  border: 1.5px solid var(--fog);
  border-radius: 10px;
  background: transparent;
  cursor: pointer;
  text-align: left;
  transition:
    border-color var(--dur-fast) var(--ease-io),
    background   var(--dur-fast) var(--ease-io),
    transform    var(--dur-fast) var(--ease-spring);
}
.activity-btn:hover {
  border-color: rgba(61,170,106,0.4);
  background: rgba(61,170,106,0.04);
  transform: translateY(-1px);
}
.activity-btn:active { transform: scale(0.97); }
.activity-btn.selected {
  border-color: var(--leaf);
  background: rgba(45,122,79,0.07);
  box-shadow: 0 0 0 3px rgba(45,122,79,0.10);
}
.activity-btn-icon {
  width: 32px;
  height: 32px;
  border-radius: 8px;
  background: rgba(45,122,79,0.08);
  display: flex;
  align-items: center;
  justify-content: center;
  margin-bottom: 6px;
  color: var(--leaf);
  transition: background var(--dur-fast);
}
.activity-btn.selected .activity-btn-icon { background: rgba(45,122,79,0.15); }
.activity-btn-name { font-size: 11px; font-weight: 600; color: var(--forest); display: block; }
.chip-row { display: flex; flex-wrap: wrap; gap: 6px; margin-bottom: 8px; min-height: 28px; align-items: center; }
.add-row { display: flex; gap: 8px; }
.empty-chips { font-family: 'JetBrains Mono', monospace; font-size: 9px; color: var(--stone); }
.form-hint.warn { color: #B8780A; }
</style>
