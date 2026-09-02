<template>
  <span class="so-item-flow-kv__value">
    <router-link
      v-if="to"
      class="link-text"
      :to="to"
      target="_blank"
      rel="noopener noreferrer"
    >{{ text }}</router-link>
    <template v-else>{{ text }}</template>
  </span>
</template>

<script setup lang="ts">
defineProps<{
  text: string
  to?: { name: string; params?: Record<string, string>; query?: Record<string, string> }
}>()
</script>
