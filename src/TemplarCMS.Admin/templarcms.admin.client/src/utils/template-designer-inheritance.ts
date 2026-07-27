import type { TemplateFieldViewModel, TemplateSectionViewModel } from '@/types/admin-ui'

type InheritedFieldMatch = {
  section: TemplateSectionViewModel
  field: TemplateFieldViewModel
}

export function findInheritedSectionMatch(
  sectionKey: string,
  inheritedSections: readonly TemplateSectionViewModel[]
): TemplateSectionViewModel | null {
  const normalizedSectionKey = normalizeTemplateDesignerKey(sectionKey)

  if (normalizedSectionKey.length === 0) {
    return null
  }

  return inheritedSections.find(section =>
    normalizeTemplateDesignerKey(section.key) === normalizedSectionKey) ?? null
}

export function findInheritedFieldMatch(
  sectionKey: string,
  fieldKey: string,
  inheritedSections: readonly TemplateSectionViewModel[]
): InheritedFieldMatch | null {
  const section = findInheritedSectionMatch(sectionKey, inheritedSections)

  if (section == null) {
    return null
  }

  const normalizedFieldKey = normalizeTemplateDesignerKey(fieldKey)

  if (normalizedFieldKey.length === 0) {
    return null
  }

  const field = section.fields.find(candidate =>
    normalizeTemplateDesignerKey(candidate.key) === normalizedFieldKey)

  return field == null
    ? null
    : {
        section,
        field
      }
}

function normalizeTemplateDesignerKey(value: string): string {
  return value.trim().toLowerCase()
}
