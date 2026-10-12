<template>
  <div>
    <!-- Work part-done right now: click one to see exactly which areas were marked -->
    <div v-if="inProgress.length" class="progress-strip">
      <div class="progress-title">Blocks in progress <span>{{ inProgress.length }}</span></div>
      <div class="progress-list">
        <button v-for="p in inProgress" :key="p.blockId + p.activityType" class="progress-card" @click="openProgress(p)">
          <div class="pc-top"><b>{{ p.blockName }}</b><span class="pc-type">{{ typeLabel(p.activityType) }}</span></div>
          <div class="pc-bar"><i :style="{ width: Math.min(100, p.percent) + '%' }" /></div>
          <div class="pc-meta">{{ p.percent }}% · {{ p.coveredHectares.toFixed(2) }} of {{ p.blockHectares.toFixed(2) }} ha · {{ p.activityCount }} {{ p.activityCount === 1 ? 'area' : 'areas' }}</div>
        </button>
      </div>
    </div>

    <!-- Crop condition from satellite -->
    <div v-if="store.satellite" class="sat-bar">
      <template v-if="store.satellite.configured">
        <span>🛰️ Satellite crop monitoring{{ store.satellite.lastSyncAt ? ` · updated ${formatDate(store.satellite.lastSyncAt)}` : ' · no data fetched yet' }}</span>
        <button class="btn btn-ghost btn-sm" :disabled="satBusy" @click="refreshSat">{{ satBusy ? 'Fetching…' : 'Refresh' }}</button>
      </template>
      <span v-else>🛰️ Satellite crop monitoring is not connected yet. Add the Copernicus client id and secret to the API settings.</span>
      <span v-if="satError" class="sat-err">{{ satError }}</span>
    </div>

    <div v-if="store.loading" style="padding:40px;text-align:center;color:var(--stone);font-size:12px;">Loading blocks…</div>
    <div v-else-if="!store.blocks.length" style="padding:60px;text-align:center;">
      <div style="font-size:32px;margin-bottom:12px;">🗺️</div>
      <div style="font-family:'Syne',sans-serif;font-weight:700;font-size:16px;color:var(--forest);margin-bottom:4px;">No blocks yet</div>
      <div style="font-size:12px;color:var(--stone);">Add blocks to your farm to start tracking activities.</div>
    </div>
    <div v-else class="grid-3">
      <div v-for="s in store.blocks" :key="s.id" class="block-card">
        <div class="block-card-header">
          <div style="display:flex;align-items:center;gap:8px;">
            <div style="width:10px;height:10px;border-radius:50%;" :style="{ background: dotColor(s.id) }" />
            <div class="block-card-name">{{ s.name }}</div>
          </div>
          <div style="display:flex;align-items:center;gap:8px;">
            <StatBadge :variant="s.status === 'Active' ? 'green' : s.status === 'HarvestSoon' ? 'harvest' : 'stone'">
              {{ s.status }}
            </StatBadge>
            <button class="btn-edit" title="Work progress: what is done and what is left" @click="progressBlock = s">◐</button>
            <button class="btn-edit" title="Satellite pictures of this block" @click="satelliteBlock = s">🛰</button>
            <button class="btn-edit" title="Edit block" @click="openEdit(s)">✎</button>
          </div>
        </div>
        <!-- Crop mix pills -->
        <div class="crop-pills">
          <span v-for="c in s.crops" :key="c.cropType" class="crop-pill">
            {{ c.cropType }} {{ c.percentage }}%
          </span>
        </div>
        <div style="padding:0 16px 14px;">
          <div class="stat-row">
            <span class="stat-key">Area</span>
            <span class="stat-val">{{ s.area.displayValue }} {{ s.area.displayUnit }}</span>
          </div>
          <div class="stat-row">
            <span class="stat-key">Boundary</span>
            <span class="stat-val">{{ s.boundarySource != null ? BLOCK_BOUNDARY_SOURCE_LABELS[s.boundarySource] : 'None' }}</span>
          </div>
          <div v-if="s.treeCount != null" class="stat-row">
            <span class="stat-key">Trees</span>
            <span class="stat-val">{{ s.treeCount.toLocaleString() }}</span>
          </div>
          <div class="stat-row">
            <span class="stat-key">Last Activity</span>
            <span class="stat-val">{{ s.lastActivityDate ? formatDate(s.lastActivityDate) : 'Never' }}</span>
          </div>
          <template v-if="satOf(s.id)?.hasBoundary === false || (store.satellite?.configured && satOf(s.id))">
            <div class="stat-row clickable" @click="toggleWhy(s.id + 'sat')">
              <span class="stat-key">Crop condition</span>
              <span class="stat-val" :class="'sat-' + (satOf(s.id)?.condition ?? 'NoData')">{{ satOf(s.id)?.label }}<span class="why">{{ openWhy.has(s.id + 'sat') ? '▴' : '▾' }}</span></span>
            </div>
            <div v-if="openWhy.has(s.id + 'sat')" class="why-box">
              <div class="why-line">{{ satOf(s.id)?.summary }}</div>
              <svg v-if="(satOf(s.id)?.points.length ?? 0) > 1" class="spark" viewBox="0 0 200 40" preserveAspectRatio="none">
                <polyline :points="spark(satOf(s.id)!.points.map(p => p.ndvi))" fill="none" stroke="#3fb56d" stroke-width="2" />
                <polyline :points="spark(satOf(s.id)!.points.map(p => p.ndmi))" fill="none" stroke="#3a86ff" stroke-width="2" />
              </svg>
              <div v-if="(satOf(s.id)?.points.length ?? 0) > 1" class="why-line"><span style="color:#3fb56d">━</span> NDVI (greenness) &nbsp; <span style="color:#3a86ff">━</span> NDMI (leaf water) · last {{ satOf(s.id)!.points.length }} clear readings</div>
              <div class="why-line">Indicators of the crop's condition from Sentinel-2, not a soil-moisture reading.</div>
            </div>
          </template>
          <template v-for="row in cycleRows(s)" :key="row.key">
            <div class="stat-row clickable" @click="toggleWhy(s.id + row.key)">
              <span class="stat-key">{{ row.label }}<span v-if="row.task.aiReviewed" class="ai-chip" title="Reviewed by AI against the forecast">🤖 AI</span></span>
              <span class="stat-val" :class="row.cls">{{ row.text }}<span class="why">{{ openWhy.has(s.id + row.key) ? '▴' : '▾' }}</span></span>
            </div>
            <div v-if="openWhy.has(s.id + row.key)" class="why-box">
              <div v-for="f in row.task.factors ?? []" :key="f" class="why-line">• {{ f }}</div>
              <div v-if="row.task.recommendedSprayDate" class="why-line best">
                {{ row.key === 'f' ? '🌱 Best day' : '💧 Best spray day' }}: {{ formatDate(row.task.recommendedSprayDate) }}
              </div>
            </div>
          </template>
        </div>
      </div>
    </div>

    <BlockModal
      v-if="modalMode"
      :mode="modalMode"
      :block="editingBlock"
      @close="closeModal"
      @saved="onSaved"
    />

    <BlockSatelliteModal v-if="satelliteBlock" :block="satelliteBlock" @close="satelliteBlock = null" />
    <BlockCoverageModal v-if="progressBlock" :block="progressBlock" :initial-type="progressType" @close="closeProgress" />
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useAgroStore } from '@/stores/agro'
import { api, FARM_ID } from '@/services/api'
import { extractErrorMessage } from '@/utils/http'
import StatBadge from '@/components/ui/StatBadge.vue'
import BlockModal from '@/components/modal/BlockModal.vue'
import BlockCoverageModal from '@/components/modal/BlockCoverageModal.vue'
import BlockSatelliteModal from '@/components/modal/BlockSatelliteModal.vue'
import { BLOCK_BOUNDARY_SOURCE_LABELS, type Block, type UpcomingTask } from '@/types'

const store = useAgroStore()

const COLORS = ['#3DAA6A','#E8A838','#2D7A4F','#5B8DB8','#A0783C','#8D6B94']

function dotColor(id: string) {
  const idx = id.charCodeAt(id.length - 1) % COLORS.length
  return COLORS[idx]
}

// Next weeding / spraying for a block, worked out from the weather and what was last done.
const openWhy = ref(new Set<string>())
function toggleWhy(key: string) {
  const next = new Set(openWhy.value)
  if (!next.delete(key)) next.add(key)
  openWhy.value = next
}

function cycleText(t: UpcomingTask): { text: string; cls: string } {
  if (t.status === 'NotNeeded') return { text: 'Not needed (dry)', cls: 'muted' }
  if (t.status === 'Unknown') return { text: 'No history yet', cls: 'muted' }
  const d = t.daysUntilDue
  if (d < 0) return { text: `Overdue ${-d}d`, cls: 'bad' }
  if (d === 0) return { text: 'Today', cls: 'warn' }
  const when = d >= 14 ? `in ${Math.round(d / 7)} wks` : `in ${d}d`
  return { text: when, cls: d <= 7 ? 'warn' : '' }
}

function cycleRows(b: Block) {
  const rows: { key: string; label: string; text: string; cls: string; task: UpcomingTask }[] = []
  if (b.nextWeedingPrediction) rows.push({ key: 'w', label: 'Next Weeding', task: b.nextWeedingPrediction, ...cycleText(b.nextWeedingPrediction) })
  if (b.nextSprayingPrediction) rows.push({ key: 's', label: 'Next Spray', task: b.nextSprayingPrediction, ...cycleText(b.nextSprayingPrediction) })
  if (b.nextFertilizingPrediction) rows.push({ key: 'f', label: 'Next Fertilizer', task: b.nextFertilizingPrediction, ...cycleText(b.nextFertilizingPrediction) })
  return rows
}

function formatDate(iso: string) {
  return new Date(iso).toLocaleDateString('en-NG', { day: 'numeric', month: 'short' })
}

// ── Edit modal (Add Block itself is triggered from the Topbar) ──
const modalMode = ref<'create' | 'edit' | null>(null)
const editingBlock = ref<Block | null>(null)
const progressBlock = ref<Block | null>(null)
const satelliteBlock = ref<Block | null>(null)
const progressType = ref('Weeding')

interface ProgressRow { blockId: string; blockName: string; activityType: string | number; percent: number; coveredHectares: number; blockHectares: number; activityCount: number }
const inProgress = ref<ProgressRow[]>([])
const typeLabel = (t: string | number) => ({ Weeding: 'Weeding', Spraying: 'Spraying', FertilizerApplication: 'Fertilizer', Harvesting: 'Harvest' } as Record<string, string>)[typeName(t)] ?? String(t)
// The API sends enums as numbers; the coverage endpoint wants the name.
function typeName(t: string | number) { return typeof t === 'number' ? (ACTIVITY_NAMES[t] ?? 'Weeding') : t }
const ACTIVITY_NAMES = ['Weeding', 'Spraying', 'FertilizerApplication', 'Maintenance', 'Harvesting', 'Clearing', 'Pruning', 'PestControl']

async function loadProgress() {
  try {
    inProgress.value = (await api.get(`/api/farms/${FARM_ID}/blocks/coverage-progress`)).data ?? []
  } catch { inProgress.value = [] }
}

const satBusy = ref(false)
const satError = ref('')
const satOf = (blockId: string) => store.satellite?.blocks.find(x => x.blockId === blockId)

async function refreshSat() {
  satBusy.value = true
  satError.value = ''
  try { await store.refreshSatellite() } catch (e) { satError.value = extractErrorMessage(e, 'Could not fetch satellite readings.') } finally { satBusy.value = false }
}

/** Scales a series to fit the 200x40 sparkline box; both lines share the same -0.2..1 range so they compare fairly. */
function spark(values: number[]) {
  const lo = -0.2, hi = 1
  return values.map((v, i) => `${(i / (values.length - 1)) * 200},${38 - ((Math.min(hi, Math.max(lo, v)) - lo) / (hi - lo)) * 36}`).join(' ')
}

function openProgress(p: ProgressRow) {
  const b = store.blocks.find(x => x.id === p.blockId)
  if (!b) return
  progressType.value = typeName(p.activityType)
  progressBlock.value = b
}

function closeProgress() {
  progressBlock.value = null
  loadProgress() // a block may have just been completed by hand
}

function openEdit(block: Block) {
  editingBlock.value = block
  modalMode.value = 'edit'
}

function closeModal() {
  modalMode.value = null
  editingBlock.value = null
}

function onSaved() {
  store.fetchBlocks()
}

onMounted(() => { store.fetchBlocks(); loadProgress(); store.fetchSatellite() })
</script>

<style scoped>
.grid-3 { display: grid; grid-template-columns: repeat(3,1fr); gap: 16px; }
.block-card { background: white; border: 1px solid var(--fog); border-radius: 12px; overflow: hidden; transition: border-color 0.15s; }
.block-card:hover { border-color: var(--palm); }
.block-card-header { padding: 14px 16px; border-bottom: 1px solid var(--fog); display: flex; align-items: center; justify-content: space-between; }
.block-card-name { font-family: 'Syne', sans-serif; font-weight: 700; font-size: 14px; color: var(--forest); }
.btn-edit {
  width: 24px; height: 24px; border-radius: 6px; border: 1px solid var(--fog);
  background: transparent; cursor: pointer; font-size: 11px; color: var(--stone);
  display: flex; align-items: center; justify-content: center; transition: border-color 0.15s, color 0.15s;
}
.btn-edit:hover { border-color: var(--palm); color: var(--leaf, #2d7a4f); }
.crop-pills { display: flex; gap: 6px; padding: 10px 16px 0; flex-wrap: wrap; }
.crop-pill { font-family: 'JetBrains Mono', monospace; font-size: 9px; letter-spacing: 0.08em; padding: 3px 8px; background: rgba(45,122,79,0.08); border: 1px solid rgba(45,122,79,0.15); border-radius: 4px; color: var(--leaf); }
.stat-row { display: flex; justify-content: space-between; align-items: center; padding: 5px 0; border-bottom: 1px solid var(--fog); }
.stat-row:last-child { border-bottom: none; }
.stat-key { font-family: 'JetBrains Mono', monospace; font-size: 9px; letter-spacing: 0.08em; color: var(--stone); text-transform: uppercase; }
.stat-val { font-family: 'JetBrains Mono', monospace; font-size: 10px; font-weight: 500; color: var(--forest); display: flex; align-items: center; }
.stat-val.warn { color: var(--harvest); }
.stat-val.bad { color: #E53935; }
.stat-val.muted { color: var(--stone); }
.stat-row.clickable { cursor: pointer; }
.ai-chip { margin-left: 6px; font-size: 8px; letter-spacing: 0; color: var(--leaf); text-transform: none; }
.why { margin-left: 6px; font-size: 8px; color: var(--stone); }
.why-box { background: rgba(45,122,79,0.05); border-radius: 6px; padding: 8px 10px; margin: 2px 0 6px; }
.why-line { font-size: 10px; line-height: 1.5; color: var(--ink); }
.why-line.best { color: var(--leaf); font-weight: 600; margin-top: 2px; }
.sat-bar { display: flex; align-items: center; gap: 12px; flex-wrap: wrap; font-size: 12px; color: var(--stone); margin-bottom: 14px; }
.sat-err { color: var(--alert); }
.sat-Improving { color: var(--leaf); }
.sat-Stable { color: var(--forest); }
.sat-Declining, .sat-DryOrBare { color: var(--alert); }
.sat-NoData, .sat-Building { color: var(--stone); }
.spark { width: 100%; height: 40px; margin: 6px 0 2px; background: var(--mist); border-radius: 6px; }
.progress-strip { margin-bottom: 18px; }
.progress-title { font-family: 'Syne', sans-serif; font-weight: 700; font-size: 13px; color: var(--forest); margin-bottom: 8px; }
.progress-title span { font-family: 'JetBrains Mono', monospace; font-size: 11px; color: var(--stone); margin-left: 6px; }
.progress-list { display: grid; grid-template-columns: repeat(auto-fill, minmax(230px, 1fr)); gap: 10px; }
.progress-card { text-align: left; border: 1px solid var(--fog); background: white; border-radius: 12px; padding: 10px 12px; cursor: pointer; }
.progress-card:hover { border-color: var(--palm); }
.pc-top { display: flex; justify-content: space-between; align-items: center; font-size: 13px; color: var(--forest); }
.pc-type { font-size: 11px; color: var(--stone); }
.pc-bar { height: 6px; background: var(--fog); border-radius: 4px; margin: 8px 0 6px; overflow: hidden; }
.pc-bar i { display: block; height: 100%; background: #3fb56d; }
.pc-meta { font-size: 11.5px; color: var(--stone); }
</style>
