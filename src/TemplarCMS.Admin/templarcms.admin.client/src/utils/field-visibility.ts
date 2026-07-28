import type { TemplateFieldItemResponse, TemplateFieldResponse } from '@/types/admin-api'

const fieldVisibilityMetadataKey = 'templar.visibility'
const systemOwnedFieldVisibility = 'system'

type FieldWithMetadata = {
  metadata?: Readonly<Record<string, string>> | null
}

export function isSystemOwnedField(field: FieldWithMetadata): boolean {
  const visibility = field.metadata?.[fieldVisibilityMetadataKey]

  return visibility?.trim().toLowerCase() === systemOwnedFieldVisibility
}

export function getAuthorVisibleTemplateFields(
  fields: readonly TemplateFieldItemResponse[]
): TemplateFieldItemResponse[] {
  return fields.filter(field => !isSystemOwnedField(field))
}

export function getAuthorVisibleSectionFields<TField extends TemplateFieldResponse>(
  fields: readonly TField[]
): TField[] {
  return fields.filter(field => !isSystemOwnedField(field))
}
