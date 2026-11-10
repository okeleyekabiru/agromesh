<template>
  <AuthShell title="Forgot your password?" subtitle="Enter your email and we will send you a link to choose a new one.">
    <div v-if="sent" class="ok-msg">If an account exists for that email, a reset link is on its way. It expires in 1 hour.</div>
    <form v-else class="form" @submit.prevent="submit">
      <label>Email<input v-model.trim="email" type="email" autocomplete="email" required /></label>
      <div v-if="error" class="error-msg">{{ error }}</div>
      <button class="btn btn-primary" :disabled="saving">{{ saving ? 'Sending…' : 'Send reset link' }}</button>
    </form>
    <router-link class="back" to="/">Back to sign in</router-link>
  </AuthShell>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { authApi } from '@/services/api'
import { extractErrorMessage } from '@/utils/http'
import AuthShell from '@/components/ui/AuthShell.vue'

const email = ref('')
const saving = ref(false)
const sent = ref(false)
const error = ref('')

async function submit() {
  saving.value = true
  error.value = ''
  try {
    await authApi.forgotPassword({ email: email.value })
    sent.value = true
  } catch (e) {
    // The API answers the same for known and unknown emails; only throttling or network problems land here.
    error.value = extractErrorMessage(e, 'Please try again in a few minutes.')
  } finally {
    saving.value = false
  }
}
</script>

<style scoped>
.form { display: flex; flex-direction: column; gap: 12px; }
label { display: flex; flex-direction: column; gap: 5px; font-size: 11.5px; font-weight: 600; color: var(--stone); }
input { border: 1.5px solid var(--fog); border-radius: 9px; padding: 10px 12px; font-size: 14px; background: #fff; color: var(--forest); font-weight: 400; }
input:focus { outline: none; border-color: var(--palm); }
.btn { justify-content: center; padding: 11px; }
.error-msg { font-size: 13px; color: var(--alert); }
.ok-msg { font-size: 13.5px; color: var(--leaf); line-height: 1.5; }
.back { display: inline-block; margin-top: 16px; font-size: 13px; color: var(--leaf); }
</style>
