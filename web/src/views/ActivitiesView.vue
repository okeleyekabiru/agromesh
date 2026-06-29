<template>
  <div class="card">
    <div class="card-header">
      <div class="card-title">All Activities</div>
      <div style="display:flex;gap:8px;">
        <select class="form-select" style="width:auto;padding:5px 10px;font-size:11px;" v-model="filterSection">
          <option value="">All Sections</option>
          <option v-for="s in store.sections" :key="s.id" :value="s.name">{{ s.name }}</option>
        </select>
        <select class="form-select" style="width:auto;padding:5px 10px;font-size:11px;" v-model="filterType">
          <option value="">All Types</option>
          <option v-for="a in activityTypes" :key="a" :value="a">{{ a }}</option>
        </select>
      </div>
    </div>

    <ActivityRow v-for="a in filtered" :key="a.id" :activity="a" />

    <EmptyState
      v-if="filtered.length === 0"
      icon="📋"
      title="No activities logged"
      subtitle="Use the Log Activity button to record today's farm work."
    />
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { useAgroStore } from '@/stores/agro'
import ActivityRow from '@/components/ui/ActivityRow.vue'
import EmptyState from '@/components/ui/EmptyState.vue'

const store = useAgroStore()
const filterSection = ref('')
const filterType = ref('')

const activityTypes = ['Weeding','Spraying','Fertilizer Application','Maintenance','Harvesting','Clearing','Pruning','Pest Control']

const filtered = computed(() =>
  store.activities.filter(a => {
    const sec = !filterSection.value || a.sectionName === filterSection.value
    const typ = !filterType.value || a.activityType === filterType.value
    return sec && typ
  })
)
</script>
