<script setup lang="ts">
import { computed } from 'vue'
import type { ItemStatus } from '@/types'

const props = defineProps<{ status: ItemStatus }>()

const config = computed(() => ({
  published: { label: 'Published', cls: 'bg-emerald-50 text-emerald-700 ring-emerald-200' },
  draft:     { label: 'Draft',     cls: 'bg-amber-50 text-amber-700 ring-amber-200' },
  workflow:  { label: 'Workflow',  cls: 'bg-blue-50 text-blue-700 ring-blue-200' },
  locked:    { label: 'Locked',    cls: 'bg-rose-50 text-rose-700 ring-rose-200' },
}[props.status]))
</script>

<template>
  <span
    class="inline-flex items-center px-1.5 py-0.5 rounded text-[10px] font-medium tracking-wide ring-1 ring-inset"
    :class="config.cls"
  >{{ config.label }}</span>
</template>
