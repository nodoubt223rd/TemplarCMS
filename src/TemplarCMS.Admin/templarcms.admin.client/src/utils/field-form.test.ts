import { describe, expect, it } from 'vitest'
import {
  getCheckboxFieldValue,
  normalizeFieldValue,
  normalizeOptionalValue,
  readCheckboxEventValue,
  readInputEventValue,
  setCheckboxFieldValue,
  setFieldFormValue
} from './field-form'

describe('field form helpers', () => {
  it('normalizes optional string values', () => {
    expect(normalizeOptionalValue('  ')).toBeNull()
    expect(normalizeOptionalValue('  parent-1  ')).toBe('parent-1')
  })

  it('normalizes field values', () => {
    expect(normalizeFieldValue('')).toBeNull()
    expect(normalizeFieldValue('title')).toBe('title')
  })

  it('reads and writes checkbox values', () => {
    const fieldForm: Record<string, string> = {}
    setCheckboxFieldValue(fieldForm, 'visible', true)

    expect(fieldForm.visible).toBe('true')
    expect(getCheckboxFieldValue(fieldForm, 'visible')).toBe(true)
  })

  it('writes generic field values', () => {
    const fieldForm: Record<string, string> = {}
    setFieldFormValue(fieldForm, 'count', 2)
    setFieldFormValue(fieldForm, 'empty', null)

    expect(fieldForm).toEqual({
      count: '2',
      empty: ''
    })
  })

  it('reads values from input-like events', () => {
    const textEvent = {
      target: {
        value: 'hello'
      }
    } as unknown as Event

    const checkboxEvent = {
      target: {
        checked: true
      }
    } as unknown as Event

    expect(readInputEventValue(textEvent)).toBe('hello')
    expect(readCheckboxEventValue(checkboxEvent)).toBe(true)
  })
})
