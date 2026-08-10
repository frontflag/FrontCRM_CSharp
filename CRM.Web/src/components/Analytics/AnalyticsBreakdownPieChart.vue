<script setup lang="ts">
import { computed } from 'vue'
import type { SalesAnalyticsBreakdownItem } from '@/api/analytics/sales'
import { listAmountCurrencyDockClass } from '@/utils/moneyFormat'
import AnalyticsDefinitionButton from './AnalyticsDefinitionButton.vue'

const props = withDefaults(
  defineProps<{
    title: string
    items: SalesAnalyticsBreakdownItem[]
    /**
     * money：折算美金（$）；
     * originalCurrency：原币金额 + 右侧系统色币别（label/key 为币别）；
     * number：计数轴（默认）
     */
    valueFormat?: 'money' | 'number' | 'originalCurrency'
    /** 标题行右侧单位说明 */
    unitCaption?: string
    showDefinition?: boolean
    definitionLabel?: string
    definitionChart?: string
    definitionDataSource?: string
    definitionText?: string
  }>(),
  { valueFormat: 'number', showDefinition: false }
)

const palette = [
  '#409eff',
  '#67c23a',
  '#e6a23c',
  '#f56c6c',
  '#909399',
  '#b37feb',
  '#36cfc9'
]

const sortedItems = computed(() =>
  [...props.items].sort((a, b) => b.value - a.value || a.label.localeCompare(b.label))
)

function formatAmountNumber(v: number): string {
  return v.toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

function formatValue(v: number): string {
  if (props.valueFormat === 'money') {
    return `$ ${formatAmountNumber(v)}`
  }
  return Number.isInteger(v) ? String(v) : v.toLocaleString('zh-CN', { maximumFractionDigits: 2 })
}

const segments = computed(() => {
  const total = sortedItems.value.reduce((sum, item) => sum + item.value, 0)
  if (total <= 0) return []

  let cursor = 0
  return sortedItems.value.map((item, index) => {
    const pct = (item.value / total) * 100
    const start = cursor
    cursor += pct
    const currencyCode = Number(item.key)
    return {
      ...item,
      color: palette[index % palette.length],
      start,
      end: cursor,
      displayValue: formatValue(item.value),
      originalParts:
        props.valueFormat === 'originalCurrency'
          ? {
              amountText: formatAmountNumber(item.value),
              currencyLabel: item.label,
              currency: Number.isFinite(currencyCode) ? currencyCode : undefined,
              dockClass: listAmountCurrencyDockClass(
                Number.isFinite(currencyCode) ? currencyCode : undefined
              )
            }
          : null
    }
  })
})

const pieStyle = computed(() => {
  if (segments.value.length === 0) return {}
  const gradient = segments.value
    .map((s) => `${s.color} ${s.start}% ${s.end}%`)
    .join(', ')
  return { background: `conic-gradient(${gradient})` }
})
</script>

<template>
  <div class="pie-chart">
    <div class="title-row">
      <h4 class="title">{{ title }}</h4>
      <div v-if="unitCaption || showDefinition" class="title-right">
        <span v-if="unitCaption" class="unit-caption">{{ unitCaption }}</span>
        <AnalyticsDefinitionButton
          v-if="showDefinition"
          :label="definitionLabel"
          :chart="definitionChart"
          :data-source="definitionDataSource"
          :text="definitionText"
        />
      </div>
    </div>
    <div v-if="sortedItems.length === 0" class="empty">—</div>
    <div v-else class="body">
      <div class="pie" :style="pieStyle" />
      <ul class="legend">
        <li v-for="(item, index) in segments" :key="item.key">
          <span class="dot" :style="{ background: palette[index % palette.length] }" />
          <span class="label">{{ item.label }}</span>
          <span v-if="item.originalParts" class="value value--original">
            <span>{{ item.originalParts.amountText }}</span>
            <span class="dock-tier-ccy-gap">&nbsp;</span>
            <span :class="['dock-tier-ccy', item.originalParts.dockClass]">
              {{ item.originalParts.currencyLabel }}
            </span>
          </span>
          <span v-else class="value">{{ item.displayValue }}</span>
          <span class="ratio">{{ item.ratio.toFixed(2) }}%</span>
        </li>
      </ul>
    </div>
  </div>
</template>

<style scoped lang="scss">
.pie-chart {
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
}

.unit-caption {
  font-size: 12px;
  font-weight: 400;
  color: var(--el-text-color-secondary);
  white-space: nowrap;
  flex-shrink: 0;
}

.body {
  display: flex;
  flex-wrap: wrap;
  gap: 16px;
  align-items: center;
}

.pie {
  width: 140px;
  height: 140px;
  border-radius: 50%;
  flex-shrink: 0;
}

.legend {
  list-style: none;
  margin: 0;
  padding: 0;
  flex: 1;
  min-width: 180px;
}

.legend li {
  display: grid;
  grid-template-columns: 10px minmax(64px, 1fr) auto auto;
  gap: 8px;
  align-items: center;
  font-size: 12px;
  margin-bottom: 6px;
}

.dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
}

.label {
  color: var(--el-text-color-regular);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.value {
  font-variant-numeric: tabular-nums;
  color: var(--el-text-color-primary);
  white-space: nowrap;
}

.value--original {
  font-weight: 500;
}

.ratio {
  color: var(--el-text-color-secondary);
  min-width: 52px;
  text-align: right;
}

.empty {
  color: var(--el-text-color-secondary);
  font-size: 13px;
}
</style>
