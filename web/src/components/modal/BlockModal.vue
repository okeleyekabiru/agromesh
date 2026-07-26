<template>
  <div class="modal-overlay" @click.self="$emit('close')">
    <div class="modal-wrap">
      <div class="modal">
        <div class="modal-header">
          <div class="modal-title">Blocks — {{ section.name }}</div>
          <button class="modal-close-inline" @click="$emit('close')">×</button>
        </div>
        <div class="modal-body">
          <div v-if="localBlocks.length" class="block-list">
            <div v-for="b in localBlocks" :key="b.id" class="block-row">
              <div class="block-name">{{ b.name }}</div>
              <div class="block-meta">
                {{ b.area.displayValue }} {{ b.area.displayUnit }}
                <span v-if="b.treeCount != null"> · {{ b.treeCount }} trees</span>
              </div>
            </div>
          </div>
          <div v-else class="block-empty">No blocks yet.</div>

          <div class="add-block-form">
            <div class="form-row">
              <input class="form-input" v-model="name" placeholder="Block name e.g. Block A" style="flex:2" />
              <input class="form-input" v-model.number="areaValue" type="number" min="0.1" step="0.5" placeholder="Area (ha)" style="flex:1" @wheel.prevent />
            </div>
            <input class="form-input" v-model.number="treeCount" type="number" min="0" placeholder="Tree count (optional)" @wheel.prevent />
            <div v-if="error" class="error-msg">{{ error }}</div>
            <button class="btn btn-primary" :disabled="!canAdd || saving" @click="addBlock">
              {{ saving ? 'Adding…' : '+ Add Block' }}
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { useAgroStore } from '@/stores/agro'
import { extractErrorMessage } from '@/utils/http'
import type { Section, Block } from '@/types'

const props = defineProps<{ section: Section }>()
defineEmits<{ close: [] }>()
const store = useAgroStore()

// Local copy so a newly-added block shows immediately without needing the whole page's
// section list to re-fetch and re-render this modal's props.
const localBlocks = ref<Block[]>([...props.section.blocks])

const name = ref('')
const areaValue = ref<number | null>(null)
const treeCount = ref<number | null>(null)
const saving = ref(false)
const error = ref('')

const canAdd = computed(() => name.value.trim().length > 0 && !!areaValue.value && areaValue.value > 0)

async function addBlock() {
  if (!canAdd.value) return
  saving.value = true
  error.value = ''
  try {
    const block = await store.createBlock(props.section.id, {
      name: name.value.trim(),
      areaValue: areaValue.value!,
      treeCount: treeCount.value || null,
    })
    localBlocks.value.push(block)
    name.value = ''
    areaValue.value = null
    treeCount.value = null
  } catch (e) {
    error.value = extractErrorMessage(e, 'Could not add this block.')
  } finally {
    saving.value = false
  }
}
</script>

<style scoped>
.modal-overlay {
  position: fixed; inset: 0; background: rgba(10,26,16,0.6); backdrop-filter: blur(4px);
  z-index: 200; display: flex; align-items: flex-start; justify-content: center;
  padding: 48px 24px; overflow-y: auto;
}
.modal-wrap { position: relative; }
.modal { background: white; border-radius: 16px; width: 380px; box-shadow: 0 24px 64px rgba(10,26,16,0.2); }
.modal-header { padding: 20px 24px 16px; border-bottom: 1px solid var(--fog); display: flex; align-items: center; justify-content: space-between; }
.modal-title { font-family: 'Syne', sans-serif; font-weight: 700; font-size: 15px; color: var(--forest); }
.modal-close-inline { width: 28px; height: 28px; border-radius: 6px; border: 1px solid var(--fog); background: transparent; cursor: pointer; font-size: 18px; color: var(--stone); }
.modal-body { padding: 20px 24px; }

.block-list { margin-bottom: 16px; display: flex; flex-direction: column; gap: 6px; }
.block-row { display: flex; align-items: center; justify-content: space-between; padding: 8px 10px; background: #f9fafb; border-radius: 8px; }
.block-name { font-size: 12px; font-weight: 600; color: var(--forest); }
.block-meta { font-family: 'JetBrains Mono', monospace; font-size: 10px; color: var(--stone); }
.block-empty { font-size: 12px; color: var(--stone); text-align: center; padding: 16px; background: #f9fafb; border-radius: 8px; margin-bottom: 16px; }

.add-block-form { display: flex; flex-direction: column; gap: 8px; padding-top: 12px; border-top: 1px solid var(--fog); }
.form-row { display: flex; gap: 8px; }
.form-input {
  padding: 7px 10px; border: 1px solid #e5e7eb; border-radius: 8px;
  font-size: 12px; color: #111827; background: #fff; outline: none; transition: border-color 0.15s;
}
.form-input:focus { border-color: #2d7a4f; }
.error-msg { font-size: 11px; color: var(--alert); line-height: 1.5; }
</style>
