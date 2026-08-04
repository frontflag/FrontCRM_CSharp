<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import type { SalesAnalyticsViewLevel } from '@/api/analytics/sales'

const props = withDefaults(
  defineProps<{
    modelValue: SalesAnalyticsViewLevel
    allowedLevels: SalesAnalyticsViewLevel[]
    saleDataScope?: number
    /** 与 saleDataScope 二选一；采购看板传 purchaseDataScope */
    dataScope?: number
    /** i18n 前缀，默认 salesAnalytics */
    i18nPrefix?: string
  }>(),
  { i18nPrefix: 'salesAnalytics' }
)

const effectiveDataScope = computed(() => props.dataScope ?? props.saleDataScope ?? 0)

const emit = defineEmits<{
  'update:modelValue': [SalesAnalyticsViewLevel]
}>()

const { t } = useI18n()

function isLevelAllowed(level: SalesAnalyticsViewLevel): boolean {
  return (props.allowedLevels ?? []).some((l) => String(l).toLowerCase() === level)
}

const tabs = computed(() => {
  const all: { key: SalesAnalyticsViewLevel; label: string; disabled: boolean }[] = [
    {
      key: 'company',
      label:
        effectiveDataScope.value === 0
          ? t(`${props.i18nPrefix}.tabs.company`)
          : t(`${props.i18nPrefix}.tabs.visibleScope`),
      disabled: !isLevelAllowed('company')
    },
    {
      key: 'department',
      label: t(`${props.i18nPrefix}.tabs.department`),
      disabled: !isLevelAllowed('department')
    },
    {
      key: 'personal',
      label: t(`${props.i18nPrefix}.tabs.personal`),
      disabled: !isLevelAllowed('personal')
    }
  ]
  return all
})

function onChange(key: SalesAnalyticsViewLevel) {
  const tab = tabs.value.find((x) => x.key === key)
  if (tab && !tab.disabled) emit('update:modelValue', key)
}
</script>

<template>
  <el-radio-group :model-value="modelValue" @change="onChange">
    <el-radio-button
      v-for="tab in tabs"
      :key="tab.key"
      :value="tab.key"
      :disabled="tab.disabled"
    >
      {{ tab.label }}
    </el-radio-button>
  </el-radio-group>
</template>
