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
