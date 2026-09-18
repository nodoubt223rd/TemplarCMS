import { describe, expect, it } from 'vitest'
import { mount } from '@vue/test-utils'
import type { ContentItemResponse, TemplateFieldItemResponse } from '@/types/admin-api'
import { buildEditorFields } from '@/utils/editor-fields'
import type { EditorFieldModel } from '@/types/admin-ui'
import ContentEditor from './ContentEditor.vue'

describe('ContentEditor sections', () => {
  it('keeps a new SEO section below Content and before later template sections', () => {
    const templateField = (key: string, sectionName: string, sectionSortOrder: number): TemplateFieldItemResponse => ({
      id: key, key, name: key, type: 'SingleLineText', isShared: false, isUnversioned: false,
      sectionId: sectionName, sectionKey: sectionName.toLowerCase(), sectionName, sectionSortOrder
    })
    const fieldForm = { title: 'Unsaved title', analytics: 'Existing setting', orphan: 'Legacy value' }
    const fields = buildEditorFields(fieldForm, [
      templateField('analytics', 'Settings', 900),
      templateField('seoTitle', 'SEO', 200),
      templateField('title', 'Content', 100),
      templateField('seoDescription', 'SEO', 200)
    ], new Map())
    const wrapper = mount(ContentEditor, {
      props: { item: createItem(), templateName: 'Page', fields, fieldForm,
        isLoadingFields: false, isSubmitting: false }
    })

    expect(wrapper.findAll('button[aria-expanded]').map(button => button.find('span').text()))
      .toEqual(['Content', 'SEO', 'Settings', 'Fields'])
    expect(fields.map(field => field.key))
      .toEqual(['title', 'seoDescription', 'seoTitle', 'analytics', 'orphan'])
    expect(fields.find(field => field.key === 'title')?.value).toBe('Unsaved title')
    expect(fields.find(field => field.key === 'seoTitle')?.value).toBe('')
    wrapper.unmount()
  })

  it('lets authors open and close each field section independently', async () => {
    const wrapper = mount(ContentEditor, {
      props: {
        item: createItem(),
        templateName: 'Article',
        fields: [createField('title', 'Content'), createField('description', 'SEO')],
        fieldForm: { title: '', description: '' },
        isLoadingFields: false,
        isSubmitting: false
      }
    })

    expect(wrapper.text()).toContain('Title')
    expect(wrapper.text()).toContain('Description')

    const contentSection = wrapper.findAll('button').find(button => button.text().includes('Content'))
    const seoSection = wrapper.findAll('button').find(button => button.text().includes('SEO'))
    expect(contentSection).toBeDefined()
    expect(seoSection).toBeDefined()
    await contentSection!.trigger('click')

    expect(wrapper.text()).not.toContain('Title')
    expect(wrapper.text()).toContain('Description')
    expect(contentSection!.attributes('aria-expanded')).toBe('false')
    expect(seoSection!.attributes('aria-expanded')).toBe('true')

    await contentSection!.trigger('click')

    expect(wrapper.text()).toContain('Title')
    expect(wrapper.text()).toContain('Description')
    expect(contentSection!.attributes('aria-expanded')).toBe('true')
    expect(seoSection!.attributes('aria-expanded')).toBe('true')
  })

  it('puts the Content section before other sections regardless of field key order', () => {
    const wrapper = mount(ContentEditor, {
      props: {
        item: createItem(),
        templateName: 'Article',
        fields: [createField('description', 'SEO'), createField('title', 'Content')],
        fieldForm: { title: '', description: '' },
        isLoadingFields: false,
        isSubmitting: false
      }
    })

    const sectionHeadings = wrapper.findAll('button').filter(button =>
      button.text().endsWith('fields'))

    expect(sectionHeadings.map(button => button.find('span').text())).toEqual(['Content', 'SEO'])
  })
})

function createItem(): ContentItemResponse {
  return {
    id: 'item-1',
    name: 'Home',
    templateId: 'template-1',
    path: '/home',
    language: 'en',
    version: 1,
    fields: {},
    _links: {
      self: { href: '/api/v1/content/item-1' },
      template: { href: '/api/v1/templates/template-1' },
      children: { href: '/api/v1/content/item-1/children' },
      dependencies: { href: '/api/v1/content/item-1/dependencies' },
      'set-values': { href: '/api/v1/content/item-1/values' },
      rename: { href: '/api/v1/content/item-1/rename' },
      move: { href: '/api/v1/content/item-1/move' },
      delete: { href: '/api/v1/content/item-1' },
      branch: { href: '/api/v1/content/item-1/branch' }
    }
  }
}

function createField(key: string, sectionName: string): EditorFieldModel {
  return {
    key,
    label: key === 'title' ? 'Title' : 'Description',
    value: '',
    type: 'Text',
    sectionName,
    scopeLabel: 'Versioned',
    editorKind: 'text',
    inputType: 'text',
    placeholder: null,
    rows: null,
    step: null,
    helpText: null
  }
}
