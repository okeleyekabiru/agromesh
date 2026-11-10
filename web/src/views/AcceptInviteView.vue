<template>
  <AuthShell :title="invite ? `Join ${invite.farmName}` : 'Accept invitation'" :subtitle="invite ? `You were invited as ${roleLabel}. Set a password to continue.` : undefined">
    <PageLoader v-if="loading" label="Checking your invitation…" compact />
    <div v-else-if="loadError" class="error-msg">{{ loadError }}</div>
    <form v-else-if="invite" class="form" @submit.prevent="submit">
      <label>Email<input :value="invite.email" type="email" disabled /></label>
      <label>Full name<input v-model.trim="fullName" name="fullName" autocomplete="name" required /></label>
      <label>Password<input v-model="password" name="password" type="password" autocomplete="new-password" placeholder="At least 8 characters" required /></label>
      <div v-if="error" class="error-msg">{{ error }}</div>
      <button class="btn btn-primary" :disabled="saving">{{ saving ? 'Joining…' : 'Join the farm' }}</button>
    </form>
  </AuthShell>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { invitationApi } from '@/services/api'
import { useAgroStore } from '@/stores/agro'
import { extractErrorMessage } from '@/utils/http'
import AuthShell from '@/components/ui/AuthShell.vue'
import PageLoader from '@/components/ui/PageLoader.vue'

interface Preview { email: string; farmName: string; role: string }

const route = useRoute()
const router = useRouter()
const store = useAgroStore()
const token = String(route.query.token ?? '')

const invite = ref<Preview | null>(null)
const loading = ref(true)
const loadError = ref('')
const saving = ref(false)
const error = ref('')
const fullName = ref('')
const password = ref('')

const roleLabel = computed(() => ({ FarmManager: 'a farm manager', Supervisor: 'a supervisor' } as Record<string, string>)[invite.value?.role ?? ''] ?? 'a team member')

onMounted(async () => {
  try {
    invite.value = (await invitationApi.preview(token)).data
  } catch (e) {
    loadError.value = extractErrorMessage(e, 'This invitation link is invalid or has expired.')
  } finally {
    loading.value = false
  }
})

async function submit() {
  error.value = ''
  if (!fullName.value) { error.value = 'Enter your full name.'; return }
  if (password.value.length < 8) { error.value = 'Password must be at least 8 characters.'; return }
  saving.value = true
  try {
    await store.acceptInvitation({ token, fullName: fullName.value, password: password.value })
    await store.initDashboard()
    router.push({ name: 'dashboard' })
  } catch (e) {
    error.value = extractErrorMessage(e, 'Could not accept this invitation.')
  } finally {
    saving.value = false
  }
}
</script>

<style scoped>
.form { display: flex; flex-direction: column; gap: 12px; }
label { display: flex; flex-direction: column; gap: 5px; font-size: 11.5px; font-weight: 600; color: var(--stone); }
input { border: 1.5px solid var(--fog); border-radius: 9px; padding: 10px 12px; font-size: 14px; background: #fff; color: var(--forest); font-weight: 400; }
input:disabled { background: var(--mist); }
input:focus { outline: none; border-color: var(--palm); }
.btn { justify-content: center; padding: 11px; }
.error-msg { font-size: 13px; color: var(--alert); }
</style>
