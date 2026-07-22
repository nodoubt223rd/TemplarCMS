<script setup lang="ts">
import { computed, defineComponent, onMounted, reactive, ref } from 'vue'
import type { PropType } from 'vue'
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
import type { EditorFieldModel, TreeNode } from './types/admin-ui'
import {
  buildEditorFields,
  createFieldTypeLookup,
  getFieldTypeLabel as getEditorFieldTypeLabel
} from './utils/editor-fields'
import { buildTemplateWorkspaceViewModel } from './utils/template-workspace'
import {
  normalizeGeneralLinkKind,
  parseGeneralLinkValue,
  updateGeneralLinkDraft as updateGeneralLinkDraftValue
} from './utils/general-link'

type TemplateDesignerMode = 'create' | 'edit'

type TemplateDraftField = {
  id: string
  name: string
  key: string
  type: string
  isShared: boolean
  isUnversioned: boolean
}

type TemplateDraftSection = {
  id: string
  name: string
  key: string
  sortOrder: number
  fields: TemplateDraftField[]
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
const templateDesignerForm = reactive({
  mode: 'create' as TemplateDesignerMode,
  templateId: '',
  name: '',
  key: '',
  baseTemplateId: ''
})
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
  startNewTemplateDraft()
  await loadFieldTypes()
  await loadTemplates()
  await refreshRootBranch()

  if (selectedTemplateId.value != null) {
    await loadTemplateWorkspace(selectedTemplateId.value)
  }
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
    const response = await sendMutation(itemToDelete._links.delete.href, {
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

    upsertNode(extractParentIdFromHref(response._links.parent?.href), response)
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
    applyBranchToTree(affected.branch)
  }

  const refreshedItem = await getItem(response.item.id)
  upsertNode(extractParentIdFromHref(refreshedItem._links.parent?.href), refreshedItem)
  selectedItemId.value = response.item.id
  await syncInspectorFromItem(refreshedItem)
}

function applyDeletedMutationResponse(response: ContentMutationResponse) {
  for (const affected of response.affectedBranches) {
    applyBranchToTree(affected.branch)
  }

  const parentId = extractParentIdFromHref(response.item._links.parent?.href)

  if (parentId == null) {
    selectedItemId.value = null
    resetInspectorForms()
    return
  }

  const parentNode = findNodeById(rootNodes.value, parentId)

  if (parentNode == null) {
    selectedItemId.value = null
    resetInspectorForms()
    return
  }

  selectedItemId.value = parentNode.item.id
  void syncInspectorFromItem(parentNode.item)
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
  moveForm.parentId = extractParentIdFromHref(item._links.parent?.href) ?? ''
  createForm.parentId = item.id
  ensureCreateTemplateSelection(item)
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
  createForm.name = ''
  createForm.templateId = getSuggestedTemplateId()
}

function resetInspectorForms() {
  renameForm.name = ''
  moveForm.parentId = ''
  createForm.parentId = ''
  clearFieldForm()
  templateFields.value = []
  selectedItemDependencies.value = null
}

function syncFieldForm(item: ContentItemResponse) {
  clearFieldForm()

  for (const [key, value] of Object.entries(item.fields)) {
    fieldForm[key] = value ?? ''
  }
}

function clearFieldForm() {
  for (const key of Object.keys(fieldForm)) {
    delete fieldForm[key]
  }
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
      createForm.templateId = getSuggestedTemplateId()
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
  templateDesignerForm.mode = 'create'
  templateDesignerForm.templateId = ''
  templateDesignerForm.name = ''
  templateDesignerForm.key = ''
  templateDesignerForm.baseTemplateId = ''
  templateDraftSections.value = [createDraftSection()]
}

function loadSelectedTemplateIntoDesigner() {
  if (selectedTemplateDetail.value == null) {
    return
  }

  templateDesignerForm.mode = 'edit'
  templateDesignerForm.templateId = selectedTemplateDetail.value.id
  templateDesignerForm.name = selectedTemplateDetail.value.name
  templateDesignerForm.key = selectedTemplateDetail.value.key
  templateDesignerForm.baseTemplateId = selectedTemplateDetail.value.baseTemplate?.id ?? ''
  templateDraftSections.value = selectedTemplateDetail.value.sections.map(section => ({
    id: section.id,
    name: section.name,
    key: section.key,
    sortOrder: section.sortOrder,
    fields: section.fields.map(field => ({
      id: field.id,
      name: field.name,
      key: field.key,
      type: field.type,
      isShared: field.isShared,
      isUnversioned: field.isUnversioned
    }))
  }))

  if (templateDraftSections.value.length === 0) {
    templateDraftSections.value = [createDraftSection()]
  }
}

function addDraftSection() {
  templateDraftSections.value = [...templateDraftSections.value, createDraftSection()]
}

function removeDraftSection(sectionId: string) {
  templateDraftSections.value =
    templateDraftSections.value.filter(section => section.id !== sectionId)

  if (templateDraftSections.value.length === 0) {
    templateDraftSections.value = [createDraftSection()]
  }
}

function addDraftField(sectionId: string) {
  templateDraftSections.value = templateDraftSections.value.map(section =>
    section.id !== sectionId
      ? section
      : {
          ...section,
          fields: [...section.fields, createDraftField()]
        })
}

function removeDraftField(sectionId: string, fieldId: string) {
  templateDraftSections.value = templateDraftSections.value.map(section => {
    if (section.id !== sectionId) {
      return section
    }

    const remainingFields = section.fields.filter(field => field.id !== fieldId)

    return {
      ...section,
      fields: remainingFields.length === 0 ? [createDraftField()] : remainingFields
    }
  })
}

async function submitTemplateDesigner() {
  if (isSubmitting.value) {
    return
  }

  const baseTemplateKey = getTemplateKeyById(templateDesignerForm.baseTemplateId)

  if (templateDesignerForm.baseTemplateId.length > 0 && baseTemplateKey == null) {
    pageError.value = 'The selected base template could not be resolved.'
    return
  }

  pageError.value = null
  successMessage.value = null
  isSubmitting.value = true

  try {
    const payload = {
      name: templateDesignerForm.name,
      key: templateDesignerForm.key,
      baseTemplateKeys: baseTemplateKey == null ? [] : [baseTemplateKey],
      sections: templateDraftSections.value.map(section => ({
        name: section.name,
        key: section.key,
        sortOrder: Number(section.sortOrder),
        fields: section.fields.map(field => ({
          name: field.name,
          key: field.key,
          type: field.type,
          isShared: field.isShared,
          isUnversioned: field.isShared ? false : field.isUnversioned
        }))
      }))
    }

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
      createForm.templateId = getSuggestedTemplateId()
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

async function fetchWithNoContent(url: string, init?: RequestInit) {
  const response = await fetch(url, init)

  if (!response.ok) {
    const problem = await response.json().catch(() => null) as { detail?: string; title?: string } | null
    throw new Error(problem?.detail ?? problem?.title ?? `Request failed with ${response.status}.`)
  }
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

function normalizeFieldValue(value: string) {
  return value.length === 0 ? null : value
}

function getCheckboxValue(key: string) {
  return fieldForm[key]?.trim().toLowerCase() === 'true'
}

function setCheckboxValue(key: string, checked: boolean) {
  fieldForm[key] = checked ? 'true' : 'false'
}

function setFieldValue(key: string, value: string | number | null | undefined) {
  fieldForm[key] = value == null ? '' : String(value)
}

function onCheckboxInput(key: string, event: Event) {
  const target = event.target as HTMLInputElement | null
  setCheckboxValue(key, target?.checked ?? false)
}

function onFieldInput(key: string, event: Event) {
  const target = event.target as HTMLInputElement | null
  setFieldValue(key, target?.value ?? '')
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

function onGeneralLinkKindInput(key: string, event: Event) {
  const target = event.target as HTMLSelectElement | null
  const kind = normalizeGeneralLinkKind(target?.value)
  updateGeneralLinkDraft(key, { kind })
}

function onGeneralLinkItemIdInput(key: string, event: Event) {
  const target = event.target as HTMLInputElement | null
  updateGeneralLinkDraft(key, { itemId: target?.value ?? '' })
}

function onGeneralLinkUrlInput(key: string, event: Event) {
  const target = event.target as HTMLInputElement | null
  updateGeneralLinkDraft(key, { url: target?.value ?? '' })
}

function onGeneralLinkTextInput(key: string, event: Event) {
  const target = event.target as HTMLInputElement | null
  updateGeneralLinkDraft(key, { text: target?.value ?? '' })
}

function onGeneralLinkTargetInput(key: string, event: Event) {
  const target = event.target as HTMLInputElement | null
  updateGeneralLinkDraft(key, { target: target?.value ?? '' })
}

function ensureCreateTemplateSelection(item: ContentItemResponse) {
  if (availableTemplates.value.length === 0) {
    return
  }

  const itemTemplate =
    availableTemplates.value.find(template => template.id === item.templateId)

  if (itemTemplate?.key === 'folder') {
    createForm.templateId = getTemplateIdByKey('item') ?? itemTemplate.id
    return
  }

  createForm.templateId = itemTemplate?.id ?? getSuggestedTemplateId()
}

function getSuggestedTemplateId() {
  return getTemplateIdByKey('item')
    ?? getTemplateIdByKey('folder')
    ?? availableTemplates.value[0]?.id
    ?? ''
}

function getTemplateIdByKey(key: string) {
  return availableTemplates.value.find(template => template.key === key)?.id ?? null
}

function getTemplateKeyById(id: string) {
  return availableTemplates.value.find(template => template.id === id)?.key ?? null
}

function getFieldTypeLabel(fieldType: string) {
  return getEditorFieldTypeLabel(fieldType, fieldTypeLookup.value)
}

function createDraftSection(): TemplateDraftSection {
  return {
    id: crypto.randomUUID(),
    name: '',
    key: '',
    sortOrder: 100,
    fields: [createDraftField()]
  }
}

function createDraftField(): TemplateDraftField {
  return {
    id: crypto.randomUUID(),
    name: '',
    key: '',
    type: 'SingleLineText',
    isShared: false,
    isUnversioned: false
  }
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
          Expand a node to load its branch, select an item to rename, move, or delete it,
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
                  <dd>{{ selectedItemTemplateName == null ? selectedItem.templateId : `${selectedItemTemplateName} (${selectedItem.templateId})` }}</dd>
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
              <form class="editor-card" @submit.prevent="submitValues">
                <div class="editor-card__header">
                  <div>
                    <p class="eyebrow">Fields</p>
                    <h4>Edit resolved field values</h4>
                  </div>
                  <span class="callout">Uses the language/version value contract</span>
                </div>

                <div v-if="isLoadingTemplateFields" class="empty-state empty-state--compact">
                  Loading template field metadata...
                </div>

                <div v-else-if="editorFields.length === 0" class="empty-state empty-state--compact">
                  No editable fields were returned for this item in the current context.
                </div>

                <template v-else>
                  <div
                    v-for="field in editorFields"
                    :key="field.key"
                    class="field-editor"
                  >
                    <label class="field">
                      <span>{{ field.label }}</span>
                      <small class="field-meta">
                        {{ field.sectionName }} · {{ field.type }} · {{ field.scopeLabel }}
                      </small>
                      <small v-if="field.helpText != null" class="field-help">
                        {{ field.helpText }}
                      </small>
                      <div v-if="field.editorKind === 'general-link'" class="general-link-editor">
                        <label class="field">
                          <span>Link Kind</span>
                          <select
                            :value="getGeneralLinkDraft(field.key).kind"
                            @change="onGeneralLinkKindInput(field.key, $event)"
                          >
                            <option value="external">External URL</option>
                            <option value="internal">Internal Content Item</option>
                          </select>
                        </label>

                        <label v-if="getGeneralLinkDraft(field.key).kind === 'internal'" class="field">
                          <span>Content Item Id</span>
                          <input
                            :value="getGeneralLinkDraft(field.key).itemId"
                            type="text"
                            placeholder="Enter content item GUID"
                            @input="onGeneralLinkItemIdInput(field.key, $event)"
                          />
                        </label>

                        <label v-else class="field">
                          <span>External URL</span>
                          <input
                            :value="getGeneralLinkDraft(field.key).url"
                            type="url"
                            placeholder="https://example.com"
                            @input="onGeneralLinkUrlInput(field.key, $event)"
                          />
                        </label>

                        <label class="field">
                          <span>Link Text</span>
                          <input
                            :value="getGeneralLinkDraft(field.key).text"
                            type="text"
                            placeholder="Optional label"
                            @input="onGeneralLinkTextInput(field.key, $event)"
                          />
                        </label>

                        <label class="field">
                          <span>Target</span>
                          <input
                            :value="getGeneralLinkDraft(field.key).target"
                            type="text"
                            placeholder="_self or _blank"
                            @input="onGeneralLinkTargetInput(field.key, $event)"
                          />
                        </label>

                        <small
                          v-if="getGeneralLinkDraft(field.key).parseWarning != null"
                          class="field-help"
                        >
                          {{ getGeneralLinkDraft(field.key).parseWarning }}
                        </small>
                      </div>
                      <label v-if="field.editorKind === 'checkbox'" class="checkbox-field checkbox-field--editor">
                        <input
                          :checked="getCheckboxValue(field.key)"
                          type="checkbox"
                          @change="onCheckboxInput(field.key, $event)"
                        />
                        <span>Enabled</span>
                      </label>
                      <textarea
                        v-else-if="field.editorKind === 'textarea'"
                        v-model="fieldForm[field.key]"
                        class="field-textarea"
                        :placeholder="field.placeholder ?? undefined"
                        :rows="field.rows ?? 3"
                      />
                      <input
                        v-else-if="field.editorKind !== 'general-link'"
                        :value="fieldForm[field.key]"
                        :type="field.inputType"
                        :placeholder="field.placeholder ?? undefined"
                        :step="field.step ?? undefined"
                        @input="onFieldInput(field.key, $event)"
                      />
                    </label>
                  </div>

                  <button class="button" type="submit" :disabled="isSubmitting">
                    Save Values
                  </button>
                </template>
              </form>

              <form class="editor-card" @submit.prevent="submitRename">
                <div class="editor-card__header">
                  <div>
                    <p class="eyebrow">Rename</p>
                    <h4>Update display name</h4>
                  </div>
                  <span class="callout">Key is generated from the name</span>
                </div>

                <label class="field">
                  <span>Name</span>
                  <input v-model="renameForm.name" type="text" required />
                </label>

                <label class="field">
                  <span>Key</span>
                  <small class="field-meta">The server generates an SEO-friendly key from the current name.</small>
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

              <form class="editor-card" @submit.prevent="submitDelete">
                <div class="editor-card__header">
                  <div>
                    <p class="eyebrow">Delete</p>
                    <h4>Safe delete preflight</h4>
                  </div>
                  <span
                    v-if="selectedItemDependencies != null"
                    :class="['callout', selectedItemDependencies.canDelete ? 'callout--success' : 'callout--danger']"
                  >
                    {{ selectedItemDependencies.canDelete ? 'Delete ready' : 'Delete blocked' }}
                  </span>
                  <span v-else class="callout">Checks child dependencies</span>
                </div>

                <div v-if="isLoadingItemDependencies" class="empty-state empty-state--compact">
                  Loading child dependency state...
                </div>

                <template v-else-if="selectedItemDependencies != null">
                  <div class="dependency-summary">
                    <div class="dependency-stat">
                      <strong>{{ selectedItemDependencies.summary.directChildCount }}</strong>
                      <span>direct children</span>
                    </div>
                  </div>

                  <section class="dependency-card">
                    <h5>Direct children</h5>
                    <ul v-if="selectedItemDependencies.embedded.children.length > 0" class="dependency-list">
                      <li
                        v-for="child in selectedItemDependencies.embedded.children"
                        :key="child.id"
                      >
                        {{ child.name }} · {{ child.path }}
                      </li>
                    </ul>
                    <p v-else class="dependency-empty">This item does not currently have any direct children.</p>
                  </section>
                </template>

                <button
                  class="button button--danger"
                  type="submit"
                  :disabled="isSubmitting || isLoadingItemDependencies || selectedItemDependencies?.canDelete !== true"
                >
                  Delete Item
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
        <article class="panel template-panel">
          <div class="panel-header">
            <div>
              <p class="eyebrow">Templates</p>
              <h3>Catalog</h3>
            </div>
            <span class="panel-pill">{{ availableTemplates.length }} templates</span>
          </div>

          <div class="template-actions">
            <button
              class="button"
              type="button"
              @click="startNewTemplateDraft"
            >
              New Template Draft
            </button>
            <button
              class="button button--secondary"
              type="button"
              :disabled="selectedItem == null"
              @click="inspectSelectedItemTemplate"
            >
              Inspect Selected Item Template
            </button>
            <button
              class="button button--secondary"
              type="button"
              :disabled="selectedTemplateId == null"
              @click="refreshTemplateWorkspace"
            >
              Refresh Template Detail
            </button>
          </div>

          <div v-if="isLoadingTemplates" class="empty-state">
            Loading templates from `/api/v1/templates`...
          </div>

          <div v-else-if="availableTemplates.length === 0" class="empty-state">
            No templates are currently available to inspect.
          </div>

          <ul v-else class="template-list">
            <li v-for="template in availableTemplates" :key="template.id">
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
        </article>

        <article class="panel inspector-panel">
          <div class="panel-header">
            <div>
              <p class="eyebrow">Template Inspector</p>
              <h3>{{ selectedTemplateDetail == null ? 'Select a template' : selectedTemplateDetail.name }}</h3>
            </div>
            <span class="panel-pill">
              {{ selectedTemplateDetail == null ? 'Idle' : `${templateSections.length} sections · ${selectedTemplateFieldCount} fields` }}
            </span>
          </div>

          <div
            v-if="isLoadingTemplateDetail || isLoadingTemplateDependencies"
            class="empty-state"
          >
            Loading template detail and dependency state...
          </div>

          <div
            v-else-if="selectedTemplateDetail == null || selectedTemplateDependencies == null"
            class="empty-state"
          >
            Pick a template to inspect its structure, field inventory, and delete blockers.
          </div>

          <template v-else>
            <section class="summary-card">
              <dl class="summary-grid">
                <div>
                  <dt>Id</dt>
                  <dd>{{ selectedTemplateDetail.id }}</dd>
                </div>
                <div>
                  <dt>Key</dt>
                  <dd>{{ selectedTemplateDetail.key }}</dd>
                </div>
                <div>
                  <dt>Sections</dt>
                  <dd>{{ templateSections.length }}</dd>
                </div>
                <div>
                  <dt>Fields</dt>
                  <dd>{{ selectedTemplateFieldCount }}</dd>
                </div>
              </dl>
            </section>

            <section class="form-stack">
              <div class="editor-card">
                <div class="editor-card__header">
                  <div>
                    <p class="eyebrow">Schema</p>
                    <h4>Template sections and fields</h4>
                  </div>
                  <span class="callout">
                    {{ selectedItem?.templateId === selectedTemplateDetail.id ? 'Matches current item' : 'Available for create flow' }}
                  </span>
                </div>

                <div class="template-action-row">
                  <button class="button" type="button" @click="applyTemplateToCreate">
                    Use Template In Create Form
                  </button>
                </div>

                <div v-if="templateSections.length === 0" class="empty-state empty-state--compact">
                  This template does not currently expose any sections or fields.
                </div>

                <div v-else class="template-section-stack">
                  <section
                    v-for="section in templateSections"
                    :key="section.id"
                    class="template-section-card"
                  >
                    <div class="template-section-card__header">
                      <div>
                        <h5>{{ section.name }}</h5>
                        <p>{{ section.key }}</p>
                      </div>
                      <span class="callout">{{ section.fields.length }} fields</span>
                    </div>

                    <ul class="template-field-list">
                      <li
                        v-for="field in section.fields"
                        :key="field.id"
                        class="template-field-item"
                      >
                        <div>
                          <strong>{{ field.name }}</strong>
                          <p>{{ field.key }}</p>
                        </div>
                        <span class="template-field-item__meta">{{ field.type }} · {{ field.scopeLabel }}</span>
                      </li>
                    </ul>
                  </section>
                </div>
              </div>

              <div class="editor-card">
                <div class="editor-card__header">
                  <div>
                    <p class="eyebrow">Dependencies</p>
                    <h4>Safe delete preflight</h4>
                  </div>
                  <span :class="['callout', selectedTemplateDependencies.canDelete ? 'callout--success' : 'callout--danger']">
                    {{ selectedTemplateDependencies.canDelete ? 'Delete ready' : 'Delete blocked' }}
                  </span>
                </div>

                <div class="dependency-summary">
                  <div class="dependency-stat">
                    <strong>{{ selectedTemplateDependencies.summary.dependentTemplateCount }}</strong>
                    <span>dependent templates</span>
                  </div>
                  <div class="dependency-stat">
                    <strong>{{ selectedTemplateDependencies.summary.dependentContentItemCount }}</strong>
                    <span>content items</span>
                  </div>
                </div>

                <div class="template-dependency-grid">
                  <section class="dependency-card">
                    <h5>Dependent templates</h5>
                    <ul v-if="selectedTemplateDependencies.embedded.templates.length > 0" class="dependency-list">
                      <li
                        v-for="dependency in selectedTemplateDependencies.embedded.templates"
                        :key="dependency.id"
                      >
                        {{ dependency.name }} ({{ dependency.key }})
                      </li>
                    </ul>
                    <p v-else class="dependency-empty">No authored templates inherit from this one.</p>
                  </section>

                  <section class="dependency-card">
                    <h5>Assigned content items</h5>
                    <ul v-if="selectedTemplateDependencies.embedded.contentItems.length > 0" class="dependency-list">
                      <li
                        v-for="item in selectedTemplateDependencies.embedded.contentItems"
                        :key="item.id"
                      >
                        {{ item.name }} · {{ item.path }}
                      </li>
                    </ul>
                    <p v-else class="dependency-empty">No content items currently use this template.</p>
                  </section>
                </div>

                <button
                  class="button button--danger"
                  type="button"
                  :disabled="isSubmitting || !selectedTemplateDependencies.canDelete"
                  @click="submitTemplateDelete"
                >
                  Delete Template
                </button>
              </div>

              <form class="editor-card" @submit.prevent="submitTemplateDesigner">
                <div class="editor-card__header">
                  <div>
                    <p class="eyebrow">Designer</p>
                    <h4>{{ templateDesignerHeading }}</h4>
                  </div>
                  <span class="callout">
                    {{ templateDesignerForm.mode === 'create' ? 'POST /api/v1/templates' : 'PUT selected template' }}
                  </span>
                </div>

                <div class="template-actions">
                  <button class="button button--secondary" type="button" @click="startNewTemplateDraft">
                    Reset To New Draft
                  </button>
                  <button
                    class="button button--secondary"
                    type="button"
                    :disabled="selectedTemplateDetail == null"
                    @click="loadSelectedTemplateIntoDesigner"
                  >
                    Load Selected Template
                  </button>
                </div>

                <div class="create-grid">
                  <label class="field">
                    <span>Name</span>
                    <input v-model="templateDesignerForm.name" type="text" required />
                  </label>

                  <label class="field">
                    <span>Key</span>
                    <input v-model="templateDesignerForm.key" type="text" required />
                  </label>

                  <label class="field">
                    <span>Base Template</span>
                    <select v-model="templateDesignerForm.baseTemplateId">
                      <option value="">No base template</option>
                      <option
                        v-for="template in availableBaseTemplates"
                        :key="template.id"
                        :value="template.id"
                      >
                        {{ template.name }} ({{ template.key }})
                      </option>
                    </select>
                    <small class="field-meta">
                      {{ templateDesignerForm.baseTemplateId.length === 0 ? 'Local fields only.' : `Inherits from ${getTemplateKeyById(templateDesignerForm.baseTemplateId)}` }}
                    </small>
                  </label>
                </div>

                <div class="template-designer-stack">
                  <section
                    v-for="(section, sectionIndex) in templateDraftSections"
                    :key="section.id"
                    class="template-section-card"
                  >
                    <div class="template-section-card__header">
                      <div>
                        <h5>Section {{ sectionIndex + 1 }}</h5>
                        <p>Local authored section</p>
                      </div>
                      <div class="template-inline-actions">
                        <button class="button button--secondary" type="button" @click="addDraftField(section.id)">
                          Add Field
                        </button>
                        <button class="button button--secondary" type="button" @click="removeDraftSection(section.id)">
                          Remove Section
                        </button>
                      </div>
                    </div>

                    <div class="template-section-form">
                      <label class="field">
                        <span>Section Name</span>
                        <input v-model="section.name" type="text" required />
                      </label>

                      <label class="field">
                        <span>Section Key</span>
                        <input v-model="section.key" type="text" required />
                      </label>

                      <label class="field">
                        <span>Sort Order</span>
                        <input v-model.number="section.sortOrder" type="number" required />
                      </label>
                    </div>

                    <div class="template-field-stack">
                      <article
                        v-for="(field, fieldIndex) in section.fields"
                        :key="field.id"
                        class="template-field-editor"
                      >
                        <div class="template-section-card__header">
                          <div>
                            <h5>Field {{ fieldIndex + 1 }}</h5>
                            <p>Author-facing field definition</p>
                          </div>
                          <button class="button button--secondary" type="button" @click="removeDraftField(section.id, field.id)">
                            Remove Field
                          </button>
                        </div>

                        <div class="template-field-form">
                          <label class="field">
                            <span>Field Name</span>
                            <input v-model="field.name" type="text" required />
                          </label>

                          <label class="field">
                            <span>Field Key</span>
                            <input v-model="field.key" type="text" required />
                          </label>

                          <label class="field">
                            <span>Field Type</span>
                            <select v-model="field.type" :disabled="isLoadingFieldTypes || availableFieldTypes.length === 0">
                              <option disabled value="">
                                {{ isLoadingFieldTypes ? 'Loading field types...' : 'Select a field type' }}
                              </option>
                              <option
                                v-for="fieldType in availableFieldTypes"
                                :key="fieldType.value"
                                :value="fieldType.value"
                              >
                                {{ fieldType.label }}
                              </option>
                            </select>
                          </label>
                        </div>

                        <div class="template-field-toggles">
                          <label class="checkbox-field">
                            <input v-model="field.isShared" type="checkbox" />
                            <span>Shared across languages and versions</span>
                          </label>

                          <label class="checkbox-field">
                            <input v-model="field.isUnversioned" :disabled="field.isShared" type="checkbox" />
                            <span>Language-specific but not version-specific</span>
                          </label>
                        </div>
                      </article>
                    </div>
                  </section>
                </div>

                <div class="template-actions">
                  <button class="button button--secondary" type="button" @click="addDraftSection">
                    Add Section
                  </button>
                  <button class="button" type="submit" :disabled="isSubmitting">
                    {{ templateDesignerForm.mode === 'create' ? 'Create Template' : 'Save Template' }}
                  </button>
                </div>
              </form>
            </section>
          </template>
        </article>
      </section>
    </main>
  </div>
</template>
