<template>
  <section class="vp" aria-labelledby="vp-heading">
    <div class="vp-head">
      <h3 id="vp-heading" class="vp-title">Vendor invoices</h3>
      <button
        v-if="canUpload && !showForm"
        ref="uploadBtn"
        type="button"
        class="btn btn-ghost btn-sm"
        @click="showForm = true"
      >+ Upload vendor invoice</button>
    </div>

    <p v-if="!canUpload" class="vp-note">Only supervisors, farm managers and owners can upload vendor invoices.</p>

    <div class="vp-status" role="status" aria-live="polite">
      <span v-if="notice" class="vp-notice">{{ notice }}</span>
    </div>

    <VendorInvoiceUploadForm v-if="showForm && canUpload" :order="order" @uploaded="onUploaded" @cancel="closeForm" />

    <div v-if="actions.error.value" class="vp-error" role="alert">{{ actions.error.value }}</div>

    <div v-if="loading" class="vp-state" role="status">Loading vendor invoices…</div>
    <div v-else-if="loadError" class="vp-state vp-state--error" role="alert">
      {{ loadError }}
      <button type="button" class="btn btn-ghost btn-sm" @click="load">Try again</button>
    </div>
    <div v-else-if="!invoices.length && !showForm" class="vp-empty">
      No vendor invoice attached to this order yet.
    </div>
    <div v-else-if="invoices.length" class="vp-list">
      <VendorInvoiceCard
        v-for="inv in invoices"
        :key="inv.id"
        :invoice="inv"
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
  </section>
</template>

<script setup lang="ts">
import { computed, nextTick, onMounted, ref } from 'vue'
import { useAgroStore } from '@/stores/agro'
import { vendorInvoiceApi } from '@/services/api'
import { useVendorInvoiceActions } from '@/composables/useVendorInvoiceActions'
import { canUploadVendorInvoice } from '@/utils/roles'
import { extractProblemMessage } from '@/utils/http'
import ConfirmDialog from '@/components/modal/ConfirmDialog.vue'
import VendorInvoiceCard from '@/components/vendor/VendorInvoiceCard.vue'
import VendorInvoiceUploadForm from '@/components/vendor/VendorInvoiceUploadForm.vue'
import type { Order, VendorInvoice } from '@/types'

// Shown inside the order dialog for Received (completed) orders only; the parent decides that.
const props = defineProps<{ order: Order }>()

const store = useAgroStore()
const canUpload = computed(() => canUploadVendorInvoice(store.currentUser?.role))

const invoices = ref<VendorInvoice[]>([])
const loading = ref(true)
const loadError = ref('')
const showForm = ref(false)
const uploadBtn = ref<HTMLButtonElement | null>(null)
const notice = ref('')

const actions = useVendorInvoiceActions({
  onChanged: updated => {
    invoices.value = invoices.value.map(i => (i.id === updated.id ? updated : i))
    notice.value = `Invoice ${updated.invoiceNumber} marked ${updated.status.toLowerCase()}.`
  },
  onRemoved: id => {
    const gone = invoices.value.find(i => i.id === id)
    invoices.value = invoices.value.filter(i => i.id !== id)
    notice.value = gone ? `Invoice ${gone.invoiceNumber} deleted.` : 'Invoice deleted.'
  }
})

async function load() {
  loading.value = true
  loadError.value = ''
  try {
    invoices.value = (await vendorInvoiceApi.listForOrder(props.order.id)).data ?? []
  } catch (e) {
    loadError.value = extractProblemMessage(e, 'Could not load vendor invoices.')
  } finally {
    loading.value = false
  }
}

// The button that opened the form is removed while it is open, so focus is put back on it afterwards.
async function closeForm() {
  showForm.value = false
  await nextTick()
  uploadBtn.value?.focus()
}

function onUploaded(inv: VendorInvoice) {
  invoices.value = [inv, ...invoices.value]
  closeForm()
  notice.value = `Invoice ${inv.invoiceNumber} uploaded.`
}

onMounted(load)
</script>

<style scoped>
.vp { margin-top: 20px; padding-top: 16px; border-top: 1px solid var(--fog); }
.vp-head { display: flex; align-items: center; justify-content: space-between; gap: 8px; flex-wrap: wrap; }
.vp-title { font-family: var(--font-sans); font-weight: 700; font-size: 13px; color: var(--forest); margin: 0; }
.vp-note { margin: 6px 0 0; font-size: 12px; color: var(--stone); }
.vp-status { min-height: 18px; margin-top: 4px; }
.vp-notice { font-size: 12px; font-weight: 600; color: var(--leaf); }
.vp-error { font-size: 12px; color: #B42323; margin: 6px 0; line-height: 1.5; }
.vp-state { font-size: 12px; color: var(--stone); padding: 12px 0; display: flex; align-items: center; gap: 10px; flex-wrap: wrap; }
.vp-state--error { color: #B42323; }
.vp-empty { font-size: 12px; color: var(--stone); padding: 10px 0 2px; }
.vp-list { border: 1px solid var(--fog); border-radius: 10px; overflow: hidden; margin-top: 8px; }
</style>
