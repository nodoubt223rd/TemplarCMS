import type { TemplateSummaryResponse } from '@/types/admin-api'
import type { TemplateOrganization } from '@/types/template-organization'
export type TemplateTreeEntry = { id: string; name: string; kind: 'template' | 'template-folder'; icon: string; children: TemplateTreeEntry[] }
export function buildTemplateTree(templates: TemplateSummaryResponse[], organization: TemplateOrganization | null): TemplateTreeEntry[] {
  const roots: TemplateTreeEntry[] = []
  const folders = new Map<string, TemplateTreeEntry>((organization?.folders ?? []).map(folder => [folder.id.value,
    { id: folder.id.value, name: folder.name, kind: 'template-folder', icon: 'folder', children: [] }]))
  for (const folder of organization?.folders ?? []) {
    const node = folders.get(folder.id.value)!
    const parent = folder.parentId ? folders.get(folder.parentId.value) : null
    if (parent) parent.children.push(node)
    else roots.push(node)
  }
  for (const template of templates) {
    const placement = organization?.templates.find(entry => entry.id === template.id)?.parentId
    const parent = placement ? folders.get(placement) : null
    const node: TemplateTreeEntry = { id: template.id, name: template.name, kind: 'template', icon: template.icon ?? 'file', children: [] }
    if (parent) parent.children.push(node)
    else roots.push(node)
  }
  const sort = (nodes: TemplateTreeEntry[]) => {
    nodes.sort((a, b) => (a.kind === b.kind ? 0 : a.kind === 'template-folder' ? -1 : 1) || a.name.localeCompare(b.name) || a.id.localeCompare(b.id))
    for (const node of nodes) sort(node.children)
  }
  sort(roots)
  return roots
}

export function templateFolderPath(organization: TemplateOrganization, id: string): string {
  const names: string[] = []
  const seen = new Set<string>()
  let current: string | null = id
  while (current && !seen.has(current)) {
    seen.add(current)
    const folder = organization.folders.find(entry => entry.id.value === current)
    if (!folder) break
    names.unshift(folder.name)
    current = folder.parentId?.value ?? null
  }
  return 'Templates / ' + names.join(' / ')
}
