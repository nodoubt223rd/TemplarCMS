import type { FieldTypeResponse, TemplateFieldItemResponse } from '@/types/admin-api'
import type { EditorFieldModel } from '@/types/admin-ui'
import { getAuthorVisibleTemplateFields, isSystemOwnedField } from './field-visibility'

const defaultFieldTypeDefinition: FieldTypeResponse = {
  value: 'SingleLineText',
  label: 'SingleLineText',
  editorKind: 'text',
  inputType: 'text',
  placeholder: 'Enter text',
  rows: null,
  step: null,
  helpText: null
}

export function createFieldTypeLookup(fieldTypes: FieldTypeResponse[]): Map<string, FieldTypeResponse> {
  return new Map(fieldTypes.map(fieldType => [fieldType.value, fieldType]))
}

export function getFieldTypeDefinition(
  fieldType: string,
  fieldTypeLookup: ReadonlyMap<string, FieldTypeResponse>
): FieldTypeResponse {
  return fieldTypeLookup.get(fieldType) ?? {
    ...defaultFieldTypeDefinition,
    value: fieldType,
    label: fieldType
  }
}

export function getFieldTypeLabel(
  fieldType: string,
  fieldTypeLookup: ReadonlyMap<string, FieldTypeResponse>
): string {
  return getFieldTypeDefinition(fieldType, fieldTypeLookup).label
}

export function getFieldTypeOptions(
  selectedFieldType: string,
  fieldTypes: readonly FieldTypeResponse[]
): FieldTypeResponse[] {
  if (fieldTypes.some(fieldType => fieldType.value === selectedFieldType)) {
    return [...fieldTypes]
  }

  return [
    getFieldTypeDefinition(selectedFieldType, createFieldTypeLookup([...fieldTypes])),
    ...fieldTypes
  ]
}

export function buildEditorFields(
  fieldForm: Record<string, string>,
  templateFields: TemplateFieldItemResponse[],
  fieldTypeLookup: ReadonlyMap<string, FieldTypeResponse>
): EditorFieldModel[] {
  const visibleTemplateFields = getAuthorVisibleTemplateFields(templateFields)
  const hiddenFieldKeys = new Set(
    templateFields
      .filter(isSystemOwnedField)
      .map(field => field.key)
  )

  return Object.keys(fieldForm)
    .filter(key => !hiddenFieldKeys.has(key))
    .sort((left, right) => left.localeCompare(right))
    .map(key => {
      const templateField = visibleTemplateFields.find(field => field.key === key)
      const type = templateField?.type ?? 'SingleLineText'
      const editor = getFieldTypeDefinition(type, fieldTypeLookup)

      return {
        key,
        label: templateField?.name ?? key,
        value: fieldForm[key] ?? '',
        type: getFieldTypeLabel(type, fieldTypeLookup),
        sectionName: templateField?.sectionName ?? 'Fields',
        scopeLabel: getScopeLabel(templateField),
        editorKind: editor.editorKind,
        inputType: editor.inputType,
        placeholder: editor.placeholder,
        rows: editor.rows,
        step: editor.step,
        helpText: editor.helpText
      }
    })
}

function getScopeLabel(templateField: TemplateFieldItemResponse | undefined): string {
  if (templateField == null) {
    return 'Unknown scope'
  }

  if (templateField.isShared) {
    return 'Shared'
  }

  return templateField.isUnversioned ? 'Unversioned' : 'Versioned'
}
