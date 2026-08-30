<script setup lang="ts">
import { ICON_SVG } from '@/data/icons'
import type { NavSection } from '@/types'

defineProps<{ active: NavSection }>()
const emit = defineEmits<{ (e: 'change', s: NavSection): void }>()

const items: { key: NavSection; label: string; icon: string }[] = [
  { key: 'content',   label: 'Content',   icon: ICON_SVG['layers']   },
  { key: 'templates', label: 'Templates', icon: ICON_SVG['layout']   },
  { key: 'media',     label: 'Media',     icon: ICON_SVG['image']    },
  { key: 'system',    label: 'System',    icon: ICON_SVG['settings'] },
]
</script>

<template>
  <nav class="w-14 flex flex-col items-center py-2 gap-1 bg-[#1e1c18] border-r border-white/6 shrink-0">
    <button
      v-for="item in items"
      :key="item.key"
      @click="emit('change', item.key)"
      :title="item.label"
      class="w-10 h-10 flex flex-col items-center justify-center gap-0.5 rounded-lg transition-colors"
      :class="active === item.key
        ? 'bg-[#5970e3] text-white'
        : 'text-[#7a7268] hover:bg-white/8 hover:text-[#c8c3bc]'"
    >
      <svg width="16" height="16" viewBox="0 0 16 16" xmlns="http://www.w3.org/2000/svg"
           v-html="item.icon" />
      <span class="text-[8px] leading-none tracking-wide font-medium">{{ item.label }}</span>
    </button>
  </nav>
</template>
