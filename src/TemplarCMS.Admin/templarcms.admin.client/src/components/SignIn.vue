<script setup lang="ts">
import { ref } from 'vue'
import ModularMark from '@/components/ui/ModularMark.vue'

const emit = defineEmits<{ (e: 'login'): void }>()

const username = ref('')
const password = ref('')
const remember = ref(false)
const loading = ref(false)
const error = ref('')

async function handleSubmit() {
  if (!username.value || !password.value) {
    error.value = 'Please enter your username and password.'
    return
  }
  error.value = ''
  loading.value = true
  await new Promise(r => setTimeout(r, 800))
  loading.value = false
  emit('login')
}
</script>

<template>
  <div class="min-h-screen flex items-center justify-center relative overflow-hidden"
       style="background: radial-gradient(ellipse at 50% 40%, #2d2a42 0%, #1e1c18 55%, #131210 100%);">

    <!-- Vignette -->
    <div class="absolute inset-0 pointer-events-none"
         style="background: radial-gradient(ellipse at 50% 50%, transparent 30%, rgba(0,0,0,0.45) 100%)" />

    <!-- Honeycomb texture -->
    <svg class="absolute inset-0 w-full h-full pointer-events-none opacity-[0.045]" xmlns="http://www.w3.org/2000/svg">
      <defs>
        <pattern id="honeycomb" x="0" y="0" width="56" height="48" patternUnits="userSpaceOnUse">
          <polygon points="14,2 28,10 28,26 14,34 0,26 0,10"   fill="none" stroke="white" stroke-width="1"/>
          <polygon points="42,2 56,10 56,26 42,34 28,26 28,10"  fill="none" stroke="white" stroke-width="1"/>
          <polygon points="28,26 42,34 42,50 28,58 14,50 14,34" fill="none" stroke="white" stroke-width="1"/>
          <polygon points="0,26 14,34 14,50 0,58 -14,50 -14,34" fill="none" stroke="white" stroke-width="1"/>
          <polygon points="56,26 70,34 70,50 56,58 42,50 42,34" fill="none" stroke="white" stroke-width="1"/>
        </pattern>
      </defs>
      <rect width="100%" height="100%" fill="url(#honeycomb)"/>
    </svg>

    <!-- Card -->
    <div class="relative z-10 w-full max-w-sm mx-4">
      <div class="rounded-2xl overflow-hidden shadow-2xl"
           style="background: rgba(18,16,14,0.82); backdrop-filter: blur(12px); border: 1px solid rgba(255,255,255,0.07);">

        <!-- Logo -->
        <div class="flex flex-col items-center pt-10 pb-7 px-8">
          <div class="flex items-center gap-3 mb-2">
            <ModularMark :size="36" accent="#5970e3" solid="#f5f3ef" />
            <span class="text-[22px] font-semibold tracking-tight text-[#f5f3ef]" style="letter-spacing: -0.02em;">TemplarCMS</span>
          </div>
          <p class="text-[12px] text-[#5a5650] mt-1 tracking-wide">Content Management System</p>
        </div>

        <div class="h-px mx-8" style="background: linear-gradient(90deg, transparent, rgba(255,255,255,0.07), transparent)" />

        <!-- Form -->
        <form @submit.prevent="handleSubmit" class="px-8 pt-7 pb-8 flex flex-col gap-4">
          <p v-if="error" class="text-[12px] text-rose-400 text-center -mb-1">{{ error }}</p>

          <div class="flex flex-col gap-1.5">
            <label class="text-[11px] font-semibold uppercase tracking-widest text-[#7a7268]">User Name</label>
            <input
              v-model="username"
              type="text"
              autocomplete="username"
              placeholder="e.g. admin"
              class="w-full px-3.5 py-2.5 rounded-lg text-sm text-[#f0ede7] placeholder:text-[#3d3a34] outline-none transition-shadow"
              style="background: rgba(255,255,255,0.06); border: 1px solid rgba(255,255,255,0.09);"
              @focus="($event.target as HTMLElement).style.borderColor = '#5970e3'"
              @blur="($event.target as HTMLElement).style.borderColor = 'rgba(255,255,255,0.09)'"
            />
          </div>

          <div class="flex flex-col gap-1.5">
            <label class="text-[11px] font-semibold uppercase tracking-widest text-[#7a7268]">Password</label>
            <input
              v-model="password"
              type="password"
              autocomplete="current-password"
              placeholder="••••••••"
              class="w-full px-3.5 py-2.5 rounded-lg text-sm text-[#f0ede7] placeholder:text-[#3d3a34] outline-none transition-shadow"
              style="background: rgba(255,255,255,0.06); border: 1px solid rgba(255,255,255,0.09);"
              @focus="($event.target as HTMLElement).style.borderColor = '#5970e3'"
              @blur="($event.target as HTMLElement).style.borderColor = 'rgba(255,255,255,0.09)'"
            />
          </div>

          <button
            type="submit"
            :disabled="loading"
            class="w-full py-2.5 rounded-lg text-sm font-semibold text-white transition-all mt-1"
            style="background: linear-gradient(135deg, #6478e8 0%, #5970e3 60%, #4a5ed4 100%); box-shadow: 0 4px 16px rgba(89,112,227,0.35);"
            :style="loading ? 'opacity: 0.7; cursor: not-allowed;' : ''"
          >
            <span v-if="!loading">Log in</span>
            <span v-else class="flex items-center justify-center gap-2">
              <svg class="animate-spin w-4 h-4" viewBox="0 0 24 24" fill="none">
                <circle cx="12" cy="12" r="10" stroke="currentColor" stroke-width="3" stroke-dasharray="32" stroke-dashoffset="12"/>
              </svg>
              Signing in…
            </span>
          </button>

          <div class="flex items-center justify-between mt-0.5">
            <label class="flex items-center gap-2 cursor-pointer select-none">
              <input v-model="remember" type="checkbox" class="w-3.5 h-3.5 rounded accent-[#5970e3]" />
              <span class="text-[12px] text-[#5a5650]">Remember me</span>
            </label>
            <a href="#" class="text-[12px] text-[#5a5650] hover:text-[#c8c3bc] transition-colors">Forgot your password?</a>
          </div>
        </form>
      </div>

      <p class="text-center text-[10px] text-[#3a3830] mt-5 tracking-wide">
        TemplarCMS &copy; {{ new Date().getFullYear() }} &middot; All rights reserved
      </p>
    </div>
  </div>
</template>
