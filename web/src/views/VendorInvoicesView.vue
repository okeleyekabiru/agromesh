<template>
  <div class="vv-page">
    <div class="toolbar-row">
      <div class="filter-row" role="group" aria-label="Filter by payment status">
        <button
          v-for="f in filters"
          :key="f.value"
          type="button"
          class="vv-pill"
          :class="{ active: filter === f.value }"
          :aria-pressed="filter === f.value"
          @click="setFilter(f.value)"
        >{{ f.label }}</button>
      </div>
      <RouterLink :to="{ name: 'orders' }" class="vv-link">Upload from a received order →</RouterLink>
    </div>

    <div v-if="orderFilter" class="vv-chip">
      Showing one order only.
      <button type="button" class="vv-chip-btn" @click="clearOrderFilter">Show all</button>
    </div>

    <div class="vv-status" role="status" aria-live="polite">
      <span v-if="notice" class="vv-notice">{{ notice }}</span>
    </div>
    <div v-if="actions.error.value" class="vv-error" role="alert">{{ actions.error.value }}</div>

    <div v-if="loading" class="state-box" role="status">Loading vendor invoices…</div>

    <div v-else-if="loadError" class="state-box state-box--error" role="alert">
      <div>{{ loadError }}</div>
      <button type="button" class="btn btn-ghost btn-sm" @click="load">Try again</button>
    </div>

    <div v-else-if="!invoices.length" class="card empty-card">
      <div class="empty-icon" aria-hidden="true">📎</div>
      <h2 class="empty-title">{{ filter === 'All' ? 'No vendor invoices yet' : `No ${filter.toLowerCase()} vendor invoices` }}</h2>
      <p class="empty-sub">
        {{ filter === 'All'
          ? 'When an order has been received, open it on the Orders page and upload the invoice your supplier sent. It will be listed here.'
          : 'Try another filter to see the rest.' }}
      </p>
    </div>

    <div v-else class="card">
      <div class="card-header">
        <h2 class="card-title">Vendor invoices</h2>
        <span class="count">{{ invoices.length }} shown · {{ formatNaira(shownTotal) }}</span>
      </div>
      <VendorInvoiceCard
        v-for="inv in invoices"
        :key="inv.id"
        :invoice="inv"
        show-order
        :busy="actions.busyId.value === inv.id"
        @view="actions.view"
        @download="actions.download"
        @toggle="actions.toggleStatus"
        @delete="actions.askDelete"
      />
    </div>

    <ConfirmDialog
      v-if="actions.toDelete.value"
      title="Delete this vendor invoice?"
      :message="`Invoice ${actions.toDelete.value.invoiceNumber} from ${actions.toDelete.value.vendorName} and its uploaded file will be permanently deleted.`"
      confirm-label="Delete invoice"
      :busy="actions.deleting.value"
      :error="actions.deleteError.value"
      @cancel="actions.toDelete.value = null"
      @confirm="actions.confirmDelete"
    />
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { vendorInvoiceApi } from '@/services/api'
import { useVendorInvoiceActions } from '@/composables/useVendorInvoiceActions'
import ConfirmDialog from '@/components/modal/ConfirmDialog.vue'
import VendorInvoiceCard from '@/components/vendor/VendorInvoiceCard.vue'
import { formatNaira, round2 } from '@/utils/money'
import { extractProblemMessage } from '@/utils/http'
import type { VendorInvoice } from '@/types'

type Filter = 'All' | 'Unpaid' | 'Paid'
const filters: { value: Filter; label: string }[] = [
  { value: 'All', label: 'All' },
  { value: 'Unpaid', label: 'Unpaid' },
  { value: 'Paid', label: 'Paid' }
]

const route = useRoute()
const router = useRouter()

const filter = ref<Filter>('All')
const orderFilter = ref(typeof route.query.orderId === 'string' ? route.query.orderId : '')
const invoices = ref<VendorInvoice[]>([])
const loading = ref(true)
const loadError = ref('')
const notice = ref('')

const shownTotal = computed(() => round2(invoices.value.reduce((s, i) => s + i.amount, 0)))

let ticket = 0
async function load() {
  const mine = ++ticket
  loading.value = true
  loadError.value = ''
  try {
    const res = await vendorInvoiceApi.list({
      status: filter.value === 'All' ? undefined : filter.value,
      orderId: orderFilter.value || undefined
    })
    if (mine !== ticket) return // a newer filter was chosen meanwhile
    invoices.value = res.data ?? []
  } catch (e) {
    if (mine !== ticket) return
    loadError.value = extractProblemMessage(e, 'Could not load vendor invoices. Please try again.')
  } finally {
    if (mine === ticket) loading.value = false
  }
}

function setFilter(f: Filter) {
  if (f === filter.value) return
  filter.value = f
  notice.value = ''
  load()
}

function clearOrderFilter() {
  orderFilter.value = ''
  router.replace({ name: 'vendor-invoices' })
  load()
}

const actions = useVendorInvoiceActions({
  onChanged: updated => {
    notice.value = `Invoice ${updated.invoiceNumber} marked ${updated.status.toLowerCase()}.`
    // With a Paid/Unpaid filter on, a bill whose status just changed no longer belongs in the list.
    if (filter.value !== 'All' && updated.status !== filter.value) {
      invoices.value = invoices.value.filter(i => i.id !== updated.id)
    } else {
      invoices.value = invoices.value.map(i => (i.id === updated.id ? updated : i))
    }
  },
  onRemoved: id => {
    const gone = invoices.value.find(i => i.id === id)
    invoices.value = invoices.value.filter(i => i.id !== id)
    notice.value = gone ? `Invoice ${gone.invoiceNumber} deleted.` : 'Invoice deleted.'
  }
})

onMounted(load)
</script>

<style scoped>
.toolbar-row { display: flex; align-items: center; justify-content: space-between; gap: 12px; flex-wrap: wrap; margin-bottom: 10px; }
.filter-row { display: flex; gap: 8px; flex-wrap: wrap; }
.vv-pill {
  padding: 6px 14px; min-height: 32px; border-radius: 20px; border: 1.5px solid var(--fog); background: transparent;
  font-family: var(--font-sans); font-size: 12px; letter-spacing: 0.04em; cursor: pointer; color: var(--stone);
}
.vv-pill:hover { border-color: var(--palm); color: var(--forest); }
.vv-pill.active { background: var(--forest); border-color: var(--forest); color: white; }
.vv-link { font-size: 12.5px; font-weight: 600; color: var(--leaf); text-decoration: none; }
.vv-link:hover { text-decoration: underline; }
.vv-chip { display: inline-flex; gap: 8px; align-items: center; font-size: 12px; color: var(--forest); background: var(--fog); border-radius: 20px; padding: 4px 6px 4px 12px; margin-bottom: 8px; }
.vv-chip-btn { border: none; background: white; border-radius: 14px; padding: 3px 10px; cursor: pointer; font-size: 11px; font-weight: 600; color: var(--leaf); }
.vv-status { min-height: 20px; margin-bottom: 4px; }
.vv-notice { font-size: 12px; font-weight: 600; color: var(--leaf); }
.vv-error { font-size: 12px; color: #B42323; margin-bottom: 8px; line-height: 1.5; }
.state-box { text-align: center; padding: 48px 16px; color: var(--stone); font-size: 13px; display: flex; flex-direction: column; align-items: center; gap: 12px; }
.state-box--error { color: #B42323; }
.empty-card { padding: 44px 20px; text-align: center; }
.empty-icon { font-size: 34px; margin-bottom: 10px; opacity: 0.5; }
.empty-title { font-family: var(--font-sans); font-weight: 700; font-size: 16px; color: var(--forest); margin: 0 0 6px; }
.empty-sub { font-size: 12.5px; color: var(--stone); line-height: 1.6; max-width: 400px; margin: 0 auto; }
.count { font-size: 11px; color: var(--stone); }
</style>
