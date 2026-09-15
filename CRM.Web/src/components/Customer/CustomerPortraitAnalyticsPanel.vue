<template>
  <div class="customer-portrait-analytics">
    <div class="portrait-toolbar">
      <el-date-picker
        v-model="dateRange"
        type="daterange"
        value-format="YYYY-MM-DD"
        :start-placeholder="t('salesAnalytics.dateFrom')"
        :end-placeholder="t('salesAnalytics.dateTo')"
      />
    </div>

    <div class="tabs-nav portrait-sub-tabs">
      <button
        v-for="tab in subTabDefs"
        :key="tab.key"
        type="button"
        class="tab-btn"
        :class="{ 'tab-btn--active': subTab === tab.key }"
        @click="selectSubTab(tab.key)"
      >
        {{ t(`salesAnalytics.contentTabs.${tab.key}`) }}
      </button>
    </div>

    <RfqItemListBoard
      v-show="subTab === 'rfq'"
      mode="report"
      customer-scoped
      :report-query="reportQuery"
      :active="active && subTab === 'rfq'"
    />
    <SalesOrderItemListBoard
      v-show="subTab === 'order'"
      mode="report"
      customer-scoped
      :report-query="reportQuery"
      :active="active && subTab === 'order'"
    />
  </div>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import type { SalesAnalyticsQuery } from '@/api/analytics/sales'
import RfqItemListBoard from '@/views/RFQ/RfqItemListBoard.vue'
import SalesOrderItemListBoard from '@/views/RFQ/SalesOrderItemListBoard.vue'
import {
  CUSTOMER_PORTRAIT_SUB_TAB_KEYS,
  parseCustomerPortraitSubTab,
  type CustomerPortraitSubTab
} from '@/utils/customerDetailPrimaryTabs'

const props = defineProps<{
  customerId: string
  active: boolean
}>()

const { t } = useI18n()
const route = useRoute()
const router = useRouter()

const subTabDefs = CUSTOMER_PORTRAIT_SUB_TAB_KEYS.map((key) => ({ key }))
const subTab = ref<CustomerPortraitSubTab>(parseCustomerPortraitSubTab(route.query.portraitTab))
const dateRange = ref<[string, string] | null>(defaultDateRange())

const reportQuery = computed<SalesAnalyticsQuery>(() => {
  const range =
    dateRange.value && dateRange.value[0] && dateRange.value[1]
      ? dateRange.value
      : defaultDateRange()
  return {
    viewLevel: 'company',
    customerId: props.customerId,
    dateFrom: range[0],
    dateTo: range[1]
  }
})

function defaultDateRange(): [string, string] {
  const end = new Date()
  const start = new Date(end)
  start.setMonth(start.getMonth() - 5)
  return [formatDate(start), formatDate(end)]
}

function formatDate(d: Date): string {
  const y = d.getFullYear()
  const m = String(d.getMonth() + 1).padStart(2, '0')
  const day = String(d.getDate()).padStart(2, '0')
  return `${y}-${m}-${day}`
}

function selectSubTab(key: CustomerPortraitSubTab) {
  subTab.value = key
  if (String(route.query.portraitTab ?? '') === key) return
  void router.replace({ query: { ...route.query, portraitTab: key } })
}

watch(
  () => route.query.portraitTab,
  (raw) => {
    const parsed = parseCustomerPortraitSubTab(raw)
    if (subTab.value !== parsed) subTab.value = parsed
  }
)
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.customer-portrait-analytics {
  min-height: 240px;
}

.portrait-toolbar {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-bottom: 12px;
}

.portrait-sub-tabs {
  margin-bottom: 16px;
  background: $layer-2;
  border: 1px solid $border-card;
  border-radius: $border-radius-lg;
  overflow: hidden;
}

.tabs-nav {
  display: flex;
  gap: 4px;
  padding: 8px 12px;
  overflow-x: auto;
}

.tab-btn {
  padding: 6px 16px;
  background: transparent;
  border: none;
  border-radius: $border-radius-sm;
  color: $text-muted;
  font-size: 13px;
  cursor: pointer;
  white-space: nowrap;

  &:hover {
    color: $text-secondary;
    background: rgba(255, 255, 255, 0.04);
  }

  &--active {
    color: $text-primary;
    background: rgba(0, 212, 255, 0.12);
  }
}
</style>
