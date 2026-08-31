<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import ContentInspectorPane from './components/ContentInspectorPane.vue'
import TemplateCatalogPane from './components/TemplateCatalogPane.vue'
import TemplateInspectorPane from './components/TemplateInspectorPane.vue'
import TemplateDesignerPane from './components/TemplateDesignerPane.vue'
import TreeBranch from './components/TreeBranch.vue'
import AuthorWorkspace from './components/AuthorWorkspace.vue'
import TopBar from './components/layout/TopBar.vue'
import NavRail from './components/layout/NavRail.vue'
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
import type { GeneralLinkDraft } from './types/general-link'
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
  getCheckboxFieldValue,
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
  normalizeGeneralLinkKind,
  parseGeneralLinkValue,
  updateGeneralLinkDraft as updateGeneralLinkDraftValue
} from './utils/general-link'
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

const treeCount = computed(() => countNodes(rootNodes.value))
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

onMounted(async () => {
  await loadFieldTypes()
  await loadTemplates()
  await refreshRootBranch()

  if (selectedTemplateId.value != null) {
    await loadTemplateWorkspace(selectedTemplateId.value)
  }
})

watch(
  () => templateDesignerForm.baseTemplateId,
  async baseTemplateId => {
    await loadBaseTemplatePreview(baseTemplateId)
  }
)

async function refreshRootBranch() {
  isBootstrapping.value = true
  pageError.value = null

  try {
    const branch = await getRootBranch()
    rootNodes.value = branch.item == null ? [] : [createWorkspaceRootNode(branch)]

    if (selectedItemId.value != null) {
      const currentNode = findTreeNodeById(rootNodes.value, selectedItemId.value)
      if (currentNode != null) {
        await syncInspectorFromItem(currentNode.item)
      } else {
        selectedItemId.value = null
        resetInspectorForms()
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
  await syncInspectorFromItem(node.item)

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
      return currentChild == null ? createTreeNode(branchChild) : currentChild
    }).sort((left, right) => left.item.path.localeCompare(right.item.path))
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
    const response = await sendMutation<ContentMutationResponse>('/api/v1/content', {
      method: 'POST',
      body: JSON.stringify({
        name: createForm.name,
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
    const response = await sendMutation<ContentMutationResponse>(selectedItem.value._links.rename.href, {
      method: 'POST',
      body: JSON.stringify({
        name: renameForm.name
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
    const response = await sendMutation<ContentMutationResponse>(selectedItem.value._links.move.href, {
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

async function submitDelete() {
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
    const response = await sendMutation<ContentMutationResponse>(itemToDelete._links.delete.href, {
      method: 'DELETE'
    })

    applyDeletedMutationResponse(response)
    successMessage.value = `Deleted ${response.item.name} and refreshed the affected branch.`
  } catch (error) {
    pageError.value = getErrorMessage(error)
  } finally {
    isSubmitting.value = false
  }
}

async function submitValues() {
  if (isSubmitting.value || selectedItem.value == null) {
    return
  }

  pageError.value = null
  successMessage.value = null
  isSubmitting.value = true

  try {
    const response = await fetchJson<ContentItemResponse>(
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
    )

    rootNodes.value = upsertTreeNode(rootNodes.value, extractParentIdFromHref(response._links.parent?.href), response)
    selectedItemId.value = response.id
    await syncInspectorFromItem(response)
    successMessage.value = `Saved ${Object.keys(fieldForm).length} field values for ${response.name}.`
  } catch (error) {
    pageError.value = getErrorMessage(error)
  } finally {
    isSubmitting.value = false
  }
}

async function updateSelectedItemIcon(icon: string | null) {
  if (isSubmitting.value || selectedItem.value == null) {
    return
  }

  isSubmitting.value = true
  pageError.value = null

  try {
    const response = await fetchJson<ContentItemResponse>(
      selectedItem.value._links.self.href,
      withJsonDefaults({
        method: 'PUT',
        body: JSON.stringify({ name: selectedItem.value.name, icon })
      })
    )

    rootNodes.value = upsertTreeNode(
      rootNodes.value,
      extractParentIdFromHref(response._links.parent?.href),
      response
    )
    await syncInspectorFromItem(response)
    successMessage.value = icon == null
      ? `Cleared the icon override for ${response.name}.`
      : `Updated the icon for ${response.name}.`
  } catch (error) {
    pageError.value = getErrorMessage(error)
  } finally {
    isSubmitting.value = false
  }
}

async function updateSelectedTemplateIcon(icon: string) {
  if (isSubmitting.value || selectedTemplateDetail.value == null) {
    return
  }

  const template = selectedTemplateDetail.value
  isSubmitting.value = true
  pageError.value = null

  try {
    const response = await fetchJson<TemplateResponse>(
      template._links.self.href,
      withJsonDefaults({
        method: 'PUT',
        body: JSON.stringify({
          name: template.name,
          key: template.key,
          icon,
          baseTemplateKeys: template.baseTemplate == null ? [] : [template.baseTemplate.key],
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
    )

    await loadTemplates()
    selectedTemplateDetail.value = response
    successMessage.value = `Updated the icon for ${response.name}.`
  } catch (error) {
    pageError.value = getErrorMessage(error)
  } finally {
    isSubmitting.value = false
  }
}

async function applyMutationResponse(response: ContentMutationResponse) {
  for (const affected of response.affectedBranches) {
    if (shouldApplyWorkspaceBranch(affected.branch)) {
      rootNodes.value = applyBranchToContentTree(rootNodes.value, affected.branch)
    }
  }

  const refreshedItem = await getItem(response.item.id)

  if (!isWithinContentWorkspace(refreshedItem.path)) {
    selectedItemId.value = null
    resetInspectorForms()
    return
  }

  rootNodes.value = upsertTreeNode(rootNodes.value, extractParentIdFromHref(refreshedItem._links.parent?.href), refreshedItem)
  selectedItemId.value = response.item.id
  await syncInspectorFromItem(refreshedItem)
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
  syncFormsFromItem(item)
  await Promise.all([
    loadTemplateFields(item),
    loadContentDependencies(item)
  ])
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
  return await fetchJson<ContentBranchResponse>(withContext('/api/v1/content/workspaces/content/branch'))
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
  isLoadingTemplates.value = true

  try {
    const response = await fetchJson<TemplateCollectionResponse>('/api/v1/templates')
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
    availableTemplates.value = []
    selectedTemplateId.value = null
  } finally {
    isLoadingTemplates.value = false
  }
}

async function loadFieldTypes() {
  isLoadingFieldTypes.value = true

  try {
    const response = await fetchJson<FieldTypeCollectionResponse>('/api/v1/field-types')
    availableFieldTypes.value = response.embedded.fieldTypes
  } catch (error) {
    availableFieldTypes.value = []
    pageError.value = getErrorMessage(error)
  } finally {
    isLoadingFieldTypes.value = false
  }
}

async function getBranch(itemId: string) {
  return await fetchJson<ContentBranchResponse>(withContext(`/api/v1/content/${itemId}/branch`))
}

async function getItem(itemId: string) {
  return await fetchJson<ContentItemResponse>(withContext(`/api/v1/content/${itemId}`))
}

async function loadTemplateFields(item: ContentItemResponse) {
  isLoadingTemplateFields.value = true

  try {
    const response =
      await fetchJson<TemplateFieldCollectionResponse>(`${item._links.template.href}/fields`)

    templateFields.value = response.embedded.fields
      .slice()
      .sort((left, right) =>
        left.sectionSortOrder - right.sectionSortOrder ||
        left.sectionName.localeCompare(right.sectionName) ||
        left.name.localeCompare(right.name))
  } catch {
    templateFields.value = []
  } finally {
    isLoadingTemplateFields.value = false
  }
}

async function loadContentDependencies(item: ContentItemResponse) {
  isLoadingItemDependencies.value = true
  selectedItemDependencies.value = null

  try {
    selectedItemDependencies.value =
      await fetchJson<ContentItemDependencyResponse>(item._links.dependencies.href)
  } catch (error) {
    selectedItemDependencies.value = null
    pageError.value = getErrorMessage(error)
  } finally {
    isLoadingItemDependencies.value = false
  }
}

async function selectTemplate(templateId: string) {
  isNewTemplateDraftOpen.value = false

  if (selectedTemplateId.value === templateId && selectedTemplateDetail.value != null) {
    return
  }

  selectedTemplateId.value = templateId
  await loadTemplateWorkspace(templateId)
}

async function inspectSelectedItemTemplate() {
  if (selectedItem.value == null) {
    return
  }

  const template =
    availableTemplates.value.find(candidate => candidate.id === selectedItem.value?.templateId)

  if (template != null && !isAuthorVisibleTemplate(template)) {
    successMessage.value = 'The selected item uses a system-owned template that stays outside the authoring catalog.'
    return
  }

  await selectTemplate(selectedItem.value.templateId)
}

async function loadTemplateWorkspace(templateId: string) {
  pageError.value = null

  await Promise.all([
    loadTemplateDetail(templateId),
    loadTemplateDependencies(templateId)
  ])
}

function onTemplateDesignerNameUpdate(value: string) {
  Object.assign(
    templateDesignerForm,
    updateTemplateDesignerFormName(templateDesignerForm, value)
  )
}

async function loadTemplateDetail(templateId: string) {
  isLoadingTemplateDetail.value = true

  try {
    selectedTemplateDetail.value =
      await fetchJson<TemplateResponse>(`/api/v1/templates/${templateId}`)
  } catch (error) {
    selectedTemplateDetail.value = null
    pageError.value = getErrorMessage(error)
  } finally {
    isLoadingTemplateDetail.value = false
  }
}

async function loadTemplateDependencies(templateId: string) {
  isLoadingTemplateDependencies.value = true

  try {
    selectedTemplateDependencies.value =
      await fetchJson<TemplateDependencyResponse>(`/api/v1/templates/${templateId}/dependencies`)
  } catch (error) {
    selectedTemplateDependencies.value = null
    pageError.value = getErrorMessage(error)
  } finally {
    isLoadingTemplateDependencies.value = false
  }
}

async function applyTemplateToCreate() {
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
      await fetchJson<TemplateResponse>(`/api/v1/templates/${templateId}`)
  } catch (error) {
    selectedBaseTemplateDetail.value = null
    baseTemplatePreviewError.value = getErrorMessage(error)
  } finally {
    isLoadingBaseTemplatePreview.value = false
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
    const response = await fetchJson<TemplateResponse>(
      isEditing
        ? `/api/v1/templates/${templateDesignerForm.templateId}`
        : '/api/v1/templates',
      withJsonDefaults({
        method: isEditing ? 'PUT' : 'POST',
        body: JSON.stringify(payload)
      })
    )

    await loadTemplates()
    selectedTemplateId.value = response.id
    await loadTemplateWorkspace(response.id)
    loadSelectedTemplateIntoDesigner()

    if (selectedItem.value?.templateId === response.id) {
      await loadTemplateFields(selectedItem.value)
    }

    successMessage.value = isEditing
      ? `Updated template ${response.name}.`
      : `Created template ${response.name}.`
  } catch (error) {
    pageError.value = getErrorMessage(error)
  } finally {
    isSubmitting.value = false
  }
}

async function refreshTemplateWorkspace() {
  if (selectedTemplateId.value == null) {
    return
  }

  await loadTemplateWorkspace(selectedTemplateId.value)
}

async function submitTemplateDelete() {
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
    await fetchWithNoContent(selectedTemplateSummary.value._links.self.href, {
      method: 'DELETE'
    })

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
      await loadTemplateWorkspace(selectedTemplateId.value)
    }

    successMessage.value = `Deleted template ${deletedTemplateName}.`
  } catch (error) {
    pageError.value = getErrorMessage(error)
  } finally {
    isSubmitting.value = false
  }
}

function withContext(url: string) {
  return withRequestContext(url, language.value, version.value)
}

function getCheckboxValue(key: string) {
  return getCheckboxFieldValue(fieldForm, key)
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

function getGeneralLinkDraft(key: string): GeneralLinkDraft {
  return parseGeneralLinkValue(fieldForm[key])
}

function updateGeneralLinkDraft(
  key: string,
  update: Partial<GeneralLinkDraft>)
{
  fieldForm[key] = updateGeneralLinkDraftValue(fieldForm[key], update)
}

function onGeneralLinkKindInput(key: string, value: string) {
  const kind = normalizeGeneralLinkKind(value)
  updateGeneralLinkDraft(key, { kind })
}

function onGeneralLinkItemIdInput(key: string, value: string) {
  updateGeneralLinkDraft(key, { itemId: value })
}

function onGeneralLinkUrlInput(key: string, value: string) {
  updateGeneralLinkDraft(key, { url: value })
}

function onGeneralLinkTextInput(key: string, value: string) {
  updateGeneralLinkDraft(key, { text: value })
}

function onGeneralLinkTargetInput(key: string, value: string) {
  updateGeneralLinkDraft(key, { target: value })
}

function countNodes(nodes: TreeNode[]): number {
  return nodes.reduce((total, node) => total + 1 + countNodes(node.children), 0)
}
</script>

<template>
  <AuthorWorkspace
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
    :selected-template-id="selectedTemplateId"
    :selected-template="selectedTemplateDetail"
    :is-loading-templates="isLoadingTemplates"
    :page-error="pageError"
    :success-message="successMessage"
    @update:active-workspace="activeWorkspace = $event"
    @update:language="language = $event"
    @update:version="version = $event"
    @toggle-actions="showActions = !showActions"
    @close-actions="showActions = false"
    @select-node="selectNode"
    @toggle-node="toggleNode"
    @save="submitValues"
    @delete="submitDelete"
    @update-item-icon="updateSelectedItemIcon"
    @field-input="onFieldInput"
    @checkbox-input="onCheckboxInput"
    @select-template="selectTemplate"
    @update-template-icon="updateSelectedTemplateIcon"
  />

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
            <input v-model="createForm.name" type="text" required />
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
