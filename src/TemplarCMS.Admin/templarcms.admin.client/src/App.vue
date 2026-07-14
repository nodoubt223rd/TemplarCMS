<script setup lang="ts">
import { computed, defineComponent, onMounted, reactive, ref } from 'vue'
import type { PropType } from 'vue'

type LinkResponse = {
  href: string
}

type ContentItemResponse = {
  id: string
  name: string
  templateId: string
  path: string
  language: string
  version: number
  fields: Record<string, string | null>
  _links: {
    self: LinkResponse
    template: LinkResponse
    children: LinkResponse
    dependencies: LinkResponse
    'set-values': LinkResponse
    rename: LinkResponse
    move: LinkResponse
    branch: LinkResponse
    parent?: LinkResponse | null
  }
}

type ContentBranchResponse = {
  item: ContentItemResponse | null
  _links: {
    self: LinkResponse
    item?: LinkResponse | null
  }
  embedded: {
    children: ContentItemResponse[]
  }
}

type ContentMutationAffectedBranchResponse = {
  scope: string
  branch: ContentBranchResponse
}

type ContentMutationResponse = {
  item: ContentItemResponse
  affectedBranches: ContentMutationAffectedBranchResponse[]
}

type TreeNode = {
  item: ContentItemResponse
  children: TreeNode[]
  isExpanded: boolean
  isBranchLoaded: boolean
  isBranchLoading: boolean
}

const language = ref('en')
const version = ref(1)
const isBootstrapping = ref(false)
const isSubmitting = ref(false)
const pageError = ref<string | null>(null)
const successMessage = ref<string | null>(null)

const rootNodes = ref<TreeNode[]>([])
const selectedItemId = ref<string | null>(null)
const selectedNode = computed(() => findNodeById(rootNodes.value, selectedItemId.value))
const selectedItem = computed(() => selectedNode.value?.item ?? null)

const createForm = reactive({
  name: '',
  key: '',
  templateId: '',
  parentId: ''
})

const renameForm = reactive({
  name: '',
  key: ''
})

const moveForm = reactive({
  parentId: ''
})

const treeCount = computed(() => countNodes(rootNodes.value))

const TreeBranch = defineComponent({
  name: 'TreeBranch',
  props: {
    node: {
      type: Object as PropType<TreeNode>,
      required: true
    },
    selectedItemId: {
      type: String as PropType<string | null>,
      required: true
    }
  },
  emits: ['toggle', 'select'],
  setup(props, { emit }) {
    const isSelected = computed(() => props.selectedItemId === props.node.item.id)

    function onToggle() {
      emit('toggle', props.node)
    }

    function onSelect() {
      emit('select', props.node)
    }

    return {
      isSelected,
      onToggle,
      onSelect
    }
  },
  template: `
    <li class="tree-item">
      <div :class="['tree-row', { 'tree-row--selected': isSelected }]">
        <button class="tree-toggle" type="button" @click="onToggle">
          {{ node.isExpanded ? '−' : '+' }}
        </button>
        <button class="tree-entry" type="button" @click="onSelect">
          <span class="tree-entry__title">{{ node.item.name }}</span>
          <span class="tree-entry__path">{{ node.item.path }}</span>
        </button>
      </div>

      <div v-if="node.isExpanded && node.isBranchLoading" class="tree-status">
        Loading branch...
      </div>

      <ul v-if="node.isExpanded && node.children.length > 0" class="tree-list tree-list--nested">
        <TreeBranch
          v-for="child in node.children"
          :key="child.item.id"
          :node="child"
          :selected-item-id="selectedItemId"
          @toggle="$emit('toggle', $event)"
          @select="$emit('select', $event)"
        />
      </ul>

      <div
        v-else-if="node.isExpanded && node.isBranchLoaded && node.children.length === 0"
        class="tree-status"
      >
        No direct children.
      </div>
    </li>
  `
})

onMounted(async () => {
  await refreshRootBranch()
})

async function refreshRootBranch() {
  isBootstrapping.value = true
  pageError.value = null

  try {
    const branch = await getRootBranch()
    rootNodes.value = branch.embedded.children.map(createNode)

    if (selectedItemId.value != null) {
      const currentNode = findNodeById(rootNodes.value, selectedItemId.value)
      if (currentNode != null) {
        syncFormsFromItem(currentNode.item)
      } else {
        selectedItemId.value = null
      }
    }
  } catch (error) {
    pageError.value = getErrorMessage(error)
  } finally {
    isBootstrapping.value = false
  }
}

async function selectNode(node: TreeNode) {
  selectedItemId.value = node.item.id
  syncFormsFromItem(node.item)

  if (!node.isBranchLoaded && !node.isBranchLoading) {
    await loadBranch(node)
  }
}

async function toggleNode(node: TreeNode) {
  node.isExpanded = !node.isExpanded

  if (node.isExpanded && !node.isBranchLoaded && !node.isBranchLoading) {
    await loadBranch(node)
  }
}

async function loadBranch(node: TreeNode) {
  node.isBranchLoading = true
  pageError.value = null

  try {
    const branch = await getBranch(node.item.id)
    node.children = branch.embedded.children.map(branchChild => {
      const currentChild = node.children.find(child => child.item.id === branchChild.id)
      return currentChild == null ? createNode(branchChild) : mergeNode(currentChild, branchChild)
    }).sort(compareNodes)
    node.isBranchLoaded = true
  } catch (error) {
    pageError.value = getErrorMessage(error)
    node.isExpanded = false
  } finally {
    node.isBranchLoading = false
  }
}

async function submitCreate() {
  if (isSubmitting.value) {
    return
  }

  pageError.value = null
  successMessage.value = null
  isSubmitting.value = true

  try {
    const response = await sendMutation('/api/v1/content', {
      method: 'POST',
      body: JSON.stringify({
        name: createForm.name,
        key: createForm.key,
        templateId: createForm.templateId,
        parentId: normalizeOptionalValue(createForm.parentId)
      })
    })

    await applyMutationResponse(response)
    resetCreateForm()
    successMessage.value = `Created ${response.item.name} and refreshed the affected branch.`
  } catch (error) {
    pageError.value = getErrorMessage(error)
  } finally {
    isSubmitting.value = false
  }
}

async function submitRename() {
  if (isSubmitting.value || selectedItem.value == null) {
    return
  }

  pageError.value = null
  successMessage.value = null
  isSubmitting.value = true

  try {
    const response = await sendMutation(selectedItem.value._links.rename.href, {
      method: 'POST',
      body: JSON.stringify({
        name: renameForm.name,
        key: renameForm.key
      })
    })

    await applyMutationResponse(response)
    successMessage.value = `Renamed ${response.item.name} and refreshed the branch in place.`
  } catch (error) {
    pageError.value = getErrorMessage(error)
  } finally {
    isSubmitting.value = false
  }
}

async function submitMove() {
  if (isSubmitting.value || selectedItem.value == null) {
    return
  }

  pageError.value = null
  successMessage.value = null
  isSubmitting.value = true

  try {
    const response = await sendMutation(selectedItem.value._links.move.href, {
      method: 'POST',
      body: JSON.stringify({
        parentId: normalizeOptionalValue(moveForm.parentId)
      })
    })

    await applyMutationResponse(response)
    successMessage.value = `Moved ${response.item.name} and refreshed the affected branches.`
  } catch (error) {
    pageError.value = getErrorMessage(error)
  } finally {
    isSubmitting.value = false
  }
}

async function applyMutationResponse(response: ContentMutationResponse) {
  for (const affected of response.affectedBranches) {
    applyBranchToTree(affected.branch)
  }

  const refreshedItem = await getItem(response.item.id)
  upsertNode(extractParentIdFromHref(refreshedItem._links.parent?.href), refreshedItem)
  selectedItemId.value = response.item.id
  syncFormsFromItem(refreshedItem)
}

function applyBranchToTree(branch: ContentBranchResponse) {
  if (branch.item == null) {
    rootNodes.value = branch.embedded.children
      .map(branchChild => {
        const currentNode = findNodeById(rootNodes.value, branchChild.id)
        return currentNode == null ? createNode(branchChild) : mergeNode(currentNode, branchChild)
      })
      .sort(compareNodes)

    return
  }

  const parentNode = findNodeById(rootNodes.value, branch.item.id)

  if (parentNode == null) {
    rootNodes.value = upsertAtRoot(rootNodes.value, branch.item)
    return
  }

  parentNode.item = branch.item
  parentNode.isBranchLoaded = true
  parentNode.children = branch.embedded.children
    .map(branchChild => {
      const existingChild = parentNode.children.find(child => child.item.id === branchChild.id)
      return existingChild == null ? createNode(branchChild) : mergeNode(existingChild, branchChild)
    })
    .sort(compareNodes)
}

function upsertNode(parentId: string | null, item: ContentItemResponse) {
  if (parentId == null) {
    rootNodes.value = upsertAtRoot(rootNodes.value, item)
    return
  }

  const parentNode = findNodeById(rootNodes.value, parentId)

  if (parentNode == null) {
    return
  }

  const currentNode = parentNode.children.find(child => child.item.id === item.id)

  if (currentNode == null) {
    parentNode.children = [...parentNode.children, createNode(item)].sort(compareNodes)
  } else {
    mergeNode(currentNode, item)
    parentNode.children = [...parentNode.children].sort(compareNodes)
  }

  parentNode.isBranchLoaded = true
}

function upsertAtRoot(nodes: TreeNode[], item: ContentItemResponse) {
  const currentNode = nodes.find(node => node.item.id === item.id)

  if (currentNode == null) {
    return [...nodes, createNode(item)].sort(compareNodes)
  }

  mergeNode(currentNode, item)
  return [...nodes].sort(compareNodes)
}

function mergeNode(node: TreeNode, item: ContentItemResponse) {
  node.item = item
  return node
}

function createNode(item: ContentItemResponse): TreeNode {
  return {
    item,
    children: [],
    isExpanded: false,
    isBranchLoaded: false,
    isBranchLoading: false
  }
}

function compareNodes(left: TreeNode, right: TreeNode) {
  return left.item.path.localeCompare(right.item.path)
}

function findNodeById(nodes: TreeNode[], id: string | null): TreeNode | null {
  if (id == null) {
    return null
  }

  for (const node of nodes) {
    if (node.item.id === id) {
      return node
    }

    const nested = findNodeById(node.children, id)
    if (nested != null) {
      return nested
    }
  }

  return null
}

function syncFormsFromItem(item: ContentItemResponse) {
  renameForm.name = item.name
  const pathSegments = item.path.split('/').filter(Boolean)
  renameForm.key = pathSegments[pathSegments.length - 1] ?? ''
  moveForm.parentId = extractParentIdFromHref(item._links.parent?.href) ?? ''
  createForm.parentId = item.id
}

function resetCreateForm() {
  createForm.name = ''
  createForm.key = ''
  createForm.templateId = ''
}

async function getRootBranch() {
  return await fetchJson<ContentBranchResponse>(withContext('/api/v1/content/root/branch'))
}

async function getBranch(itemId: string) {
  return await fetchJson<ContentBranchResponse>(withContext(`/api/v1/content/${itemId}/branch`))
}

async function getItem(itemId: string) {
  return await fetchJson<ContentItemResponse>(withContext(`/api/v1/content/${itemId}`))
}

async function sendMutation(url: string, init: RequestInit) {
  return await fetchJson<ContentMutationResponse>(url, withJsonDefaults(init))
}

async function fetchJson<T>(url: string, init?: RequestInit): Promise<T> {
  const response = await fetch(url, init)

  if (!response.ok) {
    const problem = await response.json().catch(() => null) as { detail?: string; title?: string } | null
    throw new Error(problem?.detail ?? problem?.title ?? `Request failed with ${response.status}.`)
  }

  return await response.json() as T
}

function withContext(url: string) {
  const separator = url.includes('?') ? '&' : '?'
  return `${url}${separator}lang=${encodeURIComponent(language.value)}&version=${encodeURIComponent(version.value)}`
}

function withJsonDefaults(init: RequestInit): RequestInit {
  return {
    ...init,
    headers: {
      'Content-Type': 'application/json',
      ...(init.headers ?? {})
    }
  }
}

function extractParentIdFromHref(href: string | undefined) {
  if (href == null) {
    return null
  }

  const match = href.match(/\/api\/v1\/content\/([^/?]+)/i)
  return match?.[1] ?? null
}

function normalizeOptionalValue(value: string) {
  const trimmed = value.trim()
  return trimmed.length === 0 ? null : trimmed
}

function getErrorMessage(error: unknown) {
  if (error instanceof Error) {
    return error.message
  }

  return 'Something went wrong while talking to the authoring API.'
}

function countNodes(nodes: TreeNode[]): number {
  return nodes.reduce((total, node) => total + 1 + countNodes(node.children), 0)
}
</script>

<template>
  <div class="workspace-shell">
    <aside class="workspace-sidebar">
      <div class="brand">
        <span class="brand-mark">TC</span>
        <div>
          <p class="eyebrow">Authoring</p>
          <h1>TemplarCMS Admin</h1>
        </div>
      </div>

      <section class="context-panel">
        <p class="eyebrow">Tree-Aware Flow</p>
        <h2>Client refreshes only the branches that changed.</h2>
        <p>
          This screen consumes the branch and mutation contracts so create, rename,
          and move can patch the tree without a full explorer reload.
        </p>
      </section>

      <section class="context-panel">
        <p class="eyebrow">Request Context</p>
        <div class="context-grid">
          <label class="field">
            <span>Language</span>
            <input v-model="language" type="text" />
          </label>
          <label class="field">
            <span>Version</span>
            <input v-model.number="version" type="number" min="1" />
          </label>
        </div>
        <button class="button button--secondary" type="button" @click="refreshRootBranch">
          Refresh Root Branch
        </button>
      </section>

      <section class="context-panel">
        <p class="eyebrow">Current State</p>
        <ul class="meta-list">
          <li>{{ treeCount }} loaded nodes</li>
          <li>{{ selectedItem == null ? 'No active selection' : `Selected: ${selectedItem.name}` }}</li>
          <li>{{ isBootstrapping ? 'Loading branch data' : 'Ready for authoring flow' }}</li>
        </ul>
      </section>
    </aside>

    <main class="workspace-main">
      <header class="top-panel">
        <div>
          <p class="eyebrow">Content Tree Explorer</p>
          <h2>Branch refreshes driven by the API we just designed.</h2>
        </div>
        <p class="top-panel__copy">
          Expand a node to load its branch, select an item to rename or move it,
          and create new items beneath the current selection or root.
        </p>
      </header>

      <div v-if="pageError != null" class="banner banner--error">
        {{ pageError }}
      </div>

      <div v-if="successMessage != null" class="banner banner--success">
        {{ successMessage }}
      </div>

      <section class="content-grid">
        <article class="panel tree-panel">
          <div class="panel-header">
            <div>
              <p class="eyebrow">Navigator</p>
              <h3>Loaded Branches</h3>
            </div>
            <span class="panel-pill">{{ rootNodes.length }} root nodes</span>
          </div>

          <div v-if="isBootstrapping" class="empty-state">
            Loading the root branch from `/api/v1/content/root/branch`...
          </div>

          <div v-else-if="rootNodes.length === 0" class="empty-state">
            No root content items have been loaded yet.
          </div>

          <ul v-else class="tree-list">
            <TreeBranch
              v-for="node in rootNodes"
              :key="node.item.id"
              :node="node"
              :selected-item-id="selectedItemId"
              @toggle="toggleNode"
              @select="selectNode"
            />
          </ul>
        </article>

        <article class="panel inspector-panel">
          <div class="panel-header">
            <div>
              <p class="eyebrow">Inspector</p>
              <h3>{{ selectedItem == null ? 'Select an item' : selectedItem.name }}</h3>
            </div>
            <span class="panel-pill">{{ selectedItem == null ? 'Idle' : selectedItem.path }}</span>
          </div>

          <div v-if="selectedItem == null" class="empty-state">
            Pick a content item from the tree to inspect it, rename it, or move it.
          </div>

          <template v-else>
            <section class="summary-card">
              <dl class="summary-grid">
                <div>
                  <dt>Id</dt>
                  <dd>{{ selectedItem.id }}</dd>
                </div>
                <div>
                  <dt>Template</dt>
                  <dd>{{ selectedItem.templateId }}</dd>
                </div>
                <div>
                  <dt>Path</dt>
                  <dd>{{ selectedItem.path }}</dd>
                </div>
                <div>
                  <dt>Parent Link</dt>
                  <dd>{{ selectedItem._links.parent?.href ?? 'Root item' }}</dd>
                </div>
              </dl>
            </section>

            <section class="form-stack">
              <form class="editor-card" @submit.prevent="submitRename">
                <div class="editor-card__header">
                  <div>
                    <p class="eyebrow">Rename</p>
                    <h4>Update display name and key</h4>
                  </div>
                  <span class="callout">Uses the explicit rename contract</span>
                </div>

                <label class="field">
                  <span>Name</span>
                  <input v-model="renameForm.name" type="text" required />
                </label>

                <label class="field">
                  <span>Key</span>
                  <input v-model="renameForm.key" type="text" required />
                </label>

                <button class="button" type="submit" :disabled="isSubmitting">
                  Rename Item
                </button>
              </form>

              <form class="editor-card" @submit.prevent="submitMove">
                <div class="editor-card__header">
                  <div>
                    <p class="eyebrow">Move</p>
                    <h4>Re-parent the current item</h4>
                  </div>
                  <span class="callout">Blank parent means move to root</span>
                </div>

                <label class="field">
                  <span>New Parent Id</span>
                  <input v-model="moveForm.parentId" type="text" placeholder="Leave blank for root" />
                </label>

                <button class="button" type="submit" :disabled="isSubmitting">
                  Move Item
                </button>
              </form>
            </section>
          </template>
        </article>
      </section>

      <section class="panel composer-panel">
        <div class="panel-header">
          <div>
            <p class="eyebrow">Create</p>
            <h3>Author a new item</h3>
          </div>
          <span class="panel-pill">
            {{ createForm.parentId.trim().length === 0 ? 'Creating at root' : `Parent: ${createForm.parentId}` }}
          </span>
        </div>

        <form class="create-grid" @submit.prevent="submitCreate">
          <label class="field">
            <span>Name</span>
            <input v-model="createForm.name" type="text" required />
          </label>

          <label class="field">
            <span>Key</span>
            <input v-model="createForm.key" type="text" required />
          </label>

          <label class="field">
            <span>Template Id</span>
            <input v-model="createForm.templateId" type="text" required />
          </label>

          <label class="field">
            <span>Parent Id</span>
            <input v-model="createForm.parentId" type="text" placeholder="Leave blank for root" />
          </label>

          <div class="create-actions">
            <button class="button" type="submit" :disabled="isSubmitting">
              Create Item
            </button>
            <button class="button button--secondary" type="button" @click="resetCreateForm">
              Clear
            </button>
          </div>
        </form>
      </section>
    </main>
  </div>
</template>
