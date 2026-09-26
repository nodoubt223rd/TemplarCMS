import { expect, it } from 'vitest'
import { reconcileOrderDraft } from './reconcile-order-draft'

it('refreshes the selected sibling order while preserving unrelated unsaved fields', () => {
  const draft = { title: 'Unsaved title', __sortorder: '200' }
  reconcileOrderDraft(draft, 'selected', [
    { id: 'moved', fields: { __sortorder: '200' } },
    { id: 'selected', fields: { __sortorder: '100' } }
  ])
  expect(draft).toEqual({ title: 'Unsaved title', __sortorder: '100' })
})

it('leaves drafts outside the reordered sibling group untouched', () => {
  const draft = { title: 'Unsaved title', __sortorder: '200' }
  reconcileOrderDraft(draft, 'selected', [{ id: 'other', fields: { __sortorder: '100' } }])
  expect(draft.__sortorder).toBe('200')
})
