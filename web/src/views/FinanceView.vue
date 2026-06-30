<template>
  <div>
    <div class="kpi-grid">
      <KpiCard label="Total · MTD"     :value="store.dashboard ? `₦${(store.dashboard.totalSpendMtd/1000).toFixed(0)}K` : '—'"  delta="auto-categorized" deltaType="down" :dark="true" />
      <KpiCard label="Transactions"    :value="`${store.transactions.length}`"                                                    delta="synced from Mono" deltaType="mute" />
      <KpiCard label="Reviewed"        :value="`${store.transactions.filter(t=>t.reviewedByUser).length}`"                        delta="manually confirmed" deltaType="up" />
      <KpiCard label="Pending Review"  :value="`${store.pendingReviews}`"                                                         delta="Need categorization" :deltaType="store.pendingReviews > 0 ? 'warn' : 'mute'" />
    </div>

    <div class="card">
      <div style="display:flex;align-items:center;gap:8px;padding:12px 16px;border-bottom:1px solid var(--fog);flex-wrap:wrap;">
        <FilterPill v-for="f in filters" :key="f" :active="activeFilter === f" @click="activeFilter = f">
          {{ f }}
        </FilterPill>
      </div>

      <div v-if="store.loading" style="padding:24px;text-align:center;color:var(--stone);font-size:12px;">Loading transactions…</div>
      <TransactionRow v-for="t in filtered" :key="t.id" :txn="t" :showAlloc="true" />
      <EmptyState
        v-if="!store.loading && filtered.length === 0"
        icon="🔍"
        title="No transactions found"
        subtitle="Try a different filter or check back after the next sync."
      />
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
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
  if (activeFilter.value === 'Pending Review') return store.transactions.filter(t => !t.reviewedByUser)
  return store.transactions.filter(t => t.category === activeFilter.value)
})

onMounted(() => store.fetchTransactions())
</script>

<style scoped>
.kpi-grid { display: grid; grid-template-columns: repeat(4,1fr); gap: 12px; margin-bottom: 16px; }
</style>
