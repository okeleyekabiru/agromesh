<template>
  <div>
    <div v-if="store.loading" style="padding:40px;text-align:center;color:var(--stone);font-size:12px;">Loading sections…</div>
    <div v-else-if="!store.sections.length" style="padding:60px;text-align:center;">
      <div style="font-size:32px;margin-bottom:12px;">🗺️</div>
      <div style="font-family:'Syne',sans-serif;font-weight:700;font-size:16px;color:var(--forest);margin-bottom:4px;">No sections yet</div>
      <div style="font-size:12px;color:var(--stone);">Add sections to your farm to start tracking activities.</div>
    </div>
    <div v-else class="grid-3">
      <div v-for="s in store.sections" :key="s.id" class="section-card">
        <div class="section-card-header">
          <div style="display:flex;align-items:center;gap:8px;">
            <div style="width:10px;height:10px;border-radius:50%;" :style="{ background: dotColor(s.id) }" />
            <div class="section-card-name">{{ s.name }}</div>
          </div>
          <div style="display:flex;align-items:center;gap:8px;">
            <StatBadge :variant="s.status === 'Active' ? 'green' : s.status === 'HarvestSoon' ? 'harvest' : 'stone'">
              {{ s.status }}
            </StatBadge>
            <button class="btn-edit" title="Edit section" @click="openEdit(s)">✎</button>
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
            <span class="stat-val">{{ s.boundarySource != null ? SECTION_BOUNDARY_SOURCE_LABELS[s.boundarySource] : 'None' }}</span>
          </div>
          <div class="stat-row">
            <span class="stat-key">Blocks</span>
            <span class="stat-val">
              {{ s.blocks.length }}
              <button class="btn-blocks" title="Manage blocks" @click="openBlocks(s)">+</button>
            </span>
          </div>
          <div class="stat-row">
            <span class="stat-key">Last Activity</span>
            <span class="stat-val">{{ s.lastActivityDate ? formatDate(s.lastActivityDate) : 'Never' }}</span>
          </div>
          <div v-if="s.nextWeedingPrediction" class="stat-row">
            <span class="stat-key">Next Weeding</span>
            <span class="stat-val" :class="{ warn: s.nextWeedingPrediction.daysUntilDue <= 7 }">
              {{ s.nextWeedingPrediction.daysUntilDue < 0 ? 'Overdue' : `in ${s.nextWeedingPrediction.daysUntilDue}d` }}
            </span>
          </div>
          <div v-if="s.nextSprayingPrediction" class="stat-row">
            <span class="stat-key">Next Spray</span>
            <span class="stat-val" :class="{ warn: s.nextSprayingPrediction.daysUntilDue <= 7 }">
              {{ s.nextSprayingPrediction.daysUntilDue < 0 ? 'Overdue' : `in ${s.nextSprayingPrediction.daysUntilDue}d` }}
            </span>
          </div>
        </div>
      </div>
    </div>

    <SectionModal
      v-if="modalMode"
      :mode="modalMode"
      :section="editingSection"
      @close="closeModal"
      @saved="onSaved"
    />

    <BlockModal
      v-if="blocksSection"
      :section="blocksSection"
      @close="blocksSection = null"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useAgroStore } from '@/stores/agro'
import StatBadge from '@/components/ui/StatBadge.vue'
import SectionModal from '@/components/modal/SectionModal.vue'
import BlockModal from '@/components/modal/BlockModal.vue'
import { SECTION_BOUNDARY_SOURCE_LABELS, type Section } from '@/types'

const store = useAgroStore()

const COLORS = ['#3DAA6A','#E8A838','#2D7A4F','#5B8DB8','#A0783C','#8D6B94']

function dotColor(id: string) {
  const idx = id.charCodeAt(id.length - 1) % COLORS.length
  return COLORS[idx]
}

function formatDate(iso: string) {
  return new Date(iso).toLocaleDateString('en-NG', { day: 'numeric', month: 'short' })
}

// ── Edit modal (Add Section itself is triggered from the Topbar) ──
const modalMode = ref<'create' | 'edit' | null>(null)
const editingSection = ref<Section | null>(null)

function openEdit(section: Section) {
  editingSection.value = section
  modalMode.value = 'edit'
}

function closeModal() {
  modalMode.value = null
  editingSection.value = null
}

function onSaved() {
  store.fetchSections()
}

// ── Blocks modal ──
const blocksSection = ref<Section | null>(null)

function openBlocks(section: Section) {
  blocksSection.value = section
}

onMounted(() => store.fetchSections())
</script>

<style scoped>
.grid-3 { display: grid; grid-template-columns: repeat(3,1fr); gap: 16px; }
.section-card { background: white; border: 1px solid var(--fog); border-radius: 12px; overflow: hidden; transition: border-color 0.15s; }
.section-card:hover { border-color: var(--palm); }
.section-card-header { padding: 14px 16px; border-bottom: 1px solid var(--fog); display: flex; align-items: center; justify-content: space-between; }
.section-card-name { font-family: 'Syne', sans-serif; font-weight: 700; font-size: 14px; color: var(--forest); }
.btn-edit {
  width: 24px; height: 24px; border-radius: 6px; border: 1px solid var(--fog);
  background: transparent; cursor: pointer; font-size: 11px; color: var(--stone);
  display: flex; align-items: center; justify-content: center; transition: border-color 0.15s, color 0.15s;
}
.btn-edit:hover { border-color: var(--palm); color: var(--leaf, #2d7a4f); }
.btn-blocks {
  width: 16px; height: 16px; border-radius: 4px; border: 1px solid var(--fog); margin-left: 6px;
  background: transparent; cursor: pointer; font-size: 10px; line-height: 1; color: var(--stone);
  display: inline-flex; align-items: center; justify-content: center; transition: border-color 0.15s, color 0.15s;
}
.btn-blocks:hover { border-color: var(--palm); color: var(--leaf, #2d7a4f); }
.crop-pills { display: flex; gap: 6px; padding: 10px 16px 0; flex-wrap: wrap; }
.crop-pill { font-family: 'JetBrains Mono', monospace; font-size: 9px; letter-spacing: 0.08em; padding: 3px 8px; background: rgba(45,122,79,0.08); border: 1px solid rgba(45,122,79,0.15); border-radius: 4px; color: var(--leaf); }
.stat-row { display: flex; justify-content: space-between; align-items: center; padding: 5px 0; border-bottom: 1px solid var(--fog); }
.stat-row:last-child { border-bottom: none; }
.stat-key { font-family: 'JetBrains Mono', monospace; font-size: 9px; letter-spacing: 0.08em; color: var(--stone); text-transform: uppercase; }
.stat-val { font-family: 'JetBrains Mono', monospace; font-size: 10px; font-weight: 500; color: var(--forest); display: flex; align-items: center; }
.stat-val.warn { color: var(--harvest); }
</style>
