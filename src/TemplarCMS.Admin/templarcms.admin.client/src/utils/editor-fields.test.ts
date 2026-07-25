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

  it('builds editor field models with template metadata and sorts by key', () => {
    const templateFields: TemplateFieldItemResponse[] = [
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
        heroLink: '{"kind":"external","url":"https://example.com"}'
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
