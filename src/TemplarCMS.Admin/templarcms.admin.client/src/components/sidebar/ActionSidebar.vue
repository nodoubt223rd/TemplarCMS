<script setup lang="ts">
import { computed } from 'vue'
import { findItem } from '@/composables/useTree'
import type { ItemStatus } from '@/types'
import StatusPill from '@/components/ui/StatusPill.vue'

const props = defineProps<{
  selectedId: string
  show: boolean
}>()

const emit = defineEmits<{ (e: 'close'): void }>()

const item = computed(() => findItem(props.selectedId))

type Action = { label: string; icon: string; variant: 'default' | 'danger' | 'accent'; description: string }
const actions: Action[] = [
  { label: 'Publish',          icon: '↑', variant: 'accent',  description: 'Publish this item to all targets' },
  { label: 'Unpublish',        icon: '↓', variant: 'default', description: 'Remove from published targets' },
  { label: 'Create Version',   icon: '+', variant: 'default', description: 'Snapshot the current state' },
  { label: 'Duplicate',        icon: '⧉', variant: 'default', description: 'Clone this item and its children' },
  { label: 'Rename',           icon: '✎', variant: 'default', description: 'Rename this item' },
  { label: 'Move',             icon: '↗', variant: 'default', description: 'Move to a different location' },
  { label: 'Lock',             icon: '🔒', variant: 'default', description: 'Prevent editing by other users' },
  { label: 'Set Workflow',     icon: '◎', variant: 'default', description: 'Assign to a workflow state' },
  { label: 'Delete',           icon: '×', variant: 'danger',  description: 'Permanently delete this item' },
]
</script>

<template>
  <aside
    v-if="show"
    class="w-56 flex flex-col bg-white border-l border-stone-200 overflow-y-auto"
  >
    <div class="flex items-center justify-between px-4 py-3 border-b border-stone-100">
      <span class="text-xs font-semibold text-stone-600 uppercase tracking-wider">Actions</span>
      <button @click="emit('close')" class="text-stone-400 hover:text-stone-600 text-lg leading-none">×</button>
    </div>

    <div v-if="item" class="px-3 py-2.5 bg-stone-50 border-b border-stone-100">
      <p class="text-[12px] font-semibold text-stone-700 truncate">{{ item.label }}</p>
      <div class="flex items-center gap-2 mt-1">
        <StatusPill v-if="item.status" :status="item.status as ItemStatus" />
        <span class="text-[10px] text-stone-400 truncate">{{ item.template ?? item.type }}</span>
      </div>
    </div>

    <div class="flex-1 px-2 py-2">
      <button
        v-for="action in actions"
        :key="action.label"
        class="w-full flex items-start gap-2.5 px-3 py-2.5 rounded-lg mb-0.5 text-left transition-colors group"
        :class="{
          'hover:bg-rose-50': action.variant === 'danger',
          'hover:bg-[#f0f0fd]': action.variant === 'accent',
          'hover:bg-stone-50': action.variant === 'default',
        }"
      >
        <span class="w-5 h-5 flex items-center justify-center text-base shrink-0 mt-0.5"
              :class="{
                'text-rose-500': action.variant === 'danger',
                'text-[#5970e3]': action.variant === 'accent',
                'text-stone-400 group-hover:text-stone-600': action.variant === 'default',
              }">
          {{ action.icon }}
        </span>
        <div>
          <p class="text-[13px] font-medium leading-tight"
             :class="{
               'text-rose-600': action.variant === 'danger',
               'text-[#5970e3]': action.variant === 'accent',
               'text-stone-700': action.variant === 'default',
             }">
            {{ action.label }}
          </p>
          <p class="text-[10px] text-stone-400 mt-0.5 leading-tight">{{ action.description }}</p>
        </div>
      </button>
    </div>

    <div class="px-4 pb-4">
      <div class="rounded-lg bg-stone-50 p-3 ring-1 ring-stone-100">
        <p class="text-[10px] font-semibold text-stone-400 uppercase tracking-wider mb-1.5">API Path</p>
        <p class="text-[10px] font-mono text-stone-600 break-all leading-relaxed">/api/items/{{ selectedId }}</p>
      </div>
    </div>
  </aside>
</template>
