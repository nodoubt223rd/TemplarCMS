<script setup lang="ts">
import { computed } from 'vue'
import { buildBreadcrumb } from '@/composables/useTree'

const props = defineProps<{ selectedId: string }>()

const crumbs = computed(() => buildBreadcrumb(props.selectedId))
</script>

<template>
  <div class="flex items-center gap-1 px-4 h-7 bg-[#1e1c18] border-t border-white/6 shrink-0">
    <template v-for="(crumb, i) in crumbs" :key="crumb.id">
      <span
        class="text-[10px] leading-none"
        :class="i === crumbs.length - 1 ? 'text-[#c8c3bc]' : 'text-[#5a5650]'"
      >{{ crumb.label }}</span>
      <span v-if="i < crumbs.length - 1" class="text-[#3a3830] text-[10px]">/</span>
    </template>
    <span v-if="!crumbs.length" class="text-[#5a5650] text-[10px]">No item selected</span>
    <div class="flex-1" />
    <span class="text-[10px] text-[#5a5650]">TemplarCMS v0.1.0</span>
  </div>
</template>
