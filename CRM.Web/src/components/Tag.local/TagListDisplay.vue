<template>
  <div class="tag-list" v-if="visibleTags.length">
    <span
      v-for="tag in visibleTags"
      :key="tag.id"
      class="tag-pill"
      :style="computeStyle(tag.color)"
      :title="tag.name"
    >
      <span class="tag-dot" :style="{ backgroundColor: tag.color || '#4FC3F7' }" />
      <span class="tag-text">{{ tag.name }}</span>
    </span>
    <span v-if="restCount > 0" class="tag-more">+{{ restCount }}</span>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import type { TagDefinitionDto } from '@/api/tag'

const props = withDefaults(
  defineProps<{
    tags: TagDefinitionDto[]
    maxCount?: number
    size?: 'sm' | 'md'
  }>(),
  {
    maxCount: 3,
    size: 'sm'
  }
)

const visibleTags = computed(() => props.tags.slice(0, props.maxCount))
const restCount = computed(() => Math.max(0, props.tags.length - props.maxCount))

const computeStyle = (color?: string | null) => {
  const bg = color || '#263445'
  return {
    '--tag-color': bg
  } as any
}
</script>

<style scoped lang="scss">
.tag-list {
  display: inline-flex;
  flex-wrap: wrap;
  gap: 4px;
}

.tag-pill {
  --tag-color: #263445;
  display: inline-flex;
  align-items: center;
  gap: 4px;
  padding: 2px 6px;
  border-radius: 999px;
  font-size: 11px;
  line-height: 1;
  background: color-mix(in srgb, var(--tag-color) 22%, #020712);
  border: 1px solid color-mix(in srgb, var(--tag-color) 55%, #020712);
  color: #e0f4ff;
}

.tag-dot {
  width: 6px;
  height: 6px;
  border-radius: 999px;
}

.tag-text {
  white-space: nowrap;
}

.tag-more {
  font-size: 11px;
  color: rgba(200, 216, 232, 0.7);
}
</style>

