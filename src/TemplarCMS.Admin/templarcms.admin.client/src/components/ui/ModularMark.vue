<script setup lang="ts">
withDefaults(defineProps<{
  size?: number
  accent?: string
  solid?: string
}>(), { size: 24, accent: '#5970e3', solid: '#1c1917' })
</script>

<template>
  <svg :width="size" :height="size" viewBox="0 0 30 24" fill="none" xmlns="http://www.w3.org/2000/svg">
    <!-- top-left block -->
    <rect x="0" y="0" width="9" height="8" rx="1.5" :fill="solid" />
    <!-- top-center block (accent) -->
    <rect x="10.5" y="0" width="9" height="8" rx="1.5" :fill="accent" />
    <!-- top-right block -->
    <rect x="21" y="0" width="9" height="8" rx="1.5" :fill="solid" />
    <!-- mid-left block (hidden — forms the T stem gap) -->
    <!-- stem-left -->
    <rect x="10.5" y="9.5" width="9" height="6" rx="1.5" :fill="solid" />
    <!-- stem-bottom -->
    <rect x="10.5" y="17" width="9" height="7" rx="1.5" :fill="solid" />
  </svg>
</template>
