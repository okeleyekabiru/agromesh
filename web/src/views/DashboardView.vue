<template>
  <div>
    <AlertBanner v-if="store.pendingReviews > 0" action="Review now" @action="router.push('/finance')">
      <strong>{{ store.pendingReviews }} transactions</strong> need categorization review before tonight's P&L update.
    </AlertBanner>

    <!-- KPIs -->
    <div class="kpi-grid">
      <KpiCard label="Total Spend · MTD" :value="`₦${(store.totalMTDSpend/1000).toFixed(0)}K`" delta="↑ 8% vs May" deltaType="down" :dark="true" />
      <KpiCard :label="`Active Sections Today`" :value="`${store.activeSectionsToday} / ${store.sections.length}`" delta="⚠ 2 idle" deltaType="warn" />
      <KpiCard label="Workers Logged Today" value="23" delta="↑ 3 vs yesterday" deltaType="up" />
      <KpiCard label="Transactions Synced" :value="`${store.transactions.length}`" :delta="`${store.pendingReviews} need review`" deltaType="warn" />
    </div>

    <div class="grid-3-1">
      <!-- Section costs -->
      <div class="card">
        <div class="card-header">
          <div class="card-title">Section Costs — June 2025</div>
          <div class="card-label">MTD per section</div>
        </div>
        <SectionRow
          v-for="(s, i) in store.sections"
          :key="s.id"
          :section="s"
          :amount="sectionAmounts[i]"
          :pct="sectionPcts[i]"
          @click="router.push('/sections')"
        />
      </div>

      <!-- Crop P&L -->
      <div class="card">
        <div class="card-header">
          <div class="card-title">Crop P&L</div>
          <div class="card-label">Auto-categorized</div>
        </div>
        <div class="crop-split">
          <div class="crop-card crop-card-palm" style="border-radius:10px;padding:14px;">
            <div style="font-family:'JetBrains Mono',monospace;font-size:9px;letter-spacing:0.12em;text-transform:uppercase;color:var(--leaf);margin-bottom:8px;">Oil Palm</div>
            <div style="font-family:'Syne',sans-serif;font-weight:700;font-size:22px;color:var(--forest);letter-spacing:-0.02em;">₦1.12M</div>
            <div style="font-size:11px;color:var(--stone);margin-top:4px;">73% of spend</div>
            <div style="height:4px;background:var(--fog);border-radius:2px;margin-top:10px;overflow:hidden;">
              <div style="width:73%;height:100%;background:var(--leaf);border-radius:2px;" />
            </div>
          </div>
          <div class="crop-card crop-card-maize" style="border-radius:10px;padding:14px;">
            <div style="font-family:'JetBrains Mono',monospace;font-size:9px;letter-spacing:0.12em;text-transform:uppercase;color:#8A6000;margin-bottom:8px;">Maize</div>
            <div style="font-family:'Syne',sans-serif;font-weight:700;font-size:22px;color:var(--forest);letter-spacing:-0.02em;">₦420K</div>
            <div style="font-size:11px;color:var(--stone);margin-top:4px;">27% of spend</div>
            <div style="height:4px;background:var(--fog);border-radius:2px;margin-top:10px;overflow:hidden;">
              <div style="width:27%;height:100%;background:var(--harvest);border-radius:2px;" />
            </div>
          </div>
        </div>
        <!-- Weekly bar chart -->
        <div style="padding:0 16px 16px;">
          <div style="height:1px;background:var(--fog);margin-bottom:12px;"></div>
          <div class="card-label" style="margin-bottom:10px;">Weekly Spend</div>
          <div style="display:flex;align-items:flex-end;gap:6px;height:52px;">
            <div v-for="b in weekBars" :key="b.label" style="flex:1;display:flex;flex-direction:column;align-items:center;gap:4px;justify-content:flex-end;">
              <div :style="{ width:'100%', height:b.h+'px', borderRadius:'3px 3px 0 0', background: b.dim ? 'var(--fog)' : 'var(--palm)', minHeight:'4px' }" />
              <span style="font-family:'JetBrains Mono',monospace;font-size:8px;color:var(--stone);">{{ b.label }}</span>
            </div>
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
        <ActivityRow v-for="a in store.activities.slice(0,4)" :key="a.id" :activity="a" />
      </div>
      <div class="card">
        <div class="card-header">
          <div class="card-title">Recent Transactions</div>
          <button class="btn btn-ghost btn-sm" @click="router.push('/finance')">View all</button>
        </div>
        <TransactionRow v-for="t in store.transactions.slice(0,4)" :key="t.id" :txn="t" />
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { useRouter } from 'vue-router'
import { useAgroStore } from '@/stores/agro'
import KpiCard from '@/components/ui/KpiCard.vue'
import AlertBanner from '@/components/ui/AlertBanner.vue'
import SectionRow from '@/components/ui/SectionRow.vue'
import ActivityRow from '@/components/ui/ActivityRow.vue'
import TransactionRow from '@/components/ui/TransactionRow.vue'

const router = useRouter()
const store = useAgroStore()

const sectionAmounts = ['612,400', '284,500', '340,000', '98,200', '168,000', '62,400']
const sectionPcts    = [100, 46, 56, 16, 28, 10]

const weekBars = [
  { label:'Mon', h:32, dim:false }, { label:'Tue', h:44, dim:false },
  { label:'Wed', h:28, dim:false }, { label:'Thu', h:52, dim:false },
  { label:'Fri', h:36, dim:false }, { label:'Sat', h:20, dim:true },
  { label:'Sun', h:8, dim:true },
]
</script>

<style scoped>
.kpi-grid { display: grid; grid-template-columns: repeat(4,1fr); gap: 12px; margin-bottom: 20px; }
.grid-3-1 { display: grid; grid-template-columns: 1fr 360px; gap: 16px; margin-bottom: 16px; }
.grid-2   { display: grid; grid-template-columns: 1fr 1fr; gap: 16px; }
.crop-split { display: grid; grid-template-columns: 1fr 1fr; gap: 10px; padding: 16px; }
</style>
