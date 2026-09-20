<script setup lang="ts">
import { onMounted, onBeforeUnmount, ref } from 'vue'
import type { TreeActionTarget, TreeActionDescriptor, TreeActionRequest } from '@/types/tree-actions'
const props = defineProps<{ target: TreeActionTarget; label: string; actions: TreeActionDescriptor[]; x: number; y: number }>()
const emit = defineEmits<{ close: []; action: [request: TreeActionRequest] }>()
const menu = ref<HTMLElement>()
const left = ref(props.x)
const top = ref(props.y)
const previousFocus = document.activeElement as HTMLElement | null
function buttons() { return Array.from(menu.value?.querySelectorAll<HTMLButtonElement>('button:not(:disabled)') ?? []) }
function onKey(event: KeyboardEvent) {
  if (event.key === 'Escape' || event.key === 'Tab') { event.preventDefault(); emit('close'); return }
  const options = buttons()
  if (!options.length) return
  const index = options.indexOf(document.activeElement as HTMLButtonElement)
  let target = index
  if (event.key === 'ArrowDown') target = (index + 1) % options.length
  else if (event.key === 'ArrowUp') target = (index - 1 + options.length) % options.length
  else if (event.key === 'Home') target = 0
  else if (event.key === 'End') target = options.length - 1
  else return
  event.preventDefault()
  options[target]?.focus()
}
function onOutside(event: PointerEvent) { if (!menu.value?.contains(event.target as Node)) emit('close') }
onMounted(() => {
  const rect = menu.value!.getBoundingClientRect()
  left.value = Math.max(8, Math.min(props.x, window.innerWidth - rect.width - 8))
  top.value = Math.max(8, Math.min(props.y, window.innerHeight - rect.height - 8))
  buttons()[0]?.focus()
  window.addEventListener('keydown', onKey)
  window.addEventListener('pointerdown', onOutside)
})
onBeforeUnmount(() => {
  window.removeEventListener('keydown', onKey)
  window.removeEventListener('pointerdown', onOutside)
  previousFocus?.focus()
})
</script>
<template>
  <Teleport to="body">
    <div ref="menu" role="menu" :aria-label="label + ' actions'" class="tree-action-menu" :style="{ left: left + 'px', top: top + 'px' }">
      <div class="menu-label">{{ label }}</div>
      <button v-for="entry in actions" :key="entry.action" role="menuitem" :disabled="entry.disabled" type="button"
        @click="emit('action', { target, action: entry.action }); emit('close')">{{ entry.label }}</button>
    </div>
  </Teleport>
</template>
<style scoped>
.tree-action-menu { position: fixed; z-index: 999; min-width: 210px; max-height: calc(100vh - 16px); overflow-y: auto; background: white; border: 1px solid #ddd; border-radius: 8px; padding: 6px; box-shadow: 0 8px 30px #0003; }
.menu-label { padding: 5px 10px; color: #777; font-size: 12px; }
button { display: block; width: 100%; text-align: left; border: 0; background: transparent; padding: 7px 10px; border-radius: 4px; font-size: 13px; }
button:focus, button:hover { background: #e8eaf8; outline: 2px solid #5970e3; }
button:disabled { opacity: .45; }
</style>
