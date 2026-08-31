<template>
  <div v-if="docs.length" class="ops-generated-docs">
    <span class="ops-generated-docs__label">{{ label }}</span>
    <template v-for="doc in docs" :key="doc.id">
      <router-link
        v-if="!maskSensitive && doc.to"
        :to="doc.to"
        class="ops-generated-docs__link link-text"
      >
        {{ doc.code }}
      </router-link>
      <span v-else class="ops-generated-docs__code">{{ doc.code }}</span>
    </template>
  </div>
</template>

<script setup lang="ts">
import type { RouteLocationRaw } from 'vue-router'

export type OpsGeneratedDocLink = {
  id: string
  code: string
  to?: RouteLocationRaw
}

defineProps<{
  label: string
  docs: OpsGeneratedDocLink[]
  maskSensitive?: boolean
}>()
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.ops-generated-docs {
  display: flex;
  flex-wrap: wrap;
  align-items: baseline;
  gap: 4px 10px;
  margin: 0 0 8px;
  padding-top: 8px;
  border-top: 1px solid $border-panel;
  font-size: 12px;
  line-height: 1.5;
}

.ops-generated-docs:last-child {
  margin-bottom: 0;
}

.ops-generated-docs__label {
  color: $text-secondary;
  flex: 0 0 auto;
}

.ops-generated-docs__label::after {
  content: '：';
}

.ops-generated-docs__link,
.ops-generated-docs__code {
  font-weight: 400;
  word-break: break-all;
}

.ops-generated-docs__link {
  color: inherit;
  text-decoration: none;
  cursor: default;

  &:hover {
    color: var(--el-color-primary);
    text-decoration: underline;
    cursor: pointer;
  }
}

.ops-generated-docs__code {
  color: $text-primary;
}
</style>
