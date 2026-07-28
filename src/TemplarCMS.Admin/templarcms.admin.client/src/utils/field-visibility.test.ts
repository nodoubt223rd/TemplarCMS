import { describe, expect, it } from 'vitest'
import type { TemplateFieldItemResponse, TemplateFieldResponse } from '@/types/admin-api'
import {
  getAuthorVisibleSectionFields,
  getAuthorVisibleTemplateFields,
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
