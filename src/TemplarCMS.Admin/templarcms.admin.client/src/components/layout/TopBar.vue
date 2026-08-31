<script setup lang="ts">
import ModularMark from '@/components/ui/ModularMark.vue'

defineProps<{
  language: string
  version: number
  showActions: boolean
}>()

const emit = defineEmits<{
  (e: 'update:language', v: string): void
  (e: 'update:version', v: number): void
  (e: 'toggle-actions'): void
}>()

const languages = ['en', 'en-US', 'fr-FR', 'de-DE', 'es-ES', 'ja-JP']
const versions = [1, 2, 3]
</script>

<template>
  <header class="flex items-center gap-3 px-4 h-11 bg-[#1e1c18] shrink-0">
    <!-- Logo -->
    <div class="flex items-center gap-2 select-none mr-2">
      <ModularMark :size="22" accent="#5970e3" solid="#f5f3ef" />
      <span class="text-[13px] font-semibold tracking-tight text-[#f5f3ef]">TemplarCMS</span>
    </div>

    <!-- Search -->
    <div class="flex items-center gap-1.5 px-2.5 py-1.5 rounded-md bg-white/8 ring-1 ring-white/10 w-48
                focus-within:ring-[#5970e3]/50 transition-shadow">
      <svg width="12" height="12" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.8"
           stroke-linecap="round" stroke-linejoin="round" class="text-[#6e6560] shrink-0">
        <circle cx="6.5" cy="6.5" r="4.5"/><line x1="10" y1="10" x2="14" y2="14"/>
      </svg>
      <input
        placeholder="Search content…"
        class="flex-1 text-xs bg-transparent outline-none text-[#d4cfc8] placeholder:text-[#6e6560]"
      />
    </div>

    <div class="flex-1" />

    <!-- Language picker -->
    <select
      :value="language"
      @change="emit('update:language', ($event.target as HTMLSelectElement).value)"
      class="text-xs bg-white/8 text-[#c8c3bc] border border-white/10 rounded-md px-2 py-1 outline-none
             hover:bg-white/12 transition-colors cursor-pointer"
    >
      <option v-for="l in languages" :key="l" :value="l" class="bg-[#2a2824] text-stone-200">{{ l }}</option>
    </select>

    <!-- Version picker -->
    <select
      :value="version"
      @change="emit('update:version', Number(($event.target as HTMLSelectElement).value))"
      class="text-xs bg-white/8 text-[#c8c3bc] border border-white/10 rounded-md px-2 py-1 outline-none
             hover:bg-white/12 transition-colors cursor-pointer"
    >
      <option v-for="v in versions" :key="v" :value="v" class="bg-[#2a2824] text-stone-200">v{{ v }}</option>
    </select>

    <!-- Actions toggle -->
    <button
      @click="emit('toggle-actions')"
      class="px-2.5 py-1 rounded-md text-xs font-medium transition-colors"
      :class="showActions
        ? 'bg-[#5970e3] text-white'
        : 'bg-white/8 text-[#c8c3bc] hover:bg-white/12'"
    >
      Actions
    </button>

    <!-- User avatar -->
    <div class="w-7 h-7 rounded-full bg-[#5970e3] flex items-center justify-center
                text-[11px] font-bold text-white select-none cursor-pointer" title="A. Kumar">
      AK
    </div>
  </header>
</template>
