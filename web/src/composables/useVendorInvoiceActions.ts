import { ref } from 'vue'
import { vendorInvoiceApi } from '@/services/api'
import { openBlobTab, saveBlob } from '@/utils/download'
import { extractProblemMessage, normalizeBlobError } from '@/utils/http'
import type { VendorInvoice } from '@/types'

/**
 * The per-bill actions shared by the order panel and the farm-wide list: download / view the stored
 * file (bearer token, so as a blob), mark paid or unpaid, and delete after a confirmation.
 * `onChanged` receives the server's copy after a status change; `onRemoved` the id after a delete.
 */
export function useVendorInvoiceActions(handlers: {
  onChanged: (updated: VendorInvoice) => void
  onRemoved: (id: string) => void
}) {
  const busyId = ref<string | null>(null)
  const error = ref('')
  const toDelete = ref<VendorInvoice | null>(null)
  const deleting = ref(false)
  const deleteError = ref('')

  async function fetchFile(inv: VendorInvoice, inline: boolean): Promise<Blob> {
    try {
      const res = await vendorInvoiceApi.file(inv.id, inline)
      return new Blob([res.data], { type: inv.contentType || 'application/octet-stream' })
    } catch (e) {
      throw await normalizeBlobError(e)
    }
  }

  async function download(inv: VendorInvoice) {
    busyId.value = inv.id
    error.value = ''
    try {
      saveBlob(await fetchFile(inv, false), inv.fileName)
    } catch (e) {
      error.value = extractProblemMessage(e, 'Could not download the invoice file. Please try again.')
    } finally {
      busyId.value = null
    }
  }

  async function view(inv: VendorInvoice) {
    const tab = openBlobTab(inv.invoiceNumber)
    busyId.value = inv.id
    error.value = ''
    try {
      tab.show(await fetchFile(inv, true), inv.fileName)
    } catch (e) {
      tab.cancel()
      error.value = extractProblemMessage(e, 'Could not open the invoice file. Please try again.')
    } finally {
      busyId.value = null
    }
  }

  async function toggleStatus(inv: VendorInvoice) {
    busyId.value = inv.id
    error.value = ''
    try {
      const next = inv.status === 'Paid' ? 'Unpaid' : 'Paid'
      const res = await vendorInvoiceApi.setStatus(inv.id, next, next === 'Paid' ? new Date().toISOString() : undefined)
      handlers.onChanged(res.data as VendorInvoice)
    } catch (e) {
      error.value = extractProblemMessage(e, 'Could not update the payment status. Please try again.')
    } finally {
      busyId.value = null
    }
  }

  function askDelete(inv: VendorInvoice) {
    deleteError.value = ''
    toDelete.value = inv
  }

  async function confirmDelete() {
    if (!toDelete.value) return
    const inv = toDelete.value
    deleting.value = true
    deleteError.value = ''
    try {
      await vendorInvoiceApi.remove(inv.id)
      toDelete.value = null
      handlers.onRemoved(inv.id)
    } catch (e) {
      if ((e as { response?: { status?: number } })?.response?.status === 404) {
        // Already deleted elsewhere: treat it as gone instead of leaving an error open.
        toDelete.value = null
        handlers.onRemoved(inv.id)
        return
      }
      deleteError.value = extractProblemMessage(e, 'Could not delete this invoice. Please try again.')
    } finally {
      deleting.value = false
    }
  }

  return { busyId, error, toDelete, deleting, deleteError, download, view, toggleStatus, askDelete, confirmDelete }
}
