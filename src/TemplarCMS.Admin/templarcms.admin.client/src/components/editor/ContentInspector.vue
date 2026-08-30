<script setup lang="ts">
import { computed, ref } from 'vue'
import { findItem } from '@/composables/useTree'

const props = defineProps<{
  selectedId: string
  language: string
  version: string
}>()

const item = computed(() => findItem(props.selectedId))

// Deterministic fake UUID from item id
function hashStr(s: string): number {
  let h = 0
  for (let i = 0; i < s.length; i++) h = (Math.imul(31, h) + s.charCodeAt(i)) | 0
  return h >>> 0
}
function fakeUuid(seed: string): string {
  const a = hashStr(seed).toString(16).padStart(8, '0')
  const b = hashStr(seed + '1').toString(16).padStart(4, '0').slice(0, 4)
  const c = '4' + hashStr(seed + '2').toString(16).padStart(3, '0').slice(0, 3)
  const d = hashStr(seed + '3').toString(16).padStart(4, '0').slice(0, 4)
  const e = hashStr(seed + '4').toString(16).padStart(8, '0') + hashStr(seed + '5').toString(16).padStart(4, '0').slice(0, 4)
  return `${a}-${b}-${c}-${d}-${e}`
}

const itemId       = computed(() => fakeUuid(props.selectedId))
const templateId   = computed(() => fakeUuid(props.selectedId + 'tpl'))
const itemPath     = computed(() => `/templar/content/${props.selectedId.replace(/-/g, '/')}`)
const parentLink   = computed(() => `/api/v1/content/${fakeUuid(props.selectedId + 'parent')}?lang=${props.language}&version=${props.version}`)
const templateName = computed(() => item.value?.template ?? 'Base Item')

// Copy to clipboard
const copied = ref<string | null>(null)
function copy(val: string, key: string) {
  navigator.clipboard.writeText(val)
  copied.value = key
  setTimeout(() => { if (copied.value === key) copied.value = null }, 1800)
}

const fields = computed(() => [
  { key: 'id',       label: 'ID',          value: itemId.value,     mono: true,  copyable: true  },
  { key: 'template', label: 'Template',    value: `${templateName.value} (${templateId.value})`, mono: false, copyable: false },
  { key: 'path',     label: 'Path',        value: itemPath.value,   mono: true,  copyable: true  },
  { key: 'parent',   label: 'Parent Link', value: parentLink.value, mono: true,  copyable: true  },
])
</script>

<template>
  <div v-if="item" class="border-b border-stone-200 bg-[#faf9f7]">
    <!-- Header bar -->
    <div class="flex items-center px-5 py-2.5 border-b border-stone-200">
      <span class="text-[10px] font-bold uppercase tracking-[0.12em] text-[#5970e3]">Content Inspector</span>
    </div>

    <!-- Fields grid -->
    <div class="grid grid-cols-2 gap-0 divide-x divide-stone-100">
      <div
        v-for="field in fields"
        :key="field.key"
        class="group flex flex-col gap-0.5 px-5 py-3 hover:bg-stone-50 transition-colors"
        :class="{ 'border-t border-stone-100': field.key === 'path' || field.key === 'template' }"
      >
        <div class="flex items-center gap-1.5">
          <span class="text-[9px] font-bold uppercase tracking-[0.12em] text-stone-400">{{ field.label }}</span>

          <!-- Copy button -->
          <button
            v-if="field.copyable"
            @click="copy(field.value, field.key)"
            class="opacity-0 group-hover:opacity-100 transition-opacity"
            :title="copied === field.key ? 'Copied!' : 'Copy'"
          >
            <svg v-if="copied !== field.key" width="11" height="11" viewBox="0 0 16 16" fill="none"
                 stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round"
                 class="text-stone-400 hover:text-[#5970e3]">
              <rect x="5" y="5" width="9" height="9" rx="1.5"/>
              <path d="M11 5V3.5A1.5 1.5 0 009.5 2h-6A1.5 1.5 0 002 3.5v6A1.5 1.5 0 003.5 11H5"/>
            </svg>
            <svg v-else width="11" height="11" viewBox="0 0 16 16" fill="none"
                 stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"
                 class="text-emerald-500">
              <path d="M3 8l3.5 3.5L13 4"/>
            </svg>
          </button>
        </div>

        <span
          class="text-[12px] text-stone-600 break-all leading-snug"
          :class="field.mono ? 'font-mono' : 'font-normal'"
        >{{ field.value }}</span>
      </div>
    </div>
  </div>
</template>
