<script setup lang="ts">
import { computed } from 'vue'
import type { TreeNode } from '@/types/admin-ui'
import { treeNodeMatchesFilter } from '@/utils/content-tree'

const props = defineProps<{
  node: TreeNode
  depth: number
  selectedId: string | null
  filterText: string
  templateIcons: Record<string, string>
}>()

const emit = defineEmits<{
  toggle: [node: TreeNode]
  select: [node: TreeNode]
}>()

const hasChildren = computed(() => props.node.isWorkspaceRoot === true || !props.node.isBranchLoaded || props.node.children.length > 0)
const isSelected = computed(() => props.selectedId === props.node.item.id)
const isWorkspaceRoot = computed(() => props.node.isWorkspaceRoot === true)
const visibleChildren = computed(() =>
  props.node.children.filter(child => treeNodeMatchesFilter(child, props.filterText)))
const iconPath = computed(() => {
  switch (props.node.item.icon ?? props.templateIcons[props.node.item.templateId] ?? 'file') {
    case 'folder': return 'M2.5 4.5h4l1.3 1.5h5.7v6.5a1 1 0 0 1-1 1h-10a1 1 0 0 1-1-1v-7a1 1 0 0 1 1-1Z'
    case 'layout': return 'M2.5 2.5h11v11h-11zM2.5 6h11M6 6v7'
    case 'image': return 'M2.5 3.5h11v9h-11zM4.5 10l2-2 1.5 1.5 1.5-2 2.5 2.5'
    default: return 'M3 2.5h7l3 3v8H3zM10 2.5v3h3'
  }
})

function selectNode() {
  if (!isWorkspaceRoot.value) {
    emit('select', props.node)
  }
}

function toggle(event: MouseEvent) {
  event.stopPropagation()

  if (!isWorkspaceRoot.value) {
    emit('toggle', props.node)
  }
}
</script>

<template>
  <div>
    <button
      class="w-full flex items-center gap-1.5 px-2 py-1 rounded-md text-left text-sm transition-colors group"
      :class="isSelected
        ? 'bg-[#e8eaf8] text-[#3a4eb0] font-medium'
        : 'text-[#3d3a34] hover:bg-[#e6e2dc]'"
      :style="{ paddingLeft: `${8 + depth * 14}px` }"
      type="button"
      :disabled="isWorkspaceRoot"
      @click="selectNode"
    >
      <span
        class="w-3.5 h-3.5 flex items-center justify-center shrink-0 text-stone-400 transition-transform"
        :class="hasChildren ? 'opacity-100' : 'opacity-0'"
        @click="toggle"
      >
        <svg width="8" height="8" viewBox="0 0 8 8" fill="currentColor">
          <path :d="node.isExpanded ? 'M1 2.5l3 3 3-3' : 'M2.5 7L5.5 4 2.5 1'"
                stroke="currentColor" stroke-width="1.5" fill="none"
                stroke-linecap="round" stroke-linejoin="round" />
        </svg>
      </span>

      <svg width="14" height="14" viewBox="0 0 16 16" xmlns="http://www.w3.org/2000/svg"
           class="shrink-0 transition-colors"
           :class="isSelected ? 'text-[#5970e3]' : 'text-stone-400 group-hover:text-stone-600'">
        <path :d="iconPath" fill="none" stroke="currentColor" stroke-width="1.4" stroke-linecap="round" stroke-linejoin="round" />
      </svg>

      <span class="truncate flex-1 text-[13px]">{{ node.item.name }}</span>
    </button>

    <template v-if="node.isExpanded && visibleChildren.length > 0">
      <Treenode
        v-for="child in visibleChildren"
        :key="child.item.id"
        :node="child"
        :depth="depth + 1"
        :selected-id="selectedId"
        :filter-text="filterText"
        :template-icons="templateIcons"
        @toggle="emit('toggle', $event)"
        @select="emit('select', $event)"
      />
    </template>
  </div>
</template>
