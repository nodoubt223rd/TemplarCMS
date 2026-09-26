<script setup lang="ts">
import { computed } from 'vue'
import type { TreeNode } from '@/types/admin-ui'
import { treeNodeMatchesFilter } from '@/utils/content-tree'
import { resolveItemIcon } from '@/utils/item-icon'
import ItemIcon from '@/components/ui/ItemIcon.vue'
import type { TreeMenuRequest } from '@/types/tree-actions'

defineOptions({ name: 'ContentTreeNode' })

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
  menu: [request: TreeMenuRequest]
}>()

const hasChildren = computed(() => props.node.isWorkspaceRoot === true || !props.node.isBranchLoaded || props.node.children.length > 0)
const isSelected = computed(() => props.selectedId === props.node.item.id)
const isWorkspaceRoot = computed(() => props.node.isWorkspaceRoot === true)
const visibleChildren = computed(() =>
  props.node.children.filter(child => treeNodeMatchesFilter(child, props.filterText)))
function selectNode() {
  if (!isWorkspaceRoot.value) {
    emit('select', props.node)
  }
}

function openMenu(event: MouseEvent | KeyboardEvent) {
  if (isWorkspaceRoot.value) return
  if (event instanceof KeyboardEvent && event.key !== 'ContextMenu' && !(event.shiftKey && event.key === 'F10')) return
  event.preventDefault()
  event.stopPropagation()
  const rect = (event.currentTarget as HTMLElement).getBoundingClientRect()
  emit('menu', { target: { kind: 'content', id: props.node.item.id }, label: props.node.item.name,
    x: event instanceof MouseEvent && event.type === 'contextmenu' ? event.clientX : rect.left,
    y: event instanceof MouseEvent && event.type === 'contextmenu' ? event.clientY : rect.bottom })
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
    <div class="flex items-center">
    <button
      class="w-full flex items-center gap-1.5 px-2 py-1 rounded-md text-left text-sm transition-colors group"
      :class="isSelected
        ? 'bg-[#e8eaf8] text-[#3a4eb0] font-medium'
        : 'text-[#3d3a34] hover:bg-[#e6e2dc]'"
      :style="{ paddingLeft: `${8 + depth * 14}px` }"
      type="button"
      :disabled="isWorkspaceRoot"
      @click="selectNode"
      @contextmenu="openMenu"
      @keydown="openMenu"
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

      <ItemIcon :icon="resolveItemIcon(node.item.icon, templateIcons[node.item.templateId])" class="shrink-0 text-stone-500" />

      <span class="truncate flex-1 text-[13px]">{{ node.item.name }}</span>
    </button>
    <button v-if="!isWorkspaceRoot" type="button" :aria-label="node.item.name + ' actions'" class="px-2 text-stone-500" @click="openMenu">⋯</button>
    </div>

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
        @menu="emit('menu', $event)"
      />
    </template>
  </div>
</template>
