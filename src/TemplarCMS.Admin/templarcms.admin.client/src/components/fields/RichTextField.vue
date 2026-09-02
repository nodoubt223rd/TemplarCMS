<script setup lang="ts">
import { EditorContent, useEditor } from '@tiptap/vue-3'
import StarterKit from '@tiptap/starter-kit'
import { computed, watch } from 'vue'

const props = defineProps<{ modelValue: string; readonly?: boolean; placeholder?: string }>()
const emit = defineEmits<{ 'update:modelValue': [value: string] }>()
const editor = useEditor({
  content: props.modelValue || '', editable: !props.readonly,
  extensions: [StarterKit.configure({ heading: { levels: [2, 3, 4] }, code: false, codeBlock: false, horizontalRule: false })],
  onUpdate: ({ editor }) => emit('update:modelValue', editor.getHTML())
})
const isEmpty = computed(() => editor.value?.isEmpty ?? true)
watch(() => props.readonly, value => editor.value?.setEditable(!value))
watch(() => props.modelValue, value => {
  if (editor.value != null && value !== editor.value.getHTML()) editor.value.commands.setContent(value || '', { emitUpdate: false })
})
const groups = [
  [{ label: 'B', title: 'Bold', run: () => editor.value?.chain().focus().toggleBold().run(), active: () => editor.value?.isActive('bold') }],
  [{ label: 'I', title: 'Italic', run: () => editor.value?.chain().focus().toggleItalic().run(), active: () => editor.value?.isActive('italic') }],
  [2, 3, 4].map(level => ({ label: `H${level}`, title: `Heading ${level}`, run: () => editor.value?.chain().focus().toggleHeading({ level: level as 2 | 3 | 4 }).run(), active: () => editor.value?.isActive('heading', { level }) })),
  [{ label: 'List', title: 'Bullet list', run: () => editor.value?.chain().focus().toggleBulletList().run(), active: () => editor.value?.isActive('bulletList') }, { label: '1.', title: 'Numbered list', run: () => editor.value?.chain().focus().toggleOrderedList().run(), active: () => editor.value?.isActive('orderedList') }],
  [{ label: 'Quote', title: 'Block quote', run: () => editor.value?.chain().focus().toggleBlockquote().run(), active: () => editor.value?.isActive('blockquote') }]
]
</script>

<template>
  <div class="overflow-hidden rounded-lg ring-1 transition-shadow" :class="readonly ? 'bg-stone-50/60 ring-stone-100' : 'bg-white ring-stone-200 focus-within:ring-[#5970e3]/60 focus-within:shadow-sm'">
    <div v-if="!readonly" class="flex flex-wrap items-center gap-px border-b border-stone-100 bg-stone-50 px-2 py-1.5">
      <template v-for="(group, index) in groups" :key="index"><span v-if="index" class="mx-1 h-4 w-px bg-stone-200" /><button v-for="button in group" :key="button.title" type="button" :title="button.title" :disabled="!editor" class="rounded px-1.5 py-0.5 text-[11px] font-bold transition-colors" :class="button.active() ? 'bg-[#5970e3] text-white shadow-sm' : 'text-stone-500 hover:bg-stone-200'" @click="button.run()">{{ button.label }}</button></template>
    </div>
    <div class="relative"><p v-if="isEmpty && !readonly" class="pointer-events-none absolute left-3 top-2.5 text-sm text-stone-300">{{ placeholder ?? 'Start writing...' }}</p><EditorContent :editor="editor" class="tiptap-content" :class="readonly ? 'opacity-70' : ''" /><span v-if="readonly" class="absolute right-2 top-2 rounded bg-stone-100 px-1.5 py-0.5 text-[9px] font-semibold uppercase tracking-wider text-stone-400">Read-only</span></div>
  </div>
</template>

<style>
.tiptap-content .ProseMirror { min-height: 80px; outline: none; padding: 10px 12px; color: #44403c; font-size: 13px; line-height: 1.6; }
.tiptap-content .ProseMirror h2 { margin: .75em 0 .25em; color: #1c1917; font-size: 1.1em; font-weight: 700; }
.tiptap-content .ProseMirror h3 { margin: .65em 0 .2em; color: #1c1917; font-size: 1em; font-weight: 700; }
.tiptap-content .ProseMirror h4 { margin: .55em 0 .15em; color: #292524; font-size: .9em; font-weight: 700; letter-spacing: .04em; text-transform: uppercase; }
.tiptap-content .ProseMirror p { margin: 0 0 .5em; }.tiptap-content .ProseMirror ul { margin: .4em 0; padding-left: 1.4em; list-style: disc; }.tiptap-content .ProseMirror ol { margin: .4em 0; padding-left: 1.4em; list-style: decimal; }
.tiptap-content .ProseMirror blockquote { margin: .5em 0; border-left: 3px solid #5970e3; border-radius: 0 6px 6px 0; background: #f5f4ff; padding: .3em .8em; color: #44403c; font-style: italic; }
</style>
