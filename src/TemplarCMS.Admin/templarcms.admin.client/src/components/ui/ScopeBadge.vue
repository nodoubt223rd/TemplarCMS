<script setup lang="ts">
import { computed } from 'vue'
import type { FieldScope } from '@/types'

const props = defineProps<{ scope: FieldScope }>()

const config = computed(() => ({
  Versioned:   { cls: 'bg-[#5970e3]/10 text-[#5970e3]' },
  Unversioned: { cls: 'bg-amber-100 text-amber-700' },
  Shared:      { cls: 'bg-emerald-100 text-emerald-700' },
  System:      { cls: 'bg-stone-100 text-stone-500' },
}[props.scope]))
</script>

<template>
  <span
    class="inline-block px-1.5 py-px rounded text-[9px] font-semibold tracking-wider uppercase"
    :class="config.cls"
  >{{ scope }}</span>
</template>
