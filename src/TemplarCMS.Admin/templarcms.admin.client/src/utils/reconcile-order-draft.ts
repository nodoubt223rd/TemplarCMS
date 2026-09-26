import type { ContentItemResponse } from '@/types/admin-api'

export function reconcileOrderDraft(
  draft: Record<string, string>, selectedId: string | null,
  siblings: Pick<ContentItemResponse, 'id' | 'fields'>[]
) {
  const selected = siblings.find(item => item.id === selectedId)
  // Reordering renumbers siblings as well as the clicked item. Only its managed
  // field is authoritative here; all other values may contain unsaved edits.
  if (selected && Object.prototype.hasOwnProperty.call(draft, '__sortorder')) {
    draft.__sortorder = selected.fields.__sortorder ?? ''
  }
}
