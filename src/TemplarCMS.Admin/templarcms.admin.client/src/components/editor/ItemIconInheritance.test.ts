import { mount } from '@vue/test-utils'
import { describe, expect, it } from 'vitest'
import ContentEditor from './ContentEditor.vue'
import Treenode from '../tree/Treenode.vue'
import { createTreeNode } from '@/utils/content-tree'
import type { ContentItemResponse } from '@/types/admin-api'
describe('live icon inheritance', () => {
  it('updates tree and selected display while preserving values and explicit overrides', async () => {
    const item = { id: 'item', name: 'Item', icon: null, templateId: 'page' } as ContentItemResponse
    const draft = { title: 'Unsaved title' }
    const editor = mount(ContentEditor, { props: { item, templateName: 'Page', templateIcon: 'article', fields: [], fieldForm: draft, isLoadingFields: false, isSubmitting: false } })
    const tree = mount(Treenode, { props: { node: createTreeNode(item), depth: 0, selectedId: 'item', filterText: '', templateIcons: { page: 'article' } } })
    await editor.setProps({ templateIcon: 'star' }); await tree.setProps({ templateIcons: { page: 'star' } })
    expect(editor.find('[data-icon]').attributes('data-icon')).toBe('star')
    expect(tree.find('[data-icon]').attributes('data-icon')).toBe('star')
    const overridden = { ...item, icon: 'article' }
    await editor.setProps({ item: overridden }); await tree.setProps({ node: createTreeNode(overridden) })
    expect(editor.find('[data-icon]').attributes('data-icon')).toBe('article')
    expect(tree.find('[data-icon]').attributes('data-icon')).toBe('article')
    expect(draft.title).toBe('Unsaved title')
    editor.unmount(); tree.unmount()
  })
})
