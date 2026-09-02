<script setup lang="ts">
import { computed, ref } from 'vue'

export type MultilistOption = {
  value: string
  label: string
  description?: string
}

const props = defineProps<{
  available: MultilistOption[]
  modelValue: string[]
  readonly?: boolean
}>()

const emit = defineEmits<{ 'update:modelValue': [value: string[]] }>()

const query = ref('')
const availableSelection = ref<string[]>([])
const selectedSelection = ref<string[]>([])

const availableOptions = computed(() => {
  const selected = new Set(props.modelValue)
  const normalizedQuery = query.value.trim().toLowerCase()

  return props.available.filter(option =>
    !selected.has(option.value) &&
    (normalizedQuery.length === 0 || `${option.label} ${option.description ?? ''}`.toLowerCase().includes(normalizedQuery)))
})

const selectedOptions = computed(() =>
  props.modelValue
    .map(value => props.available.find(option => option.value === value))
    .filter((option): option is MultilistOption => option != null))

function addSelected(): void {
  const values = availableSelection.value.filter(value => availableOptions.value.some(option => option.value === value))
  if (values.length === 0) return

  emit('update:modelValue', [...props.modelValue, ...values])
  availableSelection.value = []
}

function removeSelected(): void {
  const values = new Set(selectedSelection.value)
  if (values.size === 0) return

  emit('update:modelValue', props.modelValue.filter(value => !values.has(value)))
  selectedSelection.value = []
}

function moveSelected(direction: -1 | 1): void {
  const values = new Set(selectedSelection.value)
  const next = [...props.modelValue]

  const indexes = next
    .map((value, index) => values.has(value) ? index : -1)
    .filter(index => index >= 0)
    .sort((left, right) => direction === -1 ? left - right : right - left)

  for (const index of indexes) {
    const target = index + direction
    const currentValue = next[index]
    const targetValue = next[target]
    if (target < 0 || target >= next.length || currentValue == null || targetValue == null || values.has(targetValue)) continue
    next[index] = targetValue
    next[target] = currentValue
  }

  emit('update:modelValue', next)
}
</script>

<template>
  <div class="grid gap-3 md:grid-cols-[1fr_auto_1fr]" data-testid="multilist-with-search">
    <section class="min-w-0 rounded border border-stone-200 bg-white">
      <label class="sr-only" for="available-template-search">Search available templates</label>
      <input
        id="available-template-search"
        v-model="query"
        class="w-full border-b border-stone-200 px-2.5 py-2 text-xs outline-none placeholder:text-stone-400"
        placeholder="Search templates"
        :disabled="readonly"
      >
      <select
        v-model="availableSelection"
        class="h-36 w-full bg-white p-1 text-xs outline-none"
        multiple
        :disabled="readonly"
        aria-label="Available templates"
      >
        <option v-for="option in availableOptions" :key="option.value" :value="option.value">
          {{ option.label }}{{ option.description ? ` (${option.description})` : '' }}
        </option>
      </select>
    </section>

    <div class="flex items-center justify-center gap-1 md:flex-col">
      <button class="rounded border border-stone-200 px-2 py-1 text-xs disabled:opacity-40" type="button" :disabled="readonly || availableSelection.length === 0" @click="addSelected">Add</button>
      <button class="rounded border border-stone-200 px-2 py-1 text-xs disabled:opacity-40" type="button" :disabled="readonly || selectedSelection.length === 0" @click="removeSelected">Remove</button>
      <button class="rounded border border-stone-200 px-2 py-1 text-xs disabled:opacity-40" type="button" :disabled="readonly || selectedSelection.length === 0" @click="moveSelected(-1)">Up</button>
      <button class="rounded border border-stone-200 px-2 py-1 text-xs disabled:opacity-40" type="button" :disabled="readonly || selectedSelection.length === 0" @click="moveSelected(1)">Down</button>
    </div>

    <section class="min-w-0 rounded border border-stone-200 bg-white">
      <p class="border-b border-stone-200 px-2.5 py-2 text-[11px] font-medium text-stone-500">Selected order: later templates override earlier templates.</p>
      <select
        v-model="selectedSelection"
        class="h-36 w-full bg-white p-1 text-xs outline-none"
        multiple
        :disabled="readonly"
        aria-label="Selected base templates"
      >
        <option v-for="option in selectedOptions" :key="option.value" :value="option.value">
          {{ option.label }}{{ option.description ? ` (${option.description})` : '' }}
        </option>
      </select>
    </section>
  </div>
</template>
