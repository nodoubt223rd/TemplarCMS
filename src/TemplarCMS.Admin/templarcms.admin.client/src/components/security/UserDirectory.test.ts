import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { flushPromises, mount } from '@vue/test-utils'
import { ref } from 'vue'
import UserDirectory from './UserDirectory.vue'

const user = { id: 'user-1', firstName: 'Ada', lastName: 'Lovelace', email: 'ada@example.test',
  language: 'en', roles: ['Author'], status: 'deactivated', revision: 'revision-1',
  createdAt: '2026-09-01T00:00:00Z', lastLogin: null }
const fetchMock = vi.fn()
const wrappers: ReturnType<typeof mount>[] = []
const catalog = [
  { id: 'Author', label: 'Author', description: 'Creates structured content.', isAssignable: true, availability: 'available' },
  { id: 'MarketingAutomationEditors', label: 'Marketing Automation Editors', description: 'Configures automated journeys.', isAssignable: false, availability: 'planned' }
]

beforeEach(() => {
  vi.stubGlobal('fetch', fetchMock)
  fetchMock.mockImplementation(async (url: string) => new Response(JSON.stringify(
    url.endsWith('/roles') ? { items: catalog } : { items: [user], total: 1 }
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
  await flushPromises()
  return wrapper
}

describe('UserDirectory', () => {
  it('resumes an open profile draft after the same account signs back in', async () => {
    const suspended = ref(false)
    const wrapper = mount(UserDirectory, { global: { provide: { 'workspace-suspended': suspended } } }); wrappers.push(wrapper)
    await flushPromises()
    await wrapper.get('tbody button').trigger('click')
    await wrapper.get('dialog input').setValue('Unsaved first name')
    suspended.value = true; await flushPromises()
    expect((wrapper.get('dialog').element as HTMLDialogElement).open).toBe(false)
    suspended.value = false; await flushPromises()
    expect((wrapper.get('dialog').element as HTMLDialogElement).open).toBe(true)
    expect((wrapper.get('dialog input').element as HTMLInputElement).value).toBe('Unsaved first name')
  })

  it('includes deactivated users by default and keeps lifecycle actions absent', async () => {
    const wrapper = await connect()
    expect(wrapper.get('tbody').text()).toContain('deactivated')
    expect(fetchMock.mock.calls[0]![0]).not.toContain('status=')
    expect(wrapper.find('input[type="password"]').exists()).toBe(false)
    expect(new Headers((fetchMock.mock.calls[0]![1] as RequestInit).headers).has('X-Templar-Api-Key')).toBe(false)
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
    expect(JSON.parse(call[1].body)).toEqual({ firstName: 'Ada', lastName: 'Lovelace', email: 'ada@example.test', language: 'en', roles: ['Author'], revision: 'revision-1' })
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

  it('uses the shared account session without separate connection controls', async () => {
    const wrapper = await connect()
    expect(wrapper.find('input[type="password"]').exists()).toBe(false)
    expect(wrapper.findAll('button').some(button => button.text() === 'Disconnect')).toBe(false)
    expect(wrapper.find('tbody').exists()).toBe(true)
  })

  it('shows server descriptions and prevents assigning the planned role', async () => {
    const wrapper = await connect()
    await wrapper.findAll('button').find(b => b.text() === 'Add pending user')!.trigger('click')
    await wrapper.findAll('dialog nav button')[1]!.trigger('click')
    expect(wrapper.get('fieldset').text()).toContain('Configures automated journeys.')
    expect(wrapper.get('fieldset').text()).toContain('Planned')
    expect(wrapper.get('fieldset').text()).toContain('No roles assigned')
    expect(wrapper.get('input[value="MarketingAutomationEditors"]').attributes('disabled')).toBeDefined()
    await wrapper.get('input[value="Author"]').setValue(true)
    expect(wrapper.get('input[value="Author"]').element).toHaveProperty('checked', true)
    expect(wrapper.findAll('fieldset button').some(b => /Up|Down/.test(b.text()))).toBe(false)
  })

  it('lets an existing planned membership be preserved or removed without offering reassignment', async () => {
    fetchMock.mockImplementation(async (url: string) => new Response(JSON.stringify(
      url.endsWith('/roles') ? { items: catalog } : { items: [{ ...user, roles: ['MarketingAutomationEditors'] }], total: 1 }
    ), { status: 200 }))
    const wrapper = await connect()
    await wrapper.get('tbody button').trigger('click')
    const planned = wrapper.get('input[value="MarketingAutomationEditors"]')
    expect(planned.element).toHaveProperty('checked', true)
    expect(planned.attributes('disabled')).toBeUndefined()
    await wrapper.get('dialog form').trigger('submit')
    await flushPromises()
    const preserved = fetchMock.mock.calls.find(c => c[1].method === 'PUT')!
    expect(JSON.parse(preserved[1].body).roles).toEqual(['MarketingAutomationEditors'])
    await wrapper.get('tbody button').trigger('click')
    await wrapper.get('input[value="MarketingAutomationEditors"]').setValue(false)
    expect(wrapper.get('input[value="MarketingAutomationEditors"]').attributes('disabled')).toBeDefined()
    await wrapper.get('dialog form').trigger('submit')
    await flushPromises()
    const removed = fetchMock.mock.calls.filter(c => c[1].method === 'PUT')[1]!
    expect(JSON.parse(removed[1].body).roles).toEqual([])
  })
})
