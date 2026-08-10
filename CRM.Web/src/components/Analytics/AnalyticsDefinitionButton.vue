<script setup lang="ts">
import { useI18n } from 'vue-i18n'

withDefaults(
  defineProps<{
    label?: string
    /** 首行：图（分解/趋势）或指标（KPI） */
    tipKind?: 'chart' | 'metric'
    chart?: string
    dataSource?: string
    text?: string
  }>(),
  { tipKind: 'chart' }
)

const { t } = useI18n()
</script>

<template>
  <el-popover placement="bottom-end" :width="360" trigger="click">
    <template #reference>
      <el-button link type="primary" size="small" class="def-btn">
        {{ label || t('salesAnalytics.definitionTip.button') }}
      </el-button>
    </template>
    <div class="definition-tip">
      <div v-if="chart" class="definition-tip__row">
        <span class="definition-tip__label">{{
          tipKind === 'metric'
            ? t('salesAnalytics.definitionTip.metric')
            : t('salesAnalytics.definitionTip.chart')
        }}</span>
        <span>{{ chart }}</span>
      </div>
      <div v-if="dataSource" class="definition-tip__row">
        <span class="definition-tip__label">{{ t('salesAnalytics.definitionTip.dataSource') }}</span>
        <span>{{ dataSource }}</span>
      </div>
      <div v-if="text" class="definition-tip__row">
        <span class="definition-tip__label">{{ t('salesAnalytics.definitionTip.definition') }}</span>
        <span class="definition-tip__text">{{ text }}</span>
      </div>
    </div>
  </el-popover>
</template>

<style scoped lang="scss">
.def-btn {
  padding: 0;
  height: auto;
  font-size: 12px;
}

.definition-tip {
  display: flex;
  flex-direction: column;
  gap: 8px;
  font-size: 13px;
  line-height: 1.5;
  color: var(--el-text-color-primary);
}

.definition-tip__row {
  display: grid;
  grid-template-columns: 64px 1fr;
  gap: 8px;
  align-items: start;
}

.definition-tip__label {
  color: var(--el-text-color-secondary);
  flex-shrink: 0;
}

.definition-tip__text {
  white-space: pre-wrap;
}
</style>
