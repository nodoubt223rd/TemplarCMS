import { describe, expect, it } from 'vitest'
import { mount } from '@vue/test-utils'
import type {
  ContentItemDependencyResponse,
  ContentItemResponse
} from '@/types/admin-api'
import type { EditorFieldModel } from '@/types/admin-ui'
import type { GeneralLinkDraft } from '@/types/general-link'
import ContentInspectorPane from './ContentInspectorPane.vue'

describe('ContentInspectorPane general link editor', () => {
  it('renders the external general link editor and emits structured updates', async () => {
    const wrapper = mountComponent({
      getGeneralLinkDraft: () => ({
        kind: 'external',
        itemId: '',
        url: 'https://example.com/docs',
        text: 'Docs',
        target: '_blank',
        parseWarning: null
      })
    })

    expect(wrapper.text()).toContain('General Link')
    expect(wrapper.find('input[placeholder="https://example.com"]').exists()).toBe(true)
    expect(wrapper.find('input[placeholder="Enter content item GUID"]').exists()).toBe(false)

    await wrapper.get('select').setValue('internal')
    await wrapper.get('input[placeholder="https://example.com"]').setValue('https://example.com/updated')
    await wrapper.get('input[placeholder="Optional label"]').setValue('Updated docs')
    await wrapper.get('input[placeholder="_self or _blank"]').setValue('_self')

    expect(wrapper.emitted('generalLinkKindInput')).toEqual([['cta', 'internal']])
    expect(wrapper.emitted('generalLinkUrlInput')).toEqual([['cta', 'https://example.com/updated']])
    expect(wrapper.emitted('generalLinkTextInput')).toEqual([['cta', 'Updated docs']])
    expect(wrapper.emitted('generalLinkTargetInput')).toEqual([['cta', '_self']])
  })

  it('renders the internal general link editor and emits item id updates', async () => {
    const wrapper = mountComponent({
      getGeneralLinkDraft: () => ({
        kind: 'internal',
        itemId: '3f2504e0-4f89-41d3-9a0c-0305e82c3301',
        url: '',
        text: 'Read more',
        target: '',
        parseWarning: null
      })
    })

    expect(wrapper.find('input[placeholder="Enter content item GUID"]').exists()).toBe(true)
    expect(wrapper.find('input[placeholder="https://example.com"]').exists()).toBe(false)

    await wrapper.get('input[placeholder="Enter content item GUID"]').setValue('7e57d004-2b97-0e7a-b45f-5387367791cd')

    expect(wrapper.emitted('generalLinkItemIdInput')).toEqual([
      ['cta', '7e57d004-2b97-0e7a-b45f-5387367791cd']
    ])
  })

  it('shows legacy upgrade guidance for stored general link values', () => {
    const wrapper = mountComponent({
      getGeneralLinkDraft: () => ({
        kind: 'external',
        itemId: '',
        url: 'https://legacy.example.com',
        text: '',
        target: '',
        parseWarning: 'Legacy external link value detected. Saving will convert it to structured JSON.'
      })
    })

    expect(wrapper.text()).toContain(
      'Legacy external link value detected. Saving will convert it to structured JSON.'
    )
  })
})

describe('ContentInspectorPane date and number editors', () => {
  it('renders a date-time field with the expected input type and emits updates', async () => {
    const wrapper = mountComponent({
      editorFields: [
        createEditorField({
          key: 'publishAt',
          label: 'Publish At',
          value: '2026-07-25T08:30',
          type: 'Date/Time',
          editorKind: 'date-time',
          inputType: 'datetime-local',
          placeholder: null,
          helpText: 'Use local date and time; the API still persists a string value.'
        })
      ],
      fieldForm: {
        publishAt: '2026-07-25T08:30'
      }
    })

    const input = wrapper.get('input[type="datetime-local"]')

    expect(input.element.getAttribute('step')).toBeNull()
    expect(wrapper.text()).toContain('Use local date and time; the API still persists a string value.')

    await input.setValue('2026-07-26T09:45')

    expect(wrapper.emitted('fieldInput')).toEqual([
      ['publishAt', '2026-07-26T09:45']
    ])
  })

  it('renders numeric field hints and preserves the configured step', () => {
    const wrapper = mountComponent({
      editorFields: [
        createEditorField({
          key: 'price',
          label: 'Price',
          value: '19.99',
          type: 'Decimal',
          editorKind: 'number',
          inputType: 'number',
          placeholder: '0.00',
          step: '0.01',
          helpText: 'Decimal numbers are validated by the API.'
        })
      ],
      fieldForm: {
        price: '19.99'
      }
    })

    const input = wrapper.get('input[type="number"]')

    expect(input.attributes('step')).toBe('0.01')
    expect(input.attributes('placeholder')).toBe('0.00')
    expect(wrapper.text()).toContain('Decimal numbers are validated by the API.')
  })
})

describe('ContentInspectorPane textarea editors', () => {
  it('renders rich text fields as textareas with configured rows and help text', async () => {
    const wrapper = mountComponent({
      editorFields: [
        createEditorField({
          key: 'body',
          label: 'Body',
          value: '<p>Hello</p>',
          type: 'Rich Text',
          editorKind: 'textarea',
          inputType: 'text',
          placeholder: 'Enter rich text or HTML',
          rows: 6,
          helpText: 'Rich text currently saves as string content.'
        })
      ],
      fieldForm: {
        body: '<p>Hello</p>'
      }
    })

    const textarea = wrapper.get('textarea')

    expect(textarea.attributes('rows')).toBe('6')
    expect(textarea.attributes('placeholder')).toBe('Enter rich text or HTML')
    expect(wrapper.text()).toContain('Rich text currently saves as string content.')

    await textarea.setValue('<p>Updated</p>')

    expect(wrapper.emitted('fieldInput')).toEqual([
      ['body', '<p>Updated</p>']
    ])
  })

  it('renders json fields with distinct textarea affordances', () => {
    const wrapper = mountComponent({
      editorFields: [
        createEditorField({
          key: 'schema',
          label: 'Schema',
          value: '{\n  "title": "Example"\n}',
          type: 'JSON',
          editorKind: 'textarea',
          inputType: 'text',
          placeholder: '{ }',
          rows: 6,
          helpText: 'JSON is not schema-aware yet, but the editor keeps the field distinct.'
        })
      ],
      fieldForm: {
        schema: '{\n  "title": "Example"\n}'
      }
    })

    const textarea = wrapper.get('textarea')

    expect(textarea.attributes('placeholder')).toBe('{ }')
    expect(textarea.attributes('rows')).toBe('6')
    expect(wrapper.text()).toContain(
      'JSON is not schema-aware yet, but the editor keeps the field distinct.'
    )
  })
})

describe('ContentInspectorPane checkbox editor', () => {
  it('renders checkbox fields with their checked state and emits toggle updates', async () => {
    const wrapper = mountComponent({
      editorFields: [
        createEditorField({
          key: 'showInNav',
          label: 'Show In Navigation',
          value: 'true',
          type: 'Checkbox',
          editorKind: 'checkbox',
          inputType: 'checkbox',
          helpText: 'Stored as true or false.'
        })
      ],
      fieldForm: {
        showInNav: 'true'
      },
      getCheckboxValue: key => key === 'showInNav'
    })

    const checkbox = wrapper.get('input[type="checkbox"]')

    expect((checkbox.element as HTMLInputElement).checked).toBe(true)
    expect(wrapper.text()).toContain('Stored as true or false.')
    expect(wrapper.text()).toContain('Enabled')

    await checkbox.setValue(false)

    expect(wrapper.emitted('checkboxInput')).toEqual([
      ['showInNav', false]
    ])
  })
})

function mountComponent(overrides: Partial<ComponentProps> = {}) {
  return mount(ContentInspectorPane, {
    props: {
      selectedItem: createSelectedItem(),
      selectedItemTemplateName: 'Article',
      isLoadingTemplateFields: false,
      editorFields: [createGeneralLinkField()],
      fieldForm: {
        cta: '{"kind":"external","url":"https://example.com"}'
      },
      isSubmitting: false,
      renameName: 'Home',
      moveParentId: '',
      selectedItemDependencies: null,
      isLoadingItemDependencies: false,
      getGeneralLinkDraft: () => createGeneralLinkDraft(),
      getCheckboxValue: () => false,
      ...overrides
    }
  })
}

function createSelectedItem(): ContentItemResponse {
  return {
    id: 'item-1',
    name: 'Home',
    templateId: 'template-1',
    path: '/home',
    language: 'en',
    version: 1,
    fields: {
      cta: '{"kind":"external","url":"https://example.com"}'
    },
    _links: {
      self: { href: '/api/v1/content/item-1' },
      template: { href: '/api/v1/templates/template-1' },
      children: { href: '/api/v1/content/item-1/children' },
      dependencies: { href: '/api/v1/content/item-1/dependencies' },
      'set-values': { href: '/api/v1/content/item-1/values' },
      rename: { href: '/api/v1/content/item-1/rename' },
      move: { href: '/api/v1/content/item-1/move' },
      delete: { href: '/api/v1/content/item-1' },
      branch: { href: '/api/v1/content/item-1/branch' },
      parent: null
    }
  }
}

function createGeneralLinkField(): EditorFieldModel {
  return createEditorField({
    key: 'cta',
    label: 'Call To Action',
    value: '{"kind":"external","url":"https://example.com"}',
    type: 'General Link',
    sectionName: 'Content',
    scopeLabel: 'Versioned',
    editorKind: 'general-link',
    inputType: 'text',
    placeholder: null,
    rows: null,
    step: null,
    helpText: 'Structured link editor.'
  })
}

function createEditorField(overrides: Partial<EditorFieldModel>): EditorFieldModel {
  return {
    key: 'field',
    label: 'Field',
    value: '',
    type: 'Single-Line Text',
    sectionName: 'Content',
    scopeLabel: 'Versioned',
    editorKind: 'text',
    inputType: 'text',
    placeholder: null,
    rows: null,
    step: null,
    helpText: null,
    ...overrides
  }
}

function createGeneralLinkDraft(): GeneralLinkDraft {
  return {
    kind: 'external',
    itemId: '',
    url: 'https://example.com',
    text: '',
    target: '',
    parseWarning: null
  }
}

type ComponentProps = {
  selectedItem: ContentItemResponse | null
  selectedItemTemplateName: string | null
  isLoadingTemplateFields: boolean
  editorFields: EditorFieldModel[]
  fieldForm: Record<string, string>
  isSubmitting: boolean
  renameName: string
  moveParentId: string
  selectedItemDependencies: ContentItemDependencyResponse | null
  isLoadingItemDependencies: boolean
  getGeneralLinkDraft: (key: string) => GeneralLinkDraft
  getCheckboxValue: (key: string) => boolean
}
