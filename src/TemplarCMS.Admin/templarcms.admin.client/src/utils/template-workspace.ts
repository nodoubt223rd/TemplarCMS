import type { FieldTypeResponse, TemplateResponse, TemplateSectionResponse } from '@/types/admin-api'
import type { TemplateWorkspaceViewModel, TemplateSectionViewModel } from '@/types/admin-ui'
import { getFieldTypeLabel } from './editor-fields'
import { getAuthorVisibleSectionFields, getAuthorVisibleTemplateSections } from './field-visibility'

export function buildTemplateWorkspaceViewModel(
  template: TemplateResponse | null,
  fieldTypeLookup: ReadonlyMap<string, FieldTypeResponse>
): TemplateWorkspaceViewModel {
  const sections = getAuthorVisibleTemplateSections(template?.sections ?? [])
    .slice()
    .sort(compareSections)
    .map(section => mapTemplateSectionViewModel(section, fieldTypeLookup))

  return {
    sections,
    fieldCount: sections.reduce((total, section) => total + section.fields.length, 0)
  }
}

function mapTemplateSectionViewModel(
  section: TemplateSectionResponse,
  fieldTypeLookup: ReadonlyMap<string, FieldTypeResponse>
): TemplateSectionViewModel {
  return {
    id: section.id,
    name: section.name,
    key: section.key,
    sortOrder: section.sortOrder,
    fields: getAuthorVisibleSectionFields(section.fields)
      .slice()
      .sort((left, right) => left.name.localeCompare(right.name) || left.key.localeCompare(right.key))
      .map(field => ({
        id: field.id,
        name: field.name,
        key: field.key,
        type: getFieldTypeLabel(field.type, fieldTypeLookup),
        scopeLabel: field.isShared
          ? 'Shared'
          : field.isUnversioned
            ? 'Unversioned'
            : 'Versioned'
      }))
  }
}

function compareSections(left: TemplateSectionResponse, right: TemplateSectionResponse): number {
  return left.sortOrder - right.sortOrder
    || left.name.localeCompare(right.name)
    || left.key.localeCompare(right.key)
}
