import type { FieldTypeResponse } from '@/types/admin-api'
import type {
  TemplateFieldViewModel,
  TemplateSectionViewModel,
  TemplateWorkspaceViewModel
} from '@/types/admin-ui'
import type { TemplateDraftSection } from '@/types/template-designer'
import { createFieldTypeLookup, getFieldTypeLabel } from './editor-fields'
import { getTemplateDesignerFieldScopeLabel } from './template-designer-fields'

export type TemplateDesignerPreviewFieldViewModel = TemplateFieldViewModel & {
  originLabel: 'Inherited' | 'Local' | 'Override'
}

export type TemplateDesignerPreviewSectionViewModel = {
  id: string
  name: string
  key: string
  sortOrder: number
  originLabel: 'Inherited' | 'Local' | 'Override'
  fields: TemplateDesignerPreviewFieldViewModel[]
}

export type TemplateDesignerPreviewWorkspaceViewModel = TemplateWorkspaceViewModel & {
  sections: TemplateDesignerPreviewSectionViewModel[]
}

export function buildTemplateDesignerPreviewWorkspace(
  localSections: readonly TemplateDraftSection[],
  inheritedSections: readonly TemplateSectionViewModel[],
  availableFieldTypes: readonly FieldTypeResponse[]
): TemplateDesignerPreviewWorkspaceViewModel {
  const fieldTypeLookup = createFieldTypeLookup([...availableFieldTypes])
  const sectionsByKey = new Map<string, SectionAccumulator>()

  for (const section of inheritedSections) {
    sectionsByKey.set(normalizeKey(section.key), {
      id: section.id,
      name: section.name,
      key: section.key,
      sortOrder: section.sortOrder,
      originLabel: 'Inherited',
      fieldsByKey: new Map(
        section.fields.map(field => [
          normalizeKey(field.key),
          {
            ...field,
            originLabel: 'Inherited' as const
          }
        ])
      )
    })
  }

  for (const section of localSections) {
    const normalizedSectionKey = normalizeKey(section.key)

    if (!sectionsByKey.has(normalizedSectionKey)) {
      sectionsByKey.set(normalizedSectionKey, {
        id: section.id,
        name: section.name,
        key: section.key,
        sortOrder: section.sortOrder,
        originLabel: 'Local',
        fieldsByKey: new Map()
      })
    } else {
      const existingSection = sectionsByKey.get(normalizedSectionKey)

      if (existingSection != null) {
        existingSection.id = section.id
        existingSection.name = section.name
        existingSection.key = section.key
        existingSection.sortOrder = section.sortOrder
        existingSection.originLabel = 'Override'
      }
    }

    const accumulator = sectionsByKey.get(normalizedSectionKey)

    if (accumulator == null) {
      continue
    }

    for (const field of section.fields) {
      const normalizedFieldKey = normalizeKey(field.key)

      accumulator.fieldsByKey.set(normalizedFieldKey, {
        id: field.id,
        name: field.name,
        key: field.key,
        type: getFieldTypeLabel(field.type, fieldTypeLookup),
        scopeLabel: getTemplateDesignerFieldScopeLabel(field),
        originLabel: accumulator.fieldsByKey.has(normalizedFieldKey) ? 'Override' : 'Local'
      })
    }
  }

  const sections = [...sectionsByKey.values()]
    .sort((left, right) =>
      left.sortOrder - right.sortOrder
      || left.name.localeCompare(right.name)
      || left.key.localeCompare(right.key))
    .map(section => ({
      id: section.id,
      name: section.name,
      key: section.key,
      sortOrder: section.sortOrder,
      originLabel: section.originLabel,
      fields: [...section.fieldsByKey.values()].sort((left, right) =>
        left.name.localeCompare(right.name) || left.key.localeCompare(right.key))
    }))

  return {
    sections,
    fieldCount: sections.reduce((total, section) => total + section.fields.length, 0)
  }
}

type SectionAccumulator = {
  id: string
  name: string
  key: string
  sortOrder: number
  originLabel: 'Inherited' | 'Local' | 'Override'
  fieldsByKey: Map<string, TemplateDesignerPreviewFieldViewModel>
}

function normalizeKey(value: string): string {
  return value.trim().toLowerCase()
}
