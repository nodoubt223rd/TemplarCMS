<script setup lang="ts">
import { ref } from 'vue'

const open = ref(false)

const scopes = [
  { label: 'Versioned',   desc: 'Per language + version',      cls: 'bg-[#5970e3]/10 text-[#5970e3]' },
  { label: 'Unversioned', desc: 'Per language, not versioned',  cls: 'bg-amber-100 text-amber-700' },
  { label: 'Shared',      desc: 'Shared across all languages',  cls: 'bg-emerald-100 text-emerald-700' },
  { label: 'System',      desc: 'Read-only system metadata',    cls: 'bg-stone-100 text-stone-500' },
]
</script>

<template>
  <div class="relative inline-block">
    <button
      @click="open = !open"
      class="w-5 h-5 rounded-full text-[10px] font-bold leading-none flex items-center justify-center
             bg-stone-200 text-stone-500 hover:bg-stone-300 transition-colors"
      :aria-expanded="open"
    >?</button>

    <div
      v-if="open"
      class="absolute bottom-full left-0 mb-2 w-52 bg-white rounded-lg shadow-lg ring-1 ring-black/8 p-3 z-50"
    >
      <p class="text-[10px] font-semibold uppercase tracking-wider text-stone-400 mb-2">Field Scopes</p>
      <div v-for="s in scopes" :key="s.label" class="flex items-start gap-2 mb-1.5 last:mb-0">
        <span class="inline-block px-1.5 py-px rounded text-[9px] font-semibold tracking-wider uppercase mt-0.5 shrink-0" :class="s.cls">{{ s.label }}</span>
        <span class="text-[11px] text-stone-500 leading-tight">{{ s.desc }}</span>
      </div>
    </div>

    <div v-if="open" @click="open = false" class="fixed inset-0 z-40" />
  </div>
</template>
