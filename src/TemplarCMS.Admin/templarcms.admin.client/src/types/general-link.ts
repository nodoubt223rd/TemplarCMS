export type GeneralLinkKind = 'internal' | 'external'

export type GeneralLinkDraft = {
  kind: GeneralLinkKind
  itemId: string
  url: string
  text: string
  target: string
  parseWarning: string | null
}
