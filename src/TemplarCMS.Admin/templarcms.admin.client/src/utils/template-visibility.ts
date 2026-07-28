import type { TemplateSummaryResponse } from '@/types/admin-api'

const systemOwnedTemplateKeys = new Set(['standard'])

export function isSystemOwnedTemplateKey(key: string): boolean {
  return systemOwnedTemplateKeys.has(key.trim().toLowerCase())
}

export function isAuthorVisibleTemplate(template: Pick<TemplateSummaryResponse, 'key'>): boolean {
  return !isSystemOwnedTemplateKey(template.key)
}

export function getAuthorVisibleTemplates(
  availableTemplates: readonly TemplateSummaryResponse[]
): TemplateSummaryResponse[] {
  return availableTemplates.filter(isAuthorVisibleTemplate)
}

export function getAuthorVisibleBaseTemplates(
  availableTemplates: readonly TemplateSummaryResponse[],
  currentTemplateId: string
): TemplateSummaryResponse[] {
  return getAuthorVisibleTemplates(availableTemplates)
    .filter(template => template.id !== currentTemplateId)
}

export function getTemplateVisibilityLabel(
  availableTemplates: readonly TemplateSummaryResponse[],
  templateId: string
): string | null {
  if (templateId.trim().length === 0) {
    return null
  }

  const template = availableTemplates.find(candidate => candidate.id === templateId)

  if (template == null) {
    return null
  }

  return isAuthorVisibleTemplate(template)
    ? template.key
    : 'system baseline'
}
