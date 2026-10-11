<template>
  <div class="review-shell">
    <div class="review-card">
      <div class="review-header">
        <div class="review-logo">AgroMesh</div>
        <div class="review-subtitle">Farm Setup · Review &amp; Confirm</div>
      </div>

      <div class="review-body">
        <div class="review-title">Review your farm setup</div>
        <p class="review-lead">Take a last look before you finish — you can always adjust blocks and boundaries later from the Blocks page.</p>

        <!-- Farm summary -->
        <div class="summary-block">
          <div class="summary-row">
            <span class="summary-label">Farm</span>
            <span class="summary-value">{{ store.farm?.name ?? '—' }}</span>
          </div>
          <div class="summary-row">
            <span class="summary-label">State</span>
            <span class="summary-value">{{ stateName }}</span>
          </div>
          <div class="summary-row" v-if="store.farm?.lgaFreetext">
            <span class="summary-label">LGA</span>
            <span class="summary-value">{{ store.farm.lgaFreetext }}</span>
          </div>
        </div>

        <!-- Blocks list -->
        <div class="blocks-list">
          <div class="blocks-list-label">
            Blocks ({{ store.wizardBlocks.length }})
          </div>
          <div v-if="!store.wizardBlocks.length" class="blocks-empty">
            No blocks added yet — you can add them later from the Blocks page.
          </div>
          <div v-else class="block-row" v-for="s in store.wizardBlocks" :key="s.id">
            <div class="block-row-name">{{ s.name }}</div>
            <div class="block-row-meta">
              <span class="block-row-area">{{ s.area.hectares.toFixed(2) }} ha</span>
              <span class="boundary-src-badge" :class="`src-${boundarySrcClass(s.boundarySource)}`">
                {{ s.boundarySource != null ? BLOCK_BOUNDARY_SOURCE_LABELS[s.boundarySource] : 'Manual estimate' }}
              </span>
            </div>
          </div>
        </div>

        <div v-if="confirmError" class="confirm-error">{{ confirmError }}</div>

        <button class="btn-confirm" @click="confirm" :disabled="confirming || !store.activeFarmId">
          <span v-if="confirming" class="spinner-sm" />
          {{ confirming ? 'Finishing…' : 'Confirm & Go to Dashboard' }}
        </button>
        <button class="btn-back" @click="router.push({ name: 'blocks-setup' })" :disabled="confirming">
          ← Back to blocks
        </button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useAgroStore } from '@/stores/agro'
import { extractErrorMessage } from '@/utils/http'
import { BLOCK_BOUNDARY_SOURCE_LABELS } from '@/types'

const router = useRouter()
const store = useAgroStore()

const BOUNDARY_SRC_CLASS = ['gps', 'upload', 'manual']
function boundarySrcClass(source: number | null) {
  return BOUNDARY_SRC_CLASS[source ?? 2]
}

const confirming = ref(false)
const confirmError = ref('')

const stateName = computed(() => {
  const id = store.farm?.stateId
  if (!id) return '—'
  return store.states.find(s => s.id === id)?.name ?? '—'
})

async function confirm() {
  if (!store.activeFarmId) return
  confirming.value = true
  confirmError.value = ''
  try {
    await store.completeFarmSetup(store.activeFarmId)
    store.wizardBlocks.length = 0
    router.push({ name: 'dashboard' })
  } catch (e) {
    confirmError.value = extractErrorMessage(e, 'Could not finish setup. Please try again.')
  } finally {
    confirming.value = false
  }
}

onMounted(async () => {
  await Promise.all([store.fetchFarm(), store.fetchStates()])
})
</script>

<style scoped>
.review-shell {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, #0d2b1f 0%, #1a4a33 100%);
  padding: 24px;
}

.review-card {
  background: white;
  border-radius: 20px;
  width: 100%;
  max-width: 560px;
  box-shadow: 0 32px 80px rgba(0,0,0,0.4);
  overflow: hidden;
}

.review-header {
  background: linear-gradient(135deg, #0d2b1f 0%, #2d7a4f 100%);
  padding: 24px 32px 20px;
}
.review-logo {
  font-family: 'Syne', sans-serif;
  font-weight: 700;
  font-size: 20px;
  color: #fff;
  letter-spacing: -0.01em;
}
.review-subtitle {
  font-family: 'JetBrains Mono', monospace;
  font-size: 9px;
  letter-spacing: 0.12em;
  text-transform: uppercase;
  color: rgba(255,255,255,0.6);
  margin-top: 2px;
}

.review-body { padding: 24px 32px 32px; }
.review-title {
  font-family: 'Syne', sans-serif;
  font-size: 18px;
  font-weight: 700;
  color: var(--forest);
  letter-spacing: -0.01em;
  margin-bottom: 6px;
}
.review-lead { font-size: 12px; color: var(--stone); line-height: 1.6; margin-bottom: 20px; }

.summary-block {
  background: rgba(45,122,79,0.05);
  border: 1px solid rgba(45,122,79,0.14);
  border-radius: 10px;
  padding: 12px 16px;
  margin-bottom: 16px;
}
.summary-row { display: flex; justify-content: space-between; padding: 4px 0; }
.summary-label { font-family: 'JetBrains Mono', monospace; font-size: 9px; letter-spacing: 0.1em; text-transform: uppercase; color: var(--stone); }
.summary-value { font-size: 12.5px; font-weight: 600; color: var(--forest); }

.blocks-list { margin-bottom: 16px; }
.blocks-list-label { font-family: 'JetBrains Mono', monospace; font-size: 9px; letter-spacing: 0.1em; text-transform: uppercase; color: var(--stone); margin-bottom: 8px; }
.blocks-empty { font-size: 12px; color: var(--stone); padding: 12px; text-align: center; background: var(--mist); border-radius: 8px; }
.block-row {
  display: flex; align-items: center; justify-content: space-between;
  padding: 9px 12px;
  border: 1px solid var(--fog);
  border-radius: 8px;
  margin-bottom: 6px;
}
.block-row-name { font-size: 12.5px; font-weight: 600; color: var(--forest); }
.block-row-meta { display: flex; align-items: center; gap: 8px; }
.block-row-area { font-family: 'JetBrains Mono', monospace; font-size: 11px; color: var(--stone); }

.boundary-src-badge {
  font-family: 'JetBrains Mono', monospace;
  font-size: 8px; padding: 2px 7px; border-radius: 4px;
  text-transform: uppercase; letter-spacing: 0.05em;
}
.boundary-src-badge.src-gps    { background: rgba(61,170,106,0.12); color: #2d7a4f; }
.boundary-src-badge.src-upload { background: rgba(91,79,207,0.12); color: #5b4fcf; }
.boundary-src-badge.src-manual { background: var(--fog); color: var(--stone); }

.confirm-error { font-size: 11.5px; color: #dc2626; margin-bottom: 12px; line-height: 1.5; }

.btn-confirm {
  width: 100%;
  display: flex; align-items: center; justify-content: center; gap: 8px;
  padding: 12px 16px;
  background: #3DAA6A;
  color: #fff;
  border: none;
  border-radius: 10px;
  font-size: 13px;
  font-weight: 700;
  cursor: pointer;
  transition: background 0.15s;
  margin-bottom: 10px;
}
.btn-confirm:hover:not(:disabled) { background: #2d7a4f; }
.btn-confirm:disabled { opacity: 0.55; cursor: not-allowed; }

.btn-back {
  width: 100%;
  padding: 8px;
  background: none;
  border: none;
  font-size: 11.5px;
  color: var(--stone);
  cursor: pointer;
  text-align: center;
}
.btn-back:hover:not(:disabled) { color: var(--forest); }

.spinner-sm {
  width: 12px; height: 12px;
  border: 2px solid rgba(255,255,255,0.35);
  border-top-color: #fff;
  border-radius: 50%;
  animation: spin 0.7s linear infinite;
}
@keyframes spin { to { transform: rotate(360deg); } }
</style>
