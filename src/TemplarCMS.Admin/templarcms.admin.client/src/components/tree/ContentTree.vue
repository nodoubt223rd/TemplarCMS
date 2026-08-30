<script setup lang="ts">
import { ref, computed } from 'vue'
import { TREE_ROOT_IDS } from '@/data/tree'
import { filterTree } from '@/composables/useTree'
import { useTreeState } from '@/composables/useTreeState'

const { tree } = useTreeState()
import type { NavSection, IconKey } from '@/types'
import TreeNode from './TreeNode.vue'
import ScopeLegendPopover from '@/components/ui/ScopeLegendPopover.vue'

const props = defineProps<{
  navSection: NavSection
  selectedId: string
  iconOverrides: Record<string, IconKey>
}>()

const emit = defineEmits<{ (e: 'select', id: string): void }>()

const treeFilter = ref('')

const rootId = computed(() => TREE_ROOT_IDS[props.navSection])
const rootNode = computed(() => tree.value.find(n => n.id === rootId.value) ?? tree.value[0])

const visibleNodes = computed(() => {
  const nodes = rootNode.value.children ?? []
  return filterTree(nodes, treeFilter.value)
})
</script>

<template>
  <aside class="w-60 flex flex-col bg-[#f0ede7] border-r border-[#dbd6ce]">
    <!-- Tree header -->
    <div class="flex items-center justify-between px-3 pt-3 pb-1.5">
      <span class="text-[11px] font-semibold uppercase tracking-widest text-stone-400">
        {{ rootNode.label }}
      </span>
      <ScopeLegendPopover v-if="navSection === 'content'" />
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
      <TreeNode
        :node="rootNode"
        :depth="0"
        :selectedId="selectedId"
        :iconOverrides="iconOverrides"
        @select="emit('select', $event)"
      />
    </div>
  </aside>
</template>
