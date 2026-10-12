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

    <!-- Picked out of the store but not yet used or returned -->
    <OutstandingStockCard />

    <!-- A new item has no stock: it comes in on a purchase order -->
    <div v-if="addedItem" class="card added-card">
      <div>
        <b>{{ addedItem.name }}</b> is added, with no stock yet. Create an order for it; when the order is received the stock
        appears here and can be picked for work.
      </div>
      <div class="added-actions">
        <button class="btn btn-primary btn-sm" @click="orderItem(addedItem.id)">Create order</button>
        <button class="btn btn-ghost btn-sm" @click="addedItem = null">Not now</button>
      </div>
    </div>

    <!-- Filter pills + Add Item -->
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
      <button class="btn btn-primary" @click="openAddItem" aria-label="Add inventory item or stock">+ Add Item</button>
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
        <div class="inv-icon" :class="categoryClass(item.categoryName)">{{ categoryIcon(item.categoryName) }}</div>
        <div class="inv-info">
          <div class="inv-name">{{ item.name }}</div>
          <div class="inv-price" :class="{ none: !item.unitCost }">
            <template v-if="item.unitCost">
              ₦{{ fmtMoney(item.unitCost * item.packSize) }} / {{ item.packLabel }}
              <template v-if="item.packSize !== 1">({{ item.packSize }} {{ item.unit }})</template>
              · ₦{{ fmtMoney(item.unitCost) }}/{{ item.unit }}
            </template>
            <template v-else>No price set — costs can't be calculated</template>
          </div>
          <div class="inv-meta">
            <span class="cat-badge">{{ item.categoryName }}</span>
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
          <span v-if="priceWaiting(item)" class="badge badge-warn" :title="`₦${priceWaiting(item)!.requestedPricePerPack.toLocaleString()} per ${priceWaiting(item)!.requestedPackLabel} is waiting for an admin`">PRICE PENDING</span>
          <span v-if="onOrderRef(item)" class="badge badge-warn">ON ORDER {{ onOrderRef(item) }}</span>
          <span v-else-if="item.currentStock <= 0" class="badge badge-warn">NO STOCK</span>
          <span v-else-if="item.isLowStock" class="badge badge-warn">LOW STOCK</span>
        </div>
        <div class="inv-actions">
          <button v-if="item.currentStock <= 0 && !onOrderRef(item)" class="btn btn-primary btn-sm" @click="orderItem(item.id)">Order</button>
          <button class="btn btn-ghost btn-sm" @click="openPricing(item)">{{ item.unitCost ? 'Price' : 'Set price' }}</button>
          <button class="btn btn-ghost btn-sm" :disabled="item.currentStock <= 0"
                  :title="item.currentStock <= 0 ? 'No stock yet: receive a purchase order first' : ''" @click="openPick(item)">Pick Stock</button>
          <button class="btn btn-ghost btn-sm" @click="openAdjust(item)">Adjust</button>
        </div>
      </div>
    </div>

    <!-- Every `type="number"` input in this file carries `@wheel.prevent`: Chrome silently
         changes a focused number input's value by one `step` per mouse-wheel notch (the same
         behavior as clicking its native up/down spinner) — a well-known footgun where scrolling
         the page while the cursor happens to sit over a still-focused quantity/price field can
         silently corrupt data the user believes they already finished typing (e.g. typed "1000",
         two accidental wheel notches at step=0.01 silently leaves "999.98"). preventDefault on
         wheel suppresses just that spinner behavior; the input keeps focus and the page still
         scrolls normally via the outer container. -->

    <!-- Adjust modal — Phase 3: submitting creates a Pending approval request, it does not touch
         stock immediately. See closeAdjust()/submitAdjust() below. -->
    <div v-if="adjustTarget" class="modal-overlay" @click.self="closeAdjust">
      <div class="modal">
        <div class="modal-header">
          <div class="modal-title">Adjust Stock — {{ adjustTarget.name }}</div>
          <button class="modal-close" @click="closeAdjust">×</button>
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
            <input type="number" class="form-input" v-model.number="adjustQty" min="0" :disabled="!!adjustSuccess" @wheel.prevent />
          </div>
          <div class="form-group">
            <label class="form-label">Reason</label>
            <input class="form-input" placeholder="e.g. Physical recount, spillage..." v-model="adjustNotes" :disabled="!!adjustSuccess" />
          </div>
          <div v-if="adjustError" class="error-msg">{{ adjustError }}</div>
          <div v-if="adjustSuccess" class="success-msg">{{ adjustSuccess }}</div>
        </div>
        <div class="modal-footer">
          <template v-if="!adjustSuccess">
            <button class="btn btn-ghost" @click="closeAdjust" :disabled="adjusting">Cancel</button>
            <button class="btn btn-primary" @click="submitAdjust" :disabled="adjusting">
              {{ adjusting ? 'Submitting…' : 'Submit for Approval' }}
            </button>
          </template>
          <button v-else class="btn btn-primary" style="margin-left:auto;" @click="closeAdjust">Done</button>
        </div>
      </div>
    </div>

    <!-- Pick Stock modal -->
    <div v-if="pickTarget" class="modal-overlay" @click.self="closePick">
      <div class="modal">
        <div class="modal-header">
          <div class="modal-title">Pick Stock — {{ pickTarget.name }}</div>
          <button class="modal-close" @click="closePick">×</button>
        </div>
        <div class="modal-body">
          <div class="form-group">
            <label class="form-label">Available Stock</label>
            <div style="font-size:20px;font-weight:700;color:var(--forest);">
              {{ pickTarget.currentStock }} {{ pickTarget.unit }}
            </div>
          </div>
          <!-- Who performed the pick is auto-attributed server-side from the authenticated session
               (see PickStockCommandHandler's doc comment) — shown here read-only for transparency,
               not as a field the user chooses. -->
          <div class="form-group" v-if="store.currentUser">
            <label class="form-label">Picked By</label>
            <div style="font-size:13px;color:var(--stone);">{{ store.currentUser.fullName }} (you)</div>
          </div>
          <div class="form-group">
            <label class="form-label">Quantity to Pick ({{ pickTarget.unit }})</label>
            <input type="number" class="form-input" v-model.number="pickQty" min="0" step="0.01" :disabled="!!pickWarning" @wheel.prevent />
          </div>
          <div class="form-group">
            <label class="form-label">Block</label>
            <select class="form-select" v-model="pickBlockId" :disabled="!!pickWarning">
              <option value="" disabled>— Select block —</option>
              <option v-for="s in store.blocks" :key="s.id" :value="s.id">{{ s.name }}</option>
            </select>
          </div>
          <div class="form-group">
            <label class="form-label">Purpose</label>
            <input class="form-input" placeholder="e.g. Spraying Block A" v-model="pickPurpose" :disabled="!!pickWarning" />
          </div>
          <div v-if="pickError" class="error-msg">{{ pickError }}</div>
          <div v-if="pickWarning" class="warn-msg">{{ pickWarning }}</div>
        </div>
        <div class="modal-footer">
          <template v-if="!pickWarning">
            <button class="btn btn-ghost" @click="closePick" :disabled="picking">Cancel</button>
            <button class="btn btn-primary" @click="submitPick" :disabled="!canSubmitPick || picking">
              {{ picking ? 'Saving…' : 'Pick Stock' }}
            </button>
          </template>
          <button v-else class="btn btn-primary" style="margin-left:auto;" @click="closePick">Done</button>
        </div>
      </div>
    </div>

    <!-- Set price modal -->
    <div v-if="pricingTarget" class="modal-overlay" @click.self="closePricing">
      <div class="modal">
        <div class="modal-header">
          <div class="modal-title">Price — {{ pricingTarget.name }}</div>
          <button class="modal-close" @click="closePricing">×</button>
        </div>
        <div class="modal-body">
          <div class="form-row-2">
            <div class="form-group">
              <label class="form-label">Bought as</label>
              <input class="form-input" v-model="pricingLabel" :disabled="!!pricingSuccess" placeholder="bag, container…" />
            </div>
            <div class="form-group">
              <label class="form-label">Size of one {{ pricingLabel || 'pack' }} ({{ pricingTarget.unit }})</label>
              <input type="number" class="form-input" v-model.number="pricingSize" min="0" step="0.01" :disabled="!!pricingSuccess" @wheel.prevent />
            </div>
          </div>
          <div class="form-group">
            <label class="form-label">Price per {{ pricingLabel || 'pack' }} (₦)</label>
            <input type="number" class="form-input" v-model.number="pricingPrice" min="0" step="0.01" :disabled="!!pricingSuccess" @wheel.prevent />
            <div v-if="Number(pricingSize) > 0 && Number(pricingPrice) > 0" class="form-hint">
              = ₦{{ fmtMoney(Number(pricingPrice) / Number(pricingSize)) }} per {{ pricingTarget.unit }}
            </div>
          </div>
          <label class="check-row">
            <input type="checkbox" v-model="pricingBackfill" :disabled="!!pricingSuccess" />
            Also cost past activities that used this item and have no input cost yet
          </label>
          <div v-if="pricingError" class="error-msg">{{ pricingError }}</div>
          <div v-if="pricingSuccess" class="success-msg">{{ pricingSuccess }}</div>
        </div>
        <div class="modal-footer">
          <template v-if="!pricingSuccess">
            <button class="btn btn-ghost" @click="closePricing" :disabled="pricingSaving">Cancel</button>
            <button class="btn btn-primary" @click="submitPricing" :disabled="pricingSaving || !canSavePricing">
              {{ pricingSaving ? 'Sending…' : 'Send for approval' }}
            </button>
          </template>
          <button v-else class="btn btn-primary" style="margin-left:auto;" @click="closePricing">Done</button>
        </div>
      </div>
    </div>

    <!-- Add Item / Add Stock modal -->
    <div v-if="showAddItem" class="modal-overlay" @click.self="closeAddItem">
      <div class="modal">
        <div class="modal-header">
          <div class="modal-title">Add Item</div>
          <button class="modal-close" @click="closeAddItem">×</button>
        </div>
        <div class="modal-body">
          <div class="form-hint" style="margin-bottom:12px;">
            Stock comes in through a purchase order, not from here: add the item, then order it on the Orders page and receive it
            when it arrives. That is what links the payment, the stock and the cost of each activity that uses it.
          </div>

          <div class="form-group" v-if="!isExistingItemMode">
            <label class="form-label">Name</label>
            <input class="form-input" v-model="itemName" placeholder="e.g. Glyphosate 360" />
            <div v-if="matchedExistingItem" class="form-hint">
              An item named "{{ matchedExistingItem.name }}" already exists in {{ matchedExistingItem.categoryName }} —
              this will update it instead of creating a duplicate.
            </div>
          </div>

          <div class="form-group">
            <label class="form-label">Category</label>
            <select class="form-select" v-model="selectedCategoryId" :disabled="isExistingItemMode" @change="onCategorySelectChange">
              <option value="" disabled>— Select category —</option>
              <option v-for="c in store.inventoryCategories" :key="c.id" :value="c.id">{{ c.name }}</option>
              <option value="__new__">+ New Category</option>
            </select>
            <div v-if="categoriesLoading" class="form-hint">Loading categories…</div>
            <div v-if="showNewCategoryInput" class="add-row">
              <input class="form-input" v-model="newCategoryName" placeholder="New category name" style="flex:1" />
              <button class="btn btn-primary btn-sm" @click="submitNewCategory" :disabled="!newCategoryName.trim() || creatingCategory">
                {{ creatingCategory ? 'Adding…' : 'Add' }}
              </button>
            </div>
          </div>

          <div class="form-group">
            <label class="form-label">Unit</label>
            <input class="form-input" v-model="itemUnit" :disabled="isExistingItemMode" placeholder="e.g. kg, L" @input="onUnitInput" />
            <div class="form-hint">What you measure and use it in. It is bought by the pack below.</div>
          </div>

          <div class="form-row-2">
            <div class="form-group">
              <label class="form-label">Bought as</label>
              <input class="form-input" v-model="packLabel" placeholder="bag, container, drum…" />
            </div>
            <div class="form-group">
              <label class="form-label">Size of one {{ packLabel || 'pack' }} ({{ itemUnit || 'unit' }}) <span class="opt">optional</span></label>
              <input type="number" class="form-input" v-model.number="packSize" min="0" step="0.01" @wheel.prevent />
            </div>
          </div>

          <div class="form-group" v-if="!isExistingItemMode">
            <label class="form-label">Reorder Threshold</label>
            <input type="number" class="form-input" v-model.number="reorderThreshold" min="0" step="0.01" placeholder="e.g. 10" @wheel.prevent />
          </div>

          <div v-if="submitError" class="error-msg">{{ submitError }}</div>
        </div>
        <div class="modal-footer">
          <button class="btn btn-ghost" @click="closeAddItem" :disabled="submitting">Cancel</button>
          <button class="btn btn-primary" @click="submitAddItem" :disabled="!canSubmitAddItem || submitting">
            {{ submitting ? 'Saving…' : 'Add Item' }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useAgroStore } from '@/stores/agro'
import type { InventoryItem } from '@/types'
import KpiCard from '@/components/ui/KpiCard.vue'
import AlertBanner from '@/components/ui/AlertBanner.vue'
import OutstandingStockCard from '@/components/ui/OutstandingStockCard.vue'
import { extractErrorMessage } from '@/utils/http'

const store = useAgroStore()
const router = useRouter()

// ── Stock comes in on an order ──
const addedItem = ref<InventoryItem | null>(null)

/** A price asked for on this item that an admin has not decided yet. */
function priceWaiting(item: InventoryItem) {
  return store.pendingPriceChanges.find(r => r.inventoryItemId === item.id)
}

/** The number of a placed order that is waiting to bring this item in ("ORD-1042"), or ''. */
function onOrderRef(item: InventoryItem): string {
  const o = store.orders.find(x => x.status === 'Placed' && x.lineItems.some(l => l.inventoryItemId === item.id))
  return o?.reference ?? (o ? 'placed' : '')
}

/** Opens the Orders page with the create-order form already holding this item. */
function orderItem(itemId: string) {
  router.push({ path: '/orders', query: { item: itemId } })
}

const filters = computed(() => ['All', ...store.inventoryCategories.map(c => c.name)])
const activeFilter = ref('All')

const adjustTarget = ref<InventoryItem | null>(null)
const adjustQty = ref(0)
const adjustNotes = ref('')
const adjusting = ref(false)
const adjustError = ref('')
const adjustSuccess = ref('')

const filteredItems = computed(() => {
  if (activeFilter.value === 'All') return store.inventoryItems
  return store.inventoryItems.filter(i => i.categoryName === activeFilter.value)
})

const chemicalCount = computed(() => store.inventoryItems.filter(i => i.categoryName === 'Chemical').length)
const fuelEquipCount = computed(() => store.inventoryItems.filter(i => i.categoryName === 'Fuel' || i.categoryName === 'Equipment').length)

function stockPct(item: InventoryItem) {
  const max = Math.max(item.currentStock, item.reorderThreshold * 2, 1)
  return Math.min(Math.round((item.currentStock / max) * 100), 100)
}

const CATEGORY_ICONS: Record<string, string> = {
  Chemical: '🧪', Fertilizer: '🌱', Seed: '🌾', Fuel: '⛽',
  Equipment: '🔧', 'Spare Parts': '⚙️', Other: '📦', 'Farm Tools': '🛠️'
}

function categoryIcon(name: string) {
  return CATEGORY_ICONS[name] ?? '📦'
}

function categoryClass(name: string) {
  return 'cat-' + name.toLowerCase().replace(/\s+/g, '-')
}

function formatDate(iso: string) {
  return new Date(iso).toLocaleDateString('en-NG', { day: 'numeric', month: 'short' })
}

function openAdjust(item: InventoryItem) {
  adjustTarget.value = item
  adjustQty.value = item.currentStock
  adjustNotes.value = ''
  adjustError.value = ''
  adjustSuccess.value = ''
}

function closeAdjust() {
  adjustTarget.value = null
  adjustError.value = ''
  adjustSuccess.value = ''
}

// Phase 3: submitting no longer applies the change — it creates a Pending StockAdjustmentRequest
// for an admin to approve/reject. The item's displayed stock is deliberately left untouched here
// (no store.fetchInventory() call) since CurrentStock hasn't moved yet.
async function submitAdjust() {
  if (!adjustTarget.value) return
  adjusting.value = true
  adjustError.value = ''
  try {
    await store.adjustStock(adjustTarget.value.id, { newQuantity: adjustQty.value, notes: adjustNotes.value.trim() })
    adjustSuccess.value = 'Adjustment submitted for approval.'
  } catch (e) {
    adjustError.value = extractErrorMessage(e, 'Could not submit this adjustment. Please try again.')
  } finally {
    adjusting.value = false
  }
}

// ── Pick / Issue Stock (Phase 2, reworked) ──
// A genuinely separate action from Adjust: Adjust overwrites an absolute quantity (a correction),
// Pick records a real withdrawal (how much/where/why) and always leaves an Out movement behind.
// Who performed the pick is no longer chosen here — the backend auto-attributes the authenticated
// session (see PickStockCommandHandler) — this modal instead requires WHERE the stock is going (the Block).
const pickTarget = ref<InventoryItem | null>(null)
const pickQty = ref<number | ''>('')
const pickBlockId = ref('')
const pickPurpose = ref('')
const picking = ref(false)
const pickError = ref('')
const pickWarning = ref('')

const canSubmitPick = computed(() =>
  !!pickTarget.value &&
  !!pickQty.value && Number(pickQty.value) > 0 &&
  !!pickBlockId.value &&
  pickPurpose.value.trim().length > 0
)

function openPick(item: InventoryItem) {
  pickTarget.value = item
  pickQty.value = ''
  pickBlockId.value = ''
  pickPurpose.value = ''
  pickError.value = ''
  pickWarning.value = ''
  if (!store.blocks.length) store.fetchBlocks()
}

function closePick() {
  pickTarget.value = null
}

async function submitPick() {
  if (!canSubmitPick.value || !pickTarget.value) return
  picking.value = true
  pickError.value = ''
  pickWarning.value = ''
  try {
    const res = await store.pickStock(pickTarget.value.id, {
      quantity: Number(pickQty.value),
      blockId: pickBlockId.value,
      purpose: pickPurpose.value.trim()
    })
    if (res.lowStockWarning) {
      // Warning, not a failure — the pick already succeeded. Keep the modal open so it can't be
      // missed; the user dismisses it explicitly via "Done" instead of it auto-closing.
      pickWarning.value = res.lowStockWarning
    } else {
      pickTarget.value = null
    }
  } catch (e) {
    pickError.value = extractErrorMessage(e, 'Could not record this stock pick. Please try again.')
  } finally {
    picking.value = false
  }
}

// ── Add Item / Add Stock ──
const showAddItem = ref(false)
const categoriesLoading = ref(false)

const selectedExistingItemId = ref('')
const itemName = ref('')
const itemUnit = ref('')
const selectedCategoryId = ref('')
const showNewCategoryInput = ref(false)
const newCategoryName = ref('')
const creatingCategory = ref(false)
const packLabel = ref('unit')
const packSize = ref<number | ''>(1)
const packCount = ref<number | ''>('')
const pricePerPack = ref<number | ''>('')
const reorderThreshold = ref<number | ''>(0)
const submitting = ref(false)
const submitError = ref('')

const isExistingItemMode = computed(() => !!selectedExistingItemId.value)

// Informational only — the authoritative name match happens server-side (case-insensitive,
// farm-scoped) so this is purely a UX hint, not a guarantee of what the backend will do.
const matchedExistingItem = computed(() => {
  if (isExistingItemMode.value || !itemName.value.trim()) return null
  const name = itemName.value.trim().toLowerCase()
  return store.inventoryItems.find(i => i.name.toLowerCase() === name) ?? null
})

function fmtMoney(n: number) {
  return n.toLocaleString('en-NG', { maximumFractionDigits: 2 })
}

// Sensible defaults: fertilizer by the 50 kg bag, liquids by the container. Both can be changed.
function applyPackDefaults(unit: string) {
  const u = unit.trim().toLowerCase()
  if (['kg', 'kgs', 'kilogram', 'kilograms'].includes(u)) { packLabel.value = 'bag'; packSize.value = 50 }
  else if (['l', 'ltr', 'litre', 'litres', 'liter', 'liters'].includes(u)) { packLabel.value = 'container'; packSize.value = 20 }
  else { packLabel.value = 'unit'; packSize.value = 1 }
}

function onUnitInput() {
  applyPackDefaults(itemUnit.value)
}

const canSubmitAddItem = computed(() => {
  if (!(Number(packSize.value) > 0)) return false
  if (isExistingItemMode.value) {
    // Defensive: only allow submit if the picked id still resolves to a real item (it always
    // should in normal use, but guards against sending a stale/empty categoryId if it doesn't).
    return store.inventoryItems.some(i => i.id === selectedExistingItemId.value)
  }
  return itemName.value.trim().length > 0 &&
    itemUnit.value.trim().length > 0 &&
    !!selectedCategoryId.value &&
    selectedCategoryId.value !== '__new__'
})

function openAddItem() {
  selectedExistingItemId.value = ''
  itemName.value = ''
  itemUnit.value = ''
  selectedCategoryId.value = ''
  showNewCategoryInput.value = false
  newCategoryName.value = ''
  packLabel.value = 'unit'
  packSize.value = 1
  packCount.value = ''
  pricePerPack.value = ''
  reorderThreshold.value = 0
  submitError.value = ''
  showAddItem.value = true

  if (!store.inventoryCategories.length) {
    categoriesLoading.value = true
    store.fetchInventoryCategories().finally(() => { categoriesLoading.value = false })
  }
}

function closeAddItem() {
  showAddItem.value = false
}


function onCategorySelectChange() {
  showNewCategoryInput.value = selectedCategoryId.value === '__new__'
}

async function submitNewCategory() {
  if (!newCategoryName.value.trim()) return
  creatingCategory.value = true
  submitError.value = ''
  try {
    const created = await store.createInventoryCategory(newCategoryName.value.trim())
    selectedCategoryId.value = created.id
    showNewCategoryInput.value = false
    newCategoryName.value = ''
  } catch (e) {
    submitError.value = extractErrorMessage(e, 'Could not create this category. Please try again.')
  } finally {
    creatingCategory.value = false
  }
}

async function submitAddItem() {
  if (!canSubmitAddItem.value) return
  submitting.value = true
  submitError.value = ''
  try {
    const existing = isExistingItemMode.value
      ? store.inventoryItems.find(i => i.id === selectedExistingItemId.value)
      : null

    const created = await store.addOrRestockInventoryItem({
      name: existing ? existing.name : itemName.value.trim(),
      unit: existing ? existing.unit : itemUnit.value.trim(),
      categoryId: existing ? existing.categoryId : selectedCategoryId.value,
      quantity: 0,
      packSize: Number(packSize.value),
      packLabel: packLabel.value.trim() || 'unit',
      reorderThreshold: existing ? existing.reorderThreshold : Number(reorderThreshold.value || 0),
      chemicalProductId: null,
      existingItemId: existing ? existing.id : null
    })
    showAddItem.value = false
    addedItem.value = created
  } catch (e) {
    submitError.value = extractErrorMessage(e, 'Could not save this item. Please try again.')
  } finally {
    submitting.value = false
  }
}

// ── Set price on stock already in the store ──
const pricingTarget = ref<InventoryItem | null>(null)
const pricingLabel = ref('')
const pricingSize = ref<number | ''>(1)
const pricingPrice = ref<number | ''>('')
const pricingBackfill = ref(true)
const pricingSaving = ref(false)
const pricingError = ref('')
const pricingSuccess = ref('')

const canSavePricing = computed(() => Number(pricingSize.value) > 0 && Number(pricingPrice.value) > 0)

function openPricing(item: InventoryItem) {
  pricingTarget.value = item
  pricingLabel.value = item.packLabel
  // Unpriced items still on the default pack get the usual starting point for their unit.
  if (!item.unitCost && item.packSize === 1) {
    applyPackDefaults(item.unit)
    pricingLabel.value = packLabel.value
    pricingSize.value = packSize.value
  } else {
    pricingSize.value = item.packSize
  }
  pricingPrice.value = item.unitCost ? Math.round(item.unitCost * item.packSize * 100) / 100 : ''
  pricingBackfill.value = true
  pricingError.value = ''
  pricingSuccess.value = ''
}

function closePricing() {
  pricingTarget.value = null
}

async function submitPricing() {
  if (!pricingTarget.value || !canSavePricing.value) return
  pricingSaving.value = true
  pricingError.value = ''
  try {
    const res = await store.setItemPricing(pricingTarget.value.id, {
      packSize: Number(pricingSize.value),
      packLabel: pricingLabel.value.trim() || 'unit',
      pricePerPack: Number(pricingPrice.value),
      backfillActivities: pricingBackfill.value
    })
    pricingSuccess.value = `Sent for approval: ₦${res.requestedPricePerPack.toLocaleString()} per ${res.requestedPackLabel}. ` +
      'The price applies once an admin approves it.'
  } catch (e) {
    pricingError.value = extractErrorMessage(e, 'Could not save this price. Please try again.')
  } finally {
    pricingSaving.value = false
  }
}

onMounted(() => {
  store.fetchPendingPriceChanges()
  store.fetchOrders()
  store.fetchInventory()
  store.fetchInventoryCategories()
  store.fetchChemicals()
  // Blocks (for the Pick Stock modal's Block dropdown) are lazily fetched in
  // openPick() instead — no view here needs the block list before a Pick modal is opened.
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
.inv-header { padding: 16px 20px 12px; border-bottom: 1px solid var(--fog); display: flex; align-items: center; justify-content: space-between; }
.inv-row { display: flex; align-items: center; gap: 14px; padding: 12px 20px; border-bottom: 1px solid var(--fog); }
.inv-row:last-child { border-bottom: none; }
.inv-icon { width: 36px; height: 36px; border-radius: 8px; background: var(--fog); display: flex; align-items: center; justify-content: center; font-size: 16px; flex-shrink: 0; }
.inv-info { flex: 1; min-width: 0; }
.inv-name { font-size: 13px; font-weight: 600; color: var(--forest); }
.inv-price { font-family: 'JetBrains Mono', monospace; font-size: 10px; color: var(--leaf); margin-top: 2px; }
.inv-price.none { color: var(--harvest); }
.form-row-2 { display: grid; grid-template-columns: 1fr 1fr; gap: 12px; }
.opt { font-weight: 400; color: var(--stone); font-size: 10px; margin-left: 4px; }
.check-row { display: flex; align-items: flex-start; gap: 8px; font-size: 12px; color: var(--ink, #223); margin-top: 4px; cursor: pointer; }
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
/* Modals (Adjust + Add Item share the same look) */
.modal-overlay { position: fixed; inset: 0; background: rgba(10,26,16,0.6); backdrop-filter: blur(4px); z-index: 100; display: flex; align-items: center; justify-content: center; }
.modal { background: white; border-radius: 16px; width: 400px; max-height: 88vh; overflow-y: auto; box-shadow: 0 24px 64px rgba(10,26,16,0.2); }
.modal-header { padding: 20px 24px 16px; border-bottom: 1px solid var(--fog); display: flex; align-items: center; justify-content: space-between; }
.modal-title { font-family: 'Syne', sans-serif; font-weight: 700; font-size: 15px; color: var(--forest); }
.modal-close { width: 28px; height: 28px; border-radius: 6px; border: 1px solid var(--fog); background: transparent; cursor: pointer; font-size: 18px; color: var(--stone); }
.modal-body { padding: 20px 24px; }
.modal-footer { padding: 16px 24px; border-top: 1px solid var(--fog); display: flex; justify-content: space-between; }
.form-group { margin-bottom: 16px; }
.add-row { display: flex; gap: 8px; margin-top: 8px; }
.error-msg { font-size: 11.5px; color: var(--alert); line-height: 1.5; }
.warn-msg { font-size: 11.5px; color: #8A6000; line-height: 1.5; }
.success-msg { font-size: 11.5px; color: var(--palm); line-height: 1.5; }
.added-card { display: flex; align-items: center; justify-content: space-between; gap: 12px; flex-wrap: wrap; padding: 12px 16px; margin-bottom: 14px; font-size: 12.5px; color: var(--forest); background: rgba(45,122,79,0.08); }
.added-actions { display: flex; gap: 8px; }
</style>
