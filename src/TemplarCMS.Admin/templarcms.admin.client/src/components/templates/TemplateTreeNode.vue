<script setup lang="ts">
import type { TemplateTreeEntry } from '@/utils/template-tree'
import type { TreeMenuRequest } from '@/types/tree-actions'
import ItemIcon from '@/components/ui/ItemIcon.vue'
const props = defineProps<{ node: TemplateTreeEntry; selectedId: string | null; expanded: Record<string, boolean> }>()
const emit = defineEmits<{ select: [id: string]; toggle: [id: string]; menu: [request: TreeMenuRequest] }>()
function menu(event: MouseEvent | KeyboardEvent) {
  if (event instanceof KeyboardEvent && event.key !== 'ContextMenu' && !(event.shiftKey && event.key === 'F10')) return
  event.preventDefault(); event.stopPropagation()
  const rect = (event.currentTarget as HTMLElement).getBoundingClientRect()
  emit('menu', { target: { kind: props.node.kind, id: props.node.id }, label: props.node.name,
    x: event instanceof MouseEvent && event.type === 'contextmenu' ? event.clientX : rect.left,
    y: event instanceof MouseEvent && event.type === 'contextmenu' ? event.clientY : rect.bottom })
}
</script>
<template>
  <li role="none">
    <div class="flex items-center">
      <button type="button" role="treeitem" :aria-selected="node.kind === 'template' ? selectedId === node.id : undefined"
        :aria-expanded="node.kind === 'template-folder' ? expanded[node.id] !== false : undefined"
        :aria-owns="node.kind === 'template-folder' && expanded[node.id] !== false ? 'template-group-' + node.id : undefined"
        class="flex min-w-0 flex-1 items-center gap-2 rounded px-2 py-1.5 text-left text-sm"
        :class="selectedId === node.id ? 'bg-[#e8eaf8] text-[#3a4eb0]' : 'text-stone-600 hover:bg-stone-100'"
        @click="node.kind === 'template-folder' ? emit('toggle', node.id) : emit('select', node.id)"
        @contextmenu="menu" @keydown="menu"
        @keydown.right.prevent="node.kind === 'template-folder' && expanded[node.id] === false && emit('toggle', node.id)"
        @keydown.left.prevent="node.kind === 'template-folder' && expanded[node.id] !== false && emit('toggle', node.id)">
        <span v-if="node.kind === 'template-folder'" aria-hidden="true">{{ expanded[node.id] === false ? '▸' : '▾' }}</span>
        <ItemIcon :icon="node.icon" /><span class="truncate">{{ node.name }}</span>
      </button>
      <button type="button" :aria-label="node.name + ' actions'" class="px-2" @click="menu">⋯</button>
    </div>
    <ul v-if="node.kind === 'template-folder' && expanded[node.id] !== false" :id="'template-group-' + node.id" role="group" class="pl-3">
      <TemplateTreeNode v-for="child in node.children" :key="child.kind + child.id" :node="child" :selected-id="selectedId" :expanded="expanded"
        @select="emit('select', $event)" @toggle="emit('toggle', $event)" @menu="emit('menu', $event)" />
    </ul>
  </li>
</template>
