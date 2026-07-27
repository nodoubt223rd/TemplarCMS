import { describe, expect, it } from 'vitest'
import type { TemplateSectionViewModel } from '@/types/admin-ui'
import {
  findInheritedFieldMatch,
  findInheritedSectionMatch
} from './template-designer-inheritance'

describe('template designer inheritance helpers', () => {
  it('finds inherited sections case-insensitively', () => {
    expect(findInheritedSectionMatch(' seo ', createInheritedSections())?.name).toBe('SEO')
  })

  it('finds inherited field overrides only within a matching inherited section', () => {
    expect(
      findInheritedFieldMatch('seo', 'metaDescription', createInheritedSections())
    ).toEqual({
      section: createInheritedSections()[0],
      field: createInheritedSections()[0]?.fields[0]
    })

    expect(
      findInheritedFieldMatch('content', 'metaDescription', createInheritedSections())
    ).toBeNull()
  })
})

function createInheritedSections(): TemplateSectionViewModel[] {
  return [
    {
      id: 'section-1',
      name: 'SEO',
      key: 'seo',
      sortOrder: 100,
      fields: [
        {
          id: 'field-1',
          name: 'Meta Description',
          key: 'metaDescription',
          type: 'Single-Line Text',
          scopeLabel: 'Versioned'
        }
      ]
    }
  ]
}
