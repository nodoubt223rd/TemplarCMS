<script setup lang="ts">
import { useToast, type ToastType } from '@/composables/useToast'

const { toasts, dismiss } = useToast()

const config: Record<ToastType, { icon: string; bar: string; title: string; bg: string; ring: string }> = {
  success: {
    icon: `<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"/>`,
    bar: 'bg-emerald-500',
    title: 'text-emerald-700',
    bg: 'bg-white',
    ring: 'ring-emerald-100',
  },
  info: {
    icon: `<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16h-1v-4h-1m1-4h.01M12 2a10 10 0 100 20A10 10 0 0012 2z"/>`,
    bar: 'bg-[#5970e3]',
    title: 'text-[#3a4eb0]',
    bg: 'bg-white',
    ring: 'ring-[#5970e3]/15',
  },
  warning: {
    icon: `<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v4m0 4h.01M10.29 3.86L1.82 18a2 2 0 001.71 3h16.94a2 2 0 001.71-3L13.71 3.86a2 2 0 00-3.42 0z"/>`,
    bar: 'bg-amber-400',
    title: 'text-amber-700',
    bg: 'bg-white',
    ring: 'ring-amber-100',
  },
  error: {
    icon: `<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/>`,
    bar: 'bg-rose-500',
    title: 'text-rose-700',
    bg: 'bg-white',
    ring: 'ring-rose-100',
  },
}

const iconBg: Record<ToastType, string> = {
  success: 'bg-emerald-100 text-emerald-600',
  info:    'bg-[#e8eaf8] text-[#5970e3]',
  warning: 'bg-amber-100 text-amber-600',
  error:   'bg-rose-100 text-rose-600',
}
</script>

<template>
  <Teleport to="body">
    <div class="fixed bottom-5 right-5 z-[9999] flex flex-col gap-2.5 w-80 pointer-events-none">
      <TransitionGroup
        enter-active-class="transition-all duration-300 ease-out"
        enter-from-class="opacity-0 translate-y-3 scale-95"
        enter-to-class="opacity-100 translate-y-0 scale-100"
        leave-active-class="transition-all duration-200 ease-in"
        leave-from-class="opacity-100 translate-y-0 scale-100"
        leave-to-class="opacity-0 translate-y-2 scale-95"
      >
        <div
          v-for="toast in toasts"
          :key="toast.id"
          class="pointer-events-auto relative flex items-start gap-3 rounded-xl p-3.5 shadow-lg ring-1"
          :class="[config[toast.type].bg, config[toast.type].ring]"
        >
          <!-- Accent bar -->
          <div class="absolute left-1 top-3 bottom-3 w-1 rounded-full" :class="config[toast.type].bar" />

          <!-- Icon -->
          <div class="w-7 h-7 rounded-lg flex items-center justify-center shrink-0 ml-2" :class="iconBg[toast.type]">
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"
                 v-html="config[toast.type].icon" />
          </div>

          <!-- Text -->
          <div class="flex-1 min-w-0 pt-0.5">
            <p class="text-[13px] font-semibold leading-tight" :class="config[toast.type].title">
              {{ toast.title }}
            </p>
            <p v-if="toast.message" class="text-[12px] text-stone-500 mt-0.5 leading-snug">
              {{ toast.message }}
            </p>
          </div>

          <!-- Dismiss -->
          <button
            @click="dismiss(toast.id)"
            class="shrink-0 w-5 h-5 flex items-center justify-center rounded-md text-stone-400 hover:text-stone-600 hover:bg-stone-100 transition-colors mt-0.5"
          >
            <svg class="w-3 h-3" fill="none" stroke="currentColor" stroke-width="2.5" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" d="M6 18L18 6M6 6l12 12"/>
            </svg>
          </button>
        </div>
      </TransitionGroup>
    </div>
  </Teleport>
</template>
