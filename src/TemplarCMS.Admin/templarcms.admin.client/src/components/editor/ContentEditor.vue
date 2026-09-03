<script setup lang="ts">
import { computed, ref } from 'vue'
import type { ContentItemResponse } from '@/types/admin-api'
import type { EditorFieldModel } from '@/types/admin-ui'
import FieldRow from './FieldRow.vue'

const props = defineProps<{
  item: ContentItemResponse | null
  templateName: string | null
  fields: EditorFieldModel[]
  fieldForm: Record<string, string>
  isLoadingFields: boolean
  isSubmitting: boolean
}>()

const emit = defineEmits<{
  save: []
  fieldInput: [key: string, value: string]
  checkboxInput: [key: string, checked: boolean]
}>()

const openSections = ref<Record<string, boolean>>({})
const sections = computed(() => {
  const groups = new Map<string, EditorFieldModel[]>()
  for (const field of props.fields) {
    groups.set(field.sectionName, [...(groups.get(field.sectionName) ?? []), field])
  }
  return [...groups.entries()]
    .map(([name, fields]) => ({ name, fields }))
    .sort((left, right) => {
      if (left.name.localeCompare('Content', undefined, { sensitivity: 'accent' }) === 0) return -1
      if (right.name.localeCompare('Content', undefined, { sensitivity: 'accent' }) === 0) return 1
      return 0
    })
})

function isOpen(name: string) {
  return openSections.value[name] !== false
}

function toggleSection(name: string) {
  openSections.value[name] = !isOpen(name)
}
</script>

<template>
  <div class="flex min-w-0 flex-1 flex-col overflow-hidden bg-white">
    <div v-if="item" class="flex items-center gap-3 border-b border-stone-200 px-5 py-3">
      <span class="flex h-8 w-8 items-center justify-center rounded-lg bg-[#e8eaf8] text-[#5970e3]">□</span>
      <div class="min-w-0 flex-1">
        <h2 class="truncate text-base font-semibold text-stone-800">{{ item.name }}</h2>
        <p class="mt-0.5 text-[11px] text-stone-400">
          Template: <span class="text-stone-500">{{ templateName ?? item.templateId }}</span>
          <span class="mx-2 text-stone-200">|</span>{{ item.language }} <span class="mx-2 text-stone-200">|</span>v{{ item.version }}
        </p>
      </div>
    </div>

    <div v-if="!item" class="flex flex-1 items-center justify-center text-sm text-stone-400">
      Select an item to edit
    </div>

    <div v-else class="flex-1 overflow-y-auto">
      <div v-if="isLoadingFields" class="px-5 py-4 text-sm text-stone-400">Loading field definitions...</div>
      <div v-else-if="fields.length === 0" class="px-5 py-4 text-sm text-stone-400">This item has no editable fields.</div>
      <template v-else v-for="section in sections" :key="section.name">
        <button
          class="flex w-full items-center justify-between border-y border-stone-200 bg-stone-50 px-4 py-2.5 text-left"
          type="button"
          :aria-expanded="isOpen(section.name)"
          @click="toggleSection(section.name)"
        >
          <span class="text-xs font-semibold tracking-wide text-stone-600">{{ section.name }}</span>
          <span class="text-[10px] text-stone-400">{{ section.fields.length }} fields</span>
        </button>
        <div v-if="isOpen(section.name)">
          <FieldRow
            v-for="field in section.fields"
            :key="field.key"
            :field="field"
            :value="fieldForm[field.key] ?? ''"
            @input="emit('fieldInput', field.key, $event)"
            @checkbox="emit('checkboxInput', field.key, $event)"
          />
        </div>
      </template>
    </div>

    <div v-if="item" class="flex items-center gap-2 border-t border-stone-200 bg-stone-50 px-4 py-2.5">
      <button class="rounded-lg bg-[#5970e3] px-4 py-1.5 text-sm font-medium text-white shadow-sm hover:bg-[#4a5ed4] disabled:opacity-60" type="button" :disabled="isSubmitting" @click="emit('save')">
        {{ isSubmitting ? 'Saving...' : 'Save' }}
      </button>
      <span class="ml-auto text-[11px] text-stone-400">Changes are saved to the current language and version.</span>
    </div>
  </div>
</template>
