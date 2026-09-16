<script setup lang="ts">
import { computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import {
  SO_LIST_QUICK_FILTER_PRESET_IDS,
  SO_LIST_TIME_PRESET_IDS,
  type SoListPresetId,
  buildSoListRouteQuery,
  isSoListPresetId,
  pickSoListKeywordQuery,
  presetI18nKey
} from '@/utils/salesOrderListPreset'

const route = useRoute()
const router = useRouter()
const { t } = useI18n()

const activePreset = computed(() => {
  const p = route.query.preset
  return typeof p === 'string' && isSoListPresetId(p) ? p : null
})

const timePresets = SO_LIST_TIME_PRESET_IDS
const todoPresets = [
  'pending_submit_audit',
  'pending_submit_purchase_req',
  'pending_submit_stock_out_notify'
] as const satisfies readonly SoListPresetId[]

const documentPresets = [
  'has_sales_order_docs',
  'no_sales_order_docs'
] as const satisfies readonly SoListPresetId[]

const inventoryPresets = [
  'in_stock_pending_out',
  'used_stocking'
] as const satisfies readonly SoListPresetId[]

const receiptPresets = [
  'stock_out_pending_receipt',
  'receipt_partial',
  'receipt_complete'
] as const satisfies readonly SoListPresetId[]

const businessPresets = SO_LIST_QUICK_FILTER_PRESET_IDS.filter(
  (id) =>
    !(todoPresets as readonly string[]).includes(id) &&
    !(documentPresets as readonly string[]).includes(id) &&
    !(inventoryPresets as readonly string[]).includes(id) &&
    !(receiptPresets as readonly string[]).includes(id)
)

function onPresetClick(id: SoListPresetId) {
  if (activePreset.value === id) {
    router.replace({ name: 'SalesOrderList', query: {} })
    return
  }
  const keywords = pickSoListKeywordQuery(route.query as Record<string, unknown>)
  router.replace({
    name: 'SalesOrderList',
    query: buildSoListRouteQuery({ preset: id, keywords })
  })
}
</script>

<template>
  <div class="so-search-panel">
    <div class="so-search-panel__head">{{ t('salesOrderList.searchPanel.title') }}</div>

    <section class="so-search-panel__group">
      <h4 class="so-search-panel__group-title">{{ t('salesOrderList.searchPanel.groups.time') }}</h4>
      <ul class="so-search-panel__list">
        <li v-for="id in timePresets" :key="id">
          <button
            type="button"
            class="so-search-panel__item"
            :class="{ 'is-active': activePreset === id }"
            @click="onPresetClick(id)"
          >
            {{ t(presetI18nKey(id)) }}
          </button>
        </li>
      </ul>
    </section>

    <section class="so-search-panel__group">
      <h4 class="so-search-panel__group-title">{{ t('salesOrderList.searchPanel.groups.todo') }}</h4>
      <ul class="so-search-panel__list">
        <li v-for="id in todoPresets" :key="id">
          <button
            type="button"
            class="so-search-panel__item"
            :class="{ 'is-active': activePreset === id }"
            @click="onPresetClick(id)"
          >
            {{ t(presetI18nKey(id)) }}
          </button>
        </li>
      </ul>
    </section>

    <section class="so-search-panel__group">
      <h4 class="so-search-panel__group-title">{{ t('salesOrderList.searchPanel.groups.docs') }}</h4>
      <ul class="so-search-panel__list">
        <li v-for="id in documentPresets" :key="id">
          <button
            type="button"
            class="so-search-panel__item"
            :class="{ 'is-active': activePreset === id }"
            @click="onPresetClick(id)"
          >
            {{ t(presetI18nKey(id)) }}
          </button>
        </li>
      </ul>
    </section>

    <section class="so-search-panel__group">
      <h4 class="so-search-panel__group-title">{{ t('salesOrderList.searchPanel.groups.business') }}</h4>
      <ul class="so-search-panel__list">
        <li v-for="id in businessPresets" :key="id">
          <button
            type="button"
            class="so-search-panel__item"
            :class="{ 'is-active': activePreset === id }"
            @click="onPresetClick(id)"
          >
            {{ t(presetI18nKey(id)) }}
          </button>
        </li>
      </ul>
    </section>

    <section class="so-search-panel__group">
      <h4 class="so-search-panel__group-title">{{ t('salesOrderList.searchPanel.groups.inventory') }}</h4>
      <ul class="so-search-panel__list">
        <li v-for="id in inventoryPresets" :key="id">
          <button
            type="button"
            class="so-search-panel__item"
            :class="{ 'is-active': activePreset === id }"
            @click="onPresetClick(id)"
          >
            {{ t(presetI18nKey(id)) }}
          </button>
        </li>
      </ul>
    </section>

    <section class="so-search-panel__group">
      <h4 class="so-search-panel__group-title">{{ t('salesOrderList.searchPanel.groups.receipt') }}</h4>
      <ul class="so-search-panel__list">
        <li v-for="id in receiptPresets" :key="id">
          <button
            type="button"
            class="so-search-panel__item"
            :class="{ 'is-active': activePreset === id }"
            @click="onPresetClick(id)"
          >
            {{ t(presetI18nKey(id)) }}
          </button>
        </li>
      </ul>
    </section>
  </div>
</template>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.so-search-panel {
  min-height: 80px;
  font-size: 12px;
  color: $text-secondary;
}

.so-search-panel__head {
  font-weight: 600;
  color: $text-primary;
  margin-bottom: 12px;
  font-size: 13px;
}

.so-search-panel__group {
  margin-bottom: 14px;
}

.so-search-panel__group-title {
  margin: 0 0 6px;
  font-size: 11px;
  font-weight: 600;
  color: $text-muted;
}

.so-search-panel__list {
  list-style: none;
  margin: 0;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.so-search-panel__item {
  width: 100%;
  text-align: left;
  padding: 4px 10px;
  font-size: 12px;
  color: $text-secondary;
  background: transparent;
  border: 1px solid transparent;
  border-radius: 6px;
  cursor: pointer;
  transition: background 0.12s, border-color 0.12s, color 0.12s;

  &:hover {
    background: var(--crm-accent-008);
    border-color: var(--crm-accent-018);
    color: $text-primary;
  }

  &.is-active {
    background: var(--crm-accent-012);
    border-color: var(--crm-accent-04);
    color: $text-primary;
    font-weight: 500;
  }
}
</style>
