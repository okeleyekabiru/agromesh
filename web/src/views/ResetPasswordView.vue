<template>
  <AuthShell title="Choose a new password">
    <div v-if="done" class="ok-msg">
      Your password has been changed. You can sign in now.
      <div><router-link class="back" to="/">Go to sign in</router-link></div>
    </div>
    <form v-else class="form" @submit.prevent="submit">
      <label>New password<input v-model="password" name="password" type="password" autocomplete="new-password" placeholder="At least 8 characters" required /></label>
      <div v-if="error" class="error-msg">{{ error }}</div>
      <button class="btn btn-primary" :disabled="saving">{{ saving ? 'Saving…' : 'Change password' }}</button>
      <router-link class="back" to="/forgot-password">Send me a new link</router-link>
    </form>
  </AuthShell>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useRoute } from 'vue-router'
import { authApi } from '@/services/api'
import { extractErrorMessage } from '@/utils/http'
import AuthShell from '@/components/ui/AuthShell.vue'

const token = String(useRoute().query.token ?? '')
const password = ref('')
const saving = ref(false)
const done = ref(false)
const error = ref('')

async function submit() {
  error.value = ''
  if (password.value.length < 8) { error.value = 'Password must be at least 8 characters.'; return }
  saving.value = true
  try {
    await authApi.resetPassword({ token, newPassword: password.value })
    done.value = true
  } catch (e) {
    error.value = extractErrorMessage(e, 'Could not change your password.')
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
.back { display: inline-block; margin-top: 8px; font-size: 13px; color: var(--leaf); }
</style>
