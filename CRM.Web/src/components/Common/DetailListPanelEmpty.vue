<template>
  <div class="detail-list-panel-empty" :class="`detail-list-panel-empty--${size}`">
    <el-empty
      v-if="size !== 'low'"
      :description="description"
      :image-size="size === 'medium' ? mediumIconSize : highIconSize"
    />
    <span v-else class="detail-list-panel-empty__text">{{ description }}</span>
  </div>
</template>

<script setup lang="ts">
export type DetailListPanelEmptySize = 'high' | 'medium' | 'low'

withDefaults(
  defineProps<{
    /** 高版：图标在上、文字在下；中版：图标在左、文字在右；矮版：仅文字 */
    size?: DetailListPanelEmptySize
    description?: string
    highIconSize?: number
    mediumIconSize?: number
  }>(),
  {
    size: 'high',
    description: '暂无数据',
    highIconSize: 64,
    mediumIconSize: 48
  }
)
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';
@import '@/assets/styles/detail-list-panel-empty.scss';
</style>
