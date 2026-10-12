<template>
  <div class="picker">
    <div class="picker-top">
      <div class="count" :class="{ ok: enough }">
        {{ points.length }} / {{ MIN_POINTS }} points<span v-if="enough"> ✓</span>
      </div>
      <div class="picker-actions">
        <button type="button" class="pbtn gps" :disabled="locating" @click="addMyLocation">
          {{ locating ? 'Reading GPS…' : '📍 Add my location' }}
        </button>
        <button type="button" class="pbtn" :class="{ on: following }" @click="toggleFollow">{{ following ? 'Following me' : 'Show me on map' }}</button>
        <button type="button" class="pbtn" :disabled="!points.length" @click="undo">Undo</button>
        <button type="button" class="pbtn" :disabled="!points.length" @click="clear">Clear</button>
      </div>
    </div>

    <div ref="mapEl" class="map" role="application" aria-label="Map: click to mark the area that was worked" />

    <div v-if="locError" class="warn">{{ locError }}</div>
    <div v-else-if="accuracy !== null" class="hint">
      Last point read at your location (accuracy about {{ Math.round(accuracy) }} m).
      <span v-if="accuracy > 25"> That is rough: wait a few seconds in the open and add it again, or undo it.</span>
    </div>
    <!-- Work already done in this cycle, so the new area can start where it stopped -->
    <div v-if="existing && existing.status !== 'NotStarted'" class="done-note">
      <span class="sw done" /> Already done this cycle: <b>{{ existing.coveredHectares.toFixed(2) }} ha</b>
      <span v-if="existing.blockHectares"> ({{ Math.round(existing.percent) }}% of the block)</span>.
      <template v-if="existing.status === 'Complete'">The whole block is already covered: this will start a new cycle.</template>
      <template v-else><span class="sw left" /> Mark only the amber part so the work does not overlap.</template>
    </div>
    <div v-if="overlapCount" class="warn">
      {{ overlapCount }} point{{ overlapCount === 1 ? ' is' : 's are' }} inside the area already done. If that is not on purpose, move {{ overlapCount === 1 ? 'it' : 'them' }} into the amber part.
    </div>
    <div class="hint" v-if="!hasBoundary">
      This block has no boundary yet, so the area cannot be checked against its edge. Set the boundary on the Blocks page for best results.
    </div>
    <div class="hint">
      Click around the edge of the area that was worked, in order, at least {{ MIN_POINTS }} points. Or walk to each corner
      and press <b>Add my location</b>. Drag a point to adjust it.
    </div>
    <div v-if="points.length >= 3" class="stats">
      Marked area about <b>{{ areaHa.toFixed(2) }} ha</b>
      <span v-if="blockHa"> of {{ blockHa.toFixed(2) }} ha ({{ Math.min(100, Math.round(areaHa / blockHa * 100)) }}% of the block)</span>
    </div>
    <div v-if="outsideCount" class="warn">{{ outsideCount }} point{{ outsideCount === 1 ? '' : 's' }} outside the block: anything outside its edge is cut off.</div>
    <div v-if="crossing" class="warn">The lines cross each other. Mark the points in order around the edge.</div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onBeforeUnmount, watch } from 'vue'
import L from 'leaflet'
import 'leaflet/dist/leaflet.css'
import type { Block } from '@/types'
import { api, FARM_ID } from '@/services/api'

export interface LatLng { lat: number; lng: number }

const MIN_POINTS = 8

const props = defineProps<{ block: Block | null | undefined; modelValue: LatLng[]; activityType?: string }>()
const emit = defineEmits<{ 'update:modelValue': [LatLng[]] }>()

const mapEl = ref<HTMLElement | null>(null)
const points = computed(() => props.modelValue)
const enough = computed(() => points.value.length >= MIN_POINTS)

// ── my location ──
// Adds the position of the person using the device, so a supervisor can walk the sides and add a point at each. The browser
// only offers location on https or localhost, and the person has to allow it.
const locating = ref(false)
const following = ref(false)
const locError = ref('')
const accuracy = ref<number | null>(null)
let watchId: number | null = null
let youDot: L.CircleMarker | null = null
let youRing: L.Circle | null = null

function showYou(lat: number, lng: number, acc: number) {
  if (!map) return
  const ll: L.LatLngExpression = [lat, lng]
  if (!youDot) youDot = L.circleMarker(ll, { radius: 7, color: '#fff', weight: 2, fillColor: '#2f80ed', fillOpacity: 1, interactive: false }).addTo(map)
  else youDot.setLatLng(ll)
  if (!youRing) youRing = L.circle(ll, { radius: acc, color: '#2f80ed', weight: 1, fillOpacity: 0.12, interactive: false }).addTo(map)
  else { youRing.setLatLng(ll); youRing.setRadius(acc) }
}

function geoMessage(e: GeolocationPositionError) {
  if (e.code === e.PERMISSION_DENIED) return 'Location is blocked. Allow location for this site in the browser, then try again.'
  if (e.code === e.TIMEOUT) return 'Could not get a GPS fix in time. Move into the open and try again.'
  return 'Your location is not available right now.'
}

function addMyLocation() {
  locError.value = ''
  if (!('geolocation' in navigator)) { locError.value = 'This device or browser cannot share its location.'; return }
  locating.value = true
  navigator.geolocation.getCurrentPosition(
    pos => {
      locating.value = false
      const { latitude: lat, longitude: lng, accuracy: acc } = pos.coords
      accuracy.value = acc
      showYou(lat, lng, acc)
      set([...points.value, { lat, lng }])
      map?.panTo([lat, lng])
    },
    err => { locating.value = false; locError.value = geoMessage(err) },
    { enableHighAccuracy: true, timeout: 20000, maximumAge: 0 }
  )
}

function toggleFollow() {
  locError.value = ''
  if (following.value) {
    if (watchId !== null) navigator.geolocation.clearWatch(watchId)
    watchId = null
    following.value = false
    youDot?.remove(); youRing?.remove(); youDot = null; youRing = null
    return
  }
  if (!('geolocation' in navigator)) { locError.value = 'This device or browser cannot share its location.'; return }
  following.value = true
  watchId = navigator.geolocation.watchPosition(
    pos => {
      const { latitude: lat, longitude: lng, accuracy: acc } = pos.coords
      showYou(lat, lng, acc)
    },
    err => { locError.value = geoMessage(err); following.value = false },
    { enableHighAccuracy: true, maximumAge: 2000, timeout: 30000 }
  )
}

// ── what is already done this cycle ──

interface Existing {
  status: string; blockHectares: number; coveredHectares: number; percent: number
  coveredGeoJson: string | null; remainingGeoJson: string | null
}
const existing = ref<Existing | null>(null)
let existingLayer: L.FeatureGroup | null = null
let coveredPolys: LatLng[][][] = []

function polysOf(geoJson: string | null | undefined): LatLng[][][] {
  if (!geoJson) return []
  try {
    const g = JSON.parse(geoJson)
    const polys = g.type === 'Polygon' ? [g.coordinates] : g.type === 'MultiPolygon' ? g.coordinates : []
    return polys.map((rings: number[][][]) => rings.map(r => r.map(([lng, lat]) => ({ lat, lng }))))
  } catch { return [] }
}

async function loadExisting() {
  existing.value = null
  coveredPolys = []
  existingLayer?.remove(); existingLayer = null
  if (!props.block?.id || !props.activityType) return
  try {
    existing.value = (await api.get(`/api/farms/${FARM_ID}/blocks/${props.block.id}/coverage?activityType=${props.activityType}`)).data
  } catch { return } // offline or not allowed: the picker still works without the overlay
  drawExisting()
}

function drawExisting() {
  existingLayer?.remove(); existingLayer = null
  if (!map || !existing.value || existing.value.status === 'NotStarted') return
  coveredPolys = polysOf(existing.value.coveredGeoJson)
  existingLayer = L.featureGroup().addTo(map)
  const add = (json: string | null, style: L.PathOptions) => {
    if (!json) return
    try { L.geoJSON(JSON.parse(json), { style: () => style, interactive: false } as L.GeoJSONOptions).addTo(existingLayer!) } catch { /* ignore a bad shape */ }
  }
  add(existing.value.remainingGeoJson, { color: '#f5a623', weight: 1, fillColor: '#f5a623', fillOpacity: 0.3 })
  add(existing.value.coveredGeoJson, { color: '#ffffff', weight: 2, dashArray: '6 4', fillColor: '#3fb56d', fillOpacity: 0.5 })
  existingLayer.bringToBack()
}

const insideCovered = (p: LatLng) =>
  coveredPolys.some(rings => inside(p, rings[0]) && !rings.slice(1).some(h => inside(p, h)))
const overlapCount = computed(() => (existing.value ? points.value.filter(insideCovered).length : 0))

let map: L.Map | null = null
let polygonLayer: L.Polygon | null = null
let markers: L.Marker[] = []

// ── block outline ──

function ringOf(geoJson: string | null | undefined): LatLng[] | null {
  if (!geoJson) return null
  try {
    const g = JSON.parse(geoJson)
    const ring = g.type === 'Polygon' ? g.coordinates[0] : g.type === 'MultiPolygon' ? g.coordinates[0][0] : null
    return ring ? ring.map(([lng, lat]: number[]) => ({ lat, lng })) : null
  } catch { return null }
}

const blockRing = computed(() => ringOf(props.block?.boundaryGeoJson))
const hasBoundary = computed(() => !!blockRing.value && blockRing.value.length >= 3)

// ── geometry helpers (flat metres around the first point: fine at farm scale) ──

const M_LAT = 111132.92
function toXy(p: LatLng, o: LatLng) {
  return { x: (p.lng - o.lng) * M_LAT * Math.cos(o.lat * Math.PI / 180), y: (p.lat - o.lat) * M_LAT }
}
function areaOfHa(pts: LatLng[]) {
  if (pts.length < 3) return 0
  const xy = pts.map(p => toXy(p, pts[0]))
  let s = 0
  for (let i = 0; i < xy.length; i++) { const a = xy[i], b = xy[(i + 1) % xy.length]; s += a.x * b.y - b.x * a.y }
  return Math.abs(s) / 2 / 10000
}
function inside(p: LatLng, ring: LatLng[]) {
  let c = false
  for (let i = 0, j = ring.length - 1; i < ring.length; j = i++) {
    const a = ring[i], b = ring[j]
    if ((a.lat > p.lat) !== (b.lat > p.lat) && p.lng < (b.lng - a.lng) * (p.lat - a.lat) / (b.lat - a.lat) + a.lng) c = !c
  }
  return c
}
function segCross(a: LatLng, b: LatLng, c: LatLng, d: LatLng) {
  const o = (p: LatLng, q: LatLng, r: LatLng) => (q.lng - p.lng) * (r.lat - p.lat) - (q.lat - p.lat) * (r.lng - p.lng)
  return o(a, b, c) * o(a, b, d) < 0 && o(c, d, a) * o(c, d, b) < 0
}

const areaHa = computed(() => areaOfHa(points.value))
const blockHa = computed(() => (blockRing.value ? areaOfHa(blockRing.value) : 0))
const outsideCount = computed(() => (blockRing.value ? points.value.filter(p => !inside(p, blockRing.value!)).length : 0))
const crossing = computed(() => {
  const p = points.value
  if (p.length < 4) return false
  for (let i = 0; i < p.length; i++) {
    const a = p[i], b = p[(i + 1) % p.length]
    for (let j = i + 1; j < p.length; j++) {
      if (j === i || (j + 1) % p.length === i || (i + 1) % p.length === j) continue
      if (segCross(a, b, p[j], p[(j + 1) % p.length])) return true
    }
  }
  return false
})

// ── editing ──

function set(next: LatLng[]) { emit('update:modelValue', next) }
function undo() { set(points.value.slice(0, -1)) }
function clear() { set([]) }

function pinIcon(n: number) {
  return L.divIcon({ className: 'wp-pin', html: `<span>${n}</span>`, iconSize: [24, 24], iconAnchor: [12, 12] })
}

function redraw() {
  if (!map) return
  markers.forEach(m => m.remove())
  markers = points.value.map((p, i) => {
    const m = L.marker([p.lat, p.lng], { draggable: true, icon: pinIcon(i + 1) }).addTo(map!)
    m.on('dragend', () => {
      const ll = m.getLatLng()
      const next = points.value.slice()
      next[i] = { lat: ll.lat, lng: ll.lng }
      set(next)
    })
    return m
  })
  polygonLayer?.remove()
  polygonLayer = null
  if (points.value.length >= 2) {
    polygonLayer = L.polygon(points.value.map(p => [p.lat, p.lng] as [number, number]), {
      color: crossing.value ? '#c0392b' : '#2d7a4f', weight: 2, fillColor: '#2d7a4f', fillOpacity: 0.25, interactive: false
    }).addTo(map)
  }
}

onMounted(() => {
  if (!mapEl.value) return
  map = L.map(mapEl.value, { zoomControl: true, attributionControl: true })
  L.tileLayer('https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}', {
    maxZoom: 19, attribution: 'Imagery © Esri'
  }).addTo(map)

  if (hasBoundary.value) {
    const outline = L.polygon(blockRing.value!.map(p => [p.lat, p.lng] as [number, number]), {
      color: '#f5c542', weight: 3, fill: false, interactive: false
    }).addTo(map)
    map.fitBounds(outline.getBounds(), { padding: [20, 20] })
  } else if (props.block?.centroidLat != null && props.block.centroidLng != null) {
    map.setView([props.block.centroidLat, props.block.centroidLng], 17)
  } else {
    map.setView([9.08, 8.68], 6) // Nigeria
  }

  map.on('click', (e: L.LeafletMouseEvent) => set([...points.value, { lat: e.latlng.lat, lng: e.latlng.lng }]))
  redraw()
  loadExisting()
})

onBeforeUnmount(() => {
  if (watchId !== null) navigator.geolocation.clearWatch(watchId)
  map?.remove(); map = null
})
watch(points, redraw, { deep: true })
watch(() => [props.block?.id, props.activityType], loadExisting)
</script>

<style scoped>
.picker { display: flex; flex-direction: column; gap: 8px; }
.picker-top { display: flex; align-items: center; justify-content: space-between; }
.count { font-family: 'JetBrains Mono', monospace; font-size: 12px; color: var(--stone); }
.count.ok { color: var(--palm); font-weight: 600; }
.picker-actions { display: flex; gap: 6px; }
.pbtn { border: 1px solid var(--fog); background: white; border-radius: 6px; padding: 4px 10px; font-size: 12px; cursor: pointer; color: var(--forest); }
.pbtn:disabled { opacity: 0.4; cursor: default; }
.pbtn.gps { border-color: var(--palm); color: var(--forest); font-weight: 600; }
.pbtn.on { background: rgba(47,128,237,0.12); border-color: #2f80ed; }
.map { height: 320px; border-radius: 10px; overflow: hidden; border: 1px solid var(--fog); cursor: crosshair; }
.hint { font-size: 11.5px; color: var(--stone); line-height: 1.5; }
.stats { font-size: 12.5px; color: var(--forest); }
.done-note { font-size: 12.5px; color: var(--forest); background: var(--mist); border-radius: 8px; padding: 8px 10px; line-height: 1.6; }
.sw { display: inline-block; width: 11px; height: 11px; border-radius: 3px; margin: 0 4px 0 2px; vertical-align: -1px; }
.sw.done { background: #3fb56d; }
.sw.left { background: #f5a623; }
.warn { font-size: 12px; color: #8a6000; background: rgba(245,197,66,0.18); border-radius: 6px; padding: 6px 10px; }
</style>

<style>
.wp-pin span {
  display: flex; align-items: center; justify-content: center; width: 24px; height: 24px; border-radius: 50%;
  background: #2d7a4f; color: #fff; font: 600 11px 'JetBrains Mono', monospace; border: 2px solid #fff; box-shadow: 0 1px 4px rgba(0,0,0,0.4);
}
</style>
