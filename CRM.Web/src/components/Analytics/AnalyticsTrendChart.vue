<script setup lang="ts">
import { computed } from 'vue'

const props = defineProps<{
  points: { period: string; value: number }[]
  valueLabel?: string
}>()

const max = computed(() => Math.max(...props.points.map((p) => p.value), 1))
</script>

<template>
  <div class="trend-chart">
    <div v-if="points.length === 0" class="empty">{{ valueLabel || '' }} —</div>
    <div v-for="p in points" :key="p.period" class="trend-row">
      <span class="period">{{ p.period }}</span>
      <div class="bar-track">
        <div class="bar-fill" :style="{ width: `${(p.value / max) * 100}%` }" />
      </div>
      <span class="val">{{ p.value }}</span>
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

.trend-row {
  display: grid;
  grid-template-columns: 88px 1fr 72px;
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
}

.empty {
  color: var(--el-text-color-placeholder);
  padding: 24px 0;
}
</style>
