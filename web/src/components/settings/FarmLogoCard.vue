<template>
  <section class="card logo-card" aria-labelledby="logo-heading">
    <div class="card-header">
      <h2 id="logo-heading" class="card-title">Farm logo</h2>
    </div>
    <div class="logo-body">
      <div class="logo-preview" :class="{ 'logo-preview--empty': !store.farm?.hasLogo }">
        <FarmLogo
          :farm-id="farmId"
          :has-logo="store.farm?.hasLogo"
          :version="store.farm?.logoUpdatedAt"
          :farm-name="store.farm?.name"
          variant="preview"
        >
          <template #fallback>
            <div class="logo-empty">
              <IlevoraMark :size="44" />
              <span>{{ store.farm?.hasLogo ? 'The logo could not be loaded' : 'No logo uploaded yet' }}</span>
            </div>
          </template>
        </FarmLogo>
      </div>

      <div class="logo-side">
        <p class="logo-help">
          Printed at the top of your invoices. The app keeps showing the Ilevora logo.
          PNG, JPEG or WebP, up to 2 MB and 4096 x 4096 pixels.
        </p>

        <p v-if="!canEdit" class="logo-note">Only farm owners and managers can change the logo.</p>

        <div v-else class="logo-actions">
          <input
            ref="fileInput"
            type="file"
            class="logo-file"
            accept="image/png,image/jpeg,image/webp"
            tabindex="-1"
            aria-hidden="true"
            @change="onPick"
          />
          <button type="button" class="btn btn-primary" :disabled="busy || !farmId" @click="fileInput?.click()">
            {{ busy ? 'Uploading…' : store.farm?.hasLogo ? 'Replace logo' : 'Upload logo' }}
          </button>
          <button
            v-if="store.farm?.hasLogo"
            type="button"
            class="btn btn-ghost"
            :disabled="busy"
            @click="confirmRemove = true"
          >Remove logo</button>
        </div>

        <div class="logo-status" role="status" aria-live="polite">
          <span v-if="message" class="logo-ok">{{ message }}</span>
        </div>
        <div v-if="error" class="logo-error" role="alert">{{ error }}</div>
      </div>
    </div>

    <ConfirmDialog
      v-if="confirmRemove"
      title="Remove the farm logo?"
      message="New invoice PDFs will print the farm name instead of the logo."
      confirm-label="Remove logo"
      :busy="busy"
      :error="removeError"
      @cancel="confirmRemove = false"
      @confirm="remove"
    />
  </section>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue'
import { useAgroStore } from '@/stores/agro'
import FarmLogo from '@/components/ui/FarmLogo.vue'
import IlevoraMark from '@/components/ui/IlevoraMark.vue'
import ConfirmDialog from '@/components/modal/ConfirmDialog.vue'
import { canManageFarm } from '@/utils/roles'
import { validateLogoFile, readImageSize, LOGO_MAX_DIMENSION } from '@/utils/files'
import { extractProblemMessage } from '@/utils/http'

const store = useAgroStore()
const farmId = computed(() => store.farm?.id ?? store.activeFarmId)
const canEdit = computed(() => canManageFarm(store.currentUser?.role))

const fileInput = ref<HTMLInputElement | null>(null)
const busy = ref(false)
const message = ref('')
const error = ref('')
const confirmRemove = ref(false)
const removeError = ref('')

async function onPick(e: Event) {
  const input = e.target as HTMLInputElement
  const file = input.files?.[0]
  input.value = '' // allow picking the same file again
  if (!file || !farmId.value) return
  message.value = ''
  error.value = ''

  const check = validateLogoFile(file)
  if (!check.ok) { error.value = check.message; return }
  const size = await readImageSize(file)
  if (!size) { error.value = 'That file could not be read as an image.'; return }
  if (size.width > LOGO_MAX_DIMENSION || size.height > LOGO_MAX_DIMENSION) {
    error.value = `That image is ${size.width} x ${size.height} pixels. The largest allowed is ${LOGO_MAX_DIMENSION} x ${LOGO_MAX_DIMENSION}.`
    return
  }

  busy.value = true
  try {
    await store.uploadFarmLogo(farmId.value, file)
    message.value = 'Logo saved.'
  } catch (err) {
    error.value = extractProblemMessage(err, 'Could not upload the logo. Please try again.')
  } finally {
    busy.value = false
  }
}

async function remove() {
  if (!farmId.value) return
  busy.value = true
  removeError.value = ''
  try {
    await store.removeFarmLogo(farmId.value)
    confirmRemove.value = false
    message.value = 'Logo removed.'
    error.value = ''
  } catch (err) {
    removeError.value = extractProblemMessage(err, 'Could not remove the logo. Please try again.')
  } finally {
    busy.value = false
  }
}
</script>

<style scoped>
.logo-card { margin-bottom: 16px; overflow: visible; }
.logo-body { display: flex; gap: 24px; padding: 20px; align-items: flex-start; flex-wrap: wrap; }
.logo-preview {
  width: 220px; max-width: 100%; min-height: 140px; padding: 16px;
  display: flex; align-items: center; justify-content: center;
  border: 1px dashed var(--fog); border-radius: 12px; background: var(--mist);
}
.logo-preview:not(.logo-preview--empty) { background: white; border-style: solid; }
.logo-empty { display: flex; flex-direction: column; align-items: center; gap: 8px; text-align: center; font-size: 12px; color: var(--stone); }
.logo-side { flex: 1; min-width: 240px; }
.logo-help { margin: 0 0 14px; font-size: 12.5px; line-height: 1.6; color: var(--stone); }
.logo-note { margin: 0; font-size: 12.5px; color: var(--stone); }
.logo-file { display: none; }
.logo-actions { display: flex; gap: 8px; flex-wrap: wrap; }
.logo-status { min-height: 20px; margin-top: 10px; font-size: 12px; }
.logo-ok { color: var(--leaf); font-weight: 600; }
.logo-error { margin-top: 6px; font-size: 12px; color: #B42323; line-height: 1.5; }
</style>
