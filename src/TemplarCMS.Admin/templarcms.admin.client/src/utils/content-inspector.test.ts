import { describe, expect, it } from 'vitest'
import type { ContentItemResponse, TemplateSummaryResponse } from '@/types/admin-api'
import type { CreateFormState, MoveFormState, RenameFormState } from '@/types/content-inspector'
import {
  clearFieldFormValues,
  getSuggestedCreateTemplateId,
  getSuggestedTemplateId,
  getTemplateIdByKey,
  getTemplateKeyById,
  resetCreateForm,
  resetInspectorForms,
  syncFieldFormValues,
  syncInspectorFormsFromItem
} from './content-inspector'

describe('content inspector utilities', () => {
  const templates: TemplateSummaryResponse[] = [
    createTemplate({ id: 'folder-id', key: 'folder', name: 'Folder' }),
    createTemplate({ id: 'item-id', key: 'item', name: 'Item' }),
    createTemplate({ id: 'article-id', key: 'article', name: 'Article' })
  ]

  it('suggests the item template when the current item is a folder', () => {
    expect(getSuggestedCreateTemplateId(templates, 'folder-id')).toBe('item-id')
  })

  it('falls back to item, folder, then first template for suggested ids', () => {
    expect(getSuggestedTemplateId(templates)).toBe('item-id')
    expect(getSuggestedTemplateId([templates[0]!])).toBe('folder-id')
  })

  it('gets template ids and keys by lookup', () => {
    expect(getTemplateIdByKey(templates, 'article')).toBe('article-id')
    expect(getTemplateKeyById(templates, 'article-id')).toBe('article')
  })

  it('syncs inspector forms from the selected item', () => {
    const renameForm: RenameFormState = { name: '' }
    const moveForm: MoveFormState = { parentId: '' }
    const createForm: CreateFormState = { name: '', templateId: '', parentId: '' }

    syncInspectorFormsFromItem(
      createItem({
        id: 'article-1',
        name: 'Hello',
        templateId: 'folder-id',
        _links: {
          self: { href: '/api/v1/content/article-1' },
          template: { href: '/api/v1/templates/folder-id' },
          children: { href: '/api/v1/content/article-1/children' },
          dependencies: { href: '/api/v1/content/article-1/dependencies' },
          'set-values': { href: '/api/v1/content/article-1/values' },
          rename: { href: '/api/v1/content/article-1' },
          move: { href: '/api/v1/content/article-1/move' },
          delete: { href: '/api/v1/content/article-1' },
          branch: { href: '/api/v1/content/article-1/branch' },
          parent: { href: '/api/v1/content/parent-1?lang=en&version=1' }
        }
      }),
      renameForm,
      moveForm,
      createForm,
      templates
    )

    expect(renameForm).toEqual({ name: 'Hello' })
    expect(moveForm).toEqual({ parentId: 'parent-1' })
    expect(createForm.parentId).toBe('article-1')
    expect(createForm.templateId).toBe('item-id')
  })

  it('resets create and inspector forms', () => {
    const renameForm: RenameFormState = { name: 'A' }
    const moveForm: MoveFormState = { parentId: 'parent' }
    const createForm: CreateFormState = { name: 'New', templateId: '', parentId: 'root' }

    resetCreateForm(createForm, templates)
    expect(createForm).toEqual({
      name: '',
      templateId: 'item-id',
      parentId: 'root'
    })

    resetInspectorForms(renameForm, moveForm, createForm)
    expect(renameForm).toEqual({ name: '' })
    expect(moveForm).toEqual({ parentId: '' })
    expect(createForm.parentId).toBe('')
  })

  it('syncs and clears field form values', () => {
    const fieldForm: Record<string, string> = {
      stale: 'value'
    }

    syncFieldFormValues(fieldForm, createItem({
      fields: {
        title: 'Hello',
        body: null
      }
    }))

    expect(fieldForm).toEqual({
      title: 'Hello',
      body: ''
    })

    clearFieldFormValues(fieldForm)
    expect(fieldForm).toEqual({})
  })
})

function createTemplate(overrides: Partial<TemplateSummaryResponse>): TemplateSummaryResponse {
  return {
    id: 'template-id',
    name: 'Template',
    key: 'template',
    _links: {
      self: { href: '/api/v1/templates/template-id' },
      fields: { href: '/api/v1/templates/template-id/fields' },
      dependencies: { href: '/api/v1/templates/template-id/dependencies' },
      'create-item': { href: '/api/v1/content' }
    },
    ...overrides
  }
}

function createItem(overrides: Partial<ContentItemResponse>): ContentItemResponse {
  return {
    id: 'item-1',
    name: 'Item',
    templateId: 'article-id',
    path: '/item',
    language: 'en',
    version: 1,
    fields: {},
    _links: {
      self: { href: '/api/v1/content/item-1' },
      template: { href: '/api/v1/templates/article-id' },
      children: { href: '/api/v1/content/item-1/children' },
      dependencies: { href: '/api/v1/content/item-1/dependencies' },
      'set-values': { href: '/api/v1/content/item-1/values' },
      rename: { href: '/api/v1/content/item-1' },
      move: { href: '/api/v1/content/item-1/move' },
      delete: { href: '/api/v1/content/item-1' },
      branch: { href: '/api/v1/content/item-1/branch' },
      parent: null
    },
    ...overrides
  }
}
