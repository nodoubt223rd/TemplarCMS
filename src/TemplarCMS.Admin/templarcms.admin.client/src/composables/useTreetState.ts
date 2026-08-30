import { ref } from 'vue'
import type { TreeItem, IconKey } from '@/types'
import { CONTENT_TREE } from '@/data/tree'

function deepClone<T>(v: T): T {
  return JSON.parse(JSON.stringify(v))
}

const tree = ref<TreeItem[]>(deepClone(CONTENT_TREE))

type SiblingContext = { siblings: TreeItem[]; index: number; parent: TreeItem | null }

function findContext(id: string, nodes: TreeItem[], parent: TreeItem | null = null): SiblingContext | null {
  for (let i = 0; i < nodes.length; i++) {
    if (nodes[i].id === id) return { siblings: nodes, index: i, parent }
    if (nodes[i].children) {
      const found = findContext(id, nodes[i].children!, nodes[i])
      if (found) return found
    }
  }
  return null
}

function removeById(id: string, nodes: TreeItem[]): TreeItem | null {
  for (let i = 0; i < nodes.length; i++) {
    if (nodes[i].id === id) { return nodes.splice(i, 1)[0] }
    if (nodes[i].children) {
      const found = removeById(id, nodes[i].children!)
      if (found) return found
    }
  }
  return null
}

function findNode(id: string, nodes: TreeItem[] = tree.value): TreeItem | null {
  for (const n of nodes) {
    if (n.id === id) return n
    if (n.children) { const f = findNode(id, n.children); if (f) return f }
  }
  return null
}

function moveUp(id: string) {
  const ctx = findContext(id, tree.value)
  if (!ctx || ctx.index === 0) return
  const { siblings, index } = ctx
  ;[siblings[index - 1], siblings[index]] = [siblings[index], siblings[index - 1]]
}

function moveDown(id: string) {
  const ctx = findContext(id, tree.value)
  if (!ctx || ctx.index >= ctx.siblings.length - 1) return
  const { siblings, index } = ctx
  ;[siblings[index], siblings[index + 1]] = [siblings[index + 1], siblings[index]]
}

function moveToFirst(id: string) {
  const ctx = findContext(id, tree.value)
  if (!ctx || ctx.index === 0) return
  const item = ctx.siblings.splice(ctx.index, 1)[0]
  ctx.siblings.unshift(item)
}

function moveToLast(id: string) {
  const ctx = findContext(id, tree.value)
  if (!ctx || ctx.index === ctx.siblings.length - 1) return
  const item = ctx.siblings.splice(ctx.index, 1)[0]
  ctx.siblings.push(item)
}

function reparentItem(id: string, newParentId: string) {
  if (id === newParentId) return
  const item = removeById(id, tree.value)
  if (!item) return
  const newParent = findNode(newParentId)
  if (!newParent) return
  if (!newParent.children) newParent.children = []
  newParent.children.push(item)
}

let idCounter = 1000

function addItem(parentId: string, label: string, iconKey: IconKey, type: TreeItem['type']) {
  const parent = findNode(parentId)
  if (!parent) return
  if (!parent.children) parent.children = []
  parent.children.push({
    id: `new-${++idCounter}`,
    label,
    iconKey,
    type,
    status: 'draft',
  })
}

export function useTreeState() {
  return { tree, findNode, moveUp, moveDown, moveToFirst, moveToLast, reparentItem, addItem }
}
