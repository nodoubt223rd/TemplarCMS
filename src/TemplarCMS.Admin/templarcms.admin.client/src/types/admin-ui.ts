import type { ContentItemResponse } from './admin-api'

export type TreeNode = {
  item: ContentItemResponse
  children: TreeNode[]
  isExpanded: boolean
  isBranchLoaded: boolean
  isBranchLoading: boolean
}

export type EditorFieldModel = {
  key: string
  label: string
  value: string
  type: string
  sectionName: string
  scopeLabel: string
  usesTextarea: boolean
}
