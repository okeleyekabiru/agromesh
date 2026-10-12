<template>
  <div class="modal-overlay" @click.self="$emit('close')">
    <div class="modal">
      <div class="modal-header">
        <div class="modal-title">{{ block.name }} · work progress</div>
        <button class="modal-close-inline" aria-label="Close" @click="$emit('close')">×</button>
      </div>

      <div class="modal-body">
        <div class="tabs">
          <button v-for="t in TYPES" :key="t.key" class="tab" :class="{ on: type === t.key }" @click="type = t.key">{{ t.label }}</button>
        </div>

        <div v-if="loading" class="note">Loading…</div>
        <div v-else-if="error" class="error-msg">{{ error }}</div>
        <template v-else-if="cov">
          <div class="summary">
            <div class="pct">{{ cov.hasBoundary ? `${cov.percent}%` : '—' }}</div>
            <div class="sum-text">
              <b>{{ statusText }}</b>
              <div v-if="cov.hasBoundary">
                {{ cov.coveredHectares.toFixed(2) }} ha done · {{ (cov.remainingHectares ?? 0).toFixed(2) }} ha left of {{ cov.blockHectares.toFixed(2) }} ha
              </div>
              <div v-if="cov.lastWorkedOn">Cycle started {{ fmt(cov.cycleStartedOn) }} · last worked {{ fmt(cov.lastWorkedOn) }} · {{ cov.activityCount }} {{ cov.activityCount === 1 ? 'job' : 'jobs' }}</div>
            </div>
          </div>

          <div v-if="!cov.hasBoundary" class="note">
            This block has no boundary yet, so what is done and what is left cannot be drawn. Set its boundary on the Blocks page.
          </div>
          <template v-else>
            <div ref="mapEl" class="map" />
            <div class="legend">
              <span><i class="sw done" /> Done</span>
              <span><i class="sw left" /> Left to do</span>
            </div>

            <!-- Each job's marked area, numbered, so the picks can be checked against the block -->
            <div v-if="cov.sections.length" class="sections">
              <div class="sections-title">Areas marked this cycle ({{ cov.sections.length }})</div>
              <button v-for="s in cov.sections" :key="s.number" class="section-row" :class="{ on: picked === s.number }" @click="focusSection(s.number)">
                <span class="num" :style="{ background: colour(s.number) }">{{ s.number }}</span>
                <span>{{ fmt(s.date) }}</span>
                <span class="ha">{{ s.hectares.toFixed(2) }} ha</span>
              </button>
            </div>
          </template>

          <!-- Marked areas can stop a few metres short of the block's edge and never reach the end: finish it by hand. -->
          <div v-if="cov.hasBoundary && cov.status === 'InProgress'" class="force">
            <div v-if="!confirming">
              <button class="btn btn-ghost btn-sm" @click="confirming = true">Mark as complete</button>
              <span class="hint">Use this when the work is finished but the marked areas fall a little short of the block.</span>
            </div>
            <div v-else class="force-box">
              <div class="force-text">
                {{ (cov.remainingHectares ?? 0).toFixed(2) }} ha is still shown as left. Treat the whole block as done and start a new cycle?
              </div>
              <input v-model="reason" class="force-input" maxlength="200" placeholder="Reason (optional), e.g. GPS drift on the north edge" />
              <div v-if="forceError" class="error-msg">{{ forceError }}</div>
              <div class="force-actions">
                <button class="btn btn-ghost btn-sm" :disabled="forcing" @click="confirming = false">Cancel</button>
                <button class="btn btn-primary btn-sm" :disabled="forcing" @click="forceComplete">{{ forcing ? 'Saving…' : 'Yes, complete it' }}</button>
              </div>
            </div>
          </div>
        </template>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, nextTick, onMounted, onBeforeUnmount } from 'vue'
import L from 'leaflet'
import 'leaflet/dist/leaflet.css'
import { api, FARM_ID } from '@/services/api'
import { extractErrorMessage } from '@/utils/http'
import type { Block } from '@/types'

interface Coverage {
  status: string; hasBoundary: boolean; blockHectares: number; coveredHectares: number; remainingHectares: number | null
  percent: number; cycleStartedOn: string | null; lastWorkedOn: string | null; activityCount: number
  blockGeoJson: string | null; coveredGeoJson: string | null; remainingGeoJson: string | null
  sections: { number: number; date: string; hectares: number; geoJson: string }[]
}

const props = defineProps<{ block: Block; initialType?: string }>()
defineEmits<{ close: [] }>()

const TYPES = [
  { key: 'Weeding', label: 'Weeding' },
  { key: 'Spraying', label: 'Spraying' },
  { key: 'FertilizerApplication', label: 'Fertilizer' },
  { key: 'Harvesting', label: 'Harvest' }
]

const type = ref(props.initialType ?? 'Weeding')
const cov = ref<Coverage | null>(null)
const loading = ref(false)
const error = ref('')
const mapEl = ref<HTMLElement | null>(null)
const confirming = ref(false)
const reason = ref('')
const forcing = ref(false)
const forceError = ref('')
let map: L.Map | null = null
const picked = ref<number | null>(null)
const sectionLayers = new Map<number, L.Polygon[]>()

const PALETTE = ['#e4572e', '#17bebb', '#9b5de5', '#f15bb5', '#3a86ff', '#fb8500', '#2a9d8f', '#8338ec']
const colour = (n: number) => PALETTE[(n - 1) % PALETTE.length]

function focusSection(n: number) {
  picked.value = picked.value === n ? null : n
  for (const [num, layers] of sectionLayers)
    layers.forEach(l => l.setStyle({ weight: picked.value === num ? 4 : 2, fillOpacity: picked.value === num ? 0.25 : 0.05 }))
  const layers = sectionLayers.get(n)
  if (picked.value === n && layers?.length && map) map.fitBounds(L.featureGroup(layers).getBounds(), { padding: [30, 30] })
}

const statusText = computed(() => {
  switch (cov.value?.status) {
    case 'Complete': return 'Cycle complete: the whole block is done'
    case 'InProgress': return 'In progress'
    default: return 'Not started this cycle'
  }
})

const fmt = (iso: string | null) => (iso ? new Date(iso).toLocaleDateString('en-GB', { day: 'numeric', month: 'short' }) : '')

async function load() {
  loading.value = true
  error.value = ''
  confirming.value = false
  forceError.value = ''
  try {
    cov.value = (await api.get(`/api/farms/${FARM_ID}/blocks/${props.block.id}/coverage?activityType=${type.value}`)).data
  } catch (e) {
    cov.value = null
    error.value = extractErrorMessage(e, 'Could not load the work progress.')
  } finally {
    loading.value = false
  }
  await nextTick()
  draw()
}

async function forceComplete() {
  forcing.value = true
  forceError.value = ''
  try {
    await api.post(`/api/farms/${FARM_ID}/blocks/${props.block.id}/coverage/complete?activityType=${type.value}`, { reason: reason.value.trim() || null })
    reason.value = ''
    await load()
  } catch (e) {
    forceError.value = extractErrorMessage(e, 'Could not complete this cycle. Please try again.')
  } finally {
    forcing.value = false
  }
}

function addGeo(json: string | null, style: L.PathOptions, group: L.FeatureGroup) {
  if (!json) return
  try { L.geoJSON(JSON.parse(json), { style: () => style, interactive: false } as L.GeoJSONOptions).addTo(group) } catch { /* ignore a bad shape */ }
}

/** One job's outline in its own colour, with a dot on every picked corner and its number in the middle. */
function drawSection(s: Coverage['sections'][number], group: L.FeatureGroup) {
  let geo: { type: string; coordinates: number[][][] | number[][][][] }
  try { geo = JSON.parse(s.geoJson) } catch { return }
  const polys = (geo.type === 'MultiPolygon' ? geo.coordinates : [geo.coordinates]) as number[][][][]
  const col = colour(s.number)
  const layers: L.Polygon[] = []
  for (const rings of polys) {
    const latlngs = rings.map(r => r.map(([lng, lat]) => [lat, lng] as L.LatLngTuple))
    const poly = L.polygon(latlngs, { color: col, weight: 2, fillColor: col, fillOpacity: 0.05, dashArray: '5 4' }).addTo(group)
    layers.push(poly)
    const outer = latlngs[0].slice(0, -1) // the ring repeats its first point
    outer.forEach(p => L.circleMarker(p, { radius: 3.5, color: '#fff', weight: 1, fillColor: col, fillOpacity: 1, interactive: false }).addTo(group))
  }
  sectionLayers.set(s.number, layers)
  const centre = L.featureGroup(layers).getBounds().getCenter()
  L.marker(centre, {
    interactive: false,
    icon: L.divIcon({ className: 'sec-num', html: `<span style="background:${col}">${s.number}</span>`, iconSize: [22, 22] })
  }).addTo(group)
}

function draw() {
  map?.remove()
  map = null
  sectionLayers.clear()
  picked.value = null
  if (!mapEl.value || !cov.value?.hasBoundary) return
  map = L.map(mapEl.value, { zoomControl: true })
  L.tileLayer('https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}', {
    maxZoom: 19, attribution: 'Imagery © Esri'
  }).addTo(map)

  const group = L.featureGroup().addTo(map)
  addGeo(cov.value.remainingGeoJson, { color: '#f5a623', weight: 1, fillColor: '#f5a623', fillOpacity: 0.45 }, group)
  addGeo(cov.value.coveredGeoJson, { color: '#2d7a4f', weight: 1, fillColor: '#3fb56d', fillOpacity: 0.55 }, group)
  addGeo(cov.value.blockGeoJson, { color: '#ffffff', weight: 2.5, fill: false }, group)
  for (const s of cov.value.sections) drawSection(s, group)
  const b = group.getBounds()
  if (b.isValid()) map.fitBounds(b, { padding: [20, 20] })
}

onMounted(load)
onBeforeUnmount(() => { map?.remove(); map = null })
watch(type, load)
</script>

<style>
.sec-num span { display: flex; align-items: center; justify-content: center; width: 22px; height: 22px; border-radius: 50%; color: #fff; font: 700 11px 'JetBrains Mono', monospace; border: 2px solid #fff; }
</style>

<style scoped>
.modal-overlay {
  position: fixed; inset: 0; background: rgba(10,26,16,0.6); backdrop-filter: blur(4px);
  z-index: 200; display: flex; align-items: flex-start; justify-content: center; padding: 40px 24px; overflow-y: auto;
}
.modal { background: white; border-radius: 16px; width: 640px; max-width: 100%; box-shadow: 0 24px 64px rgba(10,26,16,0.2); }
.modal-header { padding: 18px 24px 14px; border-bottom: 1px solid var(--fog); display: flex; align-items: center; justify-content: space-between; }
.modal-title { font-family: 'Syne', sans-serif; font-weight: 700; font-size: 15px; color: var(--forest); }
.modal-close-inline { width: 28px; height: 28px; border-radius: 6px; border: 1px solid var(--fog); background: transparent; cursor: pointer; font-size: 18px; color: var(--stone); }
.modal-body { padding: 16px 24px 22px; }
.tabs { display: flex; gap: 6px; flex-wrap: wrap; margin-bottom: 14px; }
.tab { border: 1.5px solid var(--fog); background: transparent; border-radius: 20px; padding: 5px 14px; font-size: 12px; cursor: pointer; color: var(--stone); }
.tab.on { border-color: var(--palm); background: rgba(45,122,79,0.1); color: var(--forest); font-weight: 600; }
.summary { display: flex; align-items: center; gap: 16px; margin-bottom: 12px; }
.pct { font-family: 'JetBrains Mono', monospace; font-size: 30px; font-weight: 600; color: var(--forest); min-width: 84px; }
.sum-text { font-size: 12.5px; color: var(--forest); line-height: 1.6; }
.map { height: 360px; border-radius: 10px; overflow: hidden; border: 1px solid var(--fog); }
.legend { display: flex; gap: 16px; font-size: 12px; color: var(--stone); margin-top: 8px; }
.sw { display: inline-block; width: 12px; height: 12px; border-radius: 3px; margin-right: 5px; vertical-align: -1px; }
.sw.done { background: #3fb56d; }
.sw.left { background: #f5a623; }
.force { margin-top: 14px; border-top: 1px solid var(--fog); padding-top: 12px; }
.hint { font-size: 11.5px; color: var(--stone); margin-left: 10px; }
.force-box { display: flex; flex-direction: column; gap: 8px; }
.force-text { font-size: 12.5px; color: var(--forest); }
.force-input { border: 1px solid var(--fog); border-radius: 8px; padding: 7px 10px; font-size: 12.5px; }
.force-actions { display: flex; gap: 8px; justify-content: flex-end; }
.sections { margin-top: 12px; }
.sections-title { font-size: 11.5px; color: var(--stone); margin-bottom: 6px; }
.section-row { display: flex; align-items: center; gap: 10px; width: 100%; text-align: left; border: 1px solid var(--fog); background: transparent; border-radius: 8px; padding: 6px 10px; margin-bottom: 4px; font-size: 12.5px; color: var(--forest); cursor: pointer; }
.section-row.on { border-color: var(--palm); background: rgba(45,122,79,0.08); }
.num { width: 20px; height: 20px; border-radius: 50%; color: #fff; font: 700 11px 'JetBrains Mono', monospace; display: inline-flex; align-items: center; justify-content: center; }
.ha { margin-left: auto; font-family: 'JetBrains Mono', monospace; color: var(--stone); }
.note { font-size: 12.5px; color: var(--stone); padding: 8px 0; }
.error-msg { font-size: 12px; color: var(--alert); }
</style>
