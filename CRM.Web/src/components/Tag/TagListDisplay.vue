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
  font-size: 11px;
  font-weight: 500;
  border: 1px solid rgba(0, 212, 255, 0.3);
  color: rgba(0, 212, 255, 0.85);
  background: rgba(0, 212, 255, 0.08);
  letter-spacing: 0.3px;
  line-height: 1.6;
}

.tag-empty {
  font-size: 11px;
  color: rgba(200, 216, 232, 0.3);
  font-style: italic;
}
</style>
