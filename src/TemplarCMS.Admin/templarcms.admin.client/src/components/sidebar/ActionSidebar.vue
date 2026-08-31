<script setup lang="ts">
import type { ContentItemResponse } from '@/types/admin-api'
import { ALL_ICONS, ICON_LABELS } from '@/types/icons'

defineProps<{
  item: ContentItemResponse | null
  show: boolean
  isSubmitting: boolean
  canDelete: boolean
}>()

const emit = defineEmits<{
  close: []
  save: []
  delete: []
  updateIcon: [icon: string | null]
}>()
</script>

<template>
  <aside v-if="show" class="flex w-56 flex-col overflow-y-auto border-l border-stone-200 bg-white">
    <div class="flex items-center justify-between border-b border-stone-100 px-4 py-3">
      <span class="text-xs font-semibold uppercase tracking-wider text-stone-600">Actions</span>
      <button class="text-lg leading-none text-stone-400 hover:text-stone-600" type="button" @click="emit('close')">×</button>
    </div>
    <div v-if="item" class="border-b border-stone-100 bg-stone-50 px-3 py-2.5">
      <p class="truncate text-[12px] font-semibold text-stone-700">{{ item.name }}</p>
      <p class="mt-1 truncate text-[10px] text-stone-400">{{ item.path }}</p>
    </div>
    <div v-else class="px-4 py-5 text-xs text-stone-400">Select content to use item actions.</div>
    <div v-if="item" class="flex-1 px-2 py-2">
      <label class="mb-3 block px-3 text-[11px] font-medium text-stone-600">
        <span class="mb-1 block">Tree icon</span>
        <select
          class="w-full rounded border border-stone-200 bg-white px-2 py-1.5 text-xs text-stone-700"
          :value="item.icon ?? ''"
          :disabled="isSubmitting"
          @change="emit('updateIcon', ($event.target as HTMLSelectElement).value || null)"
        >
          <option value="">Use template icon</option>
          <option v-for="icon in ALL_ICONS" :key="icon" :value="icon">{{ ICON_LABELS[icon] }}</option>
        </select>
      </label>
      <button class="mb-1 w-full rounded-lg px-3 py-2.5 text-left text-[13px] font-medium text-[#5970e3] hover:bg-[#f0f0fd] disabled:opacity-60" type="button" :disabled="isSubmitting" @click="emit('save')">Save changes</button>
      <button class="mb-1 w-full rounded-lg px-3 py-2.5 text-left text-[13px] font-medium text-rose-600 hover:bg-rose-50 disabled:opacity-60" type="button" :disabled="isSubmitting || !canDelete" @click="emit('delete')">Delete item</button>
      <p v-if="!canDelete" class="px-3 text-[10px] leading-relaxed text-stone-400">Delete is available after direct children are removed.</p>
    </div>
    <div v-if="item" class="px-4 pb-4">
      <div class="rounded-lg bg-stone-50 p-3 ring-1 ring-stone-100">
        <p class="mb-1.5 text-[10px] font-semibold uppercase tracking-wider text-stone-400">API Path</p>
        <p class="break-all font-mono text-[10px] leading-relaxed text-stone-600">{{ item._links.self.href }}</p>
      </div>
    </div>
  </aside>
</template>
