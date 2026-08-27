<script setup lang="ts">
import { computed } from 'vue'

const props = withDefaults(
  defineProps<{
    points: { period: string; value: number }[]
    valueLabel?: string
    /** 数值后缀单位，如「家」；与 valueFormat=number 搭配 */
    valueSuffix?: string
    /** 图表内单位说明 */
    unitCaption?: string
    /** money：美元；homeMoney：本位币（元） */
    valueFormat?: 'money' | 'homeMoney' | 'percent' | 'number'
  }>(),
  { valueFormat: 'number' }
)

const max = computed(() => Math.max(...props.points.map((p) => p.value), 1))

const valColumnClass = computed(() => {
  if (props.valueFormat === 'money' || props.valueFormat === 'homeMoney') return 'val val--money'
  return 'val'
})

function formatValue(v: number): string {
  if (props.valueFormat === 'money') {
    return `$ ${v.toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`
  }
  if (props.valueFormat === 'homeMoney') {
    return `¥ ${v.toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`
  }
  if (props.valueFormat === 'percent') {
    return `${v.toFixed(2)}%`
  }
  const qty = v.toLocaleString('zh-CN')
  if (props.valueSuffix) return `${qty} ${props.valueSuffix}`
  return qty
}
</script>

<template>
  <div class="trend-chart">
    <p v-if="unitCaption" class="unit-caption">{{ unitCaption }}</p>
    <div v-if="points.length === 0" class="empty">{{ valueLabel || '' }} —</div>
    <div v-for="p in points" :key="p.period" class="trend-row">
      <span class="period">{{ p.period }}</span>
      <div class="bar-track">
        <div class="bar-fill" :style="{ width: `${(p.value / max) * 100}%` }" />
      </div>
      <span :class="valColumnClass">{{ formatValue(p.value) }}</span>
    </div>
  </div>
</template>

<style scoped lang="scss">
.trend-chart {
  display: flex;
  flex-direction: column;
  gap: 8px;
  min-height: 200px;
}

.unit-caption {
  margin: 0 0 4px;
  font-size: 12px;
  color: var(--el-text-color-secondary);
}

.trend-row {
  display: grid;
  grid-template-columns: 88px 1fr minmax(88px, auto);
  align-items: center;
  gap: 8px;
  font-size: 12px;
}

.period {
  color: var(--el-text-color-secondary);
}

.bar-track {
  height: 10px;
  background: var(--el-fill-color-light);
  border-radius: 5px;
  overflow: hidden;
}

.bar-fill {
  height: 100%;
  background: var(--el-color-primary);
  border-radius: 5px;
  min-width: 2px;
}

.val {
  text-align: right;
  font-variant-numeric: tabular-nums;
  white-space: nowrap;
}

.val--money {
  font-size: 11px;
}

.empty {
  color: var(--el-text-color-placeholder);
  padding: 24px 0;
}
</style>
