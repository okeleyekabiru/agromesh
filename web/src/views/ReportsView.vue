<template>
  <div>
    <div class="grid-2">
      <div class="card">
        <div class="card-header">
          <div class="card-title">Monthly Summary</div>
          <div class="card-label">June 2025</div>
        </div>
        <div v-for="r in summary" :key="r.label" class="report-stat">
          <div class="report-label">{{ r.label }}</div>
          <div class="report-value">{{ r.value }}</div>
        </div>
      </div>

      <div class="card">
        <div class="card-header">
          <div class="card-title">Cost per Hectare</div>
          <div class="card-label">By section</div>
        </div>
        <div v-for="(s, i) in store.sections" :key="s.id" class="report-stat">
          <div>
            <div class="report-label">{{ s.name }} — {{ s.crops?.[0]?.cropType ?? '' }}</div>
            <div style="font-family:'JetBrains Mono',monospace;font-size:9px;color:var(--stone);">{{ s.area?.displayValue }} {{ s.area?.displayUnit }}</div>
          </div>
          <div>
            <span class="report-value">₦{{ cph[i] }}</span>
            <span style="font-size:10px;color:var(--stone);font-family:'Inter',sans-serif;">/ha</span>
          </div>
        </div>
      </div>
    </div>

    <!-- 6-month bar chart -->
    <div class="card" style="margin-top:16px;">
      <div class="card-header">
        <div class="card-title">6-Month Spend Trend</div>
        <div class="card-label">₦ thousands</div>
      </div>
      <div style="padding:20px 24px 16px;">
        <div style="display:flex;align-items:flex-end;gap:8px;height:120px;">
          <div v-for="m in monthBars" :key="m.label" style="flex:1;height:100%;display:flex;flex-direction:column;align-items:center;justify-content:flex-end;gap:4px;">
            <div style="font-family:'JetBrains Mono',monospace;font-size:8px;color:var(--stone);">{{ m.val }}</div>
            <div :style="{ width:'100%', height:m.h+'px', borderRadius:'3px 3px 0 0', background: m.current ? 'var(--palm)' : 'var(--fog)', minHeight:'4px' }" />
            <div style="font-family:'JetBrains Mono',monospace;font-size:8px;color:var(--stone);">{{ m.label }}</div>
          </div>
        </div>
      </div>
    </div>

    <div class="grid-2" style="margin-top:16px;">
      <div class="card">
        <div class="card-header">
          <div class="card-title">Cost by Category</div>
          <div class="card-label">June 2025</div>
        </div>
        <div v-for="c in categories" :key="c.label" class="report-stat">
          <div style="display:flex;align-items:center;gap:8px;">
            <span style="font-size:14px;">{{ c.icon }}</span>
            <div class="report-label">{{ c.label }}</div>
          </div>
          <div style="display:flex;align-items:center;gap:10px;">
            <StatBadge variant="stone">{{ c.pct }}%</StatBadge>
            <div class="report-value" style="font-size:14px;">{{ c.value }}</div>
          </div>
        </div>
      </div>

      <div class="card">
        <div class="card-header">
          <div class="card-title">Next Sync</div>
          <div class="card-label">Mono → Claude → Dashboard</div>
        </div>
        <div style="padding:16px;">
          <div style="background:rgba(13,43,31,0.04);border:1px solid rgba(45,122,79,0.15);border-radius:8px;padding:16px;">
            <div style="font-family:'JetBrains Mono',monospace;font-size:9px;letter-spacing:0.1em;color:var(--stone);text-transform:uppercase;margin-bottom:8px;">Scheduled</div>
            <div style="font-family:'Syne',sans-serif;font-size:20px;font-weight:700;color:var(--forest);">Tonight 02:00 AM</div>
            <div style="font-family:'JetBrains Mono',monospace;font-size:10px;color:var(--stone);margin-top:6px;line-height:1.6;">
              Pull Mono transactions<br>
              Run keyword dictionary<br>
              Claude API for ambiguous<br>
              Refresh P&L dashboard
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { onMounted } from 'vue'
import { useAgroStore } from '@/stores/agro'
import StatBadge from '@/components/ui/StatBadge.vue'

const store = useAgroStore()

onMounted(() => {
  if (!store.sections.length) store.fetchSections()
})

const cph = ['34,022', '17,781', '24,286', '4,910', '10,500', '3,900']

const summary = [
  { label: 'Total Expenditure',              value: '₦1,540,000' },
  { label: 'Labour (44%)',                   value: '₦677,600'   },
  { label: 'Fertilizer & Chemicals (28%)',   value: '₦431,200'   },
  { label: 'Fuel & Transport (18%)',         value: '₦277,200'   },
  { label: 'Equipment & Maintenance (10%)',  value: '₦154,000'   },
]

const categories = [
  { label: 'Labour',                 icon: '👷', pct: 44, value: '₦677,600' },
  { label: 'Fertilizer & Chemicals', icon: '🌱', pct: 28, value: '₦431,200' },
  { label: 'Fuel & Transport',       icon: '⛽', pct: 18, value: '₦277,200' },
  { label: 'Equipment & Maintenance',icon: '🔧', pct: 10, value: '₦154,000' },
]

const monthBars = [
  { label: 'Jan', h: 55, val: '980K',  current: false },
  { label: 'Feb', h: 44, val: '780K',  current: false },
  { label: 'Mar', h: 66, val: '1.2M',  current: false },
  { label: 'Apr', h: 77, val: '1.38M', current: false },
  { label: 'May', h: 83, val: '1.43M', current: false },
  { label: 'Jun', h: 90, val: '1.54M', current: true  },
]
</script>

<style scoped>
.grid-2 { display: grid; grid-template-columns: 1fr 1fr; gap: 16px; }
.report-stat { padding: 14px 16px; border-bottom: 1px solid var(--fog); display: flex; align-items: center; justify-content: space-between; }
.report-stat:last-child { border-bottom: none; }
.report-label { font-size: 12px; color: var(--ink); }
.report-value { font-family: 'Syne', sans-serif; font-weight: 700; font-size: 16px; color: var(--forest); letter-spacing: -0.01em; }
</style>
