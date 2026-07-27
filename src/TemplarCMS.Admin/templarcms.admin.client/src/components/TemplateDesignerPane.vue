<script setup lang="ts">
import { computed } from 'vue'
import type { FieldTypeResponse, TemplateSummaryResponse } from '@/types/admin-api'
import type { TemplateSectionViewModel } from '@/types/admin-ui'
import type {
  TemplateDesignerFormState,
  TemplateDraftSection
} from '@/types/template-designer'
import {
  createFieldTypeLookup,
  getFieldTypeDefinition,
  getFieldTypeOptions
} from '@/utils/editor-fields'
import {
  getTemplateDesignerFieldBehaviorHints,
  getTemplateDesignerFieldStorageLabel
} from '@/utils/template-designer-fields'

const props = defineProps<{
  form: TemplateDesignerFormState
  sections: TemplateDraftSection[]
  availableBaseTemplates: TemplateSummaryResponse[]
  availableFieldTypes: FieldTypeResponse[]
  isLoadingFieldTypes: boolean
  isSubmitting: boolean
  heading: string
  selectedTemplateLoaded: boolean
  baseTemplateKey: string | null
  validationErrors: string[]
  inheritedTemplateSections: TemplateSectionViewModel[]
  inheritedFieldCount: number
  isLoadingBaseTemplatePreview: boolean
  baseTemplatePreviewError: string | null
}>()

const emit = defineEmits<{
  submit: []
  resetNewDraft: []
  loadSelectedTemplate: []
  updateName: [value: string]
  updateKey: [value: string]
  updateBaseTemplateId: [value: string]
  addSection: []
  removeSection: [sectionId: string]
  updateSectionName: [sectionId: string, value: string]
  updateSectionKey: [sectionId: string, value: string]
  updateSectionSortOrder: [sectionId: string, value: number]
  addField: [sectionId: string]
  removeField: [sectionId: string, fieldId: string]
  updateFieldName: [sectionId: string, fieldId: string, value: string]
  updateFieldKey: [sectionId: string, fieldId: string, value: string]
  updateFieldType: [sectionId: string, fieldId: string, value: string]
  updateFieldShared: [sectionId: string, fieldId: string, value: boolean]
  updateFieldUnversioned: [sectionId: string, fieldId: string, value: boolean]
}>()

function readTextValue(event: Event): string {
  const target = event.target as HTMLInputElement | HTMLSelectElement | null
  return target?.value ?? ''
}

function readNumberValue(event: Event): number {
  const target = event.target as HTMLInputElement | null
  return Number(target?.value ?? '0')
}

function readCheckedValue(event: Event): boolean {
  const target = event.target as HTMLInputElement | null
  return target?.checked ?? false
}

const fieldTypeLookup = computed(() => createFieldTypeLookup(props.availableFieldTypes))

function getFieldTypeDefinitionFor(value: string): FieldTypeResponse {
  return getFieldTypeDefinition(value, fieldTypeLookup.value)
}

function getFieldTypeOptionsFor(value: string): FieldTypeResponse[] {
  return getFieldTypeOptions(value, props.availableFieldTypes)
}

function isSupportedFieldType(value: string): boolean {
  return props.availableFieldTypes.some(fieldType => fieldType.value === value)
}
</script>

<template>
  <form class="editor-card" @submit.prevent="emit('submit')">
    <div class="editor-card__header">
      <div>
        <p class="eyebrow">Designer</p>
        <h4>{{ heading }}</h4>
      </div>
      <span class="callout">
        {{ form.mode === 'create' ? 'POST /api/v1/templates' : 'PUT selected template' }}
      </span>
    </div>

    <div class="template-actions">
      <button class="button button--secondary" type="button" @click="emit('resetNewDraft')">
        Reset To New Draft
      </button>
      <button
        class="button button--secondary"
        type="button"
        :disabled="!selectedTemplateLoaded"
        @click="emit('loadSelectedTemplate')"
      >
        Load Selected Template
      </button>
    </div>

    <div class="create-grid">
      <label class="field">
        <span>Name</span>
        <input
          :value="form.name"
          type="text"
          required
          @input="emit('updateName', readTextValue($event))"
        />
      </label>

      <label class="field">
        <span>Key</span>
        <input
          :value="form.key"
          type="text"
          required
          @input="emit('updateKey', readTextValue($event))"
        />
      </label>

      <label class="field">
        <span>Base Template</span>
        <select
          :value="form.baseTemplateId"
          @change="emit('updateBaseTemplateId', readTextValue($event))"
        >
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
          {{ form.baseTemplateId.length === 0 ? 'Local fields only.' : `Inherits from ${baseTemplateKey}` }}
        </small>
      </label>
    </div>

    <div v-if="validationErrors.length > 0" class="callout callout--danger">
      <strong>Fix {{ validationErrors.length }} issue{{ validationErrors.length === 1 ? '' : 's' }} before saving.</strong>
      <ul class="template-validation-list">
        <li v-for="error in validationErrors" :key="error">{{ error }}</li>
      </ul>
    </div>

    <section v-if="form.baseTemplateId.length > 0" class="template-section-card">
      <div class="template-section-card__header">
        <div>
          <h5>Inherited Base Template</h5>
          <p>
            {{ baseTemplateKey == null ? 'Selected base template' : baseTemplateKey }}
          </p>
        </div>
        <span class="callout">
          {{ inheritedTemplateSections.length }} sections · {{ inheritedFieldCount }} fields
        </span>
      </div>

      <div v-if="isLoadingBaseTemplatePreview" class="empty-state empty-state--compact">
        Loading inherited sections and fields...
      </div>

      <div v-else-if="baseTemplatePreviewError != null" class="callout callout--danger">
        {{ baseTemplatePreviewError }}
      </div>

      <div v-else-if="inheritedTemplateSections.length === 0" class="empty-state empty-state--compact">
        This base template does not currently expose authored sections or fields.
      </div>

      <div v-else class="template-section-stack">
        <section
          v-for="section in inheritedTemplateSections"
          :key="section.id"
          class="template-field-editor"
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
    </section>

    <div class="template-designer-stack">
      <section
        v-for="(section, sectionIndex) in sections"
        :key="section.id"
        class="template-section-card"
      >
        <div class="template-section-card__header">
          <div>
            <h5>Section {{ sectionIndex + 1 }}</h5>
            <p>Local authored section</p>
          </div>
          <div class="template-inline-actions">
            <button class="button button--secondary" type="button" @click="emit('addField', section.id)">
              Add Field
            </button>
            <button class="button button--secondary" type="button" @click="emit('removeSection', section.id)">
              Remove Section
            </button>
          </div>
        </div>

        <div class="template-section-form">
          <label class="field">
            <span>Section Name</span>
            <input
              :value="section.name"
              type="text"
              required
              @input="emit('updateSectionName', section.id, readTextValue($event))"
            />
          </label>

          <label class="field">
            <span>Section Key</span>
            <input
              :value="section.key"
              type="text"
              required
              @input="emit('updateSectionKey', section.id, readTextValue($event))"
            />
          </label>

          <label class="field">
            <span>Sort Order</span>
            <input
              :value="section.sortOrder"
              type="number"
              required
              @input="emit('updateSectionSortOrder', section.id, readNumberValue($event))"
            />
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
              <button class="button button--secondary" type="button" @click="emit('removeField', section.id, field.id)">
                Remove Field
              </button>
            </div>

            <div class="template-field-form">
              <label class="field">
                <span>Field Name</span>
                <input
                  :value="field.name"
                  type="text"
                  required
                  @input="emit('updateFieldName', section.id, field.id, readTextValue($event))"
                />
              </label>

              <label class="field">
                <span>Field Key</span>
                <input
                  :value="field.key"
                  type="text"
                  required
                  @input="emit('updateFieldKey', section.id, field.id, readTextValue($event))"
                />
              </label>

              <label class="field">
                <span>Field Type</span>
                <select
                  :value="field.type"
                  :disabled="isLoadingFieldTypes || getFieldTypeOptionsFor(field.type).length === 0"
                  @change="emit('updateFieldType', section.id, field.id, readTextValue($event))"
                >
                  <option disabled value="">
                    {{ isLoadingFieldTypes ? 'Loading field types...' : 'Select a field type' }}
                  </option>
                  <option
                    v-for="fieldType in getFieldTypeOptionsFor(field.type)"
                    :key="fieldType.value"
                    :value="fieldType.value"
                  >
                    {{ fieldType.label }}
                  </option>
                </select>
                <small class="field-meta">
                  Editor: {{ getFieldTypeDefinitionFor(field.type).editorKind }}
                  <template v-if="getFieldTypeDefinitionFor(field.type).inputType !== 'text'">
                    · Input: {{ getFieldTypeDefinitionFor(field.type).inputType }}
                  </template>
                </small>
                <small
                  v-if="!isSupportedFieldType(field.type)"
                  class="field-help"
                >
                  This field keeps an existing unsupported type visible so you can preserve it or replace it intentionally.
                </small>
                <small
                  v-else-if="getFieldTypeDefinitionFor(field.type).helpText != null"
                  class="field-help"
                >
                  {{ getFieldTypeDefinitionFor(field.type).helpText }}
                </small>
              </label>
            </div>

            <div class="template-field-preview">
              <span class="callout">Storage: {{ getTemplateDesignerFieldStorageLabel(field) }}</span>
              <ul class="template-field-preview__list">
                <li
                  v-for="hint in getTemplateDesignerFieldBehaviorHints(getFieldTypeDefinitionFor(field.type))"
                  :key="hint"
                >
                  {{ hint }}
                </li>
              </ul>
              <small v-if="field.isShared" class="field-help">
                Shared fields always save with versioning disabled.
              </small>
            </div>

            <div class="template-field-toggles">
              <label class="checkbox-field">
                <input
                  :checked="field.isShared"
                  type="checkbox"
                  @change="emit('updateFieldShared', section.id, field.id, readCheckedValue($event))"
                />
                <span>Shared across languages and versions</span>
              </label>

              <label class="checkbox-field">
                <input
                  :checked="field.isUnversioned"
                  :disabled="field.isShared"
                  type="checkbox"
                  @change="emit('updateFieldUnversioned', section.id, field.id, readCheckedValue($event))"
                />
                <span>Language-specific but not version-specific</span>
              </label>
            </div>
          </article>
        </div>
      </section>
    </div>

    <div class="template-actions">
      <button class="button button--secondary" type="button" @click="emit('addSection')">
        Add Section
      </button>
      <button class="button" type="submit" :disabled="isSubmitting || validationErrors.length > 0">
        {{ form.mode === 'create' ? 'Create Template' : 'Save Template' }}
      </button>
    </div>
  </form>
</template>
