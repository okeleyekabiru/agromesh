<template>
  <div class="grid-3">
    <div v-for="s in store.sections" :key="s.id" class="section-card">
      <div class="section-card-header">
        <div style="display:flex;align-items:center;gap:8px;">
          <div style="width:10px;height:10px;border-radius:50%;" :style="{ background: s.color }" />
          <div class="section-card-name">{{ s.name }}</div>
        </div>
        <StatBadge :variant="s.status === 'Active' ? 'green' : s.status === 'Harvest Soon' ? 'harvest' : 'stone'">
          {{ s.status }}
        </StatBadge>
      </div>
      <div style="padding:14px 16px;">
        <div class="stat-row"><span class="stat-key">Crop</span><span class="stat-val">{{ s.crop }}</span></div>
        <div class="stat-row"><span class="stat-key">Hectares</span><span class="stat-val">{{ s.hectares }} ha</span></div>
        <div class="stat-row"><span class="stat-key">Blocks</span><span class="stat-val">{{ s.blockCount }}</span></div>
        <div class="stat-row"><span class="stat-key">Last Activity</span><span class="stat-val">{{ s.lastActivityDate }}</span></div>
        <div class="stat-row"><span class="stat-key">MTD Spend</span><span class="stat-val">₦{{ amounts[s.id] }}</span></div>
        <div class="stat-row"><span class="stat-key">Cost / ha</span><span class="stat-val">₦{{ cph[s.id] }}</span></div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { useAgroStore } from '@/stores/agro'
import StatBadge from '@/components/ui/StatBadge.vue'

const store = useAgroStore()

const amounts: Record<string, string> = { s1:'612,400', s2:'284,500', s3:'340,000', s4:'98,200', s5:'168,000', s6:'62,400' }
const cph: Record<string, string>    = { s1:'34,022', s2:'17,781', s3:'24,286', s4:'4,910', s5:'10,500', s6:'3,900' }
</script>

<style scoped>
.grid-3 { display: grid; grid-template-columns: repeat(3,1fr); gap: 16px; }
.section-card { background: white; border: 1px solid var(--fog); border-radius: 12px; overflow: hidden; cursor: pointer; transition: border-color 0.15s; }
.section-card:hover { border-color: var(--palm); }
.section-card-header { padding: 14px 16px; border-bottom: 1px solid var(--fog); display: flex; align-items: center; justify-content: space-between; }
.section-card-name { font-family: 'Syne', sans-serif; font-weight: 700; font-size: 14px; color: var(--forest); }
.stat-row { display: flex; justify-content: space-between; padding: 5px 0; border-bottom: 1px solid var(--fog); }
.stat-row:last-child { border-bottom: none; }
.stat-key { font-family: 'JetBrains Mono', monospace; font-size: 9px; letter-spacing: 0.08em; color: var(--stone); text-transform: uppercase; }
.stat-val { font-family: 'JetBrains Mono', monospace; font-size: 10px; font-weight: 500; color: var(--forest); }
</style>
