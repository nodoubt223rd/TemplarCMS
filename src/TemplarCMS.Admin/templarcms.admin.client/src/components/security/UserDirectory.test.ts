import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { flushPromises, mount } from '@vue/test-utils'
import UserDirectory from './UserDirectory.vue'

const user = { id: 'user-1', firstName: 'Ada', lastName: 'Lovelace', email: 'ada@example.test',
  language: 'en', roles: ['Reviewer'], status: 'deactivated', revision: 'revision-1',
  createdAt: '2026-09-01T00:00:00Z', lastLogin: null }
const fetchMock = vi.fn()
const wrappers: ReturnType<typeof mount>[] = []

beforeEach(() => {
  vi.stubGlobal('fetch', fetchMock)
  fetchMock.mockImplementation(async (url: string) => new Response(JSON.stringify(
    url.endsWith('/roles') ? { items: [{ id: 'Reviewer', label: 'Reviewer' }] } : { items: [user], total: 1 }
  ), { status: 200 }))
  HTMLDialogElement.prototype.showModal = function () { this.open = true }
  HTMLDialogElement.prototype.close = function () { this.open = false }
})
afterEach(() => {
  wrappers.forEach(w => w.unmount())
  wrappers.length = 0
  vi.unstubAllGlobals()
  vi.clearAllMocks()
})

async function connect() {
  const wrapper = mount(UserDirectory)
  wrappers.push(wrapper)
  await wrapper.get('input[type="password"]').setValue('operator-key')
  await wrapper.get('form').trigger('submit')
  await flushPromises()
  return wrapper
}

describe('UserDirectory', () => {
  it('includes deactivated users by default and keeps lifecycle actions absent', async () => {
    const wrapper = await connect()
    expect(wrapper.get('tbody').text()).toContain('deactivated')
    expect(fetchMock.mock.calls[0]![0]).not.toContain('status=')
    expect(fetchMock.mock.calls[0]![1].headers['X-Templar-Api-Key']).toBe('operator-key')
    await wrapper.get('tbody button').trigger('click')
    expect(wrapper.get('dialog').text()).toContain('Status changes are not available yet')
    expect(wrapper.find('dialog input[type="password"]').exists()).toBe(false)
    expect(wrapper.findAll('button').some(b => /deactivate|reactivate|delete|password/i.test(b.text()))).toBe(false)
  })

  it('sends profile and revision only, retaining the dialog after a stale save', async () => {
    const wrapper = await connect()
    await wrapper.get('tbody button').trigger('click')
    fetchMock.mockResolvedValueOnce(new Response(JSON.stringify({ detail: 'Reload it before saving again.' }), { status: 409 }))
    await wrapper.get('dialog form').trigger('submit')
    await flushPromises()
    const call = fetchMock.mock.calls.find(c => c[1].method === 'PUT')!
    expect(JSON.parse(call[1].body)).toEqual({ firstName: 'Ada', lastName: 'Lovelace', email: 'ada@example.test', language: 'en', roles: ['Reviewer'], revision: 'revision-1' })
    expect(wrapper.get('dialog [role="alert"]').text()).toContain('Reload')
    expect((wrapper.get('dialog').element as HTMLDialogElement).open).toBe(true)
  })

  it('creates pending records without status or credential fields', async () => {
    const wrapper = await connect()
    await wrapper.findAll('button').find(b => b.text() === 'Add pending user')!.trigger('click')
    const inputs = wrapper.findAll('dialog input')
    await inputs[0]!.setValue('Grace')
    await inputs[1]!.setValue('Hopper')
    await inputs[2]!.setValue('grace@example.test')
    await wrapper.get('dialog form').trigger('submit')
    await flushPromises()
    const call = fetchMock.mock.calls.find(c => c[1].method === 'POST')!
    expect(JSON.parse(call[1].body)).toEqual({ firstName: 'Grace', lastName: 'Hopper', email: 'grace@example.test', language: 'en', roles: [] })
  })

  it('reveals invalid fields on a hidden tab instead of submitting', async () => {
    const wrapper = await connect()
    await wrapper.findAll('button').find(b => b.text() === 'Add pending user')!.trigger('click')
    await wrapper.findAll('dialog nav button')[2]!.trigger('click')
    await wrapper.get('dialog form').trigger('submit')
    await flushPromises()
    expect(wrapper.findAll('dialog nav button')[0]!.attributes('aria-pressed')).toBe('true')
    expect(fetchMock.mock.calls.some(c => c[1].method === 'POST')).toBe(false)
  })

  it('clears the operator key when disconnected', async () => {
    const wrapper = await connect()
    await wrapper.findAll('button').find(b => b.text() === 'Disconnect')!.trigger('click')
    expect((wrapper.get('input[type="password"]').element as HTMLInputElement).value).toBe('')
    expect(wrapper.find('tbody').exists()).toBe(false)
  })
})
