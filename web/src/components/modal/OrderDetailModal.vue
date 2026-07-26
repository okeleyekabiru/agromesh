<template>
  <div class="modal-overlay" @click.self="$emit('close')">
    <div class="modal-wrap">
      <div class="modal">
        <div class="modal-header">
          <div class="modal-title">Order — {{ localOrder.supplierName }}</div>
          <button class="modal-close-inline" @click="$emit('close')">×</button>
        </div>
        <div class="modal-body">
          <div class="status-row">
            <StatBadge :variant="statusVariant(localOrder.status)">{{ localOrder.status }}</StatBadge>
            <span class="created-at">Created {{ formatDate(localOrder.createdAt) }}</span>
          </div>

          <div class="line-item-list">
            <div class="line-item-header">
              <span>Item</span>
              <span>Qty</span>
              <span>Unit Price</span>
              <span>Line Total</span>
            </div>
            <div class="line-item-detail-row" v-for="line in localOrder.lineItems" :key="line.inventoryItemId">
              <span>{{ line.inventoryItemName }} ({{ line.unit }})</span>
              <span>{{ line.quantity.toLocaleString() }}</span>
              <span>₦{{ line.unitPrice.toLocaleString() }}</span>
              <span>₦{{ line.lineTotal.toLocaleString() }}</span>
            </div>
          </div>

          <div class="order-total-row">
            <span>Total</span>
            <span class="order-total-value">₦{{ localOrder.total.toLocaleString() }}</span>
          </div>

          <div v-if="actionError" class="error-msg">{{ actionError }}</div>
        </div>
        <div class="modal-footer">
          <button class="btn btn-ghost btn-sm" @click="downloadInvoice" :disabled="downloading" aria-label="Download invoice">
            {{ downloading ? 'Preparing…' : 'Download Invoice' }}
          </button>
          <div class="footer-actions">
            <button
              v-if="localOrder.status === 'Draft'"
              class="btn btn-primary btn-sm"
              @click="place"
              :disabled="!!busy"
              aria-label="Place order"
            >{{ busy === 'place' ? 'Placing…' : 'Place Order' }}</button>
            <button
              v-if="localOrder.status === 'Placed'"
              class="btn btn-primary btn-sm"
              @click="receive"
              :disabled="!!busy"
              aria-label="Receive order"
            >{{ busy === 'receive' ? 'Receiving…' : 'Receive Order' }}</button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useAgroStore } from '@/stores/agro'
import StatBadge from '@/components/ui/StatBadge.vue'
import { extractErrorMessage } from '@/utils/http'
import type { Order, OrderStatus } from '@/types'

const props = defineProps<{ order: Order }>()
defineEmits<{ close: [] }>()
const store = useAgroStore()

// Local copy so a Place/Receive transition (or a re-downloaded invoice) reflects immediately in
// this modal without needing the whole Orders list to re-render — same pattern BlockModal.vue uses
// for its local block list.
const localOrder = ref<Order>({ ...props.order })

const busy = ref<'place' | 'receive' | undefined>()
const actionError = ref('')
const downloading = ref(false)

function statusVariant(status: OrderStatus) {
  if (status === 'Draft') return 'stone' as const
  if (status === 'Placed') return 'harvest' as const
  return 'green' as const
}

function formatDate(iso: string) {
  return new Date(iso).toLocaleDateString('en-NG', { day: 'numeric', month: 'short' })
}

async function place() {
  busy.value = 'place'
  actionError.value = ''
  try {
    localOrder.value = await store.placeOrder(localOrder.value.id)
  } catch (e) {
    actionError.value = extractErrorMessage(e, 'Could not place this order. Please try again.')
  } finally {
    busy.value = undefined
  }
}

async function receive() {
  busy.value = 'receive'
  actionError.value = ''
  try {
    localOrder.value = await store.receiveOrder(localOrder.value.id)
  } catch (e) {
    actionError.value = extractErrorMessage(e, 'Could not receive this order. Please try again.')
  } finally {
    busy.value = undefined
  }
}

async function downloadInvoice() {
  downloading.value = true
  actionError.value = ''
  try {
    await store.downloadOrderInvoice(localOrder.value.id)
  } catch (e) {
    actionError.value = extractErrorMessage(e, 'Could not download the invoice. Please try again.')
  } finally {
    downloading.value = false
  }
}
</script>

<style scoped>
.modal-overlay {
  position: fixed; inset: 0; background: rgba(10,26,16,0.6); backdrop-filter: blur(4px);
  z-index: 200; display: flex; align-items: flex-start; justify-content: center;
  padding: 48px 24px; overflow-y: auto;
}
.modal-wrap { position: relative; }
.modal { background: white; border-radius: 16px; width: 460px; box-shadow: 0 24px 64px rgba(10,26,16,0.2); }
.modal-header { padding: 20px 24px 16px; border-bottom: 1px solid var(--fog); display: flex; align-items: center; justify-content: space-between; }
.modal-title { font-family: 'Syne', sans-serif; font-weight: 700; font-size: 15px; color: var(--forest); }
.modal-close-inline { width: 28px; height: 28px; border-radius: 6px; border: 1px solid var(--fog); background: transparent; cursor: pointer; font-size: 18px; color: var(--stone); }
.modal-body { padding: 20px 24px; }
.modal-footer { padding: 16px 24px; border-top: 1px solid var(--fog); display: flex; align-items: center; justify-content: space-between; gap: 8px; }
.footer-actions { display: flex; gap: 8px; }

.status-row { display: flex; align-items: center; gap: 10px; margin-bottom: 16px; }
.created-at { font-family: 'JetBrains Mono', monospace; font-size: 10px; color: var(--stone); }

.line-item-list { border: 1px solid var(--fog); border-radius: 10px; overflow: hidden; margin-bottom: 12px; }
.line-item-header, .line-item-detail-row {
  display: grid; grid-template-columns: 2.2fr 0.8fr 1fr 1fr; gap: 8px; padding: 8px 12px; font-size: 11.5px;
}
.line-item-header { background: #f9fafb; font-family: 'JetBrains Mono', monospace; font-size: 9px; letter-spacing: 0.06em; text-transform: uppercase; color: var(--stone); }
.line-item-detail-row { border-top: 1px solid var(--fog); color: var(--forest); }
.line-item-detail-row span:not(:first-child) { font-family: 'JetBrains Mono', monospace; text-align: right; }
.line-item-header span:not(:first-child) { text-align: right; }

.order-total-row { display: flex; justify-content: space-between; align-items: center; padding-top: 12px; border-top: 1px solid var(--fog); font-size: 13px; font-weight: 600; color: var(--forest); }
.order-total-value { font-family: 'JetBrains Mono', monospace; font-size: 15px; }
.error-msg { font-size: 11.5px; color: var(--alert); line-height: 1.5; margin-top: 12px; }
</style>
