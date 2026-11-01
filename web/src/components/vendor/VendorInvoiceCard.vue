<template>
  <article class="vi" :aria-label="`Vendor invoice ${invoice.invoiceNumber} from ${invoice.vendorName}`">
    <div class="vi-top">
      <div class="vi-main">
        <div class="vi-title">
          <span class="vi-number">{{ invoice.invoiceNumber }}</span>
          <span class="vi-vendor">{{ invoice.vendorName }}</span>
        </div>
        <div class="vi-meta">
          <span>Invoice date {{ formatDay(invoice.invoiceDate) }}</span>
          <span v-if="invoice.dueDate"> · Due {{ formatDay(invoice.dueDate) }}</span>
          <span v-if="showOrder && invoice.orderReference"> · Order {{ invoice.orderReference }}</span>
        </div>
      </div>
      <div class="vi-amount">{{ formatNaira(invoice.amount) }}</div>
    </div>

    <div class="vi-badges">
      <span class="pill" :class="invoice.status === 'Paid' ? 'pill-paid' : 'pill-unpaid'">{{ invoice.status }}</span>
      <span v-if="overdue" class="pill pill-overdue">Overdue</span>
      <span v-if="invoice.status === 'Paid' && invoice.paidAt" class="vi-paid-on">Paid {{ formatDay(invoice.paidAt) }}</span>
    </div>

    <p v-if="mismatch" class="vi-mismatch" role="note">
      <span aria-hidden="true">⚠ </span>{{ mismatch }}
    </p>
    <p v-if="invoice.notes" class="vi-notes">{{ invoice.notes }}</p>
    <div class="vi-file">{{ invoice.fileName }} · {{ formatBytes(invoice.fileSize) }}</div>

    <div class="vi-actions">
      <button type="button" class="btn btn-ghost btn-sm" :disabled="busy" :aria-label="`View file for invoice ${invoice.invoiceNumber}`" @click="$emit('view', invoice)">View</button>
      <button type="button" class="btn btn-ghost btn-sm" :disabled="busy" :aria-label="`Download file for invoice ${invoice.invoiceNumber}`" @click="$emit('download', invoice)">Download</button>
      <button
        v-if="canManage"
        type="button"
        class="btn btn-ghost btn-sm"
        :disabled="busy"
        :aria-label="`${invoice.status === 'Paid' ? 'Mark unpaid' : 'Mark paid'}: invoice ${invoice.invoiceNumber}`"
        @click="$emit('toggle', invoice)"
      >{{ invoice.status === 'Paid' ? 'Mark unpaid' : 'Mark paid' }}</button>
      <button
        v-if="canManage"
        type="button"
        class="btn btn-ghost btn-sm vi-del"
        :disabled="busy"
        :aria-label="`Delete invoice ${invoice.invoiceNumber}`"
        @click="$emit('delete', invoice)"
      >Delete</button>
    </div>
  </article>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useAgroStore } from '@/stores/agro'
import { canManageVendorInvoice } from '@/utils/roles'
import { formatDay, formatNaira } from '@/utils/money'
import { formatBytes } from '@/utils/files'
import { isShownOverdue, vendorMismatch } from '@/utils/vendorInvoice'
import type { VendorInvoice } from '@/types'

const props = defineProps<{ invoice: VendorInvoice; busy?: boolean; showOrder?: boolean }>()
defineEmits<{
  view: [VendorInvoice]
  download: [VendorInvoice]
  toggle: [VendorInvoice]
  delete: [VendorInvoice]
}>()

const store = useAgroStore()
const canManage = computed(() => canManageVendorInvoice(store.currentUser?.role))
const overdue = computed(() => isShownOverdue(props.invoice))
const mismatch = computed(() => vendorMismatch(props.invoice))
</script>

<style scoped>
.vi { padding: 14px 18px; border-bottom: 1px solid var(--fog); }
.vi:last-child { border-bottom: none; }
.vi-top { display: flex; justify-content: space-between; gap: 12px; align-items: flex-start; }
.vi-main { min-width: 0; }
.vi-title { display: flex; gap: 8px; align-items: baseline; flex-wrap: wrap; }
.vi-number { font-weight: 700; font-size: 13px; color: var(--forest); overflow-wrap: anywhere; }
.vi-vendor { font-size: 12.5px; color: var(--stone); overflow-wrap: anywhere; }
.vi-meta { font-size: 12px; color: var(--stone); margin-top: 2px; }
.vi-amount { font-weight: 800; font-size: 14px; color: var(--forest); white-space: nowrap; }
.vi-badges { display: flex; gap: 6px; align-items: center; flex-wrap: wrap; margin-top: 8px; }
.pill { font-size: 11px; font-weight: 700; padding: 3px 9px; border-radius: 999px; letter-spacing: 0.04em; }
.pill-paid { background: rgba(45,122,79,0.12); color: #1F6B42; }
.pill-unpaid { background: rgba(232,168,56,0.18); color: #7A5200; }
.pill-overdue { background: rgba(180,35,35,0.1); color: #A02020; }
.vi-paid-on { font-size: 11px; color: var(--stone); }
.vi-mismatch { margin: 8px 0 0; padding: 7px 10px; border-radius: 8px; background: rgba(232,168,56,0.15); color: #6B4700; font-size: 12px; line-height: 1.5; }
.vi-notes { margin: 8px 0 0; font-size: 12px; color: var(--forest); line-height: 1.5; overflow-wrap: anywhere; }
.vi-file { margin-top: 6px; font-size: 11px; color: var(--stone); overflow-wrap: anywhere; }
.vi-actions { display: flex; gap: 6px; flex-wrap: wrap; margin-top: 10px; }
.vi-del { color: #B42323; }
</style>
