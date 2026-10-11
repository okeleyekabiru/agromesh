<template>
  <div>
    <div class="kpi-grid">
      <KpiCard label="Active Workers"    :value="`${store.workers.filter(w=>w.active).length}`"               delta="active this month"       deltaType="up"   :dark="true" />
      <KpiCard label="Employees"         :value="`${store.workers.filter(w=>w.type==='Employee').length}`"    delta="on payroll"              deltaType="mute" />
      <KpiCard label="Contractors"       :value="`${store.workers.filter(w=>w.type==='Contractor').length}`"  delta="seasonal"                deltaType="mute" />
      <KpiCard label="Roles"             :value="`${store.workerRoles.length}`"                               delta="defined wage bands"      deltaType="mute" />
    </div>

    <div class="card">
      <div class="card-header">
        <div class="card-title">Workforce</div>
        <div style="display:flex;gap:8px;align-items:center;">
          <StatBadge variant="green">{{ store.workers.filter(w=>w.type==='Employee').length }} Employees</StatBadge>
          <StatBadge variant="blue">{{ store.workers.filter(w=>w.type==='Contractor').length }} Contractors</StatBadge>
          <button v-if="canAddWorker" class="btn btn-primary btn-sm" @click="showAdd = true">+ Add Worker</button>
        </div>
      </div>
      <div v-if="store.loading" style="padding:24px;text-align:center;color:var(--stone);font-size:12px;">Loading workers…</div>
      <WorkerRow v-for="w in store.workers" :key="w.id" :worker="w" />
      <div v-if="!store.loading && !store.workers.length" style="padding:40px;text-align:center;color:var(--stone);font-size:12px;">No workers yet</div>
    </div>

    <AddWorkerModal v-if="showAdd" @close="showAdd = false" />
    <WageBandModal v-if="bandOpen" :role="editingBand" @close="bandOpen = false" />

    <!-- Worker Roles -->
    <div class="card" style="margin-top:16px;">
      <div class="card-header">
        <div class="card-title">Wage Bands</div>
        <div style="display:flex;gap:8px;align-items:center;">
          <div class="card-label">BY ROLE</div>
          <button v-if="canManageBands" class="btn btn-primary btn-sm" @click="openBand(null)">+ Add Band</button>
        </div>
      </div>
      <div class="role-row" v-for="role in store.workerRoles" :key="role.id">
        <div class="role-title">{{ role.title }}</div>
        <StatBadge :variant="role.defaultType === 'Employee' ? 'green' : 'blue'">{{ role.defaultType }}</StatBadge>
        <div class="role-rate">₦{{ role.payRate.toLocaleString() }} / {{ PAY_FREQUENCY_LABELS[role.payFrequency].toLowerCase() }}</div>
        <div class="role-count" style="font-family:'JetBrains Mono',monospace;font-size:10px;color:var(--stone);">{{ role.workerCount }} workers</div>
        <button v-if="canManageBands" class="btn btn-ghost btn-sm" @click="openBand(role)">Edit rate</button>
      </div>
      <div v-if="!store.workerRoles.length" style="padding:32px;text-align:center;color:var(--stone);font-size:12px;">
        No wage bands yet<template v-if="canManageBands"> — add one (e.g. Harvester, ₦5,000 / day) before adding workers.</template>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useAgroStore } from '@/stores/agro'
import { PAY_FREQUENCY_LABELS, type WorkerRole } from '@/types'
import KpiCard from '@/components/ui/KpiCard.vue'
import StatBadge from '@/components/ui/StatBadge.vue'
import WorkerRow from '@/components/ui/WorkerRow.vue'
import AddWorkerModal from '@/components/modal/AddWorkerModal.vue'
import WageBandModal from '@/components/modal/WageBandModal.vue'

const store = useAgroStore()

const showAdd = ref(false)
const bandOpen = ref(false)
const editingBand = ref<WorkerRole | null>(null)
function openBand(role: WorkerRole | null) {
  editingBand.value = role
  bandOpen.value = true
}
// Mirrors the backend rule (CreateWorkerRoleCommandHandler.CanManage): pay rates are Admin / FarmManager only.
const canManageBands = computed(() => ['Admin', 'FarmManager'].includes(store.currentUser?.role ?? ''))
// Mirrors the backend rule (AddWorkerCommandHandler.CanAdd): the legacy 'Member' role can't add workers.
const canAddWorker = computed(() => ['Admin', 'FarmManager', 'Supervisor'].includes(store.currentUser?.role ?? ''))

onMounted(async () => {
  await Promise.all([store.fetchWorkers(), store.fetchWorkerRoles()])
})
</script>

<style scoped>
.kpi-grid { display: grid; grid-template-columns: repeat(4,1fr); gap: 12px; margin-bottom: 16px; }
.role-row { display: flex; align-items: center; gap: 12px; padding: 10px 16px; border-bottom: 1px solid var(--fog); }
.role-row:last-child { border-bottom: none; }
.role-title { font-size: 12px; font-weight: 600; color: var(--forest); flex: 1; }
.role-rate { font-family: 'JetBrains Mono', monospace; font-size: 12px; font-weight: 600; color: var(--forest); min-width: 120px; text-align: right; }
.role-count { min-width: 80px; text-align: right; }
</style>
