import type { ContentBranchResponse, ContentItemResponse } from '@/types/admin-api'
import type { TreeNode } from '@/types/admin-ui'

export function extractParentIdFromHref(href: string | undefined): string | null {
  if (href == null) {
    return null
  }

  const match = href.match(/\/api\/v1\/content\/([^/?]+)/i)
  return match?.[1] ?? null
}

export function createTreeNode(item: ContentItemResponse): TreeNode {
  return {
    item,
    children: [],
    isExpanded: false,
    isBranchLoaded: false,
    isBranchLoading: false
  }
}

export function findTreeNodeById(nodes: TreeNode[], id: string | null): TreeNode | null {
  if (id == null) {
    return null
  }

  for (const node of nodes) {
    if (node.item.id === id) {
      return node
    }

    const nested = findTreeNodeById(node.children, id)
    if (nested != null) {
      return nested
    }
  }

  return null
}

export function treeNodeMatchesFilter(node: TreeNode, filterText: string): boolean {
  const normalizedFilter = filterText.trim().toLocaleLowerCase()

  if (normalizedFilter.length === 0) {
    return true
  }

  if (
    node.item.name.toLocaleLowerCase().includes(normalizedFilter)
    || node.item.path.toLocaleLowerCase().includes(normalizedFilter)
  ) {
    return true
  }

  return node.children.some(child => treeNodeMatchesFilter(child, normalizedFilter))
}

export function applyBranchToTree(nodes: TreeNode[], branch: ContentBranchResponse): TreeNode[] {
  if (branch.item == null) {
    return branch.embedded.children
      .map(branchChild => {
        const currentNode = findTreeNodeById(nodes, branchChild.id)
        return currentNode == null ? createTreeNode(branchChild) : mergeTreeNode(currentNode, branchChild)
      })
      .sort(compareTreeNodes)
  }

  const parentNode = findTreeNodeById(nodes, branch.item.id)

  if (parentNode == null) {
    return upsertAtRoot(nodes, branch.item)
  }

  parentNode.item = branch.item
  parentNode.isBranchLoaded = true
  parentNode.children = branch.embedded.children
    .map(branchChild => {
      const existingChild = parentNode.children.find(child => child.item.id === branchChild.id)
      return existingChild == null ? createTreeNode(branchChild) : mergeTreeNode(existingChild, branchChild)
    })
    .sort(compareTreeNodes)

  return nodes
}

export function upsertTreeNode(
  nodes: TreeNode[],
  parentId: string | null,
  item: ContentItemResponse
): TreeNode[] {
  if (parentId == null) {
    return upsertAtRoot(nodes, item)
  }

  const parentNode = findTreeNodeById(nodes, parentId)

  if (parentNode == null) {
    return nodes
  }

  const currentNode = parentNode.children.find(child => child.item.id === item.id)

  if (currentNode == null) {
    parentNode.children = [...parentNode.children, createTreeNode(item)].sort(compareTreeNodes)
  } else {
    mergeTreeNode(currentNode, item)
    parentNode.children = [...parentNode.children].sort(compareTreeNodes)
  }

  parentNode.isBranchLoaded = true
  return nodes
}

function upsertAtRoot(nodes: TreeNode[], item: ContentItemResponse): TreeNode[] {
  const currentNode = nodes.find(node => node.item.id === item.id)

  if (currentNode == null) {
    return [...nodes, createTreeNode(item)].sort(compareTreeNodes)
  }

  mergeTreeNode(currentNode, item)
  return [...nodes].sort(compareTreeNodes)
}

function mergeTreeNode(node: TreeNode, item: ContentItemResponse): TreeNode {
  node.item = item
  return node
}

function compareTreeNodes(left: TreeNode, right: TreeNode): number {
  return left.item.path.localeCompare(right.item.path)
}
