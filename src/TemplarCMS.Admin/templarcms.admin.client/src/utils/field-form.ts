export function normalizeOptionalValue(value: string): string | null {
  const trimmed = value.trim()
  return trimmed.length === 0 ? null : trimmed
}

export function normalizeFieldValue(value: string): string | null {
  return value.length === 0 ? null : value
}

export function getCheckboxFieldValue(
  fieldForm: Record<string, string>,
  key: string
): boolean {
  return fieldForm[key]?.trim().toLowerCase() === 'true'
}

export function setCheckboxFieldValue(
  fieldForm: Record<string, string>,
  key: string,
  checked: boolean
): void {
  fieldForm[key] = checked ? 'true' : 'false'
}

export function setFieldFormValue(
  fieldForm: Record<string, string>,
  key: string,
  value: string | number | null | undefined
): void {
  fieldForm[key] = value == null ? '' : String(value)
}

export function readInputEventValue(event: Event): string {
  const target = event.target as HTMLInputElement | null
  return target?.value ?? ''
}

export function readCheckboxEventValue(event: Event): boolean {
  const target = event.target as HTMLInputElement | null
  return target?.checked ?? false
}
