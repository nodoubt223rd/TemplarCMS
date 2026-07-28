import { describe, expect, it } from 'vitest'
import type { FieldTypeResponse, TemplateResponse, TemplateSectionResponse } from '@/types/admin-api'
import { createFieldTypeLookup } from './editor-fields'
import { buildTemplateWorkspaceViewModel } from './template-workspace'

describe('buildTemplateWorkspaceViewModel', () => {
  const fieldTypes: FieldTypeResponse[] = [
    {
      value: 'SingleLineText',
      label: 'Single-Line Text',
      editorKind: 'text',
      inputType: 'text',
      placeholder: 'Enter text',
      rows: null,
      step: null,
      helpText: null
    },
    {
      value: 'Checkbox',
      label: 'Checkbox',
      editorKind: 'checkbox',
      inputType: 'checkbox',
      placeholder: null,
      rows: null,
      step: null,
      helpText: null
    }
  ]

  const lookup = createFieldTypeLookup(fieldTypes)

  it('returns an empty workspace for a missing template', () => {
    expect(buildTemplateWorkspaceViewModel(null, lookup)).toEqual({
      sections: [],
      fieldCount: 0
    })
  })

  it('sorts sections and fields and maps display labels', () => {
    const template: TemplateResponse = {
      id: 'template-1',
      name: 'Article',
      key: 'article',
      sections: [
        createSection({
          id: 'section-hidden',
          name: 'Advanced',
          key: 'advanced',
          sortOrder: 50,
          metadata: {
            'templar.visibility': 'system'
          },
          fields: [
            createField({
              id: 'field-system',
              name: 'Owner',
              key: '__owner'
            })
          ]
        }),
        createSection({
          id: 'section-b',
          name: 'Meta',
          key: 'meta',
          sortOrder: 200,
          fields: [
            createField({
              id: 'field-z',
              name: 'Visible',
              key: 'visible',
              type: 'Checkbox',
              isShared: true
            }),
            createField({
              id: 'field-hidden',
              name: 'Owner',
              key: '__owner',
              metadata: {
                'templar.visibility': 'system'
              }
            }),
            createField({
              id: 'field-a',
              name: 'Summary',
              key: 'summary',
              type: 'SingleLineText',
              isUnversioned: true
            })
          ]
        }),
        createSection({
          id: 'section-a',
          name: 'Content',
          key: 'content',
          sortOrder: 100,
          fields: [
            createField({
              id: 'field-b',
              name: 'Body',
              key: 'body',
              type: 'SingleLineText'
            })
          ]
        })
      ],
      _links: {
        self: { href: '/api/v1/templates/template-1' },
        fields: { href: '/api/v1/templates/template-1/fields' },
        dependencies: { href: '/api/v1/templates/template-1/dependencies' },
        'create-item': { href: '/api/v1/content' }
      }
    }

    expect(buildTemplateWorkspaceViewModel(template, lookup)).toEqual({
      sections: [
        {
          id: 'section-a',
          name: 'Content',
          key: 'content',
          sortOrder: 100,
          fields: [
            {
              id: 'field-b',
              name: 'Body',
              key: 'body',
              type: 'Single-Line Text',
              scopeLabel: 'Versioned'
            }
          ]
        },
        {
          id: 'section-b',
          name: 'Meta',
          key: 'meta',
          sortOrder: 200,
          fields: [
            {
              id: 'field-a',
              name: 'Summary',
              key: 'summary',
              type: 'Single-Line Text',
              scopeLabel: 'Unversioned'
            },
            {
              id: 'field-z',
              name: 'Visible',
              key: 'visible',
              type: 'Checkbox',
              scopeLabel: 'Shared'
            }
          ]
        }
      ],
      fieldCount: 3
    })
  })
})

function createSection(overrides: Partial<TemplateSectionResponse>): TemplateSectionResponse {
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

function createField(overrides: Partial<TemplateSectionResponse['fields'][number]>): TemplateSectionResponse['fields'][number] {
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
