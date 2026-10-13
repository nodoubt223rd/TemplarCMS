<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, provide, reactive, ref, watch } from 'vue'
import { getAuthRequestGeneration, withinSession } from './utils/auth-request-context'
import { useAuthSession } from './composables/useAuthSession'
import LoginForm from './components/security/LoginForm.vue'
import ChangePasswordForm from './components/security/ChangePasswordForm.vue'
import TemplateCatalogPane from './components/TemplateCatalogPane.vue'
import TemplateInspectorPane from './components/TemplateInspectorPane.vue'
import TemplateDesignerPane from './components/TemplateDesignerPane.vue'
import AuthorWorkspace from './components/AuthorWorkspace.vue'
import ContextMenu from './components/tree/ContextMenu.vue'
import TreeActionDialog from './components/tree/TreeActionDialog.vue'
import { useTreeActions } from './composables/useTreeActions'
import { reconcileTreeAction } from './utils/reconcile-tree-action'
import { reconcileOrderDraft } from './utils/reconcile-order-draft'
import TopBar from './components/layout/TopBar.vue'
import StatusBar from './components/layout/StatusBar.vue'
import ContentTree from './components/tree/ContentTree.vue'
import ContentEditor from './components/editor/ContentEditor.vue'
import ActionSidebar from './components/sidebar/ActionSidebar.vue'
import type {
  ContentBranchResponse,
  FieldTypeCollectionResponse,
  FieldTypeResponse,
  ContentItemDependencyResponse,
  ContentItemResponse,
  ContentMutationResponse,
  TemplateCollectionResponse,
  TemplateDependencyResponse,
  TemplateResponse,
  TemplateFieldCollectionResponse,
  TemplateFieldItemResponse,
  TemplateSummaryResponse
} from './types/admin-api'
import type {
  TemplateDesignerFormState,
  TemplateDraftSection
} from './types/template-designer'
import type { EditorFieldModel, TreeNode } from './types/admin-ui'
import {
  clearFieldFormValues,
  getCreatableTemplates,
  getSuggestedTemplateId,
  getTemplateKeyById,
  resetCreateForm as resetCreateInspectorForm,
  resetInspectorForms as resetInspectorFormState,
  syncFieldFormValues,
  syncInspectorFormsFromItem
} from './utils/content-inspector'
import {
  applyBranchToTree as applyBranchToContentTree,
  createTreeNode,
  extractParentIdFromHref,
  findTreeNodeById,
  upsertTreeNode
} from './utils/content-tree'
import {
  buildEditorFields,
  createFieldTypeLookup
} from './utils/editor-fields'
import {
  normalizeFieldValue,
  normalizeOptionalValue,
  setCheckboxFieldValue,
  setFieldFormValue
} from './utils/field-form'
import {
  fetchJson,
  fetchWithNoContent,
  getErrorMessage,
  sendMutation,
  withContext as withRequestContext,
  withJsonDefaults
} from './utils/request-helpers'
import { buildTemplateWorkspaceViewModel } from './utils/template-workspace'
import {
  addTemplateDraftField,
  addTemplateDraftSection,
  buildTemplateDesignerPayload,
  createNewTemplateDesignerState,
  getDefaultTemplateDesignerBaseTemplateId,
  mapTemplateToDesignerState,
  removeTemplateDraftField,
  removeTemplateDraftSection,
  validateTemplateDesignerState
} from './utils/template-designer'
import {
  getAuthorVisibleBaseTemplates,
  getAuthorVisibleTemplates,
  getTemplateVisibilityLabel,
  isAuthorVisibleTemplate
} from './utils/template-visibility'
import {
  updateTemplateDesignerFormName,
  updateTemplateDraftField,
  updateTemplateDraftFieldName,
  updateTemplateDraftSection,
  updateTemplateDraftSectionName
} from './utils/template-designer-state'

const language = ref('en')
const auth = useAuthSession()
provide('workspace-suspended', computed(() => auth.state.value !== 'authenticated'))

const workspaceInitialized = ref(false)
const loadedAccountId = ref<string | null>(null)
const version = ref(1)
const activeWorkspace = ref<'content' | 'templates' | 'media' | 'system'>('content')
const isBootstrapping = ref(false)
const isSubmitting = ref(false)
const pageError = ref<string | null>(null)
const successMessage = ref<string | null>(null)
const showActions = ref(false)

const rootNodes = ref<TreeNode[]>([])
const selectedItemId = ref<string | null>(null)
const selectedNode = computed(() => findTreeNodeById(rootNodes.value, selectedItemId.value))
const selectedItem = computed(() => selectedNode.value?.item ?? null)
const contentWorkspaceRoot = computed<TreeNode>(() => rootNodes.value[0] ?? createFallbackContentWorkspaceRoot())

const treeActions = useTreeActions({
  templates: () => availableTemplates.value,
  context: () => ({ language: language.value, version: version.value }),
  contentRoot: () => contentWorkspaceRoot.value.item,
  beforeAction: request => {
    if (auth.state.value !== 'authenticated') return false
    if (isSubmitting.value) return false
    if (request.target.kind === 'template' && request.target.id === selectedTemplateId.value && ['rename', 'delete'].includes(request.action))
      return window.confirm('This action will reload or close the selected template. Discard any unsaved template edits?')
    if (request.target.kind === 'content' && request.target.id === selectedItemId.value && request.action === 'delete')
      return window.confirm('Delete the selected item and discard its unsaved edits?')
    return true
  },
  onContentChanged: async response => {
    const generation = getAuthRequestGeneration()
    rootNodes.value = await withinSession(reconcileTreeAction(rootNodes.value, response, getItem,
      id => id ? getBranch(id) : getRootBranch(), shouldApplyWorkspaceBranch), generation)
    if (treeActions.dialog.value?.request.action === 'delete' && selectedItemId.value === response.item.id) {
      selectedItemId.value = null
      resetInspectorForms()
      clearFieldFormValues(fieldForm)
    }
    successMessage.value = 'Tree action saved.'
  },
  onReordered: async parentId => {
    const generation = getAuthRequestGeneration()
    const branch = parentId ? await withinSession(getBranch(parentId), generation) : await withinSession(getRootBranch(), generation)
    rootNodes.value = applyBranchToContentTree(rootNodes.value, branch)
    reconcileOrderDraft(fieldForm, selectedItemId.value, branch.embedded.children)
    successMessage.value = 'Item order saved.'
  },
  onTemplatesChanged: async request => {
    const generation = getAuthRequestGeneration()
    const wasSelected = request.target.kind === 'template' && request.target.id === selectedTemplateId.value
    const response = await withinSession(fetchJson<TemplateCollectionResponse>('/api/v1/templates'), generation)
    availableTemplates.value = response.embedded.templates
    if (request.target.kind === 'template' && wasSelected) {
      if (request.action === 'delete') { selectedTemplateId.value = null; selectedTemplateDetail.value = null }
      else if (request.action === 'rename') await withinSession(loadTemplateDetail(request.target.id), generation)
    }
    successMessage.value = 'Template action saved.'
  }
})
watch(activeWorkspace, workspace => {
  const generation = getAuthRequestGeneration()
  if (auth.state.value === 'authenticated' && workspace === 'templates') void treeActions.refreshOrganization().catch(error => {
    if (generation === getAuthRequestGeneration()) pageError.value = getErrorMessage(error)
  })
})

const createForm = reactive({
  name: '',
  templateId: '',
  parentId: ''
})

const renameForm = reactive({
  name: ''
})

const moveForm = reactive({
  parentId: ''
})

const fieldForm = reactive<Record<string, string>>({})
const availableFieldTypes = ref<FieldTypeResponse[]>([])
const isLoadingFieldTypes = ref(false)
const availableTemplates = ref<TemplateSummaryResponse[]>([])
const isLoadingTemplates = ref(false)
const templateFields = ref<TemplateFieldItemResponse[]>([])
const isLoadingTemplateFields = ref(false)
const selectedItemDependencies = ref<ContentItemDependencyResponse | null>(null)
const isLoadingItemDependencies = ref(false)
const selectedTemplateId = ref<string | null>(null)
const isNewTemplateDraftOpen = ref(false)
const selectedTemplateDetail = ref<TemplateResponse | null>(null)
const selectedTemplateDependencies = ref<TemplateDependencyResponse | null>(null)
const isLoadingTemplateDetail = ref(false)
const isLoadingTemplateDependencies = ref(false)
const selectedBaseTemplateDetail = ref<TemplateResponse | null>(null)
const isLoadingBaseTemplatePreview = ref(false)
const baseTemplatePreviewError = ref<string | null>(null)
const templateDesignerForm = reactive<TemplateDesignerFormState>(createNewTemplateDesignerState().form)
const templateDraftSections = ref<TemplateDraftSection[]>([])

const selectedCreateTemplate = computed(() =>
  creatableTemplates.value.find(template => template.id === createForm.templateId) ?? null)
const creatableTemplates = computed(() =>
  getCreatableTemplates(availableTemplates.value))
const visibleTemplates = computed(() =>
  getAuthorVisibleTemplates(availableTemplates.value))
const selectedTemplateSummary = computed(() =>
  selectedTemplateId.value == null
    ? null
    : availableTemplates.value.find(template => template.id === selectedTemplateId.value) ?? null)
const templateWorkspace = computed(() =>
  buildTemplateWorkspaceViewModel(selectedTemplateDetail.value, fieldTypeLookup.value))
const templateSections = computed(() => templateWorkspace.value.sections)
const selectedTemplateFieldCount = computed(() => templateWorkspace.value.fieldCount)
const availableBaseTemplates = computed(() =>
  getAuthorVisibleBaseTemplates(availableTemplates.value, templateDesignerForm.templateId))
const baseTemplateVisibilityLabel = computed(() =>
  getTemplateVisibilityLabel(availableTemplates.value, templateDesignerForm.baseTemplateId))
const templateDesignerHeading = computed(() =>
  templateDesignerForm.mode === 'create'
    ? 'Draft a new template'
    : `Editing ${templateDesignerForm.name || 'template'}`)
const selectedBaseTemplateKey = computed(() =>
  getTemplateKeyById(availableTemplates.value, templateDesignerForm.baseTemplateId))
const inheritedTemplateWorkspace = computed(() =>
  buildTemplateWorkspaceViewModel(selectedBaseTemplateDetail.value, fieldTypeLookup.value))
const templateDesignerValidationErrors = computed(() =>
  validateTemplateDesignerState(
    templateDesignerForm,
    templateDraftSections.value,
    availableFieldTypes.value.map(fieldType => fieldType.value),
    selectedBaseTemplateKey.value
  ))
const fieldTypeLookup = computed(() =>
  createFieldTypeLookup(availableFieldTypes.value))
const selectedItemTemplateName = computed(() => {
  const item = selectedItem.value

  if (item == null) {
    return null
  }

  return availableTemplates.value.find(template => template.id === item.templateId)?.name ?? null
})
const editorFields = computed<EditorFieldModel[]>(() =>
  buildEditorFields(fieldForm, templateFields.value, fieldTypeLookup.value))

onMounted(() => { void auth.initialize() })
onBeforeUnmount(auth.dispose)

watch(auth.state, async state => {
  const generation = getAuthRequestGeneration()
  if (state !== 'authenticated') {
    treeActions.menu.value = null
    return
  }
  const accountId = auth.session.value?.userId ?? null
  if (loadedAccountId.value !== accountId) clearWorkspaceState()
  loadedAccountId.value = accountId
  isSubmitting.value = false
  treeActions.busy.value = false
  isBootstrapping.value = false
  isLoadingFieldTypes.value = false
  isLoadingTemplates.value = false
  isLoadingTemplateFields.value = false
  isLoadingItemDependencies.value = false
  isLoadingTemplateDetail.value = false
  isLoadingTemplateDependencies.value = false
  isLoadingBaseTemplatePreview.value = false
  if (workspaceInitialized.value) return
  try {
    language.value = auth.session.value?.language ?? 'en'
    await withinSession(loadFieldTypes(), generation)
    await withinSession(loadTemplates(), generation)
    await withinSession(refreshRootBranch(), generation)
    if (selectedTemplateId.value != null) await withinSession(loadTemplateWorkspace(selectedTemplateId.value), generation)
    if (auth.state.value === 'authenticated') workspaceInitialized.value = true
  } catch (cause) {
    if (generation !== getAuthRequestGeneration()) return
    pageError.value = getErrorMessage(cause)
  }
})

watch(
  () => templateDesignerForm.baseTemplateId,
  async baseTemplateId => {
    const generation = getAuthRequestGeneration()
    if (auth.state.value === 'authenticated') await withinSession(loadBaseTemplatePreview(baseTemplateId), generation)
  }
)

function clearWorkspaceState() {
  workspaceInitialized.value = false
  isSubmitting.value = false
  loadedAccountId.value = null
  rootNodes.value = []
  selectedItemId.value = null
  selectedTemplateId.value = null
  selectedTemplateDetail.value = null
  selectedTemplateDependencies.value = null
  selectedItemDependencies.value = null
  selectedBaseTemplateDetail.value = null
  availableTemplates.value = []
  availableFieldTypes.value = []
  templateFields.value = []
  templateDraftSections.value = []
  Object.assign(templateDesignerForm, createNewTemplateDesignerState().form)
  resetInspectorForms()
  clearFieldFormValues(fieldForm)
  treeActions.reset()
  pageError.value = null
  successMessage.value = null
  activeWorkspace.value = 'content'
}
async function logoutWorkspace() {
  if (await auth.logout()) clearWorkspaceState()
}

async function refreshRootBranch() {
  const generation = getAuthRequestGeneration()
  isBootstrapping.value = true
  pageError.value = null

  try {
    const branch = await withinSession(getRootBranch(), generation)
    rootNodes.value = branch.item == null ? [] : [createWorkspaceRootNode(branch)]

    if (selectedItemId.value != null) {
      const currentNode = findTreeNodeById(rootNodes.value, selectedItemId.value)
      if (currentNode != null) {
        await withinSession(syncInspectorFromItem(currentNode.item), generation)
      } else {
        selectedItemId.value = null
        resetInspectorForms()
      }
    }
  } catch (error) {
    if (generation !== getAuthRequestGeneration()) return
    pageError.value = getErrorMessage(error)
  } finally {
    if (generation === getAuthRequestGeneration()) isBootstrapping.value = false
  }
}

async function selectNode(node: TreeNode) {
  const generation = getAuthRequestGeneration()
  selectedItemId.value = node.item.id
  await withinSession(syncInspectorFromItem(node.item), generation)

  if (!node.isBranchLoaded && !node.isBranchLoading) {
    await withinSession(loadBranch(node), generation)
  }
}

async function toggleNode(node: TreeNode) {
  const generation = getAuthRequestGeneration()
  node.isExpanded = !node.isExpanded

  if (node.isExpanded && !node.isBranchLoaded && !node.isBranchLoading) {
    await withinSession(loadBranch(node), generation)
  }
}

async function loadBranch(node: TreeNode) {
  const generation = getAuthRequestGeneration()
  node.isBranchLoading = true
  pageError.value = null

  try {
    const branch = await withinSession(getBranch(node.item.id), generation)
    node.children = branch.embedded.children.map(branchChild => {
      const currentChild = node.children.find(child => child.item.id === branchChild.id)
      return currentChild == null ? createTreeNode(branchChild) : currentChild
    }).sort((left, right) => left.item.path.localeCompare(right.item.path))
    node.isBranchLoaded = true
  } catch (error) {
    if (generation !== getAuthRequestGeneration()) return
    pageError.value = getErrorMessage(error)
    node.isExpanded = false
  } finally {
    if (generation === getAuthRequestGeneration()) node.isBranchLoading = false
  }
}

async function submitCreate() {
  const generation = getAuthRequestGeneration()
  if (isSubmitting.value) {
    return
  }

  pageError.value = null
  successMessage.value = null
  isSubmitting.value = true

  try {
    const response = await withinSession(sendMutation<ContentMutationResponse>('/api/v1/content', {
      method: 'POST',
      body: JSON.stringify({
        name: createForm.name,
        templateId: createForm.templateId,
        parentId: normalizeOptionalValue(createForm.parentId)
      })
    }), generation)

    await withinSession(applyMutationResponse(response), generation)
    resetCreateForm()
    successMessage.value = `Created ${response.item.name} and refreshed the affected branch.`
  } catch (error) {
    if (generation !== getAuthRequestGeneration()) return
    pageError.value = getErrorMessage(error)
  } finally {
    if (generation === getAuthRequestGeneration()) isSubmitting.value = false
  }
}

async function submitDelete() {
  const generation = getAuthRequestGeneration()
  if (isSubmitting.value || selectedItem.value == null) {
    return
  }

  const itemToDelete = selectedItem.value
  const dependencyState = selectedItemDependencies.value

  if (dependencyState?.canDelete === false) {
    pageError.value = `Content item ${itemToDelete.name} still has direct children.`
    return
  }

  if (!window.confirm(`Delete ${itemToDelete.name}? This only works when the item has no direct children.`)) {
    return
  }

  pageError.value = null
  successMessage.value = null
  isSubmitting.value = true

  try {
    const response = await withinSession(sendMutation<ContentMutationResponse>(itemToDelete._links.delete.href, {
      method: 'DELETE'
    }), generation)

    applyDeletedMutationResponse(response)
    successMessage.value = `Deleted ${response.item.name} and refreshed the affected branch.`
  } catch (error) {
    if (generation !== getAuthRequestGeneration()) return
    pageError.value = getErrorMessage(error)
  } finally {
    if (generation === getAuthRequestGeneration()) isSubmitting.value = false
  }
}

async function submitValues() {
  const generation = getAuthRequestGeneration()
  if (isSubmitting.value || selectedItem.value == null) {
    return
  }

  pageError.value = null
  successMessage.value = null
  isSubmitting.value = true

  try {
    const response = await withinSession(fetchJson<ContentItemResponse>(
      selectedItem.value._links['set-values'].href,
      withJsonDefaults({
        method: 'POST',
        body: JSON.stringify({
          language: language.value,
          version: version.value,
          values: Object.fromEntries(
            Object.entries(fieldForm).map(([key, value]) => [key, normalizeFieldValue(value)])
          )
        })
      })
    ), generation)

    rootNodes.value = upsertTreeNode(rootNodes.value, extractParentIdFromHref(response._links.parent?.href), response)
    selectedItemId.value = response.id
    await withinSession(syncInspectorFromItem(response), generation)
    successMessage.value = `Saved ${Object.keys(fieldForm).length} field values for ${response.name}.`
  } catch (error) {
    if (generation !== getAuthRequestGeneration()) return
    pageError.value = getErrorMessage(error)
  } finally {
    if (generation === getAuthRequestGeneration()) isSubmitting.value = false
  }
}

async function updateSelectedItemIcon(icon: string | null) {
  const generation = getAuthRequestGeneration()
  if (isSubmitting.value || selectedItem.value == null) {
    return
  }

  isSubmitting.value = true
  pageError.value = null

  try {
    const response = await withinSession(fetchJson<ContentItemResponse>(
      selectedItem.value._links.self.href,
      withJsonDefaults({
        method: 'PUT',
        body: JSON.stringify({ name: selectedItem.value.name, icon })
      })
    ), generation)

    rootNodes.value = upsertTreeNode(
      rootNodes.value,
      extractParentIdFromHref(response._links.parent?.href),
      response
    )
    successMessage.value = icon == null
      ? `Cleared the icon override for ${response.name}.`
      : `Updated the icon for ${response.name}.`
  } catch (error) {
    if (generation !== getAuthRequestGeneration()) return
    pageError.value = getErrorMessage(error)
  } finally {
    if (generation === getAuthRequestGeneration()) isSubmitting.value = false
  }
}

async function updateSelectedTemplateIcon(icon: string) {
  const generation = getAuthRequestGeneration()
  if (isSubmitting.value || selectedTemplateDetail.value == null) {
    return
  }

  const template = selectedTemplateDetail.value
  isSubmitting.value = true
  pageError.value = null

  try {
    const response = await withinSession(fetchJson<TemplateResponse>(
      template._links.self.href,
      withJsonDefaults({
        method: 'PUT',
        body: JSON.stringify({
          name: template.name,
          key: template.key,
          icon,
          baseTemplateKeys: template.baseTemplates.map(baseTemplate => baseTemplate.key),
          sections: template.sections.map(section => ({
            name: section.name,
            key: section.key,
            sortOrder: section.sortOrder,
            fields: section.fields.map(field => ({
              name: field.name,
              key: field.key,
              type: field.type,
              isShared: field.isShared,
              isUnversioned: field.isUnversioned
            }))
          }))
        })
      })
    ), generation)

    await withinSession(loadTemplates(), generation)
    selectedTemplateDetail.value = response
    successMessage.value = `Updated the icon for ${response.name}.`
  } catch (error) {
    if (generation !== getAuthRequestGeneration()) return
    pageError.value = getErrorMessage(error)
  } finally {
    if (generation === getAuthRequestGeneration()) isSubmitting.value = false
  }
}

async function updateSelectedTemplateBaseTemplates(templateIds: string[]) {
  const generation = getAuthRequestGeneration()
  if (isSubmitting.value || selectedTemplateDetail.value == null) {
    return
  }

  const template = selectedTemplateDetail.value
  const baseTemplateKeys = templateIds
    .map(templateId => availableTemplates.value.find(candidate => candidate.id === templateId)?.key)
    .filter((key): key is string => key != null)

  if (baseTemplateKeys.length !== templateIds.length) {
    pageError.value = 'One or more selected base templates could not be resolved.'
    return
  }

  isSubmitting.value = true
  pageError.value = null

  try {
    const response = await withinSession(fetchJson<TemplateResponse>(
      template._links.self.href,
      withJsonDefaults({
        method: 'PUT',
        body: JSON.stringify({
          name: template.name,
          key: template.key,
          icon: template.authoredIcon === undefined ? template.icon ?? null : template.authoredIcon,
          baseTemplateKeys,
          sections: template.sections.map(section => ({
            name: section.name,
            key: section.key,
            sortOrder: section.sortOrder,
            fields: section.fields.map(field => ({
              name: field.name,
              key: field.key,
              type: field.type,
              isShared: field.isShared,
              isUnversioned: field.isUnversioned
            }))
          }))
        })
      })
    ), generation)

    await withinSession(loadTemplates(), generation)
    selectedTemplateDetail.value = response
    successMessage.value = `Updated inherited templates for ${response.name}.`
    if (selectedItem.value != null) await withinSession(loadTemplateFields(selectedItem.value), generation)
  } catch (error) {
    if (generation !== getAuthRequestGeneration()) return
    pageError.value = getErrorMessage(error)
  } finally {
    if (generation === getAuthRequestGeneration()) isSubmitting.value = false
  }
}

async function saveSelectedTemplate(template: {
  name: string
  key: string
  icon: string | null
  baseTemplateKeys: string[]
  sections: Array<{
    name: string
    key: string
    sortOrder: number
    fields: Array<{ name: string; key: string; type: string; isShared: boolean; isUnversioned: boolean }>
  }>
}) {
  const generation = getAuthRequestGeneration()
  if (isSubmitting.value || selectedTemplateDetail.value == null) {
    return
  }

  isSubmitting.value = true
  pageError.value = null

  try {
    const response = await withinSession(fetchJson<TemplateResponse>(
      selectedTemplateDetail.value._links.self.href,
      withJsonDefaults({ method: 'PUT', body: JSON.stringify(template) })
    ), generation)

    await withinSession(loadTemplates(), generation)
    selectedTemplateDetail.value = response
    successMessage.value = `Updated template ${response.name}.`
    // A base-template edit can also affect the selected item's effective fields.
    // Reload definitions without replacing unsaved author-entered field values.
    if (selectedItem.value != null) await withinSession(loadTemplateFields(selectedItem.value), generation)
  } catch (error) {
    if (generation !== getAuthRequestGeneration()) return
    pageError.value = getErrorMessage(error)
  } finally {
    if (generation === getAuthRequestGeneration()) isSubmitting.value = false
  }
}

async function applyMutationResponse(response: ContentMutationResponse) {
  const generation = getAuthRequestGeneration()
  for (const affected of response.affectedBranches) {
    if (shouldApplyWorkspaceBranch(affected.branch)) {
      rootNodes.value = applyBranchToContentTree(rootNodes.value, affected.branch)
    }
  }

  const refreshedItem = await withinSession(getItem(response.item.id), generation)

  if (!isWithinContentWorkspace(refreshedItem.path)) {
    selectedItemId.value = null
    resetInspectorForms()
    return
  }

  rootNodes.value = upsertTreeNode(rootNodes.value, extractParentIdFromHref(refreshedItem._links.parent?.href), refreshedItem)
  selectedItemId.value = response.item.id
  await withinSession(syncInspectorFromItem(refreshedItem), generation)
}

function applyDeletedMutationResponse(response: ContentMutationResponse) {
  for (const affected of response.affectedBranches) {
    if (shouldApplyWorkspaceBranch(affected.branch)) {
      rootNodes.value = applyBranchToContentTree(rootNodes.value, affected.branch)
    }
  }

  const parentId = extractParentIdFromHref(response.item._links.parent?.href)

  if (parentId == null) {
    selectedItemId.value = null
    resetInspectorForms()
    return
  }

  const parentNode = findTreeNodeById(rootNodes.value, parentId)

  if (parentNode == null) {
    selectedItemId.value = null
    resetInspectorForms()
    return
  }

  selectedItemId.value = parentNode.item.id
  void syncInspectorFromItem(parentNode.item)
}

function syncFormsFromItem(item: ContentItemResponse) {
  syncInspectorFormsFromItem(item, renameForm, moveForm, createForm, availableTemplates.value)
  syncFieldForm(item)
}

async function syncInspectorFromItem(item: ContentItemResponse) {
  const generation = getAuthRequestGeneration()
  syncFormsFromItem(item)
  await withinSession(Promise.all([
    loadTemplateFields(item),
    loadContentDependencies(item)
  ]), generation)
}

function resetCreateForm() {
  resetCreateInspectorForm(createForm, availableTemplates.value)
}

function resetInspectorForms() {
  resetInspectorFormState(renameForm, moveForm, createForm)
  clearFieldForm()
  templateFields.value = []
  selectedItemDependencies.value = null
}

function syncFieldForm(item: ContentItemResponse) {
  syncFieldFormValues(fieldForm, item)
}

function clearFieldForm() {
  clearFieldFormValues(fieldForm)
}

async function getRootBranch() {
  const generation = getAuthRequestGeneration()
  return await withinSession(fetchJson<ContentBranchResponse>(withContext('/api/v1/content/workspaces/content/branch')), generation)
}

function createWorkspaceRootNode(branch: ContentBranchResponse): TreeNode {
  if (branch.item == null) {
    return createFallbackContentWorkspaceRoot()
  }

  const rootNode = createTreeNode(branch.item)
  rootNode.children = branch.embedded.children.map(createTreeNode)
  rootNode.isExpanded = true
  rootNode.isBranchLoaded = true
  return rootNode
}

function createFallbackContentWorkspaceRoot(): TreeNode {
  return {
    item: {
      id: 'workspace-root:content',
      name: 'Content',
      templateId: '',
      path: '/templar/content',
      language: language.value,
      version: version.value,
      fields: {},
      _links: {
        self: { href: '/api/v1/content/workspaces/content/branch' },
        template: { href: '' },
        children: { href: '/api/v1/content/workspaces/content/branch' },
        dependencies: { href: '' },
        'set-values': { href: '' },
        rename: { href: '' },
        move: { href: '' },
        delete: { href: '' },
        branch: { href: '/api/v1/content/workspaces/content/branch' }
      }
    },
    children: [],
    isExpanded: true,
    isBranchLoaded: true,
    isBranchLoading: false
  }
}

function shouldApplyWorkspaceBranch(branch: ContentBranchResponse): boolean {
  return branch.item != null && isWithinContentWorkspace(branch.item.path)
}

function isWithinContentWorkspace(path: string): boolean {
  const workspacePath = contentWorkspaceRoot.value.item.path
  return path === workspacePath || path.startsWith(`${workspacePath}/`)
}

async function loadTemplates() {
  const generation = getAuthRequestGeneration()
  isLoadingTemplates.value = true

  try {
    const response = await withinSession(fetchJson<TemplateCollectionResponse>('/api/v1/templates'), generation)
    availableTemplates.value = response.embedded.templates
      .slice()
      .sort((left, right) => left.name.localeCompare(right.name) || left.key.localeCompare(right.key))

    if (createForm.templateId.length === 0) {
      createForm.templateId = getSuggestedTemplateId(availableTemplates.value)
    }

    if (templateDesignerForm.mode === 'create' && templateDesignerForm.baseTemplateId.length === 0) {
      templateDesignerForm.baseTemplateId = getDefaultTemplateDesignerBaseTemplateId(availableTemplates.value)
    }

    if (selectedTemplateId.value != null) {
      const selectedTemplate = selectedTemplateSummary.value

      if (selectedTemplate == null || !isAuthorVisibleTemplate(selectedTemplate)) {
        selectedTemplateId.value = null
      }
    }
  } catch {
    if (generation !== getAuthRequestGeneration()) return
    availableTemplates.value = []
    selectedTemplateId.value = null
  } finally {
    if (generation === getAuthRequestGeneration()) isLoadingTemplates.value = false
  }
}

async function loadFieldTypes() {
  const generation = getAuthRequestGeneration()
  isLoadingFieldTypes.value = true

  try {
    const response = await withinSession(fetchJson<FieldTypeCollectionResponse>('/api/v1/field-types'), generation)
    availableFieldTypes.value = response.embedded.fieldTypes
  } catch (error) {
    if (generation !== getAuthRequestGeneration()) return
    availableFieldTypes.value = []
    pageError.value = getErrorMessage(error)
  } finally {
    if (generation === getAuthRequestGeneration()) isLoadingFieldTypes.value = false
  }
}

async function getBranch(itemId: string) {
  const generation = getAuthRequestGeneration()
  return await withinSession(fetchJson<ContentBranchResponse>(withContext(`/api/v1/content/${itemId}/branch`)), generation)
}

async function getItem(itemId: string) {
  const generation = getAuthRequestGeneration()
  return await withinSession(fetchJson<ContentItemResponse>(withContext(`/api/v1/content/${itemId}`)), generation)
}

async function loadTemplateFields(item: ContentItemResponse) {
  const generation = getAuthRequestGeneration()
  isLoadingTemplateFields.value = true

  try {
    const response =
      await withinSession(fetchJson<TemplateFieldCollectionResponse>(`${item._links.template.href}/fields`), generation)

    templateFields.value = response.embedded.fields
      .slice()
      .sort((left, right) =>
        left.sectionSortOrder - right.sectionSortOrder ||
        left.sectionName.localeCompare(right.sectionName) ||
        left.name.localeCompare(right.name))
  } catch {
    if (generation !== getAuthRequestGeneration()) return
    templateFields.value = []
  } finally {
    if (generation === getAuthRequestGeneration()) isLoadingTemplateFields.value = false
  }
}

async function loadContentDependencies(item: ContentItemResponse) {
  const generation = getAuthRequestGeneration()
  isLoadingItemDependencies.value = true
  selectedItemDependencies.value = null

  try {
    selectedItemDependencies.value =
      await withinSession(fetchJson<ContentItemDependencyResponse>(item._links.dependencies.href), generation)
  } catch (error) {
    if (generation !== getAuthRequestGeneration()) return
    selectedItemDependencies.value = null
    pageError.value = getErrorMessage(error)
  } finally {
    if (generation === getAuthRequestGeneration()) isLoadingItemDependencies.value = false
  }
}

async function selectTemplate(templateId: string) {
  const generation = getAuthRequestGeneration()
  isNewTemplateDraftOpen.value = false

  if (selectedTemplateId.value === templateId && selectedTemplateDetail.value != null) {
    return
  }

  selectedTemplateId.value = templateId
  await withinSession(loadTemplateWorkspace(templateId), generation)
}

async function inspectSelectedItemTemplate() {
  const generation = getAuthRequestGeneration()
  if (selectedItem.value == null) {
    return
  }

  const template =
    availableTemplates.value.find(candidate => candidate.id === selectedItem.value?.templateId)

  if (template != null && !isAuthorVisibleTemplate(template)) {
    successMessage.value = 'The selected item uses a system-owned template that stays outside the authoring catalog.'
    return
  }

  await withinSession(selectTemplate(selectedItem.value.templateId), generation)
}

async function loadTemplateWorkspace(templateId: string) {
  const generation = getAuthRequestGeneration()
  pageError.value = null

  await withinSession(Promise.all([
    loadTemplateDetail(templateId),
    loadTemplateDependencies(templateId)
  ]), generation)

  if (selectedTemplateId.value === templateId && selectedTemplateDetail.value != null) {
    loadSelectedTemplateIntoDesigner()
  }
}

function onTemplateDesignerNameUpdate(value: string) {
  Object.assign(
    templateDesignerForm,
    updateTemplateDesignerFormName(templateDesignerForm, value)
  )
}

async function loadTemplateDetail(templateId: string) {
  const generation = getAuthRequestGeneration()
  isLoadingTemplateDetail.value = true

  try {
    selectedTemplateDetail.value =
      await withinSession(fetchJson<TemplateResponse>(`/api/v1/templates/${templateId}`), generation)
  } catch (error) {
    if (generation !== getAuthRequestGeneration()) return
    selectedTemplateDetail.value = null
    pageError.value = getErrorMessage(error)
  } finally {
    if (generation === getAuthRequestGeneration()) isLoadingTemplateDetail.value = false
  }
}

async function loadTemplateDependencies(templateId: string) {
  const generation = getAuthRequestGeneration()
  isLoadingTemplateDependencies.value = true

  try {
    selectedTemplateDependencies.value =
      await withinSession(fetchJson<TemplateDependencyResponse>(`/api/v1/templates/${templateId}/dependencies`), generation)
  } catch (error) {
    if (generation !== getAuthRequestGeneration()) return
    selectedTemplateDependencies.value = null
    pageError.value = getErrorMessage(error)
  } finally {
    if (generation === getAuthRequestGeneration()) isLoadingTemplateDependencies.value = false
  }
}

async function applyTemplateToCreate() {
  const generation = getAuthRequestGeneration()
  if (selectedTemplateSummary.value == null) {
    return
  }

  createForm.templateId = selectedTemplateSummary.value.id
  successMessage.value = `Create form now targets the ${selectedTemplateSummary.value.name} template.`
}

function startNewTemplateDraft() {
  isNewTemplateDraftOpen.value = true
  selectedTemplateId.value = null
  selectedTemplateDetail.value = null
  selectedTemplateDependencies.value = null

  const state = createNewTemplateDesignerState(
    getDefaultTemplateDesignerBaseTemplateId(availableTemplates.value)
  )
  Object.assign(templateDesignerForm, state.form)
  templateDraftSections.value = state.sections
}

function loadSelectedTemplateIntoDesigner() {
  if (selectedTemplateDetail.value == null) {
    return
  }

  const state = mapTemplateToDesignerState(selectedTemplateDetail.value)
  Object.assign(templateDesignerForm, state.form)
  templateDraftSections.value = state.sections
}

async function loadBaseTemplatePreview(templateId: string) {
  const generation = getAuthRequestGeneration()
  if (templateId.trim().length === 0) {
    selectedBaseTemplateDetail.value = null
    baseTemplatePreviewError.value = null
    isLoadingBaseTemplatePreview.value = false
    return
  }

  const template =
    availableTemplates.value.find(candidate => candidate.id === templateId)

  if (template != null && !isAuthorVisibleTemplate(template)) {
    selectedBaseTemplateDetail.value = null
    baseTemplatePreviewError.value = null
    isLoadingBaseTemplatePreview.value = false
    return
  }

  isLoadingBaseTemplatePreview.value = true
  baseTemplatePreviewError.value = null

  try {
    selectedBaseTemplateDetail.value =
      await withinSession(fetchJson<TemplateResponse>(`/api/v1/templates/${templateId}`), generation)
  } catch (error) {
    if (generation !== getAuthRequestGeneration()) return
    selectedBaseTemplateDetail.value = null
    baseTemplatePreviewError.value = getErrorMessage(error)
  } finally {
    if (generation === getAuthRequestGeneration()) isLoadingBaseTemplatePreview.value = false
  }
}

function addDraftSection() {
  templateDraftSections.value = addTemplateDraftSection(templateDraftSections.value)
}

function removeDraftSection(sectionId: string) {
  templateDraftSections.value = removeTemplateDraftSection(templateDraftSections.value, sectionId)
}

function addDraftField(sectionId: string) {
  templateDraftSections.value = addTemplateDraftField(templateDraftSections.value, sectionId)
}

function removeDraftField(sectionId: string, fieldId: string) {
  templateDraftSections.value = removeTemplateDraftField(templateDraftSections.value, sectionId, fieldId)
}

function updateTemplateSection(
  sectionId: string,
  update: Partial<TemplateDraftSection>
) {
  templateDraftSections.value = updateTemplateDraftSection(
    templateDraftSections.value,
    sectionId,
    update
  )
}

function updateTemplateField(
  sectionId: string,
  fieldId: string,
  update: Partial<TemplateDraftSection['fields'][number]>
) {
  templateDraftSections.value = updateTemplateDraftField(
    templateDraftSections.value,
    sectionId,
    fieldId,
    update
  )
}

function onTemplateSectionNameUpdate(sectionId: string, value: string) {
  templateDraftSections.value = updateTemplateDraftSectionName(
    templateDraftSections.value,
    sectionId,
    value
  )
}

function onTemplateSectionKeyUpdate(sectionId: string, value: string) {
  updateTemplateSection(sectionId, { key: value })
}

function onTemplateSectionSortOrderUpdate(sectionId: string, value: number) {
  updateTemplateSection(sectionId, { sortOrder: value })
}

function onTemplateFieldNameUpdate(sectionId: string, fieldId: string, value: string) {
  templateDraftSections.value = updateTemplateDraftFieldName(
    templateDraftSections.value,
    sectionId,
    fieldId,
    value
  )
}

function onTemplateFieldKeyUpdate(sectionId: string, fieldId: string, value: string) {
  updateTemplateField(sectionId, fieldId, { key: value })
}

function onTemplateFieldTypeUpdate(sectionId: string, fieldId: string, value: string) {
  updateTemplateField(sectionId, fieldId, { type: value })
}

function onTemplateFieldSharedUpdate(sectionId: string, fieldId: string, value: boolean) {
  updateTemplateField(sectionId, fieldId, { isShared: value })
}

function onTemplateFieldUnversionedUpdate(sectionId: string, fieldId: string, value: boolean) {
  updateTemplateField(sectionId, fieldId, { isUnversioned: value })
}

async function submitTemplateDesigner() {
  const generation = getAuthRequestGeneration()
  if (isSubmitting.value) {
    return
  }

  if (templateDesignerValidationErrors.value.length > 0) {
    pageError.value = templateDesignerValidationErrors.value[0] ?? null
    return
  }

  pageError.value = null
  successMessage.value = null
  isSubmitting.value = true

  try {
    const payload = buildTemplateDesignerPayload(
      templateDesignerForm,
      templateDraftSections.value,
      selectedBaseTemplateKey.value
    )

    const isEditing = templateDesignerForm.mode === 'edit' && templateDesignerForm.templateId.length > 0
    const response = await withinSession(fetchJson<TemplateResponse>(
      isEditing
        ? `/api/v1/templates/${templateDesignerForm.templateId}`
        : '/api/v1/templates',
      withJsonDefaults({
        method: isEditing ? 'PUT' : 'POST',
        body: JSON.stringify(payload)
      })
    ), generation)

    await withinSession(loadTemplates(), generation)
    selectedTemplateId.value = response.id
    await withinSession(loadTemplateWorkspace(response.id), generation)
    loadSelectedTemplateIntoDesigner()

    if (selectedItem.value?.templateId === response.id) {
      await withinSession(loadTemplateFields(selectedItem.value), generation)
    }

    successMessage.value = isEditing
      ? `Updated template ${response.name}.`
      : `Created template ${response.name}.`
  } catch (error) {
    if (generation !== getAuthRequestGeneration()) return
    pageError.value = getErrorMessage(error)
  } finally {
    if (generation === getAuthRequestGeneration()) isSubmitting.value = false
  }
}

async function refreshTemplateWorkspace() {
  const generation = getAuthRequestGeneration()
  if (selectedTemplateId.value == null) {
    return
  }

  await withinSession(loadTemplateWorkspace(selectedTemplateId.value), generation)
}

async function submitTemplateDelete() {
  const generation = getAuthRequestGeneration()
  if (isSubmitting.value || selectedTemplateSummary.value == null) {
    return
  }

  if (selectedTemplateDependencies.value?.canDelete === false) {
    pageError.value = `Template ${selectedTemplateSummary.value.name} still has dependent templates or content items.`
    return
  }

  if (!window.confirm(`Delete template ${selectedTemplateSummary.value.name}? This only works when nothing depends on it.`)) {
    return
  }

  pageError.value = null
  successMessage.value = null
  isSubmitting.value = true

  try {
    await withinSession(fetchWithNoContent(selectedTemplateSummary.value._links.self.href, {
      method: 'DELETE'
    }), generation)

    const deletedTemplateId = selectedTemplateSummary.value.id
    const deletedTemplateName = selectedTemplateSummary.value.name

    availableTemplates.value = availableTemplates.value
      .filter(template => template.id !== deletedTemplateId)
      .sort((left, right) => left.name.localeCompare(right.name) || left.key.localeCompare(right.key))

    if (createForm.templateId === deletedTemplateId) {
      createForm.templateId = getSuggestedTemplateId(availableTemplates.value)
    }

    if (templateDesignerForm.templateId === deletedTemplateId) {
      startNewTemplateDraft()
    }

    selectedTemplateId.value = visibleTemplates.value[0]?.id ?? null
    selectedTemplateDetail.value = null
    selectedTemplateDependencies.value = null

    if (selectedTemplateId.value != null) {
      await withinSession(loadTemplateWorkspace(selectedTemplateId.value), generation)
    }

    successMessage.value = `Deleted template ${deletedTemplateName}.`
  } catch (error) {
    if (generation !== getAuthRequestGeneration()) return
    pageError.value = getErrorMessage(error)
  } finally {
    if (generation === getAuthRequestGeneration()) isSubmitting.value = false
  }
}

function withContext(url: string) {
  return withRequestContext(url, language.value, version.value)
}

function setCheckboxValue(key: string, checked: boolean) {
  setCheckboxFieldValue(fieldForm, key, checked)
}

function setFieldValue(key: string, value: string | number | null | undefined) {
  setFieldFormValue(fieldForm, key, value)
}

function onCheckboxInput(key: string, checked: boolean) {
  setCheckboxValue(key, checked)
}

function onFieldInput(key: string, value: string) {
  setFieldValue(key, value)
}

</script>

<template>
  <p v-if="auth.state.value === 'checking'" role="status" class="p-8">Checking your session…</p>
  <LoginForm v-else-if="auth.state.value === 'anonymous'" :busy="auth.busy.value" :error="auth.error.value"
    @submit="auth.login($event.loginName, $event.password)" />
  <ChangePasswordForm v-else-if="auth.state.value === 'change-password'" :busy="auth.busy.value" :error="auth.error.value"
    @submit="auth.changePassword($event.currentPassword, $event.newPassword)" @logout="logoutWorkspace" />
  <p v-if="auth.state.value === 'authenticated' && !workspaceInitialized" role="status" class="p-8">Loading the workspace…</p>
  <div v-if="auth.state.value === 'authenticated' && auth.error.value" role="alert" class="bg-rose-50 p-3 text-rose-800">{{ auth.error.value }}</div>
  <AuthorWorkspace
    v-if="workspaceInitialized"
    v-show="auth.state.value === 'authenticated'"
    :user-name="auth.session.value?.loginName ?? 'TemplarAdmin'"
    @logout="logoutWorkspace"
    :active-workspace="activeWorkspace"
    :language="language"
    :version="version"
    :show-actions="showActions"
    :root-node="contentWorkspaceRoot"
    :selected-item="selectedItem"
    :selected-item-id="selectedItemId"
    :fields="editorFields"
    :field-form="fieldForm"
    :template-name="selectedItemTemplateName"
    :is-loading-tree="isBootstrapping"
    :is-loading-fields="isLoadingTemplateFields"
    :is-submitting="isSubmitting"
    :dependencies="selectedItemDependencies"
    :templates="visibleTemplates"
    :template-organization="treeActions.treeOrganization.value"
    :selected-template-id="selectedTemplateId"
    :selected-template="selectedTemplateDetail"
    :is-loading-templates="isLoadingTemplates"
    :available-field-types="availableFieldTypes"
    :page-error="pageError"
    :success-message="successMessage"
    @update:active-workspace="activeWorkspace = $event"
    @update:language="language = $event"
    @update:version="version = $event"
    @toggle-actions="showActions = !showActions"
    @close-actions="showActions = false"
    @select-node="selectNode"
    @toggle-node="toggleNode"
    @tree-menu="treeActions.open"
    @save="submitValues"
    @delete="submitDelete"
    @update-item-icon="updateSelectedItemIcon"
    @field-input="onFieldInput"
    @checkbox-input="onCheckboxInput"
    @select-template="selectTemplate"
    @update-template-icon="updateSelectedTemplateIcon"
    @update-template-base-template-ids="updateSelectedTemplateBaseTemplates"
    @save-template="saveSelectedTemplate"
  />

  <ContextMenu v-if="auth.state.value === 'authenticated' && treeActions.menu.value" v-bind="treeActions.menu.value" :actions="treeActions.actions.value"
    @close="treeActions.menu.value = null" @action="treeActions.choose" />
  <TreeActionDialog v-if="treeActions.dialog.value" v-bind="treeActions.dialog.value" :busy="treeActions.busy.value" :suspended="auth.state.value !== 'authenticated'"
    :error="treeActions.error.value" :destructive="treeActions.dialog.value.request.action === 'delete'"
    @cancel="treeActions.dialog.value = null" @submit="treeActions.submit" />
  <div v-if="auth.state.value === 'authenticated' && treeActions.error.value && !treeActions.dialog.value" role="alert" class="fixed bottom-12 right-4 z-50 max-w-md rounded border border-red-200 bg-white p-4 text-red-800">
    {{ treeActions.error.value }} <button type="button" aria-label="Dismiss action error" @click="treeActions.error.value = null">×</button>
  </div>

  <div v-if="false" class="workspace-shell">
    <TopBar
      class="workspace-toolbar"
      :language="language"
      :version="version"
      :show-actions="showActions"
      @update:language="language = $event"
      @update:version="version = $event"
      @toggle-actions="showActions = !showActions"
    />

    <nav class="workspace-rail" aria-label="Primary workspace areas">
      <button
        :class="['workspace-rail__item', { 'workspace-rail__item--active': activeWorkspace === 'content' }]"
        type="button"
        title="Content"
        aria-label="Content workspace"
        :aria-pressed="activeWorkspace === 'content'"
        @click="activeWorkspace = 'content'"
      >
        <span aria-hidden="true">▤</span>
        <span>Content</span>
      </button>
      <button
        :class="['workspace-rail__item', { 'workspace-rail__item--active': activeWorkspace === 'templates' }]"
        type="button"
        title="Templates"
        aria-label="Template workspace"
        :aria-pressed="activeWorkspace === 'templates'"
        @click="activeWorkspace = 'templates'"
      >
        <span aria-hidden="true">▧</span>
        <span>Templates</span>
      </button>
      <button
        :class="['workspace-rail__item', { 'workspace-rail__item--active': activeWorkspace === 'media' }]"
        type="button"
        title="Media"
        aria-label="Media workspace"
        :aria-pressed="activeWorkspace === 'media'"
        @click="activeWorkspace = 'media'"
      >
        <span aria-hidden="true">▣</span>
        <span>Media</span>
      </button>
      <button
        :class="['workspace-rail__item', { 'workspace-rail__item--active': activeWorkspace === 'system' }]"
        type="button"
        title="System"
        aria-label="System workspace"
        :aria-pressed="activeWorkspace === 'system'"
        @click="activeWorkspace = 'system'"
      >
        <span aria-hidden="true">⚙</span>
        <span>System</span>
      </button>
    </nav>

    <aside :class="['workspace-sidebar', { 'workspace-sidebar--figma-tree': activeWorkspace === 'content' }]">
      <template v-if="activeWorkspace === 'content'">
      <ContentTree
        :root-node="contentWorkspaceRoot"
        :selected-id="selectedItemId"
        :is-loading="isBootstrapping"
        @toggle="toggleNode"
        @select="selectNode"
      />
      </template>

      <template v-else-if="activeWorkspace === 'templates'">
        <div class="navigator-heading">
          <div>
            <p class="eyebrow">Template</p>
            <h2>Template</h2>
          </div>
          <span class="panel-pill">{{ visibleTemplates.length }}</span>
        </div>

        <button class="button workspace-sidebar__new-template" type="button" @click="startNewTemplateDraft">
          New template
        </button>

        <div v-if="isLoadingTemplates" class="empty-state">
          Loading templates...
        </div>

        <div v-else-if="visibleTemplates.length === 0" class="empty-state">
          No templates are available.
        </div>

        <ul v-else class="template-list workspace-template-list">
          <li v-for="template in visibleTemplates" :key="template.id">
            <button
              type="button"
              :class="['template-entry', { 'template-entry--selected': selectedTemplateId === template.id }]"
              @click="selectTemplate(template.id)"
            >
              <span class="template-entry__name">{{ template.name }}</span>
              <span class="template-entry__meta">{{ template.key }}</span>
            </button>
          </li>
        </ul>
      </template>

      <template v-else>
        <div class="navigator-heading">
          <div>
            <p class="eyebrow">{{ activeWorkspace }}</p>
            <h2>{{ activeWorkspace === 'media' ? 'Media' : 'System' }}</h2>
          </div>
        </div>

        <div class="empty-state">
          {{ activeWorkspace === 'media' ? 'Media items are not available yet.' : 'System items are not available yet.' }}
        </div>
      </template>
    </aside>

    <main class="workspace-main">
      <header class="top-panel">
        <template v-if="activeWorkspace === 'content'">
          <div class="top-panel__title">
            <p class="eyebrow">Content editor</p>
            <h2>{{ selectedItem?.name ?? 'Select a content item to begin.' }}</h2>
          </div>

          <dl v-if="selectedItem != null" class="top-panel__metadata" aria-label="Content item context">
            <div>
              <dt>Template</dt>
              <dd>{{ selectedItemTemplateName ?? 'Loading template' }}</dd>
            </div>
            <div>
              <dt>Language</dt>
              <dd>{{ language }}</dd>
            </div>
            <div>
              <dt>Version</dt>
              <dd>v{{ version }}</dd>
            </div>
            <div>
              <dt>Publishing</dt>
              <dd><span class="top-panel__status">Not published</span></dd>
            </div>
          </dl>
        </template>

        <template v-else-if="activeWorkspace === 'templates'">
          <div class="top-panel__title">
            <p class="eyebrow">Template workspace</p>
            <h2>{{ selectedTemplateSummary?.name ?? 'Template designer' }}</h2>
          </div>
        </template>

        <template v-else>
          <div class="top-panel__title">
            <p class="eyebrow">{{ activeWorkspace }}</p>
            <h2>{{ activeWorkspace === 'media' ? 'Media workspace' : 'System workspace' }}</h2>
          </div>
        </template>
      </header>

      <div v-if="pageError != null" class="banner banner--error">
        {{ pageError }}
      </div>

      <div v-if="successMessage != null" class="banner banner--success">
        {{ successMessage }}
      </div>

      <template v-if="activeWorkspace === 'content'">
        <section v-if="selectedItem != null" class="content-grid">
          <ContentEditor
            :item="selectedItem"
            :template-name="selectedItemTemplateName"
            :fields="editorFields"
            :field-form="fieldForm"
            :is-loading-fields="isLoadingTemplateFields"
            :is-submitting="isSubmitting"
            @save="submitValues"
            @field-input="onFieldInput"
            @checkbox-input="onCheckboxInput"
          />
          <ActionSidebar
            :item="selectedItem"
            :show="showActions"
            :is-submitting="isSubmitting"
            :can-delete="selectedItemDependencies?.canDelete ?? false"
            @close="showActions = false"
            @save="submitValues"
            @delete="submitDelete"
          />
        </section>

        <section v-if="selectedItem != null" class="panel composer-panel">
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
            <input v-model="createForm.name" type="text" required maxlength="255" />
          </label>

          <label class="field">
            <span>Key</span>
            <small class="field-meta">The server generates an SEO-friendly key from the authored name.</small>
          </label>

          <label class="field">
            <span>Template</span>
            <select v-model="createForm.templateId" :disabled="isLoadingTemplates || creatableTemplates.length === 0" required>
              <option disabled value="">
                {{ isLoadingTemplates ? 'Loading templates...' : 'Select a template' }}
              </option>
              <option
                v-for="template in creatableTemplates"
                :key="template.id"
                :value="template.id"
              >
                {{ template.name }} ({{ template.key }})
              </option>
            </select>
            <small class="field-meta">
              {{
                selectedCreateTemplate == null
                  ? 'Creatable template ids are loaded from /api/v1/templates.'
                  : `Selected id: ${selectedCreateTemplate?.id ?? ''}`
              }}
            </small>
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
      </template>

      <section
        v-else-if="activeWorkspace === 'templates' && (selectedTemplateId != null || isNewTemplateDraftOpen)"
        class="template-grid"
      >
        <TemplateCatalogPane
          :available-templates="visibleTemplates"
          :selected-template-id="selectedTemplateId"
          :is-loading-templates="isLoadingTemplates"
          :selected-item-available="selectedItem != null"
          @start-new-template-draft="startNewTemplateDraft"
          @inspect-selected-item-template="inspectSelectedItemTemplate"
          @refresh-template-workspace="refreshTemplateWorkspace"
          @select-template="selectTemplate"
        />

        <TemplateInspectorPane
          :selected-template-detail="selectedTemplateDetail"
          :selected-template-dependencies="selectedTemplateDependencies"
          :template-sections="templateSections"
          :selected-template-field-count="selectedTemplateFieldCount"
          :selected-item-template-id="selectedItem?.templateId ?? null"
          :is-loading-template-detail="isLoadingTemplateDetail"
          :is-loading-template-dependencies="isLoadingTemplateDependencies"
          :is-submitting="isSubmitting"
          @apply-template-to-create="applyTemplateToCreate"
          @submit-template-delete="submitTemplateDelete"
        />

              <TemplateDesignerPane
                :form="templateDesignerForm"
                :sections="templateDraftSections"
                :available-base-templates="availableBaseTemplates"
                :available-field-types="availableFieldTypes"
                :is-loading-field-types="isLoadingFieldTypes"
                :is-submitting="isSubmitting"
                :heading="templateDesignerHeading"
                :selected-template-loaded="selectedTemplateDetail != null"
                :base-template-key="baseTemplateVisibilityLabel"
                :validation-errors="templateDesignerValidationErrors"
                :inherited-template-sections="inheritedTemplateWorkspace.sections"
                :inherited-field-count="inheritedTemplateWorkspace.fieldCount"
                :is-loading-base-template-preview="isLoadingBaseTemplatePreview"
                :base-template-preview-error="baseTemplatePreviewError"
                @submit="submitTemplateDesigner"
                @reset-new-draft="startNewTemplateDraft"
                @load-selected-template="loadSelectedTemplateIntoDesigner"
                @update-name="onTemplateDesignerNameUpdate"
                @update-key="templateDesignerForm.key = $event"
                @update-base-template-id="templateDesignerForm.baseTemplateId = $event"
                @add-section="addDraftSection"
                @remove-section="removeDraftSection"
                @update-section-name="onTemplateSectionNameUpdate"
                @update-section-key="onTemplateSectionKeyUpdate"
                @update-section-sort-order="onTemplateSectionSortOrderUpdate"
                @add-field="addDraftField"
                @remove-field="removeDraftField"
                @update-field-name="onTemplateFieldNameUpdate"
                @update-field-key="onTemplateFieldKeyUpdate"
	                @update-field-type="onTemplateFieldTypeUpdate"
	                @update-field-shared="onTemplateFieldSharedUpdate"
	                @update-field-unversioned="onTemplateFieldUnversionedUpdate"
	              />
	      </section>
    </main>

    <StatusBar class="workspace-statusbar" :selected-item="selectedItem" />
  </div>
</template>
