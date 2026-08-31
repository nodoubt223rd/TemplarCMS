<script setup lang="ts">
type Workspace = 'content' | 'templates' | 'media' | 'system'

defineProps<{ active: Workspace }>()
const emit = defineEmits<{ (e: 'change', s: Workspace): void }>()

const items: { key: Workspace; label: string; icon: string }[] = [
  { key: 'content', label: 'Content', icon: '▤' },
  { key: 'templates', label: 'Templates', icon: '⌘' },
  { key: 'media', label: 'Media', icon: '◫' },
  { key: 'system', label: 'System', icon: '⚙' }
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
      <span class="text-base leading-none" aria-hidden="true">{{ item.icon }}</span>
      <span class="text-[8px] leading-none tracking-wide font-medium">{{ item.label }}</span>
    </button>
  </nav>
</template>
