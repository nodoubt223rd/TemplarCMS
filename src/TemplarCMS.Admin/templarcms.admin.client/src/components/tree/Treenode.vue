<script setup lang="ts">
import { ref, computed } from 'vue'
import { ICON_SVG } from '@/data/icons'
import type { TreeItem, IconKey } from '@/types'
import { useTreeState } from '@/composables/useTreeState'
import { useToast } from '@/composables/useToast'
import StatusPill from '@/components/ui/StatusPill.vue'
import ContextMenu from './ContextMenu.vue'
import ReparentModal from './ReparentModal.vue'

const props = defineProps<{
  node: TreeItem
  depth: number
  selectedId: string
  iconOverrides: Record<string, IconKey>
}>()

const emit = defineEmits<{ (e: 'select', id: string): void }>()

const expanded = ref(props.depth < 2)
const hasChildren = computed(() => !!props.node.children?.length)
const effectiveIcon = computed(() => props.iconOverrides[props.node.id] ?? props.node.iconKey)

const { moveUp, moveDown, moveToFirst, moveToLast, reparentItem, addItem } = useTreeState()
const toast = useToast()

// Context menu
const ctxVisible = ref(false)
const ctxX = ref(0)
const ctxY = ref(0)
const showReparent = ref(false)

function onContextMenu(e: MouseEvent) {
  e.preventDefault()
  e.stopPropagation()
  ctxX.value = e.clientX
  ctxY.value = e.clientY
  ctxVisible.value = true
}

function handleAction(action: string) {
  const label = props.node.label
  switch (action) {
    case 'move-up':
      moveUp(props.node.id)
      toast.info('Moved up', `'${label}' moved up one position.`)
      break
    case 'move-down':
      moveDown(props.node.id)
      toast.info('Moved down', `'${label}' moved down one position.`)
      break
    case 'move-first':
      moveToFirst(props.node.id)
      toast.info('Moved to top', `'${label}' is now first in this level.`)
      break
    case 'move-last':
      moveToLast(props.node.id)
      toast.info('Moved to bottom', `'${label}' is now last in this level.`)
      break
    case 'reparent':
      showReparent.value = true
      break
    case 'new-item':
      addItem(props.node.id, 'New Item', 'file', 'page')
      expanded.value = true
      toast.success('Item created', `New item added under '${label}'.`)
      break
    case 'new-template':
      addItem(props.node.id, 'New Template', 'layout', 'template')
      expanded.value = true
      toast.success('Template created', `New template added under '${label}'.`)
      break
    case 'new-folder':
      addItem(props.node.id, 'New Folder', 'folder', 'folder')
      expanded.value = true
      toast.success('Folder created', `New folder added under '${label}'.`)
      break
    case 'rename':
      toast.warning('Rename', `Rename '${label}' — coming soon.`)
      break
    case 'delete':
      toast.error('Deleted', `'${label}' has been deleted.`)
      break
  }
}

function handleReparent(newParentId: string) {
  reparentItem(props.node.id, newParentId)
  const { findNode } = useTreeState()
  const newParent = findNode(newParentId)
  toast.success('Item moved', `'${props.node.label}' moved to '${newParent?.label ?? newParentId}'.`)
  showReparent.value = false
}

function toggle(e: MouseEvent) {
  e.stopPropagation()
  expanded.value = !expanded.value
}
</script>

<template>
  <div>
    <button
      @click="emit('select', node.id)"
      @contextmenu="onContextMenu"
      class="w-full flex items-center gap-1.5 px-2 py-1 rounded-md text-left text-sm transition-colors group"
      :style="{ paddingLeft: `${8 + depth * 14}px` }"
      :class="selectedId === node.id
        ? 'bg-[#e8eaf8] text-[#3a4eb0] font-medium'
        : 'text-[#3d3a34] hover:bg-[#e6e2dc]'"
    >
      <!-- chevron -->
      <span
        class="w-3.5 h-3.5 flex items-center justify-center shrink-0 text-stone-400 transition-transform"
        :class="hasChildren ? 'opacity-100' : 'opacity-0'"
        @click.stop="toggle"
      >
        <svg width="8" height="8" viewBox="0 0 8 8" fill="currentColor">
          <path :d="expanded ? 'M1 2.5l3 3 3-3' : 'M2.5 7L5.5 4 2.5 1'"
                stroke="currentColor" stroke-width="1.5" fill="none"
                stroke-linecap="round" stroke-linejoin="round"/>
        </svg>
      </span>

      <!-- icon -->
      <svg
        width="14" height="14" viewBox="0 0 16 16"
        xmlns="http://www.w3.org/2000/svg"
        class="shrink-0 transition-colors"
        :class="selectedId === node.id ? 'text-[#5970e3]' : 'text-stone-400 group-hover:text-stone-600'"
        v-html="ICON_SVG[effectiveIcon]"
      />

      <span class="truncate flex-1 text-[13px]">{{ node.label }}</span>

      <StatusPill v-if="node.status" :status="node.status" class="shrink-0 ml-1" />
    </button>

    <!-- Context menu -->
    <ContextMenu
      v-if="ctxVisible"
      :node="node"
      :x="ctxX"
      :y="ctxY"
      @close="ctxVisible = false"
      @action="handleAction"
    />

    <!-- Reparent modal -->
    <ReparentModal
      v-if="showReparent"
      :itemId="node.id"
      @close="showReparent = false"
      @reparent="handleReparent"
    />

    <!-- Children -->
    <template v-if="expanded && hasChildren">
      <TreeNode
        v-for="child in node.children"
        :key="child.id"
        :node="child"
        :depth="depth + 1"
        :selectedId="selectedId"
        :iconOverrides="iconOverrides"
        @select="emit('select', $event)"
      />
    </template>
  </div>
</template>
