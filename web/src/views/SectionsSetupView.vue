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
          Each section is a distinct plot — it can be in a different location
          and can have multiple crops. Define its boundary using one of the
          three methods on the right.
        </p>

        <!-- Boundary method legend -->
        <div class="method-legend">
          <div class="method-legend-item">
            <div class="method-dot method-dot--map" />
            <div>
              <div class="method-legend-title">Pick on map</div>
              <div class="method-legend-sub">Click 4 corners directly on the satellite view</div>
            </div>
          </div>
          <div class="method-legend-item">
            <div class="method-dot method-dot--coords" />
            <div>
              <div class="method-legend-title">Enter coordinates</div>
              <div class="method-legend-sub">Type in the lat/lng for each corner</div>
            </div>
          </div>
          <div class="method-legend-item">
            <div class="method-dot method-dot--survey" />
            <div>
              <div class="method-legend-title">Survey plan</div>
              <div class="method-legend-sub">Copy the 4-corner coords from your document</div>
            </div>
          </div>
        </div>

        <!-- Added sections list -->
        <div class="sections-list" v-if="sections.length">
          <div class="sections-list-label">Added plots ({{ sections.length }})</div>
          <div
            v-for="(s, i) in sections"
            :key="i"
            class="section-item"
            :class="{ 'section-item--active': activeSection === i }"
            @click="focusSection(i)"
          >
            <div class="section-dot" :style="{ background: COLORS[i % COLORS.length] }" />
            <div class="section-item-body">
              <div class="section-item-name">{{ s.name }}</div>
              <div class="section-item-meta">
                {{ s.crops.map(c => `${c.cropType} ${c.percentage}%`).join(' · ') }}
                · {{ s.areaValue }}{{ s.areaUnit === 'Hectares' ? ' ha' : ' ac' }}
                <span v-if="s.boundaryGeoJson" class="has-boundary">· boundary ✓</span>
              </div>
            </div>
            <button class="section-remove" @click.stop="removeSection(i)">×</button>
          </div>
        </div>

        <div class="panel-actions">
          <button class="btn-finish" @click="goToDashboard" :disabled="saving">
            <span v-if="saving" class="spinner-sm" />
            {{ saving ? 'Saving…' : (sections.length ? 'Done — Go to Dashboard →' : 'Skip for now →') }}
          </button>
          <div v-if="saveError" class="panel-err">{{ saveError }}</div>
        </div>
      </div>
    </div>

    <!-- ── Right: map + form ── -->
    <div class="sections-main">

      <!-- Map -->
      <div id="sections-map" class="sections-map" />

      <!-- Form card -->
      <div class="section-form-card">

        <!-- Boundary method tabs -->
        <div class="boundary-tabs" role="tablist">
          <button role="tab" :class="['btab', { 'btab--active': boundaryMethod === 'map' }]"
                  @click="setMethod('map')">
            <svg width="11" height="11" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polygon points="3 11 22 2 13 21 11 13 3 11"/></svg>
            Pick on map
          </button>
          <button role="tab" :class="['btab', { 'btab--active': boundaryMethod === 'corners' }]"
                  @click="setMethod('corners')">
            <svg width="11" height="11" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="5" cy="5" r="2"/><circle cx="19" cy="5" r="2"/><circle cx="19" cy="19" r="2"/><circle cx="5" cy="19" r="2"/><line x1="5" y1="7" x2="5" y2="17"/><line x1="19" y1="7" x2="19" y2="17"/><line x1="7" y1="5" x2="17" y2="5"/><line x1="7" y1="19" x2="17" y2="19"/></svg>
            Enter 4 corners
          </button>
          <button role="tab" :class="['btab', { 'btab--active': boundaryMethod === 'survey' }]"
                  @click="setMethod('survey')">
            <svg width="11" height="11" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M14 2H6a2 2 0 00-2 2v16a2 2 0 002 2h12a2 2 0 002-2V8z"/><polyline points="14 2 14 8 20 8"/><line x1="16" y1="13" x2="8" y2="13"/><line x1="16" y1="17" x2="8" y2="17"/></svg>
            Survey plan
          </button>
        </div>

        <!-- ── Map method ── -->
        <div v-if="boundaryMethod === 'map'" class="method-body">
          <div class="map-progress">
            <div class="progress-track">
              <div
                v-for="n in 4" :key="n"
                class="progress-dot"
                :class="{
                  'progress-dot--done': cur.polygonPoints.length >= n,
                  'progress-dot--active': cur.polygonPoints.length === n - 1
                }"
              >{{ n }}</div>
            </div>
            <span class="progress-label">{{ mapProgressLabel }}</span>
            <button v-if="cur.polygonPoints.length" class="btn-clear-sm" @click="clearPolygon">Clear</button>
          </div>
          <div class="map-hint">
            Click 4 corners of this plot on the map above.
            Auto-closes after the 4th click.
          </div>
        </div>

        <!-- ── Corners / Survey plan method ── -->
        <div v-else class="method-body">
          <div v-if="boundaryMethod === 'survey'" class="survey-hint">
            <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M14 2H6a2 2 0 00-2 2v16a2 2 0 002 2h12a2 2 0 002-2V8z"/></svg>
            Copy the 4 corner coordinates from your survey plan document into the fields below.
          </div>

          <div class="corners-grid">
            <div v-for="(corner, i) in corners" :key="i" class="corner-row">
              <div class="corner-label-wrap">
                <div class="corner-dot" :style="{ background: CORNER_COLORS[i] }" />
                <span class="corner-label">{{ CORNER_NAMES[i] }}</span>
              </div>
              <input
                class="form-input corner-input"
                v-model.number="corner.lat"
                type="number"
                step="0.00001"
                placeholder="Latitude"
                @input="clearCornersPreview"
              />
              <input
                class="form-input corner-input"
                v-model.number="corner.lng"
                type="number"
                step="0.00001"
                placeholder="Longitude"
                @input="clearCornersPreview"
              />
            </div>
          </div>

          <div class="corners-actions">
            <button class="btn-preview" :disabled="!cornersValid" @click="previewCorners">
              <svg width="11" height="11" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"/><circle cx="12" cy="12" r="3"/></svg>
              Preview on map
            </button>
            <div v-if="cornersPreviewDone" class="corners-ok">
              <svg width="10" height="10" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><polyline points="20 6 9 17 4 12"/></svg>
              Boundary ready
            </div>
          </div>

          <div class="corners-hint-txt" v-if="!cornersValid">
            Enter all 4 corners to preview the plot boundary on the map.
          </div>

          <!-- GPS assist for each corner -->
          <div class="gps-assist">
            <button type="button" class="btn-gps-assist" @click="fillCornerFromGps" :disabled="gpsLoading">
              <svg width="11" height="11" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round"><circle cx="12" cy="12" r="3"/><path d="M12 2v3M12 19v3M2 12h3M19 12h3"/></svg>
              <span class="gps-spinner-sm" v-if="gpsLoading" />
              {{ gpsLoading ? 'Getting GPS…' : 'Centre map on my GPS location' }}
            </button>
          </div>
        </div>

        <!-- ── Section details ── -->
        <div class="section-details">
          <div class="details-divider"><span>Plot details</span></div>

          <div class="form-row">
            <div class="form-group" style="flex:2">
              <label class="form-label">Plot / section name</label>
              <input class="form-input" v-model="cur.name" placeholder="e.g. North Plot, Block A" />
            </div>
            <div class="form-group" style="flex:1">
              <label class="form-label">Area</label>
              <input class="form-input" v-model.number="cur.areaValue" type="number" min="0.1" step="0.5" />
            </div>
            <div class="form-group" style="width:68px">
              <label class="form-label">Unit</label>
              <select class="form-select" v-model="cur.areaUnit">
                <option value="Hectares">ha</option>
                <option value="Acres">ac</option>
              </select>
            </div>
          </div>

          <!-- Crops -->
          <div class="form-group">
            <label class="form-label">Crops <span class="label-hint">% must total 100</span></label>
            <div v-for="(c, i) in cur.crops" :key="i" class="crop-row">
              <select class="form-select" v-model="c.cropType" style="flex:1">
                <option v-for="ct in CROP_TYPES" :key="ct" :value="ct">{{ ct }}</option>
              </select>
              <input class="form-input" v-model.number="c.percentage" type="number"
                     min="1" max="100" style="width:64px" placeholder="%" />
              <button class="btn-icon" @click="cur.crops.splice(i,1)" :disabled="cur.crops.length === 1">✕</button>
            </div>
            <button class="btn-add-crop" @click="cur.crops.push({ cropType: 'OilPalm', percentage: 0 })">
              + Add crop
            </button>
            <div v-if="cropTotal !== 100 && cur.crops.length > 0" class="crop-warning">
              Total: {{ cropTotal }}% — must equal 100%
            </div>
          </div>

          <div class="form-actions">
            <button class="btn-add-section" :disabled="!canAdd" @click="addSection">
              + Add this plot
            </button>
          </div>
        </div>

      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted, nextTick } from 'vue'
import { useRouter } from 'vue-router'
import { useAgroStore } from '@/stores/agro'
import { sectionApi } from '@/services/api'

const router = useRouter()
const store  = useAgroStore()

// ── Constants ──
const COLORS = ['#2d7a4f', '#3daa6a', '#e8a838', '#e64a19', '#5b4fcf', '#c2185b', '#0288d1', '#00796b']
const CORNER_NAMES  = ['Corner 1 (NW)', 'Corner 2 (NE)', 'Corner 3 (SE)', 'Corner 4 (SW)']
const CORNER_COLORS = ['#3daa6a', '#e8a838', '#e64a19', '#5b4fcf']
const CROP_TYPES = ['OilPalm', 'Maize', 'Cassava', 'Cocoa', 'Soybean', 'Sorghum', 'Rice', 'Groundnut', 'Yam', 'Vegetables', 'Plantain', 'Tomato', 'Pepper']

// ── Boundary method ──
type BoundaryMethod = 'map' | 'corners' | 'survey'
const boundaryMethod = ref<BoundaryMethod>('map')

// 4 corner inputs (for 'corners' and 'survey' methods)
const corners = ref([
  { lat: null as number | null, lng: null as number | null },
  { lat: null as number | null, lng: null as number | null },
  { lat: null as number | null, lng: null as number | null },
  { lat: null as number | null, lng: null as number | null },
])
const cornersPreviewDone = ref(false)

const cornersValid = computed(() =>
  corners.value.every(c => c.lat !== null && c.lng !== null && !isNaN(c.lat) && !isNaN(c.lng))
)

function clearCornersPreview() { cornersPreviewDone.value = false }

function setMethod(m: BoundaryMethod) {
  boundaryMethod.value = m
  clearPolygon()
  cornersPreviewDone.value = false
  if (m !== 'map') clickPoints = []
}

// ── Section draft ──
interface SectionDraft {
  name: string
  areaValue: number
  areaUnit: 'Hectares' | 'Acres'
  crops: { cropType: string; percentage: number }[]
  polygonPoints: [number, number][]
  boundaryGeoJson: string | null
}

const sections     = ref<SectionDraft[]>([])
const activeSection = ref<number | null>(null)
const saveError    = ref('')
const saving       = ref(false)
const gpsLoading   = ref(false)

const cur = ref<SectionDraft>({
  name: '', areaValue: 10, areaUnit: 'Hectares',
  crops: [{ cropType: 'OilPalm', percentage: 100 }],
  polygonPoints: [], boundaryGeoJson: null,
})

const cropTotal = computed(() =>
  cur.value.crops.reduce((s, c) => s + (c.percentage || 0), 0)
)

const mapProgressLabel = computed(() => {
  const n = cur.value.polygonPoints.length
  if (n === 0) return 'Click first corner'
  if (n === 1) return 'Click second corner'
  if (n === 2) return 'Click third corner'
  if (n === 3) return 'Click fourth corner'
  return 'Boundary closed ✓'
})

const canAdd = computed(() => {
  const hasBoundary = cur.value.boundaryGeoJson !== null ||
    (boundaryMethod.value !== 'map' && cornersValid.value && cornersPreviewDone.value) ||
    cur.value.polygonPoints.length >= 3
  return cur.value.name.trim().length > 0 && cur.value.areaValue > 0 && cropTotal.value === 100 && hasBoundary
})

// ── Leaflet ──
let L: any = null
let map: any = null
let drawLayer: any = null
let cornersLayer: any = null
let sectionPolygons: any[] = []
let clickPoints: [number, number][] = []
let tempMarkers: any[] = []
let previewPolygon: any = null

async function initMap() {
  await nextTick()
  const el = document.getElementById('sections-map')
  if (!el || map) return

  L = (await import('leaflet')).default
  await import('leaflet/dist/leaflet.css')

  delete (L.Icon.Default.prototype as any)._getIconUrl
  L.Icon.Default.mergeOptions({
    iconUrl:       'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.9.4/images/marker-icon.png',
    iconRetinaUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.9.4/images/marker-icon-2x.png',
    shadowUrl:     'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.9.4/images/marker-shadow.png',
  })

  const farmLat = store.farm?.centroidLat ?? 7.3775
  const farmLng = store.farm?.centroidLng ?? 3.9470

  map = L.map('sections-map', { zoomControl: true }).setView([farmLat, farmLng], 13)
  L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
    attribution: '© OpenStreetMap contributors',
    maxZoom: 19,
  }).addTo(map)

  drawLayer   = L.layerGroup().addTo(map)
  cornersLayer = L.layerGroup().addTo(map)

  map.on('click', (e: any) => {
    if (boundaryMethod.value !== 'map') return
    if (clickPoints.length >= 4) return // already have 4

    const pt: [number, number] = [e.latlng.lat, e.latlng.lng]
    clickPoints.push(pt)
    cur.value.polygonPoints = [...clickPoints]

    // Corner marker
    const marker = L.circleMarker(pt, {
      radius: 7,
      fillColor: CORNER_COLORS[clickPoints.length - 1],
      color: '#fff',
      weight: 2,
      fillOpacity: 1,
    })
      .bindTooltip(CORNER_NAMES[clickPoints.length - 1], { permanent: false })
      .addTo(drawLayer)
    tempMarkers.push(marker)

    // Auto-draw polygon after 4 clicks
    if (clickPoints.length === 4) {
      closeMapPolygon()
    }
  })
}

function closeMapPolygon() {
  if (clickPoints.length < 3 || !L) return
  const ring = [...clickPoints, clickPoints[0]]
  cur.value.boundaryGeoJson = JSON.stringify({
    type: 'Polygon',
    coordinates: [ring.map(([lat, lng]) => [lng, lat])],
  })
  if (previewPolygon) drawLayer.removeLayer(previewPolygon)
  previewPolygon = L.polygon(clickPoints, { color: '#3daa6a', fillOpacity: 0.15, weight: 2 }).addTo(drawLayer)
}

function clearPolygon() {
  clickPoints = []
  cur.value.polygonPoints = []
  cur.value.boundaryGeoJson = null
  if (drawLayer) drawLayer.clearLayers()
  tempMarkers = []
  previewPolygon = null
}

// ── Corners preview ──
function buildGeoJsonFromCorners(): string {
  const pts = corners.value.map(c => [c.lat as number, c.lng as number] as [number, number])
  const ring = [...pts, pts[0]]
  return JSON.stringify({
    type: 'Polygon',
    coordinates: [ring.map(([lat, lng]) => [lng, lat])],
  })
}

function previewCorners() {
  if (!cornersValid.value || !map || !L) return

  cornersLayer.clearLayers()

  const pts = corners.value.map(c => [c.lat as number, c.lng as number] as [number, number])

  // Draw corner markers
  pts.forEach((pt, i) => {
    L.circleMarker(pt, {
      radius: 7,
      fillColor: CORNER_COLORS[i],
      color: '#fff',
      weight: 2,
      fillOpacity: 1,
    })
      .bindTooltip(CORNER_NAMES[i], { permanent: true, className: 'corner-tip' })
      .addTo(cornersLayer)
  })

  // Draw polygon
  L.polygon(pts, { color: '#3daa6a', fillOpacity: 0.15, weight: 2 }).addTo(cornersLayer)

  // Fit map to bounds
  map.fitBounds(L.latLngBounds(pts), { padding: [40, 40] })

  cur.value.boundaryGeoJson = buildGeoJsonFromCorners()
  cornersPreviewDone.value = true
}

// ── GPS assist: centre map on device location ──
async function fillCornerFromGps() {
  if (!navigator.geolocation) return
  gpsLoading.value = true
  try {
    const pos = await new Promise<GeolocationPosition>((res, rej) =>
      navigator.geolocation.getCurrentPosition(res, rej, { timeout: 12000, enableHighAccuracy: true })
    )
    if (map) map.setView([pos.coords.latitude, pos.coords.longitude], 16)
  } catch { /* silent */ }
  finally { gpsLoading.value = false }
}

// ── Section management ──
function addSection() {
  if (!canAdd.value) return

  // Resolve boundary from map clicks if not yet closed
  if (boundaryMethod.value === 'map' && clickPoints.length >= 3 && !cur.value.boundaryGeoJson) {
    closeMapPolygon()
  }
  if ((boundaryMethod.value === 'corners' || boundaryMethod.value === 'survey') && cornersValid.value) {
    cur.value.boundaryGeoJson = buildGeoJsonFromCorners()
  }

  const draft: SectionDraft = {
    ...cur.value,
    crops: cur.value.crops.map(c => ({ ...c })),
    polygonPoints: [...cur.value.polygonPoints],
  }

  sections.value.push(draft)
  const ci = sections.value.length - 1

  // Keep polygon on map in section colour
  const pts = clickPoints.length >= 3
    ? [...clickPoints]
    : corners.value.filter(c => c.lat && c.lng).map(c => [c.lat!, c.lng!] as [number, number])

  if (pts.length >= 3 && map && L) {
    const poly = L.polygon(pts, {
      color: COLORS[ci % COLORS.length],
      fillOpacity: 0.18,
      weight: 2,
    })
      .bindTooltip(draft.name, { permanent: true, direction: 'center', className: 'section-tooltip' })
      .addTo(map)
    sectionPolygons.push(poly)
  } else {
    sectionPolygons.push(null)
  }

  resetForm()
}

function removeSection(i: number) {
  sections.value.splice(i, 1)
  if (sectionPolygons[i]) { map?.removeLayer(sectionPolygons[i]) }
  sectionPolygons.splice(i, 1)
}

function focusSection(i: number) {
  activeSection.value = i
  if (sectionPolygons[i] && map) {
    map.fitBounds(sectionPolygons[i].getBounds(), { padding: [30, 30] })
  }
}

function resetForm() {
  clearPolygon()
  cornersPreviewDone.value = false
  corners.value = Array.from({ length: 4 }, () => ({ lat: null, lng: null }))
  if (cornersLayer) cornersLayer.clearLayers()
  cur.value = {
    name: '', areaValue: 10, areaUnit: 'Hectares',
    crops: [{ cropType: 'OilPalm', percentage: 100 }],
    polygonPoints: [], boundaryGeoJson: null,
  }
}

// ── Finish ──
async function goToDashboard() {
  if (!sections.value.length) {
    router.push({ name: 'dashboard' })
    return
  }
  saving.value = true
  saveError.value = ''
  try {
    for (const s of sections.value) {
      await sectionApi.create({
        name: s.name,
        crops: s.crops.map(c => ({ cropType: c.cropType, hectares: 0, percentage: c.percentage })),
        areaValue: s.areaValue,
        areaUnit: s.areaUnit,
        boundaryGeoJson: s.boundaryGeoJson,
      })
    }
    router.push({ name: 'dashboard' })
  } catch (e: any) {
    saveError.value = e?.response?.data?.message ?? 'Failed to save sections.'
    saving.value = false
  }
}

onMounted(() => { initMap() })
onUnmounted(() => { if (map) { map.remove(); map = null } })
</script>

<style scoped>
.sections-shell {
  display: flex;
  height: 100vh;
  overflow: hidden;
  background: #0d2b1f;
}

/* ── Left panel ── */
.sections-panel {
  width: 280px;
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
.method-dot--map     { background: #3daa6a; }
.method-dot--coords  { background: #e8a838; }
.method-dot--survey  { background: #5b4fcf; }
.method-legend-title { font-size: 11px; font-weight: 600; color: rgba(255,255,255,0.8); }
.method-legend-sub   { font-size: 10px; color: rgba(255,255,255,0.35); line-height: 1.4; margin-top: 1px; }

.sections-list { margin-bottom: 14px; }
.sections-list-label { font-family: 'JetBrains Mono', monospace; font-size: 9px; letter-spacing: 0.1em; text-transform: uppercase; color: rgba(255,255,255,0.3); margin-bottom: 6px; }
.section-item { display: flex; align-items: center; gap: 8px; padding: 7px 8px; border-radius: 8px; cursor: pointer; transition: background 0.15s; margin-bottom: 3px; }
.section-item:hover { background: rgba(255,255,255,0.05); }
.section-item--active { background: rgba(61,170,106,0.12); }
.section-dot { width: 9px; height: 9px; border-radius: 50%; flex-shrink: 0; }
.section-item-body { flex: 1; min-width: 0; }
.section-item-name { font-size: 11px; font-weight: 600; color: #fff; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
.section-item-meta { font-family: 'JetBrains Mono', monospace; font-size: 8.5px; color: rgba(255,255,255,0.35); margin-top: 2px; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
.has-boundary { color: #3daa6a; }
.section-remove { background: none; border: none; color: rgba(255,255,255,0.3); cursor: pointer; font-size: 13px; padding: 2px 3px; flex-shrink: 0; }
.section-remove:hover { color: rgba(255,255,255,0.7); }

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
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 6px;
}
.btn-finish:hover:not(:disabled) { background: #2d7a4f; }
.btn-finish:disabled { opacity: 0.6; cursor: not-allowed; }
.panel-err { font-size: 10px; color: #ef4444; margin-top: 6px; text-align: center; }

.spinner-sm {
  width: 12px; height: 12px;
  border: 2px solid rgba(255,255,255,0.3);
  border-top-color: #fff;
  border-radius: 50%;
  animation: spin 0.7s linear infinite;
  flex-shrink: 0;
}

/* ── Right: map + form ── */
.sections-main { flex: 1; display: flex; flex-direction: column; overflow: hidden; }
.sections-map { flex: 1; min-height: 0; }

.section-form-card {
  background: #fff;
  border-top: 1px solid #eee;
  overflow-y: auto;
  max-height: 58vh;
}

/* Boundary method tabs */
.boundary-tabs {
  display: flex;
  border-bottom: 1px solid #eee;
  background: #fafafa;
}
.btab {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 6px;
  padding: 10px 8px;
  font-size: 11px;
  font-family: 'JetBrains Mono', monospace;
  color: #9ca3af;
  background: none;
  border: none;
  border-bottom: 2px solid transparent;
  cursor: pointer;
  transition: all 0.15s;
}
.btab:hover { color: #374151; }
.btab--active { color: #2d7a4f; border-bottom-color: #2d7a4f; background: #fff; font-weight: 600; }

/* Method body */
.method-body { padding: 12px 16px; border-bottom: 1px solid #eee; }

.survey-hint {
  display: flex;
  align-items: center;
  gap: 7px;
  font-size: 11px;
  color: #6b7280;
  background: rgba(91,79,207,0.07);
  border: 1px solid rgba(91,79,207,0.18);
  border-radius: 8px;
  padding: 8px 12px;
  margin-bottom: 12px;
}

/* Map progress */
.map-progress { display: flex; align-items: center; gap: 10px; margin-bottom: 8px; }
.progress-track { display: flex; align-items: center; gap: 6px; }
.progress-dot {
  width: 22px; height: 22px;
  border-radius: 50%;
  border: 2px solid #e5e7eb;
  display: flex; align-items: center; justify-content: center;
  font-family: 'JetBrains Mono', monospace;
  font-size: 9px; font-weight: 700;
  color: #9ca3af;
  transition: all 0.15s;
}
.progress-dot--done { border-color: #2d7a4f; background: #2d7a4f; color: #fff; }
.progress-dot--active { border-color: #e8a838; color: #e8a838; }
.progress-label { font-size: 11px; color: #6b7280; font-family: 'JetBrains Mono', monospace; flex: 1; }
.btn-clear-sm { font-size: 10px; color: #9ca3af; background: none; border: none; cursor: pointer; text-decoration: underline; }
.btn-clear-sm:hover { color: #ef4444; }
.map-hint { font-size: 11px; color: #9ca3af; }

/* Corners grid */
.corners-grid { display: flex; flex-direction: column; gap: 7px; margin-bottom: 10px; }
.corner-row { display: flex; align-items: center; gap: 8px; }
.corner-label-wrap { display: flex; align-items: center; gap: 6px; width: 110px; flex-shrink: 0; }
.corner-dot { width: 9px; height: 9px; border-radius: 50%; flex-shrink: 0; }
.corner-label { font-family: 'JetBrains Mono', monospace; font-size: 9px; color: #6b7280; }
.corner-input { flex: 1; font-size: 11px !important; padding: 6px 8px !important; }

.corners-actions { display: flex; align-items: center; gap: 10px; margin-bottom: 6px; }
.btn-preview {
  display: flex; align-items: center; gap: 6px;
  padding: 7px 14px;
  background: #f3f4f6;
  border: 1px solid #e5e7eb;
  border-radius: 8px;
  font-size: 11px;
  color: #374151;
  cursor: pointer;
  transition: all 0.15s;
}
.btn-preview:hover:not(:disabled) { background: #e5e7eb; }
.btn-preview:disabled { opacity: 0.45; cursor: not-allowed; }
.corners-ok { display: flex; align-items: center; gap: 5px; font-size: 11px; color: #2d7a4f; font-weight: 600; }
.corners-hint-txt { font-size: 10px; color: #9ca3af; }

.gps-assist { margin-top: 8px; }
.btn-gps-assist {
  display: flex; align-items: center; gap: 6px;
  font-size: 11px; color: #2d7a4f;
  background: rgba(45,122,79,0.06);
  border: 1px solid rgba(45,122,79,0.18);
  border-radius: 8px;
  padding: 6px 12px;
  cursor: pointer;
}
.btn-gps-assist:hover:not(:disabled) { background: rgba(45,122,79,0.1); }
.gps-spinner-sm {
  width: 10px; height: 10px;
  border: 1.5px solid rgba(45,122,79,0.3);
  border-top-color: #2d7a4f;
  border-radius: 50%;
  animation: spin 0.7s linear infinite;
}

/* Section details */
.section-details { padding: 12px 16px 16px; }
.details-divider {
  display: flex; align-items: center; gap: 8px;
  font-family: 'JetBrains Mono', monospace;
  font-size: 9px; text-transform: uppercase; letter-spacing: 0.1em;
  color: #9ca3af;
  margin-bottom: 12px;
}
.details-divider::before, .details-divider::after { content: ''; flex: 1; height: 1px; background: #f3f4f6; }

.form-row { display: flex; gap: 10px; margin-bottom: 10px; }
.form-group { display: flex; flex-direction: column; }
.form-label { font-family: 'JetBrains Mono', monospace; font-size: 9px; letter-spacing: 0.1em; text-transform: uppercase; color: #6b7280; margin-bottom: 4px; }
.label-hint { font-weight: 400; color: #d1d5db; text-transform: none; letter-spacing: 0; }
.form-input {
  padding: 7px 10px;
  border: 1px solid #e5e7eb;
  border-radius: 8px;
  font-size: 12px; color: #111827; background: #fff;
  outline: none; transition: border-color 0.15s;
  font-family: 'Inter', sans-serif;
}
.form-input:focus { border-color: #2d7a4f; }
.form-select {
  padding: 7px 10px;
  border: 1px solid #e5e7eb;
  border-radius: 8px;
  font-size: 12px; color: #111827; background: #fff;
  cursor: pointer; outline: none;
}

.crop-row { display: flex; gap: 8px; align-items: center; margin-bottom: 6px; }
.btn-icon { background: none; border: none; color: #9ca3af; cursor: pointer; font-size: 12px; padding: 4px; }
.btn-icon:hover:not(:disabled) { color: #ef4444; }
.btn-icon:disabled { opacity: 0.4; cursor: not-allowed; }
.btn-add-crop {
  width: 100%; padding: 5px 10px; margin-top: 4px;
  background: none; border: 1px dashed #e5e7eb;
  border-radius: 8px; font-size: 11px; color: #9ca3af;
  cursor: pointer; text-align: center;
}
.btn-add-crop:hover { border-color: #2d7a4f; color: #2d7a4f; }
.crop-warning { font-size: 10px; color: #e64a19; margin-top: 4px; font-family: 'JetBrains Mono', monospace; }

.form-actions { display: flex; justify-content: flex-end; margin-top: 12px; }
.btn-add-section {
  padding: 10px 20px;
  background: #2d7a4f; color: #fff; border: none;
  border-radius: 10px; font-size: 12px; font-weight: 600;
  cursor: pointer; transition: background 0.15s;
}
.btn-add-section:hover:not(:disabled) { background: #1e5c3a; }
.btn-add-section:disabled { opacity: 0.45; cursor: not-allowed; }

@keyframes spin { to { transform: rotate(360deg); } }

:global(.section-tooltip) {
  background: rgba(13,43,31,0.85) !important; border: none !important;
  color: #fff !important; font-size: 10px !important;
  font-family: 'JetBrains Mono', monospace !important;
  padding: 3px 8px !important; border-radius: 6px !important;
}
:global(.corner-tip) {
  background: rgba(45,122,79,0.9) !important; border: none !important;
  color: #fff !important; font-size: 9px !important;
  font-family: 'JetBrains Mono', monospace !important;
  padding: 2px 7px !important; border-radius: 5px !important;
}
</style>
