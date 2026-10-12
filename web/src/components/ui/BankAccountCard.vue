<template>
  <div class="card bank-card">
    <div class="bank-head">
      <div>
        <div class="bank-title">Bank account</div>
        <div class="bank-sub" v-if="status?.linked">
          Linked {{ formatDate(status.linkedAt) }}
          <template v-if="status.lastSyncAt"> · last synced {{ formatDate(status.lastSyncAt) }}</template>
        </div>
        <div class="bank-sub" v-else>
          Link the farm's bank account to bring in income and capital, and to confirm paid orders automatically.
        </div>
      </div>
      <div class="bank-actions">
        <template v-if="status?.linked">
          <button class="btn-secondary" :disabled="busy" @click="sync">{{ busy ? 'Working…' : 'Sync now' }}</button>
          <button class="btn-link" :disabled="busy" @click="unlink">Unlink</button>
        </template>
        <button v-else class="btn-primary" :disabled="busy || !status?.publicKey" @click="openWidget">
          {{ busy ? 'Working…' : 'Link bank account' }}
        </button>
      </div>
    </div>

    <div v-if="status && !status.linked && !status.publicKey" class="bank-hint">
      Bank linking is not set up on the server yet (no Mono public key).
    </div>
    <div v-if="message" class="bank-msg">{{ message }}</div>
    <div v-if="error" class="error-msg">{{ error }}</div>

    <div v-if="status?.linked" class="bank-rules">
      Write the narration as <b>order / block / type</b>, for example <b>ORD-1042/Alpha/Fertilizer</b>. The order is
      marked paid when the number and amount match, and the spend goes to that block and type. Money in is income,
      unless the narration says <b>capital</b> or <b>investment</b>.
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import Connect from '@mono.co/connect.js'
import { bankApi } from '@/services/api'
import { useAgroStore } from '@/stores/agro'
import { extractErrorMessage } from '@/utils/http'
import type { BankStatus } from '@/types'

const emit = defineEmits<{ (e: 'changed'): void }>()

const store = useAgroStore()
const status = ref<BankStatus | null>(null)
const busy = ref(false)
const message = ref('')
const error = ref('')

function formatDate(iso: string | null | undefined) {
  return iso ? new Date(iso).toLocaleDateString('en-GB', { day: 'numeric', month: 'short', year: 'numeric' }) : ''
}

async function load() {
  try {
    status.value = (await bankApi.status()).data
  } catch (e) {
    error.value = extractErrorMessage(e, 'Could not load the bank account.')
  }
}

function openWidget() {
  if (!status.value?.publicKey) return
  error.value = ''
  message.value = ''
  const user = store.currentUser
  const connect = new Connect({
    key: status.value.publicKey,
    scope: 'auth',
    data: { customer: { name: user?.fullName ?? 'Farm owner', email: user?.email ?? '' } },
    // Mono hands back a one-time code; the server swaps it for the account id, so the secret key never reaches the browser.
    onSuccess: async ({ code }) => {
      busy.value = true
      try {
        const res = await bankApi.link(code)
        message.value = `Bank account linked. ${res.data.newTransactions ?? 0} transactions imported.`
        await load()
        emit('changed')
      } catch (e) {
        error.value = extractErrorMessage(e, 'Could not link the bank account.')
      } finally {
        busy.value = false
      }
    },
    onClose: () => { /* closed without linking: nothing to do */ }
  })
  connect.setup()
  connect.open()
}

async function sync() {
  busy.value = true
  error.value = ''
  message.value = ''
  try {
    const res = await bankApi.sync()
    message.value = `${res.data.newTransactions ?? 0} new transactions.`
    await load()
    emit('changed')
  } catch (e) {
    error.value = extractErrorMessage(e, 'Could not sync the bank account.')
  } finally {
    busy.value = false
  }
}

async function unlink() {
  if (!confirm('Unlink this bank account? Transactions already imported are kept.')) return
  busy.value = true
  error.value = ''
  try {
    await bankApi.unlink()
    message.value = ''
    await load()
  } catch (e) {
    error.value = extractErrorMessage(e, 'Could not unlink the bank account.')
  } finally {
    busy.value = false
  }
}

onMounted(load)
</script>

<style scoped>
.bank-card { padding: 14px 16px; margin-bottom: 16px; }
.bank-head { display: flex; align-items: center; justify-content: space-between; gap: 12px; flex-wrap: wrap; }
.bank-title { font-weight: 600; color: var(--forest); font-size: 14px; }
.bank-sub { font-size: 12px; color: var(--stone); margin-top: 2px; max-width: 520px; }
.bank-actions { display: flex; gap: 8px; align-items: center; }
.bank-hint, .bank-msg, .bank-rules { font-size: 12px; margin-top: 10px; color: var(--stone); }
.bank-msg { color: var(--palm); }
.btn-link { background: none; border: none; color: var(--stone); font-size: 12px; cursor: pointer; text-decoration: underline; }
</style>
