<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import type { LoginCredentials } from '@/types/auth'

const props = withDefaults(defineProps<{ busy?: boolean; error?: string | null }>(), { busy: false, error: null })
const emit = defineEmits<{ submit: [credentials: LoginCredentials] }>()
const loginName = ref('TemplarAdmin')
const password = ref('')
const localError = ref<string | null>(null)
const submitting = ref(false)
const usernameInput = ref<HTMLInputElement>()
const alert = ref<HTMLElement>()
const message = computed(() => localError.value ?? props.error)

onMounted(() => { if (message.value) alert.value?.focus(); else usernameInput.value?.focus() })
onBeforeUnmount(() => { password.value = '' })
watch(message, value => { if (value) void nextTick(() => alert.value?.focus()) })

async function submit() {
  if (props.busy || submitting.value) return
  localError.value = null
  if (!loginName.value.trim() || !password.value) { localError.value = 'Enter your login name and password.'; return }
  submitting.value = true
  emit('submit', { loginName: loginName.value.trim(), password: password.value })
  password.value = ''
  await nextTick()
  submitting.value = false
}
</script>

<template>
  <main class="auth-shell">
    <form class="auth-card" novalidate aria-labelledby="login-title" @submit.prevent="submit">
      <p class="brand">TemplarCMS</p>
      <h1 id="login-title">Sign in</h1>
      <p>Enter your administrator credentials to open the workspace.</p>
      <div v-if="message" ref="alert" role="alert" tabindex="-1" class="error">{{ message }}</div>
      <label>Login name<input ref="usernameInput" v-model="loginName" name="loginName" autocomplete="username" required :disabled="busy" /></label>
      <label>Password<input v-model="password" name="password" type="password" autocomplete="current-password" required :disabled="busy" /></label>
      <button type="submit" :disabled="busy || submitting">{{ busy ? 'Signing in…' : 'Sign in' }}</button>
    </form>
  </main>
</template>

<style scoped>
.auth-shell { min-height: 100vh; display: grid; place-items: center; padding: 2rem; background: #f5f3ef; color: #1e1c18; }
.auth-card { width: min(100%, 25rem); padding: 2rem; border: 1px solid #ddd8ce; border-radius: 1rem; background: white; box-shadow: 0 12px 40px #1e1c1810; }
.brand { font-weight: 700; color: #5970e3; } h1 { margin: 1rem 0; font-size: 1.6rem; font-weight: 600; } p { margin-bottom: 1rem; font-size: .9rem; }
label { display: block; margin: 1rem 0; font-size: .9rem; } input { display: block; width: 100%; margin-top: .4rem; padding: .7rem; border: 1px solid #ccc7bd; border-radius: .4rem; }
button { width: 100%; margin-top: .5rem; padding: .75rem; border-radius: .5rem; background: #5970e3; color: white; } button:disabled { opacity: .55; } .error { padding: .75rem; border-radius: .5rem; background: #fff1f2; color: #9f1239; }
</style>
