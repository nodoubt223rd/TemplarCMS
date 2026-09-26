import { describe, expect, it } from 'vitest'
import { buildTemplateTree } from './template-tree'
import type { TemplateSummaryResponse } from '@/types/admin-api'
const templates = [{ id: 'page', name: 'Page', key: 'page' }, { id: 'other', name: 'Other', key: 'other' }] as TemplateSummaryResponse[]
describe('template containment', () => {
  it('places templates inside nested folders and leaves legacy definitions at root', () => {
    const tree = buildTemplateTree(templates, { revision: 'one', folders: [
      { id: { value: 'a' }, name: 'Site', parentId: null },
      { id: { value: 'b' }, name: 'Pages', parentId: { value: 'a' } }
    ], templates: [{ id: 'page', parentId: 'b', isProtected: false }] })
    expect(tree[0]?.children[0]?.children[0]?.id).toBe('page')
    expect(tree[1]?.id).toBe('other')
    expect(buildTemplateTree(templates, null)).toHaveLength(2)
  })
})
