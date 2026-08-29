<script setup lang="ts">
import { computed } from 'vue'
import type { TreeNode } from '@/types/admin-ui'
import { treeNodeMatchesFilter } from '@/utils/content-tree'

const props = defineProps<{
  node: TreeNode
  selectedItemId: string | null
  filterText: string
}>()

const emit = defineEmits<{
  toggle: [node: TreeNode]
  select: [node: TreeNode]
}>()

const isSelected = computed(() => props.selectedItemId === props.node.item.id)
const isFilterActive = computed(() => props.filterText.trim().length > 0)
const visibleChildren = computed(() =>
  props.node.children.filter(child => treeNodeMatchesFilter(child, props.filterText)))

function onToggle() {
  emit('toggle', props.node)
}

function onSelect() {
  emit('select', props.node)
}
</script>

<template>
  <li class="tree-item">
    <div :class="['tree-row', { 'tree-row--selected': isSelected }]">
      <button class="tree-toggle" type="button" @click="onToggle">
        {{ node.isExpanded ? '−' : '+' }}
      </button>
      <button class="tree-entry" type="button" @click="onSelect">
        <span class="tree-entry__title">{{ node.item.name }}</span>
        <span class="tree-entry__path">{{ node.item.path }}</span>
      </button>
    </div>

    <div v-if="node.isExpanded && node.isBranchLoading" class="tree-status">
      Loading branch...
    </div>

    <ul v-if="(node.isExpanded || isFilterActive) && visibleChildren.length > 0" class="tree-list tree-list--nested">
      <TreeBranch
        v-for="child in visibleChildren"
        :key="child.item.id"
        :node="child"
        :selected-item-id="selectedItemId"
        :filter-text="filterText"
        @toggle="$emit('toggle', $event)"
        @select="$emit('select', $event)"
      />
    </ul>

    <div
      v-else-if="node.isExpanded && node.isBranchLoaded && node.children.length === 0"
      class="tree-status"
    >
      No direct children.
    </div>
  </li>
</template>
