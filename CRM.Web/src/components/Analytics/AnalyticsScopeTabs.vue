<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import type { SalesAnalyticsViewLevel } from '@/api/analytics/sales'

const props = defineProps<{
  modelValue: SalesAnalyticsViewLevel
  allowedLevels: SalesAnalyticsViewLevel[]
  saleDataScope: number
}>()

const emit = defineEmits<{
  'update:modelValue': [SalesAnalyticsViewLevel]
}>()

const { t } = useI18n()

const tabs = computed(() => {
  const all: { key: SalesAnalyticsViewLevel; label: string; disabled: boolean }[] = [
    {
      key: 'company',
      label:
        props.saleDataScope === 0
          ? t('salesAnalytics.tabs.company')
          : t('salesAnalytics.tabs.visibleScope'),
      disabled: !props.allowedLevels.includes('company')
    },
    {
      key: 'department',
      label: t('salesAnalytics.tabs.department'),
      disabled: !props.allowedLevels.includes('department')
    },
    {
      key: 'personal',
      label: t('salesAnalytics.tabs.personal'),
      disabled: !props.allowedLevels.includes('personal')
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
