import type { ContentBranchResponse, ContentItemResponse, ContentMutationResponse } from '@/types/admin-api'
import type { TreeNode } from '@/types/admin-ui'
import { applyBranchToTree, extractParentIdFromHref, findTreeNodeById } from './content-tree'

export async function reconcileTreeAction(
  nodes: TreeNode[], response: ContentMutationResponse,
  loadItem: (id: string) => Promise<ContentItemResponse>,
  loadBranch: (id: string | null) => Promise<ContentBranchResponse>,
  withinWorkspace: (branch: ContentBranchResponse) => boolean
): Promise<TreeNode[]> {
  const cached = new Map<string, TreeNode>()
  const remember = (entries: TreeNode[]) => { for (const node of entries) { cached.set(node.item.id, node); remember(node.children) } }
  remember(nodes)
  const previousPath = cached.get(response.item.id)?.item.path
  const ensureLoaded = async (id: string): Promise<void> => {
    if (findTreeNodeById(nodes, id)) return
    const item = await loadItem(id)
    const parentId = extractParentIdFromHref(item._links.parent?.href)
    if (!parentId) throw new Error('Destination is outside the loaded workspace. Refresh the tree.')
    await ensureLoaded(parentId)
    nodes = applyBranchToTree(nodes, await loadBranch(parentId))
  }
  for (const affected of response.affectedBranches) {
    if (!withinWorkspace(affected.branch)) continue
    if (affected.branch.item) await ensureLoaded(affected.branch.item.id)
    // Mutation defaults can differ from the active editor's language/version.
    nodes = applyBranchToTree(nodes, await loadBranch(affected.branch.item?.id ?? null))
  }
  if (previousPath && previousPath !== response.item.path) for (const node of cached.values()) {
    if (node.item.path.startsWith(previousPath + '/')) node.item = { ...node.item, path: response.item.path + node.item.path.slice(previousPath.length) }
  }
  const restore = (entries: TreeNode[]) => { for (const node of entries) {
    const old = cached.get(node.item.id)
    if (old && old !== node) { node.children = old.children; node.isExpanded = old.isExpanded; node.isBranchLoaded = old.isBranchLoaded }
    restore(node.children)
  } }
  restore(nodes)
  return nodes
}
