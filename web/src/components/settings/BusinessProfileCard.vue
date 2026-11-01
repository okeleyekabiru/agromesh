<template>
  <section class="card" aria-labelledby="bp-heading">
    <div class="card-header">
      <h2 id="bp-heading" class="card-title">Business profile</h2>
    </div>

    <div v-if="loading" class="bp-state" role="status">Loading business profile…</div>
    <div v-else-if="loadError" class="bp-state bp-state--error" role="alert">
      {{ loadError }}
      <button type="button" class="btn btn-ghost btn-sm" @click="load">Try again</button>
    </div>

    <form v-else class="bp-form" novalidate @submit.prevent="save">
      <p class="bp-help">
        These details are printed on your invoices: the footer contact rows and, when an invoice has no
        payment details of its own, the payment method.
      </p>
      <p v-if="!canEdit" class="bp-note">Only farm owners and managers can edit the business profile.</p>

      <fieldset class="bp-fieldset" :disabled="!canEdit || saving">
        <legend class="bp-legend">Contact</legend>
        <div class="bp-grid">
          <div class="bp-field bp-field--wide">
            <label class="form-label" for="bp-address">Address</label>
            <textarea id="bp-address" class="form-input bp-textarea" v-model="form.address" maxlength="300" rows="2" autocomplete="street-address" />
          </div>
          <div class="bp-field">
            <label class="form-label" for="bp-phone">Phone</label>
            <input id="bp-phone" class="form-input" type="tel" v-model="form.phone" maxlength="60" autocomplete="tel" />
          </div>
          <div class="bp-field">
            <label class="form-label" for="bp-email">Email</label>
            <input
              id="bp-email" class="form-input" type="email" v-model="form.email" maxlength="200" autocomplete="email"
              :aria-invalid="!!emailError" :aria-describedby="emailError ? 'bp-email-err' : undefined"
            />
            <div v-if="emailError" id="bp-email-err" class="bp-field-error">{{ emailError }}</div>
          </div>
          <div class="bp-field bp-field--wide">
            <label class="form-label" for="bp-website">Website</label>
            <input id="bp-website" class="form-input" type="text" inputmode="url" v-model="form.website" maxlength="200" autocomplete="url" />
          </div>
        </div>
      </fieldset>

      <fieldset class="bp-fieldset" :disabled="!canEdit || saving">
        <legend class="bp-legend">Bank account for payments</legend>
        <div class="bp-grid">
          <div class="bp-field">
            <label class="form-label" for="bp-acc-name">Account name</label>
            <input id="bp-acc-name" class="form-input" type="text" v-model="form.bankAccountName" maxlength="150" />
          </div>
          <div class="bp-field">
            <label class="form-label" for="bp-acc-no">Account number</label>
            <input id="bp-acc-no" class="form-input" type="text" inputmode="numeric" v-model="form.bankAccountNumber" maxlength="40" autocomplete="off" />
          </div>
          <div class="bp-field bp-field--wide">
            <label class="form-label" for="bp-bank">Bank name</label>
            <input id="bp-bank" class="form-input" type="text" v-model="form.bankName" maxlength="100" />
          </div>
        </div>
      </fieldset>

      <div v-if="canEdit" class="bp-footer">
        <button type="submit" class="btn btn-primary" :disabled="saving">{{ saving ? 'Saving…' : 'Save business profile' }}</button>
        <span class="bp-status" role="status" aria-live="polite">
          <span v-if="saved" class="bp-ok">Saved.</span>
        </span>
      </div>
      <div v-if="saveError" class="bp-error" role="alert">{{ saveError }}</div>
    </form>
  </section>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { useAgroStore } from '@/stores/agro'
import { businessProfileApi } from '@/services/api'
import { canManageFarm } from '@/utils/roles'
import { extractProblemMessage } from '@/utils/http'
import type { FarmBusinessProfile } from '@/types'

const store = useAgroStore()
const farmId = computed(() => store.farm?.id ?? store.activeFarmId)
const canEdit = computed(() => canManageFarm(store.currentUser?.role))

const blank = (): Record<keyof FarmBusinessProfile, string> => ({
  address: '', phone: '', email: '', website: '', bankAccountName: '', bankAccountNumber: '', bankName: ''
})
const form = reactive(blank())
const loading = ref(true)
const loadError = ref('')
const saving = ref(false)
const saved = ref(false)
const saveError = ref('')
const emailError = ref('')

async function load() {
  if (!farmId.value) return
  loading.value = true
  loadError.value = ''
  try {
    const res = await businessProfileApi.get(farmId.value)
    const p = res.data as Partial<FarmBusinessProfile>
    for (const k of Object.keys(form) as (keyof FarmBusinessProfile)[]) form[k] = p[k] ?? ''
  } catch (e) {
    loadError.value = extractProblemMessage(e, 'Could not load the business profile.')
  } finally {
    loading.value = false
  }
}

onMounted(load)
// The active farm can finish loading after this card is shown, or change while it is open.
watch(farmId, (n, o) => { if (n && n !== o) load() })

const EMAIL_RE = /^[^\s@]+@[^\s@]+\.[^\s@]+$/

async function save() {
  if (!farmId.value) return
  saved.value = false
  saveError.value = ''
  emailError.value = ''
  if (form.email.trim() && !EMAIL_RE.test(form.email.trim())) {
    emailError.value = 'Enter a valid email address, for example accounts@yourfarm.com.'
    return
  }
  saving.value = true
  try {
    // PUT replaces the whole profile: a blank field clears it.
    const body = Object.fromEntries(
      (Object.keys(form) as (keyof FarmBusinessProfile)[]).map(k => [k, form[k].trim() || null])
    )
    await businessProfileApi.update(farmId.value, body)
    saved.value = true
  } catch (e) {
    saveError.value = extractProblemMessage(e, 'Could not save the business profile. Please try again.')
  } finally {
    saving.value = false
  }
}
</script>

<style scoped>
.bp-state { padding: 28px 20px; font-size: 13px; color: var(--stone); display: flex; align-items: center; gap: 12px; flex-wrap: wrap; }
.bp-state--error { color: #B42323; }
.bp-form { padding: 20px; }
.bp-help { margin: 0 0 14px; font-size: 12.5px; line-height: 1.6; color: var(--stone); }
.bp-note { margin: 0 0 14px; font-size: 12.5px; color: var(--stone); font-weight: 600; }
.bp-fieldset { border: none; padding: 0; margin: 0 0 18px; min-width: 0; }
.bp-legend { font-family: var(--font-sans); font-size: 13px; font-weight: 700; color: var(--forest); padding: 0; margin-bottom: 10px; }
.bp-grid { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 12px 16px; }
.bp-field--wide { grid-column: 1 / -1; }
.bp-textarea { min-height: 64px; resize: vertical; }
.bp-field-error { margin-top: 4px; font-size: 12px; color: #B42323; }
.bp-footer { display: flex; align-items: center; gap: 12px; flex-wrap: wrap; }
.bp-status { font-size: 12px; min-height: 18px; }
.bp-ok { color: var(--leaf); font-weight: 600; }
.bp-error { margin-top: 10px; font-size: 12px; color: #B42323; line-height: 1.5; }
@media (max-width: 640px) {
  .bp-grid { grid-template-columns: minmax(0, 1fr); }
}
</style>
