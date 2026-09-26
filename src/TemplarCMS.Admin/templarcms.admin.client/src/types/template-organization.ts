export type TemplateOrganization = {
  revision: string
  folders: { id: { value: string }; name: string; parentId: { value: string } | null }[]
  templates: { id: string; isProtected: boolean; parentId: string | null }[]
}
