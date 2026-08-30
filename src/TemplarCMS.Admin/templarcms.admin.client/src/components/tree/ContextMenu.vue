<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref } from 'vue'
import type { TreeItem } from '@/types'

const props = defineProps<{
  node: TreeItem
  x: number
  y: number
}>()

const emit = defineEmits<{
  (e: 'close'): void
  (e: 'action', action: string): void
}>()

const menuRef = ref<HTMLElement>()
const adjustedX = ref(props.x)
const adjustedY = ref(props.y)

onMounted(() => {
  const el = menuRef.value
  if (!el) return
  const rect = el.getBoundingClientRect()
  if (props.x + rect.width > window.innerWidth)  adjustedX.value = props.x - rect.width
  if (props.y + rect.height > window.innerHeight) adjustedY.value = props.y - rect.height

  const close = () => emit('close')
  window.addEventListener('keydown', (e) => { if (e.key === 'Escape') close() }, { once: true })
})

const isTemplate = computed(() => props.node.type === 'template')
const isRoot     = computed(() => props.node.type === 'root')

type MenuGroup = { items: { label: string; action: string; icon: string; danger?: boolean; disabled?: boolean }[] }

const groups = computed<MenuGroup[]>(() => {
  const g: MenuGroup[] = []

  // Create
  if (!isRoot.value) {
    g.push({ items: [
      isTemplate.value
        ? { label: 'New Template',          action: 'new-template', icon: '＋' }
        : { label: 'New Item from Template…', action: 'new-item',    icon: '＋' },
      { label: 'New Folder',                action: 'new-folder',   icon: '🗂' },
    ]})
  }

  // Reorder
  if (!isRoot.value) {
    g.push({ items: [
      { label: 'Move Up',      action: 'move-up',    icon: '↑' },
      { label: 'Move Down',    action: 'move-down',  icon: '↓' },
      { label: 'Move to Top',  action: 'move-first', icon: '⇑' },
      { label: 'Move to Bottom', action: 'move-last', icon: '⇓' },
    ]})
  }

  // Reparent
  if (!isRoot.value) {
    g.push({ items: [
      { label: 'Move to…',    action: 'reparent',  icon: '↗' },
    ]})
  }

  // Danger
  if (!isRoot.value) {
    g.push({ items: [
      { label: 'Rename',  action: 'rename', icon: '✎' },
      { label: 'Delete',  action: 'delete', icon: '×', danger: true },
    ]})
  }

  return g
})
</script>

<template>
  <Teleport to="body">
    <!-- Backdrop -->
    <div class="fixed inset-0 z-[998]" @click="emit('close')" @contextmenu.prevent="emit('close')" />

    <!-- Menu -->
    <div
      ref="menuRef"
      class="fixed z-[999] w-52 bg-white rounded-xl shadow-xl ring-1 ring-black/10 py-1.5 overflow-hidden"
      :style="{ left: `${adjustedX}px`, top: `${adjustedY}px` }"
    >
      <!-- Node label header -->
      <div class="px-3 py-1.5 mb-0.5">
        <p class="text-[11px] font-semibold text-stone-400 uppercase tracking-wider truncate">{{ node.label }}</p>
      </div>

      <template v-for="(group, gi) in groups" :key="gi">
        <div v-if="gi > 0" class="my-1 mx-2 border-t border-stone-100" />
        <button
          v-for="item in group.items"
          :key="item.action"
          @click="emit('action', item.action); emit('close')"
          :disabled="item.disabled"
          class="w-full flex items-center gap-2.5 px-3 py-1.5 text-left text-[13px] transition-colors"
          :class="item.danger
            ? 'text-rose-600 hover:bg-rose-50'
            : item.disabled
              ? 'text-stone-300 cursor-not-allowed'
              : 'text-stone-700 hover:bg-[#f0f0fd] hover:text-[#3a4eb0]'"
        >
          <span class="w-4 text-center text-[13px] shrink-0 opacity-60">{{ item.icon }}</span>
          {{ item.label }}
        </button>
      </template>
    </div>
  </Teleport>
</template>
