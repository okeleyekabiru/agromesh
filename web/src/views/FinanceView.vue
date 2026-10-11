<template>
  <div>
    <BankAccountCard @changed="reload" />

    <div class="kpi-grid kpi-money">
      <KpiCard label="Income · MTD"  :value="money(summary?.income)"   delta="money in, not capital" deltaType="up" />
      <KpiCard label="Capital · MTD" :value="money(summary?.capital)"  delta="owner's own investment" deltaType="mute" />
      <KpiCard label="Expenses · MTD" :value="money(summary?.expenses)" delta="money out" deltaType="down" />
      <KpiCard label="To review" :value="`${queue.length}`" delta="transfers naming an order" :deltaType="queue.length ? 'warn' : 'mute'" />
    </div>

    <div v-if="queue.length" class="card review-card">
      <div class="review-head">Transfers that need a person</div>
      <div v-for="t in queue" :key="t.id" class="review-row">
        <div class="review-main" style="cursor:pointer;" @click="selected = t">
          <div class="review-narr">{{ t.narration }}</div>
          <div class="review-note">{{ t.reconciliationNote }}</div>
          <div class="review-meta">{{ new Date(t.date).toLocaleDateString('en-GB') }} · ₦{{ Math.abs(t.amount).toLocaleString() }}</div>
        </div>
        <div class="review-actions">
          <button v-if="t.orderId" class="btn-primary" :disabled="resolving === t.id" @click="resolve(t.id, true)">Confirm paid</button>
          <button class="btn-secondary" :disabled="resolving === t.id" @click="resolve(t.id, false)">Not an order payment</button>
        </div>
      </div>
      <div v-if="resolveError" class="error-msg" style="padding:0 16px 12px;">{{ resolveError }}</div>
    </div>

    <TransactionDetailModal v-if="selected" :txn="selected" @close="selected = null" @saved="reload" />

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
      <TransactionRow v-for="t in filtered" :key="t.id" :txn="t" :showAlloc="true" :clickable="true" @open="selected = t" />
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
import { useRoute } from 'vue-router'
import { useAgroStore } from '@/stores/agro'
import KpiCard from '@/components/ui/KpiCard.vue'
import FilterPill from '@/components/ui/FilterPill.vue'
import TransactionRow from '@/components/ui/TransactionRow.vue'
import EmptyState from '@/components/ui/EmptyState.vue'
import BankAccountCard from '@/components/ui/BankAccountCard.vue'
import TransactionDetailModal from '@/components/modal/TransactionDetailModal.vue'
import { transactionApi } from '@/services/api'
import { extractErrorMessage } from '@/utils/http'
import type { FinanceSummary, Transaction } from '@/types'

const store = useAgroStore()
const route = useRoute()
// Arriving from the dashboard's "Review now" / Pending Review card opens straight on the unreviewed ones.
const activeFilter = ref(route.query.filter === 'pending' ? 'Pending Review' : 'All')
const selected = ref<Transaction | null>(null)
const filters = ['All', 'Fertilizer', 'Labour', 'Fuel', 'Equipment', 'Pending Review']

const filtered = computed(() => {
  if (activeFilter.value === 'All') return store.transactions
  if (activeFilter.value === 'Pending Review') return store.transactions.filter(t => !t.reviewedByUser)
  return store.transactions.filter(t => t.category === activeFilter.value)
})

const summary = ref<FinanceSummary | null>(null)
const queue = ref<Transaction[]>([])
const resolving = ref('')
const resolveError = ref('')

const money = (n: number | undefined) => (n === undefined ? '—' : `₦${n.toLocaleString()}`)

async function loadMoney() {
  try {
    summary.value = (await transactionApi.summary()).data
    queue.value = (await transactionApi.needsReview()).data
  } catch {
    /* the cards stay empty; the transaction list below still works */
  }
}

async function reload() {
  await store.fetchTransactions()
  await loadMoney()
}

async function resolve(id: string, confirm: boolean) {
  resolving.value = id
  resolveError.value = ''
  try {
    await transactionApi.reconcile(id, { confirm })
    await loadMoney()
    if (confirm) await store.fetchOrders?.()
  } catch (e) {
    resolveError.value = extractErrorMessage(e, 'Could not update this transfer.')
  } finally {
    resolving.value = ''
  }
}

onMounted(() => { reload(); store.fetchOrders?.() })
</script>

<style scoped>
.kpi-grid { display: grid; grid-template-columns: repeat(4,1fr); gap: 12px; margin-bottom: 16px; }
.review-card { margin-bottom: 16px; }
.review-head { padding: 12px 16px; font-weight: 600; color: var(--forest); border-bottom: 1px solid var(--fog); }
.review-row { display: flex; gap: 12px; justify-content: space-between; align-items: center; padding: 12px 16px; border-bottom: 1px solid var(--fog); flex-wrap: wrap; }
.review-narr { font-weight: 600; font-size: 13px; }
.review-note { font-size: 12px; color: var(--harvest, #b7791f); margin-top: 2px; }
.review-meta { font-size: 11px; color: var(--stone); margin-top: 2px; }
.review-actions { display: flex; gap: 8px; flex-wrap: wrap; }
@media (max-width: 900px) { .kpi-grid { grid-template-columns: repeat(2, 1fr); } }
</style>
