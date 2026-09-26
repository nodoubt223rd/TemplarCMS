import { ref } from 'vue'
import type { ContentItemResponse, ContentMutationResponse, TemplateSummaryResponse } from '@/types/admin-api'
import type { TreeActionRequest, TreeMenuRequest, TreeActionDescriptor } from '@/types/tree-actions'
import { fetchJson, fetchWithNoContent, sendMutation, withContext } from '@/utils/request-helpers'
import { extractParentIdFromHref } from '@/utils/content-tree'
import type { TemplateOrganization as Organization } from '@/types/template-organization'
import { templateFolderPath } from '@/utils/template-tree'

type Order = { parentId: string | null; revision: string; items: { id: string; supported: boolean }[] }
export type ActionValues = { name: string; parentId: string; templateId: string }
export type ActionDialogState = { request: TreeActionRequest; title: string; initialName: string; showName: boolean; destinations?: { id: string; label: string }[]; templates?: TemplateSummaryResponse[]; initialParent?: string }
type Options = {
  templates: () => TemplateSummaryResponse[]
  context: () => { language: string; version: number }
  contentRoot: () => ContentItemResponse
  beforeAction: (request: TreeActionRequest) => boolean
  onContentChanged: (response: ContentMutationResponse) => Promise<void>
  onReordered: (parentId: string | null) => Promise<void>
  onTemplatesChanged: (request: TreeActionRequest, name: string) => Promise<void>
}
const labels: Record<string, string> = { 'new-item': 'New item', 'new-template': 'New template', 'new-folder': 'New folder', rename: 'Rename', move: 'Move to…', delete: 'Delete', 'move-up': 'Move up', 'move-down': 'Move down', 'move-first': 'Move to first', 'move-last': 'Move to last' }
const protectedKeys = new Set(['standard', 'template', 'template-folder', 'folder', 'advanced', 'appearance', 'help', 'lifetime', 'publishing', 'statistics', 'tasks', 'version'])

export function useTreeActions(options: Options) {
  const menu = ref<TreeMenuRequest | null>(null)
  const actions = ref<TreeActionDescriptor[]>([])
  const dialog = ref<ActionDialogState | null>(null)
  const busy = ref(false)
  const error = ref<string | null>(null)
  const treeOrganization = ref<Organization | null>(null)
  let organization: Organization | null = null
  let targetItem: ContentItemResponse | null = null
  let order: Order | null = null
  let opening = 0
  const contextual = (path: string) => { const context = options.context(); return withContext(path, context.language, context.version) }
  async function refreshOrganization() {
    const current = await fetchJson<Organization>('/api/v1/template-organization')
    treeOrganization.value = current
    return current
  }

  async function open(request: TreeMenuRequest) {
    if (busy.value || dialog.value) return
    const sequence = ++opening
    menu.value = null
    error.value = null
    try {
      const descriptors: TreeActionDescriptor[] = []
      const add = (action: TreeActionDescriptor['action'], disabled = false) => descriptors.push({ action, label: labels[action]!, disabled })
      if (request.target.kind === 'content') {
        const item = await fetchJson<ContentItemResponse>(contextual(`/api/v1/content/${request.target.id}`))
        const siblings = await fetchJson<Order>(`/api/v1/content-order${item._links.parent ? '?parentId=' + extractParentIdFromHref(item._links.parent.href) : ''}`)
        if (sequence !== opening) return
        targetItem = item; order = siblings
        add('new-item'); add('new-folder')
        const index = siblings.items.findIndex(candidate => candidate.id === item.id)
        const unsupported = index < 0 || siblings.items.some(candidate => !candidate.supported)
        add('move-up', unsupported || index === 0); add('move-first', unsupported || index === 0)
        add('move-down', unsupported || index === siblings.items.length - 1); add('move-last', unsupported || index === siblings.items.length - 1)
        add('move'); add('rename'); add('delete')
      } else {
        const current = await refreshOrganization()
        if (sequence !== opening) return
        organization = current
        if (request.target.kind === 'templates-root' || request.target.kind === 'template-folder') { add('new-template'); add('new-folder') }
        if (request.target.kind !== 'templates-root') {
          const protectedTemplate = current.templates.find(candidate => candidate.id === request.target.id)?.isProtected === true
          add('rename', protectedTemplate); add('move', protectedTemplate); add('delete', protectedTemplate)
        }
      }
      actions.value = descriptors
      menu.value = request
    } catch (failure) { if (sequence === opening) error.value = failure instanceof Error ? failure.message : String(failure) }
  }

  async function choose(request: TreeActionRequest) {
    if (busy.value || !options.beforeAction(request)) return
    const label = menu.value?.label ?? targetItem?.name ?? 'item'
    menu.value = null
    error.value = null
    if (request.action.startsWith('move-')) {
      busy.value = true
      try {
        const result = await sendMutation<Order>(`/api/v1/content/${request.target.id}/reorder`, { method: 'POST', body: JSON.stringify({ direction: request.action.slice(5), expectedRevision: order?.revision }) })
        await options.onReordered(result.parentId)
      } catch (failure) { error.value = failure instanceof Error ? failure.message : String(failure) }
      finally { busy.value = false }
      return
    }
    busy.value = true
    try {
      let destinations: { id: string; label: string }[] | undefined
      if (request.action === 'move' && request.target.kind === 'content') {
        destinations = []
        const source = targetItem!
        const visit = async (item: ContentItemResponse) => {
          if (item.id === source.id || item.path.startsWith(source.path + '/')) return
          destinations!.push({ id: item.id, label: item.path })
          const children = await fetchJson<{ embedded: { items: ContentItemResponse[] } }>(contextual(`/api/v1/content/${item.id}/children`))
          for (const child of children.embedded.items) await visit(child)
        }
        await visit(options.contentRoot())
      } else if (request.action === 'move') {
        const excluded = new Set<string>(request.target.kind === 'template-folder' ? [request.target.id] : [])
        let changed = true
        while (changed) { changed = false; for (const folder of organization!.folders) if (folder.parentId && excluded.has(folder.parentId.value) && !excluded.has(folder.id.value)) { excluded.add(folder.id.value); changed = true } }
        destinations = [{ id: '', label: 'Templates root' }, ...organization!.folders.filter(folder => !excluded.has(folder.id.value)).map(folder => ({ id: folder.id.value, label: templateFolderPath(organization!, folder.id.value) }))]
      }
      if (request.action === 'delete' && request.target.kind !== 'template-folder') {
        const url = request.target.kind === 'content' ? targetItem!._links.dependencies.href : `/api/v1/templates/${request.target.id}/dependencies`
        const dependencies = await fetchJson<{ canDelete: boolean }>(url)
        if (!dependencies.canDelete) throw new Error('This item has dependencies. Move or remove its children or references before deleting it.')
      }
      dialog.value = { request, title: `${labels[request.action]}${request.action.startsWith('new-') ? ' under ' : ': '}${label}`,
        initialName: request.action === 'rename' ? label : '', showName: request.action !== 'delete' && request.action !== 'move', destinations,
        templates: request.action === 'new-item' ? options.templates().filter(template => !protectedKeys.has(template.key) || template.key === 'folder') : undefined,
        initialParent: request.target.kind === 'content' ? extractParentIdFromHref(targetItem?._links.parent?.href) ?? '' : request.target.kind === 'template-folder' ? organization?.folders.find(folder => folder.id.value === request.target.id)?.parentId?.value ?? '' : organization?.templates.find(template => template.id === request.target.id)?.parentId ?? '' }
    } catch (failure) { error.value = failure instanceof Error ? failure.message : String(failure) }
    finally { busy.value = false }
  }

  async function submit(values: ActionValues) {
    if (busy.value || !dialog.value) return
    busy.value = true; error.value = null
    const { request } = dialog.value
    let saved = false
    try {
      if (request.target.kind === 'content') {
        let url = `/api/v1/content/${request.target.id}/${request.action}`
        let method = 'POST'
        let payload: unknown = { name: values.name }
        if (request.action === 'new-item' || request.action === 'new-folder') {
          url = '/api/v1/content'
          const templateId = request.action === 'new-folder' ? options.templates().find(template => template.key === 'folder')?.id : values.templateId
          if (!templateId) throw new Error('Select a valid template.')
          payload = { name: values.name, parentId: request.target.id, templateId }
        } else if (request.action === 'move') payload = { parentId: values.parentId || null }
        else if (request.action === 'delete') { url = `/api/v1/content/${request.target.id}`; method = 'DELETE'; payload = undefined }
        const response = await sendMutation<ContentMutationResponse>(url, { method, body: payload === undefined ? undefined : JSON.stringify(payload) })
        saved = true
        await options.onContentChanged(response)
      } else {
        const expectedRevision = organization!.revision
        const parentId = request.target.kind === 'template-folder' ? request.target.id : null
        const key = values.name.trim().toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/^-|-$/g, '')
        if (request.action === 'new-template') {
          await sendMutation('/api/v1/templates', { method: 'POST', body: JSON.stringify({ name: values.name, key, sections: [], baseTemplateKeys: ['standard'], parentFolderId: parentId, expectedOrganizationRevision: expectedRevision }) })
        } else if (request.action === 'new-folder') {
          await sendMutation('/api/v1/template-folders', { method: 'POST', body: JSON.stringify({ name: values.name, key, parentId, expectedRevision }) })
        } else {
          const prefix = request.target.kind === 'template-folder' ? 'template-folders' : 'templates'
          if (request.action === 'delete') await fetchWithNoContent(`/api/v1/${prefix}/${request.target.id}${prefix === 'template-folders' ? '?expectedRevision=' + expectedRevision : ''}`, { method: 'DELETE' })
          else await sendMutation(`/api/v1/${prefix}/${request.target.id}/${request.action}`, { method: 'POST', body: JSON.stringify({ name: values.name, parentId: values.parentId || null, expectedRevision }) })
        }
        saved = true
        await options.onTemplatesChanged(request, values.name)
        await refreshOrganization()
      }
      dialog.value = null
    } catch (failure) {
      error.value = failure instanceof Error ? failure.message : String(failure)
      if (saved) {
        dialog.value = null
        error.value = 'The action was saved, but refreshing the workspace failed. Refresh before making another change. ' + error.value
        return
      }
      // Keep input, but acquire a fresh revision so a deliberate retry can succeed.
      if (request.target.kind !== 'content') organization = await fetchJson<Organization>('/api/v1/template-organization').catch(() => organization)
    } finally { busy.value = false }
  }
  return { menu, actions, dialog, busy, error, treeOrganization, refreshOrganization, open, choose, submit }
}
