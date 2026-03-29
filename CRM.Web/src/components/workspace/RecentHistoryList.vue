<script setup lang="ts">
import { useRecentHistoryList } from '@/composables/useRecentHistoryList'

const props = withDefaults(
  defineProps<{
    /** 与后端 BusinessLogTypes 等一致，如 Customer */
    bizType: string
    /** 默认 20 */
    take?: number
  }>(),
  { take: 20 }
)

const { loading, items, reload } = useRecentHistoryList(() => props.bizType, () => props.take)

defineExpose({ reload })
</script>

<template>
  <div class="recent-history-list" v-loading="loading">
    <slot :items="items" :loading="loading" :reload="reload" />
  </div>
</template>

<style scoped lang="scss">
.recent-history-list {
  min-height: 48px;
}
</style>
