import { describe, it, expect } from 'vitest'
import { resolveItemIcon } from './item-icon'
describe('icon inheritance', () => {
  it('follows template changes only when the override is absent', () => {
    expect(resolveItemIcon(null, 'article')).toBe('article')
    expect(resolveItemIcon(null, 'star')).toBe('star')
    expect(resolveItemIcon('article', 'star')).toBe('article')
    expect(resolveItemIcon(undefined, 'star')).toBe('star')
  })
})
