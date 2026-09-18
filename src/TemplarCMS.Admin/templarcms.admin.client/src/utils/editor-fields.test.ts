import { describe, expect, it } from 'vitest'
import type { FieldTypeResponse, TemplateFieldItemResponse } from '@/types/admin-api'
import {
  buildEditorFields,
  createFieldTypeLookup,
  getFieldTypeDefinition,
  getFieldTypeLabel,
  getFieldTypeOptions
} from './editor-fields'

describe('editor field utilities', () => {
  it('keeps every custom section before built-in sections regardless of sort order or stored values', () => {
    const system = { 'templar.visibility': 'system' }
    const fields = [
      createTemplateField({ key: 'analytics', sectionKey: 'appearance', sectionName: 'Appearance', sectionSortOrder: 200, sectionMetadata: system }),
      createTemplateField({ key: 'seoTitle', sectionKey: 'seo', sectionName: 'SEO', sectionSortOrder: 1000 }),
      createTemplateField({ key: 'title', sectionKey: 'content', sectionName: 'Content', sectionMetadata: system }),
      createTemplateField({ key: 'campaign', sectionKey: 'marketing', sectionName: 'Marketing', sectionSortOrder: 1100 }),
      createTemplateField({ key: 'seoDescription', sectionKey: 'seo', sectionName: 'SEO', sectionSortOrder: 1000 }),
      createTemplateField({ key: 'publishAt', sectionKey: 'publishing', sectionName: 'Publishing', sectionSortOrder: 300, sectionMetadata: system })
    ]
    const forms: Record<string, string>[] = [{}, { title: 'Unsaved title', campaign: 'Summer', legacy: 'Old value' }]
    for (const form of forms) {
      const result = buildEditorFields(form, fields, new Map())
      expect(result.slice(0, 6).map(field => field.sectionName))
        .toEqual(['Content', 'SEO', 'SEO', 'Marketing', 'Appearance', 'Publishing'])
      expect(result.filter(field => field.sectionName === 'SEO').map(field => field.key))
        .toEqual(['seoDescription', 'seoTitle'])
    }
  })

  it('shows newly added template fields on existing items without overwriting unsaved values', () => {
    const form = { title: 'Unsaved author text' }
    const fields = [
      createTemplateField({ key: 'title', name: 'Title', sectionName: 'Content' }),
      createTemplateField({ key: 'summary', name: 'Summary', sectionName: 'New section' }),
      createTemplateField({ key: '__internal', metadata: { 'templar.visibility': 'system' } })
    ]
    const result = buildEditorFields(form, fields, new Map())
    expect(result).toEqual([
      expect.objectContaining({ key: 'title', value: 'Unsaved author text' }),
      expect.objectContaining({ key: 'summary', sectionName: 'New section', value: '' })
    ])
    expect(form).toEqual({ title: 'Unsaved author text' })
    expect(buildEditorFields({}, fields, new Map())).toHaveLength(2)
  })
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
      helpText: 'True or false.'
    },
    {
      value: 'GeneralLink',
      label: 'General Link',
      editorKind: 'general-link',
      inputType: 'text',
      placeholder: null,
      rows: null,
      step: null,
      helpText: 'Structured link editor.'
    },
    {
      value: 'DateTime',
      label: 'Date/Time',
      editorKind: 'date-time',
      inputType: 'datetime-local',
      placeholder: null,
      rows: null,
      step: null,
      helpText: 'Use local date and time.'
    },
    {
      value: 'Decimal',
      label: 'Decimal',
      editorKind: 'number',
      inputType: 'number',
      placeholder: '0.00',
      rows: null,
      step: '0.01',
      helpText: 'Decimal numbers are validated by the API.'
    },
    {
      value: 'Json',
      label: 'JSON',
      editorKind: 'textarea',
      inputType: 'text',
      placeholder: '{ }',
      rows: 6,
      step: null,
      helpText: 'JSON is not schema-aware yet.'
    },
    {
      value: 'Droplist',
      label: 'Choice / Droplist',
      editorKind: 'select',
      inputType: 'text',
      placeholder: null,
      rows: null,
      step: null,
      helpText: 'Options are configured in the template field source.'
    }
  ]

  const lookup = createFieldTypeLookup(fieldTypes)

  it('returns a fallback field type definition for unknown types', () => {
    expect(getFieldTypeDefinition('MysteryField', lookup)).toEqual({
      value: 'MysteryField',
      label: 'MysteryField',
      editorKind: 'text',
      inputType: 'text',
      placeholder: 'Enter text',
      rows: null,
      step: null,
      helpText: null
    })
  })

  it('returns the configured field type label', () => {
    expect(getFieldTypeLabel('GeneralLink', lookup)).toBe('General Link')
  })

  it('keeps an unknown selected field type visible in designer options', () => {
    expect(getFieldTypeOptions('LegacyField', fieldTypes)).toEqual([
      {
        value: 'LegacyField',
        label: 'LegacyField',
        editorKind: 'text',
        inputType: 'text',
        placeholder: 'Enter text',
        rows: null,
        step: null,
        helpText: null
      },
      ...fieldTypes
    ])
  })

  it('returns the configured options unchanged when the selected type is supported', () => {
    expect(getFieldTypeOptions('GeneralLink', fieldTypes)).toEqual(fieldTypes)
  })

  it('builds editor field models with template metadata and sorts by section then key', () => {
    const templateFields: TemplateFieldItemResponse[] = [
      createTemplateField({
        key: '__owner',
        name: 'Owner',
        metadata: {
          'templar.visibility': 'system'
        },
        sectionName: 'System'
      }),
      createTemplateField({
        key: 'showInNav',
        name: 'Show In Navigation',
        type: 'Checkbox',
        isShared: true,
        sectionName: 'Settings'
      }),
      createTemplateField({
        key: 'heroLink',
        name: 'Hero Link',
        type: 'GeneralLink',
        isUnversioned: true,
        sectionName: 'Content'
      })
    ]

    const result = buildEditorFields(
      {
        showInNav: 'true',
        heroLink: '{"kind":"external","url":"https://example.com"}',
        __owner: 'sitecore\\admin'
      },
      templateFields,
      lookup
    )

    expect(result).toEqual([
      {
        key: 'heroLink',
        label: 'Hero Link',
        value: '{"kind":"external","url":"https://example.com"}',
        type: 'General Link',
        sectionName: 'Content',
        scopeLabel: 'Unversioned',
        editorKind: 'general-link',
        inputType: 'text',
        placeholder: null,
        rows: null,
        step: null,
        helpText: 'Structured link editor.'
      },
      {
        key: 'showInNav',
        label: 'Show In Navigation',
        value: 'true',
        type: 'Checkbox',
        sectionName: 'Settings',
        scopeLabel: 'Shared',
        editorKind: 'checkbox',
        inputType: 'checkbox',
        placeholder: null,
        rows: null,
        step: null,
        helpText: 'True or false.'
      }
    ])
  })

  it('carries field-type-specific metadata into editor models for distinct editor kinds', () => {
    const templateFields: TemplateFieldItemResponse[] = [
      createTemplateField({
        key: 'publishAt',
        name: 'Publish At',
        type: 'DateTime'
      }),
      createTemplateField({
        key: 'price',
        name: 'Price',
        type: 'Decimal'
      }),
      createTemplateField({
        key: 'schema',
        name: 'Schema',
        type: 'Json'
      })
    ]

    const result = buildEditorFields(
      {
        schema: '{ }',
        price: '19.99',
        publishAt: '2026-07-25T08:30'
      },
      templateFields,
      lookup
    )

    expect(result).toEqual([
      {
        key: 'price',
        label: 'Price',
        value: '19.99',
        type: 'Decimal',
        sectionName: 'Fields',
        scopeLabel: 'Versioned',
        editorKind: 'number',
        inputType: 'number',
        placeholder: '0.00',
        rows: null,
        step: '0.01',
        helpText: 'Decimal numbers are validated by the API.'
      },
      {
        key: 'publishAt',
        label: 'Publish At',
        value: '2026-07-25T08:30',
        type: 'Date/Time',
        sectionName: 'Fields',
        scopeLabel: 'Versioned',
        editorKind: 'date-time',
        inputType: 'datetime-local',
        placeholder: null,
        rows: null,
        step: null,
        helpText: 'Use local date and time.'
      },
      {
        key: 'schema',
        label: 'Schema',
        value: '{ }',
        type: 'JSON',
        sectionName: 'Fields',
        scopeLabel: 'Versioned',
        editorKind: 'textarea',
        inputType: 'text',
        placeholder: '{ }',
        rows: 6,
        step: null,
        helpText: 'JSON is not schema-aware yet.'
      }
    ])
  })

  it('falls back cleanly when a field value has no matching template field', () => {
    const result = buildEditorFields(
      {
        orphanedField: 'abc'
      },
      [],
      lookup
    )

    expect(result).toEqual([
      {
        key: 'orphanedField',
        label: 'orphanedField',
        value: 'abc',
        type: 'Single-Line Text',
        sectionName: 'Fields',
        scopeLabel: 'Unknown scope',
        editorKind: 'text',
        inputType: 'text',
        placeholder: 'Enter text',
        rows: null,
        step: null,
        helpText: null
      }
    ])
  })

  it('parses configured droplist options without adding them to other editor kinds', () => {
    const result = buildEditorFields(
      { status: 'published' },
      [createTemplateField({
        key: 'status',
        name: 'Status',
        type: 'Droplist',
        metadata: {
          'templar.droplist.options': '[{"value":"draft","label":"Draft"},{"value":"published","label":"Published"}]'
        }
      })],
      lookup
    )

    expect(result).toEqual([expect.objectContaining({
      key: 'status',
      editorKind: 'select',
      options: [
        { value: 'draft', label: 'Draft' },
        { value: 'published', label: 'Published' }
      ]
    })])
  })
})

function createTemplateField(overrides: Partial<TemplateFieldItemResponse>): TemplateFieldItemResponse {
  return {
    id: 'field-id',
    name: 'Field',
    key: 'field',
    type: 'SingleLineText',
    isShared: false,
    isUnversioned: false,
    sectionId: 'section-id',
    sectionName: 'Fields',
    sectionKey: 'fields',
    sectionSortOrder: 100,
    ...overrides
  }
}
