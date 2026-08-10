<script setup lang="ts">
import type { SalesAnalyticsBreakdownItem } from '@/api/analytics/sales'

defineProps<{
  title: string
  items: SalesAnalyticsBreakdownItem[]
  /** 标题右侧显示「详情」按钮（仅出库进度等需要时开启） */
  showDetail?: boolean
  detailLabel?: string
}>()

const emit = defineEmits<{
  detail: []
}>()
</script>

<template>
  <div class="breakdown">
    <div class="title-row">
      <h4 class="title">{{ title }}</h4>
      <el-button
        v-if="showDetail"
        link
        type="primary"
        size="small"
        class="detail-btn"
        @click="emit('detail')"
      >
        {{ detailLabel || '详情' }}
      </el-button>
    </div>
    <div v-if="items.length === 0" class="empty">—</div>
    <div v-for="item in items" :key="item.key" class="row">
      <span class="label">{{ item.label }}</span>
      <div class="bar-track">
        <div class="bar-fill" :style="{ width: `${item.ratio}%` }" />
      </div>
      <span class="ratio">{{ item.ratio }}%</span>
    </div>
  </div>
</template>

<style scoped lang="scss">
.breakdown {
  min-height: 200px;
}

.title-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
  margin-bottom: 12px;
}

.title {
  margin: 0;
  font-size: 14px;
  font-weight: 600;
}

.detail-btn {
  flex-shrink: 0;
}

.row {
  display: grid;
  grid-template-columns: 100px 1fr 48px;
  gap: 8px;
  align-items: center;
  margin-bottom: 8px;
  font-size: 12px;
}

.label {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.bar-track {
  height: 8px;
  background: var(--el-fill-color-light);
  border-radius: 4px;
}

.bar-fill {
  height: 100%;
  background: var(--el-color-success);
  border-radius: 4px;
}

.ratio {
  text-align: right;
  color: var(--el-text-color-secondary);
}

.empty {
  color: var(--el-text-color-placeholder);
}
</style>
