import type { TreeItem } from '@/types'
import { CONTENT_TREE } from '@/data/tree'

export function flattenTree(nodes: TreeItem[], acc: TreeItem[] = []): TreeItem[] {
  for (const n of nodes) {
    acc.push(n)
    if (n.children) flattenTree(n.children, acc)
  }
  return acc
}

export function findItem(id: string, nodes: TreeItem[] = CONTENT_TREE): TreeItem | null {
  for (const n of nodes) {
    if (n.id === id) return n
    if (n.children) {
      const found = findItem(id, n.children)
      if (found) return found
    }
  }
  return null
}

export function buildBreadcrumb(id: string, nodes: TreeItem[] = CONTENT_TREE, path: TreeItem[] = []): TreeItem[] {
  for (const n of nodes) {
    if (n.id === id) return [...path, n]
    if (n.children) {
      const result = buildBreadcrumb(id, n.children, [...path, n])
      if (result.length) return result
    }
  }
  return []
}

export function filterTree(nodes: TreeItem[], query: string): TreeItem[] {
  if (!query) return nodes
  const q = query.toLowerCase()
  return nodes.reduce<TreeItem[]>((acc, n) => {
    const match = n.label.toLowerCase().includes(q)
    const filteredChildren = n.children ? filterTree(n.children, query) : []
    if (match || filteredChildren.length) {
      acc.push({ ...n, children: filteredChildren.length ? filteredChildren : n.children })
    }
    return acc
  }, [])
}
