<template>
  <div class="work-card">

    <!-- ══ Phase 1: plot details (name, area, crops) ══ -->
    <template v-if="phase === 'details'">
      <div class="work-card-head">
        <div class="work-card-title">{{ mode === 'edit' ? `Edit — ${detailsForm.name || block?.name}` : 'New plot' }}</div>
        <div class="work-card-sub">
          {{ mode === 'edit' ? 'Update its name, area or crop mix — you can also re-mark its boundary next.' : "Give it a name, area and crop mix — you'll mark its boundary next." }}
        </div>
      </div>

      <div class="form-row">
        <div class="form-group" style="flex:2">
          <label class="form-label">Plot / block name</label>
          <input class="form-input" v-model="detailsForm.name" placeholder="e.g. North Plot, Block A" />
        </div>
        <div class="form-group" style="flex:1">
          <label class="form-label">Area <span class="label-hint">hectares</span></label>
          <input class="form-input" v-model.number="detailsForm.areaValue" type="number" min="0.1" step="0.5" @wheel.prevent />
        </div>
        <div class="form-group" style="flex:1">
          <label class="form-label">Trees <span class="label-hint">optional</span></label>
          <input class="form-input" v-model.number="detailsForm.treeCount" type="number" min="0" step="1" placeholder="—" @wheel.prevent />
        </div>
      </div>

      <div class="form-group">
        <label class="form-label">What is growing here?</label>
        <div class="kind-toggle">
          <button type="button" :class="{ on: cropKind === 'single' }" @click="setKind('single')">One crop</button>
          <button type="button" :class="{ on: cropKind === 'mixed' }" @click="setKind('mixed')">Mixed (more than one crop)</button>
        </div>

        <!-- One crop -->
        <div v-if="cropKind === 'single'" class="crop-row">
          <select class="form-select" v-model="detailsForm.crops[0].cropType" style="flex:1">
            <option v-for="ct in CROP_TYPES" :key="ct" :value="ct">{{ CROP_LABELS[ct] }}</option>
          </select>
        </div>

        <!-- Mixed: which crops, and how much of the block each takes -->
        <template v-else>
          <div class="form-hint" style="margin-bottom:6px;">
            Pick every crop in this block, then say roughly how much of it each takes. Fertilizer
            timing follows the crops, so this matters.
          </div>
          <div class="crop-chips">
            <button v-for="ct in CROP_TYPES" :key="ct" type="button" class="crop-chip"
                    :class="{ on: hasCrop(ct) }" @click="toggleCrop(ct)">{{ CROP_LABELS[ct] }}</button>
          </div>
          <div v-for="c in detailsForm.crops" :key="c.cropType" class="crop-row">
            <div class="crop-name">{{ CROP_LABELS[c.cropType] }}</div>
            <input class="form-input" v-model.number="c.percentage" type="number"
                   min="1" max="100" style="width:72px" placeholder="%" @wheel.prevent />
            <span class="label-hint">% of the block</span>
          </div>
          <button v-if="detailsForm.crops.length > 1" type="button" class="btn-add-crop" @click="splitEvenly">Split evenly</button>
          <div v-if="detailsForm.crops.length < 2" class="crop-warning">Choose at least two crops for a mixed block.</div>
          <div v-else-if="cropTotal !== 100" class="crop-warning">Total: {{ cropTotal }}% — must equal 100%</div>
        </template>
      </div>

      <div class="form-group">
        <label class="form-label">Year planted <span class="label-hint">optional — young crops are fed more often</span></label>
        <input class="form-input" v-model.number="detailsForm.plantingYear" type="number" :min="1950" :max="new Date().getFullYear() + 1"
               step="1" placeholder="e.g. 2019" style="max-width:160px" @wheel.prevent />
      </div>

      <div v-if="detailsError" class="form-error">{{ detailsError }}</div>

      <div class="form-actions">
        <button v-if="mode === 'edit'" class="btn-skip" @click="$emit('cancelled')" :disabled="savingDetails">Cancel</button>
        <button class="btn-add-block" :disabled="!canSaveDetails || savingDetails" @click="saveDetails">
          <span v-if="savingDetails" class="spinner-sm" />
          {{ savingDetails ? 'Saving…' : (mode === 'edit' ? 'Save & continue to boundary →' : '+ Add this plot') }}
        </button>
      </div>
    </template>

    <!-- ══ Phase 2: boundary capture ══ -->
    <template v-else>
      <div class="work-card-head">
        <div class="work-card-title">Mark the boundary — {{ activeBlock?.name }}</div>
        <div class="work-card-sub">
          Choose GPS corner capture or upload a survey file, or {{ skipLabel.toLowerCase() }}.
        </div>
      </div>

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

      <!-- ── GPS corner capture (also handles manual corner entry) ── -->
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

        <div class="manual-corner-row">
          <input class="form-input" v-model.number="manualLat" type="number" step="0.00001" placeholder="Latitude e.g. 7.3775" @wheel.prevent />
          <input class="form-input" v-model.number="manualLng" type="number" step="0.00001" placeholder="Longitude e.g. 3.9470" @wheel.prevent />
          <button class="btn-add-crop" style="width:auto;padding:7px 14px;" :disabled="manualLat == null || manualLng == null" @click="addManualCorner">
            + Add point
          </button>
        </div>

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

        <div v-if="needsGeometryConfirm" class="distance-confirm">
          <div class="distance-confirm-msg">{{ boundaryError }}</div>
          <div class="distance-confirm-actions">
            <button class="btn-ghost-sm" @click="needsGeometryConfirm = false">Cancel</button>
            <button class="btn-confirm-sm" @click="finishGpsBoundary(true)" :disabled="boundarySaving">Yes, these are correct</button>
          </div>
        </div>
        <div v-else-if="boundaryError" class="form-error">{{ boundaryError }}</div>

        <div class="boundary-actions">
          <button class="btn-skip" @click="skip">{{ skipLabel }}</button>
          <button class="btn-add-block" :disabled="corners.length < MIN_CORNERS || boundarySaving" @click="finishGpsBoundary(false)">
            <span v-if="boundarySaving" class="spinner-sm" />
            {{ boundarySaving ? 'Saving…' : 'Finish boundary' }}
          </button>
        </div>
        <div v-if="corners.length > 0 && corners.length < MIN_CORNERS" class="hint-txt">Add at least {{ MIN_CORNERS }} points to close a boundary.</div>
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

        <div v-if="needsUploadGeometryConfirm" class="distance-confirm">
          <div class="distance-confirm-msg">{{ uploadError }}</div>
          <div class="distance-confirm-actions">
            <button class="btn-ghost-sm" @click="needsUploadGeometryConfirm = false">Cancel</button>
            <button class="btn-confirm-sm" @click="finishUpload(true)" :disabled="uploadSaving">Yes, this is correct</button>
          </div>
        </div>

        <div class="boundary-actions">
          <button class="btn-skip" @click="skip">{{ skipLabel }}</button>
          <button class="btn-add-block" :disabled="!uploadContent || uploadSaving" @click="finishUpload(false)">
            <span v-if="uploadSaving" class="spinner-sm" />
            {{ uploadSaving ? 'Uploading…' : 'Upload & finish' }}
          </button>
        </div>
      </div>
    </template>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useAgroStore } from '@/stores/agro'
import { blockApi } from '@/services/api'
import { extractErrorMessage, isImplausibleGeometryError } from '@/utils/http'
import { normalizeBlock } from '@/utils/enums'
import type { Block } from '@/types'

const props = defineProps<{
  mode: 'create' | 'edit'
  block?: Block | null // required when mode === 'edit'
}>()

const emit = defineEmits<{
  saved: [block: Block]
  cancelled: []
}>()

const store = useAgroStore()

// ── Constants ──
const CORNER_COLORS = ['#3daa6a', '#e8a838', '#e64a19', '#5b4fcf', '#0288d1', '#c2185b']
// Matches the backend's CropType enum (OilPalm=0, Maize=1, Mixed=2, Cocoa=3, Cassava=4). "Mixed" is not a crop
// you can pick any more: a mixed block lists the real crops in it (the backend rejects a bare "Mixed").
const CROP_TYPES = ['OilPalm', 'Maize', 'Cocoa', 'Cassava']
const CROP_LABELS: Record<string, string> = { OilPalm: 'Oil palm', Maize: 'Maize', Cocoa: 'Cocoa', Cassava: 'Cassava' }
const CROP_ORDINAL: Record<string, number> = { OilPalm: 0, Maize: 1, Mixed: 2, Cocoa: 3, Cassava: 4 }
const MIN_CORNERS = 4

// ── Phase machine: details -> boundary ──
const phase = ref<'details' | 'boundary'>('details')
const activeBlock = ref<Block | null>(props.block ?? null)

// ── Phase 1: details form ──
function cropsFromBlock(block: Block | null | undefined) {
  // A legacy "Mixed" entry says nothing about what is growing: drop it so the user is asked.
  const real = (block?.crops ?? []).filter(c => c.cropType !== 'Mixed')
  if (!block || !real.length) return [{ cropType: 'OilPalm', percentage: 100 }]
  return real.map(c => ({ cropType: c.cropType, percentage: c.percentage }))
}

const wasMixed = !!props.block && (props.block.crops.length > 1 || props.block.crops.some(c => c.cropType === 'Mixed'))

const detailsForm = ref({
  name: props.block?.name ?? '',
  areaValue: props.block?.area.hectares ?? 10,
  crops: cropsFromBlock(props.block) as { cropType: string; percentage: number }[],
  treeCount: (props.block?.treeCount ?? '') as number | '',
  plantingYear: (props.block?.plantingYear ?? '') as number | '',
})
const cropKind = ref<'single' | 'mixed'>(wasMixed ? 'mixed' : 'single')
// A legacy "Mixed" block has no real crops to show yet: start with none picked.
if (props.block?.crops.every(c => c.cropType === 'Mixed') && props.block.crops.length > 0) detailsForm.value.crops = []

function setKind(kind: 'single' | 'mixed') {
  if (kind === cropKind.value) return
  cropKind.value = kind
  const first = detailsForm.value.crops[0]?.cropType ?? 'OilPalm'
  detailsForm.value.crops = kind === 'single' ? [{ cropType: first, percentage: 100 }] : [{ cropType: first, percentage: 100 }]
}

function hasCrop(ct: string) { return detailsForm.value.crops.some(c => c.cropType === ct) }

function splitEvenly() {
  const n = detailsForm.value.crops.length
  if (!n) return
  const base = Math.floor(100 / n)
  detailsForm.value.crops.forEach((c, i) => { c.percentage = base + (i === 0 ? 100 - base * n : 0) })
}

function toggleCrop(ct: string) {
  if (hasCrop(ct)) detailsForm.value.crops = detailsForm.value.crops.filter(c => c.cropType !== ct)
  else detailsForm.value.crops.push({ cropType: ct, percentage: 0 })
  splitEvenly()
}
const savingDetails = ref(false)
const detailsError = ref('')

const cropTotal = computed(() => detailsForm.value.crops.reduce((s, c) => s + (c.percentage || 0), 0))
const canSaveDetails = computed(() =>
  detailsForm.value.name.trim().length > 0 && detailsForm.value.areaValue > 0 && cropTotal.value === 100 &&
  (cropKind.value === 'single' || detailsForm.value.crops.length >= 2)
)

async function saveDetails() {
  if (!canSaveDetails.value) return
  savingDetails.value = true
  detailsError.value = ''
  try {
    const payload = {
      name: detailsForm.value.name,
      crops: detailsForm.value.crops.map(c => ({ cropType: CROP_ORDINAL[c.cropType], percentage: c.percentage })),
      areaValue: detailsForm.value.areaValue,
      treeCount: detailsForm.value.treeCount === '' ? null : detailsForm.value.treeCount,
      plantingYear: detailsForm.value.plantingYear === '' ? null : detailsForm.value.plantingYear,
    }
    if (props.mode === 'edit' && activeBlock.value) {
      activeBlock.value = await store.updateBlock(activeBlock.value.id, payload)
    } else {
      const res = await blockApi.create(payload)
      activeBlock.value = normalizeBlock(res.data)
    }
    phase.value = 'boundary'
  } catch (e) {
    detailsError.value = extractErrorMessage(e, 'Failed to save this plot.')
  } finally {
    savingDetails.value = false
  }
}

// ── Phase 2: boundary capture ──
const hasExistingBoundary = computed(() => !!activeBlock.value?.boundaryGeoJson)
const skipLabel = computed(() =>
  hasExistingBoundary.value ? 'Keep existing boundary' : 'Skip — use manual hectares'
)

const boundaryMethod = ref<'gps' | 'upload'>('gps')
const corners = ref<{ lat: number; lng: number; source: 'gps' | 'manual'; accuracy?: number }[]>([])
const manualLat = ref<number | null>(null)
const manualLng = ref<number | null>(null)
const gpsBusy = ref(false)
const gpsError = ref('')
const gpsWarning = ref('')
const boundarySaving = ref(false)
const boundaryError = ref('')
const needsGeometryConfirm = ref(false)

// Pre-populate corners from an existing boundary when editing, so the user sees (and can
// tweak/delete) their current perimeter instead of starting from a blank slate.
function parseExistingCorners(geoJson: string | null | undefined): typeof corners.value {
  if (!geoJson) return []
  try {
    const parsed = JSON.parse(geoJson)
    const ring: [number, number][] = parsed?.coordinates?.[0] ?? []
    if (!Array.isArray(ring) || ring.length === 0) return []
    const pts = ring.map(([lng, lat]) => ({ lat, lng, source: 'manual' as const }))
    if (pts.length > 1) {
      const first = pts[0], last = pts[pts.length - 1]
      if (first.lat === last.lat && first.lng === last.lng) pts.pop()
    }
    return pts
  } catch {
    return []
  }
}

onMounted(() => {
  // Pre-populate corners so they're ready by the time the user reaches the boundary step —
  // but always start at the details step (name/crops are still editable, per spec), never
  // skip straight past it just because a boundary already exists.
  if (props.mode === 'edit' && props.block) {
    corners.value = parseExistingCorners(props.block.boundaryGeoJson)
  }
})

// ── Idempotency: one client-generated operation id per block+method, reused on retry ──
function opIdKey(blockId: string, method: 'gps' | 'upload') {
  return `agro_boundary_op_${blockId}_${method}`
}
function getOrCreateOpId(blockId: string, method: 'gps' | 'upload', forceNew = false): string {
  const key = opIdKey(blockId, method)
  if (!forceNew) {
    const existing = localStorage.getItem(key)
    if (existing) return existing
  }
  const id = crypto.randomUUID()
  localStorage.setItem(key, id)
  return id
}
function clearOpId(blockId: string, method: 'gps' | 'upload') {
  localStorage.removeItem(opIdKey(blockId, method))
}

function skip() {
  if (!activeBlock.value) return
  emit('saved', activeBlock.value)
}

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
  if (!activeBlock.value) return
  if (corners.value.length < MIN_CORNERS) {
    boundaryError.value = `Add at least ${MIN_CORNERS} points to close a boundary.`
    return
  }
  boundarySaving.value = true
  if (!confirmImplausible) boundaryError.value = ''
  try {
    const opId = getOrCreateOpId(activeBlock.value.id, 'gps')
    await store.submitBoundaryPoints(activeBlock.value.id, {
      operationId: opId,
      points: corners.value.map(c => ({ lat: c.lat, lng: c.lng })),
      confirmImplausibleGeometry: confirmImplausible,
    })
    needsGeometryConfirm.value = false
    clearOpId(activeBlock.value.id, 'gps')
    const res = await blockApi.getAll()
    const updated = (res.data as Block[]).find(s => s.id === activeBlock.value!.id)
    emit('saved', updated ?? activeBlock.value)
  } catch (e) {
    if (isImplausibleGeometryError(e)) {
      needsGeometryConfirm.value = true
      boundaryError.value = extractErrorMessage(e, 'This boundary looks implausible.')
    } else {
      needsGeometryConfirm.value = false
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
const needsUploadGeometryConfirm = ref(false)

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
  if (!file || !activeBlock.value) return
  uploadError.value = ''
  needsUploadGeometryConfirm.value = false
  uploadFile.value = file
  uploadFileName.value = file.name
  uploadContent.value = null
  uploadPreview.value = null
  // Generate (or replace) the idempotency id the moment a file is selected — not at submit time —
  // so a retry of the *same* selected file always reuses it.
  uploadOpId.value = getOrCreateOpId(activeBlock.value.id, 'upload', true)

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

async function finishUpload(confirmImplausible: boolean) {
  if (!activeBlock.value || !uploadContent.value) return
  uploadSaving.value = true
  if (!confirmImplausible) uploadError.value = ''
  try {
    const opId = uploadOpId.value ?? getOrCreateOpId(activeBlock.value.id, 'upload')
    await store.uploadBoundaryFile(activeBlock.value.id, {
      operationId: opId,
      fileName: uploadFileName.value,
      content: uploadContent.value,
      confirmImplausibleGeometry: confirmImplausible,
    })
    needsUploadGeometryConfirm.value = false
    clearOpId(activeBlock.value.id, 'upload')
    const res = await blockApi.getAll()
    const updated = (res.data as Block[]).find(s => s.id === activeBlock.value!.id)
    emit('saved', updated ?? activeBlock.value)
  } catch (e) {
    if (isImplausibleGeometryError(e)) {
      needsUploadGeometryConfirm.value = true
      uploadError.value = extractErrorMessage(e, 'This boundary looks implausible.')
    } else {
      needsUploadGeometryConfirm.value = false
      uploadError.value = extractErrorMessage(e, 'Failed to upload boundary — you can retry, the same file will be resent under the same operation id.')
    }
  } finally {
    uploadSaving.value = false
  }
}
</script>

<style scoped>
.work-card {
  background: #fff;
  border-radius: 16px;
  padding: 24px 28px;
  max-width: 640px;
  width: 100%;
}
.work-card-head { margin-bottom: 18px; }
.work-card-title { font-family: 'Syne', sans-serif; font-size: 17px; font-weight: 700; color: #111827; letter-spacing: -0.01em; }
.work-card-sub { font-size: 11.5px; color: #6b7280; margin-top: 4px; line-height: 1.5; }

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

.kind-toggle { display: inline-flex; border: 1.5px solid var(--fog); border-radius: 8px; overflow: hidden; margin-bottom: 8px; }
.kind-toggle button { border: none; background: transparent; padding: 7px 14px; font-size: 12px; cursor: pointer; color: var(--stone); }
.kind-toggle button.on { background: var(--forest); color: #fff; }
.crop-chips { display: flex; gap: 6px; flex-wrap: wrap; margin-bottom: 8px; }
.crop-chip { border: 1.5px solid var(--fog); background: transparent; border-radius: 20px; padding: 5px 12px; font-size: 12px; cursor: pointer; color: var(--stone); }
.crop-chip.on { border-color: var(--palm); background: rgba(45,122,79,0.1); color: var(--forest); font-weight: 600; }
.crop-name { flex: 1; font-size: 13px; color: var(--forest); font-weight: 600; }
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

.form-actions { display: flex; justify-content: flex-end; gap: 10px; margin-top: 16px; }
.btn-add-block {
  display: flex; align-items: center; gap: 7px;
  padding: 10px 20px;
  background: #2d7a4f; color: #fff; border: none;
  border-radius: 10px; font-size: 12px; font-weight: 600;
  cursor: pointer; transition: background 0.15s;
}
.btn-add-block:hover:not(:disabled) { background: #1e5c3a; }
.btn-add-block:disabled { opacity: 0.45; cursor: not-allowed; }

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
