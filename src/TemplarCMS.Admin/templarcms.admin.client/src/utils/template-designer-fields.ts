import type { FieldTypeResponse } from '@/types/admin-api'
import type { TemplateDraftField } from '@/types/template-designer'

export function applyTemplateDraftFieldUpdate(
  field: TemplateDraftField,
  update: Partial<TemplateDraftField>
): TemplateDraftField {
  const nextField = {
    ...field,
    ...update
  }

  return nextField.isShared
    ? {
        ...nextField,
        isUnversioned: false
      }
    : nextField
}

export function getTemplateDesignerFieldStorageLabel(
  field: Pick<TemplateDraftField, 'isShared' | 'isUnversioned'>
): string {
  if (field.isShared) {
    return 'Shared across languages and versions'
  }

  return field.isUnversioned
    ? 'Language-specific, shared across versions'
    : 'Version-specific per language'
}

export function getTemplateDesignerFieldBehaviorHints(fieldType: FieldTypeResponse): string[] {
  const hints = [`Editor: ${fieldType.editorKind}`]

  switch (fieldType.editorKind) {
    case 'general-link':
      hints.push('Authors can switch between internal content targets and external URLs.')
      break
    case 'checkbox':
      hints.push('Authors toggle this field between true and false.')
      break
    case 'textarea':
      hints.push('Authors get a larger multi-line editor for longer values.')
      break
    case 'date-time':
      hints.push('Authors enter a local date and time value.')
      break
    case 'number':
      hints.push('Authors enter numeric values with browser-level number controls.')
      break
    default:
      hints.push('Authors enter a single text value.')
      break
  }

  if (fieldType.inputType !== 'text') {
    hints.push(`HTML input: ${fieldType.inputType}`)
  }

  if (fieldType.placeholder != null) {
    hints.push(`Suggested placeholder: ${fieldType.placeholder}`)
  }

  if (fieldType.rows != null) {
    hints.push(`Default rows: ${fieldType.rows}`)
  }

  if (fieldType.step != null) {
    hints.push(`Validation step: ${fieldType.step}`)
  }

  return hints
}
