<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import ContentInspectorPane from './components/ContentInspectorPane.vue'
import TemplateCatalogPane from './components/TemplateCatalogPane.vue'
import TemplateInspectorPane from './components/TemplateInspectorPane.vue'
import TemplateDesignerPane from './components/TemplateDesignerPane.vue'
import TreeBranch from './components/TreeBranch.vue'
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
  getSuggestedCreateTemplateId,
  getSuggestedTemplateId,
  getTemplateIdByKey,
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
  createFieldTypeLookup,
  getFieldTypeLabel as getEditorFieldTypeLabel
} from './utils/editor-fields'
import {
  getCheckboxFieldValue,
  normalizeFieldValue,
  normalizeOptionalValue,
  readCheckboxEventValue,
  readInputEventValue,
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
  mapTemplateToDesignerState,
  removeTemplateDraftField,
  removeTemplateDraftSection,
  validateTemplateDesignerState
} from './utils/template-designer'

const language = ref('en')
const version = ref(1)
const isBootstrapping = ref(false)
const isSubmitting = ref(false)
const pageError = ref<string | null>(null)
const successMessage = ref<string | null>(null)

const rootNodes = ref<TreeNode[]>([])
const selectedItemId = ref<string | null>(null)
const selectedNode = computed(() => findTreeNodeById(rootNodes.value, selectedItemId.value))
const selectedItem = computed(() => selectedNode.value?.item ?? null)

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
  availableTemplates.value.find(template => template.id === createForm.templateId) ?? null)
const selectedTemplateSummary = computed(() =>
  selectedTemplateId.value == null
    ? null
    : availableTemplates.value.find(template => template.id === selectedTemplateId.value) ?? null)
const templateWorkspace = computed(() =>
  buildTemplateWorkspaceViewModel(selectedTemplateDetail.value, fieldTypeLookup.value))
const templateSections = computed(() => templateWorkspace.value.sections)
const selectedTemplateFieldCount = computed(() => templateWorkspace.value.fieldCount)
const availableBaseTemplates = computed(() =>
  availableTemplates.value.filter(template => template.id !== templateDesignerForm.templateId))
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
  startNewTemplateDraft()
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
    rootNodes.value = branch.embedded.children.map(createTreeNode)

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

async function applyMutationResponse(response: ContentMutationResponse) {
  for (const affected of response.affectedBranches) {
    rootNodes.value = applyBranchToContentTree(rootNodes.value, affected.branch)
  }

  const refreshedItem = await getItem(response.item.id)
  rootNodes.value = upsertTreeNode(rootNodes.value, extractParentIdFromHref(refreshedItem._links.parent?.href), refreshedItem)
  selectedItemId.value = response.item.id
  await syncInspectorFromItem(refreshedItem)
}

function applyDeletedMutationResponse(response: ContentMutationResponse) {
  for (const affected of response.affectedBranches) {
    rootNodes.value = applyBranchToContentTree(rootNodes.value, affected.branch)
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
  return await fetchJson<ContentBranchResponse>(withContext('/api/v1/content/root/branch'))
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

    if (selectedTemplateId.value == null) {
      selectedTemplateId.value = availableTemplates.value[0]?.id ?? null
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

  await selectTemplate(selectedItem.value.templateId)
}

async function loadTemplateWorkspace(templateId: string) {
  pageError.value = null

  await Promise.all([
    loadTemplateDetail(templateId),
    loadTemplateDependencies(templateId)
  ])
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
  const state = createNewTemplateDesignerState()
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
  templateDraftSections.value = templateDraftSections.value.map(section =>
    section.id === sectionId
      ? {
          ...section,
          ...update
        }
      : section)
}

function updateTemplateField(
  sectionId: string,
  fieldId: string,
  update: Partial<TemplateDraftSection['fields'][number]>
) {
  templateDraftSections.value = templateDraftSections.value.map(section =>
    section.id !== sectionId
      ? section
      : {
          ...section,
          fields: section.fields.map(field =>
            field.id === fieldId
              ? {
                  ...field,
                  ...update
                }
              : field)
        })
}

function onTemplateSectionNameUpdate(sectionId: string, value: string) {
  updateTemplateSection(sectionId, { name: value })
}

function onTemplateSectionKeyUpdate(sectionId: string, value: string) {
  updateTemplateSection(sectionId, { key: value })
}

function onTemplateSectionSortOrderUpdate(sectionId: string, value: number) {
  updateTemplateSection(sectionId, { sortOrder: value })
}

function onTemplateFieldNameUpdate(sectionId: string, fieldId: string, value: string) {
  updateTemplateField(sectionId, fieldId, { name: value })
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

    selectedTemplateId.value = availableTemplates.value[0]?.id ?? null
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

function getFieldTypeLabel(fieldType: string) {
  return getEditorFieldTypeLabel(fieldType, fieldTypeLookup.value)
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
        <h2>Patch only the branches that changed.</h2>
        <p>
          Create, rename, move, and delete actions refresh the affected tree edges
          instead of forcing a full explorer reload.
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
          <p class="eyebrow">Workspace</p>
          <h2>Content, schema, and authoring in one view.</h2>
        </div>
        <p class="top-panel__copy">
          The navigator stays anchored on the left while the inspector and template tools
          stay within reach on the right.
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

        <ContentInspectorPane
          :selected-item="selectedItem"
          :selected-item-template-name="selectedItemTemplateName"
          :is-loading-template-fields="isLoadingTemplateFields"
          :editor-fields="editorFields"
          :field-form="fieldForm"
          :is-submitting="isSubmitting"
          :rename-name="renameForm.name"
          :move-parent-id="moveForm.parentId"
          :selected-item-dependencies="selectedItemDependencies"
          :is-loading-item-dependencies="isLoadingItemDependencies"
          :get-general-link-draft="getGeneralLinkDraft"
          :get-checkbox-value="getCheckboxValue"
          @submit-values="submitValues"
          @submit-rename="submitRename"
          @submit-move="submitMove"
          @submit-delete="submitDelete"
          @update-rename-name="renameForm.name = $event"
          @update-move-parent-id="moveForm.parentId = $event"
          @field-input="onFieldInput"
          @checkbox-input="onCheckboxInput"
          @general-link-kind-input="onGeneralLinkKindInput"
          @general-link-item-id-input="onGeneralLinkItemIdInput"
          @general-link-url-input="onGeneralLinkUrlInput"
          @general-link-text-input="onGeneralLinkTextInput"
          @general-link-target-input="onGeneralLinkTargetInput"
        />
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
            <small class="field-meta">The server generates an SEO-friendly key from the authored name.</small>
          </label>

          <label class="field">
            <span>Template</span>
            <select v-model="createForm.templateId" :disabled="isLoadingTemplates || availableTemplates.length === 0" required>
              <option disabled value="">
                {{ isLoadingTemplates ? 'Loading templates...' : 'Select a template' }}
              </option>
              <option
                v-for="template in availableTemplates"
                :key="template.id"
                :value="template.id"
              >
                {{ template.name }} ({{ template.key }})
              </option>
            </select>
            <small class="field-meta">
              {{ selectedCreateTemplate == null ? 'Template ids are loaded from /api/v1/templates.' : `Selected id: ${selectedCreateTemplate.id}` }}
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

      <section class="template-grid">
        <TemplateCatalogPane
          :available-templates="availableTemplates"
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
                :base-template-key="selectedBaseTemplateKey"
                :validation-errors="templateDesignerValidationErrors"
                :inherited-template-sections="inheritedTemplateWorkspace.sections"
                :inherited-field-count="inheritedTemplateWorkspace.fieldCount"
                :is-loading-base-template-preview="isLoadingBaseTemplatePreview"
                :base-template-preview-error="baseTemplatePreviewError"
                @submit="submitTemplateDesigner"
                @reset-new-draft="startNewTemplateDraft"
                @load-selected-template="loadSelectedTemplateIntoDesigner"
                @update-name="templateDesignerForm.name = $event"
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
	  </div>
	</template>
