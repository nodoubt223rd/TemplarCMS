import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { flushPromises, mount } from '@vue/test-utils'
import App from './App.vue'
import { fetchJson } from './utils/request-helpers'

vi.mock('./components/AuthorWorkspace.vue', () => ({ default: {
  props: ['fieldForm'], emits: ['logout', 'fieldInput'],
  template: '<section data-test="workspace"><input data-test="draft" :value="fieldForm.body ?? \'\'" @input="$emit(\'fieldInput\', \'body\', $event.target.value)" /><button data-test="logout" @click="$emit(\'logout\')">Sign out</button></section>'
} }))

const session = { userId: 'admin-1', loginName: 'TemplarAdmin', email: 'admin@example.test', language: 'en', roles: ['TemplarAdmin'], mustChangePassword: true, _links: {} }
const fetchMock = vi.fn()
let loggedIn = false
let changed = false
let expired = false
let accountId = 'admin-1'
const wrappers: ReturnType<typeof mount>[] = []

beforeEach(() => {
  loggedIn = false; changed = false; expired = false; accountId = 'admin-1'
  vi.stubGlobal('fetch', fetchMock)
  fetchMock.mockImplementation(async (url: string) => {
    if (url.endsWith('/antiforgery')) return new Response(JSON.stringify({ requestToken: 'token', headerName: 'X-Templar-CSRF' }))
    if (url.endsWith('/login')) { loggedIn = true; expired = false; return new Response(JSON.stringify({ ...session, userId: accountId, mustChangePassword: !changed })) }
    if (url.endsWith('/change-password')) { changed = true; return new Response(JSON.stringify({ ...session, mustChangePassword: false })) }
    if (url.endsWith('/logout')) { loggedIn = false; return new Response(null, { status: 204 }) }
    if (url.endsWith('/session')) return loggedIn ? new Response(JSON.stringify({ ...session, mustChangePassword: !changed })) : new Response('{}', { status: 401 })
    if (expired) return new Response(JSON.stringify({ code: 'authoring-authentication-required' }), { status: 401 })
    if (url.includes('/field-types')) return new Response(JSON.stringify({ embedded: { fieldTypes: [] } }))
    if (url.includes('/templates')) return new Response(JSON.stringify({ embedded: { templates: [] } }))
    return new Response(JSON.stringify({ item: null, embedded: { children: [] }, _links: {} }))
  })
  HTMLDialogElement.prototype.close = function () { this.open = false }
})
afterEach(() => { wrappers.forEach(wrapper => wrapper.unmount()); wrappers.length = 0; vi.unstubAllGlobals(); fetchMock.mockReset() })

async function login(wrapper: ReturnType<typeof mount>) {
  await wrapper.get('[name="password"]').setValue('temporary-password')
  await wrapper.get('form').trigger('submit')
  await flushPromises()
}
async function enterWorkspace(wrapper: ReturnType<typeof mount>) {
  await login(wrapper)
  await wrapper.get('[name="currentPassword"]').setValue('temporary-password')
  await wrapper.get('[name="newPassword"]').setValue('new private passphrase')
  await wrapper.get('[name="confirmation"]').setValue('new private passphrase')
  await wrapper.get('form').trigger('submit')
  await flushPromises()
}

describe('workspace authentication', () => {
  it('loads no authoring data before login and the mandatory password change', async () => {
    const wrapper = mount(App); wrappers.push(wrapper)
    await flushPromises()
    expect(wrapper.find('[name="password"]').exists()).toBe(true)
    expect(fetchMock.mock.calls.every(([url]) => String(url).startsWith('/api/v1/auth/'))).toBe(true)
    await login(wrapper)
    expect(wrapper.find('[name="newPassword"]').exists()).toBe(true)
    expect(wrapper.find('[data-test="workspace"]').exists()).toBe(false)
    expect(fetchMock.mock.calls.every(([url]) => String(url).startsWith('/api/v1/auth/'))).toBe(true)
  })

  it('preserves drafts during expiry but clears them on explicit logout', async () => {
    const wrapper = mount(App); wrappers.push(wrapper)
    await flushPromises(); await enterWorkspace(wrapper)
    await wrapper.get('[data-test="draft"]').setValue('Unsaved draft')
    expired = true
    await expect(fetchJson('/api/v1/templates')).rejects.toMatchObject({ status: 401 })
    await flushPromises()
    expect(wrapper.get('[data-test="workspace"]').isVisible()).toBe(false)
    await login(wrapper)
    expect((wrapper.get('[data-test="draft"]').element as HTMLInputElement).value).toBe('Unsaved draft')
    await wrapper.get('[data-test="logout"]').trigger('click'); await flushPromises()
    expect(wrapper.find('[data-test="workspace"]').exists()).toBe(false)
    await login(wrapper)
    expect((wrapper.get('[data-test="draft"]').element as HTMLInputElement).value).toBe('')
  })

  it('never restores another account’s draft after reauthentication', async () => {
    const wrapper = mount(App); wrappers.push(wrapper)
    await flushPromises(); await enterWorkspace(wrapper)
    await wrapper.get('[data-test="draft"]').setValue('First account draft')
    expired = true
    await expect(fetchJson('/api/v1/templates')).rejects.toMatchObject({ status: 401 })
    await flushPromises(); accountId = 'admin-2'; await login(wrapper)
    expect((wrapper.get('[data-test="draft"]').element as HTMLInputElement).value).toBe('')
  })
})
