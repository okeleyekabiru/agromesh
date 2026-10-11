<template>
  <div>
    <!-- KPIs -->
    <div class="kpi-grid">
      <KpiCard label="Total Orders" :value="`${store.orders.length}`" delta="all statuses" deltaType="mute" />
      <KpiCard label="Draft" :value="`${draftCount}`" delta="not yet placed" deltaType="mute" />
      <KpiCard label="Placed" :value="`${placedCount}`" delta="awaiting receipt" deltaType="warn" />
      <KpiCard label="Received" :value="`${receivedCount}`" delta="stock updated" deltaType="up" />
    </div>

    <!-- Filter pills + New Order -->
    <div class="toolbar-row">
      <div class="filter-row">
        <button
          v-for="f in filters"
          :key="f"
          class="filter-pill"
          :class="{ active: activeFilter === f }"
          @click="activeFilter = f"
        >{{ f }}</button>
      </div>
      <button class="btn btn-primary" @click="openCreate" aria-label="Create a new order">+ New Order</button>
    </div>

    <!-- Orders list -->
    <div v-if="store.loading" class="loading-state">Loading orders…</div>
    <div v-else-if="!filteredOrders.length" class="card" style="padding:40px;text-align:center;">
      <div style="font-size:32px;margin-bottom:12px;">🧾</div>
      <div style="font-family:'Syne',sans-serif;font-weight:700;font-size:16px;color:var(--forest);margin-bottom:4px;">No orders yet</div>
      <div style="font-size:12px;color:var(--stone);">Place an order with a supplier to restock inventory.</div>
    </div>
    <div v-else class="card">
      <div class="ord-header">
        <div class="card-title">Purchase Orders</div>
        <div class="card-label">{{ filteredOrders.length }} shown</div>
      </div>
      <div class="ord-row" v-for="order in filteredOrders" :key="order.id">
        <div class="ord-info">
          <div class="ord-supplier">{{ order.supplierName }} <span v-if="order.reference" class="ord-ref">{{ order.reference }}</span></div>
          <div class="ord-meta">
            <span>{{ order.lineItems.length }} item{{ order.lineItems.length === 1 ? '' : 's' }}</span>
            <span> · {{ formatDate(order.createdAt) }}</span>
          </div>
        </div>
        <div class="ord-total">₦{{ order.total.toLocaleString() }}</div>
        <StatBadge :variant="statusVariant(order.status)">{{ order.status }}</StatBadge>
        <StatBadge v-if="order.isPaid" variant="green">Paid</StatBadge>
        <div class="ord-actions">
          <button class="btn btn-ghost btn-sm" @click="viewOrder(order)">View</button>
        </div>
      </div>
    </div>

    <!-- Create Order modal -->
    <div v-if="showCreate" class="modal-overlay" @click.self="closeCreate">
      <div class="modal">
        <div class="modal-header">
          <div class="modal-title">New Order</div>
          <button class="modal-close" @click="closeCreate">×</button>
        </div>
        <div class="modal-body">
          <div class="form-group">
            <label class="form-label">Supplier</label>
            <select class="form-select" v-model="selectedSupplierId" @change="onSupplierSelectChange">
              <option value="" disabled>— Select supplier —</option>
              <option v-for="s in store.suppliers" :key="s.id" :value="s.id">{{ s.name }}</option>
              <option value="__new__">+ New Supplier</option>
            </select>
            <div v-if="suppliersLoading" class="form-hint">Loading suppliers…</div>
            <div v-if="showNewSupplierInput" class="new-supplier-form">
              <input class="form-input" v-model="newSupplierName" placeholder="Supplier name" />
              <input class="form-input" v-model="newSupplierPhone" placeholder="Phone (optional)" />
              <input class="form-input" v-model="newSupplierEmail" placeholder="Email (optional)" />
              <button class="btn btn-primary btn-sm" @click="submitNewSupplier" :disabled="!newSupplierName.trim() || creatingSupplier">
                {{ creatingSupplier ? 'Adding…' : 'Add Supplier' }}
              </button>
            </div>
          </div>

          <div class="form-group">
            <label class="form-label">Line Items</label>
            <div class="line-item-row" v-for="(line, idx) in lineItems" :key="idx">
              <select class="form-select" v-model="line.inventoryItemId" style="flex:2">
                <option value="" disabled>— Item —</option>
                <optgroup v-for="group in itemsByCategory" :key="group.categoryName" :label="group.categoryName">
                  <option v-for="it in group.items" :key="it.id" :value="it.id">{{ it.name }} ({{ it.unit }})</option>
                </optgroup>
              </select>
              <!-- @wheel.prevent — see InventoryView.vue's number-input comment: prevents the
                   Chrome/scroll-changes-a-focused-number-input footgun from silently corrupting a
                   quantity/price the user already finished typing. -->
              <input type="number" class="form-input" v-model.number="line.quantity" min="0.01" step="0.01" placeholder="Qty" style="flex:1" @wheel.prevent />
              <input type="number" class="form-input" v-model.number="line.unitPrice" min="0" step="0.01" placeholder="Unit ₦" style="flex:1" @wheel.prevent />
              <button class="btn-remove-line" title="Remove line" @click="removeLine(idx)" aria-label="Remove line item">×</button>
            </div>
            <button class="btn btn-ghost btn-sm" style="margin-top:8px;" @click="addLine">+ Add Line</button>
          </div>

          <div class="order-total-row">
            <span>Total</span>
            <span class="order-total-value">₦{{ createTotal.toLocaleString() }}</span>
          </div>

          <div v-if="createError" class="error-msg">{{ createError }}</div>
        </div>
        <div class="modal-footer">
          <button class="btn btn-ghost" @click="closeCreate" :disabled="creatingOrder">Cancel</button>
          <button class="btn btn-primary" @click="submitCreate" :disabled="!canSubmitCreate || creatingOrder">
            {{ creatingOrder ? 'Saving…' : 'Create Order' }}
          </button>
        </div>
      </div>
    </div>

    <OrderDetailModal v-if="viewingOrder" :order="viewingOrder" @close="viewingOrder = null" />
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useAgroStore } from '@/stores/agro'
import type { Order, OrderStatus, CreateOrderLineItemRequest } from '@/types'
import KpiCard from '@/components/ui/KpiCard.vue'
import StatBadge from '@/components/ui/StatBadge.vue'
import OrderDetailModal from '@/components/modal/OrderDetailModal.vue'
import { extractErrorMessage } from '@/utils/http'

const store = useAgroStore()

const filters: (OrderStatus | 'All')[] = ['All', 'Draft', 'Placed', 'Received']
const activeFilter = ref<OrderStatus | 'All'>('All')

const draftCount = computed(() => store.orders.filter(o => o.status === 'Draft').length)
const placedCount = computed(() => store.orders.filter(o => o.status === 'Placed').length)
const receivedCount = computed(() => store.orders.filter(o => o.status === 'Received').length)

const filteredOrders = computed(() => {
  if (activeFilter.value === 'All') return store.orders
  return store.orders.filter(o => o.status === activeFilter.value)
})

function statusVariant(status: OrderStatus) {
  if (status === 'Draft') return 'stone' as const
  if (status === 'Placed') return 'harvest' as const
  return 'green' as const
}

function formatDate(iso: string) {
  return new Date(iso).toLocaleDateString('en-NG', { day: 'numeric', month: 'short' })
}

// ── View detail ──
const viewingOrder = ref<Order | null>(null)
function viewOrder(order: Order) {
  viewingOrder.value = order
}

// ── Create Order ──
const showCreate = ref(false)
const suppliersLoading = ref(false)
const selectedSupplierId = ref('')
const showNewSupplierInput = ref(false)
const newSupplierName = ref('')
const newSupplierPhone = ref('')
const newSupplierEmail = ref('')
const creatingSupplier = ref(false)
const lineItems = ref<{ inventoryItemId: string; quantity: number | ''; unitPrice: number | '' }[]>([])
const creatingOrder = ref(false)
const createError = ref('')

// Groups the line-item picker by each item's existing categoryName (already on InventoryItemDto —
// no extra fetch needed) so ordering mirrors the category organization already used on the
// Inventory page, instead of one flat alphabetical list of every item on the farm.
const itemsByCategory = computed(() => {
  const groups = new Map<string, typeof store.inventoryItems>()
  for (const item of store.inventoryItems) {
    const key = item.categoryName || 'Other'
    if (!groups.has(key)) groups.set(key, [])
    groups.get(key)!.push(item)
  }
  return [...groups.entries()]
    .sort(([a], [b]) => a.localeCompare(b))
    .map(([categoryName, items]) => ({ categoryName, items }))
})

const createTotal = computed(() =>
  lineItems.value.reduce((sum, l) => sum + (Number(l.quantity) || 0) * (Number(l.unitPrice) || 0), 0)
)

const canSubmitCreate = computed(() => {
  if (!selectedSupplierId.value || selectedSupplierId.value === '__new__') return false
  if (!lineItems.value.length) return false
  return lineItems.value.every(l => !!l.inventoryItemId && Number(l.quantity) > 0 && Number(l.unitPrice) >= 0)
})

function openCreate() {
  selectedSupplierId.value = ''
  showNewSupplierInput.value = false
  newSupplierName.value = ''
  newSupplierPhone.value = ''
  newSupplierEmail.value = ''
  lineItems.value = [{ inventoryItemId: '', quantity: '', unitPrice: '' }]
  createError.value = ''
  showCreate.value = true

  if (!store.suppliers.length) {
    suppliersLoading.value = true
    store.fetchSuppliers().finally(() => { suppliersLoading.value = false })
  }
  if (!store.inventoryItems.length) store.fetchInventory()
}

function closeCreate() {
  showCreate.value = false
}

function onSupplierSelectChange() {
  showNewSupplierInput.value = selectedSupplierId.value === '__new__'
}

async function submitNewSupplier() {
  if (!newSupplierName.value.trim()) return
  creatingSupplier.value = true
  createError.value = ''
  try {
    const created = await store.createSupplier({
      name: newSupplierName.value.trim(),
      phone: newSupplierPhone.value.trim() || null,
      email: newSupplierEmail.value.trim() || null
    })
    selectedSupplierId.value = created.id
    showNewSupplierInput.value = false
    newSupplierName.value = ''
    newSupplierPhone.value = ''
    newSupplierEmail.value = ''
  } catch (e) {
    createError.value = extractErrorMessage(e, 'Could not create this supplier. Please try again.')
  } finally {
    creatingSupplier.value = false
  }
}

function addLine() {
  lineItems.value.push({ inventoryItemId: '', quantity: '', unitPrice: '' })
}

function removeLine(idx: number) {
  lineItems.value.splice(idx, 1)
}

async function submitCreate() {
  if (!canSubmitCreate.value) return
  creatingOrder.value = true
  createError.value = ''
  try {
    const body: { supplierId: string; lineItems: CreateOrderLineItemRequest[] } = {
      supplierId: selectedSupplierId.value,
      lineItems: lineItems.value.map(l => ({
        inventoryItemId: l.inventoryItemId,
        quantity: Number(l.quantity),
        unitPrice: Number(l.unitPrice)
      }))
    }
    await store.createOrder(body)
    showCreate.value = false
  } catch (e) {
    createError.value = extractErrorMessage(e, 'Could not create this order. Please try again.')
  } finally {
    creatingOrder.value = false
  }
}

onMounted(() => {
  store.fetchOrders()
  store.fetchSuppliers()
  if (!store.inventoryItems.length) store.fetchInventory()
})
</script>

<style scoped>
.kpi-grid { display: grid; grid-template-columns: repeat(4,1fr); gap: 12px; margin-bottom: 16px; }
.toolbar-row { display: flex; align-items: center; justify-content: space-between; gap: 12px; flex-wrap: wrap; margin-bottom: 16px; }
.filter-row { display: flex; gap: 8px; flex-wrap: wrap; }
.filter-pill { padding: 5px 12px; border-radius: 20px; border: 1.5px solid var(--fog); font-family: 'JetBrains Mono', monospace; font-size: 10px; letter-spacing: 0.08em; cursor: pointer; background: transparent; color: var(--stone); transition: all 0.15s; }
.filter-pill:hover { border-color: var(--palm); color: var(--forest); }
.filter-pill.active { background: var(--forest); border-color: var(--forest); color: white; }
.loading-state { text-align: center; padding: 60px; color: var(--stone); font-size: 13px; }
.ord-header { padding: 16px 20px 12px; border-bottom: 1px solid var(--fog); display: flex; align-items: center; justify-content: space-between; }
.ord-row { display: flex; align-items: center; gap: 14px; padding: 14px 20px; border-bottom: 1px solid var(--fog); }
.ord-row:last-child { border-bottom: none; }
.ord-info { flex: 1; min-width: 0; }
.ord-supplier { font-size: 13px; font-weight: 600; color: var(--forest); }
.ord-ref { margin-left: 6px; font-size: 11px; font-weight: 600; color: var(--stone); background: var(--fog); padding: 1px 6px; border-radius: 4px; }
.ord-meta { font-family: 'JetBrains Mono', monospace; font-size: 10px; color: var(--stone); margin-top: 2px; }
.ord-total { font-family: 'JetBrains Mono', monospace; font-size: 13px; font-weight: 600; color: var(--forest); min-width: 100px; text-align: right; }
.ord-actions { flex-shrink: 0; }
/* Modal */
.modal-overlay { position: fixed; inset: 0; background: rgba(10,26,16,0.6); backdrop-filter: blur(4px); z-index: 100; display: flex; align-items: center; justify-content: center; }
.modal { background: white; border-radius: 16px; width: 460px; max-height: 88vh; overflow-y: auto; box-shadow: 0 24px 64px rgba(10,26,16,0.2); }
.modal-header { padding: 20px 24px 16px; border-bottom: 1px solid var(--fog); display: flex; align-items: center; justify-content: space-between; }
.modal-title { font-family: 'Syne', sans-serif; font-weight: 700; font-size: 15px; color: var(--forest); }
.modal-close { width: 28px; height: 28px; border-radius: 6px; border: 1px solid var(--fog); background: transparent; cursor: pointer; font-size: 18px; color: var(--stone); }
.modal-body { padding: 20px 24px; }
.modal-footer { padding: 16px 24px; border-top: 1px solid var(--fog); display: flex; justify-content: space-between; }
.form-group { margin-bottom: 16px; }
.form-hint { font-size: 11px; color: var(--stone); margin-top: 4px; }
.new-supplier-form { display: flex; flex-direction: column; gap: 8px; margin-top: 8px; }
.line-item-row { display: flex; gap: 6px; align-items: center; margin-bottom: 8px; }
.btn-remove-line { width: 26px; height: 26px; border-radius: 6px; border: 1px solid var(--fog); background: transparent; cursor: pointer; font-size: 16px; color: var(--stone); flex-shrink: 0; }
.order-total-row { display: flex; justify-content: space-between; align-items: center; padding-top: 12px; border-top: 1px solid var(--fog); font-size: 13px; font-weight: 600; color: var(--forest); }
.order-total-value { font-family: 'JetBrains Mono', monospace; font-size: 15px; }
.error-msg { font-size: 11.5px; color: var(--alert); line-height: 1.5; margin-top: 8px; }
</style>
