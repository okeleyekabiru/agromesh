<template>
  <div class="card">
    <div class="card-header">
      <div class="card-title">All Activities</div>
      <div style="display:flex;gap:8px;">
        <select class="form-select" style="width:auto;padding:5px 10px;font-size:11px;" v-model="filterBlock">
          <option value="">All Blocks</option>
          <option v-for="s in store.blocks" :key="s.id" :value="s.id">{{ s.name }}</option>
        </select>
        <select class="form-select" style="width:auto;padding:5px 10px;font-size:11px;" v-model="filterType">
          <option value="">All Types</option>
          <option v-for="a in activityTypes" :key="a.key" :value="a.key">{{ a.label }}</option>
        </select>
      </div>
    </div>

    <div v-if="store.loading" style="padding:24px;text-align:center;color:var(--stone);font-size:12px;">Loading activities…</div>
    <ActivityRow v-for="a in filtered" :key="a.id" :activity="a" />
    <EmptyState
      v-if="!store.loading && filtered.length === 0"
      icon="📋"
      title="No activities logged"
      subtitle="Use the Log Activity button to record today's farm work."
    />
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useAgroStore } from '@/stores/agro'
import ActivityRow from '@/components/ui/ActivityRow.vue'
import EmptyState from '@/components/ui/EmptyState.vue'

const store = useAgroStore()
const filterBlock = ref('')
const filterType = ref('')

const activityTypes = [
  { key: 'Weeding',               label: 'Weeding' },
  { key: 'Spraying',              label: 'Spraying' },
  { key: 'FertilizerApplication', label: 'Fertilizer Application' },
  { key: 'Maintenance',           label: 'Maintenance' },
  { key: 'Harvesting',            label: 'Harvesting' },
  { key: 'Clearing',              label: 'Clearing' },
  { key: 'Pruning',               label: 'Pruning' },
  { key: 'PestControl',           label: 'Pest Control' },
]

const filtered = computed(() =>
  store.activities.filter(a => {
    const sec = !filterBlock.value || a.blockId === filterBlock.value
    const typ = !filterType.value || a.activityType === filterType.value
    return sec && typ
  })
)

onMounted(() => {
  store.fetchActivities()
  if (!store.blocks.length) store.fetchBlocks()
})
</script>
