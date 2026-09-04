<script setup lang="ts">
import { computed, nextTick, ref, watch } from 'vue'
import type { MediaAssetCollectionResponse, MediaAssetResponse } from '@/types/admin-api'
import { fetchJson } from '@/utils/request-helpers'

const props = defineProps<{ modelValue: string; readonly?: boolean }>()
const emit = defineEmits<{ 'update:modelValue': [value: string] }>()
const open = ref(false), loading = ref(false), error = ref<string | null>(null), search = ref(''), highlighted = ref<string | null>(null), viewMode = ref<'grid' | 'list'>('grid')
const assets = ref<MediaAssetResponse[]>([]), loaded = ref(new Set<string>()), imageErrors = ref(new Set<string>())
const searchRef = ref<HTMLInputElement | null>(null), listRef = ref<HTMLElement | null>(null)
const visibleAssets = computed(() => assets.value.filter(asset => `${asset.fileName} ${asset.title ?? ''} ${asset.altText ?? ''}`.toLowerCase().includes(search.value.toLowerCase().trim())))
const selectedAsset = computed(() => assets.value.find(asset => asset.id === props.modelValue) ?? null)
watch(open, value => { if (value) nextTick(() => searchRef.value?.focus()) })
watch(
  () => props.modelValue,
  async assetId => {
    if (!assetId || assets.value.some(asset => asset.id === assetId)) return

    try {
      assets.value = (await fetchJson<MediaAssetCollectionResponse>('/api/v1/media/assets')).assets
    } catch {
      // Keep the field usable when preview hydration is unavailable; Browse can retry the catalog request.
    }
  },
  { immediate: true }
)
async function showPicker() { open.value = true; loading.value = true; error.value = null; loaded.value = new Set(); imageErrors.value = new Set(); try { assets.value = (await fetchJson<MediaAssetCollectionResponse>('/api/v1/media/assets')).assets; highlighted.value = props.modelValue || null } catch (caught) { error.value = caught instanceof Error ? caught.message : 'Media could not be loaded.' } finally { loading.value = false } }
function confirm() { if (highlighted.value) emit('update:modelValue', highlighted.value); open.value = false }
function onLoad(id: string) { loaded.value = new Set(loaded.value).add(id) }
function onError(id: string) { imageErrors.value = new Set(imageErrors.value).add(id) }
function navigate(delta: number) { if (!visibleAssets.value.length) return; const current = visibleAssets.value.findIndex(asset => asset.id === highlighted.value); const asset = visibleAssets.value[Math.max(0, Math.min(visibleAssets.value.length - 1, current < 0 ? 0 : current + delta))]; if (asset) highlighted.value = asset.id; nextTick(() => listRef.value?.querySelector<HTMLElement>('[data-highlighted="true"]')?.focus()) }
</script>
<template>
  <div class="image-picker">
    <div v-if="selectedAsset" class="selected"><img :src="selectedAsset.contentUrl" :alt="selectedAsset.altText ?? ''" /><div><strong>{{ selectedAsset.title || selectedAsset.fileName }}</strong><small>{{ selectedAsset.altText || 'No alt text set' }}</small></div><button v-if="!readonly" type="button" @click="showPicker">Browse</button><button v-if="!readonly" type="button" @click="emit('update:modelValue', '')">Clear</button></div>
    <button v-else-if="!readonly" class="empty" type="button" @click="showPicker">Choose image from media library...</button><p v-else class="muted">No image selected.</p>
    <Teleport to="body"><div v-if="open" class="backdrop" role="dialog" aria-modal="true" aria-label="Media library" @click.self="open = false" @keydown.escape="open = false"><div class="modal"><header><strong>Media Library</strong><button type="button" :aria-pressed="viewMode === 'grid'" @click="viewMode = 'grid'">Grid</button><button type="button" :aria-pressed="viewMode === 'list'" @click="viewMode = 'list'">List</button><input ref="searchRef" v-model="search" type="search" placeholder="Search media" aria-label="Search media" /><button type="button" aria-label="Close dialog" @click="open = false">x</button></header><main ref="listRef" role="listbox" :class="viewMode" @keydown.up.prevent="navigate(-1)" @keydown.left.prevent="navigate(-1)" @keydown.down.prevent="navigate(1)" @keydown.right.prevent="navigate(1)" @keydown.enter.prevent="confirm"><p v-if="loading">Loading media...</p><p v-else-if="error" role="alert">{{ error }}</p><p v-else-if="visibleAssets.length === 0" role="status">{{ search ? 'No results. Try a different search.' : 'This folder is empty. Upload assets in the Media Library.' }}</p><button v-for="asset in visibleAssets" v-else :key="asset.id" role="option" :aria-selected="highlighted === asset.id" :tabindex="highlighted === asset.id || (!highlighted && visibleAssets[0]?.id === asset.id) ? 0 : -1" :data-highlighted="highlighted === asset.id ? 'true' : undefined" :class="{ selected: highlighted === asset.id }" @click="highlighted = asset.id" @dblclick="highlighted = asset.id; confirm()"><div class="thumbnail"><span v-if="!loaded.has(asset.id) && !imageErrors.has(asset.id)" class="shimmer" aria-hidden="true" /><span v-else-if="imageErrors.has(asset.id)" class="image-error" role="img" :aria-label="`${asset.fileName} failed to load`">Load failed</span><img v-show="loaded.has(asset.id)" :src="asset.contentUrl" :alt="asset.altText ?? ''" @load="onLoad(asset.id)" @error="onError(asset.id)" /></div><span>{{ asset.title || asset.fileName }}</span><small v-if="viewMode === 'list'">{{ asset.fileName }} | {{ asset.contentType }}</small></button></main><footer><span>{{ highlighted ? 'Enter or double-click to confirm.' : 'Click or use arrow keys to navigate.' }}</span><em>Upload - future</em><button type="button" @click="open = false">Cancel</button><button type="button" :disabled="!highlighted" @click="confirm">Select</button></footer></div></div></Teleport>
  </div>
</template>
<style scoped>
.empty,.selected{width:100%;border:1px dashed #d6d3d1;border-radius:.75rem;padding:.75rem;background:#fff;text-align:left}.selected{display:flex;gap:.75rem;align-items:center;border-style:solid}.selected img{width:5rem;height:3.5rem;object-fit:cover;border-radius:.35rem}.selected small{display:block;color:#78716c}.selected button{margin-left:auto}.backdrop{position:fixed;inset:0;z-index:50;display:grid;place-items:center;background:#0008;padding:1.5rem}.modal{width:min(52rem,100%);height:min(34rem,100%);display:flex;flex-direction:column;background:#fff;border-radius:1rem;overflow:hidden}.modal header,.modal footer{display:flex;gap:.75rem;align-items:center;padding:1rem;border-bottom:1px solid #e7e5e4}.modal header input{margin-left:auto}.modal header button[aria-pressed=true]{background:#5970e3;color:white}.modal main{flex:1;overflow:auto;padding:1rem;gap:.75rem}.modal main.grid{display:grid;grid-template-columns:repeat(auto-fill,minmax(8rem,1fr))}.modal main.list{display:flex;flex-direction:column}.modal main button{border:1px solid #e7e5e4;border-radius:.5rem;text-align:left;overflow:hidden;background:#fff}.modal main button.selected,.modal main button:focus-visible{outline:2px solid #5970e3}.modal main.list button{display:grid;grid-template-columns:5rem 1fr;column-gap:.75rem;align-items:center}.modal main.list button small{grid-column:2}.thumbnail{height:5.5rem;position:relative;background:#e7e5e4}.thumbnail img{width:100%;height:100%;object-fit:cover}.modal main span,.modal main small{display:block;padding:.4rem;font-size:.75rem}.modal main small{color:#78716c;padding-top:0}.shimmer{position:absolute;inset:0;background:linear-gradient(90deg,#e7e5e4 25%,#f5f5f4 50%,#e7e5e4 75%);background-size:200% 100%;animation:shimmer 1.2s infinite}.image-error{height:100%;display:grid!important;place-items:center;color:#a8a29e}.modal footer{border-top:1px solid #e7e5e4;border-bottom:0}.modal footer span{margin-right:auto}.modal footer em{font-size:.7rem;color:#b45309;background:#fef3c7;padding:.2rem .4rem;border-radius:99px;font-style:normal}.muted{color:#a8a29e;font-style:italic}@keyframes shimmer{to{background-position:-200% 0}}
</style>
