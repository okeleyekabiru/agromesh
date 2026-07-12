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
        <div class="sections-list" v-if="addedSections.length">
          <div class="sections-list-label">Added plots ({{ addedSections.length }})</div>
          <div v-for="(s, i) in addedSections" :key="s.id" class="section-item">
            <div class="section-dot" :style="{ background: COLORS[i % COLORS.length] }" />
            <div class="section-item-body">
              <div class="section-item-name">{{ s.name }}</div>
              <div class="section-item-meta">
                {{ s.boundaryHectares ? s.boundaryHectares.toFixed(2) : s.areaValue }} ha
                <span class="boundary-src-badge" :class="`src-${s.boundarySource}`">
                  {{ BOUNDARY_SRC_LABEL[s.boundarySource] }}
                </span>
              </div>
            </div>
          </div>
        </div>

        <div v-if="pending" class="pending-notice">
          <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="12" cy="12" r="10"/><line x1="12" y1="8" x2="12" y2="12"/><line x1="12" y1="16" x2="12.01" y2="16"/></svg>
          Finish or skip the boundary for <strong>{{ pending.name }}</strong> to continue.
        </div>

        <div class="panel-actions">
          <button class="btn-finish" @click="goToReview" :disabled="!!pending">
            {{ addedSections.length ? 'Continue to Review →' : 'Skip for now →' }}
          </button>
        </div>
      </div>
    </div>

    <!-- ── Right: main work area ── -->
    <div class="sections-main">

      <!-- ══ Plot details form (no plot pending boundary capture) ══ -->
      <div v-if="!pending" class="work-card">
        <div class="work-card-head">
          <div class="work-card-title">New plot</div>
          <div class="work-card-sub">Give it a name, area and crop mix — you'll mark its boundary next.</div>
        </div>

        <div class="form-row">
          <div class="form-group" style="flex:2">
            <label class="form-label">Plot / section name</label>
            <input class="form-input" v-model="cur.name" placeholder="e.g. North Plot, Block A" />
          </div>
          <div class="form-group" style="flex:1">
            <label class="form-label">Area <span class="label-hint">hectares</span></label>
            <input class="form-input" v-model.number="cur.areaValue" type="number" min="0.1" step="0.5" />
          </div>
        </div>

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

        <div v-if="createError" class="form-error">{{ createError }}</div>

        <div class="form-actions">
          <button class="btn-add-section" :disabled="!canCreate || creating" @click="createPlot">
            <span v-if="creating" class="spinner-sm" />
            {{ creating ? 'Creating…' : '+ Add this plot' }}
          </button>
        </div>
      </div>

      <!-- ══ Boundary capture (a plot was just created and needs a boundary) ══ -->
      <div v-else class="work-card">
        <div class="work-card-head">
          <div class="work-card-title">Mark the boundary — {{ pending.name }}</div>
          <div class="work-card-sub">Choose GPS corner capture or upload a survey file, or skip and keep the {{ pending.areaValue }} ha you entered.</div>
        </div>

        <!-- Method tabs -->
        <div class="boundary-tabs" role="tablist">
          <button role="tab" :class="['btab', { 'btab--active': boundaryMethod === 'gps' }]" @click="boundaryMethod = 'gps'">
            <svg width="11" height="11" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round"><circle cx="12" cy="12" r="3"/><path d="M12 2v3M12 19v3M2 12h3M19 12h3"/></svg>
            GPS corners
          </button>
          <button role="tab" :class="['btab', { 'btab--active': boundaryMethod === 'upload' }]" @click="boundaryMethod = 'upload'">
            <svg width="11" height="11" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M21 15v4a2 2 0 01-2 2H5a2 2 0 01-2-2v-4M7 10l5 5 5-5M12 15V3"/></svg>
            Survey upload
          </button>
        </div>

        <!-- ── GPS corner capture ── -->
        <div v-if="boundaryMethod === 'gps'" class="method-body">
          <div class="gps-capture-row">
            <button type="button" class="btn-mark-corner" @click="markCornerHere" :disabled="gpsBusy">
              <span class="gps-spinner-sm" v-if="gpsBusy" />
              <svg v-else width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round"><circle cx="12" cy="12" r="3"/><path d="M12 2v3M12 19v3M2 12h3M19 12h3"/></svg>
              {{ gpsBusy ? 'Getting location…' : 'Mark Corner Here' }}
            </button>
            <div class="corner-count">{{ corners.length }} point{{ corners.length === 1 ? '' : 's' }} marked</div>
          </div>
          <div v-if="gpsError" class="form-error">{{ gpsError }}</div>
          <div v-if="gpsWarning" class="gps-warning">{{ gpsWarning }}</div>

          <!-- Manual fallback entry -->
          <div class="manual-corner-row">
            <input class="form-input" v-model.number="manualLat" type="number" step="0.00001" placeholder="Latitude e.g. 7.3775" />
            <input class="form-input" v-model.number="manualLng" type="number" step="0.00001" placeholder="Longitude e.g. 3.9470" />
            <button class="btn-add-crop" style="width:auto;padding:7px 14px;" :disabled="manualLat == null || manualLng == null" @click="addManualCorner">
              + Add point
            </button>
          </div>

          <!-- Corner list + schematic preview -->
          <div v-if="corners.length" class="gps-preview-wrap">
            <div class="corner-list">
              <div v-for="(c, i) in corners" :key="i" class="corner-list-row">
                <span class="corner-list-idx">{{ i + 1 }}</span>
                <span class="corner-list-coords">{{ c.lat.toFixed(5) }}°, {{ c.lng.toFixed(5) }}°</span>
                <span class="corner-list-src">
                  {{ c.source === 'gps' ? 'GPS' : 'manual' }}
                  <template v-if="c.accuracy != null"> · ±{{ Math.round(c.accuracy) }}m</template>
                </span>
                <button class="btn-icon" @click="corners.splice(i, 1)">✕</button>
              </div>
            </div>
            <div class="schematic-wrap">
              <div class="schematic-label">Schematic preview</div>
              <svg viewBox="0 0 200 200" class="schematic-svg">
                <polygon v-if="corners.length >= 3" :points="schematicPoints" fill="rgba(61,170,106,0.18)" stroke="#2d7a4f" stroke-width="2" />
                <polyline v-else-if="corners.length === 2" :points="schematicPoints" fill="none" stroke="#2d7a4f" stroke-width="2" />
                <circle
                  v-for="(pt, i) in schematicDots" :key="i"
                  :cx="pt.x" :cy="pt.y" r="4"
                  :fill="CORNER_COLORS[i % CORNER_COLORS.length]"
                />
              </svg>
              <div class="schematic-hint">No map tiles needed — this is a relative sketch of your points.</div>
            </div>
          </div>

          <div v-if="needsDistanceConfirm" class="distance-confirm">
            <div class="distance-confirm-msg">{{ boundaryError }}</div>
            <div class="distance-confirm-actions">
              <button class="btn-ghost-sm" @click="needsDistanceConfirm = false">Cancel</button>
              <button class="btn-confirm-sm" @click="finishGpsBoundary(true)" :disabled="boundarySaving">Yes, these are correct</button>
            </div>
          </div>
          <div v-else-if="boundaryError" class="form-error">{{ boundaryError }}</div>

          <div class="boundary-actions">
            <button class="btn-skip" @click="skipBoundary">Skip — use manual hectares</button>
            <button class="btn-add-section" :disabled="corners.length < 3 || boundarySaving" @click="finishGpsBoundary(false)">
              <span v-if="boundarySaving" class="spinner-sm" />
              {{ boundarySaving ? 'Saving…' : 'Finish boundary' }}
            </button>
          </div>
          <div v-if="corners.length > 0 && corners.length < 3" class="hint-txt">Add at least 3 points to close a boundary.</div>
        </div>

        <!-- ── Survey upload ── -->
        <div v-else class="method-body">
          <label class="file-drop">
            <input type="file" accept=".geojson,.json,.kml" @change="onFileSelected" />
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6"><path d="M21 15v4a2 2 0 01-2 2H5a2 2 0 01-2-2v-4M7 10l5 5 5-5M12 15V3"/></svg>
            <span v-if="!uploadFileName">Choose a .geojson or .kml file</span>
            <span v-else>{{ uploadFileName }}</span>
          </label>

          <div v-if="uploadError" class="form-error">{{ uploadError }}</div>

          <div v-if="uploadPreview" class="upload-preview">
            <svg width="10" height="10" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><polyline points="20 6 9 17 4 12"/></svg>
            Parsed {{ uploadPreview.type.toUpperCase() }} — {{ uploadPreview.pointCount }} coordinate{{ uploadPreview.pointCount === 1 ? '' : 's' }} found.
            <span class="upload-preview-note">(Server does the authoritative parsing on upload.)</span>
          </div>

          <div class="boundary-actions">
            <button class="btn-skip" @click="skipBoundary">Skip — use manual hectares</button>
            <button class="btn-add-section" :disabled="!uploadContent || uploadSaving" @click="finishUpload">
              <span v-if="uploadSaving" class="spinner-sm" />
              {{ uploadSaving ? 'Uploading…' : 'Upload & finish' }}
            </button>
          </div>
        </div>
      </div>

    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { useRouter } from 'vue-router'
import { useAgroStore } from '@/stores/agro'
import { sectionApi } from '@/services/api'
import { extractErrorMessage, isImplausibleDistanceError } from '@/utils/http'
import type { BoundarySource } from '@/types'

const router = useRouter()
const store  = useAgroStore()

// ── Constants ──
const COLORS = ['#2d7a4f', '#3daa6a', '#e8a838', '#e64a19', '#5b4fcf', '#c2185b', '#0288d1', '#00796b']
const CORNER_COLORS = ['#3daa6a', '#e8a838', '#e64a19', '#5b4fcf', '#0288d1', '#c2185b']
// Matches the backend's CropType enum exactly (OilPalm=0, Maize=1, Mixed=2, Cocoa=3, Cassava=4).
const CROP_TYPES = ['OilPalm', 'Maize', 'Mixed', 'Cocoa', 'Cassava']
const CROP_ORDINAL: Record<string, number> = { OilPalm: 0, Maize: 1, Mixed: 2, Cocoa: 3, Cassava: 4 }
const BOUNDARY_SRC_LABEL: Record<BoundarySource, string> = { gps: 'GPS', upload: 'survey file', manual: 'manual ha' }

// ── Added plots (server-confirmed, boundary resolved or skipped) ──
const addedSections = computed(() => store.wizardSections)

// ── New plot form ──
const cur = ref({
  name: '',
  areaValue: 10,
  crops: [{ cropType: 'OilPalm', percentage: 100 }] as { cropType: string; percentage: number }[],
})
const creating = ref(false)
const createError = ref('')

const cropTotal = computed(() => cur.value.crops.reduce((s, c) => s + (c.percentage || 0), 0))
const canCreate = computed(() =>
  cur.value.name.trim().length > 0 && cur.value.areaValue > 0 && cropTotal.value === 100
)

function resetForm() {
  cur.value = { name: '', areaValue: 10, crops: [{ cropType: 'OilPalm', percentage: 100 }] }
}

async function createPlot() {
  if (!canCreate.value) return
  creating.value = true
  createError.value = ''
  try {
    const res = await sectionApi.create({
      name: cur.value.name,
      crops: cur.value.crops.map(c => ({ cropType: CROP_ORDINAL[c.cropType], percentage: c.percentage })),
      areaValue: cur.value.areaValue,
    })
    pending.value = { id: res.data.id, name: res.data.name, areaValue: cur.value.areaValue }
    resetForm()
  } catch (e) {
    createError.value = extractErrorMessage(e, 'Failed to create this plot.')
  } finally {
    creating.value = false
  }
}

// ── Pending plot awaiting boundary capture ──
const pending = ref<{ id: string; name: string; areaValue: number } | null>(null)
const boundaryMethod = ref<'gps' | 'upload'>('gps')

function resetBoundaryState() {
  corners.value = []
  manualLat.value = null
  manualLng.value = null
  gpsError.value = ''
  boundaryError.value = ''
  needsDistanceConfirm.value = false
  uploadFile.value = null
  uploadFileName.value = ''
  uploadContent.value = null
  uploadPreview.value = null
  uploadError.value = ''
  uploadOpId.value = null
  boundaryMethod.value = 'gps'
}

function skipBoundary() {
  if (!pending.value) return
  store.wizardSections.push({
    id: pending.value.id, name: pending.value.name, areaValue: pending.value.areaValue,
    boundarySource: 'manual', boundaryHectares: null,
  })
  pending.value = null
  resetBoundaryState()
}

function finalizePending(source: BoundarySource, hectares: number | null) {
  if (!pending.value) return
  clearOpId(pending.value.id, 'gps')
  clearOpId(pending.value.id, 'upload')
  store.wizardSections.push({
    id: pending.value.id, name: pending.value.name, areaValue: pending.value.areaValue,
    boundarySource: source, boundaryHectares: hectares,
  })
  pending.value = null
  resetBoundaryState()
}

// ── Idempotency: one client-generated operation id per section+method, reused on retry ──
function opIdKey(sectionId: string, method: 'gps' | 'upload') {
  return `agro_boundary_op_${sectionId}_${method}`
}
function getOrCreateOpId(sectionId: string, method: 'gps' | 'upload', forceNew = false): string {
  const key = opIdKey(sectionId, method)
  if (!forceNew) {
    const existing = localStorage.getItem(key)
    if (existing) return existing
  }
  const id = crypto.randomUUID()
  localStorage.setItem(key, id)
  return id
}
function clearOpId(sectionId: string, method: 'gps' | 'upload') {
  localStorage.removeItem(opIdKey(sectionId, method))
}

// ── GPS corner capture ──
const corners = ref<{ lat: number; lng: number; source: 'gps' | 'manual'; accuracy?: number }[]>([])
const manualLat = ref<number | null>(null)
const manualLng = ref<number | null>(null)
const gpsBusy = ref(false)
const gpsError = ref('')
const gpsWarning = ref('')
const boundarySaving = ref(false)
const boundaryError = ref('')
const needsDistanceConfirm = ref(false)

// Great-circle distance in meters — used only client-side to sanity-check a freshly
// captured point against the previous one (not sent to the server).
function haversineMeters(a: { lat: number; lng: number }, b: { lat: number; lng: number }): number {
  const R = 6371000
  const toRad = (d: number) => (d * Math.PI) / 180
  const dLat = toRad(b.lat - a.lat)
  const dLng = toRad(b.lng - a.lng)
  const lat1 = toRad(a.lat)
  const lat2 = toRad(b.lat)
  const h = Math.sin(dLat / 2) ** 2 + Math.cos(lat1) * Math.cos(lat2) * Math.sin(dLng / 2) ** 2
  return R * 2 * Math.asin(Math.min(1, Math.sqrt(h)))
}

async function markCornerHere() {
  if (!navigator.geolocation) {
    gpsError.value = 'Geolocation is not available on this device — enter coordinates manually below.'
    return
  }
  gpsBusy.value = true
  gpsError.value = ''
  gpsWarning.value = ''
  try {
    const pos = await new Promise<GeolocationPosition>((resolve, reject) =>
      // maximumAge: 0 forces a fresh fix on every tap instead of letting the browser return a
      // cached position — already the spec default when omitted, but stated explicitly so a
      // browser/environment quirk can't silently reintroduce a stale-position bug here.
      navigator.geolocation.getCurrentPosition(resolve, reject, { timeout: 15000, enableHighAccuracy: true, maximumAge: 0 })
    )
    const next = {
      lat: Math.round(pos.coords.latitude * 100000) / 100000,
      lng: Math.round(pos.coords.longitude * 100000) / 100000,
      source: 'gps' as const,
      accuracy: pos.coords.accuracy,
    }

    // Defends against the "every corner has the same coordinates" failure mode even when it's
    // not a code bug — e.g. a desktop browser falling back to Wi-Fi/IP-based positioning, or an
    // OS location service returning a stale cached fix the Geolocation API can't force past.
    const prev = corners.value[corners.value.length - 1]
    if (prev) {
      const distance = haversineMeters(prev, next)
      if (distance < 2) {
        gpsWarning.value =
          `This point is almost exactly where the last one was (${distance.toFixed(1)}m away) — ` +
          'make sure you actually moved, or your browser may be reusing a cached/network-based location fix.'
      } else if (next.accuracy > 100) {
        gpsWarning.value =
          `Low-accuracy fix (±${Math.round(next.accuracy)}m) — this looks like Wi-Fi/IP-based positioning ` +
          'rather than real GPS. The point was still recorded; a phone with a GPS chip will be far more accurate.'
      }
    }

    corners.value.push(next)
  } catch {
    gpsError.value = 'Could not get GPS location (denied or unavailable) — enter coordinates manually below.'
  } finally {
    gpsBusy.value = false
  }
}

function addManualCorner() {
  if (manualLat.value == null || manualLng.value == null) return
  corners.value.push({ lat: manualLat.value, lng: manualLng.value, source: 'manual' })
  manualLat.value = null
  manualLng.value = null
}

// Lightweight schematic polygon preview — no map tiles required. Projects lat/lng into a
// relative 200x200 box so a farmer with no connectivity can still see their boundary shape.
function project(points: { lat: number; lng: number }[]): { x: number; y: number }[] {
  if (!points.length) return []
  const lats = points.map(p => p.lat)
  const lngs = points.map(p => p.lng)
  const minLat = Math.min(...lats), maxLat = Math.max(...lats)
  const minLng = Math.min(...lngs), maxLng = Math.max(...lngs)
  const spanLat = Math.max(maxLat - minLat, 0.00001)
  const spanLng = Math.max(maxLng - minLng, 0.00001)
  const pad = 24, size = 200
  return points.map(p => ({
    x: pad + ((p.lng - minLng) / spanLng) * (size - pad * 2),
    y: pad + (1 - (p.lat - minLat) / spanLat) * (size - pad * 2), // flip: lat increases north, svg y increases down
  }))
}
const schematicDots = computed(() => project(corners.value))
const schematicPoints = computed(() => schematicDots.value.map(p => `${p.x.toFixed(1)},${p.y.toFixed(1)}`).join(' '))

async function finishGpsBoundary(confirmImplausible: boolean) {
  if (!pending.value) return
  if (corners.value.length < 3) {
    boundaryError.value = 'Add at least 3 points to close a boundary.'
    return
  }
  boundarySaving.value = true
  if (!confirmImplausible) boundaryError.value = ''
  try {
    const opId = getOrCreateOpId(pending.value.id, 'gps')
    const res = await store.submitBoundaryPoints(pending.value.id, {
      operationId: opId,
      points: corners.value.map(c => ({ lat: c.lat, lng: c.lng })),
      confirmImplausibleDistance: confirmImplausible,
    })
    needsDistanceConfirm.value = false
    finalizePending('gps', res.computedHectares)
  } catch (e) {
    if (isImplausibleDistanceError(e)) {
      needsDistanceConfirm.value = true
      boundaryError.value = extractErrorMessage(e, 'These points look implausibly far apart.')
    } else {
      needsDistanceConfirm.value = false
      boundaryError.value = extractErrorMessage(e, 'Failed to save boundary — you can retry, your points are kept.')
    }
  } finally {
    boundarySaving.value = false
  }
}

// ── Survey upload ──
const uploadFile = ref<File | null>(null)
const uploadFileName = ref('')
const uploadContent = ref<string | null>(null)
const uploadPreview = ref<{ type: 'geojson' | 'kml'; pointCount: number } | null>(null)
const uploadError = ref('')
const uploadSaving = ref(false)
const uploadOpId = ref<string | null>(null)

function previewParse(text: string, fileName: string): { type: 'geojson' | 'kml'; pointCount: number } | null {
  const isKml = /\.kml$/i.test(fileName)
  try {
    if (isKml) {
      const matches = [...text.matchAll(/<coordinates>([\s\S]*?)<\/coordinates>/gi)]
      const coordText = matches[0]?.[1] ?? ''
      const pts = coordText.trim().split(/\s+/).filter(Boolean)
      return { type: 'kml', pointCount: pts.length }
    }
    const json = JSON.parse(text)
    const raw = json?.coordinates ?? json?.geometry?.coordinates ?? null
    if (!Array.isArray(raw)) return null
    // Polygon coordinates are [ [ [lng,lat], ... ] ]; a bare ring is [ [lng,lat], ... ]
    const ring = Array.isArray(raw[0]?.[0]) ? raw[0] : raw
    return { type: 'geojson', pointCount: Array.isArray(ring) ? ring.length : 0 }
  } catch {
    return null
  }
}

function onFileSelected(e: Event) {
  const input = e.target as HTMLInputElement
  const file = input.files?.[0]
  if (!file || !pending.value) return
  uploadError.value = ''
  uploadFile.value = file
  uploadFileName.value = file.name
  uploadContent.value = null
  uploadPreview.value = null
  // Generate (or replace) the idempotency id the moment a file is selected — not at submit time —
  // so a retry of the *same* selected file always reuses it.
  uploadOpId.value = getOrCreateOpId(pending.value.id, 'upload', true)

  const reader = new FileReader()
  reader.onload = () => {
    const text = String(reader.result ?? '')
    uploadContent.value = text
    uploadPreview.value = previewParse(text, file.name)
    if (!uploadPreview.value) {
      uploadError.value = 'Could not preview this file client-side — it will still be sent to the server for authoritative parsing.'
    }
  }
  reader.onerror = () => { uploadError.value = 'Could not read this file.' }
  reader.readAsText(file)
}

async function finishUpload() {
  if (!pending.value || !uploadContent.value) return
  uploadSaving.value = true
  uploadError.value = ''
  try {
    const opId = uploadOpId.value ?? getOrCreateOpId(pending.value.id, 'upload')
    const res = await store.uploadBoundaryFile(pending.value.id, {
      operationId: opId,
      fileName: uploadFileName.value,
      content: uploadContent.value,
    })
    finalizePending('upload', res.computedHectares)
  } catch (e) {
    uploadError.value = extractErrorMessage(e, 'Failed to upload boundary — you can retry, the same file will be resent under the same operation id.')
  } finally {
    uploadSaving.value = false
  }
}

// ── Finish ──
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

.pending-notice {
  display: flex; align-items: flex-start; gap: 6px;
  background: rgba(232,168,56,0.1);
  border: 1px solid rgba(232,168,56,0.25);
  border-radius: 8px; padding: 8px 10px;
  font-size: 10.5px; color: #f0c674; line-height: 1.5;
  margin-bottom: 12px;
}
.pending-notice svg { flex-shrink: 0; margin-top: 1px; }

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
.sections-main { flex: 1; display: flex; flex-direction: column; overflow-y: auto; padding: 32px; }
.work-card {
  background: #fff;
  border-radius: 16px;
  padding: 24px 28px;
  max-width: 640px;
  width: 100%;
  margin: 0 auto;
  box-shadow: 0 24px 64px rgba(0,0,0,0.25);
}
.work-card-head { margin-bottom: 18px; }
.work-card-title { font-family: 'Syne', sans-serif; font-size: 17px; font-weight: 700; color: #111827; letter-spacing: -0.01em; }
.work-card-sub { font-size: 11.5px; color: #6b7280; margin-top: 4px; line-height: 1.5; }

/* Boundary method tabs */
.boundary-tabs {
  display: flex;
  border-bottom: 1px solid #eee;
  background: #fafafa;
  border-radius: 10px 10px 0 0;
  margin-top: 4px;
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

.method-body { padding: 16px 4px 4px; }

/* GPS capture */
.gps-capture-row { display: flex; align-items: center; gap: 12px; margin-bottom: 10px; }
.btn-mark-corner {
  display: flex; align-items: center; gap: 7px;
  padding: 10px 16px;
  background: #2d7a4f; color: #fff; border: none;
  border-radius: 10px; font-size: 12px; font-weight: 600;
  cursor: pointer; transition: background 0.15s;
}
.btn-mark-corner:hover:not(:disabled) { background: #1e5c3a; }
.btn-mark-corner:disabled { opacity: 0.6; cursor: not-allowed; }
.corner-count { font-family: 'JetBrains Mono', monospace; font-size: 11px; color: #6b7280; }
.gps-spinner-sm {
  width: 12px; height: 12px;
  border: 1.5px solid rgba(255,255,255,0.4);
  border-top-color: #fff;
  border-radius: 50%;
  animation: spin 0.7s linear infinite;
}

.manual-corner-row { display: flex; gap: 8px; margin-bottom: 12px; }
.manual-corner-row .form-input { font-size: 11px; }

.gps-preview-wrap { display: flex; gap: 16px; margin-bottom: 12px; }
.corner-list { flex: 1; display: flex; flex-direction: column; gap: 4px; max-height: 200px; overflow-y: auto; }
.corner-list-row { display: flex; align-items: center; gap: 8px; padding: 5px 8px; background: #f9fafb; border-radius: 6px; }
.corner-list-idx {
  width: 18px; height: 18px; border-radius: 50%;
  background: #2d7a4f; color: #fff;
  display: flex; align-items: center; justify-content: center;
  font-family: 'JetBrains Mono', monospace; font-size: 9px; font-weight: 700; flex-shrink: 0;
}
.corner-list-coords { flex: 1; font-family: 'JetBrains Mono', monospace; font-size: 10.5px; color: #374151; }
.corner-list-src { font-family: 'JetBrains Mono', monospace; font-size: 8px; text-transform: uppercase; color: #9ca3af; }

.schematic-wrap { width: 160px; flex-shrink: 0; }
.schematic-label { font-family: 'JetBrains Mono', monospace; font-size: 8px; text-transform: uppercase; letter-spacing: 0.08em; color: #9ca3af; margin-bottom: 4px; }
.schematic-svg { width: 160px; height: 160px; background: #f9fafb; border: 1px solid #eee; border-radius: 10px; }
.schematic-hint { font-size: 9px; color: #9ca3af; margin-top: 4px; line-height: 1.4; }

.distance-confirm {
  background: #FFF6E5; border: 1px solid #f0c674; border-radius: 8px;
  padding: 10px 12px; margin-bottom: 10px;
}
.distance-confirm-msg { font-size: 11.5px; color: #8A6000; line-height: 1.5; margin-bottom: 8px; }
.distance-confirm-actions { display: flex; gap: 8px; }
.btn-ghost-sm { padding: 6px 12px; background: none; border: 1px solid #e5e7eb; border-radius: 7px; font-size: 11px; color: #374151; cursor: pointer; }
.btn-confirm-sm { padding: 6px 12px; background: #E8A838; border: none; border-radius: 7px; font-size: 11px; font-weight: 600; color: #3d2c00; cursor: pointer; }

.boundary-actions { display: flex; justify-content: space-between; align-items: center; margin-top: 14px; }
.btn-skip { background: none; border: none; font-size: 11.5px; color: #9ca3af; text-decoration: underline; cursor: pointer; }
.btn-skip:hover { color: #ef4444; }
.hint-txt { font-size: 10px; color: #9ca3af; margin-top: 6px; }

/* Survey upload */
.file-drop {
  display: flex; flex-direction: column; align-items: center; gap: 8px;
  padding: 28px 16px;
  border: 2px dashed #e5e7eb;
  border-radius: 12px;
  color: #6b7280;
  font-size: 12px;
  cursor: pointer;
  transition: border-color 0.15s, background 0.15s;
}
.file-drop:hover { border-color: #2d7a4f; background: rgba(45,122,79,0.03); }
.file-drop input[type="file"] { display: none; }
.upload-preview {
  display: flex; align-items: center; gap: 6px; flex-wrap: wrap;
  font-size: 11px; color: #2d7a4f; font-weight: 600;
  margin-top: 10px;
}
.upload-preview-note { font-weight: 400; color: #9ca3af; }

/* Shared form bits (reused across both work-card modes) */
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

.form-error { font-size: 11px; color: #dc2626; margin: 6px 0; line-height: 1.5; }
.gps-warning {
  font-size: 11px; color: #8A6000; line-height: 1.5; margin: 6px 0;
  background: #FFF6E5; border: 1px solid #f0c674; border-radius: 8px; padding: 8px 10px;
}

.form-actions { display: flex; justify-content: flex-end; margin-top: 16px; }
.btn-add-section {
  display: flex; align-items: center; gap: 7px;
  padding: 10px 20px;
  background: #2d7a4f; color: #fff; border: none;
  border-radius: 10px; font-size: 12px; font-weight: 600;
  cursor: pointer; transition: background 0.15s;
}
.btn-add-section:hover:not(:disabled) { background: #1e5c3a; }
.btn-add-section:disabled { opacity: 0.45; cursor: not-allowed; }

.spinner-sm {
  width: 12px; height: 12px;
  border: 2px solid rgba(255,255,255,0.3);
  border-top-color: #fff;
  border-radius: 50%;
  animation: spin 0.7s linear infinite;
  flex-shrink: 0;
}
@keyframes spin { to { transform: rotate(360deg); } }
</style>
