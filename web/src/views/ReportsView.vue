<template>
  <div>
    <div class="month-bar">
      <button class="btn btn-ghost btn-sm" @click="shift(-1)">‹ Prev</button>
      <div class="month-name">{{ monthLabel }}</div>
      <button class="btn btn-ghost btn-sm" :disabled="isCurrentMonth" @click="shift(1)">Next ›</button>
    </div>

    <div v-if="error" class="error-msg" style="margin-bottom:12px;">{{ error }}</div>
    <div v-if="loading && !report" class="empty-state">Loading report…</div>

    <template v-if="report">
      <div class="grid-2">
        <div class="card">
          <div class="card-header">
            <div class="card-title">Monthly Summary</div>
            <div class="card-label">{{ monthLabel }}</div>
          </div>
          <div class="report-stat">
            <div class="report-label">Total Expenditure</div>
            <div class="report-value">{{ money(report.totalExpenditure) }}</div>
          </div>
          <div class="report-stat">
            <div class="report-label">Labour (activities)</div>
            <div class="report-value">{{ money(report.labourCost) }}</div>
          </div>
          <div class="report-stat">
            <div class="report-label">Stock used (activities)</div>
            <div class="report-value">{{ money(report.inputCost) }}</div>
          </div>
          <div class="report-stat">
            <div class="report-label">Bank spend</div>
            <div class="report-value">{{ money(report.bankSpend) }}</div>
          </div>
          <div class="report-stat">
            <div class="report-label">Activities logged</div>
            <div class="report-value">{{ report.activityCount }}</div>
          </div>
        </div>

        <div class="card">
          <div class="card-header">
            <div class="card-title">Cost per {{ areaWord }}</div>
            <div class="card-label">By block</div>
          </div>
          <div v-if="!report.blocks.length" class="empty-sm">No blocks yet</div>
          <div v-for="b in report.blocks" :key="b.blockId" class="report-stat">
            <div>
              <div class="report-label">{{ b.blockName }}<template v-if="b.cropType"> — {{ cropLabel(b.cropType) }}</template></div>
              <div class="sub">{{ b.area.toLocaleString() }} {{ report.areaUnit }} · {{ b.activityCount }} {{ b.activityCount === 1 ? 'activity' : 'activities' }} · {{ money(b.cost) }}</div>
            </div>
            <div>
              <template v-if="b.costPerArea !== null">
                <span class="report-value">{{ money(b.costPerArea) }}</span>
                <span class="per-ha">/{{ report.areaUnit }}</span>
              </template>
              <span v-else class="no-area">Set block area</span>
            </div>
          </div>
        </div>
      </div>

      <!-- 6-month trend ending at the selected month -->
      <div class="card" style="margin-top:16px;">
        <div class="card-header">
          <div class="card-title">6-Month Spend Trend</div>
          <div class="card-label">₦ thousands</div>
        </div>
        <div style="padding:20px 24px 16px;">
          <div class="trend">
            <div v-for="m in monthBars" :key="m.key" class="trend-col" :title="`${m.label} · ${money(m.amount)}`">
              <div class="trend-val">{{ m.val }}</div>
              <div class="trend-bar" :style="{ height: m.h + 'px', background: m.current ? 'var(--palm)' : 'var(--fog)' }" />
              <div class="trend-val">{{ m.label }}</div>
            </div>
          </div>
        </div>
      </div>

      <div class="grid-2" style="margin-top:16px;">
        <div class="card">
          <div class="card-header">
            <div class="card-title">Cost by Category</div>
            <div class="card-label">{{ monthLabel }}</div>
          </div>
          <div v-if="!report.categories.length" class="empty-sm">No costs recorded this month</div>
          <div v-for="c in report.categories" :key="c.label" class="report-stat">
            <div style="display:flex;align-items:center;gap:8px;">
              <span style="font-size:14px;">{{ CATEGORY_ICONS[c.label] ?? '📦' }}</span>
              <div class="report-label">{{ c.label }}</div>
            </div>
            <div style="display:flex;align-items:center;gap:10px;">
              <StatBadge variant="stone">{{ Math.round(c.percent) }}%</StatBadge>
              <div class="report-value" style="font-size:14px;">{{ money(c.amount) }}</div>
            </div>
          </div>
        </div>

        <div class="card">
          <div class="card-header">
            <div class="card-title">Cost by Activity</div>
            <div class="card-label">{{ monthLabel }}</div>
          </div>
          <div v-if="!report.byActivityType.length" class="empty-sm">No activities logged this month</div>
          <div v-for="t in report.byActivityType" :key="t.activityType" class="report-stat">
            <div>
              <div class="report-label">{{ ACTIVITY_LABELS[t.activityType] ?? t.activityType }}</div>
              <div class="sub">{{ t.count }} {{ t.count === 1 ? 'activity' : 'activities' }}</div>
            </div>
            <div class="report-value" style="font-size:14px;">{{ money(t.cost) }}</div>
          </div>
        </div>
      </div>
    </template>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue'
import { useAgroStore } from '@/stores/agro'
import StatBadge from '@/components/ui/StatBadge.vue'
import type { Report } from '@/types'
import { extractErrorMessage } from '@/utils/http'

const store = useAgroStore()

const now = new Date()
const year = ref(now.getFullYear())
const month = ref(now.getMonth() + 1) // 1-12
const report = ref<Report | null>(null)
const loading = ref(false)
const error = ref('')

const MONTHS = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec']
const CATEGORY_ICONS: Record<string, string> = {
  'Labour': '👷', 'Fertilizer & Chemicals': '🌱', 'Fuel & Transport': '⛽',
  'Equipment & Maintenance': '🔧', 'Seeds & Other': '📦'
}
const ACTIVITY_LABELS: Record<string, string> = {
  Weeding: 'Weeding', Spraying: 'Spraying', FertilizerApplication: 'Fertilizer Application',
  Maintenance: 'Maintenance', Harvesting: 'Harvesting', Clearing: 'Clearing',
  Pruning: 'Pruning', PestControl: 'Pest Control'
}

const monthLabel = computed(() =>
  new Date(year.value, month.value - 1, 1).toLocaleDateString('en-NG', { month: 'long', year: 'numeric' }))
const areaWord = computed(() => (report.value?.areaUnit === 'ac' ? 'Acre' : 'Hectare'))
const isCurrentMonth = computed(() => year.value === now.getFullYear() && month.value === now.getMonth() + 1)

function money(n: number) {
  return '₦' + Math.round(n).toLocaleString('en-NG')
}

function cropLabel(c: string) {
  return c.replace(/([a-z])([A-Z])/g, '$1 $2')
}

function shift(delta: number) {
  const d = new Date(year.value, month.value - 1 + delta, 1)
  year.value = d.getFullYear()
  month.value = d.getMonth() + 1
}

const monthBars = computed(() => {
  const t = report.value?.spendTrend ?? []
  const max = Math.max(...t.map(m => m.amount), 0)
  return t.map((m, i) => ({
    key: `${m.year}-${m.month}`,
    label: MONTHS[m.month - 1],
    amount: m.amount,
    val: m.amount > 0 ? (m.amount >= 1_000_000 ? `${(m.amount / 1_000_000).toFixed(2)}M` : `${Math.round(m.amount / 1000)}K`) : '—',
    h: m.amount > 0 && max > 0 ? Math.max(4, Math.round((m.amount / max) * 90)) : 4,
    current: i === t.length - 1
  }))
})

async function load() {
  loading.value = true
  error.value = ''
  try {
    report.value = await store.fetchReport(year.value, month.value)
  } catch (e) {
    error.value = extractErrorMessage(e, 'Could not load this report.')
  } finally {
    loading.value = false
  }
}

watch([year, month], load)
onMounted(load)
</script>

<style scoped>
.grid-2 { display: grid; grid-template-columns: 1fr 1fr; gap: 16px; }
.month-bar { display: flex; align-items: center; justify-content: center; gap: 16px; margin-bottom: 16px; }
.month-name { font-family: 'Syne', sans-serif; font-weight: 700; font-size: 16px; color: var(--forest); min-width: 160px; text-align: center; }
.report-stat { padding: 14px 16px; border-bottom: 1px solid var(--fog); display: flex; align-items: center; justify-content: space-between; }
.report-stat:last-child { border-bottom: none; }
.report-label { font-size: 12px; color: var(--ink); }
.report-value { font-family: 'Syne', sans-serif; font-weight: 700; font-size: 16px; color: var(--forest); letter-spacing: -0.01em; }
.sub { font-family: 'JetBrains Mono', monospace; font-size: 9px; color: var(--stone); margin-top: 2px; }
.no-area { font-family: 'JetBrains Mono', monospace; font-size: 10px; color: var(--harvest); }
.per-ha { font-size: 10px; color: var(--stone); font-family: 'Inter', sans-serif; margin-left: 2px; }
.trend { display: flex; align-items: flex-end; gap: 8px; height: 120px; }
.trend-col { flex: 1; height: 100%; display: flex; flex-direction: column; align-items: center; justify-content: flex-end; gap: 4px; }
.trend-bar { width: 100%; border-radius: 3px 3px 0 0; min-height: 4px; }
.trend-val { font-family: 'JetBrains Mono', monospace; font-size: 8px; color: var(--stone); }
.empty-sm, .empty-state { padding: 24px; text-align: center; color: var(--stone); font-size: 12px; }
@media (max-width: 800px) { .grid-2 { grid-template-columns: 1fr; } }
</style>
