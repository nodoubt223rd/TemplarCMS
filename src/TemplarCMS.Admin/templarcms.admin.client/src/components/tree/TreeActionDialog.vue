<script setup lang="ts">
import { ref, onMounted, onBeforeUnmount } from 'vue'
const props = defineProps<{
  title: string; initialName?: string; showName?: boolean; busy: boolean; error?: string | null
  destinations?: { id: string; label: string }[]; templates?: { id: string; name: string }[]
  initialParent?: string; initialTemplate?: string; destructive?: boolean
}>()
const emit = defineEmits<{ cancel: []; submit: [value: { name: string; parentId: string; templateId: string }] }>()
const dialog = ref<HTMLDialogElement>()
const name = ref(props.initialName ?? '')
const parentId = ref(props.initialParent ?? '')
const templateId = ref(props.initialTemplate ?? props.templates?.[0]?.id ?? '')
const previousFocus = document.activeElement as HTMLElement | null
onMounted(() => { dialog.value?.showModal(); dialog.value?.querySelector<HTMLInputElement>('input,select,button')?.focus() })
onBeforeUnmount(() => previousFocus?.focus())
</script>
<template>
  <Teleport to="body">
    <dialog ref="dialog" class="tree-action-dialog" :aria-label="title" @cancel.prevent="!busy && emit('cancel')">
      <form @submit.prevent="emit('submit', { name, parentId, templateId })">
        <h2>{{ title }}</h2>
        <p v-if="destructive">This action cannot be undone. Items with dependencies cannot be deleted.</p>
        <label v-if="showName">Name<input v-model="name" required :disabled="busy" /></label>
        <label v-if="templates">Template<select v-model="templateId" required :disabled="busy"><option v-for="item in templates" :key="item.id" :value="item.id">{{ item.name }}</option></select></label>
        <label v-if="destinations">Destination<select v-model="parentId" :disabled="busy"><option v-for="item in destinations" :key="item.id" :value="item.id">{{ item.label }}</option></select></label>
        <p v-if="error" role="alert">{{ error }}</p>
        <footer><button type="button" :disabled="busy" @click="emit('cancel')">Cancel</button><button type="submit" :disabled="busy">{{ busy ? 'Saving…' : destructive ? 'Delete' : 'Save' }}</button></footer>
      </form>
    </dialog>
  </Teleport>
</template>
<style scoped>
.tree-action-dialog { border: 1px solid #ccc; border-radius: 10px; padding: 24px; width: min(460px, calc(100vw - 32px)); }
.tree-action-dialog::backdrop { background: #0005; }
h2 { font-size: 18px; margin-bottom: 16px; } label { display: block; margin: 12px 0; }
input, select { display: block; width: 100%; border: 1px solid #bbb; padding: 8px; border-radius: 4px; }
footer { display: flex; justify-content: flex-end; gap: 12px; margin-top: 20px; } button { padding: 8px 14px; background: #e8eaf8; border-radius: 5px; }
[role=alert] { color: #b42318; margin-top: 10px; }
</style>
