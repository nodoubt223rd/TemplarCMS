<script setup lang="ts">
import type { TemplateSummaryResponse } from '@/types/admin-api'

defineProps<{
  availableTemplates: TemplateSummaryResponse[]
  selectedTemplateId: string | null
  isLoadingTemplates: boolean
  selectedItemAvailable: boolean
}>()

const emit = defineEmits<{
  startNewTemplateDraft: []
  inspectSelectedItemTemplate: []
  refreshTemplateWorkspace: []
  selectTemplate: [templateId: string]
}>()
</script>

<template>
  <article class="panel template-panel">
    <div class="panel-header">
      <div>
        <p class="eyebrow">Templates</p>
        <h3>Catalog</h3>
      </div>
      <span class="panel-pill">{{ availableTemplates.length }} templates</span>
    </div>

    <div class="template-actions">
      <button
        class="button"
        type="button"
        @click="emit('startNewTemplateDraft')"
      >
        New Template Draft
      </button>
      <button
        class="button button--secondary"
        type="button"
        :disabled="!selectedItemAvailable"
        @click="emit('inspectSelectedItemTemplate')"
      >
        Inspect Selected Item Template
      </button>
      <button
        class="button button--secondary"
        type="button"
        :disabled="selectedTemplateId == null"
        @click="emit('refreshTemplateWorkspace')"
      >
        Refresh Template Detail
      </button>
    </div>

    <div v-if="isLoadingTemplates" class="empty-state">
      Loading templates from `/api/v1/templates`...
    </div>

    <div v-else-if="availableTemplates.length === 0" class="empty-state">
      No templates are currently available to inspect.
    </div>

    <ul v-else class="template-list">
      <li v-for="template in availableTemplates" :key="template.id">
        <button
          type="button"
          :class="['template-entry', { 'template-entry--selected': selectedTemplateId === template.id }]"
          @click="emit('selectTemplate', template.id)"
        >
          <span class="template-entry__name">{{ template.name }}</span>
          <span class="template-entry__meta">{{ template.key }}</span>
        </button>
      </li>
    </ul>
  </article>
</template>
