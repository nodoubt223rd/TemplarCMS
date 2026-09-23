import { afterEach, describe, expect, it, vi } from 'vitest'
import { useTreeActions } from './useTreeActions'
import type { ContentItemResponse, ContentMutationResponse } from '@/types/admin-api'

const item = { id: 'b', name: 'B', path: '/content/b', templateId: 'page', _links: { parent: { href: '/api/v1/content/root' }, dependencies: { href: '/deps/b' } } } as ContentItemResponse
const values = { name: 'New name', parentId: 'destination', templateId: 'page' }
afterEach(() => vi.unstubAllGlobals())
function setup(fail = false) {
  const draft = { selected: 'a', title: 'Unsaved A' }
  const changed = vi.fn(async (_response: ContentMutationResponse) => { /* Selection and draft are owned by the workspace. */ })
  const fetch = vi.fn(async (url: string, init?: RequestInit) => {
    if (init?.method) return { ok: !fail, json: async () => fail ? { detail: 'Conflict: refresh' } : { item, affectedBranches: [] } }
    return { ok: true, json: async () => url.includes('content-order') ? { revision: 'revision', items: [{ id: 'a', supported: true }, { id: 'b', supported: true }] }
      : url === '/deps/b' ? { canDelete: true } : item }
  })
  vi.stubGlobal('fetch', fetch)
  const actions = useTreeActions({ templates: () => [{ id: 'page', key: 'page', name: 'Page' }, { id: 'folder', key: 'folder', name: 'Folder' }] as never,
    context: () => ({ language: 'en', version: 1 }), contentRoot: () => item, beforeAction: () => true,
    onContentChanged: changed, onReordered: vi.fn(), onTemplatesChanged: vi.fn() })
  return { actions, fetch, changed, draft }
}
describe('clicked-target tree actions', () => {
  it.each(['rename', 'delete', 'new-item', 'new-folder'] as const)('uses B for %s while A remains selected with its draft', async action => {
    const { actions, fetch, draft, changed } = setup()
    await actions.open({ target: { kind: 'content', id: 'b' }, label: 'B', x: 0, y: 0 })
    await actions.choose({ target: { kind: 'content', id: 'b' }, action })
    await actions.submit(values)
    const mutation = fetch.mock.calls.find(([, init]) => init?.method)!
    if (action.startsWith('new-')) expect(JSON.parse(mutation[1]!.body as string).parentId).toBe('b')
    else expect(mutation[0]).toContain('/content/b')
    expect(changed).toHaveBeenCalledOnce()
    expect(draft).toEqual({ selected: 'a', title: 'Unsaved A' })
  })
  it('retains its dialog after a server conflict', async () => {
    const { actions, changed } = setup(true)
    await actions.open({ target: { kind: 'content', id: 'b' }, label: 'B', x: 0, y: 0 })
    await actions.choose({ target: { kind: 'content', id: 'b' }, action: 'rename' })
    const dialog = actions.dialog.value
    await actions.submit(values)
    expect(actions.dialog.value).toBe(dialog)
    expect(actions.error.value).toContain('Conflict')
    expect(changed).not.toHaveBeenCalled()
  })
  it('sends the observed sibling revision when reordering B', async () => {
    const { actions, fetch } = setup()
    await actions.open({ target: { kind: 'content', id: 'b' }, label: 'B', x: 0, y: 0 })
    await actions.choose({ target: { kind: 'content', id: 'b' }, action: 'move-first' })
    const mutation = fetch.mock.calls.find(([, init]) => init?.method)!
    expect(mutation[0]).toBe('/api/v1/content/b/reorder')
    expect(JSON.parse(mutation[1]!.body as string)).toEqual({ direction: 'first', expectedRevision: 'revision' })
  })
})
