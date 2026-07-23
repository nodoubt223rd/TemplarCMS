import type { ContentItemResponse, TemplateSummaryResponse } from '@/types/admin-api'
import type { CreateFormState, MoveFormState, RenameFormState } from '@/types/content-inspector'
import { extractParentIdFromHref } from './content-tree'

export function syncInspectorFormsFromItem(
  item: ContentItemResponse,
  renameForm: RenameFormState,
  moveForm: MoveFormState,
  createForm: CreateFormState,
  availableTemplates: TemplateSummaryResponse[]
): void {
  renameForm.name = item.name
  moveForm.parentId = extractParentIdFromHref(item._links.parent?.href) ?? ''
  createForm.parentId = item.id
  createForm.templateId = getSuggestedCreateTemplateId(availableTemplates, item.templateId)
}

export function resetCreateForm(
  createForm: CreateFormState,
  availableTemplates: TemplateSummaryResponse[]
): void {
  createForm.name = ''
  createForm.templateId = getSuggestedTemplateId(availableTemplates)
}

export function resetInspectorForms(
  renameForm: RenameFormState,
  moveForm: MoveFormState,
  createForm: CreateFormState
): void {
  renameForm.name = ''
  moveForm.parentId = ''
  createForm.parentId = ''
}

export function syncFieldFormValues(
  fieldForm: Record<string, string>,
  item: ContentItemResponse
): void {
  clearFieldFormValues(fieldForm)

  for (const [key, value] of Object.entries(item.fields)) {
    fieldForm[key] = value ?? ''
  }
}

export function clearFieldFormValues(fieldForm: Record<string, string>): void {
  for (const key of Object.keys(fieldForm)) {
    delete fieldForm[key]
  }
}

export function getSuggestedCreateTemplateId(
  availableTemplates: TemplateSummaryResponse[],
  itemTemplateId: string
): string {
  if (availableTemplates.length === 0) {
    return ''
  }

  const itemTemplate = availableTemplates.find(template => template.id === itemTemplateId)

  if (itemTemplate?.key === 'folder') {
    return getTemplateIdByKey(availableTemplates, 'item') ?? itemTemplate.id
  }

  return itemTemplate?.id ?? getSuggestedTemplateId(availableTemplates)
}

export function getSuggestedTemplateId(availableTemplates: TemplateSummaryResponse[]): string {
  return getTemplateIdByKey(availableTemplates, 'item')
    ?? getTemplateIdByKey(availableTemplates, 'folder')
    ?? availableTemplates[0]?.id
    ?? ''
}

export function getTemplateIdByKey(
  availableTemplates: TemplateSummaryResponse[],
  key: string
): string | null {
  return availableTemplates.find(template => template.key === key)?.id ?? null
}

export function getTemplateKeyById(
  availableTemplates: TemplateSummaryResponse[],
  id: string
): string | null {
  return availableTemplates.find(template => template.id === id)?.key ?? null
}
