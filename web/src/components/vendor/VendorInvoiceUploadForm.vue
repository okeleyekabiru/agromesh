<template>
  <form class="uf" novalidate @submit.prevent="submit" @keydown.esc.stop="$emit('cancel')">
    <h3 class="uf-title">Upload vendor invoice</h3>
    <p class="uf-help">Attach the invoice {{ order.supplierName }} sent you for this order.</p>

    <div v-if="errors.length" ref="errorBox" class="uf-errors" role="alert" tabindex="-1">
      <ul><li v-for="(m, i) in errors" :key="i">{{ m }}</li></ul>
    </div>

    <div class="uf-field">
      <label class="form-label" :for="id('file')">Invoice file <span class="req" aria-hidden="true">*</span></label>
      <input
        :id="id('file')"
        ref="fileInput"
        class="form-input uf-file"
        type="file"
        accept=".pdf,.png,.jpg,.jpeg,application/pdf,image/png,image/jpeg"
        required
        aria-required="true"
        :aria-describedby="id('file-hint')"
        @change="onFile"
      />
      <div :id="id('file-hint')" class="form-hint">PDF, PNG or JPEG, up to 10 MB.</div>
      <div v-if="fileError" class="uf-field-error" role="alert">{{ fileError }}</div>
    </div>

    <div class="uf-grid">
      <div class="uf-field">
        <label class="form-label" :for="id('number')">Invoice number <span class="req" aria-hidden="true">*</span></label>
        <input :id="id('number')" class="form-input" v-model="invoiceNumber" maxlength="100" required aria-required="true" autocomplete="off" />
      </div>
      <div class="uf-field">
        <label class="form-label" :for="id('amount')">Amount (₦) <span class="req" aria-hidden="true">*</span></label>
        <input
          :id="id('amount')" class="form-input" type="number" inputmode="decimal" min="0.01" max="999999999999.99" step="0.01"
          v-model.number="amount" required aria-required="true" :aria-describedby="id('amount-hint')" @wheel="blurOnWheel"
        />
        <div :id="id('amount-hint')" class="form-hint" :class="{ 'uf-warn': !!mismatch }">
          {{ mismatch ?? `Order total ${formatNaira(order.total)}` }}
        </div>
      </div>
      <div class="uf-field">
        <label class="form-label" :for="id('date')">Invoice date <span class="req" aria-hidden="true">*</span></label>
        <input :id="id('date')" class="form-input" type="date" v-model="invoiceDate" :min="MIN_DATE" :max="MAX_DATE" required aria-required="true" />
      </div>
      <div class="uf-field">
        <label class="form-label" :for="id('due')">Due date</label>
        <input :id="id('due')" class="form-input" type="date" v-model="dueDate" :min="invoiceDate && invoiceDate > MIN_DATE ? invoiceDate : MIN_DATE" :max="MAX_DATE" />
      </div>
    </div>

    <div class="uf-field">
      <label class="form-label" :for="id('vendor')">Vendor name</label>
      <input :id="id('vendor')" class="form-input" v-model="vendorName" maxlength="200" :placeholder="order.supplierName" autocomplete="off" :aria-describedby="id('vendor-hint')" />
      <div :id="id('vendor-hint')" class="form-hint">Leave blank to use the supplier on the order.</div>
    </div>

    <div class="uf-field">
      <label class="form-label" :for="id('notes')">Notes</label>
      <textarea :id="id('notes')" class="form-input uf-textarea" v-model="notes" maxlength="1000" rows="2" />
    </div>

    <div class="uf-actions">
      <button type="button" class="btn btn-ghost btn-sm" :disabled="saving" @click="$emit('cancel')">Cancel</button>
      <button type="submit" class="btn btn-primary btn-sm" :disabled="saving">{{ saving ? 'Uploading…' : 'Upload invoice' }}</button>
    </div>
  </form>
</template>

<script setup lang="ts">
import { computed, nextTick, onMounted, ref } from 'vue'
import { vendorInvoiceApi } from '@/services/api'
import { validateVendorFile } from '@/utils/files'
import { formatNaira, isDateInRange, MAX_DATE, MIN_DATE, toDateInput } from '@/utils/money'
import { formatAmountForUpload, mismatchHint, vendorAmountError } from '@/utils/vendorInvoice'
import { extractProblemMessage } from '@/utils/http'
import type { Order, VendorInvoice } from '@/types'

const props = defineProps<{ order: Order }>()
const emit = defineEmits<{ uploaded: [VendorInvoice]; cancel: [] }>()

const uid = Math.random().toString(36).slice(2, 7)
const id = (name: string) => `vi-${uid}-${name}`

const file = ref<File | null>(null)
const fileInput = ref<HTMLInputElement | null>(null)
const fileError = ref('')
const invoiceNumber = ref('')
const amount = ref<number | ''>('')
const invoiceDate = ref(toDateInput(new Date()))
const dueDate = ref('')
const vendorName = ref('')
const notes = ref('')
const saving = ref(false)
const errors = ref<string[]>([])
const errorBox = ref<HTMLElement | null>(null)

// Keyboard users land in the form they just opened.
onMounted(() => fileInput.value?.focus())

const mismatch = computed(() => (amount.value === '' ? null : mismatchHint(Number(amount.value), props.order.total)))

function blurOnWheel(e: Event) { (e.target as HTMLInputElement).blur() }

function onFile(e: Event) {
  const f = (e.target as HTMLInputElement).files?.[0] ?? null
  fileError.value = ''
  file.value = null
  if (!f) return
  const check = validateVendorFile(f)
  if (!check.ok) {
    fileError.value = check.message
    if (fileInput.value) fileInput.value.value = ''
    return
  }
  file.value = f
}

async function fail(list: string[]) {
  errors.value = list
  await nextTick()
  errorBox.value?.focus()
}

async function submit() {
  const problems: string[] = []
  if (!file.value) problems.push(fileError.value || 'Choose the invoice file to upload.')
  if (!invoiceNumber.value.trim()) problems.push('Enter the invoice number.')
  const amountProblem = vendorAmountError(amount.value)
  if (amountProblem) problems.push(amountProblem)
  if (!invoiceDate.value) problems.push('Choose the invoice date.')
  else if (!isDateInRange(invoiceDate.value)) problems.push('The invoice date must be a real date between 1 Jan 2000 and 31 Dec 2100 (check the year).')
  if (dueDate.value && !isDateInRange(dueDate.value)) problems.push('The due date must be a real date between 1 Jan 2000 and 31 Dec 2100 (check the year).')
  else if (dueDate.value && invoiceDate.value && dueDate.value < invoiceDate.value) problems.push('The due date cannot be before the invoice date.')
  if (problems.length) { await fail(problems); return }

  const form = new FormData()
  form.append('file', file.value as File)
  form.append('invoiceNumber', invoiceNumber.value.trim())
  form.append('amount', formatAmountForUpload(Number(amount.value))) // plain "." decimal, 2 places, never an exponent
  form.append('invoiceDate', invoiceDate.value)
  if (dueDate.value) form.append('dueDate', dueDate.value)
  if (vendorName.value.trim()) form.append('vendorName', vendorName.value.trim())
  if (notes.value.trim()) form.append('notes', notes.value.trim())

  errors.value = []
  saving.value = true
  try {
    const res = await vendorInvoiceApi.upload(props.order.id, form)
    emit('uploaded', res.data as VendorInvoice)
  } catch (e) {
    // 422 (order not received, duplicate number), 413 and 415 all carry a readable `detail`.
    await fail([extractProblemMessage(e, 'Could not upload the invoice. Please try again.')])
  } finally {
    saving.value = false
  }
}
</script>

<style scoped>
.uf { border: 1px solid var(--fog); border-radius: 12px; padding: 16px; background: #fbfdfb; margin-top: 12px; }
.uf-title { font-family: var(--font-sans); font-weight: 700; font-size: 13px; color: var(--forest); margin: 0 0 2px; }
.uf-help { margin: 0 0 12px; font-size: 12px; color: var(--stone); }
.uf-errors { border: 1px solid #E8B4B4; background: #FDF3F3; border-radius: 8px; padding: 8px 12px; margin-bottom: 12px; font-size: 12px; color: #8F1B1B; }
.uf-errors:focus { outline: 2px solid #B42323; outline-offset: 2px; }
.uf-errors ul { margin: 0; padding-left: 16px; line-height: 1.6; }
.uf-field { margin-bottom: 12px; }
.uf-grid { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 0 12px; }
.uf-file { padding: 7px 10px; }
.uf-textarea { min-height: 56px; resize: vertical; }
.uf-field-error { margin-top: 4px; font-size: 12px; color: #B42323; }
.uf-warn { color: #6B4700; font-weight: 600; }
.req { color: #B42323; }
.uf-actions { display: flex; justify-content: flex-end; gap: 8px; flex-wrap: wrap; }
@media (max-width: 480px) { .uf-grid { grid-template-columns: minmax(0, 1fr); } }
</style>
