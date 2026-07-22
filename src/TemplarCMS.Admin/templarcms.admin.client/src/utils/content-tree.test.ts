import { describe, expect, it } from 'vitest'
import type { ContentBranchResponse, ContentItemResponse } from '@/types/admin-api'
import type { TreeNode } from '@/types/admin-ui'
import {
  applyBranchToTree,
  createTreeNode,
  extractParentIdFromHref,
  findTreeNodeById,
  upsertTreeNode
} from './content-tree'

describe('content tree utilities', () => {
  it('extracts a parent id from a content link', () => {
    expect(extractParentIdFromHref('/api/v1/content/parent-123?lang=en&version=1')).toBe('parent-123')
  })

  it('returns null when a parent link is missing', () => {
    expect(extractParentIdFromHref(undefined)).toBeNull()
  })

  it('finds nested tree nodes by id', () => {
    const nested = createTreeNode(createItem({ id: 'child-1', path: '/home/child' }))
    const root = createTreeNode(createItem({ id: 'root-1', path: '/home' }))
    root.children = [nested]

    expect(findTreeNodeById([root], 'child-1')).toBe(nested)
  })

  it('applies a root branch and keeps nodes sorted by path', () => {
    const nodes = [createTreeNode(createItem({ id: 'b', path: '/zebra' }))]
    const branch = createBranch(null, [
      createItem({ id: 'b', path: '/aardvark' }),
      createItem({ id: 'a', path: '/middle' })
    ])

    const result = applyBranchToTree(nodes, branch)

    expect(result.map(node => node.item.path)).toEqual(['/aardvark', '/middle'])
  })

  it('applies a child branch onto an existing parent node', () => {
    const root = createTreeNode(createItem({ id: 'root-1', path: '/home' }))
    const nodes = [root]
    const branch = createBranch(
      createItem({ id: 'root-1', path: '/home' }),
      [
        createItem({ id: 'child-b', path: '/home/b' }),
        createItem({ id: 'child-a', path: '/home/a' })
      ]
    )

    const result = applyBranchToTree(nodes, branch)

    expect(result[0]?.children.map(node => node.item.path)).toEqual(['/home/a', '/home/b'])
    expect(result[0]?.isBranchLoaded).toBe(true)
  })

  it('upserts a root item when parent id is null', () => {
    const result = upsertTreeNode(
      [createTreeNode(createItem({ id: 'b', path: '/b' }))],
      null,
      createItem({ id: 'a', path: '/a' })
    )

    expect(result.map(node => node.item.path)).toEqual(['/a', '/b'])
  })

  it('upserts a child item under its parent and marks the branch loaded', () => {
    const root = createTreeNode(createItem({ id: 'root-1', path: '/home' }))
    const result = upsertTreeNode(
      [root],
      'root-1',
      createItem({ id: 'child-1', path: '/home/child' })
    )

    expect(result[0]?.children.map(node => node.item.id)).toEqual(['child-1'])
    expect(result[0]?.isBranchLoaded).toBe(true)
  })
})

function createItem(overrides: Partial<ContentItemResponse>): ContentItemResponse {
  return {
    id: 'item-1',
    name: 'Item',
    templateId: 'template-1',
    path: '/item',
    language: 'en',
    version: 1,
    fields: {},
    _links: {
      self: { href: '/api/v1/content/item-1' },
      template: { href: '/api/v1/templates/template-1' },
      children: { href: '/api/v1/content/item-1/children' },
      dependencies: { href: '/api/v1/content/item-1/dependencies' },
      'set-values': { href: '/api/v1/content/item-1/values' },
      rename: { href: '/api/v1/content/item-1' },
      move: { href: '/api/v1/content/item-1/move' },
      delete: { href: '/api/v1/content/item-1' },
      branch: { href: '/api/v1/content/item-1/branch' },
      parent: null
    },
    ...overrides
  }
}

function createBranch(item: ContentItemResponse | null, children: ContentItemResponse[]): ContentBranchResponse {
  return {
    item,
    _links: {
      self: { href: '/api/v1/content/root/branch' },
      item: item == null ? null : { href: `/api/v1/content/${item.id}` }
    },
    embedded: {
      children
    }
  }
}
