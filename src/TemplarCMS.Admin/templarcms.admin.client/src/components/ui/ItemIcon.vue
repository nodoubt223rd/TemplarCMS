<script setup lang="ts">
import { computed } from 'vue'
import { ICON_SVG, type IconKey } from '@/types/icons'
const props = defineProps<{ icon: string }>()
// Only source-controlled SVG fragments are rendered; API strings select a known key.
const markup = computed(() => Object.prototype.hasOwnProperty.call(ICON_SVG, props.icon) ? ICON_SVG[props.icon as IconKey] : ICON_SVG.file)
</script>
<template>
  <!-- eslint-disable-next-line vue/no-v-html -->
  <svg width="16" height="16" viewBox="0 0 16 16" aria-hidden="true" :data-icon="icon" v-html="markup" />
</template>
