<template>
  <div class="card pref">
    <div class="card-title">Email notifications</div>
    <label class="row">
      <input type="checkbox" :checked="enabled" :disabled="loading || saving" @change="toggle(($event.target as HTMLInputElement).checked)" />
      <span>Send me optional emails, such as when someone I invited joins.</span>
    </label>
    <p class="note">Security and account emails (invitations, password reset, password changed) are always sent.</p>
    <div v-if="error" class="error-msg">{{ error }}</div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { emailPreferenceApi } from '@/services/api'
import { extractErrorMessage } from '@/utils/http'

const enabled = ref(true)
const loading = ref(true)
const saving = ref(false)
const error = ref('')

onMounted(async () => {
  try { enabled.value = (await emailPreferenceApi.get()).data.enabled } catch (e) { error.value = extractErrorMessage(e, 'Could not load this setting.') } finally { loading.value = false }
})

async function toggle(next: boolean) {
  const previous = enabled.value
  enabled.value = next
  saving.value = true
  error.value = ''
  try {
    enabled.value = (await emailPreferenceApi.set(next)).data.enabled
  } catch (e) {
    enabled.value = previous
    error.value = extractErrorMessage(e, 'Could not save this setting.')
  } finally {
    saving.value = false
  }
}
</script>

<style scoped>
.pref { padding: 16px 20px; margin-top: 16px; }
.row { display: flex; align-items: center; gap: 10px; font-size: 13.5px; margin-top: 10px; }
.note { font-size: 12px; color: var(--stone); margin: 8px 0 0; }
.error-msg { font-size: 12px; color: var(--alert); margin-top: 8px; }
</style>
