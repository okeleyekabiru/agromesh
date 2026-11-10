<template>
  <div>
    <AlertBanner v-if="!canInvite">Only an admin or a farm manager can invite people.</AlertBanner>

    <div v-else class="team-grid" :class="{ single: !isAdmin }">
      <!-- People (admin only: farm managers can invite supervisors but not see or disable accounts) -->
      <div v-if="isAdmin" class="card">
        <div class="head">
          <div>
            <div class="card-title">Team</div>
            <div class="sub">{{ activeCount }} active · {{ team.length - activeCount }} disabled</div>
          </div>
        </div>
        <PageLoader v-if="loading" label="Loading team…" compact />
        <div v-for="m in team" :key="m.id" class="member" :class="{ off: !m.isActive }">
          <div class="avatar" :class="{ off: !m.isActive }">{{ initials(m.fullName) }}</div>
          <div class="who">
            <div class="name">{{ m.fullName }}</div>
            <div class="mail">{{ m.email }}</div>
          </div>
          <div class="tags">
            <StatBadge :variant="roleVariant(m.role)">{{ roleLabel(m.role) }}</StatBadge>
            <StatBadge v-if="!m.isActive" variant="red">Disabled</StatBadge>
          </div>
          <button v-if="m.role !== 'Admin'" class="btn btn-ghost btn-sm act" :class="{ danger: m.isActive }" :disabled="busy[m.id]" @click="toggle(m)">
            {{ busy[m.id] ? '…' : m.isActive ? 'Disable' : 'Enable' }}
          </button>
        </div>
        <div v-if="!loading && team.length === 1" class="empty">Only you so far. Invite a farm manager to share the work.</div>
        <div v-if="rowError" class="error-msg pad">{{ rowError }}</div>
      </div>

      <!-- Invite -->
      <div class="card">
        <div class="head">
          <div>
            <div class="card-title">Invite someone</div>
            <div class="sub">They get an email with a link to set their own password and join. The link works once and expires in 7 days.</div>
          </div>
        </div>
        <form class="invite" @submit.prevent="invite">
          <label>Email<input v-model.trim="form.email" type="email" placeholder="ada@example.com" required autocomplete="off" /></label>
          <label>Role
            <select v-model="form.role">
              <option v-if="isAdmin" value="FarmManager">Farm manager</option>
              <option value="Supervisor">Supervisor</option>
            </select>
          </label>
          <div class="role-help">{{ ROLE_HELP[form.role] }}</div>
          <div v-if="error" class="error-msg">{{ error }}</div>
          <div v-if="done" class="ok-msg">{{ done }}</div>
          <button class="btn btn-primary" :disabled="saving">{{ saving ? 'Sending…' : 'Send invitation' }}</button>
        </form>

        <div v-if="invitations.length" class="invites">
          <div class="invites-title">Invitations</div>
          <div v-for="i in invitations" :key="i.id" class="inv-row">
            <div class="who">
              <div class="name">{{ i.email }}</div>
              <div class="mail">{{ roleLabel(i.role) }} · expires {{ formatDate(i.expiresAt) }}</div>
            </div>
            <StatBadge :variant="statusVariant(i.status)">{{ i.status }}</StatBadge>
            <template v-if="i.status === 'Pending' || i.status === 'Expired'">
              <button class="btn btn-ghost btn-sm" :data-test="'resend-' + i.id" :disabled="busy[i.id]" @click="resend(i)">Resend</button>
              <button class="btn btn-ghost btn-sm danger" :data-test="'revoke-' + i.id" :disabled="busy[i.id]" @click="revoke(i)">Revoke</button>
            </template>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import { api, invitationApi } from '@/services/api'
import { useAgroStore } from '@/stores/agro'
import { extractErrorMessage } from '@/utils/http'
import AlertBanner from '@/components/ui/AlertBanner.vue'
import StatBadge from '@/components/ui/StatBadge.vue'

interface Member { id: string; fullName: string; email: string; role: string; isActive: boolean }
interface Invitation { id: string; email: string; role: string; status: string; createdAt: string; expiresAt: string }

const store = useAgroStore()
// UX hint only: the API refuses everyone but an admin.
const isAdmin = computed(() => store.currentUser?.role === 'Admin')
const canInvite = computed(() => isAdmin.value || store.currentUser?.role === 'FarmManager')

const team = ref<Member[]>([])
const loading = ref(false)
const saving = ref(false)
const error = ref('')
const done = ref('')
const rowError = ref('')
const busy = reactive<Record<string, boolean>>({})
const invitations = ref<Invitation[]>([])
const form = reactive({ email: '', role: store.currentUser?.role === 'Admin' ? 'FarmManager' : 'Supervisor' })

const initials = (name: string) => name.split(/\s+/).filter(Boolean).slice(0, 2).map(p => p[0].toUpperCase()).join('')
const activeCount = computed(() => team.value.filter(m => m.isActive).length)
const roleVariant = (r: string) => (r === 'Admin' ? 'green' : r === 'FarmManager' ? 'blue' : 'stone') as 'green' | 'blue' | 'stone'
const ROLE_HELP: Record<string, string> = {
  FarmManager: 'Runs the farm day to day: logs activities, manages blocks, workers, stock, orders and finance. Cannot invite people or disable accounts.',
  Supervisor: 'Logs activities and adds workers for the blocks they oversee. Cannot invite people or disable accounts.'
}

const roleLabel = (r: string) => ({ FarmManager: 'Farm manager', Supervisor: 'Supervisor', Admin: 'Admin' } as Record<string, string>)[r] ?? r

const formatDate = (iso: string) => new Date(iso).toLocaleDateString('en-GB', { day: 'numeric', month: 'short' })
const statusVariant = (st: string) => (st === 'Pending' ? 'blue' : st === 'Accepted' ? 'green' : st === 'Revoked' ? 'red' : 'stone') as 'green' | 'blue' | 'red' | 'stone'

async function loadInvitations() {
  try { invitations.value = (await invitationApi.list()).data ?? [] } catch (e) { rowError.value = extractErrorMessage(e, 'Could not load invitations.') }
}

async function resend(i: Invitation) {
  busy[i.id] = true
  rowError.value = ''
  try { await invitationApi.resend(i.id); await loadInvitations() } catch (e) { rowError.value = extractErrorMessage(e, 'Could not resend this invitation.') } finally { busy[i.id] = false }
}

async function revoke(i: Invitation) {
  busy[i.id] = true
  rowError.value = ''
  try { await invitationApi.revoke(i.id); await loadInvitations() } catch (e) { rowError.value = extractErrorMessage(e, 'Could not revoke this invitation.') } finally { busy[i.id] = false }
}

async function load() {
  if (!canInvite.value) return
  await loadInvitations()
  if (!isAdmin.value) return
  loading.value = true
  try { team.value = (await api.get('/api/users')).data ?? [] } catch (e) { rowError.value = extractErrorMessage(e, 'Could not load the team.') } finally { loading.value = false }
}

async function invite() {
  saving.value = true
  error.value = ''
  done.value = ''
  try {
    await invitationApi.create({ email: form.email, role: form.role })
    done.value = `Invitation sent to ${form.email}.`
    form.email = ''
    await loadInvitations()
  } catch (e) {
    error.value = extractErrorMessage(e, 'Could not send this invitation.')
  } finally {
    saving.value = false
  }
}

async function toggle(m: Member) {
  busy[m.id] = true
  rowError.value = ''
  try {
    await api.patch(`/api/users/${m.id}/status`, { isActive: !m.isActive })
    await load()
  } catch (e) {
    rowError.value = extractErrorMessage(e, 'Could not change this account.')
  } finally {
    busy[m.id] = false
  }
}

onMounted(load)
</script>

<style scoped>
.team-grid { display: grid; grid-template-columns: minmax(0, 1.6fr) minmax(300px, 1fr); gap: 16px; align-items: start; }
.team-grid.single { grid-template-columns: minmax(300px, 520px); }
@media (max-width: 900px) { .team-grid { grid-template-columns: 1fr; } }
.head { padding: 16px 20px 12px; border-bottom: 1px solid var(--fog); }
.sub { font-size: 12px; color: var(--stone); margin-top: 2px; }
.member { display: flex; align-items: center; gap: 14px; padding: 14px 20px; border-bottom: 1px solid var(--fog); }
.member:last-of-type { border-bottom: none; }
.avatar { width: 38px; height: 38px; border-radius: 50%; background: var(--leaf); color: #fff; display: flex; align-items: center; justify-content: center; font-size: 13px; font-weight: 700; flex-shrink: 0; }
.avatar.off { background: var(--fog); color: var(--stone); }
.who { flex: 1; min-width: 0; }
.name { font-size: 13.5px; font-weight: 600; color: var(--forest); }
.member.off .name { color: var(--stone); }
.mail { font-size: 12px; color: var(--stone); overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.tags { display: flex; gap: 6px; flex-wrap: wrap; justify-content: flex-end; }
.act { min-width: 78px; }
.act.danger { color: var(--alert); border-color: rgba(220,60,60,0.35); }
.empty { padding: 20px; font-size: 12.5px; color: var(--stone); }
.invite { display: flex; flex-direction: column; gap: 12px; padding: 16px 20px 20px; }
.invite label { display: flex; flex-direction: column; gap: 5px; font-size: 11.5px; font-weight: 600; color: var(--stone); letter-spacing: 0.02em; }
.invite input, .invite select { border: 1.5px solid var(--fog); border-radius: 9px; padding: 10px 12px; font-size: 13px; background: #fff; color: var(--forest); font-weight: 400; }
.invite input:focus, .invite select:focus { outline: none; border-color: var(--palm); }
.role-help { font-size: 12px; line-height: 1.5; color: var(--stone); background: var(--mist); border-radius: 9px; padding: 10px 12px; }
.invite .btn { justify-content: center; padding: 11px; }
.error-msg { font-size: 12px; color: var(--alert); }
.error-msg.pad { padding: 0 20px 14px; }
.ok-msg { font-size: 12px; color: var(--leaf); }
.invites { border-top: 1px solid var(--fog); padding: 12px 20px 16px; }
.invites-title { font-size: 11.5px; font-weight: 700; color: var(--stone); letter-spacing: 0.04em; text-transform: uppercase; margin-bottom: 6px; }
.inv-row { display: flex; align-items: center; gap: 8px; padding: 8px 0; border-bottom: 1px solid var(--fog); flex-wrap: wrap; }
.inv-row:last-child { border-bottom: none; }
/* The email and role line get the full width; the badge and buttons sit on the line below so nothing overlaps. */
.inv-row .who { flex: 1 1 100%; min-width: 0; }
.inv-row .who .name, .inv-row .who .mail { white-space: normal; overflow: visible; text-overflow: clip; overflow-wrap: anywhere; }
.inv-row .btn { margin-left: 0; }
.inv-row .btn:first-of-type { margin-left: auto; }
.inv-row .btn.danger { color: var(--alert); border-color: rgba(220,60,60,0.35); }
</style>
