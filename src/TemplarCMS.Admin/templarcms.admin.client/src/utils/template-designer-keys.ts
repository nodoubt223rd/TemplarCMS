export function buildTemplateDesignerKeyDraft(value: string): string {
  return value
    .trim()
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '')
}

export function syncTemplateDesignerDraftKey(
  currentKey: string,
  previousName: string,
  nextName: string
): string {
  const previousDraftKey = buildTemplateDesignerKeyDraft(previousName)

  if (currentKey.trim().length > 0 && currentKey !== previousDraftKey) {
    return currentKey
  }

  return buildTemplateDesignerKeyDraft(nextName)
}
