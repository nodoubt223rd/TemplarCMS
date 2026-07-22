import type { TemplateResponse } from '@/types/admin-api'
import type {
  TemplateDesignerFormState,
  TemplateDesignerPayload,
  TemplateDraftField,
  TemplateDraftSection
} from '@/types/template-designer'

type IdFactory = () => string

export function createTemplateDesignerFormState(): TemplateDesignerFormState {
  return {
    mode: 'create',
    templateId: '',
    name: '',
    key: '',
    baseTemplateId: ''
  }
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

export function createTemplateDraftSection(idFactory: IdFactory = defaultIdFactory): TemplateDraftSection {
  return {
    id: idFactory(),
    name: '',
    key: '',
    sortOrder: 100,
    fields: [createTemplateDraftField(idFactory)]
  }
}

export function createNewTemplateDesignerState(
  idFactory: IdFactory = defaultIdFactory
): { form: TemplateDesignerFormState; sections: TemplateDraftSection[] } {
  return {
    form: createTemplateDesignerFormState(),
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
  return [...sections, createTemplateDraftSection(idFactory)]
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

function defaultIdFactory(): string {
  return crypto.randomUUID()
}
