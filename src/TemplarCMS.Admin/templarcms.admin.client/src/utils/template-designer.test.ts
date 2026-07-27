import { describe, expect, it } from 'vitest'
import type { TemplateResponse } from '@/types/admin-api'
import type { TemplateSectionViewModel } from '@/types/admin-ui'
import type { TemplateDraftSection } from '@/types/template-designer'
import {
  addTemplateDraftField,
  addTemplateDraftSection,
  buildTemplateDesignerPayload,
  createNewTemplateDesignerState,
  mapTemplateToDesignerState,
  removeTemplateDraftField,
  removeTemplateDraftSection,
  validateTemplateDesignerState
} from './template-designer'
import {
  applyTemplateDraftFieldUpdate,
  getTemplateDesignerFieldBehaviorHints,
  getTemplateDesignerFieldStorageLabel
} from './template-designer-fields'

describe('template designer utilities', () => {
  it('creates a new designer state with one empty section and field', () => {
    const ids = ['section-1', 'field-1']
    const state = createNewTemplateDesignerState(() => ids.shift() ?? 'fallback')

    expect(state).toEqual({
      form: {
        mode: 'create',
        templateId: '',
        name: '',
        key: '',
        baseTemplateId: ''
      },
      sections: [
        {
          id: 'section-1',
          name: '',
          key: '',
          sortOrder: 100,
          fields: [
            {
              id: 'field-1',
              name: '',
              key: '',
              type: 'SingleLineText',
              isShared: false,
              isUnversioned: false
            }
          ]
        }
      ]
    })
  })

  it('maps a template response into editable designer state', () => {
    const template: TemplateResponse = {
      id: 'template-1',
      name: 'Article',
      key: 'article',
      baseTemplate: {
        id: 'base-1',
        name: 'Base',
        key: 'base',
        _links: {
          self: { href: '/api/v1/templates/base-1' }
        }
      },
      sections: [
        {
          id: 'section-1',
          name: 'Content',
          key: 'content',
          sortOrder: 100,
          fields: [
            {
              id: 'field-1',
              name: 'Title',
              key: 'title',
              type: 'SingleLineText',
              isShared: false,
              isUnversioned: true
            }
          ]
        }
      ],
      _links: {
        self: { href: '/api/v1/templates/template-1' },
        fields: { href: '/api/v1/templates/template-1/fields' },
        dependencies: { href: '/api/v1/templates/template-1/dependencies' },
        'create-item': { href: '/api/v1/content' }
      }
    }

    expect(mapTemplateToDesignerState(template)).toEqual({
      form: {
        mode: 'edit',
        templateId: 'template-1',
        name: 'Article',
        key: 'article',
        baseTemplateId: 'base-1'
      },
      sections: [
        {
          id: 'section-1',
          name: 'Content',
          key: 'content',
          sortOrder: 100,
          fields: [
            {
              id: 'field-1',
              name: 'Title',
              key: 'title',
              type: 'SingleLineText',
              isShared: false,
              isUnversioned: true
            }
          ]
        }
      ]
    })
  })

  it('adds and removes sections while keeping at least one section', () => {
    const ids = ['section-2', 'field-2', 'section-3', 'field-3']
    const start: TemplateDraftSection[] = [
      {
        id: 'section-1',
        name: '',
        key: '',
        sortOrder: 100,
        fields: [
          {
            id: 'field-1',
            name: '',
            key: '',
            type: 'SingleLineText',
            isShared: false,
            isUnversioned: false
          }
        ]
      }
    ]

    const added = addTemplateDraftSection(start, () => ids.shift() ?? 'fallback')
    expect(added).toHaveLength(2)

    const removed = removeTemplateDraftSection(
      start,
      'section-1',
      () => ids.shift() ?? 'fallback'
    )

    expect(removed).toHaveLength(1)
    expect(removed[0]?.fields).toHaveLength(1)
  })

  it('adds and removes fields while keeping at least one field per section', () => {
    const ids = ['field-2', 'field-3']
    const start: TemplateDraftSection[] = [
      {
        id: 'section-1',
        name: '',
        key: '',
        sortOrder: 100,
        fields: [
          {
            id: 'field-1',
            name: '',
            key: '',
            type: 'SingleLineText',
            isShared: false,
            isUnversioned: false
          }
        ]
      }
    ]

    const added = addTemplateDraftField(start, 'section-1', () => ids.shift() ?? 'fallback')
    expect(added[0]?.fields).toHaveLength(2)

    const removed = removeTemplateDraftField(start, 'section-1', 'field-1', () => ids.shift() ?? 'fallback')
    expect(removed[0]?.fields).toHaveLength(1)
  })

  it('builds a normalized template designer payload', () => {
    const payload = buildTemplateDesignerPayload(
      {
        mode: 'edit',
        templateId: 'template-1',
        name: 'Article',
        key: 'article',
        baseTemplateId: 'base-1'
      },
      [
        {
          id: 'section-1',
          name: 'Content',
          key: 'content',
          sortOrder: 100,
          fields: [
            {
              id: 'field-1',
              name: 'Visible',
              key: 'visible',
              type: 'Checkbox',
              isShared: true,
              isUnversioned: true
            }
          ]
        }
      ],
      'base'
    )

    expect(payload).toEqual({
      name: 'Article',
      key: 'article',
      baseTemplateKeys: ['base'],
      sections: [
        {
          name: 'Content',
          key: 'content',
          sortOrder: 100,
          fields: [
            {
              name: 'Visible',
              key: 'visible',
              type: 'Checkbox',
              isShared: true,
              isUnversioned: false
            }
          ]
        }
      ]
    })
  })

  it('normalizes draft field updates when shared storage is enabled', () => {
    expect(
      applyTemplateDraftFieldUpdate(
        {
          id: 'field-1',
          name: 'Visible',
          key: 'visible',
          type: 'Checkbox',
          isShared: false,
          isUnversioned: true
        },
        {
          isShared: true
        }
      )
    ).toEqual({
      id: 'field-1',
      name: 'Visible',
      key: 'visible',
      type: 'Checkbox',
      isShared: true,
      isUnversioned: false
    })
  })

  it('builds template designer field behavior hints from metadata', () => {
    expect(
      getTemplateDesignerFieldBehaviorHints({
        value: 'DecimalNumber',
        label: 'Decimal Number',
        editorKind: 'number',
        inputType: 'number',
        placeholder: '0.00',
        rows: null,
        step: '0.01',
        helpText: 'Decimal numbers are validated by the API.'
      })
    ).toEqual([
      'Editor: number',
      'Authors enter numeric values with browser-level number controls.',
      'HTML input: number',
      'Suggested placeholder: 0.00',
      'Validation step: 0.01'
    ])
  })

  it('describes the effective storage scope for template designer fields', () => {
    expect(
      getTemplateDesignerFieldStorageLabel({
        isShared: true,
        isUnversioned: false
      })
    ).toBe('Shared across languages and versions')

    expect(
      getTemplateDesignerFieldStorageLabel({
        isShared: false,
        isUnversioned: true
      })
    ).toBe('Language-specific, shared across versions')

    expect(
      getTemplateDesignerFieldStorageLabel({
        isShared: false,
        isUnversioned: false
      })
    ).toBe('Version-specific per language')
  })

  it('validates duplicate keys and unresolved base template selections', () => {
    const errors = validateTemplateDesignerState(
      {
        mode: 'create',
        templateId: '',
        name: 'Article',
        key: 'article',
        baseTemplateId: 'missing-base-id'
      },
      [
        {
          id: 'section-1',
          name: 'Hero',
          key: 'hero',
          sortOrder: 100,
          fields: [
            {
              id: 'field-1',
              name: 'Heading',
              key: 'hero',
              type: 'SingleLineText',
              isShared: false,
              isUnversioned: false
            },
            {
              id: 'field-2',
              name: 'Teaser',
              key: 'summary',
              type: 'RichText',
              isShared: false,
              isUnversioned: false
            }
          ]
        },
        {
          id: 'section-2',
          name: 'SEO',
          key: 'hero',
          sortOrder: 200,
          fields: [
            {
              id: 'field-3',
              name: 'Summary',
              key: 'summary',
              type: 'SingleLineText',
              isShared: false,
              isUnversioned: false
            }
          ]
        }
      ],
      ['SingleLineText', 'Checkbox'],
      null
    )

    expect(errors).toEqual([
      'The selected base template could not be resolved.',
      "A section and field both use the key 'hero'.",
      "Field 'Teaser' uses an unknown field type 'RichText'.",
      "Duplicate section key 'hero'.",
      "Duplicate field key 'summary' appears across the template."
    ])
  })

  it('validates collisions against inherited base template sections and fields', () => {
    const errors = validateTemplateDesignerState(
      {
        mode: 'create',
        templateId: '',
        name: 'Landing Page',
        key: 'landing-page',
        baseTemplateId: 'base-id'
      },
      [
        {
          id: 'section-1',
          name: 'SEO Override',
          key: 'seo',
          sortOrder: 100,
          fields: [
            {
              id: 'field-1',
              name: 'Replacement Meta Description',
              key: 'metaDescription',
              type: 'SingleLineText',
              isShared: false,
              isUnversioned: false
            }
          ]
        }
      ],
      ['SingleLineText'],
      'base-page'
    )

    expect(errors).toEqual([])
  })

  it('validates missing authored structure before submit', () => {
    const errors = validateTemplateDesignerState(
      {
        mode: 'create',
        templateId: '',
        name: '   ',
        key: '',
        baseTemplateId: ''
      },
      [
        {
          id: 'section-1',
          name: '',
          key: '',
          sortOrder: 100,
          fields: [
            {
              id: 'field-1',
              name: '',
              key: '',
              type: 'SingleLineText',
              isShared: false,
              isUnversioned: false
            }
          ]
        }
      ],
      ['SingleLineText'],
      null
    )

    expect(errors).toEqual([
      'Template name is required.',
      'Template key is required.',
      'Every section needs a name.',
      'Every section needs a key.',
      'Every field needs a name.',
      'Every field needs a key.'
    ])
  })
})
