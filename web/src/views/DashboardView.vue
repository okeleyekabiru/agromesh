<template>
  <div>
    <AlertBanner v-if="store.pendingReviews > 0" action="Review now" @action="router.push('/finance')">
      <strong>{{ store.pendingReviews }} transactions</strong> need categorization review before tonight's P&L update.
    </AlertBanner>
    <AlertBanner v-if="store.lowStockCount > 0" @action="router.push('/inventory')" action="View inventory">
      <strong>{{ store.lowStockCount }} item{{ store.lowStockCount > 1 ? 's' : '' }}</strong> below reorder threshold.
    </AlertBanner>

    <!-- KPIs -->
    <div class="kpi-grid">
      <KpiCard
        label="Total Spend · MTD"
        :value="store.dashboard ? `₦${Math.round(store.dashboard.totalSpendMtd/1000).toLocaleString()}K` : '—'"
        :delta="store.dashboard ? `Labour: ₦${Math.round(store.dashboard.labourCostMtd/1000).toLocaleString()}K` : 'Loading…'"
        deltaType="down"
        :dark="true"
      />
      <KpiCard
        label="Activities · MTD"
        :value="store.dashboard ? `${store.dashboard.activitiesThisMonth}` : '—'"
        delta="logged this month"
        deltaType="up"
      />
      <KpiCard
        label="Sections"
        :value="`${store.sections.length}`"
        delta="farm sections"
        deltaType="mute"
      />
      <KpiCard
        label="Pending Review"
        :value="store.dashboard ? `${store.dashboard.pendingReviewCount}` : '—'"
        :delta="store.pendingReviews > 0 ? 'Need attention' : 'All reviewed'"
        :deltaType="store.pendingReviews > 0 ? 'warn' : 'up'"
      />
    </div>

    <!-- Weather + Upcoming Tasks row -->
    <div class="grid-weather">
      <!-- Weather Widget -->
      <div class="card weather-card" v-if="store.weather">
        <div class="weather-header">
          <div>
            <div class="card-title" style="font-size:13px;">Weather · Farm</div>
            <div class="season-badge">{{ store.weather.currentSeason }}</div>
          </div>
          <div class="weather-temp">{{ store.weather.todayTempMaxC }}°C</div>
        </div>
        <div class="weather-stats">
          <div class="weather-stat">
            <div class="wstat-label">RAIN TODAY</div>
            <div class="wstat-val">{{ store.weather.todayPrecipitationMm }} mm</div>
          </div>
          <div class="weather-stat">
            <div class="wstat-label">LAST 30 DAYS</div>
            <div class="wstat-val">{{ store.weather.rainfallLast30Days }} mm</div>
          </div>
        </div>
        <div v-if="store.weather.nextDryWindowStart" class="spray-window">
          <div class="spray-icon">💧</div>
          <div>
            <div class="spray-label">BEST SPRAY WINDOW</div>
            <div class="spray-dates">{{ formatDate(store.weather.nextDryWindowStart) }}
              <span v-if="store.weather.nextDryWindowEnd"> – {{ formatDate(store.weather.nextDryWindowEnd) }}</span>
            </div>
          </div>
        </div>
      </div>
      <div v-else class="card weather-card weather-empty">
        <div style="font-size:24px;margin-bottom:6px;">🌤</div>
        <div style="font-size:11px;color:var(--stone);">Weather data unavailable</div>
      </div>

      <!-- Upcoming Tasks -->
      <div class="card" style="flex:1;min-width:0;">
        <div class="card-header">
          <div class="card-title">Upcoming Tasks</div>
          <div class="card-label">AI PREDICTIONS</div>
        </div>
        <div v-if="!store.upcomingTasks.length" class="empty-tasks">
          <div style="font-size:11px;color:var(--stone);padding:16px;">No upcoming tasks predicted yet — log your first activity to trigger AI scheduling.</div>
        </div>
        <div v-for="task in store.upcomingTasks.slice(0,5)" :key="`${task.sectionId}-${task.activityType}`" class="task-row">
          <div class="task-urgency" :class="urgencyClass(task.daysUntilDue)" />
          <div class="task-info">
            <div class="task-main">
              <span class="task-section">{{ task.sectionName }}</span>
              <span class="task-sep">→</span>
              <span class="task-type">{{ task.activityType }}</span>
              <span class="task-due" :class="urgencyClass(task.daysUntilDue)">
                {{ task.daysUntilDue < 0 ? 'OVERDUE' : `in ${task.daysUntilDue}d` }}
              </span>
              <span v-if="!task.weatherDataUsed" class="no-weather-tip" title="Prediction made without live weather data">⚡</span>
            </div>
            <div class="task-reasoning">{{ task.reasoning }}</div>
            <div v-if="task.recommendedChemical" class="task-chem">
              💧 {{ task.recommendedChemical }}
              <span v-if="task.stockAvailable !== null" class="task-stock">
                · {{ task.stockAvailable }} {{ task.stockUnit }} in stock
              </span>
            </div>
          </div>
        </div>
      </div>
    </div>

    <div class="grid-3-1">
      <!-- Section costs -->
      <div class="card">
        <div class="card-header">
          <div class="card-title">Section Costs — MTD</div>
          <div class="card-label">BY SECTION</div>
        </div>
        <template v-if="store.dashboard?.sectionsMtd?.length">
          <SectionRow
            v-for="s in store.dashboard.sectionsMtd"
            :key="s.sectionId"
            :section="{ id: s.sectionId, name: s.sectionName } as any"
            :amount="s.cost.toLocaleString('en-NG')"
            :pct="maxSectionCost > 0 ? Math.round((s.cost / maxSectionCost) * 100) : 0"
            @click="router.push('/sections')"
          />
        </template>
        <template v-else-if="store.sections.length">
          <SectionRow
            v-for="(s, i) in store.sections"
            :key="s.id"
            :section="s"
            :amount="sectionAmounts[i] ?? '0'"
            :pct="sectionPcts[i] ?? 0"
            @click="router.push('/sections')"
          />
        </template>
        <div v-else style="padding:20px;text-align:center;color:var(--stone);font-size:12px;">No section data yet</div>
      </div>

      <!-- Crop P&L -->
      <div class="card">
        <div class="card-header">
          <div class="card-title">Crop P&L</div>
          <div class="card-label">Auto-categorized</div>
        </div>
        <template v-if="store.dashboard?.cropPnl?.length">
          <div class="crop-split">
            <div v-for="cp in store.dashboard.cropPnl" :key="cp.cropType" class="crop-card" style="border-radius:10px;padding:14px;">
              <div style="font-family:'JetBrains Mono',monospace;font-size:9px;letter-spacing:0.12em;text-transform:uppercase;color:var(--leaf);margin-bottom:8px;">{{ cp.cropType }}</div>
              <div style="font-family:'Syne',sans-serif;font-weight:700;font-size:22px;color:var(--forest);letter-spacing:-0.02em;">₦{{ (cp.cost/1000).toFixed(0) }}K</div>
              <div style="font-size:11px;color:var(--stone);margin-top:4px;">{{ cp.netProfit >= 0 ? '+' : '' }}₦{{ (cp.netProfit/1000).toFixed(0) }}K net</div>
            </div>
          </div>
        </template>
        <div v-else class="crop-split">
          <div class="crop-card crop-card-palm" style="border-radius:10px;padding:14px;">
            <div style="font-family:'JetBrains Mono',monospace;font-size:9px;letter-spacing:0.12em;text-transform:uppercase;color:var(--leaf);margin-bottom:8px;">Oil Palm</div>
            <div style="font-family:'Syne',sans-serif;font-weight:700;font-size:22px;color:var(--forest);letter-spacing:-0.02em;">—</div>
          </div>
          <div class="crop-card crop-card-maize" style="border-radius:10px;padding:14px;">
            <div style="font-family:'JetBrains Mono',monospace;font-size:9px;letter-spacing:0.12em;text-transform:uppercase;color:#8A6000;margin-bottom:8px;">Maize</div>
            <div style="font-family:'Syne',sans-serif;font-weight:700;font-size:22px;color:var(--forest);letter-spacing:-0.02em;">—</div>
          </div>
        </div>
      </div>
    </div>

    <!-- Recent rows -->
    <div class="grid-2">
      <div class="card">
        <div class="card-header">
          <div class="card-title">Recent Activities</div>
          <button class="btn btn-ghost btn-sm" @click="router.push('/activities')">View all</button>
        </div>
        <div v-if="!store.activities.length" style="padding:20px;text-align:center;color:var(--stone);font-size:12px;">No activities yet</div>
        <ActivityRow v-for="a in store.activities.slice(0,4)" :key="a.id" :activity="a" />
      </div>
      <div class="card">
        <div class="card-header">
          <div class="card-title">Recent Transactions</div>
          <button class="btn btn-ghost btn-sm" @click="router.push('/finance')">View all</button>
        </div>
        <div v-if="!store.transactions.length" style="padding:20px;text-align:center;color:var(--stone);font-size:12px;">No transactions yet</div>
        <TransactionRow v-for="t in store.transactions.slice(0,4)" :key="t.id" :txn="t" />
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useAgroStore } from '@/stores/agro'
import KpiCard from '@/components/ui/KpiCard.vue'
import AlertBanner from '@/components/ui/AlertBanner.vue'
import SectionRow from '@/components/ui/SectionRow.vue'
import ActivityRow from '@/components/ui/ActivityRow.vue'
import TransactionRow from '@/components/ui/TransactionRow.vue'

const router = useRouter()
const store = useAgroStore()

const maxSectionCost = computed(() => {
  if (!store.dashboard?.sectionsMtd?.length) return 1
  return Math.max(...store.dashboard.sectionsMtd.map(s => s.cost), 1)
})

const sectionAmounts = ['612,400', '284,500', '340,000', '98,200', '168,000', '62,400']
const sectionPcts    = [100, 46, 56, 16, 28, 10]

function formatDate(iso: string) {
  return new Date(iso).toLocaleDateString('en-NG', { day: 'numeric', month: 'short' })
}

function urgencyClass(daysUntilDue: number) {
  if (daysUntilDue < 0) return 'overdue'
  if (daysUntilDue <= 7) return 'soon'
  return 'ok'
}

onMounted(async () => {
  await Promise.all([
    store.initDashboard(),
    store.fetchSections(),
    store.fetchActivities(),
    store.fetchTransactions()
  ])
})
</script>

<style scoped>
.kpi-grid { display: grid; grid-template-columns: repeat(4,1fr); gap: 12px; margin-bottom: 20px; }
.grid-weather { display: grid; grid-template-columns: 240px 1fr; gap: 16px; margin-bottom: 16px; align-items: start; }
.grid-3-1 { display: grid; grid-template-columns: 1fr 360px; gap: 16px; margin-bottom: 16px; }
.grid-2   { display: grid; grid-template-columns: 1fr 1fr; gap: 16px; }
.crop-split { display: grid; grid-template-columns: 1fr 1fr; gap: 10px; padding: 16px; }

/* Weather */
.weather-card { padding: 16px; }
.weather-empty { display: flex; flex-direction: column; align-items: center; justify-content: center; min-height: 160px; }
.weather-header { display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 12px; }
.weather-temp { font-family: 'Syne', sans-serif; font-weight: 700; font-size: 32px; color: var(--forest); }
.season-badge { display: inline-block; font-family: 'JetBrains Mono', monospace; font-size: 8px; letter-spacing: 0.1em; text-transform: uppercase; padding: 3px 7px; background: rgba(45,122,79,0.08); border: 1px solid rgba(45,122,79,0.2); border-radius: 4px; color: var(--leaf); margin-top: 4px; }
.weather-stats { display: flex; gap: 12px; margin-bottom: 12px; }
.weather-stat { flex: 1; }
.wstat-label { font-family: 'JetBrains Mono', monospace; font-size: 8px; letter-spacing: 0.1em; text-transform: uppercase; color: var(--stone); margin-bottom: 2px; }
.wstat-val { font-family: 'JetBrains Mono', monospace; font-size: 14px; font-weight: 600; color: var(--forest); }
.spray-window { display: flex; align-items: center; gap: 8px; background: rgba(61,170,106,0.06); border: 1px solid rgba(61,170,106,0.15); border-radius: 8px; padding: 8px 10px; }
.spray-icon { font-size: 16px; }
.spray-label { font-family: 'JetBrains Mono', monospace; font-size: 8px; letter-spacing: 0.1em; color: var(--stone); }
.spray-dates { font-size: 11px; font-weight: 600; color: var(--forest); margin-top: 1px; }

/* Tasks */
.empty-tasks { }
.task-row { display: flex; gap: 10px; padding: 10px 16px; border-bottom: 1px solid var(--fog); }
.task-row:last-child { border-bottom: none; }
.task-urgency { width: 3px; border-radius: 2px; flex-shrink: 0; min-height: 36px; }
.task-urgency.ok { background: var(--palm); }
.task-urgency.soon { background: var(--harvest); }
.task-urgency.overdue { background: #E53935; }
.task-info { flex: 1; }
.task-main { display: flex; align-items: center; gap: 6px; flex-wrap: wrap; }
.task-section { font-size: 12px; font-weight: 600; color: var(--forest); }
.task-sep { font-size: 10px; color: var(--stone); }
.task-type { font-size: 12px; color: var(--leaf); font-weight: 500; }
.task-due { font-family: 'JetBrains Mono', monospace; font-size: 9px; letter-spacing: 0.08em; padding: 2px 6px; border-radius: 4px; }
.task-due.ok { background: rgba(61,170,106,0.1); color: var(--leaf); }
.task-due.soon { background: #FFF0C0; color: #8A6000; }
.task-due.overdue { background: #FEECEC; color: #C62828; }
.no-weather-tip { font-size: 10px; cursor: help; opacity: 0.6; }
.task-reasoning { font-size: 10px; color: var(--stone); font-style: italic; margin-top: 2px; }
.task-chem { font-size: 10px; color: var(--stone); margin-top: 3px; }
.task-stock { font-family: 'JetBrains Mono', monospace; font-size: 9px; }
</style>
