<script setup lang="ts">
import type {
  ContentItemDependencyResponse,
  ContentItemResponse
} from '@/types/admin-api'
import type { GeneralLinkDraft } from '@/types/general-link'
import type { EditorFieldModel } from '@/types/admin-ui'

const props = defineProps<{
  selectedItem: ContentItemResponse | null
  selectedItemTemplateName: string | null
  isLoadingTemplateFields: boolean
  editorFields: EditorFieldModel[]
  fieldForm: Record<string, string>
  isSubmitting: boolean
  renameName: string
  moveParentId: string
  selectedItemDependencies: ContentItemDependencyResponse | null
  isLoadingItemDependencies: boolean
  getGeneralLinkDraft: (key: string) => GeneralLinkDraft
  getCheckboxValue: (key: string) => boolean
}>()

const emit = defineEmits<{
  submitValues: []
  submitRename: []
  submitMove: []
  submitDelete: []
  updateRenameName: [value: string]
  updateMoveParentId: [value: string]
  fieldInput: [key: string, value: string]
  checkboxInput: [key: string, checked: boolean]
  generalLinkKindInput: [key: string, value: string]
  generalLinkItemIdInput: [key: string, value: string]
  generalLinkUrlInput: [key: string, value: string]
  generalLinkTextInput: [key: string, value: string]
  generalLinkTargetInput: [key: string, value: string]
}>()

function readTextValue(event: Event): string {
  const target = event.target as HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement | null
  return target?.value ?? ''
}

function readCheckedValue(event: Event): boolean {
  const target = event.target as HTMLInputElement | null
  return target?.checked ?? false
}
</script>

<template>
  <article class="panel inspector-panel">
    <div class="panel-header">
      <div>
        <p class="eyebrow">Content Inspector</p>
        <h3>{{ selectedItem == null ? 'Select an item' : selectedItem.name }}</h3>
      </div>
      <span class="panel-pill">
        {{ selectedItem == null ? 'No active item' : selectedItem.path }}
      </span>
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
        <form class="editor-card" @submit.prevent="emit('submitValues')">
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
                      @change="emit('generalLinkKindInput', field.key, readTextValue($event))"
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
                      @input="emit('generalLinkItemIdInput', field.key, readTextValue($event))"
                    />
                  </label>

                  <label v-else class="field">
                    <span>External URL</span>
                    <input
                      :value="getGeneralLinkDraft(field.key).url"
                      type="url"
                      placeholder="https://example.com"
                      @input="emit('generalLinkUrlInput', field.key, readTextValue($event))"
                    />
                  </label>

                  <label class="field">
                    <span>Link Text</span>
                    <input
                      :value="getGeneralLinkDraft(field.key).text"
                      type="text"
                      placeholder="Optional label"
                      @input="emit('generalLinkTextInput', field.key, readTextValue($event))"
                    />
                  </label>

                  <label class="field">
                    <span>Target</span>
                    <input
                      :value="getGeneralLinkDraft(field.key).target"
                      type="text"
                      placeholder="_self or _blank"
                      @input="emit('generalLinkTargetInput', field.key, readTextValue($event))"
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
                    @change="emit('checkboxInput', field.key, readCheckedValue($event))"
                  />
                  <span>Enabled</span>
                </label>
                <textarea
                  v-else-if="field.editorKind === 'textarea'"
                  :value="fieldForm[field.key]"
                  class="field-textarea"
                  :placeholder="field.placeholder ?? undefined"
                  :rows="field.rows ?? 3"
                  @input="emit('fieldInput', field.key, readTextValue($event))"
                />
                <input
                  v-else-if="field.editorKind !== 'general-link'"
                  :value="fieldForm[field.key]"
                  :type="field.inputType"
                  :placeholder="field.placeholder ?? undefined"
                  :step="field.step ?? undefined"
                  @input="emit('fieldInput', field.key, readTextValue($event))"
                />
              </label>
            </div>

            <button class="button" type="submit" :disabled="isSubmitting">
              Save Values
            </button>
          </template>
        </form>

        <form class="editor-card" @submit.prevent="emit('submitRename')">
          <div class="editor-card__header">
            <div>
              <p class="eyebrow">Rename</p>
              <h4>Update display name</h4>
            </div>
            <span class="callout">Key is generated from the name</span>
          </div>

          <label class="field">
            <span>Name</span>
            <input
              :value="renameName"
              type="text"
              required
              @input="emit('updateRenameName', readTextValue($event))"
            />
          </label>

          <label class="field">
            <span>Key</span>
            <small class="field-meta">The server generates an SEO-friendly key from the current name.</small>
          </label>

          <button class="button" type="submit" :disabled="isSubmitting">
            Rename Item
          </button>
        </form>

        <form class="editor-card" @submit.prevent="emit('submitMove')">
          <div class="editor-card__header">
            <div>
              <p class="eyebrow">Move</p>
              <h4>Re-parent the current item</h4>
            </div>
            <span class="callout">Blank parent means move to root</span>
          </div>

          <label class="field">
            <span>New Parent Id</span>
            <input
              :value="moveParentId"
              type="text"
              placeholder="Leave blank for root"
              @input="emit('updateMoveParentId', readTextValue($event))"
            />
          </label>

          <button class="button" type="submit" :disabled="isSubmitting">
            Move Item
          </button>
        </form>

        <form class="editor-card" @submit.prevent="emit('submitDelete')">
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
</template>
