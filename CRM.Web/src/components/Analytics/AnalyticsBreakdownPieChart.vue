<script setup lang="ts">
import { computed } from 'vue'
import type { SalesAnalyticsBreakdownItem } from '@/api/analytics/sales'

const props = withDefaults(
  defineProps<{
    title: string
    items: SalesAnalyticsBreakdownItem[]
    /** money：金额轴；number：计数轴（默认） */
    valueFormat?: 'money' | 'number'
  }>(),
  { valueFormat: 'number' }
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

function formatValue(v: number): string {
  if (props.valueFormat === 'money') {
    return `¥ ${v.toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`
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
    return {
      ...item,
      color: palette[index % palette.length],
      start,
      end: cursor,
      displayValue: formatValue(item.value)
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
    <h4 class="title">{{ title }}</h4>
    <div v-if="sortedItems.length === 0" class="empty">—</div>
    <div v-else class="body">
      <div class="pie" :style="pieStyle" />
      <ul class="legend">
        <li v-for="(item, index) in segments" :key="item.key">
          <span class="dot" :style="{ background: palette[index % palette.length] }" />
          <span class="label">{{ item.label }}</span>
          <span class="value">{{ item.displayValue }}</span>
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

.title {
  margin: 0 0 12px;
  font-size: 14px;
  font-weight: 600;
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
  grid-template-columns: 10px 1fr auto auto;
  gap: 8px;
  align-items: center;
  margin-bottom: 6px;
  font-size: 12px;
}

.dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
}

.label {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.value {
  color: var(--el-text-color-regular);
}

.ratio {
  color: var(--el-text-color-secondary);
  text-align: right;
}

.empty {
  color: var(--el-text-color-placeholder);
}
</style>
