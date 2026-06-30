<template>
  <div>
    <!-- Low stock alert -->
    <AlertBanner v-if="store.lowStockCount > 0">
      <strong>{{ store.lowStockCount }} item{{ store.lowStockCount > 1 ? 's' : '' }}</strong>
      below reorder threshold — check stock levels below.
    </AlertBanner>

    <!-- KPIs -->
    <div class="kpi-grid">
      <KpiCard label="Total Items" :value="`${store.inventoryItems.length}`" delta="across all categories" deltaType="mute" />
      <KpiCard label="Low Stock" :value="`${store.lowStockCount}`" :delta="store.lowStockCount > 0 ? 'Needs restocking' : 'All good'" :deltaType="store.lowStockCount > 0 ? 'warn' : 'up'" />
      <KpiCard label="Chemicals" :value="`${chemicalCount}`" delta="herbicides, insecticides, fungicides" deltaType="mute" />
      <KpiCard label="Fuel & Equipment" :value="`${fuelEquipCount}`" delta="tracked in stock" deltaType="mute" />
    </div>

    <!-- Filter pills -->
    <div class="filter-row">
      <button
        v-for="f in filters"
        :key="f"
        class="filter-pill"
        :class="{ active: activeFilter === f }"
        @click="activeFilter = f"
      >{{ f }}</button>
    </div>

    <!-- Inventory table -->
    <div v-if="store.loading" class="loading-state">Loading inventory…</div>
    <div v-else-if="!filteredItems.length" class="card" style="padding:40px;text-align:center;">
      <div style="font-size:32px;margin-bottom:12px;">📦</div>
      <div style="font-family:'Syne',sans-serif;font-weight:700;font-size:16px;color:var(--forest);margin-bottom:4px;">No inventory items</div>
      <div style="font-size:12px;color:var(--stone);">Add items to track stock levels and get low-stock alerts.</div>
    </div>
    <div v-else class="card">
      <div class="inv-header">
        <div class="card-title">Stock Levels</div>
        <div class="card-label">{{ filteredItems.length }} items shown</div>
      </div>
      <div class="inv-row" v-for="item in filteredItems" :key="item.id">
        <div class="inv-icon" :class="`cat-${item.category.toLowerCase()}`">{{ categoryIcon(item.category) }}</div>
        <div class="inv-info">
          <div class="inv-name">{{ item.name }}</div>
          <div class="inv-meta">
            <span class="cat-badge">{{ item.category }}</span>
            <span v-if="item.chemicalProductName" style="color:var(--stone);font-size:10px;">· {{ item.chemicalProductName }}</span>
            <span v-if="item.lastMovementDate" style="color:var(--stone);font-size:10px;">· last moved {{ formatDate(item.lastMovementDate) }}</span>
          </div>
        </div>
        <div class="inv-stock">
          <div class="inv-stock-num" :class="{ low: item.isLowStock }">
            {{ item.currentStock.toLocaleString() }} <span class="inv-unit">{{ item.unit }}</span>
          </div>
          <div class="inv-bar-wrap">
            <div class="inv-bar" :style="{ width: stockPct(item) + '%', background: item.isLowStock ? 'var(--harvest)' : 'var(--palm)' }" />
          </div>
          <div class="inv-threshold">threshold: {{ item.reorderThreshold }} {{ item.unit }}</div>
        </div>
        <div class="inv-badges">
          <span v-if="item.isLowStock" class="badge badge-warn">LOW STOCK</span>
        </div>
        <div class="inv-actions">
          <button class="btn btn-ghost btn-sm" @click="openAdjust(item)">Adjust</button>
        </div>
      </div>
    </div>

    <!-- Adjust modal -->
    <div v-if="adjustTarget" class="modal-overlay" @click.self="adjustTarget = null">
      <div class="modal">
        <div class="modal-header">
          <div class="modal-title">Adjust Stock — {{ adjustTarget.name }}</div>
          <button class="modal-close" @click="adjustTarget = null">×</button>
        </div>
        <div class="modal-body">
          <div class="form-group">
            <label class="form-label">Current Stock</label>
            <div style="font-size:20px;font-weight:700;color:var(--forest);">
              {{ adjustTarget.currentStock }} {{ adjustTarget.unit }}
            </div>
          </div>
          <div class="form-group">
            <label class="form-label">New Quantity ({{ adjustTarget.unit }})</label>
            <input type="number" class="form-input" v-model.number="adjustQty" min="0" />
          </div>
          <div class="form-group">
            <label class="form-label">Reason</label>
            <input class="form-input" placeholder="e.g. Physical recount, spillage..." v-model="adjustNotes" />
          </div>
        </div>
        <div class="modal-footer">
          <button class="btn btn-ghost" @click="adjustTarget = null">Cancel</button>
          <button class="btn btn-primary" @click="submitAdjust" :disabled="adjusting">
            {{ adjusting ? 'Saving…' : 'Save Adjustment' }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useAgroStore } from '@/stores/agro'
import type { InventoryItem } from '@/types'
import KpiCard from '@/components/ui/KpiCard.vue'
import AlertBanner from '@/components/ui/AlertBanner.vue'

const store = useAgroStore()

const filters = ['All', 'Chemical', 'Fertilizer', 'Seed', 'Fuel', 'Equipment', 'SpareParts']
const activeFilter = ref('All')

const adjustTarget = ref<InventoryItem | null>(null)
const adjustQty = ref(0)
const adjustNotes = ref('')
const adjusting = ref(false)

const filteredItems = computed(() => {
  if (activeFilter.value === 'All') return store.inventoryItems
  return store.inventoryItems.filter(i => i.category === activeFilter.value)
})

const chemicalCount = computed(() => store.inventoryItems.filter(i => i.category === 'Chemical').length)
const fuelEquipCount = computed(() => store.inventoryItems.filter(i => i.category === 'Fuel' || i.category === 'Equipment').length)

function stockPct(item: InventoryItem) {
  const max = Math.max(item.currentStock, item.reorderThreshold * 2, 1)
  return Math.min(Math.round((item.currentStock / max) * 100), 100)
}

function categoryIcon(cat: string) {
  const map: Record<string, string> = {
    Chemical: '🧪', Fertilizer: '🌱', Seed: '🌾',
    Fuel: '⛽', Equipment: '🔧', SpareParts: '⚙️', Other: '📦'
  }
  return map[cat] ?? '📦'
}

function formatDate(iso: string) {
  return new Date(iso).toLocaleDateString('en-NG', { day: 'numeric', month: 'short' })
}

function openAdjust(item: InventoryItem) {
  adjustTarget.value = item
  adjustQty.value = item.currentStock
  adjustNotes.value = ''
}

async function submitAdjust() {
  if (!adjustTarget.value) return
  adjusting.value = true
  try {
    await store.adjustStock(adjustTarget.value.id, { newQuantity: adjustQty.value, notes: adjustNotes.value })
    adjustTarget.value = null
  } catch (e) {
    console.error('Adjust failed', e)
  } finally {
    adjusting.value = false
  }
}

onMounted(() => {
  store.fetchInventory()
  store.fetchChemicals()
})
</script>

<style scoped>
.kpi-grid { display: grid; grid-template-columns: repeat(4,1fr); gap: 12px; margin-bottom: 16px; }
.filter-row { display: flex; gap: 8px; margin-bottom: 16px; flex-wrap: wrap; }
.filter-pill { padding: 5px 12px; border-radius: 20px; border: 1.5px solid var(--fog); font-family: 'JetBrains Mono', monospace; font-size: 10px; letter-spacing: 0.08em; cursor: pointer; background: transparent; color: var(--stone); transition: all 0.15s; }
.filter-pill:hover { border-color: var(--palm); color: var(--forest); }
.filter-pill.active { background: var(--forest); border-color: var(--forest); color: white; }
.loading-state { text-align: center; padding: 60px; color: var(--stone); font-size: 13px; }
.inv-header { padding: 16px 20px 12px; border-bottom: 1px solid var(--fog); display: flex; align-items: center; justify-content: space-between; }
.inv-row { display: flex; align-items: center; gap: 14px; padding: 12px 20px; border-bottom: 1px solid var(--fog); }
.inv-row:last-child { border-bottom: none; }
.inv-icon { width: 36px; height: 36px; border-radius: 8px; background: var(--fog); display: flex; align-items: center; justify-content: center; font-size: 16px; flex-shrink: 0; }
.inv-info { flex: 1; min-width: 0; }
.inv-name { font-size: 13px; font-weight: 600; color: var(--forest); }
.inv-meta { display: flex; align-items: center; gap: 6px; margin-top: 2px; }
.cat-badge { font-family: 'JetBrains Mono', monospace; font-size: 8px; letter-spacing: 0.1em; text-transform: uppercase; padding: 2px 6px; background: var(--fog); border-radius: 4px; color: var(--stone); }
.inv-stock { min-width: 160px; }
.inv-stock-num { font-family: 'JetBrains Mono', monospace; font-size: 13px; font-weight: 600; color: var(--forest); }
.inv-stock-num.low { color: var(--harvest); }
.inv-unit { font-size: 10px; font-weight: 400; color: var(--stone); }
.inv-bar-wrap { height: 4px; background: var(--fog); border-radius: 2px; margin: 4px 0; overflow: hidden; }
.inv-bar { height: 100%; border-radius: 2px; transition: width 0.4s; }
.inv-threshold { font-family: 'JetBrains Mono', monospace; font-size: 8px; color: var(--stone); }
.inv-badges { min-width: 80px; }
.badge { display: inline-block; font-family: 'JetBrains Mono', monospace; font-size: 8px; letter-spacing: 0.1em; padding: 2px 6px; border-radius: 4px; }
.badge-warn { background: #FFF0C0; color: #8A6000; border: 1px solid #F5D77B; }
.inv-actions { display: flex; gap: 6px; flex-shrink: 0; }
/* Adjust modal */
.modal-overlay { position: fixed; inset: 0; background: rgba(10,26,16,0.6); backdrop-filter: blur(4px); z-index: 100; display: flex; align-items: center; justify-content: center; }
.modal { background: white; border-radius: 16px; width: 400px; box-shadow: 0 24px 64px rgba(10,26,16,0.2); }
.modal-header { padding: 20px 24px 16px; border-bottom: 1px solid var(--fog); display: flex; align-items: center; justify-content: space-between; }
.modal-title { font-family: 'Syne', sans-serif; font-weight: 700; font-size: 15px; color: var(--forest); }
.modal-close { width: 28px; height: 28px; border-radius: 6px; border: 1px solid var(--fog); background: transparent; cursor: pointer; font-size: 18px; color: var(--stone); }
.modal-body { padding: 20px 24px; }
.modal-footer { padding: 16px 24px; border-top: 1px solid var(--fog); display: flex; justify-content: space-between; }
.form-group { margin-bottom: 16px; }
</style>
