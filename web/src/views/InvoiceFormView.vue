<template>
  <div class="form-page">
    <div class="back-row">
      <RouterLink :to="{ name: 'invoices' }" class="back-link">← Back to invoices</RouterLink>
    </div>

    <div v-if="!canEdit" class="card note-card" role="alert">
      Only farm owners and managers can create or edit invoices.
    </div>

    <div v-else-if="loading" class="state-box" role="status">Loading…</div>

    <div v-else-if="loadError" class="state-box state-box--error" role="alert">
      <div>{{ loadError }}</div>
      <button type="button" class="btn btn-ghost btn-sm" @click="init">Try again</button>
    </div>

    <form v-else novalidate @submit.prevent="submit">
      <h1 class="page-h">{{ isEdit ? `Edit ${reference}` : 'New invoice' }}</h1>

      <div v-if="errors.length" ref="errorBox" class="error-box" role="alert" tabindex="-1">
        <div class="error-title">Please fix the following</div>
        <ul>
          <li v-for="(m, i) in errors" :key="i">{{ m }}</li>
        </ul>
      </div>

      <!-- Bill to -->
      <section class="card section" aria-labelledby="h-bill">
        <h2 id="h-bill" class="sec-title">Billing to</h2>
        <div class="grid">
          <div class="field wide">
            <label class="form-label" for="f-name">Customer name <span class="req" aria-hidden="true">*</span></label>
            <input id="f-name" class="form-input" v-model="form.billToName" maxlength="200" required aria-required="true" autocomplete="off" />
          </div>
          <div class="field">
            <label class="form-label" for="f-phone">Phone</label>
            <input id="f-phone" class="form-input" type="tel" v-model="form.billToPhone" maxlength="60" autocomplete="off" />
          </div>
          <div class="field">
            <label class="form-label" for="f-email">Email</label>
            <input id="f-email" class="form-input" type="email" v-model="form.billToEmail" maxlength="200" autocomplete="off" />
          </div>
          <div class="field wide">
            <label class="form-label" for="f-addr">Address</label>
            <textarea id="f-addr" class="form-input textarea" v-model="form.billToAddress" maxlength="500" rows="2" autocomplete="off" />
          </div>
        </div>
      </section>

      <!-- Details -->
      <section class="card section" aria-labelledby="h-details">
        <h2 id="h-details" class="sec-title">Invoice details</h2>
        <div class="grid grid-3">
          <div class="field">
            <label class="form-label" for="f-date">Invoice date <span class="req" aria-hidden="true">*</span></label>
            <input id="f-date" class="form-input" type="date" v-model="form.date" :min="MIN_DATE" :max="MAX_DATE" required aria-required="true" />
          </div>
          <div class="field">
            <label class="form-label" for="f-due">Due date <span class="req" aria-hidden="true">*</span></label>
            <input id="f-due" class="form-input" type="date" v-model="form.dueDate" :min="dueMin" :max="MAX_DATE" required aria-required="true" @input="dueTouched = true" />
          </div>
          <div class="field">
            <label class="form-label" for="f-tpl">Template</label>
            <select id="f-tpl" class="form-select" v-model="form.templateKey" aria-describedby="f-tpl-hint">
              <option v-for="t in templateOptions" :key="t.key" :value="t.key">{{ t.name }}</option>
            </select>
            <div id="f-tpl-hint" class="form-hint">{{ templateHint }}</div>
          </div>
        </div>
      </section>

      <!-- Line items -->
      <section class="card section" aria-labelledby="h-items">
        <h2 id="h-items" class="sec-title">Items</h2>
        <div class="li-head" aria-hidden="true">
          <span>#</span><span>Description</span><span class="r">Rate (₦)</span><span class="r">Qty</span><span class="r">Amount</span><span />
        </div>
        <div v-for="(line, i) in form.lineItems" :key="i" class="li-row" role="group" :aria-label="`Line item ${i + 1}`">
          <span class="li-no" aria-hidden="true">{{ i + 1 }}</span>
          <div class="li-cell li-desc">
            <label class="li-label" :for="`li-d-${i}`">Description, line {{ i + 1 }}</label>
            <input :id="`li-d-${i}`" class="form-input" v-model="line.description" maxlength="500" autocomplete="off" />
          </div>
          <div class="li-cell">
            <label class="li-label" :for="`li-r-${i}`">Rate, line {{ i + 1 }}</label>
            <input :id="`li-r-${i}`" class="form-input r" type="number" inputmode="decimal" min="0" step="0.01" v-model.number="line.rate" @wheel="blurOnWheel" />
          </div>
          <div class="li-cell">
            <label class="li-label" :for="`li-q-${i}`">Quantity, line {{ i + 1 }}</label>
            <input :id="`li-q-${i}`" class="form-input r" type="number" inputmode="decimal" min="0" step="any" v-model.number="line.quantity" @wheel="blurOnWheel" />
          </div>
          <div class="li-cell li-amount">
            <span class="li-label">Amount, line {{ i + 1 }}</span>
            <output class="amount">{{ formatNaira(totals.lines[i]?.amount) }}</output>
          </div>
          <button type="button" class="li-remove" :disabled="form.lineItems.length === 1" :aria-label="`Remove line item ${i + 1}`" @click="removeLine(i)">×</button>
        </div>
        <button type="button" class="btn btn-ghost btn-sm add-line" :disabled="form.lineItems.length >= MAX_LINE_ITEMS" @click="addLine">+ Add line</button>

        <div class="totals" aria-live="polite">
          <div class="tot-row"><span>Subtotal</span><span>{{ formatNaira(totals.subtotal) }}</span></div>
          <div class="tot-row tot-vat">
            <label for="f-vat">VAT (%)</label>
            <input id="f-vat" class="form-input vat-input r" type="number" inputmode="decimal" min="0" max="100" step="0.01" v-model.number="form.vatPercent" @wheel="blurOnWheel" />
            <span>{{ formatNaira(totals.vatAmount) }}</span>
          </div>
          <div class="tot-row tot-total"><span>Total</span><span>{{ formatNaira(totals.total) }}</span></div>
          <p v-if="overLimit" class="tot-warn" role="alert">{{ overLimit }}</p>
          <p class="tot-note">A live preview. The saved invoice uses the amounts calculated by the server.</p>
        </div>
      </section>

      <!-- Payment + terms -->
      <section class="card section" aria-labelledby="h-pay">
        <h2 id="h-pay" class="sec-title">Payment method</h2>
        <p class="sec-help">Filled in from your business profile. Change them here for this invoice only.</p>
        <div class="grid grid-3">
          <div class="field">
            <label class="form-label" for="f-pan">Account name</label>
            <input id="f-pan" class="form-input" v-model="form.paymentAccountName" maxlength="150" autocomplete="off" />
          </div>
          <div class="field">
            <label class="form-label" for="f-pano">Account number</label>
            <input id="f-pano" class="form-input" inputmode="numeric" v-model="form.paymentAccountNumber" maxlength="40" autocomplete="off" />
          </div>
          <div class="field">
            <label class="form-label" for="f-pbank">Bank name</label>
            <input id="f-pbank" class="form-input" v-model="form.paymentBankName" maxlength="100" autocomplete="off" />
          </div>
        </div>
      </section>

      <section class="card section" aria-labelledby="h-terms">
        <h2 id="h-terms" class="sec-title">Terms and signature</h2>
        <div class="grid">
          <div class="field wide">
            <label class="form-label" for="f-terms">Terms and notes</label>
            <textarea id="f-terms" class="form-input textarea" v-model="form.termsAndNotes" maxlength="2000" rows="3" />
          </div>
          <div class="field wide">
            <label class="form-label" for="f-sign">Signatory name</label>
            <input id="f-sign" class="form-input" v-model="form.signatoryName" maxlength="150" autocomplete="off" />
          </div>
        </div>
      </section>

      <div class="footer-bar">
        <RouterLink :to="{ name: 'invoices' }" class="btn btn-ghost">Cancel</RouterLink>
        <button type="submit" class="btn btn-primary" :disabled="saving">
          {{ saving ? 'Saving…' : isEdit ? 'Save changes' : 'Create invoice' }}
        </button>
      </div>
    </form>
  </div>
</template>

<script setup lang="ts">
import { computed, nextTick, onMounted, reactive, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAgroStore } from '@/stores/agro'
import { businessProfileApi, invoiceApi } from '@/services/api'
import { canManageFarm } from '@/utils/roles'
import { addDays, computeInvoiceTotals, formatNaira, isDateInRange, MAX_DATE, MIN_DATE, toDateInput } from '@/utils/money'
import {
  blankLine, buildInvoiceRequest, emptyInvoiceForm, invoiceToForm, MAX_LINE_AMOUNT, MAX_LINE_ITEMS, MAX_SUBTOTAL, prefillPayment, validateInvoiceForm,
  type InvoiceForm
} from '@/utils/invoiceForm'
import { extractProblemMessage } from '@/utils/http'
import type { Invoice, InvoiceTemplate } from '@/types'

const store = useAgroStore()
const route = useRoute()
const router = useRouter()

const canEdit = computed(() => canManageFarm(store.currentUser?.role))
const editId = computed(() => (typeof route.params.id === 'string' ? route.params.id : ''))
const isEdit = computed(() => !!editId.value)

const form = reactive<InvoiceForm>(emptyInvoiceForm(toDateInput(new Date())))
const templates = ref<InvoiceTemplate[]>([])
const reference = ref('')
const loading = ref(true)
const loadError = ref('')
const saving = ref(false)
const errors = ref<string[]>([])
const errorBox = ref<HTMLElement | null>(null)
const dueTouched = ref(false)

// Live preview only: the server recomputes every amount.
const totals = computed(() => computeInvoiceTotals(form.lineItems, form.vatPercent))

const dueMin = computed(() => (isDateInRange(form.date) ? form.date : MIN_DATE))

// Say so in the preview when a figure is beyond what the server accepts.
const overLimit = computed(() => {
  if (totals.value.lines.some(l => l.amount >= MAX_LINE_AMOUNT)) return `A line amount is too large: each must be less than ${MAX_LINE_AMOUNT.toLocaleString('en-NG')}.`
  if (totals.value.subtotal >= MAX_SUBTOTAL) return `The subtotal is too large: it must be less than ${MAX_SUBTOTAL.toLocaleString('en-NG')}.`
  return ''
})

// Keep the due date 14 days after the invoice date until the user picks their own.
watch(() => form.date, d => { if (!dueTouched.value && d && isDateInRange(d)) form.dueDate = addDays(d, 14) })

const templateOptions = computed(() =>
  templates.value.length ? templates.value : [{ key: 'ngf', name: 'NGF - Invoice', description: null, isDefault: true }]
)
const templateHint = computed(() => templates.value.find(t => t.key === form.templateKey)?.description ?? 'More templates will appear here as they are added.')

function blurOnWheel(e: Event) {
  // Scrolling over a focused number box should scroll the page, not change the figure.
  (e.target as HTMLInputElement).blur()
}

function addLine() {
  if (form.lineItems.length < MAX_LINE_ITEMS) form.lineItems.push(blankLine())
}
function removeLine(i: number) {
  if (form.lineItems.length > 1) form.lineItems.splice(i, 1)
}

async function init() {
  if (!canEdit.value) { loading.value = false; return }
  loading.value = true
  loadError.value = ''
  try {
    const [tpl, existing, profile] = await Promise.all([
      invoiceApi.templates().then(r => r.data as InvoiceTemplate[]).catch(() => [] as InvoiceTemplate[]),
      isEdit.value ? invoiceApi.get(editId.value).then(r => r.data as Invoice) : Promise.resolve(null),
      !isEdit.value && store.activeFarmId
        ? businessProfileApi.get(store.farm?.id ?? store.activeFarmId).then(r => r.data).catch(() => null)
        : Promise.resolve(null)
    ])
    templates.value = tpl
    if (existing) {
      Object.assign(form, invoiceToForm(existing))
      reference.value = existing.reference
      dueTouched.value = true
    } else {
      const def = tpl.find(t => t.isDefault) ?? tpl[0]
      if (def) form.templateKey = def.key
      prefillPayment(form, profile)
    }
  } catch (e) {
    loadError.value = extractProblemMessage(e, 'Could not load this invoice.')
  } finally {
    loading.value = false
  }
}

onMounted(init)

async function showErrors(list: string[]) {
  errors.value = list
  await nextTick()
  errorBox.value?.focus()
  errorBox.value?.scrollIntoView?.({ block: 'center' })
}

async function submit() {
  const problems = validateInvoiceForm(form)
  if (problems.length) { await showErrors(problems); return }
  errors.value = []
  saving.value = true
  try {
    const body = buildInvoiceRequest(form)
    const res = isEdit.value ? await invoiceApi.update(editId.value, body) : await invoiceApi.create(body)
    const saved = res.data as Invoice
    await router.push({ name: 'invoices', query: { saved: saved.reference } })
  } catch (e) {
    // 422 carries a human `detail`; 400 carries per-field messages. Both are shown as the API wrote them.
    await showErrors([extractProblemMessage(e, 'Could not save this invoice. Please try again.')])
  } finally {
    saving.value = false
  }
}
</script>

<style scoped>
.form-page { max-width: 900px; padding-bottom: 24px; }
.back-row { margin-bottom: 10px; }
.back-link { font-size: 12.5px; color: var(--leaf); font-weight: 600; text-decoration: none; }
.back-link:hover { text-decoration: underline; }
.page-h { font-family: var(--font-sans); font-weight: 800; font-size: 20px; color: var(--forest); margin: 0 0 14px; }
.note-card { padding: 20px; font-size: 13px; color: var(--stone); }
.state-box { text-align: center; padding: 48px 16px; color: var(--stone); font-size: 13px; display: flex; flex-direction: column; align-items: center; gap: 12px; }
.state-box--error { color: #B42323; }

.error-box { border: 1px solid #E8B4B4; background: #FDF3F3; border-radius: 10px; padding: 12px 16px; margin-bottom: 14px; font-size: 12.5px; color: #8F1B1B; }
.error-box:focus { outline: 2px solid #B42323; outline-offset: 2px; }
.error-title { font-weight: 700; margin-bottom: 4px; }
.error-box ul { margin: 0; padding-left: 18px; line-height: 1.6; }

.section { padding: 16px 20px 20px; margin-bottom: 14px; overflow: visible; }
.sec-title { font-family: var(--font-sans); font-weight: 700; font-size: 14px; color: var(--forest); margin: 0 0 12px; }
.sec-help { margin: -6px 0 12px; font-size: 12px; color: var(--stone); }
.grid { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 12px 16px; }
.grid-3 { grid-template-columns: repeat(3, minmax(0, 1fr)); }
.field.wide { grid-column: 1 / -1; }
.req { color: #B42323; }
.textarea { min-height: 64px; resize: vertical; }

/* Line items */
.li-head, .li-row { display: grid; grid-template-columns: 28px minmax(0, 1fr) 130px 90px 130px 32px; gap: 8px; align-items: center; }
.li-head { font-size: 11px; letter-spacing: 0.08em; text-transform: uppercase; color: var(--stone); padding: 0 0 6px; }
.li-head .r { text-align: right; }
.li-row { padding: 4px 0; }
.li-no { font-size: 12px; color: var(--stone); text-align: center; }
.li-cell { min-width: 0; }
.li-label { position: absolute; width: 1px; height: 1px; overflow: hidden; clip: rect(0 0 0 0); white-space: nowrap; }
.r { text-align: right; }
.amount { display: block; text-align: right; font-weight: 600; font-size: 13px; color: var(--forest); padding: 0 4px; overflow-wrap: anywhere; }
.li-remove { width: 32px; height: 32px; border-radius: 6px; border: 1px solid var(--fog); background: transparent; cursor: pointer; font-size: 18px; color: var(--stone); line-height: 1; }
.li-remove:hover:not(:disabled) { color: #B42323; border-color: #B42323; }
.li-remove:disabled { opacity: 0.4; cursor: not-allowed; }
.add-line { margin-top: 8px; }

.totals { margin: 16px 0 0 auto; max-width: 340px; border-top: 1px solid var(--fog); padding-top: 12px; }
.tot-row { display: flex; justify-content: space-between; align-items: center; gap: 10px; padding: 4px 0; font-size: 13px; color: var(--forest); }
.tot-vat { display: grid; grid-template-columns: 1fr 90px auto; }
.tot-vat span { text-align: right; min-width: 90px; }
.vat-input { min-height: 34px; padding: 6px 8px; }
.tot-total { border-top: 1px solid var(--fog); margin-top: 6px; padding-top: 10px; font-weight: 800; font-size: 15px; }
.tot-warn { margin: 6px 0 0; font-size: 12px; font-weight: 600; color: #B42323; }
.tot-note { margin: 8px 0 0; font-size: 11px; color: var(--stone); line-height: 1.5; }

.footer-bar { display: flex; justify-content: flex-end; gap: 8px; margin-top: 4px; flex-wrap: wrap; }
.footer-bar .btn { text-decoration: none; }

@media (max-width: 720px) {
  .grid, .grid-3 { grid-template-columns: minmax(0, 1fr); }
  .li-head { display: none; }
  .li-row {
    grid-template-columns: 24px minmax(0, 1fr) minmax(0, 1fr) 32px;
    grid-template-areas: "no desc desc rm" ". rate qty ." ". amt amt .";
    gap: 6px 8px; padding: 12px 0; border-bottom: 1px solid var(--fog);
  }
  .li-no { grid-area: no; }
  .li-desc { grid-area: desc; }
  .li-row > .li-cell:nth-of-type(2) { grid-area: rate; }
  .li-row > .li-cell:nth-of-type(3) { grid-area: qty; }
  .li-amount { grid-area: amt; display: flex; justify-content: space-between; align-items: baseline; }
  .li-remove { grid-area: rm; }
  /* labels become visible on phones, where there is no column header */
  .li-label { position: static; width: auto; height: auto; overflow: visible; clip: auto; white-space: normal; display: block; font-size: 11px; color: var(--stone); margin-bottom: 3px; }
  .li-amount .li-label { margin: 0; }
  .totals { max-width: none; }
}
</style>
