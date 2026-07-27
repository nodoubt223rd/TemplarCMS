import type {
  TemplateDesignerFormState,
  TemplateDraftField,
  TemplateDraftSection
} from '@/types/template-designer'
import { applyTemplateDraftFieldUpdate } from './template-designer-fields'
import { syncTemplateDesignerDraftKey } from './template-designer-keys'

export function updateTemplateDesignerFormName(
  form: TemplateDesignerFormState,
  value: string
): TemplateDesignerFormState {
  return {
    ...form,
    name: value,
    key: syncTemplateDesignerDraftKey(form.key, form.name, value)
  }
}

export function updateTemplateDraftSection(
  sections: readonly TemplateDraftSection[],
  sectionId: string,
  update: Partial<TemplateDraftSection>
): TemplateDraftSection[] {
  return sections.map(section =>
    section.id === sectionId
      ? {
          ...section,
          ...update
        }
      : section)
}

export function updateTemplateDraftSectionName(
  sections: readonly TemplateDraftSection[],
  sectionId: string,
  value: string
): TemplateDraftSection[] {
  const currentSection = sections.find(section => section.id === sectionId)

  if (currentSection == null) {
    return [...sections]
  }

  return updateTemplateDraftSection(sections, sectionId, {
    name: value,
    key: syncTemplateDesignerDraftKey(currentSection.key, currentSection.name, value)
  })
}

export function updateTemplateDraftField(
  sections: readonly TemplateDraftSection[],
  sectionId: string,
  fieldId: string,
  update: Partial<TemplateDraftField>
): TemplateDraftSection[] {
  return sections.map(section =>
    section.id !== sectionId
      ? section
      : {
          ...section,
          fields: section.fields.map(field =>
            field.id === fieldId
              ? applyTemplateDraftFieldUpdate(field, update)
              : field)
        })
}

export function updateTemplateDraftFieldName(
  sections: readonly TemplateDraftSection[],
  sectionId: string,
  fieldId: string,
  value: string
): TemplateDraftSection[] {
  const currentField = sections
    .find(section => section.id === sectionId)
    ?.fields.find(field => field.id === fieldId)

  if (currentField == null) {
    return [...sections]
  }

  return updateTemplateDraftField(sections, sectionId, fieldId, {
    name: value,
    key: syncTemplateDesignerDraftKey(currentField.key, currentField.name, value)
  })
}
