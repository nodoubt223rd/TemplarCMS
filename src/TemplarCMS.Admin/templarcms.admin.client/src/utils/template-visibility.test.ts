import { describe, expect, it } from 'vitest'
import type { TemplateSummaryResponse } from '@/types/admin-api'
import {
  getAuthorVisibleBaseTemplates,
  getAuthorVisibleTemplates,
  getTemplateVisibilityLabel,
  isAuthorVisibleTemplate,
  isSystemOwnedTemplateKey
} from './template-visibility'

describe('template visibility utilities', () => {
  const templates: TemplateSummaryResponse[] = [
    createTemplate({ id: 'standard-id', key: 'standard', name: 'Standard' }),
    createTemplate({ id: 'folder-id', key: 'folder', name: 'Folder' }),
    createTemplate({ id: 'article-id', key: 'article', name: 'Article' })
  ]

  it('treats standard as a system-owned template', () => {
    expect(isSystemOwnedTemplateKey('standard')).toBe(true)
    expect(isSystemOwnedTemplateKey(' Standard ')).toBe(true)
    expect(isAuthorVisibleTemplate(templates[0]!)).toBe(false)
    expect(isAuthorVisibleTemplate(templates[1]!)).toBe(true)
  })

  it('filters author-visible template collections', () => {
    expect(getAuthorVisibleTemplates(templates).map(template => template.key)).toEqual([
      'folder',
      'article'
    ])

    expect(
      getAuthorVisibleBaseTemplates(templates, 'article-id').map(template => template.key)
    ).toEqual(['folder'])
  })

  it('uses a generic label for hidden system templates', () => {
    expect(getTemplateVisibilityLabel(templates, 'article-id')).toBe('article')
    expect(getTemplateVisibilityLabel(templates, 'standard-id')).toBe('system baseline')
    expect(getTemplateVisibilityLabel(templates, '')).toBeNull()
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
