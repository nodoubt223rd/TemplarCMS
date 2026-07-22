import { describe, expect, it } from 'vitest'
import type { FieldTypeResponse, TemplateFieldItemResponse } from '@/types/admin-api'
import {
  buildEditorFields,
  createFieldTypeLookup,
  getFieldTypeDefinition,
  getFieldTypeLabel
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
