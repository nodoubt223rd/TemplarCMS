import { afterEach, describe, expect, it } from 'vitest'
import { mount } from '@vue/test-utils'
import type { TemplateResponse } from '@/types/admin-api'
import TemplateDesigner from './TemplateDesigner.vue'

const template: TemplateResponse = {
  id: 'page', name: 'Page', key: 'page', baseTemplates: [],
  sections: [{ id: 'section', name: 'Content', key: 'content', sortOrder: 100,
    fields: [{ id: 'title', name: 'Title', key: 'title', type: 'SingleLineText', isShared: false, isUnversioned: false }] }],
  _links: { self: { href: '/templates/page' }, fields: { href: '/templates/page/fields' }, dependencies: { href: '/templates/page/dependencies' }, 'create-item': { href: '/content' } }
}
const wrappers: ReturnType<typeof mount>[] = []
afterEach(() => { wrappers.forEach(w => w.unmount()); wrappers.length = 0 })
function setup() {
  const wrapper = mount(TemplateDesigner, { attachTo: document.body, props: {
    templates: [], selectedTemplateId: 'page', selectedTemplate: template,
    availableFieldTypes: [], isLoading: false, isSubmitting: false
  } })
  wrappers.push(wrapper)
  return wrapper
}

describe('TemplateDesigner input identity', () => {
  it.each(['standard', 'template', 'folder', 'advanced', 'appearance', 'help', 'lifetime', 'publishing', 'statistics', 'tasks', 'version'])('protects the system template %s while leaving Page editable', async (key) => {
    const wrapper = setup()
    expect(wrapper.findAll('button').find(b => b.text() === 'Save')!.attributes('disabled')).toBeUndefined()
    await wrapper.setProps({ selectedTemplate: { ...template, id: key, key } })
    expect(wrapper.text()).toContain('This system template is source-controlled')
    expect(wrapper.findAll('button').find(b => b.text() === 'Save')!.attributes('disabled')).toBeDefined()
  })

  it('retains DOM identity and focus while changing section and field names', async () => {
    const wrapper = setup()
    for (const input of wrapper.findAll('input')) {
      const element = input.element
      element.focus()
      const original = element.value
      for (const suffix of ['a', 'ab', 'abc']) {
        await input.setValue(original + suffix)
        expect(document.activeElement).toBe(element)
        expect(element.isConnected).toBe(true)
      }
    }
  })

  it('keeps a new field stable while typing and excludes editor IDs from the save contract', async () => {
    const wrapper = setup()
    await wrapper.findAll('button').find(b => b.text() === '+ Section')!.trigger('click')
    await wrapper.findAll('button').filter(b => b.text() === '+ Field')[1]!.trigger('click')
    const input = wrapper.findAll('input').at(-1)!
    const element = input.element
    element.focus()
    await input.setValue('Summary')
    expect(document.activeElement).toBe(element)
    await input.setValue('Summary text')
    expect(document.activeElement).toBe(element)
    await wrapper.findAll('button').find(b => b.text() === 'Save')!.trigger('click')
    const payload = wrapper.emitted('saveTemplate')![0]![0]
    expect(JSON.stringify(payload)).not.toContain('editorId')
    expect(JSON.stringify(payload)).not.toContain('baseTemplateIds')
    expect(JSON.stringify(payload)).toContain('summaryText')
  })

  it('does not remount later fields when an earlier field is removed', async () => {
    const wrapper = setup()
    await wrapper.findAll('button').find(b => b.text() === '+ Field')!.trigger('click')
    const surviving = wrapper.findAll('input').at(-1)!.element
    surviving.focus()
    await wrapper.findAll('button').filter(b => b.text() === 'Remove')[1]!.trigger('click')
    expect(wrapper.findAll('input').at(-1)!.element).toBe(surviving)
    expect(document.activeElement).toBe(surviving)
  })
})
