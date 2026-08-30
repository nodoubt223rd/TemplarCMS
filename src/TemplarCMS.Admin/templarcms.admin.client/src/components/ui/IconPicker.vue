<script setup lang="ts">
import { ref, computed } from 'vue'
import { ALL_ICONS, ICON_LABELS, ICON_SVG } from '@/data/icons'
import type { IconKey } from '@/types'

const props = defineProps<{ modelValue: IconKey }>()
const emit = defineEmits<{ (e: 'update:modelValue', v: IconKey): void }>()

const open = ref(false)
const query = ref('')

const filtered = computed(() => {
  const q = query.value.toLowerCase()
  return q ? ALL_ICONS.filter(k => ICON_LABELS[k].toLowerCase().includes(q)) : ALL_ICONS
})

function pick(k: IconKey) {
  emit('update:modelValue', k)
  open.value = false
  query.value = ''
}
</script>

<template>
  <div class="relative inline-block">
    <button
      @click="open = !open"
      class="w-9 h-9 flex items-center justify-center rounded-lg bg-[#ede9e2] hover:bg-[#e2ddd5] transition-colors"
      title="Change icon"
    >
      <svg width="18" height="18" viewBox="0 0 16 16" xmlns="http://www.w3.org/2000/svg"
           class="text-[#5970e3]" v-html="ICON_SVG[modelValue]" />
    </button>

    <div
      v-if="open"
      class="absolute top-full left-0 mt-1.5 w-64 bg-white rounded-xl shadow-xl ring-1 ring-black/10 z-50 overflow-hidden"
    >
      <div class="p-2 border-b border-stone-100">
        <input
          v-model="query"
          placeholder="Search icons…"
          class="w-full px-2.5 py-1.5 text-xs rounded-md bg-stone-50 border border-stone-200 outline-none focus:border-[#5970e3]"
          autofocus
        />
      </div>
      <div class="grid grid-cols-6 gap-0.5 p-2 max-h-48 overflow-y-auto">
        <button
          v-for="k in filtered"
          :key="k"
          @click="pick(k)"
          :title="ICON_LABELS[k]"
          class="aspect-square flex items-center justify-center rounded-lg hover:bg-[#e8eaf8] transition-colors"
          :class="k === modelValue ? 'bg-[#e8eaf8] ring-1 ring-[#5970e3]' : ''"
        >
          <svg width="16" height="16" viewBox="0 0 16 16" xmlns="http://www.w3.org/2000/svg"
               class="text-stone-600" v-html="ICON_SVG[k]" />
        </button>
        <p v-if="!filtered.length" class="col-span-6 text-xs text-stone-400 text-center py-4">No icons found</p>
      </div>
    </div>

    <div v-if="open" @click="open = false; query = ''" class="fixed inset-0 z-40" />
  </div>
</template>
