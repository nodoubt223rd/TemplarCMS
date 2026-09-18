import type { FieldTypeResponse, TemplateFieldItemResponse } from '@/types/admin-api'
import type { EditorFieldModel } from '@/types/admin-ui'
import { getAuthorVisibleTemplateFields, isSystemOwnedField, isSystemOwnedSection } from './field-visibility'

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

const droplistOptionsMetadataKey = 'templar.droplist.options'

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
  const templateFieldLookup = new Map(visibleTemplateFields.map(field => [field.key, field]))
  const hiddenFieldKeys = new Set(
    templateFields
      .filter(isSystemOwnedField)
      .map(field => field.key)
  )

  // Template definitions include new fields that do not have stored values yet.
  return [...new Set([...Object.keys(fieldForm), ...visibleTemplateFields.map(field => field.key)])]
    .filter(key => !hiddenFieldKeys.has(key))
    .sort((left, right) => {
      const leftField = templateFieldLookup.get(left)
      const rightField = templateFieldLookup.get(right)
      // Keep template sections together; unmatched stored fields follow authored sections.
      if (leftField == null) return rightField == null ? left.localeCompare(right) : 1
      if (rightField == null) return -1
      return getSectionGroup(leftField) - getSectionGroup(rightField) ||
        leftField.sectionSortOrder - rightField.sectionSortOrder ||
        leftField.sectionName.localeCompare(rightField.sectionName) ||
        left.localeCompare(right)
    })
    .map(key => {
      const templateField = templateFieldLookup.get(key)
      const type = templateField?.type ?? 'SingleLineText'
      const editor = getFieldTypeDefinition(type, fieldTypeLookup)

      const options = editor.editorKind === 'select'
        ? getDroplistOptions(templateField?.metadata?.[droplistOptionsMetadataKey])
        : undefined

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
        helpText: editor.helpText,
        ...(options == null ? {} : { options })
      }
    })
}

function getSectionGroup(field: TemplateFieldItemResponse): number {
  if (field.sectionKey.toLowerCase() === 'content') return 0
  return isSystemOwnedSection({ metadata: field.sectionMetadata }) ? 2 : 1
}

function getDroplistOptions(value: string | undefined): Array<{ value: string; label: string }> {
  if (value == null) return []

  try {
    const options: unknown = JSON.parse(value)
    return Array.isArray(options)
      ? options.filter((option): option is { value: string; label: string } =>
        typeof option === 'object' && option !== null &&
        typeof (option as { value?: unknown }).value === 'string' &&
        typeof (option as { label?: unknown }).label === 'string')
      : []
  } catch {
    return []
  }
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
