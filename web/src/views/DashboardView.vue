<template>
  <div>
    <!-- Alert banners -->
    <AlertBanner
      v-if="store.farm?.setupStatus === 0"
      action="Complete setup"
      @action="router.push({ name: 'onboarding' })"
    >
      <strong>Your farm setup isn't finished.</strong> Add your location, blocks and boundaries to unlock scheduling and reporting.
    </AlertBanner>
    <AlertBanner
      v-if="store.pendingReviews > 0"
      action="Review now"
      @action="router.push({ path: '/finance', query: { filter: 'pending' } })"
    >
      <strong>{{ store.pendingReviews }} transactions</strong> need categorization review before tonight's P&amp;L update.
    </AlertBanner>
    <AlertBanner
      v-if="store.lowStockCount > 0"
      action="View inventory"
      @action="router.push('/inventory')"
    >
      <strong>{{ store.lowStockCount }} item{{ store.lowStockCount > 1 ? 's' : '' }}</strong> below reorder threshold.
    </AlertBanner>

    <!-- ── Hero command strip ── -->
    <div class="hero">
      <div>
        <div class="hero-name">{{ store.farm?.name ?? 'AgroMesh' }}</div>
        <div class="hero-meta">
          <span v-if="store.farm?.location">{{ store.farm.location }}</span>
          <span v-if="store.farm?.location" class="hdot">·</span>
          <span>{{ todayLabel }}</span>
          <template v-if="store.lastSynced">
            <span class="hdot">·</span>
            <span class="hero-sync">
              <span class="live-dot" />
              Synced {{ store.lastSynced }}
            </span>
          </template>
        </div>
      </div>
      <div v-if="store.weather" class="hero-wx">
        <div class="hero-temp">{{ store.weather.todayTempMaxC }}<span class="hero-deg">°C</span></div>
        <div class="hero-season">{{ store.weather.currentSeason }}</div>
      </div>
    </div>

    <!-- ── KPI row ── -->
    <div class="kpi-row">
      <!-- Primary: total spend with sparkline -->
      <div class="kpi-primary">
        <div class="kpi-top">
          <div class="kpi-lbl-inv">TOTAL SPEND · MTD</div>
          <svg class="kpi-spark" viewBox="0 0 80 28" preserveAspectRatio="none">
            <polyline
              :points="sparkPoints"
              fill="none"
              stroke="rgba(255,255,255,0.3)"
              stroke-width="1.5"
              stroke-linecap="round"
              stroke-linejoin="round"
            />
            <circle :cx="sparkEnd.x" :cy="sparkEnd.y" r="2.5" fill="var(--palm)" />
          </svg>
        </div>
        <div class="kpi-val-inv">
          {{ store.dashboard ? `₦${Math.round(store.dashboard.totalSpendMtd / 1000).toLocaleString()}K` : '—' }}
        </div>
        <div class="kpi-sub-inv">
          Labour ₦{{ store.dashboard ? Math.round(store.dashboard.labourCostMtd / 1000).toLocaleString() : '—' }}K
          <span style="opacity:0.3;margin:0 4px;">·</span>
          Maintenance ₦{{ maintenanceCostK }}K
        </div>
      </div>

      <!-- Activities MTD -->
      <div class="kpi-card-sm">
        <div class="kpi-lbl">ACTIVITIES · MTD</div>
        <div class="kpi-val">{{ store.dashboard?.activitiesThisMonth ?? '—' }}</div>
        <div class="kpi-sub-up">
          <svg width="9" height="9" viewBox="0 0 9 9" fill="none">
            <path d="M4.5 7.5V2M2 4.5l2.5-2.5 2.5 2.5" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"/>
          </svg>
          logged this month
        </div>
      </div>

      <!-- Blocks -->
      <div class="kpi-card-sm">
        <div class="kpi-lbl">BLOCKS</div>
        <div class="kpi-val">{{ store.blocks.length || '—' }}</div>
        <div class="kpi-sub-mute">active farm areas</div>
      </div>

      <!-- Pending review -->
      <div class="kpi-card-sm kpi-link" :class="{ 'kpi-warn': store.pendingReviews > 0 }"
           role="button" tabindex="0"
           @click="router.push({ path: '/finance', query: { filter: 'pending' } })"
           @keydown.enter="router.push({ path: '/finance', query: { filter: 'pending' } })">
        <div class="kpi-lbl">PENDING REVIEW</div>
        <div class="kpi-val">{{ store.dashboard?.pendingReviewCount ?? '—' }}</div>
        <div :class="store.pendingReviews > 0 ? 'kpi-sub-warn' : 'kpi-sub-up'">
          {{ store.pendingReviews > 0 ? 'Need attention' : 'All reviewed' }}
        </div>
      </div>
    </div>

    <!-- ── Spend Trend + Upcoming Tasks ── -->
    <div class="grid-trend">
      <!-- 6-month bar chart -->
      <div class="card">
        <div class="card-header">
          <div class="card-title">Spend Trend</div>
          <div class="card-label">6 MONTHS · ₦ THOUSANDS</div>
        </div>
        <div class="chart-area">
          <div class="bars">
            <div v-for="b in spendBars" :key="b.label" class="bar-col">
              <div class="bar-tip">{{ b.label }} · ₦{{ b.full }}</div>
              <div class="bar-val" :class="{ cur: b.current }">{{ b.valLabel }}</div>
              <div class="bar-fill" :class="{ cur: b.current }" :style="{ height: b.h + 'px' }" />
            </div>
          </div>
          <div class="bar-months">
            <span
              v-for="b in spendBars"
              :key="b.label"
              :class="b.current ? 'bar-month-cur' : ''"
            >{{ b.label }}</span>
          </div>
        </div>
      </div>

      <!-- Upcoming Tasks -->
      <div class="card">
        <div class="card-header">
          <div class="card-title">Upcoming Tasks</div>
          <div class="card-label">AI PREDICTIONS</div>
        </div>
        <div v-if="!store.upcomingTasks.length" class="tasks-empty">
          <svg width="28" height="28" viewBox="0 0 28 28" fill="none" stroke="var(--stone)" stroke-width="1.4" stroke-linecap="round">
            <rect x="7" y="4" width="14" height="20" rx="2"/>
            <path d="M10 9h8M10 13h8M10 17h5"/>
          </svg>
          <p>No tasks yet — log your first activity to trigger AI scheduling.</p>
        </div>
        <div
          v-for="task in store.upcomingTasks.slice(0, 5)"
          :key="`${task.blockId}-${task.activityType}`"
          class="task-row"
        >
          <div class="task-bar" :class="urgencyClass(task.daysUntilDue)" />
          <div class="task-body">
            <div class="task-head">
              <span class="task-sec">{{ task.blockName }}</span>
              <span class="task-arr">→</span>
              <span class="task-type">{{ activityTypeLabel(task.activityType) }}</span>
              <span class="task-badge" :class="urgencyClass(task.daysUntilDue)">
                {{ task.daysUntilDue < 0 ? 'OVERDUE' : `in ${task.daysUntilDue}d` }}
              </span>
              <span v-if="!task.weatherDataUsed" class="no-wx" title="Prediction made without live weather data">⚡</span>
            </div>
            <div class="task-reason" :title="(task.factors ?? []).join(' | ')">{{ task.reasoning }}<template v-if="task.recommendedSprayDate"> · 💧 spray {{ formatDate(task.recommendedSprayDate) }}</template></div>
            <div v-if="task.recommendedChemical" class="task-chem">
              {{ task.recommendedChemical }}
              <span v-if="task.stockAvailable !== null" class="task-stock">
                · {{ task.stockAvailable }}{{ task.stockUnit }} in stock
              </span>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- ── Weather + Block Costs + Crop P&L ── -->
    <div class="grid-3">
      <!-- Weather widget -->
      <div class="card wx-card">
        <template v-if="store.weather">
          <div class="wx-head">
            <div>
              <div class="card-label">WEATHER · {{ (store.weather.location || 'FARM').toUpperCase() }}</div>
              <div class="season-chip">{{ store.weather.currentSeason }}</div>
            </div>
            <div class="wx-temp">{{ store.weather.todayTempMaxC }}<span class="wx-deg">°C</span></div>
          </div>
          <div class="wx-stats">
            <div>
              <div class="card-label" style="margin-bottom:3px;">RAIN TODAY</div>
              <div class="wx-stat-val">{{ store.weather.todayPrecipitationMm }} mm</div>
            </div>
            <div>
              <div class="card-label" style="margin-bottom:3px;">LAST 30 DAYS</div>
              <div class="wx-stat-val">{{ store.weather.rainfallLast30Days }} mm</div>
            </div>
          </div>
          <div v-if="store.weather.nextDryWindowStart" class="wx-spray">
            <svg width="14" height="14" viewBox="0 0 14 14" fill="none" stroke="var(--leaf)" stroke-width="1.3" stroke-linecap="round">
              <path d="M7 1.5c0 2.5-3 3.5-3 5.5a3 3 0 006 0c0-2-3-3-3-5.5z"/>
            </svg>
            <div>
              <div class="wx-spray-lbl">BEST SPRAY WINDOW</div>
              <div class="wx-spray-dates">
                {{ formatDate(store.weather.nextDryWindowStart) }}
                <template v-if="store.weather.nextDryWindowEnd"> – {{ formatDate(store.weather.nextDryWindowEnd) }}</template>
              </div>
            </div>
          </div>
        </template>
        <div v-else class="wx-empty">
          <svg width="30" height="30" viewBox="0 0 30 30" fill="none" stroke="var(--stone)" stroke-width="1.3" stroke-linecap="round">
            <circle cx="15" cy="15" r="5"/>
            <path d="M15 3v2M15 25v2M3 15h2M25 15h2M7.1 7.1l1.4 1.4M21.5 21.5l1.4 1.4M7.1 22.9l1.4-1.4M21.5 8.5l1.4-1.4"/>
          </svg>
          <p>Weather unavailable</p>
        </div>
      </div>

      <!-- Block Costs MTD -->
      <div class="card">
        <div class="card-header">
          <div class="card-title">Block Costs · MTD</div>
          <div class="card-label">BY BLOCK</div>
        </div>
        <template v-if="blockRows.length">
          <div
            v-for="s in blockRows"
            :key="s.id"
            class="sec-row"
            @click="router.push('/blocks')"
          >
            <div class="sec-name">{{ s.name }}</div>
            <div class="sec-track">
              <div class="sec-fill" :style="{ width: s.pct + '%' }" />
            </div>
            <div class="sec-amt">₦{{ s.amount }}</div>
          </div>
        </template>
        <div v-else class="empty-sm">No block data yet</div>
      </div>

      <!-- Crop P&L with SVG donut -->
      <div class="card">
        <div class="card-header">
          <div class="card-title">Crop P&amp;L</div>
          <div class="card-label">AUTO-CATEGORIZED</div>
        </div>
        <div class="pnl-body">
          <div class="pnl-donut-wrap">
            <svg viewBox="0 0 88 88" class="pnl-donut">
              <!-- track -->
              <circle cx="44" cy="44" r="30" fill="none" stroke="var(--fog)" stroke-width="14"/>
              <!-- palm segment -->
              <circle
                cx="44" cy="44" r="30"
                fill="none"
                stroke="var(--leaf)"
                stroke-width="14"
                :stroke-dasharray="`${palmArc} ${CIRC}`"
                transform="rotate(-90 44 44)"
              />
              <!-- maize segment -->
              <circle
                cx="44" cy="44" r="30"
                fill="none"
                stroke="var(--harvest)"
                stroke-width="14"
                :stroke-dasharray="`${maizeArc} ${CIRC}`"
                :stroke-dashoffset="-palmArc"
                transform="rotate(-90 44 44)"
              />
            </svg>
          </div>
          <div class="pnl-legend">
            <template v-if="store.dashboard?.cropPnl?.length">
              <div v-for="cp in store.dashboard.cropPnl" :key="cp.cropType" class="pnl-item">
                <div class="pnl-dot" :style="{ background: cropColor(cp.cropType) }" />
                <div>
                  <div class="pnl-crop">{{ cp.cropType }}</div>
                  <div class="pnl-cost">₦{{ (cp.cost / 1000).toFixed(0) }}K</div>
                  <div class="pnl-profit" :class="cp.netProfit >= 0 ? 'profit-up' : 'profit-dn'">
                    {{ cp.netProfit >= 0 ? '+' : '' }}₦{{ (cp.netProfit / 1000).toFixed(0) }}K net
                  </div>
                </div>
              </div>
            </template>
            <template v-else>
              <div class="pnl-item">
                <div class="pnl-dot" style="background:var(--leaf)" />
                <div>
                  <div class="pnl-crop">Oil Palm</div>
                  <div class="pnl-cost">—</div>
                </div>
              </div>
              <div class="pnl-item">
                <div class="pnl-dot" style="background:var(--harvest)" />
                <div>
                  <div class="pnl-crop">Maize</div>
                  <div class="pnl-cost">—</div>
                </div>
              </div>
            </template>
          </div>
        </div>
      </div>
    </div>

    <!-- ── 7-day forecast ── -->
    <div v-if="store.weather?.forecast?.length" class="card forecast-card">
      <div class="card-header">
        <div class="card-title">7-Day Forecast</div>
        <div class="card-label">{{ store.weather.location || 'YOUR FARM' }} · 💧 = GOOD SPRAY DAY</div>
      </div>
      <div class="forecast-row">
        <div v-for="(d, i) in store.weather.forecast" :key="d.date" class="fc-day" :class="{ today: i === 0 }">
          <div class="fc-name">{{ i === 0 ? 'Today' : dayName(d.date) }}</div>
          <div class="fc-date">{{ shortDate(d.date) }}</div>
          <div class="fc-icon" :title="weatherText(d.weatherCode)">{{ weatherIcon(d.weatherCode) }}</div>
          <div class="fc-temp">{{ Math.round(d.tempMaxC) }}° <span>{{ Math.round(d.tempMinC) }}°</span></div>
          <div class="fc-rain">
            {{ d.precipitationMm.toFixed(1) }} mm<template v-if="d.rainChance"> · {{ d.rainChance }}%</template>
          </div>
          <div class="fc-wind">{{ Math.round(d.windKmh) }} km/h</div>
          <div class="fc-spray" :class="{ ok: d.goodSprayDay }">{{ d.goodSprayDay ? '💧 Spray' : '—' }}</div>
        </div>
      </div>
    </div>

    <!-- ── Recent Activities + Transactions ── -->
    <div class="grid-2">
      <div class="card">
        <div class="card-header">
          <div class="card-title">Recent Activities</div>
          <button class="btn btn-ghost btn-sm" @click="router.push('/activities')">View all</button>
        </div>
        <div v-if="!store.activities.length" class="empty-sm">No activities yet</div>
        <ActivityRow v-for="(a, i) in store.activities.slice(0, 4)" :key="a.id" :activity="a" :index="i" />
      </div>
      <div class="card">
        <div class="card-header">
          <div class="card-title">Recent Transactions</div>
          <button class="btn btn-ghost btn-sm" @click="router.push('/finance')">View all</button>
        </div>
        <div v-if="!store.transactions.length" class="empty-sm">No transactions yet</div>
        <TransactionRow v-for="(t, i) in store.transactions.slice(0, 4)" :key="t.id" :txn="t" :index="i" />
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useAgroStore } from '@/stores/agro'
import { activityTypeLabel } from '@/utils/enums'
import AlertBanner from '@/components/ui/AlertBanner.vue'
import ActivityRow from '@/components/ui/ActivityRow.vue'
import TransactionRow from '@/components/ui/TransactionRow.vue'

const router = useRouter()
const store = useAgroStore()

const todayLabel = new Date().toLocaleDateString('en-NG', {
  weekday: 'long', day: 'numeric', month: 'long', year: 'numeric'
})

// ── Spend breakdown ───────────────────────────────────────────────────────
const maintenanceCostK = computed(() => {
  if (!store.dashboard) return '—'
  return Math.round(store.dashboard.maintenanceCostMtd / 1000).toLocaleString()
})

// ── KPI sparkline (7-point simulated trend, normalized to 0–26 px height) ─
const SPARK_TRAIL = [0.45, 0.54, 0.61, 0.70, 0.78, 0.88, 1.0]
const sparkPoints = computed(() =>
  SPARK_TRAIL.map((v, i) => `${(i / 6) * 76},${26 - v * 20}`).join(' ')
)
const sparkEnd = computed(() => ({
  x: 76,
  y: 26 - 1.0 * 20  // last trail value
}))

// ── 6-month bar chart ─────────────────────────────────────────────────────
const MONTHS = ['Jan','Feb','Mar','Apr','May','Jun','Jul','Aug','Sep','Oct','Nov','Dec']
const BAR_MAX_PX = 62

// Real monthly spend from the API (bank spend + logged activity costs); hover a bar for the amount.
const spendBars = computed(() => {
  const months = store.dashboard?.spendTrend ?? []
  const max = Math.max(...months.map(m => m.amount), 0)
  return months.map((m, i) => ({
    label: MONTHS[m.month - 1],
    full: Math.round(m.amount).toLocaleString('en-NG'),
    valLabel: m.amount > 0 ? `${Math.round(m.amount / 1000)}K` : '—',
    h: m.amount > 0 && max > 0 ? Math.max(4, Math.round((m.amount / max) * BAR_MAX_PX)) : 2,
    current: i === months.length - 1
  }))
})

// ── Forecast helpers ──────────────────────────────────────────────────────
function dayName(iso: string) {
  return new Date(iso).toLocaleDateString('en-NG', { weekday: 'short' })
}
function shortDate(iso: string) {
  return new Date(iso).toLocaleDateString('en-NG', { day: 'numeric', month: 'short' })
}
// WMO weather codes as returned by Open-Meteo.
function weatherIcon(code: number) {
  if (code === 0) return '☀️'
  if (code <= 2) return '🌤️'
  if (code === 3) return '☁️'
  if (code === 45 || code === 48) return '🌫️'
  if (code >= 51 && code <= 57) return '🌦️'
  if (code >= 61 && code <= 67) return '🌧️'
  if (code >= 80 && code <= 82) return '🌦️'
  if (code >= 95) return '⛈️'
  return '🌥️'
}
function weatherText(code: number) {
  if (code === 0) return 'Clear'
  if (code <= 2) return 'Partly cloudy'
  if (code === 3) return 'Overcast'
  if (code === 45 || code === 48) return 'Fog'
  if (code >= 51 && code <= 57) return 'Drizzle'
  if (code >= 61 && code <= 67) return 'Rain'
  if (code >= 80 && code <= 82) return 'Showers'
  if (code >= 95) return 'Thunderstorm'
  return 'Cloudy'
}

// ── Block cost rows ─────────────────────────────────────────────────────
const blockRows = computed(() => {
  if (store.dashboard?.blocksMtd?.length) {
    const max = Math.max(...store.dashboard.blocksMtd.map(s => s.cost), 1)
    return store.dashboard.blocksMtd.map(s => ({
      id: s.blockId,
      name: s.blockName,
      amount: s.cost.toLocaleString('en-NG'),
      pct: Math.round((s.cost / max) * 100)
    }))
  }
  // Dashboard not loaded yet: list the blocks with no cost rather than showing made-up figures.
  return store.blocks.map(s => ({ id: s.id, name: s.name, amount: '0', pct: 0 }))
})

// ── Crop donut ────────────────────────────────────────────────────────────
const CIRC = 2 * Math.PI * 30   // r=30 → ≈188.5

const palmArc = computed(() => {
  const pnl = store.dashboard?.cropPnl
  if (!pnl?.length) return 0
  const total = pnl.reduce((s, c) => s + c.cost, 0)
  const palm = pnl.find(c => /palm/i.test(c.cropType))
  return total > 0 && palm ? (palm.cost / total) * CIRC : 0
})

const maizeArc = computed(() => {
  const pnl = store.dashboard?.cropPnl
  if (!pnl?.length) return 0
  const total = pnl.reduce((s, c) => s + c.cost, 0)
  const maize = pnl.find(c => /maize|corn/i.test(c.cropType))
  return total > 0 && maize ? (maize.cost / total) * CIRC : 0
})

const CROP_PALETTE: Record<string, string> = {
  'Oil Palm': 'var(--leaf)', 'Palm': 'var(--leaf)',
  'Maize': 'var(--harvest)',  'Corn': 'var(--harvest)'
}
function cropColor(type: string) {
  return CROP_PALETTE[type] ?? 'var(--data)'
}

// ── Utilities ─────────────────────────────────────────────────────────────
function formatDate(iso: string) {
  return new Date(iso).toLocaleDateString('en-NG', { day: 'numeric', month: 'short' })
}

function urgencyClass(days: number) {
  if (days < 0) return 'overdue'
  if (days <= 7) return 'soon'
  return 'ok'
}

onMounted(async () => {
  await Promise.all([
    store.initDashboard(),
    store.fetchBlocks(),
    store.fetchActivities(),
    store.fetchTransactions()
  ])
})
</script>

<style scoped>
.kpi-link { cursor: pointer; }
.kpi-link:hover { box-shadow: var(--shadow-sm, 0 2px 8px rgba(0,0,0,0.08)); }
/* ── Hero ─────────────────────────────────────────────────────────────── */
.hero {
  display: flex;
  justify-content: space-between;
  align-items: center;
  background: var(--forest);
  border-radius: 14px;
  padding: 20px 24px;
  margin-bottom: 16px;
}
.hero-name {
  font-family: 'Syne', sans-serif;
  font-weight: 700;
  font-size: 18px;
  color: #fff;
  letter-spacing: -0.02em;
}
.hero-meta {
  display: flex;
  align-items: center;
  gap: 6px;
  flex-wrap: wrap;
  font-family: 'JetBrains Mono', monospace;
  font-size: 10px;
  color: rgba(255,255,255,0.45);
  margin-top: 5px;
}
.hdot { opacity: 0.35; }
.hero-sync { display: inline-flex; align-items: center; gap: 5px; }
.live-dot {
  width: 6px; height: 6px; border-radius: 50%;
  background: var(--palm); flex-shrink: 0;
  animation: pulse-dot 2s infinite;
}
@keyframes pulse-dot { 0%,100% { opacity:1; } 50% { opacity:0.35; } }
.hero-wx { text-align: right; }
.hero-temp {
  font-family: 'Syne', sans-serif;
  font-weight: 700; font-size: 34px;
  color: #fff; line-height: 1;
}
.hero-deg { font-size: 18px; font-weight: 400; }
.hero-season {
  font-family: 'JetBrains Mono', monospace;
  font-size: 9px; text-transform: uppercase;
  letter-spacing: 0.1em; color: rgba(255,255,255,0.45);
  margin-top: 5px;
}

/* ── KPI row ──────────────────────────────────────────────────────────── */
.kpi-row {
  display: grid;
  grid-template-columns: 2fr 1fr 1fr 1fr;
  gap: 12px;
  margin-bottom: 16px;
}

/* Primary dark spend card */
.kpi-primary {
  background: var(--canopy);
  border-radius: 12px;
  padding: 18px 20px;
}
.kpi-top {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: 8px;
}
.kpi-lbl-inv {
  font-family: 'JetBrains Mono', monospace;
  font-size: 9px; text-transform: uppercase;
  letter-spacing: 0.12em; color: rgba(255,255,255,0.4);
}
.kpi-spark { width: 80px; height: 28px; flex-shrink: 0; }
.kpi-val-inv {
  font-family: 'Syne', sans-serif;
  font-weight: 700; font-size: 30px;
  color: #fff; letter-spacing: -0.03em;
  line-height: 1; margin-bottom: 6px;
}
.kpi-sub-inv {
  font-family: 'JetBrains Mono', monospace;
  font-size: 10px; color: rgba(255,255,255,0.4);
}

/* Small KPI cards */
.kpi-card-sm {
  background: white;
  border: 1px solid var(--fog);
  border-radius: 12px;
  padding: 16px;
}
.kpi-card-sm.kpi-warn { border-color: rgba(232,168,56,0.35); background: #FFFEF5; }
.kpi-lbl {
  font-family: 'JetBrains Mono', monospace;
  font-size: 9px; text-transform: uppercase;
  letter-spacing: 0.12em; color: var(--stone);
  margin-bottom: 8px;
}
.kpi-val {
  font-family: 'Syne', sans-serif;
  font-weight: 700; font-size: 26px;
  color: var(--forest); letter-spacing: -0.02em;
  line-height: 1; margin-bottom: 5px;
}
.kpi-sub-up   { display: flex; align-items: center; gap: 3px; font-size: 11px; color: var(--palm); }
.kpi-sub-mute { font-size: 11px; color: var(--stone); }
.kpi-sub-warn { font-size: 11px; color: #B8780A; }

/* ── Forecast ─────────────────────────────────────────────────────────── */
.forecast-card { margin-bottom: 16px; }
.forecast-row { display: grid; grid-template-columns: repeat(7, 1fr); gap: 8px; padding: 12px 16px 16px; }
.fc-day { text-align: center; padding: 10px 4px; border-radius: 10px; border: 1px solid var(--fog); }
.fc-day.today { border-color: var(--palm); background: rgba(45,122,79,0.06); }
.fc-name { font-family: 'Syne', sans-serif; font-weight: 700; font-size: 12px; color: var(--forest); }
.fc-date { font-family: 'JetBrains Mono', monospace; font-size: 9px; color: var(--stone); }
.fc-icon { font-size: 26px; margin: 6px 0 2px; }
.fc-temp { font-weight: 600; font-size: 13px; color: var(--forest); }
.fc-temp span { color: var(--stone); font-weight: 400; }
.fc-rain, .fc-wind { font-family: 'JetBrains Mono', monospace; font-size: 9px; color: var(--stone); margin-top: 2px; }
.fc-spray { margin-top: 6px; font-size: 10px; color: var(--fog); }
.fc-spray.ok { color: var(--leaf); font-weight: 600; }
@media (max-width: 900px) { .forecast-row { grid-template-columns: repeat(4, 1fr); } }

/* ── Spend Trend + Tasks ──────────────────────────────────────────────── */
.grid-trend {
  display: grid;
  grid-template-columns: 1fr 360px;
  gap: 16px;
  margin-bottom: 16px;
  align-items: start;
}

.chart-area { padding: 8px 16px 14px; }
.bars { display: flex; align-items: flex-end; justify-content: space-around; height: 96px; padding: 0 4px; }
.bar-col { position: relative; flex: 1; display: flex; flex-direction: column; align-items: center; justify-content: flex-end; height: 100%; }
.bar-fill { width: 70%; border-radius: 5px; background: var(--fog); transition: filter .15s; }
.bar-val { font-family: 'JetBrains Mono', monospace; font-size: 9px; color: var(--stone); margin-bottom: 4px; }
.bar-val.cur { color: var(--leaf); }
.bar-fill.cur { background: var(--leaf); }
.bar-col:hover .bar-fill { filter: brightness(0.9); }
.bar-tip {
  position: absolute; bottom: calc(100% + 2px); left: 50%; transform: translateX(-50%);
  background: var(--forest, #1a2e1a); color: #fff; padding: 3px 8px; border-radius: 6px;
  font-family: 'JetBrains Mono', monospace; font-size: 10px; white-space: nowrap;
  opacity: 0; pointer-events: none; transition: opacity .12s; z-index: 5;
}
.bar-col:hover .bar-tip { opacity: 1; }
.bar-months {
  display: flex;
  justify-content: space-around;
  font-family: 'JetBrains Mono', monospace;
  font-size: 9px; color: var(--stone);
  margin-top: 6px; padding: 0 4px;
}
.bar-months span { flex: 1; text-align: center; }
.bar-month-cur { color: var(--leaf); font-weight: 600; }

/* Tasks */
.tasks-empty {
  display: flex; flex-direction: column;
  align-items: center; gap: 8px;
  padding: 28px 16px; color: var(--stone);
}
.tasks-empty p {
  font-size: 11px; text-align: center;
  max-width: 200px; line-height: 1.55; margin: 0;
}
.task-row {
  display: flex; gap: 10px;
  padding: 10px 16px;
  border-bottom: 1px solid var(--fog);
}
.task-row:last-child { border-bottom: none; }
.task-bar { width: 3px; border-radius: 2px; flex-shrink: 0; min-height: 38px; }
.task-bar.ok      { background: var(--palm); }
.task-bar.soon    { background: var(--harvest); }
.task-bar.overdue { background: #E53935; }
.task-body { flex: 1; min-width: 0; }
.task-head { display: flex; align-items: center; gap: 5px; flex-wrap: wrap; }
.task-sec  { font-size: 12px; font-weight: 600; color: var(--forest); }
.task-arr  { font-size: 10px; color: var(--stone); }
.task-type { font-size: 12px; color: var(--leaf); font-weight: 500; }
.task-badge {
  font-family: 'JetBrains Mono', monospace;
  font-size: 9px; letter-spacing: 0.05em;
  padding: 2px 6px; border-radius: 4px;
}
.task-badge.ok      { background: rgba(61,170,106,0.1);  color: var(--leaf); }
.task-badge.soon    { background: #FFF0C0; color: #8A6000; }
.task-badge.overdue { background: #FEECEC; color: #C62828; }
.task-reason { font-size: 10px; color: var(--stone); font-style: italic; margin-top: 3px; }
.task-chem   { font-size: 10px; color: var(--stone); margin-top: 3px; }
.task-stock  { font-family: 'JetBrains Mono', monospace; font-size: 9px; }
.no-wx       { font-size: 10px; cursor: help; opacity: 0.55; }

/* ── 3-col: Weather + Blocks + Crop P&L ────────────────────────────── */
.grid-3 {
  display: grid;
  grid-template-columns: 270px minmax(0, 1fr) 260px;
  gap: 16px;
  margin-bottom: 16px;
  align-items: start;
}

/* Weather */
.wx-card { min-width: 0; overflow: hidden; }
.wx-card :deep(.card-label) { white-space: normal; overflow-wrap: anywhere; line-height: 1.4; }
.wx-head {
  display: flex; justify-content: space-between; align-items: flex-start; gap: 12px;
  padding: 18px 18px 16px; border-bottom: 1px solid var(--fog);
}
.wx-head > div:first-child { min-width: 0; flex: 1; }
.wx-temp {
  font-family: 'Syne', sans-serif;
  font-weight: 700; font-size: 36px;
  color: var(--forest); line-height: 1; flex-shrink: 0;
}
.wx-deg { font-size: 16px; font-weight: 500; }
.season-chip {
  display: inline-block;
  font-family: 'JetBrains Mono', monospace;
  font-size: 8px; text-transform: uppercase;
  letter-spacing: 0.1em; padding: 3px 8px;
  background: rgba(45,122,79,0.07);
  border: 1px solid rgba(45,122,79,0.18);
  border-radius: 20px; color: var(--leaf);
  margin-top: 5px;
}
.wx-stats { display: flex; flex-wrap: wrap; gap: 12px 20px; padding: 14px 18px; border-bottom: 1px solid var(--fog); }
.wx-stat-val {
  font-family: 'JetBrains Mono', monospace;
  font-size: 15px; font-weight: 600; color: var(--forest);
  margin-top: 2px;
}
.wx-spray {
  display: flex; align-items: center; gap: 8px;
  margin: 14px 18px 18px;
  background: rgba(45,122,79,0.05);
  border: 1px solid rgba(45,122,79,0.13);
  border-radius: 8px; padding: 10px 12px;
}
.wx-spray-lbl { font-family: 'JetBrains Mono', monospace; font-size: 8px; color: var(--stone); }
.wx-spray-dates { overflow-wrap: anywhere; font-size: 11px; font-weight: 600; color: var(--forest); margin-top: 1px; }
.wx-empty {
  display: flex; flex-direction: column;
  align-items: center; justify-content: center;
  gap: 8px; min-height: 150px; padding: 16px;
  color: var(--stone); font-size: 11px;
}
.wx-empty p { margin: 0; }

/* Block cost rows */
.sec-row {
  display: flex; align-items: center; gap: 10px;
  padding: 9px 16px; border-bottom: 1px solid var(--fog);
  cursor: pointer; transition: background 0.1s;
}
.sec-row:last-child { border-bottom: none; }
.sec-row:hover { background: var(--mist); }
.sec-name {
  font-size: 11px; font-weight: 600; color: var(--forest);
  width: 82px; flex-shrink: 0;
  white-space: nowrap; overflow: hidden; text-overflow: ellipsis;
}
.sec-track {
  flex: 1; height: 5px; background: var(--fog);
  border-radius: 3px; overflow: hidden;
}
.sec-fill {
  height: 100%; background: var(--leaf); border-radius: 3px;
  transition: width 0.5s ease;
}
.sec-amt {
  font-family: 'JetBrains Mono', monospace;
  font-size: 10px; font-weight: 600; color: var(--forest);
  flex-shrink: 0; min-width: 58px; text-align: right;
}

/* Crop P&L donut */
.pnl-body { display: flex; align-items: center; gap: 16px; padding: 16px; }
.pnl-donut-wrap { flex-shrink: 0; }
.pnl-donut { width: 88px; height: 88px; display: block; }
.pnl-legend { display: flex; flex-direction: column; gap: 12px; }
.pnl-item { display: flex; align-items: flex-start; gap: 8px; }
.pnl-dot { width: 10px; height: 10px; border-radius: 50%; flex-shrink: 0; margin-top: 3px; }
.pnl-crop {
  font-family: 'JetBrains Mono', monospace;
  font-size: 9px; text-transform: uppercase;
  letter-spacing: 0.1em; color: var(--stone);
}
.pnl-cost {
  font-family: 'Syne', sans-serif;
  font-weight: 700; font-size: 16px; color: var(--forest);
  margin-top: 1px;
}
.pnl-profit { font-size: 10px; margin-top: 1px; }
.profit-up { color: var(--palm); }
.profit-dn { color: var(--alert); }

/* ── Recent rows ──────────────────────────────────────────────────────── */
.grid-2 { display: grid; grid-template-columns: 1fr 1fr; gap: 16px; }

/* Card label (mono secondary label used in card headers) */
.card-label {
  font-family: 'JetBrains Mono', monospace;
  font-size: 9px; text-transform: uppercase;
  letter-spacing: 0.1em; color: var(--stone);
}

/* Empty state */
.empty-sm { padding: 20px; text-align: center; color: var(--stone); font-size: 12px; }
</style>
