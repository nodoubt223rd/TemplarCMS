export type LinkResponse = {
  href: string
}

export type ContentItemResponse = {
  id: string
  name: string
  templateId: string
  path: string
  language: string
  version: number
  fields: Record<string, string | null>
  _links: {
    self: LinkResponse
    template: LinkResponse
    children: LinkResponse
    dependencies: LinkResponse
    'set-values': LinkResponse
    rename: LinkResponse
    move: LinkResponse
    delete: LinkResponse
    branch: LinkResponse
    parent?: LinkResponse | null
  }
}

export type ContentBranchResponse = {
  item: ContentItemResponse | null
  _links: {
    self: LinkResponse
    item?: LinkResponse | null
  }
  embedded: {
    children: ContentItemResponse[]
  }
}

export type ContentMutationAffectedBranchResponse = {
  scope: string
  branch: ContentBranchResponse
}

export type ContentMutationResponse = {
  item: ContentItemResponse
  affectedBranches: ContentMutationAffectedBranchResponse[]
}

export type ContentDependencyChildResponse = {
  id: string
  name: string
  path: string
  _links: {
    self: LinkResponse
  }
}

export type ContentItemDependencyResponse = {
  id: string
  path: string
  canDelete: boolean
  summary: {
    directChildCount: number
  }
  embedded: {
    children: ContentDependencyChildResponse[]
  }
  _links: {
    self: LinkResponse
    'content-item': LinkResponse
  }
}

export type FieldTypeResponse = {
  value: string
  label: string
  editorKind: 'checkbox' | 'date-time' | 'general-link' | 'number' | 'textarea' | 'text'
  inputType: 'checkbox' | 'datetime-local' | 'number' | 'text'
  placeholder: string | null
  rows: number | null
  step: string | null
  helpText: string | null
}

export type FieldTypeCollectionResponse = {
  embedded: {
    fieldTypes: FieldTypeResponse[]
  }
  _links: {
    self: LinkResponse
  }
}

export type TemplateSummaryResponse = {
  id: string
  name: string
  key: string
  _links: {
    self: LinkResponse
    fields: LinkResponse
    dependencies: LinkResponse
    'create-item': LinkResponse
  }
}

export type TemplateFieldResponse = {
  id: string
  name: string
  key: string
  type: string
  isShared: boolean
  isUnversioned: boolean
  metadata?: Record<string, string> | null
}

export type TemplateSectionResponse = {
  id: string
  name: string
  key: string
  sortOrder: number
  metadata?: Record<string, string> | null
  fields: TemplateFieldResponse[]
}

export type TemplateResponse = {
  id: string
  name: string
  key: string
  baseTemplate?: {
    id: string
    name: string
    key: string
    _links: {
      self: LinkResponse
    }
  } | null
  sections: TemplateSectionResponse[]
  _links: {
    self: LinkResponse
    fields: LinkResponse
    dependencies: LinkResponse
    'create-item': LinkResponse
  }
}

export type TemplateCollectionResponse = {
  embedded: {
    templates: TemplateSummaryResponse[]
  }
}

export type TemplateFieldItemResponse = {
  id: string
  name: string
  key: string
  type: string
  isShared: boolean
  isUnversioned: boolean
  metadata?: Record<string, string> | null
  sectionId: string
  sectionName: string
  sectionKey: string
  sectionSortOrder: number
}

export type TemplateFieldCollectionResponse = {
  embedded: {
    fields: TemplateFieldItemResponse[]
  }
}

export type TemplateDependencyTemplateItemResponse = {
  id: string
  name: string
  key: string
  _links: {
    self: LinkResponse
  }
}

export type TemplateDependencyContentItemResponse = {
  id: string
  name: string
  path: string
  _links: {
    self: LinkResponse
  }
}

export type TemplateDependencyResponse = {
  templateId: string
  templateKey: string
  canDelete: boolean
  summary: {
    dependentTemplateCount: number
    dependentContentItemCount: number
  }
  embedded: {
    templates: TemplateDependencyTemplateItemResponse[]
    contentItems: TemplateDependencyContentItemResponse[]
  }
  _links: {
    self: LinkResponse
    template: LinkResponse
  }
}
