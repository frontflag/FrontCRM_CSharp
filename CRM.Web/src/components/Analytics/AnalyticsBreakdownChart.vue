<script setup lang="ts">
import type { SalesAnalyticsBreakdownItem } from '@/api/analytics/sales'
import AnalyticsDefinitionButton from './AnalyticsDefinitionButton.vue'

const props = withDefaults(
  defineProps<{
    title: string
    items: SalesAnalyticsBreakdownItem[]
    /** money：金额；number：计数/行数（默认，与全链路环节图例一致） */
    valueFormat?: 'money' | 'number'
    /** 标题行右侧单位说明（如「单位：折算美元（USD）」） */
    unitCaption?: string
    /** 标题右侧显示「详情」按钮（仅出库进度等需要时开启） */
    showDetail?: boolean
    detailLabel?: string
    /** 标题右侧显示「口径」按钮（popover） */
    showDefinition?: boolean
    definitionLabel?: string
    definitionChart?: string
    definitionDataSource?: string
    definitionText?: string
  }>(),
  { valueFormat: 'number' }
)

const emit = defineEmits<{
  detail: []
}>()

function formatValue(v: number): string {
  if (props.valueFormat === 'money') {
    // 看板金额分解值为折算美金（如 convert_total），与趋势图 money 一致
    return `$ ${v.toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`
  }
  return Number.isInteger(v) ? String(v) : v.toLocaleString('zh-CN', { maximumFractionDigits: 2 })
}

function formatRatio(ratio: number): string {
  return `${Number(ratio).toFixed(2)}%`
}
</script>

<template>
  <div class="breakdown">
    <div class="title-row">
      <h4 class="title">{{ title }}</h4>
      <div v-if="unitCaption || showDefinition || showDetail" class="title-right">
        <span v-if="unitCaption" class="unit-caption">{{ unitCaption }}</span>
        <div v-if="showDefinition || showDetail" class="title-actions">
          <AnalyticsDefinitionButton
            v-if="showDefinition"
            :label="definitionLabel"
            :chart="definitionChart"
            :data-source="definitionDataSource"
            :text="definitionText"
          />
          <el-button
            v-if="showDetail"
            link
            type="primary"
            size="small"
            class="action-btn"
            @click="emit('detail')"
          >
            {{ detailLabel || '详情' }}
          </el-button>
        </div>
      </div>
    </div>
    <div v-if="items.length === 0" class="empty">—</div>
    <div v-for="item in items" :key="item.key" class="row">
      <span class="label">{{ item.label }}</span>
      <div class="bar-track">
        <div class="bar-fill" :style="{ width: `${item.ratio}%` }" />
      </div>
      <span class="value">{{ formatValue(item.value) }}</span>
      <span class="ratio">{{ formatRatio(item.ratio) }}</span>
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

.title-right {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-shrink: 0;
  margin-left: auto;
}

.unit-caption {
  font-size: 12px;
  font-weight: 400;
  color: var(--el-text-color-secondary);
  white-space: nowrap;
}

.title-actions {
  display: flex;
  align-items: center;
  gap: 4px;
  flex-shrink: 0;
}

.action-btn {
  flex-shrink: 0;
}

.definition-tip {
  font-size: 13px;
  line-height: 1.5;
  color: var(--el-text-color-primary);
}

.definition-tip__row {
  display: grid;
  grid-template-columns: 64px 1fr;
  gap: 8px;
  margin-bottom: 8px;

  &:last-child {
    margin-bottom: 0;
  }
}

.definition-tip__label {
  color: var(--el-text-color-secondary);
  flex-shrink: 0;
}

.row {
  display: grid;
  grid-template-columns: 100px 1fr auto auto;
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

.value {
  color: var(--el-text-color-regular);
  text-align: right;
  white-space: nowrap;
}

.ratio {
  text-align: right;
  color: var(--el-text-color-secondary);
  white-space: nowrap;
  min-width: 52px;
}

.empty {
  color: var(--el-text-color-placeholder);
}
</style>
