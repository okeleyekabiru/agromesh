<template>
  <div class="onboard-shell">
    <div class="onboard-card">
      <!-- Header -->
      <div class="onboard-header">
        <div class="onboard-logo">AgroMesh</div>
        <div class="onboard-subtitle">Farm Setup · Step {{ step + 1 }} of {{ steps.length }}</div>
      </div>

      <!-- Step indicator -->
      <div class="step-bar">
        <div
          v-for="(s, i) in steps" :key="s"
          class="step-item"
          :class="{ active: step === i, done: step > i }"
        >
          <div class="step-dot">{{ step > i ? '✓' : i + 1 }}</div>
          <div class="step-name">{{ s }}</div>
        </div>
      </div>

      <!-- ── STEP 0: Farm Profile ── -->
      <div v-if="step === 0" class="onboard-body">
        <div class="step-title">Tell us about your farm</div>
        <div class="form-group">
          <label class="form-label">Farm Name</label>
          <input class="form-input" v-model="farm.name" placeholder="e.g. Adeyemi Oil Palm Plantation" />
        </div>
        <div class="form-row">
          <div class="form-group" style="flex:1">
            <label class="form-label">Total Area</label>
            <input class="form-input" v-model.number="farm.areaValue" type="number" min="0.1" step="0.5" placeholder="45" />
          </div>
          <div class="form-group" style="width:120px">
            <label class="form-label">Unit</label>
            <select class="form-select" v-model="farm.areaUnit">
              <option value="Hectares">Hectares</option>
              <option value="Acres">Acres</option>
            </select>
          </div>
        </div>
        <div class="form-row">
          <div class="form-group" style="flex:1">
            <label class="form-label">State</label>
            <select class="form-select" v-model="farm.stateId" style="width:100%">
              <option value="" disabled>— Select state —</option>
              <option v-for="s in store.states" :key="s.id" :value="s.id">{{ s.name }}</option>
            </select>
          </div>
          <div class="form-group" style="flex:1">
            <label class="form-label">
              LGA <span class="label-opt">optional</span>
            </label>
            <input class="form-input" v-model="farm.lgaFreetext" placeholder="e.g. Ikeja" />
          </div>
        </div>
        <div v-if="statesError" class="gps-err-msg">{{ statesError }}</div>
        <div v-if="submitError" class="gps-err-msg">{{ submitError }}</div>
        <div class="hint-box">
          Sections and farm boundaries are set up in the next step after this wizard.
        </div>
      </div>

      <!-- ── STEP 1: Worker Roles ── -->
      <div v-if="step === 1" class="onboard-body">
        <div class="step-title">Define worker roles &amp; pay rates</div>
        <div class="hint-box">
          Rates are set per role, at whatever frequency that role is actually paid — contractors
          are usually daily, supervisors are usually salaried monthly. All workers assigned to a
          role share its rate and frequency.
        </div>
        <div class="roles-list">
          <div v-for="(r, i) in roles" :key="i" class="role-input-row">
            <input class="form-input" v-model="r.title" placeholder="Role title e.g. Farm Worker" style="flex:2" />
            <select
              class="form-select"
              v-model="r.workerType"
              style="width:130px"
              @change="r.payFrequency = defaultFrequencyFor(r.workerType)"
            >
              <option value="Employee">Employee</option>
              <option value="Contractor">Contractor</option>
            </select>
            <div class="rate-input-wrap">
              <span class="rate-prefix">₦</span>
              <input class="form-input rate-input" v-model.number="r.payRate" type="number" min="500" step="100" placeholder="2200" />
            </div>
            <select class="form-select" v-model="r.payFrequency" style="width:110px">
              <option v-for="f in PAY_FREQUENCIES" :key="f" :value="f">/ {{ f.toLowerCase() }}</option>
            </select>
            <button class="btn btn-ghost btn-sm" @click="roles.splice(i,1)" :disabled="roles.length === 1">✕</button>
          </div>
        </div>
        <button
          class="btn btn-ghost"
          @click="roles.push({ title: '', workerType: 'Employee', payRate: 2200, payFrequency: defaultFrequencyFor('Employee') })"
          style="margin-top:8px;"
        >
          + Add Role
        </button>
      </div>

      <!-- ── STEP 2: Workers ── -->
      <div v-if="step === 2" class="onboard-body">
        <div class="step-title">Add your workers</div>
        <div class="workers-list">
          <div v-for="(w, i) in workers" :key="i" class="worker-input-row">
            <input class="form-input" v-model="w.name" placeholder="Full name" style="flex:2" />
            <input class="form-input" v-model="w.initials" placeholder="Initials" style="width:72px;text-transform:uppercase" maxlength="3" />
            <select class="form-select" v-model="w.roleIndex" style="flex:2">
              <option v-for="(r, ri) in roles" :key="ri" :value="ri">
                {{ r.title }} (₦{{ r.payRate.toLocaleString() }} / {{ r.payFrequency.toLowerCase() }})
              </option>
            </select>
            <button class="btn btn-ghost btn-sm" @click="workers.splice(i,1)">✕</button>
          </div>
        </div>
        <button class="btn btn-ghost" @click="workers.push({ name: '', initials: '', roleIndex: 0 })" style="margin-top:8px;">
          + Add Worker
        </button>
        <div v-if="!workers.length" class="hint-box" style="margin-top:12px;">
          You can skip this step and add workers later from the Workers page.
        </div>
      </div>

      <!-- Footer nav -->
      <div class="onboard-footer">
        <button class="btn btn-ghost" @click="back" :disabled="step === 0 || saving">
          ← Back
        </button>
        <div style="display:flex;align-items:center;gap:12px;">
          <span style="font-family:'JetBrains Mono',monospace;font-size:9px;color:var(--stone);">Step {{ step + 1 }} of {{ steps.length }}</span>
          <button class="btn btn-primary" @click="next" :disabled="!canProceed || saving">
            {{ step === steps.length - 1 ? (saving ? 'Saving…' : 'Continue to Sections →') : 'Continue →' }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useAgroStore } from '@/stores/agro'
import { workerRoleApi, workerApi } from '@/services/api'
import { extractErrorMessage } from '@/utils/http'

const router = useRouter()
const store = useAgroStore()

const step = ref(0)
const saving = ref(false)
const steps = ['Farm Profile', 'Worker Roles', 'Workers']

// ── Farm profile ──
const farm = ref({
  name: store.activeFarmSummary?.name ?? '',
  areaValue: 0,
  areaUnit: 'Hectares' as 'Hectares' | 'Acres',
  stateId: '',
  lgaFreetext: '',
})

// ── Reference states ──
const statesError = ref('')
const submitError = ref('')

async function loadStates() {
  try {
    await store.fetchStates()
    if (!store.states.length) statesError.value = 'Could not load states — please retry.'
  } catch {
    statesError.value = 'Could not load states — please retry.'
  }
}

// ── Worker roles ──
// Backend enums serialize as raw integers (no JsonStringEnumConverter) — these string
// labels are for the UI only; ordinal maps below convert at the API call boundary,
// mirroring the CROP_ORDINAL pattern already used in SectionsSetupView.vue.
type WorkerTypeLabel = 'Employee' | 'Contractor'
type PayFrequencyLabel = 'Daily' | 'Weekly' | 'Monthly'

const WORKER_TYPE_ORDINAL: Record<WorkerTypeLabel, number> = { Employee: 0, Contractor: 1 }
const PAY_FREQUENCY_ORDINAL: Record<PayFrequencyLabel, number> = { Daily: 0, Weekly: 1, Monthly: 2 }
const PAY_FREQUENCIES: PayFrequencyLabel[] = ['Daily', 'Weekly', 'Monthly']

// Only contractors are reliably daily-paid; anyone else (e.g. a salaried supervisor)
// defaults to monthly. Both stay fully editable — this is just the initial pick.
function defaultFrequencyFor(type: WorkerTypeLabel): PayFrequencyLabel {
  return type === 'Contractor' ? 'Daily' : 'Monthly'
}

const roles = ref([
  { title: 'Field Supervisor', workerType: 'Employee' as WorkerTypeLabel, payRate: 91000, payFrequency: 'Monthly' as PayFrequencyLabel },
  { title: 'Farm Worker',      workerType: 'Employee' as WorkerTypeLabel, payRate: 2200,  payFrequency: 'Daily' as PayFrequencyLabel },
  { title: 'Contractor',       workerType: 'Contractor' as WorkerTypeLabel, payRate: 4000, payFrequency: 'Daily' as PayFrequencyLabel },
])

// ── Workers ──
const workers = ref<{ name: string; initials: string; roleIndex: number }[]>([])

// ── Validation ──
const canProceed = computed(() => {
  if (step.value === 0) return farm.value.name.trim() && farm.value.areaValue > 0 && !!farm.value.stateId
  if (step.value === 1) return roles.value.every(r => r.title.trim() && r.payRate > 0 && !!r.payFrequency)
  return true
})

// ── Navigation ──
async function next() {
  if (step.value < steps.length - 1) {
    step.value++
    return
  }
  await submit()
}

function back() {
  if (step.value > 0) step.value--
}

// ── Submit ──
async function submit() {
  saving.value = true
  submitError.value = ''
  try {
    const farmId = store.activeFarmId
    if (!farmId) throw new Error('No farm associated with this account.')

    // Update farm profile (name, area)
    await store.updateFarmProfile({
      farmId,
      name: farm.value.name,
      areaValue: farm.value.areaValue,
      areaUnit: farm.value.areaUnit,
    })

    // Set farm location (state / LGA)
    await store.setFarmLocation(farmId, {
      stateId: farm.value.stateId,
      lgaFreetext: farm.value.lgaFreetext.trim() || undefined,
    })

    // Create worker roles
    const createdRoles: { id: string }[] = []
    for (const r of roles.value) {
      const res = await workerRoleApi.create({
        title: r.title,
        defaultType: WORKER_TYPE_ORDINAL[r.workerType],
        payRate: r.payRate,
        payFrequency: PAY_FREQUENCY_ORDINAL[r.payFrequency],
      })
      createdRoles.push({ id: res.data.id })
    }

    // Create workers
    for (const w of workers.value) {
      const role = createdRoles[w.roleIndex]
      if (!role) continue
      await workerApi.add({
        name: w.name,
        initials: w.initials.toUpperCase(),
        workerRoleId: role.id,
        type: WORKER_TYPE_ORDINAL[roles.value[w.roleIndex].workerType],
      })
    }

    // Go to sections setup wizard
    router.push({ name: 'sections-setup' })
  } catch (e: unknown) {
    console.error('Onboarding failed', e)
    submitError.value = extractErrorMessage(e, 'Could not save your farm setup. Please try again.')
    saving.value = false
  }
}

onMounted(() => {
  // This wizard operates on an already-created farm (from Quick Signup or Add Farm) — if
  // there's no active farm context (e.g. a stale bookmark hit outside an active session),
  // fail fast here instead of letting the user fill out the whole form and hit a cryptic
  // error only at the final submit step.
  if (!store.activeFarmId) {
    router.replace({ name: 'landing' })
    return
  }
  loadStates()
})
</script>

<style scoped>
.onboard-shell {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, #0d2b1f 0%, #1a4a33 100%);
  padding: 24px;
}

.onboard-card {
  background: white;
  border-radius: 20px;
  width: 100%;
  max-width: 560px;
  box-shadow: 0 32px 80px rgba(0,0,0,0.4);
  overflow: hidden;
}

.onboard-header {
  background: linear-gradient(135deg, #0d2b1f 0%, #2d7a4f 100%);
  padding: 24px 32px 20px;
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.onboard-logo {
  font-family: 'Syne', sans-serif;
  font-weight: 700;
  font-size: 20px;
  color: #fff;
  letter-spacing: -0.01em;
}

.onboard-subtitle {
  font-family: 'JetBrains Mono', monospace;
  font-size: 9px;
  letter-spacing: 0.12em;
  text-transform: uppercase;
  color: rgba(255,255,255,0.6);
}

.step-bar {
  display: flex;
  padding: 16px 24px;
  border-bottom: 1px solid var(--fog);
}

.step-item {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  position: relative;
}

.step-item::after {
  content: '';
  position: absolute;
  top: 12px;
  left: 50%;
  width: 100%;
  height: 1px;
  background: var(--fog);
}

.step-item:last-child::after { display: none; }

.step-dot {
  width: 24px;
  height: 24px;
  border-radius: 50%;
  border: 2px solid var(--fog);
  display: flex;
  align-items: center;
  justify-content: center;
  font-family: 'JetBrains Mono', monospace;
  font-size: 10px;
  font-weight: 700;
  color: var(--stone);
  background: white;
  position: relative;
  z-index: 1;
  transition: all 0.2s;
}

.step-item.active .step-dot { border-color: var(--leaf); color: var(--leaf); }
.step-item.done .step-dot { background: var(--palm); border-color: var(--palm); color: white; }

.step-name {
  font-family: 'JetBrains Mono', monospace;
  font-size: 8px;
  letter-spacing: 0.1em;
  text-transform: uppercase;
  color: var(--stone);
  margin-top: 4px;
  text-align: center;
}

.step-item.active .step-name { color: var(--leaf); }

.onboard-body {
  padding: 24px 32px;
  min-height: 240px;
}

.step-title {
  font-family: 'Syne', sans-serif;
  font-size: 18px;
  font-weight: 700;
  color: var(--forest);
  letter-spacing: -0.01em;
  margin-bottom: 20px;
}

.form-row { display: flex; gap: 12px; }

.hint-box {
  background: rgba(45,122,79,0.06);
  border: 1px solid rgba(45,122,79,0.15);
  border-radius: 8px;
  padding: 10px 14px;
  font-size: 11px;
  color: var(--stone);
  line-height: 1.6;
  margin-top: 12px;
}

/* GPS */
.label-opt {
  font-size: 9px;
  font-family: 'JetBrains Mono', monospace;
  color: var(--stone);
  font-weight: 400;
  margin-left: 6px;
  text-transform: uppercase;
  letter-spacing: 0.06em;
}
.gps-btn {
  display: flex;
  align-items: center;
  gap: 7px;
  width: 100%;
  padding: 9px 14px;
  background: rgba(45,122,79,0.06);
  border: 1px solid rgba(45,122,79,0.2);
  border-radius: 10px;
  color: var(--leaf);
  font-size: 12px;
  font-family: 'JetBrains Mono', monospace;
  cursor: pointer;
  transition: background 0.15s, border-color 0.15s;
  margin-bottom: 8px;
}
.gps-btn:hover:not(:disabled) { background: rgba(45,122,79,0.1); border-color: rgba(45,122,79,0.35); }
.gps-btn:disabled { opacity: 0.6; cursor: not-allowed; }
.gps-spinner {
  width: 11px; height: 11px;
  border: 1.5px solid rgba(45,122,79,0.25);
  border-top-color: var(--leaf);
  border-radius: 50%;
  animation: spin 0.7s linear infinite;
  flex-shrink: 0;
}
@keyframes spin { to { transform: rotate(360deg); } }
.gps-result {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 6px 10px;
  background: rgba(45,122,79,0.08);
  border: 1px solid rgba(45,122,79,0.2);
  border-radius: 8px;
  font-size: 10px;
  font-family: 'JetBrains Mono', monospace;
  color: var(--leaf);
  margin-bottom: 8px;
}
.gps-clear {
  margin-left: auto;
  background: none;
  border: none;
  color: var(--stone);
  cursor: pointer;
  font-size: 13px;
  line-height: 1;
  padding: 0 2px;
}
.gps-clear:hover { color: var(--forest); }
.gps-err-msg { font-size: 10px; color: #E64A19; margin-bottom: 6px; padding: 0 2px; }

/* Roles */
.roles-list { display: flex; flex-direction: column; gap: 8px; margin-top: 12px; }
.role-input-row { display: flex; gap: 8px; align-items: center; }
.rate-input-wrap { display: flex; align-items: center; gap: 4px; width: 140px; }
.rate-prefix { font-family: 'JetBrains Mono', monospace; font-size: 12px; color: var(--stone); }
.rate-input { flex: 1; width: 80px !important; text-align: right; }
.rate-suffix { font-family: 'JetBrains Mono', monospace; font-size: 9px; color: var(--stone); white-space: nowrap; }

/* Workers */
.workers-list { display: flex; flex-direction: column; gap: 8px; margin-top: 12px; }
.worker-input-row { display: flex; gap: 8px; align-items: center; }

.onboard-footer {
  padding: 16px 32px;
  border-top: 1px solid var(--fog);
  display: flex;
  justify-content: space-between;
  align-items: center;
}
</style>
