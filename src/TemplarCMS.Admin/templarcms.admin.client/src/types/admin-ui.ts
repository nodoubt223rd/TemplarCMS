import type { ContentItemResponse } from './admin-api'

export type TreeNode = {
  item: ContentItemResponse
  children: TreeNode[]
  isExpanded: boolean
  isBranchLoaded: boolean
  isBranchLoading: boolean
  isWorkspaceRoot?: boolean
}

export type EditorFieldModel = {
  key: string
  label: string
  value: string
  type: string
  sectionName: string
  scopeLabel: string
  editorKind: 'checkbox' | 'date-time' | 'general-link' | 'number' | 'rich-text' | 'select' | 'textarea' | 'text'
  inputType: 'checkbox' | 'datetime-local' | 'number' | 'text'
  placeholder: string | null
  rows: number | null
  step: string | null
  helpText: string | null
  options?: Array<{ value: string; label: string }>
}

export type TemplateFieldViewModel = {
  id: string
  name: string
  key: string
  type: string
  scopeLabel: string
}

export type TemplateSectionViewModel = {
  id: string
  name: string
  key: string
  sortOrder: number
  fields: TemplateFieldViewModel[]
}

export type TemplateWorkspaceViewModel = {
  sections: TemplateSectionViewModel[]
  fieldCount: number
}
