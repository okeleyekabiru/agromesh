<template>
  <div>
    <div class="kpi-grid">
      <KpiCard label="Total · MTD"     value="₦1.54M"  delta="↑ 8% vs May"      deltaType="down" :dark="true" />
      <KpiCard label="Oil Palm"        value="₦1.12M"  delta="73% of spend"      deltaType="mute" />
      <KpiCard label="Maize"           value="₦420K"   delta="27% of spend"      deltaType="mute" />
      <KpiCard label="Pending Review"  :value="`${store.pendingReviews}`" delta="Need categorization" deltaType="warn" />
    </div>

    <div class="card">
      <div style="display:flex;align-items:center;gap:8px;padding:12px 16px;border-bottom:1px solid var(--fog);flex-wrap:wrap;">
        <FilterPill v-for="f in filters" :key="f" :active="activeFilter === f" @click="activeFilter = f">
          {{ f }}
        </FilterPill>
      </div>

      <TransactionRow
        v-for="t in filtered"
        :key="t.id"
        :txn="t"
        :showAlloc="true"
      />

      <EmptyState
        v-if="filtered.length === 0"
        icon="🔍"
        title="No transactions found"
        subtitle="Try a different filter or check back after the next sync at 2am."
      />
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { useAgroStore } from '@/stores/agro'
import KpiCard from '@/components/ui/KpiCard.vue'
import FilterPill from '@/components/ui/FilterPill.vue'
import TransactionRow from '@/components/ui/TransactionRow.vue'
import EmptyState from '@/components/ui/EmptyState.vue'

const store = useAgroStore()
const activeFilter = ref('All')
const filters = ['All', 'Fertilizer', 'Labour', 'Fuel', 'Equipment', 'Pending Review']

const filtered = computed(() => {
  if (activeFilter.value === 'All') return store.transactions
  if (activeFilter.value === 'Pending Review') return store.transactions.filter(t => t.confidence !== 'high')
  return store.transactions.filter(t => t.category === activeFilter.value)
})
</script>

<style scoped>
.kpi-grid { display: grid; grid-template-columns: repeat(4,1fr); gap: 12px; margin-bottom: 16px; }
</style>
