<template>
  <div class="modal-overlay" @click.self="$emit('close')">
    <div class="modal">
      <div class="modal-header">
        <div class="modal-title">Add a new farm</div>
        <button class="modal-close" @click="$emit('close')">×</button>
      </div>
      <div class="modal-body">
        <div class="form-group">
          <label class="form-label">Farm name</label>
          <input class="form-input" v-model="name" placeholder="e.g. Riverside Cassava Farm" />
        </div>
        <div class="form-group">
          <label class="form-label">State</label>
          <select class="form-select" v-model="stateId">
            <option value="" disabled>— Select state —</option>
            <option v-for="s in store.states" :key="s.id" :value="s.id">{{ s.name }}</option>
          </select>
        </div>
        <div class="form-group">
          <label class="form-label">LGA <span class="opt">optional</span></label>
          <input class="form-input" v-model="lgaFreetext" placeholder="e.g. Ikeja" />
        </div>
        <div v-if="error" class="error-msg">{{ error }}</div>
      </div>
      <div class="modal-footer">
        <button class="btn btn-ghost" @click="$emit('close')" :disabled="saving">Cancel</button>
        <button class="btn btn-primary" @click="submit" :disabled="!canSubmit || saving">
          {{ saving ? 'Creating…' : 'Create farm' }}
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

const emit = defineEmits(['close'])
const router = useRouter()
const store = useAgroStore()

const name = ref('')
const stateId = ref('')
const lgaFreetext = ref('')
const saving = ref(false)
const error = ref('')

const canSubmit = computed(() => name.value.trim().length > 0 && !!stateId.value)

async function submit() {
  if (!canSubmit.value) return
  saving.value = true
  error.value = ''
  try {
    await store.addFarm({
      name: name.value.trim(),
      stateId: stateId.value,
      lgaFreetext: lgaFreetext.value.trim() || undefined,
    })
    emit('close')
    router.push({ name: 'sections-setup' })
  } catch (e) {
    error.value = extractErrorMessage(e, 'Could not create this farm. Please try again.')
  } finally {
    saving.value = false
  }
}

onMounted(() => {
  store.fetchStates()
})
</script>

<style scoped>
.modal-overlay { position: fixed; inset: 0; background: rgba(10,26,16,0.6); backdrop-filter: blur(4px); z-index: 200; display: flex; align-items: center; justify-content: center; }
.modal { background: white; border-radius: 16px; width: 400px; box-shadow: 0 24px 64px rgba(10,26,16,0.2); }
.modal-header { padding: 20px 24px 16px; border-bottom: 1px solid var(--fog); display: flex; align-items: center; justify-content: space-between; }
.modal-title { font-family: 'Syne', sans-serif; font-weight: 700; font-size: 15px; color: var(--forest); }
.modal-close { width: 28px; height: 28px; border-radius: 6px; border: 1px solid var(--fog); background: transparent; cursor: pointer; font-size: 18px; color: var(--stone); }
.modal-body { padding: 20px 24px; }
.modal-footer { padding: 16px 24px; border-top: 1px solid var(--fog); display: flex; justify-content: space-between; }
.form-group { margin-bottom: 16px; }
.opt { font-weight: 400; text-transform: none; letter-spacing: 0; color: var(--stone); }
.error-msg { font-size: 11.5px; color: var(--alert); line-height: 1.5; }
</style>
