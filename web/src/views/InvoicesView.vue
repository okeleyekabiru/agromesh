<template>
  <div class="inv-page">
    <div class="toolbar-row">
      <p class="toolbar-text">
        Invoices you issue to customers. Each one gets the next number automatically and downloads as a PDF with your logo.
      </p>
      <RouterLink v-if="canEdit" :to="{ name: 'invoice-new' }" class="btn btn-primary">+ New invoice</RouterLink>
    </div>

    <div class="inv-status" role="status" aria-live="polite">
      <span v-if="notice" class="inv-notice">{{ notice }}</span>
    </div>
    <div v-if="actionError" class="inv-error" role="alert">{{ actionError }}</div>

    <div v-if="loading" class="state-box" role="status">Loading invoices…</div>

    <div v-else-if="loadError" class="state-box state-box--error" role="alert">
      <div>{{ loadError }}</div>
      <button type="button" class="btn btn-ghost btn-sm" @click="load">Try again</button>
    </div>

    <div v-else-if="!invoices.length" class="card empty-card">
      <div class="empty-icon" aria-hidden="true">🧾</div>
      <h2 class="empty-title">No invoices yet</h2>
      <p class="empty-sub">
        {{ canEdit
          ? 'Create your first invoice: add who you are billing, the items sold and the VAT, then download the PDF.'
          : 'Invoices created by your farm owner or managers will appear here.' }}
      </p>
      <RouterLink v-if="canEdit" :to="{ name: 'invoice-new' }" class="btn btn-primary">Create your first invoice</RouterLink>
    </div>

    <div v-else class="card">
      <div class="card-header">
        <h2 class="card-title">Invoices</h2>
        <span class="count">{{ invoices.length }} total</span>
      </div>
      <div class="table-wrap">
        <table class="inv-table">
          <thead>
            <tr>
              <th scope="col">Reference</th>
              <th scope="col">Bill to</th>
              <th scope="col">Date</th>
              <th scope="col">Due</th>
              <th scope="col" class="num">Total</th>
              <th scope="col"><span class="sr-only">Actions</span></th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="inv in invoices" :key="inv.id">
              <td data-label="Reference" class="ref">{{ inv.reference }}</td>
              <td data-label="Bill to" class="bill">{{ inv.billToName }}</td>
              <td data-label="Date">{{ formatDay(inv.date) }}</td>
              <td data-label="Due">{{ formatDay(inv.dueDate) }}</td>
              <td data-label="Total" class="num total">{{ formatNaira(inv.total) }}</td>
              <td class="actions">
                <button type="button" class="btn btn-ghost btn-sm" :disabled="busyId === inv.id" :aria-label="`Open PDF for ${inv.reference}`" @click="openPdf(inv)">
                  {{ busyId === inv.id && busyKind === 'open' ? 'Opening…' : 'Open' }}
                </button>
                <button type="button" class="btn btn-ghost btn-sm" :disabled="busyId === inv.id" :aria-label="`Download PDF for ${inv.reference}`" @click="downloadPdf(inv)">
                  {{ busyId === inv.id && busyKind === 'download' ? 'Preparing…' : 'Download' }}
                </button>
                <RouterLink v-if="canEdit" :to="{ name: 'invoice-edit', params: { id: inv.id } }" class="btn btn-ghost btn-sm" :aria-label="`Edit ${inv.reference}`">Edit</RouterLink>
                <button v-if="canEdit" type="button" class="btn btn-ghost btn-sm del" :aria-label="`Delete ${inv.reference}`" @click="toDelete = inv">Delete</button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <ConfirmDialog
      v-if="toDelete"
      title="Delete this invoice?"
      :message="`${toDelete.reference} for ${toDelete.billToName} (${formatNaira(toDelete.total)}) will be permanently deleted. Its number is not reused.`"
      confirm-label="Delete invoice"
      :busy="deleting"
      :error="deleteError"
      @cancel="toDelete = null"
      @confirm="doDelete"
    />
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAgroStore } from '@/stores/agro'
import { invoiceApi } from '@/services/api'
import ConfirmDialog from '@/components/modal/ConfirmDialog.vue'
import { canManageFarm } from '@/utils/roles'
import { formatDay, formatNaira } from '@/utils/money'
import { saveBlob, openBlobTab } from '@/utils/download'
import { extractProblemMessage, normalizeBlobError } from '@/utils/http'
import type { InvoiceSummary } from '@/types'

const store = useAgroStore()
const route = useRoute()
const router = useRouter()
const canEdit = computed(() => canManageFarm(store.currentUser?.role))

const invoices = ref<InvoiceSummary[]>([])
const loading = ref(true)
const loadError = ref('')
const actionError = ref('')
const notice = ref('')

async function load() {
  loading.value = true
  loadError.value = ''
  try {
    invoices.value = (await invoiceApi.list()).data ?? []
  } catch (e) {
    loadError.value = extractProblemMessage(e, 'Could not load invoices. Please try again.')
  } finally {
    loading.value = false
  }
}

onMounted(() => {
  load()
  // The editor sends us back here with ?saved=INV-0003 after a save.
  const saved = route.query.saved
  if (typeof saved === 'string' && saved) {
    notice.value = `Invoice ${saved} saved.`
    router.replace({ name: 'invoices' })
  }
})

// ── PDF (the endpoint needs the bearer token, so it is fetched as a blob) ──
const busyId = ref<string | null>(null)
const busyKind = ref<'open' | 'download' | null>(null)

async function fetchPdf(inv: InvoiceSummary): Promise<Blob> {
  try {
    const res = await invoiceApi.pdf(inv.id)
    return new Blob([res.data], { type: 'application/pdf' })
  } catch (e) {
    throw await normalizeBlobError(e)
  }
}

async function openPdf(inv: InvoiceSummary) {
  const tab = openBlobTab(inv.reference) // opened now, while the click still counts as a user gesture
  busyId.value = inv.id
  busyKind.value = 'open'
  actionError.value = ''
  try {
    tab.show(await fetchPdf(inv), `${inv.reference}.pdf`)
  } catch (e) {
    tab.cancel()
    actionError.value = extractProblemMessage(e, `Could not open ${inv.reference}. Please try again.`)
  } finally {
    busyId.value = null
    busyKind.value = null
  }
}

async function downloadPdf(inv: InvoiceSummary) {
  busyId.value = inv.id
  busyKind.value = 'download'
  actionError.value = ''
  try {
    saveBlob(await fetchPdf(inv), `${inv.reference}.pdf`)
  } catch (e) {
    actionError.value = extractProblemMessage(e, `Could not download ${inv.reference}. Please try again.`)
  } finally {
    busyId.value = null
    busyKind.value = null
  }
}

// ── Delete ──
const toDelete = ref<InvoiceSummary | null>(null)
const deleting = ref(false)
const deleteError = ref('')

async function doDelete() {
  if (!toDelete.value) return
  const inv = toDelete.value
  deleting.value = true
  deleteError.value = ''
  try {
    await invoiceApi.remove(inv.id)
    invoices.value = invoices.value.filter(i => i.id !== inv.id)
    notice.value = `Invoice ${inv.reference} deleted.`
    toDelete.value = null
  } catch (e) {
    if ((e as { response?: { status?: number } })?.response?.status === 404) {
      // Already gone (deleted elsewhere): close the dialog and show the real list.
      toDelete.value = null
      notice.value = `Invoice ${inv.reference} was already deleted.`
      await load()
      return
    }
    deleteError.value = extractProblemMessage(e, 'Could not delete this invoice. Please try again.')
  } finally {
    deleting.value = false
  }
}
</script>

<style scoped>
.toolbar-row { display: flex; align-items: center; justify-content: space-between; gap: 12px; flex-wrap: wrap; margin-bottom: 8px; }
.toolbar-text { margin: 0; font-size: 12.5px; color: var(--stone); max-width: 560px; line-height: 1.5; }
.inv-status { min-height: 22px; margin-bottom: 4px; }
.inv-notice { font-size: 12px; font-weight: 600; color: var(--leaf); }
.inv-error { font-size: 12px; color: #B42323; margin-bottom: 10px; line-height: 1.5; }
.state-box { text-align: center; padding: 48px 16px; color: var(--stone); font-size: 13px; display: flex; flex-direction: column; align-items: center; gap: 12px; }
.state-box--error { color: #B42323; }
.empty-card { padding: 44px 20px; text-align: center; }
.empty-icon { font-size: 34px; margin-bottom: 10px; opacity: 0.5; }
.empty-title { font-family: var(--font-sans); font-weight: 700; font-size: 16px; color: var(--forest); margin: 0 0 6px; }
.empty-sub { font-size: 12.5px; color: var(--stone); line-height: 1.6; max-width: 380px; margin: 0 auto 16px; }
.count { font-size: 11px; color: var(--stone); }
.table-wrap { overflow-x: auto; }
.inv-table { width: 100%; border-collapse: collapse; font-size: 13px; }
.inv-table th {
  text-align: left; font-size: 11px; font-weight: 600; letter-spacing: 0.1em; text-transform: uppercase;
  color: var(--stone); padding: 10px 16px; background: #f9fafb; border-bottom: 1px solid var(--fog); white-space: nowrap;
}
.inv-table td { padding: 12px 16px; border-bottom: 1px solid var(--fog); color: var(--forest); vertical-align: middle; }
.inv-table tbody tr:last-child td { border-bottom: none; }
.num { text-align: right; }
.ref { font-weight: 700; white-space: nowrap; }
.bill { font-weight: 600; max-width: 260px; overflow-wrap: anywhere; }
.total { font-weight: 700; white-space: nowrap; }
.actions { display: flex; gap: 6px; justify-content: flex-end; flex-wrap: wrap; }
.actions .btn { text-decoration: none; }
.del { color: #B42323; }

@media (max-width: 720px) {
  .inv-table thead { position: absolute; width: 1px; height: 1px; overflow: hidden; clip: rect(0 0 0 0); }
  .inv-table, .inv-table tbody, .inv-table tr, .inv-table td { display: block; }
  .inv-table tr { padding: 12px 16px; border-bottom: 1px solid var(--fog); }
  .inv-table tbody tr:last-child { border-bottom: none; }
  .inv-table td { border: none; padding: 3px 0; display: flex; justify-content: space-between; gap: 12px; text-align: right; }
  .inv-table td[data-label]::before { content: attr(data-label); font-size: 11px; letter-spacing: 0.08em; text-transform: uppercase; color: var(--stone); text-align: left; flex-shrink: 0; }
  .inv-table td.actions { justify-content: flex-start; padding-top: 10px; }
}
</style>
