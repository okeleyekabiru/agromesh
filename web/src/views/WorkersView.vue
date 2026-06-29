<template>
  <div>
    <div class="kpi-grid">
      <KpiCard label="Active Workers"      value="18"    delta="↑ 3 this month"   deltaType="up"   :dark="true" />
      <KpiCard label="Contractors"         value="6"     delta="Seasonal"          deltaType="mute" />
      <KpiCard label="Labour Cost · MTD"   value="₦684K" delta="↓ 4% vs May"      deltaType="up"   />
      <KpiCard label="Days Logged · MTD"   value="312"   delta="Avg 17.3/worker"   deltaType="mute" />
    </div>

    <div class="card">
      <div class="card-header">
        <div class="card-title">Workforce</div>
        <div style="display:flex;gap:8px;align-items:center;">
          <StatBadge variant="green">{{ store.workers.filter(w=>w.type==='Employee').length }} Employees</StatBadge>
          <StatBadge variant="blue">{{ store.workers.filter(w=>w.type==='Contractor').length }} Contractors</StatBadge>
        </div>
      </div>
      <WorkerRow
        v-for="(w, i) in store.workers"
        :key="w.id"
        :worker="w"
        :daysLogged="daysLogged[i]"
      />
    </div>
  </div>
</template>

<script setup lang="ts">
import { useAgroStore } from '@/stores/agro'
import KpiCard from '@/components/ui/KpiCard.vue'
import StatBadge from '@/components/ui/StatBadge.vue'
import WorkerRow from '@/components/ui/WorkerRow.vue'

const store = useAgroStore()
const daysLogged = [22, 20, 18, 19, 14, 10]
</script>

<style scoped>
.kpi-grid { display: grid; grid-template-columns: repeat(4,1fr); gap: 12px; margin-bottom: 16px; }
</style>
