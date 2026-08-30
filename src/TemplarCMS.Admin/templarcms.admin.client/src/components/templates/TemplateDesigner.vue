<script setup lang="ts">
import { ref, computed } from 'vue'
import { TEMPLATES_LIST, STANDARD_PAGE_TEMPLATE_SECTIONS, FIELD_TYPE_LABELS } from '@/data/content'
import { ICON_SVG } from '@/data/icons'
import ScopeBadge from '@/components/ui/ScopeBadge.vue'

const selectedTplId = ref('tpl-standard')
const template = computed(() => TEMPLATES_LIST.find(t => t.id === selectedTplId.value) ?? TEMPLATES_LIST[0])
const sections = computed(() => STANDARD_PAGE_TEMPLATE_SECTIONS)
</script>

<template>
  <div class="flex-1 flex min-w-0 overflow-hidden">
    <!-- Template list sidebar -->
    <div class="w-52 flex flex-col bg-[#f7f5f1] border-r border-stone-200 overflow-y-auto">
      <p class="px-3 pt-3 pb-1.5 text-[11px] font-semibold uppercase tracking-widest text-stone-400">Templates</p>
      <button
        v-for="tpl in TEMPLATES_LIST"
        :key="tpl.id"
        @click="selectedTplId = tpl.id"
        class="flex items-center gap-2.5 px-3 py-2 text-left text-sm transition-colors"
        :class="tpl.id === selectedTplId
          ? 'bg-[#e8eaf8] text-[#3a4eb0] font-medium'
          : 'text-stone-600 hover:bg-stone-100'"
      >
        <svg width="14" height="14" viewBox="0 0 16 16" xmlns="http://www.w3.org/2000/svg"
             :class="tpl.id === selectedTplId ? 'text-[#5970e3]' : 'text-stone-400'"
             v-html="ICON_SVG[tpl.iconKey]" />
        <span class="truncate text-[13px]">{{ tpl.name }}</span>
      </button>
    </div>

    <!-- Template detail -->
    <div class="flex-1 flex flex-col overflow-hidden bg-white">
      <!-- Header -->
      <div class="flex items-center gap-3 px-5 py-3 border-b border-stone-200">
        <div class="w-9 h-9 flex items-center justify-center rounded-lg bg-[#e8eaf8]">
          <svg width="18" height="18" viewBox="0 0 16 16" xmlns="http://www.w3.org/2000/svg"
               class="text-[#5970e3]" v-html="ICON_SVG[template.iconKey]" />
        </div>
        <div>
          <h2 class="text-base font-semibold text-stone-800">{{ template.name }}</h2>
          <p class="text-[11px] text-stone-400">
            Inherits from: <span class="text-stone-500">{{ template.base }}</span>
          </p>
        </div>
        <div class="ml-auto flex gap-2">
          <button class="px-3 py-1.5 rounded-lg bg-[#5970e3] text-white text-xs font-medium hover:bg-[#4a5ed4] transition-colors">
            Add Section
          </button>
          <button class="px-3 py-1.5 rounded-lg ring-1 ring-stone-200 text-stone-600 text-xs hover:bg-stone-50 transition-colors">
            Add Field
          </button>
        </div>
      </div>

      <!-- Sections -->
      <div class="flex-1 overflow-y-auto">
        <div v-for="section in sections" :key="section.name" class="border-b border-stone-100">
          <!-- Section row -->
          <div class="flex items-center gap-2 px-4 py-2.5 bg-stone-50">
            <span class="text-xs font-semibold text-stone-600">{{ section.name }}</span>
            <span
              v-if="section.source === 'inherited'"
              class="text-[9px] px-1.5 py-0.5 rounded bg-amber-100 text-amber-600 font-semibold uppercase tracking-wider"
            >
              Inherited · {{ section.inheritedFrom }}
            </span>
            <span class="ml-auto text-[10px] text-stone-400">{{ section.fields.length }} fields</span>
          </div>

          <!-- Field rows -->
          <div
            v-for="field in section.fields"
            :key="field.name"
            class="flex items-center gap-3 px-4 py-2 border-b border-stone-50 last:border-0 hover:bg-stone-50 group"
          >
            <span class="w-36 text-[12px] font-medium text-stone-700 mono truncate">{{ field.name }}</span>
            <ScopeBadge :scope="field.scope" />
            <span class="text-[11px] text-stone-400">{{ FIELD_TYPE_LABELS[field.type] ?? field.type }}</span>
            <div v-if="section.source === 'own'" class="ml-auto flex gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
              <button class="px-2 py-0.5 rounded text-[10px] text-stone-500 hover:bg-stone-100">Edit</button>
              <button class="px-2 py-0.5 rounded text-[10px] text-rose-500 hover:bg-rose-50">Remove</button>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
