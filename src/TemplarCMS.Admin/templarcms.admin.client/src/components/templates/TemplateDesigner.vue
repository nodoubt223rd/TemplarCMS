<script setup lang="ts">
import type { TemplateResponse, TemplateSummaryResponse } from '@/types/admin-api'

defineProps<{
  templates: TemplateSummaryResponse[]
  selectedTemplateId: string | null
  selectedTemplate: TemplateResponse | null
  isLoading: boolean
}>()

const emit = defineEmits<{ select: [templateId: string] }>()

function scopeLabel(field: { isShared: boolean; isUnversioned: boolean }): string {
  if (field.isShared) return 'Shared'
  if (field.isUnversioned) return 'Unversioned'
  return 'Versioned'
}
</script>

<template>
  <div class="flex min-w-0 flex-1 overflow-hidden">
    <aside class="flex w-52 flex-col overflow-y-auto border-r border-stone-200 bg-[#f7f5f1]">
      <p class="px-3 pb-1.5 pt-3 text-[11px] font-semibold uppercase tracking-widest text-stone-400">Templates</p>
      <div v-if="isLoading" class="px-3 py-2 text-xs text-stone-400">Loading templates...</div>
      <button
        v-for="template in templates"
        :key="template.id"
        class="flex items-center gap-2.5 px-3 py-2 text-left text-sm transition-colors"
        :class="template.id === selectedTemplateId ? 'bg-[#e8eaf8] font-medium text-[#3a4eb0]' : 'text-stone-600 hover:bg-stone-100'"
        type="button"
        @click="emit('select', template.id)"
      >
        <span class="text-stone-400" aria-hidden="true">▧</span>
        <span class="truncate text-[13px]">{{ template.name }}</span>
      </button>
    </aside>

    <section class="flex min-w-0 flex-1 flex-col overflow-hidden bg-white">
      <div v-if="selectedTemplate" class="flex items-center gap-3 border-b border-stone-200 px-5 py-3">
        <span class="flex h-9 w-9 items-center justify-center rounded-lg bg-[#e8eaf8] text-[#5970e3]">▧</span>
        <div class="min-w-0">
          <h2 class="truncate text-base font-semibold text-stone-800">{{ selectedTemplate.name }}</h2>
          <p class="text-[11px] text-stone-400">Inherits from: <span class="text-stone-500">{{ selectedTemplate.baseTemplate?.name ?? 'None' }}</span></p>
        </div>
      </div>
      <div v-else class="flex flex-1 items-center justify-center text-sm text-stone-400">Select a template to inspect it.</div>

      <div v-if="selectedTemplate" class="flex-1 overflow-y-auto">
        <section v-for="section in selectedTemplate.sections" :key="section.id" class="border-b border-stone-100">
          <div class="flex items-center gap-2 bg-stone-50 px-4 py-2.5">
            <span class="text-xs font-semibold text-stone-600">{{ section.name }}</span>
            <span class="ml-auto text-[10px] text-stone-400">{{ section.fields.length }} fields</span>
          </div>
          <div v-for="field in section.fields" :key="field.id" class="flex items-center gap-3 border-b border-stone-50 px-4 py-2 last:border-0">
            <span class="w-36 truncate font-mono text-[12px] font-medium text-stone-700">{{ field.name }}</span>
            <span class="rounded bg-stone-100 px-1.5 py-px text-[9px] font-semibold uppercase tracking-wider text-stone-500">{{ scopeLabel(field) }}</span>
            <span class="text-[11px] text-stone-400">{{ field.type }}</span>
          </div>
        </section>
      </div>
    </section>
  </div>
</template>
