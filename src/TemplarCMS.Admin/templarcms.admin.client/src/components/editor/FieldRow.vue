<script setup lang="ts">
import type { EditorFieldModel } from '@/types/admin-ui'

defineProps<{
  field: EditorFieldModel
  value: string
}>()

const emit = defineEmits<{
  input: [value: string]
  checkbox: [checked: boolean]
}>()
</script>

<template>
  <div class="grid gap-1.5 border-b border-stone-100 px-4 py-3 last:border-0">
    <div class="flex items-center gap-2">
      <label :for="`field-${field.key}`" class="text-[12px] font-semibold text-stone-700">{{ field.label }}</label>
      <span class="rounded bg-stone-100 px-1.5 py-px text-[9px] font-semibold uppercase tracking-wider text-stone-500">{{ field.scopeLabel }}</span>
      <span class="ml-auto text-[10px] text-stone-400">{{ field.type }}</span>
    </div>
    <p v-if="field.helpText" class="text-[11px] text-stone-400">{{ field.helpText }}</p>
    <label v-if="field.editorKind === 'checkbox'" class="flex w-fit items-center gap-2 text-xs text-stone-600">
      <input :id="`field-${field.key}`" class="h-4 w-4 accent-[#5970e3]" type="checkbox" :checked="value === 'true'" @change="emit('checkbox', ($event.target as HTMLInputElement).checked)" />
      Enabled
    </label>
    <textarea
      v-else-if="field.editorKind === 'textarea'"
      :id="`field-${field.key}`"
      class="w-full resize-y rounded-lg bg-white px-2.5 py-1.5 text-sm text-stone-700 outline-none ring-1 ring-stone-200 focus:ring-[#5970e3]/50"
      :rows="field.rows ?? 3"
      :placeholder="field.placeholder ?? ''"
      :value="value"
      @input="emit('input', ($event.target as HTMLTextAreaElement).value)"
    />
    <input
      v-else
      :id="`field-${field.key}`"
      class="w-full rounded-lg bg-white px-2.5 py-1.5 text-sm text-stone-700 outline-none ring-1 ring-stone-200 focus:ring-[#5970e3]/50"
      :type="field.inputType"
      :step="field.step ?? undefined"
      :placeholder="field.placeholder ?? ''"
      :value="value"
      @input="emit('input', ($event.target as HTMLInputElement).value)"
    />
  </div>
</template>
