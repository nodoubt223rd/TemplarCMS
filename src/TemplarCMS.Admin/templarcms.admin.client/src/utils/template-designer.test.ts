import { describe, expect, it } from 'vitest'
import type { TemplateResponse } from '@/types/admin-api'
import type { TemplateDraftSection } from '@/types/template-designer'
import {
  addTemplateDraftField,
  addTemplateDraftSection,
  buildTemplateDesignerPayload,
  createNewTemplateDesignerState,
  mapTemplateToDesignerState,
  removeTemplateDraftField,
  removeTemplateDraftSection
} from './template-designer'

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
})
