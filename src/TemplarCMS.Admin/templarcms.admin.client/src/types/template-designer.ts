export type TemplateDesignerMode = 'create' | 'edit'

export type TemplateDraftField = {
  id: string
  name: string
  key: string
  type: string
  isShared: boolean
  isUnversioned: boolean
}

export type TemplateDraftSection = {
  id: string
  name: string
  key: string
  sortOrder: number
  fields: TemplateDraftField[]
}

export type TemplateDesignerFormState = {
  mode: TemplateDesignerMode
  templateId: string
  name: string
  key: string
  baseTemplateId: string
}

export type TemplateDesignerPayload = {
  name: string
  key: string
  baseTemplateKeys: string[]
  sections: Array<{
    name: string
    key: string
    sortOrder: number
    fields: Array<{
      name: string
      key: string
      type: string
      isShared: boolean
      isUnversioned: boolean
    }>
  }>
}
