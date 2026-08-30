<script setup lang="ts">
import { ref, computed } from 'vue'
import { findItem } from '@/composables/useTree'
import { HOME_SECTIONS } from '@/data/content'
import type { IconKey, SectionDef } from '@/types'
import FieldRow from './FieldRow.vue'
import StatusPill from '@/components/ui/StatusPill.vue'
import IconPicker from '@/components/ui/IconPicker.vue'
import ContentInspector from './ContentInspector.vue'

const props = defineProps<{
  selectedId: string
  language: string
  version: string
  iconOverrides: Record<string, IconKey>
}>()

const emit = defineEmits<{
  (e: 'update-icon', id: string, key: IconKey): void
}>()

const item = computed(() => findItem(props.selectedId))
const openSections = ref<Record<string, boolean>>({ 's-content': true, 's-cta': true, 's-seo': false, 's-nav': false, 's-system': false })

const sections = computed<SectionDef[]>(() => {
  if (!item.value) return []
  return HOME_SECTIONS
})

function toggleSection(id: string) {
  openSections.value[id] = !openSections.value[id]
}

const iconModel = computed({
  get: () => (props.iconOverrides[props.selectedId] ?? item.value?.iconKey ?? 'file') as IconKey,
  set: (v: IconKey) => emit('update-icon', props.selectedId, v),
})
</script>

<template>
  <div class="flex-1 flex flex-col min-w-0 overflow-hidden">
    <!-- Editor header -->
    <div v-if="item" class="flex items-center gap-3 px-5 py-3 border-b border-stone-200 bg-white">
      <IconPicker v-model="iconModel" />
      <div class="flex-1 min-w-0">
        <h2 class="text-base font-semibold text-stone-800 leading-tight truncate">{{ item.label }}</h2>
        <p class="text-[11px] text-stone-400 mt-0.5">
          Template: <span class="text-stone-500">{{ item.template ?? '—' }}</span>
          <span class="mx-2 text-stone-200">|</span>
          Lang: <span class="text-stone-500">{{ language }}</span>
          <span class="mx-2 text-stone-200">|</span>
          Version: <span class="text-stone-500">{{ version }}</span>
        </p>
      </div>
      <StatusPill v-if="item.status" :status="item.status" />
    </div>

    <!-- Content Inspector -->
    <ContentInspector
      v-if="item"
      :selectedId="selectedId"
      :language="language"
      :version="version"
    />

    <!-- Sections -->
    <div class="flex-1 overflow-y-auto bg-white">
      <div v-if="!item" class="flex items-center justify-center h-full text-stone-400 text-sm">
        Select an item to edit
      </div>

      <template v-for="section in sections" :key="section.id">
        <!-- Section header -->
        <button
          @click="toggleSection(section.id)"
          class="w-full flex items-center justify-between px-4 py-2.5 bg-stone-50 border-b border-stone-200 hover:bg-stone-100 transition-colors"
          :class="section.system ? 'opacity-70' : ''"
        >
          <div class="flex items-center gap-2">
            <svg width="10" height="10" viewBox="0 0 8 8" fill="none" stroke="currentColor" stroke-width="1.8"
                 stroke-linecap="round" stroke-linejoin="round" class="text-stone-400 transition-transform"
                 :class="openSections[section.id] ? 'rotate-90' : ''">
              <path d="M2 1.5l3 2.5-3 2.5"/>
            </svg>
            <span class="text-xs font-semibold text-stone-600 tracking-wide">{{ section.name }}</span>
            <span v-if="section.system" class="text-[9px] uppercase tracking-wider text-stone-400 font-medium">System</span>
          </div>
          <span class="text-[10px] text-stone-400">{{ section.fields.length }} fields</span>
        </button>

        <!-- Fields -->
        <div v-if="openSections[section.id]">
          <FieldRow
            v-for="field in section.fields"
            :key="field.id"
            :field="field"
            :readonly="!!section.system"
          />
        </div>
      </template>
    </div>

    <!-- Save toolbar -->
    <div v-if="item" class="flex items-center gap-2 px-4 py-2.5 bg-stone-50 border-t border-stone-200">
      <button class="px-4 py-1.5 rounded-lg bg-[#5970e3] text-white text-sm font-medium hover:bg-[#4a5ed4] transition-colors shadow-sm">
        Save
      </button>
      <button class="px-3 py-1.5 rounded-lg text-stone-600 text-sm hover:bg-stone-100 transition-colors">
        Discard
      </button>
      <span class="ml-auto text-[11px] text-stone-400">Auto-saved 2 min ago</span>
    </div>
  </div>
</template>
