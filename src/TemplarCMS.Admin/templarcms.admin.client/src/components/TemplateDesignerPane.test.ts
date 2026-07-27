import { describe, expect, it } from 'vitest'
import { mount } from '@vue/test-utils'
import type { FieldTypeResponse, TemplateSummaryResponse } from '@/types/admin-api'
import type { TemplateSectionViewModel } from '@/types/admin-ui'
import type {
  TemplateDesignerFormState,
  TemplateDraftSection
} from '@/types/template-designer'
import TemplateDesignerPane from './TemplateDesignerPane.vue'

describe('TemplateDesignerPane', () => {
  it('shows field type help text for supported field types', () => {
    const wrapper = mountComponent()

    const helpText = wrapper.text()

    expect(helpText).toContain('General links can point to an internal content item or an external URL.')
    expect(helpText).toContain('Editor: general-link')
    expect(helpText).toContain('Storage: Version-specific per language')
    expect(helpText).toContain('Authors can switch between internal content targets and external URLs.')
  })

  it('preserves unsupported field types in the selector with guidance', async () => {
    const wrapper = mountComponent({
      sections: [
        createSection({
          fields: [
            createField({
              type: 'LegacyField'
            })
          ]
        })
      ]
    })

    const fieldTypeSelect = wrapper.findAll('select')[1]

    if (fieldTypeSelect == null) {
      throw new Error('Expected a field type select to be rendered.')
    }

    const options = fieldTypeSelect.findAll('option').map(option => option.text())

    expect(options).toContain('LegacyField')
    expect(wrapper.text()).toContain(
      'This field keeps an existing unsupported type visible so you can preserve it or replace it intentionally.'
    )

    await fieldTypeSelect.setValue('GeneralLink')

    expect(wrapper.emitted('updateFieldType')).toEqual([['section-1', 'field-1', 'GeneralLink']])
  })

  it('disables save and shows validation summary when errors are present', () => {
    const wrapper = mountComponent({
      validationErrors: ['Template key is required.', 'Every field needs a key.']
    })

    const submitButton = wrapper.get('button[type="submit"]')

    expect(submitButton.attributes('disabled')).toBeDefined()
    expect(wrapper.text()).toContain('Fix 2 issues before saving.')
    expect(wrapper.text()).toContain('Template key is required.')
    expect(wrapper.text()).toContain('Every field needs a key.')
  })

  it('shows inherited base template sections when a base template is selected', () => {
    const wrapper = mountComponent({
      form: createForm({
        baseTemplateId: 'base-template-id'
      }),
      baseTemplateKey: 'base-page',
      inheritedTemplateSections: [
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
      inheritedFieldCount: 1
    })

    expect(wrapper.text()).toContain('Inherited Base Template')
    expect(wrapper.text()).toContain('base-page')
    expect(wrapper.text()).toContain('SEO')
    expect(wrapper.text()).toContain('Meta Description')
    expect(wrapper.text()).toContain('1 sections · 1 fields')
  })

  it('shows detailed number editor hints from field type metadata', () => {
    const wrapper = mountComponent({
      sections: [
        createSection({
          fields: [
            createField({
              type: 'DecimalNumber'
            })
          ]
        })
      ],
      availableFieldTypes: [
        ...createFieldTypes(),
        {
          value: 'DecimalNumber',
          label: 'Decimal Number',
          editorKind: 'number',
          inputType: 'number',
          placeholder: '0.00',
          rows: null,
          step: '0.01',
          helpText: 'Decimal numbers are validated by the API.'
        }
      ]
    })

    const text = wrapper.text()

    expect(text).toContain('Authors enter numeric values with browser-level number controls.')
    expect(text).toContain('HTML input: number')
    expect(text).toContain('Suggested placeholder: 0.00')
    expect(text).toContain('Validation step: 0.01')
  })
})

function mountComponent(overrides: Partial<ComponentProps> = {}) {
  return mount(TemplateDesignerPane, {
    props: {
      form: createForm(),
      sections: [createSection()],
      availableBaseTemplates: [],
      availableFieldTypes: createFieldTypes(),
      isLoadingFieldTypes: false,
      isSubmitting: false,
      heading: 'Draft a new template',
      selectedTemplateLoaded: false,
      baseTemplateKey: null,
      validationErrors: [],
      inheritedTemplateSections: [],
      inheritedFieldCount: 0,
      isLoadingBaseTemplatePreview: false,
      baseTemplatePreviewError: null,
      ...overrides
    }
  })
}

function createForm(overrides: Partial<TemplateDesignerFormState> = {}): TemplateDesignerFormState {
  return {
    mode: 'create',
    templateId: '',
    name: 'Article',
    key: 'article',
    baseTemplateId: '',
    ...overrides
  }
}

function createSection(overrides: Partial<TemplateDraftSection> = {}): TemplateDraftSection {
  return {
    id: 'section-1',
    name: 'Content',
    key: 'content',
    sortOrder: 100,
    fields: [createField()],
    ...overrides
  }
}

function createField(overrides: Partial<TemplateDraftSection['fields'][number]> = {}) {
  return {
    id: 'field-1',
    name: 'Hero Link',
    key: 'heroLink',
    type: 'GeneralLink',
    isShared: false,
    isUnversioned: false,
    ...overrides
  }
}

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
    },
    {
      value: 'GeneralLink',
      label: 'General Link',
      editorKind: 'general-link',
      inputType: 'text',
      placeholder: null,
      rows: null,
      step: null,
      helpText: 'General links can point to an internal content item or an external URL.'
    }
  ]
}

type ComponentProps = {
  form: TemplateDesignerFormState
  sections: TemplateDraftSection[]
  availableBaseTemplates: TemplateSummaryResponse[]
  availableFieldTypes: FieldTypeResponse[]
  isLoadingFieldTypes: boolean
  isSubmitting: boolean
  heading: string
  selectedTemplateLoaded: boolean
  baseTemplateKey: string | null
  validationErrors: string[]
  inheritedTemplateSections: TemplateSectionViewModel[]
  inheritedFieldCount: number
  isLoadingBaseTemplatePreview: boolean
  baseTemplatePreviewError: string | null
}
