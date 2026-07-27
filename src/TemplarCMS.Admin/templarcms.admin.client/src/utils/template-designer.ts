import type { TemplateResponse, TemplateSummaryResponse } from '@/types/admin-api'
import type {
  TemplateDesignerFormState,
  TemplateDesignerPayload,
  TemplateDraftField,
  TemplateDraftSection
} from '@/types/template-designer'

type IdFactory = () => string
const defaultSectionSortOrder = 100
const sectionSortOrderStep = 100

export function createTemplateDesignerFormState(): TemplateDesignerFormState {
  return {
    mode: 'create',
    templateId: '',
    name: '',
    key: '',
    baseTemplateId: ''
  }
}

export function getDefaultTemplateDesignerBaseTemplateId(
  availableTemplates: readonly TemplateSummaryResponse[]
): string {
  return availableTemplates.find(template => template.key.toLowerCase() === 'standard')?.id ?? ''
}

export function createTemplateDraftField(idFactory: IdFactory = defaultIdFactory): TemplateDraftField {
  return {
    id: idFactory(),
    name: '',
    key: '',
    type: 'SingleLineText',
    isShared: false,
    isUnversioned: false
  }
}

export function createTemplateDraftSection(
  idFactory: IdFactory = defaultIdFactory,
  sortOrder: number = defaultSectionSortOrder
): TemplateDraftSection {
  return {
    id: idFactory(),
    name: '',
    key: '',
    sortOrder,
    fields: [createTemplateDraftField(idFactory)]
  }
}

export function createNewTemplateDesignerState(
  defaultBaseTemplateId: string = '',
  idFactory: IdFactory = defaultIdFactory
): { form: TemplateDesignerFormState; sections: TemplateDraftSection[] } {
  return {
    form: {
      ...createTemplateDesignerFormState(),
      baseTemplateId: defaultBaseTemplateId
    },
    sections: [createTemplateDraftSection(idFactory)]
  }
}

export function mapTemplateToDesignerState(template: TemplateResponse): {
  form: TemplateDesignerFormState
  sections: TemplateDraftSection[]
} {
  const sections = template.sections.map(section => ({
    id: section.id,
    name: section.name,
    key: section.key,
    sortOrder: section.sortOrder,
    fields: section.fields.map(field => ({
      id: field.id,
      name: field.name,
      key: field.key,
      type: field.type,
      isShared: field.isShared,
      isUnversioned: field.isUnversioned
    }))
  }))

  return {
    form: {
      mode: 'edit',
      templateId: template.id,
      name: template.name,
      key: template.key,
      baseTemplateId: template.baseTemplate?.id ?? ''
    },
    sections: sections.length === 0 ? [createTemplateDraftSection()] : sections
  }
}

export function addTemplateDraftSection(
  sections: TemplateDraftSection[],
  idFactory: IdFactory = defaultIdFactory
): TemplateDraftSection[] {
  return [...sections, createTemplateDraftSection(idFactory, getNextTemplateSectionSortOrder(sections))]
}

export function removeTemplateDraftSection(
  sections: TemplateDraftSection[],
  sectionId: string,
  idFactory: IdFactory = defaultIdFactory
): TemplateDraftSection[] {
  const nextSections = sections.filter(section => section.id !== sectionId)
  return nextSections.length === 0 ? [createTemplateDraftSection(idFactory)] : nextSections
}

export function addTemplateDraftField(
  sections: TemplateDraftSection[],
  sectionId: string,
  idFactory: IdFactory = defaultIdFactory
): TemplateDraftSection[] {
  return sections.map(section =>
    section.id !== sectionId
      ? section
      : {
          ...section,
          fields: [...section.fields, createTemplateDraftField(idFactory)]
        })
}

export function removeTemplateDraftField(
  sections: TemplateDraftSection[],
  sectionId: string,
  fieldId: string,
  idFactory: IdFactory = defaultIdFactory
): TemplateDraftSection[] {
  return sections.map(section => {
    if (section.id !== sectionId) {
      return section
    }

    const remainingFields = section.fields.filter(field => field.id !== fieldId)

    return {
      ...section,
      fields: remainingFields.length === 0 ? [createTemplateDraftField(idFactory)] : remainingFields
    }
  })
}

export function buildTemplateDesignerPayload(
  form: TemplateDesignerFormState,
  sections: TemplateDraftSection[],
  baseTemplateKey: string | null
): TemplateDesignerPayload {
  return {
    name: form.name,
    key: form.key,
    baseTemplateKeys: baseTemplateKey == null ? [] : [baseTemplateKey],
    sections: sections.map(section => ({
      name: section.name,
      key: section.key,
      sortOrder: Number(section.sortOrder),
      fields: section.fields.map(field => ({
        name: field.name,
        key: field.key,
        type: field.type,
        isShared: field.isShared,
        isUnversioned: field.isShared ? false : field.isUnversioned
      }))
    }))
  }
}

export function validateTemplateDesignerState(
  form: TemplateDesignerFormState,
  sections: TemplateDraftSection[],
  availableFieldTypes: readonly string[],
  baseTemplateKey: string | null
): string[] {
  const errors: string[] = []

  if (form.name.trim().length === 0) {
    errors.push('Template name is required.')
  }

  if (form.key.trim().length === 0) {
    errors.push('Template key is required.')
  }

  if (form.baseTemplateId.trim().length > 0 && baseTemplateKey == null) {
    errors.push('The selected base template could not be resolved.')
  }

  if (sections.length === 0) {
    errors.push('At least one section is required.')
    return errors
  }

  const normalizedSectionKeys = new Set<string>()
  const normalizedFieldKeys = new Set<string>()
  const availableFieldTypeSet = new Set(availableFieldTypes)

  for (const section of sections) {
    if (section.name.trim().length === 0) {
      errors.push('Every section needs a name.')
    }

    const normalizedSectionKey = section.key.trim().toLowerCase()

    if (normalizedSectionKey.length === 0) {
      errors.push('Every section needs a key.')
    } else {
      if (normalizedSectionKeys.has(normalizedSectionKey)) {
        errors.push(`Duplicate section key '${section.key.trim()}'.`)
      }

      normalizedSectionKeys.add(normalizedSectionKey)
    }

    if (section.fields.length === 0) {
      errors.push(`Section '${section.name.trim() || section.key.trim() || 'untitled'}' needs at least one field.`)
      continue
    }

    const normalizedSectionFieldKeys = new Set<string>()

    for (const field of section.fields) {
      if (field.name.trim().length === 0) {
        errors.push('Every field needs a name.')
      }

      const normalizedFieldKey = field.key.trim().toLowerCase()

      if (normalizedFieldKey.length === 0) {
        errors.push('Every field needs a key.')
      } else {
        if (normalizedSectionFieldKeys.has(normalizedFieldKey)) {
          errors.push(
            `Section '${section.key.trim() || section.name.trim() || 'untitled'}' has duplicate field key '${field.key.trim()}'.`
          )
        }

        if (normalizedFieldKeys.has(normalizedFieldKey)) {
          errors.push(`Duplicate field key '${field.key.trim()}' appears across the template.`)
        }

        if (normalizedSectionKeys.has(normalizedFieldKey)) {
          errors.push(`A section and field both use the key '${field.key.trim()}'.`)
        }

        normalizedSectionFieldKeys.add(normalizedFieldKey)
        normalizedFieldKeys.add(normalizedFieldKey)
      }

      if (availableFieldTypeSet.size > 0 && !availableFieldTypeSet.has(field.type)) {
        errors.push(`Field '${field.name.trim() || field.key.trim() || 'untitled'}' uses an unknown field type '${field.type}'.`)
      }
    }
  }

  return Array.from(new Set(errors))
}

function defaultIdFactory(): string {
  return crypto.randomUUID()
}

function getNextTemplateSectionSortOrder(sections: readonly TemplateDraftSection[]): number {
  if (sections.length === 0) {
    return defaultSectionSortOrder
  }

  return Math.max(...sections.map(section => section.sortOrder)) + sectionSortOrderStep
}
