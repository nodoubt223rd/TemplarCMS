<script setup lang="ts">
import { computed } from 'vue'
import type { ContentItemResponse } from '@/types/admin-api'

const props = defineProps<{ selectedItem: ContentItemResponse | null }>()
const crumbs = computed(() => props.selectedItem?.path.split('/').filter(Boolean) ?? [])
</script>

<template>
  <div class="flex h-7 items-center gap-1 border-t border-white/6 bg-[#1e1c18] px-4">
    <template v-for="(crumb, index) in crumbs" :key="`${crumb}-${index}`">
      <span class="text-[10px]" :class="index === crumbs.length - 1 ? 'text-[#c8c3bc]' : 'text-[#5a5650]'">{{ crumb }}</span>
      <span v-if="index < crumbs.length - 1" class="text-[10px] text-[#3a3830]">/</span>
    </template>
    <span v-if="crumbs.length === 0" class="text-[10px] text-[#5a5650]">No item selected</span>
    <span class="flex-1" />
    <span class="text-[10px] text-[#5a5650]">TemplarCMS v0.1.0</span>
  </div>
</template>
