<script setup lang="ts">
import { ref, computed } from 'vue'
import { useTreeState } from '@/composables/useTreeState'
import { ICON_SVG } from '@/data/icons'
import type { TreeItem } from '@/types'

const props = defineProps<{ itemId: string }>()
const emit = defineEmits<{ (e: 'close'): void; (e: 'reparent', newParentId: string): void }>()

const { tree } = useTreeState()
const query = ref('')

type FlatNode = { item: TreeItem; depth: number }

function flatten(nodes: TreeItem[], depth = 0, acc: FlatNode[] = []): FlatNode[] {
  for (const n of nodes) {
    // Can't reparent to self or a descendant
    if (n.id !== props.itemId) {
      acc.push({ item: n, depth })
      if (n.children) flatten(n.children, depth + 1, acc)
    }
  }
  return acc
}

const candidates = computed(() => {
  const all = flatten(tree.value)
  const q = query.value.toLowerCase()
  return q ? all.filter(n => n.item.label.toLowerCase().includes(q)) : all
})

const selected = ref('')
</script>

<template>
  <Teleport to="body">
    <div class="fixed inset-0 z-[1000] flex items-center justify-center bg-black/40 backdrop-blur-sm" @click.self="emit('close')">
      <div class="w-80 bg-white rounded-2xl shadow-2xl ring-1 ring-black/10 overflow-hidden flex flex-col max-h-[70vh]">

        <!-- Header -->
        <div class="flex items-center justify-between px-4 py-3 border-b border-stone-100">
          <h3 class="text-sm font-semibold text-stone-800">Move to…</h3>
          <button @click="emit('close')" class="text-stone-400 hover:text-stone-600 text-xl leading-none">×</button>
        </div>

        <!-- Search -->
        <div class="px-3 py-2 border-b border-stone-100">
          <input
            v-model="query"
            placeholder="Search nodes…"
            autofocus
            class="w-full px-2.5 py-1.5 text-xs rounded-lg bg-stone-50 ring-1 ring-stone-200 outline-none focus:ring-[#5970e3]"
          />
        </div>

        <!-- Tree list -->
        <div class="flex-1 overflow-y-auto py-1">
          <button
            v-for="{ item, depth } in candidates"
            :key="item.id"
            @click="selected = item.id"
            class="w-full flex items-center gap-2 py-1.5 pr-3 text-left text-[13px] transition-colors"
            :style="{ paddingLeft: `${12 + depth * 14}px` }"
            :class="selected === item.id
              ? 'bg-[#e8eaf8] text-[#3a4eb0]'
              : 'text-stone-700 hover:bg-stone-50'"
          >
            <svg width="13" height="13" viewBox="0 0 16 16" xmlns="http://www.w3.org/2000/svg"
                 class="shrink-0" :class="selected === item.id ? 'text-[#5970e3]' : 'text-stone-400'"
                 v-html="ICON_SVG[item.iconKey]" />
            <span class="truncate">{{ item.label }}</span>
          </button>
          <p v-if="!candidates.length" class="text-center text-xs text-stone-400 py-6">No matching nodes</p>
        </div>

        <!-- Actions -->
        <div class="flex items-center justify-end gap-2 px-4 py-3 border-t border-stone-100">
          <button @click="emit('close')"
            class="px-3 py-1.5 rounded-lg text-sm text-stone-600 hover:bg-stone-100 transition-colors">
            Cancel
          </button>
          <button
            :disabled="!selected"
            @click="emit('reparent', selected)"
            class="px-4 py-1.5 rounded-lg text-sm font-medium text-white bg-[#5970e3] hover:bg-[#4a5ed4] transition-colors disabled:opacity-40 disabled:cursor-not-allowed">
            Move Here
          </button>
        </div>
      </div>
    </div>
  </Teleport>
</template>
