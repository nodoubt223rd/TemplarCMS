<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import type { PasswordChange } from '@/types/auth'

const props = withDefaults(defineProps<{ busy?: boolean; error?: string | null }>(), { busy: false, error: null })
const emit = defineEmits<{ submit: [passwords: PasswordChange]; logout: [] }>()
const currentPassword = ref('')
const newPassword = ref('')
const confirmation = ref('')
const localError = ref<string | null>(null)
const submitting = ref(false)
const currentInput = ref<HTMLInputElement>()
const alert = ref<HTMLElement>()
const message = computed(() => localError.value ?? props.error)

onMounted(() => currentInput.value?.focus())
onBeforeUnmount(clearPasswords)
watch(message, value => { if (value) void nextTick(() => alert.value?.focus()) })

function clearPasswords() { currentPassword.value = ''; newPassword.value = ''; confirmation.value = '' }
async function submit() {
  if (props.busy || submitting.value) return
  localError.value = null
  if (!currentPassword.value) { localError.value = 'Enter your temporary password.'; return }
  if (newPassword.value.length < 15 || newPassword.value.length > 128) { localError.value = 'Choose a password between 15 and 128 characters.'; return }
  if (newPassword.value === currentPassword.value) { localError.value = 'Choose a different password.'; return }
  if (newPassword.value !== confirmation.value) { localError.value = 'The new passwords must match.'; return }
  submitting.value = true
  emit('submit', { currentPassword: currentPassword.value, newPassword: newPassword.value })
  clearPasswords()
  await nextTick()
  submitting.value = false
}
</script>

<template>
  <main class="auth-shell">
    <form class="auth-card" novalidate aria-labelledby="change-title" @submit.prevent="submit">
      <p class="brand">TemplarCMS</p>
      <h1 id="change-title">Choose your password</h1>
      <p>Replace your temporary password before opening the workspace. Use 15–128 characters; spaces are welcome.</p>
      <div v-if="message" ref="alert" role="alert" tabindex="-1" class="error">{{ message }}</div>
      <label>Temporary password<input ref="currentInput" v-model="currentPassword" name="currentPassword" type="password" autocomplete="current-password" required :disabled="busy" /></label>
      <label>New password<input v-model="newPassword" name="newPassword" type="password" autocomplete="new-password" minlength="15" maxlength="128" required :disabled="busy" /></label>
      <label>Confirm new password<input v-model="confirmation" name="confirmation" type="password" autocomplete="new-password" required :disabled="busy" /></label>
      <button type="submit" :disabled="busy || submitting">{{ busy ? 'Saving password…' : 'Save password' }}</button>
      <button type="button" class="secondary" :disabled="busy" @click="emit('logout')">Sign out</button>
    </form>
  </main>
</template>

<style scoped>
.auth-shell { min-height: 100vh; display: grid; place-items: center; padding: 2rem; background: #f5f3ef; color: #1e1c18; }
.auth-card { width: min(100%, 28rem); padding: 2rem; border: 1px solid #ddd8ce; border-radius: 1rem; background: white; box-shadow: 0 12px 40px #1e1c1810; }
.brand { font-weight: 700; color: #5970e3; } h1 { margin: 1rem 0; font-size: 1.6rem; font-weight: 600; } p { margin-bottom: 1rem; font-size: .9rem; }
label { display: block; margin: 1rem 0; font-size: .9rem; } input { display: block; width: 100%; margin-top: .4rem; padding: .7rem; border: 1px solid #ccc7bd; border-radius: .4rem; }
button { width: 100%; margin-top: .5rem; padding: .75rem; border-radius: .5rem; background: #5970e3; color: white; } button:disabled { opacity: .55; } .secondary { background: transparent; color: #3a4eb0; } .error { padding: .75rem; border-radius: .5rem; background: #fff1f2; color: #9f1239; }
</style>
