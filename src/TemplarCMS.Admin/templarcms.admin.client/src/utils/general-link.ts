import type { GeneralLinkDraft, GeneralLinkKind } from '@/types/general-link'

const legacyGuidPattern = /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i

export function parseGeneralLinkValue(rawValue: string | null | undefined): GeneralLinkDraft {
  const trimmedValue = rawValue?.trim() ?? ''

  if (trimmedValue.length === 0) {
    return createEmptyGeneralLinkDraft()
  }

  if (legacyGuidPattern.test(trimmedValue)) {
    return {
      kind: 'internal',
      itemId: trimmedValue,
      url: '',
      text: '',
      target: '',
      parseWarning: 'Legacy internal link value detected. Saving will convert it to structured JSON.'
    }
  }

  try {
    const asUrl = new URL(trimmedValue)

    return {
      kind: 'external',
      itemId: '',
      url: asUrl.toString(),
      text: '',
      target: '',
      parseWarning: 'Legacy external link value detected. Saving will convert it to structured JSON.'
    }
  } catch {
    // Fall through to JSON parsing.
  }

  try {
    const parsed = JSON.parse(trimmedValue) as {
      kind?: string
      itemId?: string
      url?: string
      text?: string
      target?: string
    }

    return {
      kind: parsed.kind === 'internal' ? 'internal' : 'external',
      itemId: parsed.itemId ?? '',
      url: parsed.url ?? '',
      text: parsed.text ?? '',
      target: parsed.target ?? '',
      parseWarning: parsed.kind === 'internal' || parsed.kind === 'external'
        ? null
        : 'Stored General Link value is missing a valid kind. Saving will normalize it.'
    }
  } catch {
    return {
      kind: 'external',
      itemId: '',
      url: '',
      text: '',
      target: '',
      parseWarning: 'Stored General Link value could not be parsed. Saving will replace it with the structured editor value.'
    }
  }
}

export function createEmptyGeneralLinkDraft(): GeneralLinkDraft {
  return {
    kind: 'external',
    itemId: '',
    url: '',
    text: '',
    target: '',
    parseWarning: null
  }
}

export function updateGeneralLinkDraft(
  currentValue: string | null | undefined,
  update: Partial<GeneralLinkDraft>
): string {
  const nextDraft = {
    ...parseGeneralLinkValue(currentValue),
    ...update,
    parseWarning: null
  }

  return serializeGeneralLinkDraft(nextDraft)
}

export function serializeGeneralLinkDraft(draft: GeneralLinkDraft): string {
  const isEmpty =
    draft.itemId.trim().length === 0
    && draft.url.trim().length === 0
    && draft.text.trim().length === 0
    && draft.target.trim().length === 0

  if (isEmpty) {
    return ''
  }

  const payload: Record<string, string> = {
    kind: draft.kind
  }

  if (draft.kind === 'internal') {
    payload.itemId = draft.itemId.trim()
  } else {
    payload.url = draft.url.trim()
  }

  if (draft.text.trim().length > 0) {
    payload.text = draft.text.trim()
  }

  if (draft.target.trim().length > 0) {
    payload.target = draft.target.trim()
  }

  return JSON.stringify(payload)
}

export function normalizeGeneralLinkKind(value: string | null | undefined): GeneralLinkKind {
  return value === 'internal' ? 'internal' : 'external'
}
