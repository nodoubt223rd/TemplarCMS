<script setup lang="ts">
import { computed } from 'vue'
import { ICON_SVG } from '@/data/icons'
import type { IconKey } from '@/types'

const props = withDefaults(defineProps<{
  iconKey: IconKey
  size?: number
  class?: string
}>(), { size: 16 })

const svg = computed(() => ICON_SVG[props.iconKey] ?? '')
</script>

<template>
  <svg
    :width="size"
    :height="size"
    viewBox="0 0 16 16"
    xmlns="http://www.w3.org/2000/svg"
    v-html="svg"
  />
</template>
