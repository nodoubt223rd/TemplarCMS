<script setup lang="ts">
import type {
  TemplateDependencyResponse,
  TemplateResponse
} from '@/types/admin-api'
import type { TemplateSectionViewModel } from '@/types/admin-ui'

defineProps<{
  selectedTemplateDetail: TemplateResponse | null
  selectedTemplateDependencies: TemplateDependencyResponse | null
  templateSections: TemplateSectionViewModel[]
  selectedTemplateFieldCount: number
  selectedItemTemplateId: string | null
  isLoadingTemplateDetail: boolean
  isLoadingTemplateDependencies: boolean
  isSubmitting: boolean
}>()

const emit = defineEmits<{
  applyTemplateToCreate: []
  submitTemplateDelete: []
}>()
</script>

<template>
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
              {{ selectedItemTemplateId === selectedTemplateDetail.id ? 'Matches current item' : 'Available for create flow' }}
            </span>
          </div>

          <div class="template-action-row">
            <button class="button" type="button" @click="emit('applyTemplateToCreate')">
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
            @click="emit('submitTemplateDelete')"
          >
            Delete Template
          </button>
        </div>
      </section>
    </template>
  </article>
</template>
