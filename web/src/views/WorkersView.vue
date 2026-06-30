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
        </div>
      </div>
      <div v-if="store.loading" style="padding:24px;text-align:center;color:var(--stone);font-size:12px;">Loading workers…</div>
      <WorkerRow v-for="w in store.workers" :key="w.id" :worker="w" />
      <div v-if="!store.loading && !store.workers.length" style="padding:40px;text-align:center;color:var(--stone);font-size:12px;">No workers yet</div>
    </div>

    <!-- Worker Roles -->
    <div class="card" style="margin-top:16px;">
      <div class="card-header">
        <div class="card-title">Wage Bands</div>
        <div class="card-label">BY ROLE</div>
      </div>
      <div class="role-row" v-for="role in store.workerRoles" :key="role.id">
        <div class="role-title">{{ role.title }}</div>
        <StatBadge :variant="role.defaultType === 'Employee' ? 'green' : 'blue'">{{ role.defaultType }}</StatBadge>
        <div class="role-rate">₦{{ role.dailyRate.toLocaleString() }} / day</div>
        <div class="role-count" style="font-family:'JetBrains Mono',monospace;font-size:10px;color:var(--stone);">{{ role.workerCount }} workers</div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { onMounted } from 'vue'
import { useAgroStore } from '@/stores/agro'
import KpiCard from '@/components/ui/KpiCard.vue'
import StatBadge from '@/components/ui/StatBadge.vue'
import WorkerRow from '@/components/ui/WorkerRow.vue'

const store = useAgroStore()

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
