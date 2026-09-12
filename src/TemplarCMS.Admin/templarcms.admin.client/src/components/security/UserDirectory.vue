<script setup lang="ts">
import { nextTick, onBeforeUnmount, reactive, ref } from 'vue'
import { fetchJson, getErrorMessage } from '@/utils/request-helpers'
import { useToast } from '@/composables/useToast'
import MultilistWithSearchField from '@/components/fields/MultilistWithSearchField.vue'

type Status = 'invited' | 'active' | 'suspended' | 'deactivated'
type User = { id: string; firstName: string; lastName: string; email: string; language: string;
  roles: string[]; status: Status; revision: string; createdAt: string; lastLogin: string | null }
type Role = { id: string; label: string }
const statuses: Status[] = ['invited', 'active', 'suspended', 'deactivated']
const users = ref<User[]>([])
const roles = ref<Role[]>([])
const total = ref(0)
const offset = ref(0)
const search = ref('')
const status = ref('')
const role = ref('')
const key = ref('')
const headerName = ref('X-Templar-Api-Key')
const connected = ref(false)
const loading = ref(false)
const saving = ref(false)
const error = ref('')
const formError = ref('')
const dialog = ref<HTMLDialogElement>()
const selected = ref<User | null>(null)
const tab = ref('general')
const draft = reactive({ firstName: '', lastName: '', email: '', language: 'en', roles: [] as string[] })
const toast = useToast()
let controller: AbortController | undefined
const headers = () => ({ [headerName.value]: key.value, 'Content-Type': 'application/json' })

async function load(reset = false) {
  if (reset) offset.value = 0
  controller?.abort()
  const current = new AbortController()
  controller = current
  loading.value = true
  error.value = ''
  try {
    const query = new URLSearchParams({ offset: String(offset.value), limit: '25' })
    if (search.value.trim()) query.set('search', search.value.trim())
    if (status.value) query.set('status', status.value)
    if (role.value) query.set('role', role.value)
    const [list, catalog] = await Promise.all([
      fetchJson<{ items: User[]; total: number }>(`/api/v1/security/users?${query}`, { headers: headers(), signal: current.signal }),
      fetchJson<{ items: Role[] }>('/api/v1/security/roles', { headers: headers(), signal: current.signal })
    ])
    if (current.signal.aborted) return
    users.value = list.items
    total.value = list.total
    roles.value = catalog.items
    connected.value = true
  } catch (cause) {
    if (current.signal.aborted) return
    error.value = getErrorMessage(cause)
    users.value = []
    total.value = 0
  } finally {
    if (!current.signal.aborted) loading.value = false
  }
}

function edit(user: User | null) {
  selected.value = user
  Object.assign(draft, { firstName: user?.firstName ?? '', lastName: user?.lastName ?? '',
    email: user?.email ?? '', language: user?.language ?? 'en', roles: [...(user?.roles ?? [])] })
  tab.value = 'general'
  formError.value = ''
  dialog.value?.showModal()
}

async function save() {
  if (saving.value) return
  const invalid = dialog.value?.querySelector<HTMLInputElement>('input:invalid')
  if (invalid) {
    tab.value = invalid.closest('[data-language]') ? 'language' : 'general'
    await nextTick()
    invalid.reportValidity()
    invalid.focus()
    return
  }
  saving.value = true
  formError.value = ''
  try {
    const id = selected.value?.id
    await fetchJson<User>(`/api/v1/security/users${id ? '/' + id : ''}`, {
      method: id ? 'PUT' : 'POST', headers: headers(),
      body: JSON.stringify({ ...draft, ...(id ? { revision: selected.value!.revision } : {}) })
    })
    dialog.value?.close()
    toast.success(id ? 'Profile saved' : 'Pending user created', id ? 'Directory details updated.' : 'No invitation has been sent and no sign-in access has been granted.')
    await load(true)
  } catch (cause) {
    formError.value = getErrorMessage(cause)
  } finally { saving.value = false }
}

function disconnect() {
  controller?.abort()
  key.value = ''
  users.value = []
  roles.value = []
  selected.value = null
  connected.value = false
  loading.value = false
  dialog.value?.close()
}
onBeforeUnmount(disconnect)
const roleLabel = (id: string) => roles.value.find(r => r.id === id)?.label ?? id
const date = (value: string | null) => value ? new Date(value).toLocaleDateString() : 'Never'
</script>

<template>
  <section class="flex min-h-0 flex-1 flex-col bg-white text-stone-700">
    <header class="flex flex-wrap items-center justify-between gap-3 border-b border-stone-200 px-6 py-4">
      <div><h1 class="text-base font-semibold">Security</h1><p class="text-xs text-stone-500">User directory and role assignments</p></div>
      <div v-if="connected" class="flex gap-2">
        <button class="rounded-lg px-3 py-2 text-sm hover:bg-stone-100" @click="disconnect">Disconnect</button>
        <button class="rounded-lg bg-[#5970e3] px-3 py-2 text-sm text-white" @click="edit(null)">Add pending user</button>
      </div>
    </header>
    <p class="border-b border-stone-200 bg-stone-50 px-6 py-3 text-xs text-stone-600">Invitation delivery, account activation, and sign-in permissions are not available yet. Role assignments are recorded for the upcoming access-control release.</p>
    <form v-if="!connected" class="mx-auto my-8 w-full max-w-md space-y-4 px-6" @submit.prevent="load(true)">
      <h2 class="font-semibold">Connect to the user directory</h2>
      <p class="text-sm">Use the configured operator API key. It is held only while this screen is open.</p>
      <label class="block text-sm">API key header<input v-model="headerName" required class="mt-1 w-full rounded border border-stone-300 p-2" /></label>
      <label class="block text-sm">Operator API key<input v-model="key" required type="password" autocomplete="off" class="mt-1 w-full rounded border border-stone-300 p-2" /></label>
      <button :disabled="loading" class="rounded-lg bg-[#5970e3] px-4 py-2 text-white disabled:opacity-50">{{ loading ? 'Connecting...' : 'Connect' }}</button>
    </form>
    <form v-if="connected" class="flex flex-wrap gap-3 border-b border-stone-200 px-6 py-3" @submit.prevent="load(true)">
      <label class="text-xs">Search<input v-model="search" type="search" class="ml-2 rounded border border-stone-300 p-2 text-sm" placeholder="Name or email" /></label>
      <label class="text-xs">Status<select v-model="status" class="ml-2 rounded border border-stone-300 p-2 text-sm" @change="load(true)"><option value="">All statuses</option><option v-for="s in statuses" :key="s" :value="s">{{ s }}</option></select></label>
      <label class="text-xs">Role<select v-model="role" class="ml-2 rounded border border-stone-300 p-2 text-sm" @change="load(true)"><option value="">All roles</option><option v-for="r in roles" :key="r.id" :value="r.id">{{ r.label }}</option></select></label>
      <button :disabled="loading" class="rounded-lg border border-stone-300 px-3 py-2 text-sm">Refresh</button>
    </form>
    <div v-if="error" role="alert" class="m-4 rounded bg-rose-50 p-4 text-sm text-rose-800">{{ error }}</div>
    <div v-if="loading" role="status" class="p-6 text-sm">Loading users...</div>
    <div v-else-if="connected" class="min-h-0 flex-1 overflow-auto">
      <table v-if="users.length" class="w-full text-left text-sm">
        <thead class="sticky top-0 bg-stone-50 text-xs text-stone-500"><tr><th class="p-4">User</th><th class="p-4">Email</th><th class="p-4">Roles</th><th class="p-4">Status</th><th class="p-4">Last login</th></tr></thead>
        <tbody><tr v-for="user in users" :key="user.id" class="border-b border-stone-100 hover:bg-stone-50" :class="{ 'bg-stone-50 text-stone-500': user.status === 'deactivated' }">
          <td class="p-4"><button class="text-left font-medium text-[#3a4eb0] underline-offset-4 hover:underline" @click="edit(user)">{{ user.firstName }} {{ user.lastName }}</button></td>
          <td class="p-4">{{ user.email }}</td><td class="p-4"><span v-for="r in user.roles" :key="r" class="m-0.5 inline-block rounded-full bg-[#e8eaf8] px-2 py-1 text-xs text-[#3a4eb0]">{{ roleLabel(r) }}</span></td>
          <td class="p-4 capitalize">{{ user.status }}</td><td class="p-4">{{ date(user.lastLogin) }}</td>
        </tr></tbody>
      </table>
      <p v-else-if="!error" class="p-10 text-center text-sm text-stone-500">{{ search || role || status ? 'No users match your filters.' : 'No users yet. Add a pending user to prepare the directory.' }}</p>
    </div>
    <footer v-if="connected" class="flex items-center justify-between border-t border-stone-200 px-6 py-3 text-xs">
      <span>{{ total }} users</span><div class="flex gap-3"><button :disabled="offset === 0 || loading" @click="offset -= 25; load()">Previous</button><button :disabled="offset + 25 >= total || loading" @click="offset += 25; load()">Next</button></div>
    </footer>
    <dialog ref="dialog" class="m-auto max-h-[90vh] w-[min(42rem,95vw)] rounded-2xl p-0 shadow-xl backdrop:bg-black/50" aria-labelledby="profile-title" @cancel="saving && $event.preventDefault()">
      <form novalidate class="flex max-h-[90vh] flex-col" @submit.prevent="save">
        <header class="flex items-center justify-between bg-[#1e1c18] px-6 py-4 text-white"><h2 id="profile-title">{{ selected ? 'Edit user profile' : 'Add pending user' }}</h2><button type="button" :disabled="saving" aria-label="Close profile" @click="dialog?.close()">Close</button></header>
        <nav class="flex gap-2 border-b border-stone-200 bg-stone-50 p-2" aria-label="Profile sections"><button v-for="t in ['general', 'member-of', 'language']" :key="t" type="button" class="rounded px-4 py-2 text-sm capitalize" :class="{ 'bg-white text-[#3a4eb0]': tab === t }" :aria-pressed="tab === t" @click="tab = t">{{ t.replace('-', ' ') }}</button></nav>
        <div class="overflow-y-auto p-6">
          <div v-show="tab === 'general'" class="space-y-4">
            <p v-if="selected" class="text-xs text-stone-500">Created {{ date(selected.createdAt) }} · Last login {{ date(selected.lastLogin) }}</p>
            <label class="block text-sm">First name<input v-model="draft.firstName" required maxlength="100" class="mt-1 w-full rounded border border-stone-300 p-2" /></label>
            <label class="block text-sm">Last name<input v-model="draft.lastName" required maxlength="100" class="mt-1 w-full rounded border border-stone-300 p-2" /></label>
            <label class="block text-sm">Email address<input v-model="draft.email" required type="email" maxlength="254" class="mt-1 w-full rounded border border-stone-300 p-2" /></label>
            <p class="text-sm">Account status: <strong class="capitalize">{{ selected?.status ?? 'invited' }}</strong></p>
            <p class="text-xs text-stone-500">Status changes are not available yet. Creating this record does not send an invitation.</p>
          </div>
          <fieldset v-show="tab === 'member-of'" class="space-y-3"><legend class="mb-3 text-sm">Role membership</legend><MultilistWithSearchField v-model="draft.roles" :available="roles.map(r => ({ value: r.id, label: r.label }))" :readonly="saving" /></fieldset>
          <label v-show="tab === 'language'" data-language class="block text-sm">Preferred interface language<input v-model="draft.language" required maxlength="35" pattern="[A-Za-z0-9-]+" class="mt-2 w-full rounded border border-stone-300 p-2" /><span class="mt-2 block text-xs text-stone-500">Records a preference; available translations are unchanged.</span></label>
          <p v-if="formError" role="alert" class="mt-4 rounded bg-rose-50 p-3 text-sm text-rose-800">{{ formError }}</p>
        </div>
        <footer class="flex justify-end gap-3 border-t border-stone-200 bg-stone-50 p-4"><button type="button" :disabled="saving" @click="dialog?.close()">Cancel</button><button :disabled="saving" class="rounded-lg bg-[#5970e3] px-4 py-2 text-white disabled:opacity-50">{{ saving ? 'Saving...' : 'Save changes' }}</button></footer>
      </form>
    </dialog>
  </section>
</template>
