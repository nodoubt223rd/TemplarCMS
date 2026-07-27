import { describe, expect, it } from 'vitest'
import type { FieldTypeResponse } from '@/types/admin-api'
import type { TemplateSectionViewModel } from '@/types/admin-ui'
import type { TemplateDraftSection } from '@/types/template-designer'
import { buildTemplateDesignerPreviewWorkspace } from './template-designer-preview'

describe('template designer preview builder', () => {
  it('merges inherited sections with local section and field overrides', () => {
    const preview = buildTemplateDesignerPreviewWorkspace(
      [
        {
          id: 'section-local',
          name: 'SEO Override',
          key: 'seo',
          sortOrder: 100,
          fields: [
            {
              id: 'field-local-1',
              name: 'Meta Description Override',
              key: 'metaDescription',
              type: 'SingleLineText',
              isShared: false,
              isUnversioned: false
            },
            {
              id: 'field-local-2',
              name: 'Canonical Url',
              key: 'canonicalUrl',
              type: 'SingleLineText',
              isShared: true,
              isUnversioned: false
            }
          ]
        }
      ],
      [
        {
          id: 'section-base',
          name: 'SEO',
          key: 'seo',
          sortOrder: 100,
          fields: [
            {
              id: 'field-base',
              name: 'Meta Description',
              key: 'metaDescription',
              type: 'Single-Line Text',
              scopeLabel: 'Versioned'
            }
          ]
        }
      ],
      createFieldTypes()
    )

    expect(preview).toEqual({
      sections: [
        {
          id: 'section-local',
          name: 'SEO Override',
          key: 'seo',
          sortOrder: 100,
          originLabel: 'Override',
          fields: [
            {
              id: 'field-local-2',
              name: 'Canonical Url',
              key: 'canonicalUrl',
              type: 'Single-Line Text',
              scopeLabel: 'Shared',
              originLabel: 'Local'
            },
            {
              id: 'field-local-1',
              name: 'Meta Description Override',
              key: 'metaDescription',
              type: 'Single-Line Text',
              scopeLabel: 'Versioned',
              originLabel: 'Override'
            }
          ]
        }
      ],
      fieldCount: 2
    })
  })
})

function createFieldTypes(): FieldTypeResponse[] {
  return [
    {
      value: 'SingleLineText',
      label: 'Single-Line Text',
      editorKind: 'text',
      inputType: 'text',
      placeholder: 'Enter text',
      rows: null,
      step: null,
      helpText: null
    }
  ]
}
