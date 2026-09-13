<template>
  <div class="spark">
    <p v-if="!points.length" class="spark__empty">—</p>
    <svg v-else viewBox="0 0 240 56" preserveAspectRatio="none" class="spark__svg" aria-hidden="true">
      <polyline :points="line" fill="none" stroke="currentColor" stroke-width="2" stroke-linejoin="round" stroke-linecap="round" />
    </svg>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'

const props = defineProps<{
  points: { period: string; value: number }[]
}>()

const line = computed(() => {
  const vals = props.points.map((p) => (Number.isFinite(p.value) ? p.value : 0))
  const n = vals.length
  if (n === 0) return ''
  const min = Math.min(...vals)
  const max = Math.max(...vals)
  const span = max - min || 1
  return vals
    .map((v, i) => {
      const x = n === 1 ? 120 : (i / (n - 1)) * 240
      const y = 50 - ((v - min) / span) * 44
      return `${x.toFixed(1)},${y.toFixed(1)}`
    })
    .join(' ')
})
</script>

<style scoped lang="scss">
.spark {
  height: 56px;
  color: var(--el-color-primary);
}
.spark__svg {
  display: block;
  width: 100%;
  height: 56px;
}
.spark__empty {
  margin: 0;
  height: 56px;
  display: flex;
  align-items: center;
  color: var(--el-text-color-secondary);
  font-size: 13px;
}
</style>
