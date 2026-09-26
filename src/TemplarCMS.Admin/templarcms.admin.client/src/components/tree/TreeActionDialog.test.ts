import { mount } from '@vue/test-utils'
import { afterEach, describe, expect, it, vi } from 'vitest'
import TreeActionDialog from './TreeActionDialog.vue'
afterEach(() => { vi.restoreAllMocks(); Reflect.deleteProperty(HTMLDialogElement.prototype, 'showModal') })
describe('action dialog', () => {
  it('retains input across failures and blocks submission while saving', async () => {
    Object.defineProperty(HTMLDialogElement.prototype, 'showModal', { configurable: true, value: function (this: HTMLDialogElement) { this.open = true } })
    const wrapper = mount(TreeActionDialog, { props: { title: 'Rename', initialName: 'Original', showName: true, busy: false }, global: { stubs: { teleport: true } }, attachTo: document.body })
    await wrapper.find('input').setValue('Edited')
    await wrapper.setProps({ busy: true })
    await wrapper.find('form').trigger('submit')
    expect(wrapper.emitted('submit')).toBeUndefined()
    await wrapper.setProps({ busy: false, error: 'Stale revision' })
    expect((wrapper.find('input').element as HTMLInputElement).value).toBe('Edited')
    await wrapper.find('form').trigger('submit')
    expect(wrapper.emitted('submit')?.[0]?.[0]).toMatchObject({ name: 'Edited' })
    wrapper.unmount()
  })
})
