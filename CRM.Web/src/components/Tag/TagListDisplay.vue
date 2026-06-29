<template>
  <div class="tag-list-display">
    <span
      v-for="tag in tags"
      :key="tag.id"
      class="tag-chip"
      :style="tag.color ? { borderColor: tag.color + '55', color: tag.color, background: tag.color + '18' } : {}"
    >
      {{ tag.name }}
    </span>
    <span v-if="!tags || tags.length === 0" class="tag-empty">暂无标签</span>
  </div>
</template>

<script setup lang="ts">
import type { TagDefinitionDto } from '@/api/tag';

defineProps<{
  tags: TagDefinitionDto[];
}>();
</script>

<style scoped lang="scss">
.tag-list-display {
  display: flex;
  flex-wrap: wrap;
  gap: 5px;
  align-items: center;
}

.tag-chip {
  display: inline-flex;
  align-items: center;
  padding: 2px 8px;
  border-radius: 999px;
  font-size: 12px;
  font-weight: 500;
  border: 1px solid var(--crm-accent-03, rgba(0, 212, 255, 0.3));
  color: var(--crm-primary-color, rgba(0, 212, 255, 0.85));
  background: var(--crm-accent-008, rgba(0, 212, 255, 0.08));
  letter-spacing: 0.2px;
  line-height: 1.5;
}

.tag-empty {
  font-size: 12px;
  color: var(--crm-text-muted);
  font-style: normal;
}
</style>
