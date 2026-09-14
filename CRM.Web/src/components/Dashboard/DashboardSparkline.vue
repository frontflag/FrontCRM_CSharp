<template>
  <div class="spark">
    <p v-if="!points.length" class="spark__empty">—</p>
    <div
      v-else
      class="spark__wrap"
      @pointermove="onMove"
      @pointerleave="hoverIndex = null"
    >
      <svg viewBox="0 0 240 56" preserveAspectRatio="none" class="spark__svg" aria-hidden="true">
        <polyline
          :points="line"
          fill="none"
          stroke="currentColor"
          stroke-width="2"
          stroke-linejoin="round"
          stroke-linecap="round"
        />
      </svg>
      <template v-if="hover">
        <span class="spark__guide" :style="{ left: hover.xPct + '%' }" />
        <span class="spark__dot" :style="{ left: hover.xPct + '%', top: hover.yPct + '%' }" />
        <div class="spark__tip" :class="hover.tipClass" :style="{ left: hover.xPct + '%' }">
          <div class="spark__tip-val">
            <span v-if="hover.prefix" class="spark__tip-prefix">{{ hover.prefix }}</span>
            <span>{{ hover.value }}</span>
          </div>
          <div class="spark__tip-date">{{ hover.date }}</div>
        </div>
      </template>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue'

const props = withDefaults(
  defineProps<{
    points: { period: string; value: number }[]
    valueFormat?: 'number' | 'money'
    valuePrefix?: string
    masked?: boolean
  }>(),
  { valueFormat: 'number', valuePrefix: '', masked: false }
)

const hoverIndex = ref<number | null>(null)

const layout = computed(() => {
  const vals = props.points.map((p) => (Number.isFinite(p.value) ? p.value : 0))
  const n = vals.length
  const min = n ? Math.min(...vals) : 0
  const max = n ? Math.max(...vals) : 0
  const span = max - min || 1
  return { vals, n, min, span }
})

const line = computed(() => {
  const { vals, n, min, span } = layout.value
  if (n === 0) return ''
  return vals
    .map((v, i) => {
      const x = n === 1 ? 120 : (i / (n - 1)) * 240
      const y = 50 - ((v - min) / span) * 44
      return `${x.toFixed(1)},${y.toFixed(1)}`
    })
    .join(' ')
})

function formatPeriod(raw: string) {
  const m = raw.match(/^(\d{4})-(\d{2})-(\d{2})/)
  if (m) return `${m[1]}-${m[2]}-${m[3]}`
  return raw
}

function formatValue(v: number) {
  if (props.masked) return '—'
  if (props.valueFormat === 'money') {
    return v.toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
  }
  return v.toLocaleString('zh-CN')
}

const hover = computed(() => {
  const i = hoverIndex.value
  if (i == null) return null
  const { vals, n, min, span } = layout.value
  if (i < 0 || i >= n) return null
  const xPct = n === 1 ? 50 : (i / (n - 1)) * 100
  const y = 50 - ((vals[i] - min) / span) * 44
  const yPct = (y / 56) * 100
  const tipClass = xPct > 78 ? 'is-right' : xPct < 22 ? 'is-left' : 'is-center'
  return {
    xPct,
    yPct,
    date: formatPeriod(props.points[i].period),
    prefix: props.valuePrefix || '',
    value: formatValue(vals[i]),
    tipClass
  }
})

function onMove(e: PointerEvent) {
  const rect = (e.currentTarget as HTMLElement).getBoundingClientRect()
  const { n } = layout.value
  if (n === 0 || rect.width <= 0) return
  const t = (e.clientX - rect.left) / rect.width
  hoverIndex.value = n === 1 ? 0 : Math.min(n - 1, Math.max(0, Math.round(t * (n - 1))))
}
</script>

<style scoped lang="scss">
.spark {
  height: 56px;
  color: var(--el-color-primary);
}

.spark__wrap {
  position: relative;
  height: 56px;
  cursor: crosshair;
}

.spark__svg {
  display: block;
  width: 100%;
  height: 56px;
}

.spark__guide {
  position: absolute;
  top: 0;
  bottom: 0;
  width: 1px;
  background: var(--el-border-color);
  transform: translateX(-50%);
  pointer-events: none;
}

.spark__dot {
  position: absolute;
  width: 8px;
  height: 8px;
  border-radius: 50%;
  background: var(--el-color-primary);
  border: 2px solid var(--el-bg-color);
  box-shadow: 0 0 0 1px var(--el-color-primary);
  transform: translate(-50%, -50%);
  pointer-events: none;
}

.spark__tip {
  position: absolute;
  bottom: calc(100% + 6px);
  z-index: 3;
  padding: 6px 8px;
  border-radius: 6px;
  background: var(--el-bg-color-overlay);
  border: 1px solid var(--el-border-color-lighter);
  box-shadow: var(--el-box-shadow-light);
  pointer-events: none;
  white-space: nowrap;
  &.is-center {
    transform: translateX(-50%);
  }
  &.is-left {
    transform: translateX(0);
  }
  &.is-right {
    transform: translateX(-100%);
  }
}

.spark__tip-date {
  margin-top: 2px;
  font-size: 11px;
  color: var(--el-text-color-secondary);
}

.spark__tip-val {
  font-size: 13px;
  font-weight: 600;
  font-variant-numeric: tabular-nums;
  color: var(--el-text-color-primary);
}

.spark__tip-prefix {
  font-weight: 400;
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
