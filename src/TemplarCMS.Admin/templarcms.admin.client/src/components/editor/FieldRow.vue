<script setup lang="ts">
import { computed } from 'vue'
import type { FieldDef } from '@/types'
import ScopeBadge from '@/components/ui/ScopeBadge.vue'
import { FIELD_TYPE_LABELS } from '@/data/content'

const props = defineProps<{ field: FieldDef; readonly?: boolean }>()
const emit = defineEmits<{ (e: 'update', id: string, v: string | boolean): void }>()

const typeLabel = computed(() => FIELD_TYPE_LABELS[props.field.type] ?? props.field.type)
const isReadonly = computed(() => props.readonly || props.field.system)

function onInput(e: Event) {
  emit('update', props.field.id, (e.target as HTMLInputElement).value)
}
function onCheck(e: Event) {
  emit('update', props.field.id, (e.target as HTMLInputElement).checked)
}
</script>

<template>
  <div
    class="grid gap-1.5 px-4 py-3 border-b border-stone-100 last:border-0"
    :class="field.system ? 'bg-stone-50/60' : ''"
  >
    <!-- label row -->
    <div class="flex items-center gap-2">
      <span class="text-[12px] font-semibold text-stone-700 mono tracking-tight">{{ field.name }}</span>
      <ScopeBadge :scope="field.scope" />
      <span class="text-[10px] text-stone-400 ml-auto">{{ typeLabel }}</span>
    </div>

    <!-- checkbox -->
    <label v-if="field.type === 'checkbox'" class="flex items-center gap-2 cursor-pointer w-fit">
      <input
        type="checkbox"
        :checked="!!field.value"
        :disabled="isReadonly"
        @change="onCheck"
        class="w-4 h-4 rounded accent-[#5970e3]"
      />
      <span class="text-xs text-stone-500">{{ field.value ? 'Yes' : 'No' }}</span>
    </label>

    <!-- richtext -->
    <div v-else-if="field.type === 'richtext'"
         class="relative rounded-lg ring-1 ring-stone-200 focus-within:ring-[#5970e3]/50 overflow-hidden">
      <div class="flex items-center gap-1 px-2 py-1 bg-stone-50 border-b border-stone-100">
        <button v-for="t in ['B','I','U','H2','—']" :key="t"
          class="px-1.5 py-0.5 rounded text-[10px] font-bold text-stone-500 hover:bg-stone-200 transition-colors">{{ t }}</button>
      </div>
      <textarea
        :value="String(field.value)"
        :readonly="isReadonly"
        @input="onInput"
        rows="3"
        class="w-full px-3 py-2 text-sm text-stone-700 bg-white outline-none resize-none"
      />
    </div>

    <!-- droplist -->
    <select
      v-else-if="field.type === 'droplist'"
      :value="String(field.value)"
      :disabled="isReadonly"
      @change="onInput"
      class="w-full px-2.5 py-1.5 rounded-lg text-sm text-stone-700 bg-white ring-1 ring-stone-200 focus:ring-[#5970e3]/50 outline-none"
    >
      <option v-for="opt in (field.options ?? [])" :key="opt" :value="opt">{{ opt }}</option>
    </select>

    <!-- everything else -->
    <input
      v-else
      :type="field.type === 'datetime' ? 'datetime-local' : field.type === 'integer' ? 'number' : 'text'"
      :value="String(field.value)"
      :readonly="isReadonly"
      :placeholder="field.placeholder ?? ''"
      @input="onInput"
      class="w-full px-2.5 py-1.5 rounded-lg text-sm text-stone-700 bg-white ring-1 ring-stone-200 focus:ring-[#5970e3]/50 outline-none transition-shadow"
      :class="isReadonly ? 'bg-stone-50 text-stone-400' : ''"
    />
  </div>
</template>
