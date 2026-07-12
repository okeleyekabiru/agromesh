<template>
  <div class="sections-shell">

    <!-- ── Left: info panel ── -->
    <div class="sections-panel">
      <div class="panel-brand">
        <svg width="20" height="20" viewBox="0 0 72 72" fill="none">
          <rect width="72" height="72" rx="16" fill="#1A4A32"/>
          <circle cx="36" cy="36" r="7" fill="#E8A838"/>
          <circle cx="36" cy="36" r="3.5" fill="#0D2B1F"/>
          <line x1="24" y1="24" x2="36" y2="36" stroke="#3DAA6A" stroke-width="1.5"/>
          <line x1="48" y1="24" x2="36" y2="36" stroke="#3DAA6A" stroke-width="1.5"/>
          <line x1="24" y1="48" x2="36" y2="36" stroke="#3DAA6A" stroke-width="1.5"/>
          <line x1="48" y1="48" x2="36" y2="36" stroke="#3DAA6A" stroke-width="1.5"/>
        </svg>
        <span class="brand-name">Agro<span class="brand-accent">Mesh</span></span>
      </div>

      <div class="panel-content">
        <div class="panel-tag">Farm Sections Setup</div>
        <h2 class="panel-h2">Define your<br>farm plots</h2>
        <p class="panel-sub">
          Each plot is a distinct section of your farm. Add its name, area and crops, then mark its
          boundary — with GPS corners or a survey file — or skip and rely on the area you entered.
        </p>

        <!-- Boundary method legend -->
        <div class="method-legend">
          <div class="method-legend-item">
            <div class="method-dot method-dot--gps" />
            <div>
              <div class="method-legend-title">GPS corner capture</div>
              <div class="method-legend-sub">Walk to each corner and mark it with your device's GPS</div>
            </div>
          </div>
          <div class="method-legend-item">
            <div class="method-dot method-dot--upload" />
            <div>
              <div class="method-legend-title">Survey upload</div>
              <div class="method-legend-sub">Upload a GeoJSON or KML boundary file</div>
            </div>
          </div>
        </div>

        <!-- Added sections list -->
        <div class="sections-list" v-if="store.wizardSections.length">
          <div class="sections-list-label">Added plots ({{ store.wizardSections.length }})</div>
          <div v-for="(s, i) in store.wizardSections" :key="s.id" class="section-item">
            <div class="section-dot" :style="{ background: COLORS[i % COLORS.length] }" />
            <div class="section-item-body">
              <div class="section-item-name">{{ s.name }}</div>
              <div class="section-item-meta">
                {{ s.area.hectares.toFixed(2) }} ha
                <span class="boundary-src-badge" :class="`src-${boundarySrcClass(s.boundarySource)}`">
                  {{ s.boundarySource != null ? SECTION_BOUNDARY_SOURCE_LABELS[s.boundarySource] : 'Manual estimate' }}
                </span>
              </div>
            </div>
          </div>
        </div>

        <div class="panel-actions">
          <button class="btn-finish" @click="goToReview">
            {{ store.wizardSections.length ? 'Continue to Review →' : 'Skip for now →' }}
          </button>
        </div>
      </div>
    </div>

    <!-- ── Right: main work area ── -->
    <div class="sections-main">
      <SectionBoundaryEditor :key="editorKey" mode="create" @saved="onSectionSaved" />
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAgroStore } from '@/stores/agro'
import SectionBoundaryEditor from '@/components/section/SectionBoundaryEditor.vue'
import { SECTION_BOUNDARY_SOURCE_LABELS, type Section } from '@/types'

const router = useRouter()
const store  = useAgroStore()

const COLORS = ['#2d7a4f', '#3daa6a', '#e8a838', '#e64a19', '#5b4fcf', '#c2185b', '#0288d1', '#00796b']
const BOUNDARY_SRC_CLASS = ['gps', 'upload', 'manual']

function boundarySrcClass(source: number | null) {
  return BOUNDARY_SRC_CLASS[source ?? 2]
}

// Remounting the editor with a fresh key resets its internal form state for the next plot.
const editorKey = ref(0)

function onSectionSaved(section: Section) {
  store.wizardSections.push(section)
  editorKey.value++
}

function goToReview() {
  router.push({ name: 'setup-review' })
}
</script>

<style scoped>
.sections-shell {
  display: flex;
  min-height: 100vh;
  background: #0d2b1f;
}

/* ── Left panel ── */
.sections-panel {
  width: 320px;
  flex-shrink: 0;
  display: flex;
  flex-direction: column;
  background: #0d2b1f;
  border-right: 1px solid rgba(255,255,255,0.07);
  overflow-y: auto;
}

.panel-brand {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 18px 20px 14px;
  border-bottom: 1px solid rgba(255,255,255,0.06);
}
.brand-name { font-family: 'Syne', sans-serif; font-weight: 700; font-size: 15px; color: #fff; letter-spacing: -0.01em; }
.brand-accent { color: #3DAA6A; }

.panel-content { padding: 18px 20px; flex: 1; display: flex; flex-direction: column; }
.panel-tag { font-family: 'JetBrains Mono', monospace; font-size: 9px; letter-spacing: 0.14em; text-transform: uppercase; color: #3DAA6A; margin-bottom: 8px; }
.panel-h2 { font-family: 'Syne', sans-serif; font-size: 20px; font-weight: 700; color: #fff; line-height: 1.25; letter-spacing: -0.02em; margin-bottom: 8px; }
.panel-sub { font-size: 11px; color: rgba(255,255,255,0.45); line-height: 1.6; margin-bottom: 16px; }

.method-legend { margin-bottom: 16px; }
.method-legend-item { display: flex; align-items: flex-start; gap: 10px; margin-bottom: 10px; }
.method-dot { width: 8px; height: 8px; border-radius: 50%; flex-shrink: 0; margin-top: 3px; }
.method-dot--gps     { background: #3daa6a; }
.method-dot--upload  { background: #5b4fcf; }
.method-legend-title { font-size: 11px; font-weight: 600; color: rgba(255,255,255,0.8); }
.method-legend-sub   { font-size: 10px; color: rgba(255,255,255,0.35); line-height: 1.4; margin-top: 1px; }

.sections-list { margin-bottom: 14px; }
.sections-list-label { font-family: 'JetBrains Mono', monospace; font-size: 9px; letter-spacing: 0.1em; text-transform: uppercase; color: rgba(255,255,255,0.3); margin-bottom: 6px; }
.section-item { display: flex; align-items: center; gap: 8px; padding: 7px 8px; border-radius: 8px; margin-bottom: 3px; }
.section-dot { width: 9px; height: 9px; border-radius: 50%; flex-shrink: 0; }
.section-item-body { flex: 1; min-width: 0; }
.section-item-name { font-size: 11px; font-weight: 600; color: #fff; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
.section-item-meta { display: flex; align-items: center; gap: 6px; font-family: 'JetBrains Mono', monospace; font-size: 8.5px; color: rgba(255,255,255,0.35); margin-top: 2px; }
.boundary-src-badge {
  font-family: 'JetBrains Mono', monospace;
  font-size: 8px; padding: 1px 6px; border-radius: 4px;
  text-transform: uppercase; letter-spacing: 0.05em;
}
.boundary-src-badge.src-gps    { background: rgba(61,170,106,0.15); color: #3daa6a; }
.boundary-src-badge.src-upload { background: rgba(91,79,207,0.18); color: #9b8fef; }
.boundary-src-badge.src-manual { background: rgba(255,255,255,0.08); color: rgba(255,255,255,0.5); }

.panel-actions { margin-top: auto; padding-top: 14px; }
.btn-finish {
  width: 100%;
  padding: 11px 16px;
  background: #3DAA6A;
  color: #fff;
  border: none;
  border-radius: 10px;
  font-size: 12px;
  font-weight: 600;
  cursor: pointer;
  transition: background 0.15s;
}
.btn-finish:hover:not(:disabled) { background: #2d7a4f; }
.btn-finish:disabled { opacity: 0.5; cursor: not-allowed; }

/* ── Right: main work area ── */
.sections-main { flex: 1; display: flex; flex-direction: column; align-items: center; overflow-y: auto; padding: 32px; }
</style>
