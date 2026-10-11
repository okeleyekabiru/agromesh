<template>
  <div>
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
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useAgroStore } from '@/stores/agro'
import StatBadge from '@/components/ui/StatBadge.vue'
import BlockModal from '@/components/modal/BlockModal.vue'
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

onMounted(() => store.fetchBlocks())
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
</style>
