/** Resolves display only; never persist the result as an authored override. */
export function resolveItemIcon(
  override: string | null | undefined,
  effectiveTemplateIcon: string | null | undefined
): string {
  return override ?? effectiveTemplateIcon ?? 'file'
}
