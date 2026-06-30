<template>
  <div class="onboard-shell">
    <div class="onboard-card">
      <!-- Header -->
      <div class="onboard-header">
        <div class="onboard-logo">🌿 AgroMesh</div>
        <div class="onboard-subtitle">Farm Setup Wizard</div>
      </div>

      <!-- Step indicator -->
      <div class="step-bar">
        <div
          v-for="(s, i) in steps" :key="s"
          class="step-item"
          :class="{ active: step === i, done: step > i }"
        >
          <div class="step-dot">{{ step > i ? '✓' : i + 1 }}</div>
          <div class="step-name">{{ s }}</div>
        </div>
      </div>

      <!-- ── STEP 0: Farm Profile ── -->
      <div v-if="step === 0" class="onboard-body">
        <div class="step-title">Tell us about your farm</div>
        <div class="form-group">
          <label class="form-label">Farm Name</label>
          <input class="form-input" v-model="farm.name" placeholder="e.g. Adeyemi Oil Palm Plantation" />
        </div>
        <div class="form-group">
          <label class="form-label">Location</label>
          <input class="form-input" v-model="farm.location" placeholder="e.g. Oyo State, Nigeria" />
        </div>
        <div class="form-row">
          <div class="form-group" style="flex:1">
            <label class="form-label">Total Area</label>
            <input class="form-input" v-model.number="farm.areaValue" type="number" min="0.1" step="0.5" placeholder="45" />
          </div>
          <div class="form-group" style="width:120px">
            <label class="form-label">Unit</label>
            <select class="form-select" v-model="farm.areaUnit">
              <option value="Hectares">Hectares</option>
              <option value="Acres">Acres</option>
            </select>
          </div>
        </div>
        <div class="form-group">
          <label class="form-label">Farm Coordinates (optional — used for weather)</label>
          <div class="form-row">
            <input class="form-input" v-model.number="farm.lat" type="number" step="0.001" placeholder="Latitude e.g. 7.3775" style="flex:1" />
            <input class="form-input" v-model.number="farm.lng" type="number" step="0.001" placeholder="Longitude e.g. 3.9470" style="flex:1" />
          </div>
        </div>
        <div class="hint-box">
          💡 Coordinates enable live weather forecasts and spray-window predictions powered by Open-Meteo. You can add these later from Farm Settings.
        </div>
      </div>

      <!-- ── STEP 1: Sections + Map ── -->
      <div v-if="step === 1" class="onboard-body">
        <div class="step-title">Define your farm sections</div>
        <div class="hint-box" style="margin-bottom:12px;">
          Click on the map to draw a polygon boundary for each section. Double-click or click the first point to close.
        </div>

        <!-- Map -->
        <div id="onboarding-map" class="map-container"></div>

        <!-- Section form -->
        <div class="section-form">
          <div class="form-row">
            <div class="form-group" style="flex:1">
              <label class="form-label">Section Name</label>
              <input class="form-input" v-model="curSection.name" placeholder="e.g. Section A" />
            </div>
            <div class="form-group" style="width:120px">
              <label class="form-label">Area</label>
              <input class="form-input" v-model.number="curSection.areaValue" type="number" min="0.1" step="0.5" />
            </div>
            <div class="form-group" style="width:90px">
              <label class="form-label">Unit</label>
              <select class="form-select" v-model="curSection.areaUnit">
                <option value="Hectares">ha</option>
                <option value="Acres">ac</option>
              </select>
            </div>
          </div>

          <!-- Crops -->
          <div class="form-group">
            <label class="form-label">Crops (% must total 100)</label>
            <div v-for="(c, i) in curSection.crops" :key="i" class="crop-row">
              <select class="form-select" v-model="c.cropType" style="flex:1">
                <option v-for="ct in cropTypes" :key="ct" :value="ct">{{ ct }}</option>
              </select>
              <input class="form-input" v-model.number="c.percentage" type="number" min="1" max="100" style="width:70px" placeholder="%" />
              <button class="btn btn-ghost btn-sm" @click="curSection.crops.splice(i,1)" :disabled="curSection.crops.length === 1">✕</button>
            </div>
            <button class="btn btn-ghost btn-sm" @click="curSection.crops.push({ cropType: 'OilPalm', percentage: 0 })" style="margin-top:6px;">
              + Add Crop
            </button>
          </div>

          <div class="map-coords-hint" v-if="curSection.polygonPoints.length">
            {{ curSection.polygonPoints.length }} points drawn
            <button class="btn btn-ghost btn-sm" @click="clearPolygon">Clear</button>
          </div>

          <button class="btn btn-primary" @click="addSection" :disabled="!canAddSection" style="width:100%;margin-top:8px;">
            + Add Section
          </button>
        </div>

        <!-- Added sections list -->
        <div v-if="sections.length" class="added-sections">
          <div class="form-label" style="margin-bottom:8px;">Added sections ({{ sections.length }})</div>
          <div v-for="(s, i) in sections" :key="i" class="added-section-row">
            <div class="section-dot" :style="{ background: sectionColor(i) }"></div>
            <div style="flex:1">
              <div class="added-section-name">{{ s.name }}</div>
              <div class="added-section-meta">{{ s.crops.map(c => `${c.cropType} ${c.percentage}%`).join(' · ') }} · {{ s.areaValue }} {{ s.areaUnit === 'Hectares' ? 'ha' : 'ac' }}</div>
            </div>
            <button class="btn btn-ghost btn-sm" @click="sections.splice(i,1)">✕</button>
          </div>
        </div>
      </div>

      <!-- ── STEP 2: Worker Roles ── -->
      <div v-if="step === 2" class="onboard-body">
        <div class="step-title">Define worker roles &amp; daily rates</div>
        <div class="hint-box">Rates are set per role. All workers assigned to a role share its rate — change it once to update everyone.</div>
        <div class="roles-list">
          <div v-for="(r, i) in roles" :key="i" class="role-input-row">
            <input class="form-input" v-model="r.title" placeholder="Role title e.g. Farm Worker" style="flex:2" />
            <select class="form-select" v-model="r.workerType" style="width:130px">
              <option value="Employee">Employee</option>
              <option value="Contractor">Contractor</option>
            </select>
            <div class="rate-input-wrap">
              <span class="rate-prefix">₦</span>
              <input class="form-input rate-input" v-model.number="r.dailyRate" type="number" min="500" step="100" placeholder="2200" />
              <span class="rate-suffix">/day</span>
            </div>
            <button class="btn btn-ghost btn-sm" @click="roles.splice(i,1)" :disabled="roles.length === 1">✕</button>
          </div>
        </div>
        <button class="btn btn-ghost" @click="roles.push({ title: '', workerType: 'Employee', dailyRate: 2200 })" style="margin-top:8px;">
          + Add Role
        </button>
      </div>

      <!-- ── STEP 3: Workers ── -->
      <div v-if="step === 3" class="onboard-body">
        <div class="step-title">Add your workers</div>
        <div class="workers-list">
          <div v-for="(w, i) in workers" :key="i" class="worker-input-row">
            <input class="form-input" v-model="w.name" placeholder="Full name" style="flex:2" />
            <input class="form-input" v-model="w.initials" placeholder="Initials" style="width:72px;text-transform:uppercase" maxlength="3" />
            <select class="form-select" v-model="w.roleIndex" style="flex:2">
              <option v-for="(r, ri) in roles" :key="ri" :value="ri">{{ r.title }} (₦{{ r.dailyRate.toLocaleString() }}/day)</option>
            </select>
            <button class="btn btn-ghost btn-sm" @click="workers.splice(i,1)">✕</button>
          </div>
        </div>
        <button class="btn btn-ghost" @click="workers.push({ name: '', initials: '', roleIndex: 0 })" style="margin-top:8px;">
          + Add Worker
        </button>
        <div v-if="!workers.length" class="hint-box" style="margin-top:12px;">
          You can skip this step and add workers later from the Workers page.
        </div>
      </div>

      <!-- Footer nav -->
      <div class="onboard-footer">
        <button class="btn btn-ghost" @click="back" :disabled="step === 0 || saving">
          ← Back
        </button>
        <div style="display:flex;align-items:center;gap:12px;">
          <span style="font-family:'JetBrains Mono',monospace;font-size:9px;color:var(--stone);">Step {{ step + 1 }} of {{ steps.length }}</span>
          <button class="btn btn-primary" @click="next" :disabled="!canProceed || saving">
            {{ step === steps.length - 1 ? (saving ? 'Setting up…' : '✓ Launch Farm') : 'Continue →' }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted, nextTick, watch } from 'vue'
import { useRouter } from 'vue-router'
import { useAgroStore } from '@/stores/agro'
import { workerRoleApi, workerApi, sectionApi } from '@/services/api'

const router = useRouter()
const store = useAgroStore()

const step = ref(0)
const saving = ref(false)
const steps = ['Farm Profile', 'Sections', 'Worker Roles', 'Workers']

// ── Farm profile ──
const farm = ref({
  name: '',
  location: '',
  areaValue: 45,
  areaUnit: 'Acres' as 'Hectares' | 'Acres',
  lat: null as number | null,
  lng: null as number | null,
})

// ── Sections ──
const SECTION_COLORS = ['#2d7a4f', '#3daa6a', '#8bc34a', '#ffb300', '#e64a19', '#7b1fa2']
function sectionColor(i: number) { return SECTION_COLORS[i % SECTION_COLORS.length] }

const cropTypes = ['OilPalm', 'Maize', 'Cassava', 'Cocoa', 'Soybean', 'Sorghum', 'Rice', 'Groundnut', 'Yam', 'Vegetables']

const curSection = ref({
  name: '',
  areaValue: 10,
  areaUnit: farm.value.areaUnit as 'Hectares' | 'Acres',
  crops: [{ cropType: 'OilPalm', percentage: 100 }],
  polygonPoints: [] as [number, number][],
  boundaryGeoJson: null as string | null,
})

const sections = ref<{
  name: string
  areaValue: number
  areaUnit: 'Hectares' | 'Acres'
  crops: { cropType: string; percentage: number }[]
  polygonPoints: [number, number][]
  boundaryGeoJson: string | null
}[]>([])

const canAddSection = computed(() => {
  const s = curSection.value
  const cropSum = s.crops.reduce((acc, c) => acc + (c.percentage || 0), 0)
  return s.name.trim() && s.areaValue > 0 && cropSum === 100
})

// ── Worker roles ──
const roles = ref([
  { title: 'Field Supervisor', workerType: 'Employee' as const, dailyRate: 3500 },
  { title: 'Farm Worker', workerType: 'Employee' as const, dailyRate: 2200 },
  { title: 'Contractor', workerType: 'Contractor' as const, dailyRate: 4000 },
])

// ── Workers ──
const workers = ref<{ name: string; initials: string; roleIndex: number }[]>([])

// ── Validation ──
const canProceed = computed(() => {
  if (step.value === 0) return farm.value.name.trim() && farm.value.location.trim() && farm.value.areaValue > 0
  if (step.value === 1) return sections.value.length > 0
  if (step.value === 2) return roles.value.every(r => r.title.trim() && r.dailyRate > 0)
  return true // workers optional
})

// ── Leaflet map ──
let leafletMap: any = null
let drawingLayer: any = null
let sectionLayers: any[] = []
let clickPoints: [number, number][] = []
let tempMarkers: any[] = []
let tempPolyline: any = null
let L: any = null

async function initMap() {
  await nextTick()
  const el = document.getElementById('onboarding-map')
  if (!el || leafletMap) return

  L = (await import('leaflet')).default
  await import('leaflet/dist/leaflet.css')

  // Fix default marker icons
  delete (L.Icon.Default.prototype as any)._getIconUrl
  L.Icon.Default.mergeOptions({
    iconRetinaUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.9.4/images/marker-icon-2x.png',
    iconUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.9.4/images/marker-icon.png',
    shadowUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.9.4/images/marker-shadow.png',
  })

  // Default center: Oyo State, Nigeria
  const lat = farm.value.lat ?? 7.3775
  const lng = farm.value.lng ?? 3.9470

  leafletMap = L.map('onboarding-map').setView([lat, lng], 14)
  L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
    attribution: '© OpenStreetMap contributors',
    maxZoom: 19
  }).addTo(leafletMap)

  drawingLayer = L.layerGroup().addTo(leafletMap)

  leafletMap.on('click', (e: any) => {
    clickPoints.push([e.latlng.lat, e.latlng.lng])
    curSection.value.polygonPoints = [...clickPoints]

    // Add marker
    const marker = L.circleMarker([e.latlng.lat, e.latlng.lng], {
      radius: 5, fillColor: '#2d7a4f', color: '#fff', weight: 2, fillOpacity: 1
    }).addTo(drawingLayer!)
    tempMarkers.push(marker)

    // Redraw polyline
    if (tempPolyline) drawingLayer!.removeLayer(tempPolyline)
    if (clickPoints.length > 1) {
      tempPolyline = L.polyline(clickPoints, { color: '#2d7a4f', weight: 2, dashArray: '4,4' }).addTo(drawingLayer!)
    }

    // Auto-close polygon on 3+ points if last click near first
    if (clickPoints.length >= 3) {
      const first = clickPoints[0]
      const dist = Math.hypot(e.latlng.lat - first[0], e.latlng.lng - first[1])
      if (dist < 0.0003) closePolygon()
    }
  })

  leafletMap.on('dblclick', (e: any) => {
    e.originalEvent.preventDefault()
    if (clickPoints.length >= 3) closePolygon()
  })
}

function closePolygon() {
  if (clickPoints.length < 3 || !drawingLayer || !L) return

  // Build GeoJSON from click points (close ring)
  const ring = [...clickPoints, clickPoints[0]]
  const geoJson = {
    type: 'Polygon',
    coordinates: [ring.map(([lat, lng]) => [lng, lat])]
  }
  curSection.value.boundaryGeoJson = JSON.stringify(geoJson)
}

function clearPolygon() {
  clickPoints = []
  curSection.value.polygonPoints = []
  curSection.value.boundaryGeoJson = null
  if (drawingLayer) {
    drawingLayer.clearLayers()
    tempMarkers = []
    tempPolyline = null
  }
}

function addSection() {
  if (!canAddSection.value) return

  // If we have click points but haven't closed the polygon, close it now
  if (clickPoints.length >= 3 && !curSection.value.boundaryGeoJson) {
    const ring = [...clickPoints, clickPoints[0]]
    curSection.value.boundaryGeoJson = JSON.stringify({
      type: 'Polygon',
      coordinates: [ring.map(([lat, lng]) => [lng, lat])]
    })
  }

  sections.value.push({ ...curSection.value, crops: [...curSection.value.crops.map(c => ({ ...c }))] })

  // Draw the closed polygon on the map
  if (clickPoints.length >= 3 && leafletMap && L) {
    const color = sectionColor(sections.value.length - 1)
    const poly = L.polygon([...clickPoints], { color, fillOpacity: 0.15, weight: 2 })
      .bindTooltip(curSection.value.name, { permanent: true, direction: 'center', className: 'map-section-label' })
      .addTo(leafletMap)
    sectionLayers.push(poly)
  }

  // Reset form
  clearPolygon()
  curSection.value = {
    name: '',
    areaValue: 10,
    areaUnit: farm.value.areaUnit,
    crops: [{ cropType: 'OilPalm', percentage: 100 }],
    polygonPoints: [],
    boundaryGeoJson: null,
  }
}

// ── Navigation ──
async function next() {
  if (step.value < steps.length - 1) {
    step.value++
    if (step.value === 1) {
      await nextTick()
      await initMap()
    }
    return
  }
  await submit()
}

function back() {
  if (step.value > 0) step.value--
}

// ── Submit ──
async function submit() {
  saving.value = true
  try {
    // 1. Create farm
    await store.createFarm({
      name: farm.value.name,
      location: farm.value.location,
      areaValue: farm.value.areaValue,
      areaUnit: farm.value.areaUnit,
      ownerId: 'owner_001',
    })

    // 2. Set coordinates if provided
    if (farm.value.lat && farm.value.lng) {
      // POST to set coordinates (handled by update endpoint or create already stores them)
      // For MVP, coordinates can be set via a separate PATCH call if available
    }

    // 3. Create sections
    for (const s of sections.value) {
      await sectionApi.create({
        name: s.name,
        crops: s.crops.map(c => ({ cropType: c.cropType, hectares: 0, percentage: c.percentage })),
        areaValue: s.areaValue,
        areaUnit: s.areaUnit,
        boundaryGeoJson: s.boundaryGeoJson,
      })
    }

    // 4. Create worker roles
    const createdRoles: { id: string; title: string }[] = []
    for (const r of roles.value) {
      const res = await workerRoleApi.create({
        title: r.title,
        workerType: r.workerType,
        dailyRate: r.dailyRate,
      })
      createdRoles.push({ id: res.data.id, title: r.title })
    }

    // 5. Create workers
    for (const w of workers.value) {
      const role = createdRoles[w.roleIndex]
      if (!role) continue
      await workerApi.add({
        name: w.name,
        initials: w.initials.toUpperCase(),
        workerRoleId: role.id,
        workerType: roles.value[w.roleIndex].workerType,
      })
    }

    // Done — navigate to dashboard
    router.push('/')
  } catch (e) {
    console.error('Onboarding failed', e)
    saving.value = false
  }
}

onMounted(async () => {
  // If a farm already exists, go straight to dashboard
  const hasFarm = await store.discoverFarm()
  if (hasFarm) router.replace('/')
})

onUnmounted(() => {
  if (leafletMap) {
    leafletMap.remove()
    leafletMap = null
  }
})

// Sync section unit with farm unit preference
watch(() => farm.value.areaUnit, (unit) => {
  curSection.value.areaUnit = unit
})
</script>

<style scoped>
.onboard-shell {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, #0d2b1f 0%, #1a4a33 100%);
  padding: 24px;
}

.onboard-card {
  background: white;
  border-radius: 20px;
  width: 100%;
  max-width: 680px;
  box-shadow: 0 32px 80px rgba(0,0,0,0.4);
  overflow: hidden;
}

.onboard-header {
  background: linear-gradient(135deg, #0d2b1f 0%, #2d7a4f 100%);
  padding: 24px 32px 20px;
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.onboard-logo {
  font-family: 'Syne', sans-serif;
  font-weight: 700;
  font-size: 20px;
  color: #fff;
  letter-spacing: -0.01em;
}

.onboard-subtitle {
  font-family: 'JetBrains Mono', monospace;
  font-size: 9px;
  letter-spacing: 0.12em;
  text-transform: uppercase;
  color: rgba(255,255,255,0.6);
}

.step-bar {
  display: flex;
  padding: 16px 24px;
  border-bottom: 1px solid var(--fog);
  gap: 0;
}

.step-item {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  position: relative;
}

.step-item::after {
  content: '';
  position: absolute;
  top: 12px;
  left: 50%;
  width: 100%;
  height: 1px;
  background: var(--fog);
}

.step-item:last-child::after { display: none; }

.step-dot {
  width: 24px;
  height: 24px;
  border-radius: 50%;
  border: 2px solid var(--fog);
  display: flex;
  align-items: center;
  justify-content: center;
  font-family: 'JetBrains Mono', monospace;
  font-size: 10px;
  font-weight: 700;
  color: var(--stone);
  background: white;
  position: relative;
  z-index: 1;
  transition: all 0.2s;
}

.step-item.active .step-dot { border-color: var(--leaf); color: var(--leaf); }
.step-item.done .step-dot { background: var(--palm); border-color: var(--palm); color: white; }

.step-name {
  font-family: 'JetBrains Mono', monospace;
  font-size: 8px;
  letter-spacing: 0.1em;
  text-transform: uppercase;
  color: var(--stone);
  margin-top: 4px;
  text-align: center;
}

.step-item.active .step-name { color: var(--leaf); }

.onboard-body {
  padding: 24px 32px;
  min-height: 240px;
}

.step-title {
  font-family: 'Syne', sans-serif;
  font-size: 18px;
  font-weight: 700;
  color: var(--forest);
  letter-spacing: -0.01em;
  margin-bottom: 20px;
}

.form-row { display: flex; gap: 12px; }
.hint-box {
  background: rgba(45,122,79,0.06);
  border: 1px solid rgba(45,122,79,0.15);
  border-radius: 8px;
  padding: 10px 14px;
  font-size: 11px;
  color: var(--stone);
  line-height: 1.6;
  margin-top: 12px;
}

/* Map */
.map-container {
  height: 280px;
  border-radius: 10px;
  overflow: hidden;
  border: 1px solid var(--fog);
  margin-bottom: 16px;
}

.section-form { background: rgba(45,122,79,0.03); border: 1px solid var(--fog); border-radius: 10px; padding: 14px; margin-bottom: 14px; }
.crop-row { display: flex; gap: 8px; align-items: center; margin-bottom: 6px; }
.map-coords-hint { font-family: 'JetBrains Mono', monospace; font-size: 9px; color: var(--stone); display: flex; align-items: center; gap: 8px; margin-bottom: 6px; }

.added-sections { }
.added-section-row { display: flex; align-items: center; gap: 10px; padding: 8px 0; border-bottom: 1px solid var(--fog); }
.added-section-row:last-child { border-bottom: none; }
.section-dot { width: 10px; height: 10px; border-radius: 50%; flex-shrink: 0; }
.added-section-name { font-size: 12px; font-weight: 600; color: var(--forest); }
.added-section-meta { font-family: 'JetBrains Mono', monospace; font-size: 9px; color: var(--stone); margin-top: 1px; }

/* Roles */
.roles-list { display: flex; flex-direction: column; gap: 8px; margin-top: 12px; }
.role-input-row { display: flex; gap: 8px; align-items: center; }
.rate-input-wrap { display: flex; align-items: center; gap: 4px; width: 140px; }
.rate-prefix { font-family: 'JetBrains Mono', monospace; font-size: 12px; color: var(--stone); }
.rate-input { flex: 1; width: 80px !important; text-align: right; }
.rate-suffix { font-family: 'JetBrains Mono', monospace; font-size: 9px; color: var(--stone); white-space: nowrap; }

/* Workers */
.workers-list { display: flex; flex-direction: column; gap: 8px; margin-top: 12px; }
.worker-input-row { display: flex; gap: 8px; align-items: center; }

.onboard-footer {
  padding: 16px 32px;
  border-top: 1px solid var(--fog);
  display: flex;
  justify-content: space-between;
  align-items: center;
}
</style>
