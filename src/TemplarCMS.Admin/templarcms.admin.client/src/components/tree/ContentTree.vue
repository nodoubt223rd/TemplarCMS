<script setup lang="ts">
import { ref } from 'vue'
import type { TreeNode } from '@/types/admin-ui'
import TreeNodeItem from './Treenode.vue'

withDefaults(defineProps<{
  rootNode: TreeNode
  selectedId: string | null
  isLoading: boolean
  templateIcons?: Record<string, string>
}>(), {
  templateIcons: () => ({})
})

const emit = defineEmits<{
  select: [node: TreeNode]
  toggle: [node: TreeNode]
}>()

const treeFilter = ref('')
</script>

<template>
  <aside class="figma-content-tree w-60 flex flex-col bg-[#f0ede7] border-r border-[#dbd6ce]">
    <!-- Tree header -->
    <div class="flex items-center justify-between px-3 pt-3 pb-1.5">
      <span class="text-[11px] font-semibold uppercase tracking-widest text-stone-400">
        Content
      </span>
      <span class="text-[10px] text-stone-400">Server tree</span>
    </div>

    <!-- Filter input -->
    <div class="px-2 pb-2">
      <div class="flex items-center gap-1.5 px-2 py-1 rounded-md bg-[#e6e2dc] ring-1 ring-transparent focus-within:ring-[#5970e3]/40">
        <svg width="12" height="12" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.8"
             stroke-linecap="round" stroke-linejoin="round" class="text-stone-400 shrink-0">
          <circle cx="6.5" cy="6.5" r="4.5"/><line x1="10" y1="10" x2="14" y2="14"/>
        </svg>
        <input
          v-model="treeFilter"
          placeholder="Filter items…"
          class="flex-1 text-xs bg-transparent outline-none text-stone-700 placeholder:text-stone-400"
        />
      </div>
    </div>

    <!-- Tree nodes -->
    <div class="flex-1 overflow-y-auto px-1 pb-3">
      <div v-if="isLoading" class="px-3 py-4 text-xs text-stone-400">Loading content tree...</div>
      <TreeNodeItem
        v-else
        :node="rootNode"
        :depth="0"
        :selected-id="selectedId"
        :filter-text="treeFilter"
        :template-icons="templateIcons"
        @select="emit('select', $event)"
        @toggle="emit('toggle', $event)"
      />
    </div>
  </aside>
</template>

<style scoped>
.figma-content-tree,
.figma-content-tree button,
.figma-content-tree input {
  font-family: 'DM Sans', sans-serif;
}

.figma-content-tree {
  font-size: 13px;
  font-weight: 400;
}
</style>
