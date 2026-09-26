<script setup lang="ts">
import { computed, reactive } from 'vue'
import type { TemplateSummaryResponse } from '@/types/admin-api'
import type { TemplateOrganization } from '@/types/template-organization'
import type { TreeMenuRequest } from '@/types/tree-actions'
import { buildTemplateTree } from '@/utils/template-tree'
import TemplateTreeNode from './TemplateTreeNode.vue'
const props = defineProps<{ templates: TemplateSummaryResponse[]; organization?: TemplateOrganization | null; selectedId: string | null; loading: boolean }>()
const emit = defineEmits<{ select: [id: string]; menu: [request: TreeMenuRequest] }>()
const expanded = reactive<Record<string, boolean>>({})
const tree = computed(() => buildTemplateTree(props.templates, props.organization ?? null))
function rootMenu(event: MouseEvent | KeyboardEvent) {
  if (event instanceof KeyboardEvent && event.key !== 'ContextMenu' && !(event.shiftKey && event.key === 'F10')) return
  event.preventDefault()
  const rect = (event.currentTarget as HTMLElement).getBoundingClientRect()
  emit('menu', { target: { kind: 'templates-root', id: null }, label: 'Templates', x: rect.left, y: rect.bottom })
}
function navigate(event: KeyboardEvent) {
  if (!['ArrowUp', 'ArrowDown', 'Home', 'End'].includes(event.key)) return
  const entries = Array.from((event.currentTarget as HTMLElement).querySelectorAll<HTMLElement>('[role=treeitem]'))
  const index = entries.indexOf(event.target as HTMLElement)
  if (index < 0) return
  event.preventDefault()
  const target = event.key === 'Home' ? 0 : event.key === 'End' ? entries.length - 1 : Math.max(0, Math.min(entries.length - 1, index + (event.key === 'ArrowDown' ? 1 : -1)))
  entries[target]?.focus()
}
</script>
<template>
  <aside class="flex w-60 shrink-0 flex-col overflow-y-auto border-r border-stone-200 bg-[#f7f5f1]">
    <button type="button" aria-label="Templates actions" class="px-3 py-3 text-left text-sm font-semibold" @click="rootMenu" @contextmenu="rootMenu" @keydown="rootMenu">Templates ⋯</button>
    <p v-if="loading" class="px-3 text-xs">Loading templates…</p>
    <ul role="tree" aria-label="Templates" class="px-1" @keydown="navigate">
      <TemplateTreeNode v-for="node in tree" :key="node.kind + node.id" :node="node" :selected-id="selectedId" :expanded="expanded"
        @select="emit('select', $event)" @menu="emit('menu', $event)" @toggle="expanded[$event] = expanded[$event] === false" />
    </ul>
  </aside>
</template>
