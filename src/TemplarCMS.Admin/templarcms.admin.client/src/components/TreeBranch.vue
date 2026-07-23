<script setup lang="ts">
import { computed } from 'vue'
import type { TreeNode } from '@/types/admin-ui'

const props = defineProps<{
  node: TreeNode
  selectedItemId: string | null
}>()

const emit = defineEmits<{
  toggle: [node: TreeNode]
  select: [node: TreeNode]
}>()

const isSelected = computed(() => props.selectedItemId === props.node.item.id)

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

    <ul v-if="node.isExpanded && node.children.length > 0" class="tree-list tree-list--nested">
      <TreeBranch
        v-for="child in node.children"
        :key="child.item.id"
        :node="child"
        :selected-item-id="selectedItemId"
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
