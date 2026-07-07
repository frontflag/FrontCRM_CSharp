<script setup lang="ts">
import { computed } from 'vue'

const props = defineProps<{
  declarationId?: string | null
  declarationCode?: string | null
  emptyText?: string
}>()

const display = computed(() => (props.declarationCode || '').trim())
const hasLink = computed(() => !!(props.declarationId || '').trim() && !!display.value)
</script>

<template>
  <router-link
    v-if="hasLink"
    :to="{ name: 'CustomsDeclarationDetail', params: { id: (declarationId || '').trim() } }"
    class="cell-link"
    @click.stop
  >
    {{ display }}
  </router-link>
  <span v-else class="text-secondary">{{ emptyText ?? '—' }}</span>
</template>
