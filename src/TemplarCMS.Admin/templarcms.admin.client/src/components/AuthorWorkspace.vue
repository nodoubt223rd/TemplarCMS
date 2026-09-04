<script setup lang="ts">
import { computed, watch } from 'vue'
import TopBar from './layout/TopBar.vue'
import NavRail from './layout/NavRail.vue'
import StatusBar from './layout/StatusBar.vue'
import ContentTree from './tree/ContentTree.vue'
import ContentEditor from './editor/ContentEditor.vue'
import TemplateDesigner from './templates/TemplateDesigner.vue'
import ActionSidebar from './sidebar/ActionSidebar.vue'
import ToastContainer from './ui/ToastContainer.vue'
import { useToast } from '@/composables/useToast'
import type { ContentItemDependencyResponse, ContentItemResponse, FieldTypeResponse, TemplateResponse, TemplateSummaryResponse } from '@/types/admin-api'
import type { EditorFieldModel, TreeNode } from '@/types/admin-ui'

type Workspace = 'content' | 'templates' | 'media' | 'system'

const props = defineProps<{
  activeWorkspace: Workspace
  language: string
  version: number
  showActions: boolean
  rootNode: TreeNode
  selectedItem: ContentItemResponse | null
  selectedItemId: string | null
  fields: EditorFieldModel[]
  fieldForm: Record<string, string>
  templateName: string | null
  isLoadingTree: boolean
  isLoadingFields: boolean
  isSubmitting: boolean
  dependencies: ContentItemDependencyResponse | null
  templates: TemplateSummaryResponse[]
  selectedTemplateId: string | null
  selectedTemplate: TemplateResponse | null
  isLoadingTemplates: boolean
  availableFieldTypes: FieldTypeResponse[]
  pageError: string | null
  successMessage: string | null
}>()

const templateIcons = computed(() =>
  Object.fromEntries(props.templates.map(template => [template.id, template.icon ?? 'file'])))

const toast = useToast()

watch(
  () => props.pageError,
  message => {
    if (message != null) {
      toast.error('Unable to complete the request', message)
    }
  }
)

watch(
  () => props.successMessage,
  message => {
    if (message != null) {
      toast.success('Changes saved', message)
    }
  }
)

const emit = defineEmits<{
  'update:activeWorkspace': [workspace: Workspace]
  'update:language': [language: string]
  'update:version': [version: number]
  'toggle-actions': []
  'close-actions': []
  selectNode: [node: TreeNode]
  toggleNode: [node: TreeNode]
  save: []
  delete: []
  updateItemIcon: [icon: string | null]
  fieldInput: [key: string, value: string]
  checkboxInput: [key: string, checked: boolean]
  selectTemplate: [templateId: string]
  updateTemplateIcon: [icon: string]
  updateTemplateBaseTemplateIds: [templateIds: string[]]
  saveTemplate: [template: TemplateSaveRequest]
}>()

type TemplateSaveRequest = {
  name: string
  key: string
  icon: string
  baseTemplateKeys: string[]
  sections: Array<{
    name: string
    key: string
    sortOrder: number
    fields: Array<{ name: string; key: string; type: string; isShared: boolean; isUnversioned: boolean }>
  }>
}
</script>

<template>
  <div class="flex h-screen flex-col overflow-hidden bg-[#f5f3ef]">
    <TopBar
      :language="language"
      :version="version"
      :show-actions="showActions"
      @update:language="emit('update:language', $event)"
      @update:version="emit('update:version', $event)"
      @toggle-actions="emit('toggle-actions')"
    />
    <div class="flex min-h-0 flex-1">
      <NavRail :active="activeWorkspace" @change="emit('update:activeWorkspace', $event)" />
      <ContentTree
        v-if="activeWorkspace === 'content'"
        :root-node="rootNode"
        :selected-id="selectedItemId"
        :is-loading="isLoadingTree"
        :template-icons="templateIcons"
        @select="emit('selectNode', $event)"
        @toggle="emit('toggleNode', $event)"
      />
      <main class="flex min-w-0 flex-1">
        <template v-if="activeWorkspace === 'content'">
          <ContentEditor
            :item="selectedItem"
            :template-name="templateName"
            :fields="fields"
            :field-form="fieldForm"
            :is-loading-fields="isLoadingFields"
            :is-submitting="isSubmitting"
            @save="emit('save')"
            @field-input="(key, value) => emit('fieldInput', key, value)"
            @checkbox-input="(key, checked) => emit('checkboxInput', key, checked)"
          />
          <ActionSidebar
            :item="selectedItem"
            :show="showActions"
            :is-submitting="isSubmitting"
            :can-delete="dependencies?.canDelete ?? false"
            @close="emit('close-actions')"
            @save="emit('save')"
            @delete="emit('delete')"
            @update-icon="emit('updateItemIcon', $event)"
          />
        </template>
        <TemplateDesigner
          v-else-if="activeWorkspace === 'templates'"
          :templates="templates"
          :selected-template-id="selectedTemplateId"
          :selected-template="selectedTemplate"
          :is-loading="isLoadingTemplates"
          :is-submitting="isSubmitting"
          :available-field-types="availableFieldTypes"
          @select="emit('selectTemplate', $event)"
          @update-icon="emit('updateTemplateIcon', $event)"
          @update-base-template-ids="emit('updateTemplateBaseTemplateIds', $event)"
          @save-template="emit('saveTemplate', $event)"
        />
        <section v-else class="flex flex-1 items-center justify-center text-sm text-stone-400">
          {{ activeWorkspace === 'media' ? 'Media authoring is not available yet.' : 'System authoring is not available yet.' }}
        </section>
      </main>
    </div>
    <ToastContainer />
    <StatusBar :selected-item="selectedItem" />
  </div>
</template>
