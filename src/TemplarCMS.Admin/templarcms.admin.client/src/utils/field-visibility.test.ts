import { describe, expect, it } from 'vitest'
import type {
  TemplateFieldItemResponse,
  TemplateFieldResponse,
  TemplateSectionResponse
} from '@/types/admin-api'
import {
  getAuthorVisibleTemplateSections,
  getAuthorVisibleSectionFields,
  getAuthorVisibleTemplateFields,
  isSystemOwnedSection,
  isSystemOwnedField
} from './field-visibility'

describe('field visibility utilities', () => {
  it('treats metadata-marked fields as system-owned', () => {
    expect(
      isSystemOwnedField({
        metadata: {
          'templar.visibility': 'system'
        }
      })
    ).toBe(true)

    expect(
      isSystemOwnedField({
        metadata: {
          'templar.visibility': 'author'
        }
      })
    ).toBe(false)
  })

  it('filters hidden fields from template field collections', () => {
    expect(
      getAuthorVisibleTemplateFields([
        createTemplateFieldItem({
          key: '__owner',
          metadata: {
            'templar.visibility': 'system'
          }
        }),
        createTemplateFieldItem({
          key: 'title'
        })
      ]).map(field => field.key)
    ).toEqual(['title'])

    expect(
      getAuthorVisibleSectionFields([
        createTemplateField({
          key: '__owner',
          metadata: {
            'templar.visibility': 'system'
          }
        }),
        createTemplateField({
          key: 'title'
        })
      ]).map(field => field.key)
    ).toEqual(['title'])
  })

  it('treats metadata-marked sections as system-owned', () => {
    expect(
      isSystemOwnedSection({
        metadata: {
          'templar.visibility': 'system'
        }
      })
    ).toBe(true)

    expect(
      isSystemOwnedSection({
        metadata: {
          'templar.visibility': 'author'
        }
      })
    ).toBe(false)
  })

  it('filters hidden sections from template responses', () => {
    expect(
      getAuthorVisibleTemplateSections([
        createTemplateSection({
          key: 'advanced',
          metadata: {
            'templar.visibility': 'system'
          }
        }),
        createTemplateSection({
          key: 'content'
        })
      ]).map(section => section.key)
    ).toEqual(['content'])
  })
})

function createTemplateField(overrides: Partial<TemplateFieldResponse>): TemplateFieldResponse {
  return {
    id: 'field-id',
    name: 'Field',
    key: 'field',
    type: 'SingleLineText',
    isShared: false,
    isUnversioned: false,
    metadata: null,
    ...overrides
  }
}

function createTemplateFieldItem(overrides: Partial<TemplateFieldItemResponse>): TemplateFieldItemResponse {
  return {
    id: 'field-id',
    name: 'Field',
    key: 'field',
    type: 'SingleLineText',
    isShared: false,
    isUnversioned: false,
    metadata: null,
    sectionId: 'section-id',
    sectionName: 'Fields',
    sectionKey: 'fields',
    sectionSortOrder: 100,
    ...overrides
  }
}

function createTemplateSection(overrides: Partial<TemplateSectionResponse>): TemplateSectionResponse {
  return {
    id: 'section-id',
    name: 'Section',
    key: 'section',
    sortOrder: 100,
    metadata: null,
    fields: [],
    ...overrides
  }
}
