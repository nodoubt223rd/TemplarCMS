import { describe, expect, it } from 'vitest'
import type { ContentItemResponse, ContentBranchResponse } from '@/types/admin-api'
import { createTreeNode, findTreeNodeById } from './content-tree'
import { reconcileTreeAction } from './reconcile-tree-action'

const item = (id: string, path: string, parent: string | null, language = 'fr'): ContentItemResponse => ({ id, name: id, path, language, version: 2, templateId: 'page', fields: { title: language }, _links: {
  parent: parent ? { href: '/api/v1/content/' + parent } : null,
  self: { href: '' }, template: { href: '' }, children: { href: '' }, dependencies: { href: '' },
  'set-values': { href: '' }, rename: { href: '' }, move: { href: '' }, delete: { href: '' }, branch: { href: '' }
} })
const branch = (parent: ContentItemResponse, children: ContentItemResponse[]): ContentBranchResponse => ({ item: parent, embedded: { children }, _links: { self: { href: '' } } })
describe('workspace action reconciliation', () => {
  it('attaches unloaded destinations, preserves descendants and uses contextual branch values', async () => {
    const root = item('root', '/content', null)
    const a = item('a', '/content/a', 'root')
    const b = item('b', '/content/b', 'root')
    const child = item('child', '/content/b/child', 'b')
    const destination = item('destination', '/content/a/destination', 'a')
    const moved = item('b', '/content/a/destination/b', 'destination')
    const rootNode = createTreeNode(root)
    const aNode = createTreeNode(a)
    const bNode = createTreeNode(b)
    bNode.children = [createTreeNode(child)]; bNode.isExpanded = true; bNode.isBranchLoaded = true
    rootNode.children = [aNode, bNode]
    const draft = { title: 'Unsaved A' }
    const branches: Record<string, ContentBranchResponse> = { root: branch(root, [a]), a: branch(a, [destination]), destination: branch(destination, [moved]) }
    const result = await reconcileTreeAction([rootNode], { item: moved, affectedBranches: [
      { scope: 'source', branch: branch(root, [item('a', '/content/a', 'root', 'en')]) },
      { scope: 'destination', branch: branch(destination, [item('b', moved.path, 'destination', 'en')]) }
    ] }, async () => destination, async id => branches[id!]!, () => true)
    expect(result).toHaveLength(1)
    expect(findTreeNodeById(result, 'destination')?.children[0]?.item.id).toBe('b')
    expect(findTreeNodeById(result, 'child')?.item.path).toBe('/content/a/destination/b/child')
    expect(findTreeNodeById(result, 'a')?.item.language).toBe('fr')
    expect(findTreeNodeById(result, 'b')?.isExpanded).toBe(true)
    expect(draft.title).toBe('Unsaved A')
  })
})
