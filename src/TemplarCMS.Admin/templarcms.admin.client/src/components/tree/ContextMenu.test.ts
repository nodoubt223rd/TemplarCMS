import { mount } from '@vue/test-utils'
import { describe, it, expect } from 'vitest'
import ContextMenu from './ContextMenu.vue'

describe('tree context menu', () => {
  it('acts on its target and keeps Escape active after navigation', async () => {
    const wrapper = mount(ContextMenu, { props: {
      target: { kind: 'content', id: 'clicked' }, label: 'Clicked', x: 10, y: 10,
      actions: [{ action: 'rename', label: 'Rename' }, { action: 'delete', label: 'Delete' }]
    }, attachTo: document.body })
    const button = document.querySelector<HTMLButtonElement>('[role="menuitem"]')!
    button.click()
    expect(wrapper.emitted('action')?.[0]).toEqual([{ target: { kind: 'content', id: 'clicked' }, action: 'rename' }])
    window.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowDown' }))
    window.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }))
    expect(wrapper.emitted('close')).toBeTruthy()
    wrapper.unmount()
  })
})
