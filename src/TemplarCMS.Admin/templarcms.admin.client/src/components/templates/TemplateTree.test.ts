import { mount } from '@vue/test-utils'
import { describe, expect, it } from 'vitest'
import TemplateTree from './TemplateTree.vue'
describe('template tree', () => {
  it('retains folder expansion across catalog refreshes and emits clicked identities', async () => {
    const organization = { revision: '1', folders: [{ id: { value: 'folder' }, name: 'Pages', parentId: null }], templates: [{ id: 'page', parentId: 'folder', isProtected: false }] }
    const wrapper = mount(TemplateTree, { props: { templates: [{ id: 'page', name: 'Page', key: 'page', icon: 'star', _links: {} }] as never, organization, selectedId: 'other', loading: false } })
    expect(wrapper.find('[role=group]').text()).toContain('Page')
    await wrapper.find('[aria-label="Page actions"]').trigger('click')
    expect(wrapper.emitted('menu')?.[0]?.[0]).toMatchObject({ target: { kind: 'template', id: 'page' } })
    expect(wrapper.emitted('select')).toBeUndefined()
    await wrapper.find('[aria-expanded]').trigger('click')
    await wrapper.setProps({ organization: { ...organization, revision: '2' } })
    expect(wrapper.find('[role=group]').exists()).toBe(false)
    wrapper.unmount()
  })
})
