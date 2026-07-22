import { describe, expect, it } from 'vitest'
import {
  createEmptyGeneralLinkDraft,
  normalizeGeneralLinkKind,
  parseGeneralLinkValue,
  serializeGeneralLinkDraft,
  updateGeneralLinkDraft
} from './general-link'

describe('parseGeneralLinkValue', () => {
  it('returns an empty external draft for blank values', () => {
    expect(parseGeneralLinkValue('')).toEqual(createEmptyGeneralLinkDraft())
  })

  it('parses legacy internal guid values', () => {
    expect(parseGeneralLinkValue('3f2504e0-4f89-41d3-9a0c-0305e82c3301')).toEqual({
      kind: 'internal',
      itemId: '3f2504e0-4f89-41d3-9a0c-0305e82c3301',
      url: '',
      text: '',
      target: '',
      parseWarning: 'Legacy internal link value detected. Saving will convert it to structured JSON.'
    })
  })

  it('parses legacy external url values', () => {
    expect(parseGeneralLinkValue('https://example.com/docs')).toEqual({
      kind: 'external',
      itemId: '',
      url: 'https://example.com/docs',
      text: '',
      target: '',
      parseWarning: 'Legacy external link value detected. Saving will convert it to structured JSON.'
    })
  })

  it('parses structured json values', () => {
    expect(parseGeneralLinkValue('{"kind":"external","url":"https://example.com","text":"Docs","target":"_blank"}')).toEqual({
      kind: 'external',
      itemId: '',
      url: 'https://example.com',
      text: 'Docs',
      target: '_blank',
      parseWarning: null
    })
  })

  it('keeps working when structured json is missing kind', () => {
    expect(parseGeneralLinkValue('{"url":"https://example.com"}')).toEqual({
      kind: 'external',
      itemId: '',
      url: 'https://example.com',
      text: '',
      target: '',
      parseWarning: 'Stored General Link value is missing a valid kind. Saving will normalize it.'
    })
  })

  it('falls back safely when the stored value is invalid', () => {
    expect(parseGeneralLinkValue('{not-json')).toEqual({
      kind: 'external',
      itemId: '',
      url: '',
      text: '',
      target: '',
      parseWarning: 'Stored General Link value could not be parsed. Saving will replace it with the structured editor value.'
    })
  })
})

describe('serializeGeneralLinkDraft', () => {
  it('returns an empty string when every editable field is blank', () => {
    expect(serializeGeneralLinkDraft(createEmptyGeneralLinkDraft())).toBe('')
  })

  it('serializes internal links with the trimmed item id', () => {
    expect(serializeGeneralLinkDraft({
      kind: 'internal',
      itemId: '  abc  ',
      url: '',
      text: '  Child item  ',
      target: ' _self ',
      parseWarning: null
    })).toBe('{"kind":"internal","itemId":"abc","text":"Child item","target":"_self"}')
  })

  it('serializes external links with the trimmed url', () => {
    expect(serializeGeneralLinkDraft({
      kind: 'external',
      itemId: '',
      url: ' https://example.com/docs ',
      text: '',
      target: '',
      parseWarning: null
    })).toBe('{"kind":"external","url":"https://example.com/docs"}')
  })
})

describe('updateGeneralLinkDraft', () => {
  it('updates a parsed legacy draft and clears its warning', () => {
    expect(updateGeneralLinkDraft(
      '3f2504e0-4f89-41d3-9a0c-0305e82c3301',
      { text: 'Read more' }
    )).toBe('{"kind":"internal","itemId":"3f2504e0-4f89-41d3-9a0c-0305e82c3301","text":"Read more"}')
  })
})

describe('normalizeGeneralLinkKind', () => {
  it('maps unknown values back to external', () => {
    expect(normalizeGeneralLinkKind('mystery')).toBe('external')
  })

  it('preserves internal values', () => {
    expect(normalizeGeneralLinkKind('internal')).toBe('internal')
  })
})
