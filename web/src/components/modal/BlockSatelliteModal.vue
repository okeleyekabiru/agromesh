<template>
  <div class="modal-overlay" @click.self="$emit('close')">
    <div class="modal">
      <div class="modal-header">
        <div class="modal-title">{{ block.name }} · satellite</div>
        <button class="modal-close-inline" aria-label="Close" @click="$emit('close')">×</button>
      </div>

      <div class="modal-body">
        <div v-if="loading" class="note">Loading…</div>
        <div v-else-if="error" class="error-msg">{{ error }}</div>
        <div v-else-if="view && !view.configured" class="note">Satellite monitoring is not connected yet. Add the Copernicus client id and secret to the API settings.</div>
        <div v-else-if="view && !view.hasBoundary" class="note">
          This block has no usable boundary, so there is no picture to show. Redraw its boundary on the Blocks page.
        </div>
        <div v-else-if="view && !view.dates.length" class="note">
          No satellite readings for this block yet. Press <b>Refresh</b> in the satellite bar on the Blocks page, then open this again.
        </div>

        <template v-else-if="view">
          <div class="tabs">
            <button v-for="l in LAYERS" :key="l.key" class="tab" :class="{ on: layer === l.key }" @click="layer = l.key">{{ l.label }}</button>
          </div>

          <div class="stepper">
            <button class="step" :disabled="idx >= view.dates.length - 1" @click="idx++" aria-label="Earlier">‹</button>
            <select v-model.number="idx" class="date-select">
              <option v-for="(d, i) in view.dates" :key="d.date" :value="i">
                {{ fmt(d.date) }} – {{ fmt(addDays(d.date, 4)) }} · {{ Math.round(d.clearShare * 100) }}% clear
              </option>
            </select>
            <button class="step" :disabled="idx <= 0" @click="idx--" aria-label="Later">›</button>
          </div>

          <div class="map-wrap">
            <div ref="mapEl" class="map" />
            <div v-if="imgLoading" class="map-note">Fetching the picture…</div>
          </div>
          <div v-if="imgError" class="error-msg">{{ imgError }}</div>

          <div class="legend">
            <template v-if="layer === 'Ndvi'">
              <span v-for="s in NDVI_LEGEND" :key="s.label"><i class="sw" :style="{ background: s.color }" />{{ s.label }}</span>
            </template>
            <template v-else-if="layer === 'Ndmi'">
              <span v-for="s in NDMI_LEGEND" :key="s.label"><i class="sw" :style="{ background: s.color }" />{{ s.label }}</span>
            </template>
            <span v-else>Natural colour. White patches are cloud.</span>
          </div>
          <div class="fine">
            {{ layer === 'Ndvi' ? 'Greenness (NDVI): how dense and healthy the crop is.' : layer === 'Ndmi' ? 'Leaf water (NDMI): how much water the leaves hold.' : 'Sentinel-2 picture, 10 m detail.' }}
            Cloudy areas are left clear on the two index views. One picture covers a 5-day window.
          </div>
        </template>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, watch, nextTick, onMounted, onBeforeUnmount } from 'vue'
import L from 'leaflet'
import 'leaflet/dist/leaflet.css'
import { api, FARM_ID } from '@/services/api'
import { extractErrorMessage } from '@/utils/http'
import type { Block } from '@/types'

interface View {
  configured: boolean; hasBoundary: boolean; bounds: number[] | null
  dates: { date: string; ndvi: number; ndmi: number; clearShare: number }[]
}

const props = defineProps<{ block: Block }>()
defineEmits<{ close: [] }>()

const LAYERS = [
  { key: 'TrueColour', label: 'True colour' },
  { key: 'Ndvi', label: 'Greenness (NDVI)' },
  { key: 'Ndmi', label: 'Leaf water (NDMI)' }
]
const NDVI_LEGEND = [
  { color: '#8c7359', label: 'Bare' }, { color: '#d9c78c', label: 'Sparse' }, { color: '#bfd973', label: 'Light' },
  { color: '#66b84d', label: 'Healthy' }, { color: '#268c33', label: 'Dense' }, { color: '#055a1f', label: 'Very dense' }
]
const NDMI_LEGEND = [
  { color: '#a64d33', label: 'Very dry' }, { color: '#e6a640', label: 'Dry' }, { color: '#f2e673', label: 'Moderate' },
  { color: '#8cd1d9', label: 'Moist' }, { color: '#4099d9', label: 'Wet' }, { color: '#0d40a6', label: 'Very wet' }
]

const view = ref<View | null>(null)
const loading = ref(true)
const error = ref('')
const layer = ref('Ndvi')
const idx = ref(0)
const imgLoading = ref(false)
const imgError = ref('')
const mapEl = ref<HTMLElement | null>(null)

let map: L.Map | null = null
let overlay: L.ImageOverlay | null = null
let objectUrl: string | null = null
let seq = 0 // only the newest request may draw: a slow picture must not replace a newer one

const fmt = (iso: string) => new Date(iso).toLocaleDateString('en-GB', { day: 'numeric', month: 'short' })
const addDays = (iso: string, n: number) => new Date(new Date(iso).getTime() + n * 86400000).toISOString()

async function load() {
  loading.value = true
  error.value = ''
  try {
    view.value = (await api.get(`/api/farms/${FARM_ID}/blocks/${props.block.id}/satellite`)).data
    // Start on the newest picture that is mostly clear; fall back to the newest one.
    const clear = view.value!.dates.findIndex(d => d.clearShare >= 0.3)
    idx.value = clear >= 0 ? clear : 0
  } catch (e) {
    error.value = extractErrorMessage(e, 'Could not load the satellite pictures.')
  } finally {
    loading.value = false
  }
  await nextTick()
  buildMap()
  await showImage()
}

function buildMap() {
  map?.remove()
  map = null
  overlay = null
  if (!mapEl.value || !view.value?.bounds) return
  map = L.map(mapEl.value, { zoomControl: true })
  L.tileLayer('https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}', {
    maxZoom: 19, attribution: 'Imagery © Esri · Sentinel-2 © Copernicus'
  }).addTo(map)
  const [s, w, n, e] = view.value.bounds
  map.fitBounds([[s, w], [n, e]], { padding: [24, 24] })
  try {
    if (props.block.boundaryGeoJson)
      L.geoJSON(JSON.parse(props.block.boundaryGeoJson), { style: () => ({ color: '#ffffff', weight: 2, fill: false }), interactive: false } as L.GeoJSONOptions).addTo(map)
  } catch { /* the picture still shows without the outline */ }
}

async function showImage() {
  const v = view.value
  if (!map || !v?.bounds || !v.dates.length) return
  const mine = ++seq
  imgLoading.value = true
  imgError.value = ''
  try {
    const date = v.dates[idx.value].date.slice(0, 10)
    const res = await api.get(`/api/farms/${FARM_ID}/blocks/${props.block.id}/satellite/image`, {
      params: { layer: layer.value, date }, responseType: 'blob'
    })
    if (mine !== seq || !map) return
    const url = URL.createObjectURL(res.data as Blob)
    const [s, w, n, e] = v.bounds
    overlay?.remove()
    overlay = L.imageOverlay(url, [[s, w], [n, e]], { opacity: 0.92, interactive: false }).addTo(map)
    if (objectUrl) URL.revokeObjectURL(objectUrl)
    objectUrl = url
  } catch (e: unknown) {
    if (mine !== seq) return
    imgError.value = await blobError(e)
  } finally {
    if (mine === seq) imgLoading.value = false
  }
}

// A failed picture request comes back as a blob; read the server's message out of it.
async function blobError(e: unknown): Promise<string> {
  const data = (e as { response?: { data?: unknown } })?.response?.data
  if (data instanceof Blob) {
    try {
      const j = JSON.parse(await data.text())
      if (j?.detail) return String(j.detail)
    } catch { /* fall through */ }
  }
  return extractErrorMessage(e, 'Could not fetch this picture. Try another date.')
}

onMounted(load)
onBeforeUnmount(() => { map?.remove(); map = null; if (objectUrl) URL.revokeObjectURL(objectUrl) })
watch([layer, idx], showImage)
</script>

<style scoped>
.modal-overlay {
  position: fixed; inset: 0; background: rgba(10,26,16,0.6); backdrop-filter: blur(4px);
  z-index: 200; display: flex; align-items: flex-start; justify-content: center; padding: 40px 24px; overflow-y: auto;
}
.modal { background: white; border-radius: 16px; width: 700px; max-width: 100%; box-shadow: 0 24px 64px rgba(10,26,16,0.2); }
.modal-header { padding: 18px 24px 14px; border-bottom: 1px solid var(--fog); display: flex; align-items: center; justify-content: space-between; }
.modal-title { font-family: 'Syne', sans-serif; font-weight: 700; font-size: 15px; color: var(--forest); }
.modal-close-inline { width: 28px; height: 28px; border-radius: 6px; border: 1px solid var(--fog); background: transparent; cursor: pointer; font-size: 18px; color: var(--stone); }
.modal-body { padding: 16px 24px 22px; }
.tabs { display: flex; gap: 6px; flex-wrap: wrap; margin-bottom: 12px; }
.tab { border: 1.5px solid var(--fog); background: transparent; border-radius: 20px; padding: 5px 14px; font-size: 12px; cursor: pointer; color: var(--stone); }
.tab.on { border-color: var(--palm); background: rgba(45,122,79,0.1); color: var(--forest); font-weight: 600; }
.stepper { display: flex; gap: 6px; margin-bottom: 12px; }
.step { width: 34px; border: 1px solid var(--fog); background: white; border-radius: 8px; font-size: 18px; cursor: pointer; color: var(--forest); }
.step:disabled { opacity: 0.35; cursor: default; }
.date-select { flex: 1; border: 1px solid var(--fog); border-radius: 8px; padding: 7px 10px; font-size: 12.5px; color: var(--forest); background: white; }
.map-wrap { position: relative; }
.map { height: 400px; border-radius: 10px; overflow: hidden; border: 1px solid var(--fog); }
.map-note { position: absolute; left: 10px; bottom: 10px; z-index: 500; background: rgba(13,43,31,0.85); color: #fff; font-size: 11.5px; padding: 5px 10px; border-radius: 6px; }
.legend { display: flex; gap: 12px; flex-wrap: wrap; font-size: 11.5px; color: var(--stone); margin-top: 10px; }
.sw { display: inline-block; width: 12px; height: 12px; border-radius: 3px; margin-right: 5px; vertical-align: -1px; }
.fine { font-size: 11.5px; color: var(--stone); margin-top: 6px; line-height: 1.5; }
.note { font-size: 12.5px; color: var(--stone); padding: 8px 0; line-height: 1.6; }
.error-msg { font-size: 12px; color: var(--alert); margin-top: 8px; }
</style>
