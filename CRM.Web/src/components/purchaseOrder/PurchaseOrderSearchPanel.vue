<script setup lang="ts">
import { computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import {
  PO_LIST_TIME_PRESET_IDS,
  type PoListPresetId,
  buildPoListRouteQuery,
  isPoListPresetId,
  pickPoListKeywordQuery,
  presetI18nKey
} from '@/utils/purchaseOrderListPreset'

const route = useRoute()
const router = useRouter()
const { t } = useI18n()

const activePreset = computed(() => {
  const p = route.query.preset
  return typeof p === 'string' && isPoListPresetId(p) ? p : null
})

const timePresets = PO_LIST_TIME_PRESET_IDS
const todoPresets = [
  'pending_submit_audit',
  'pending_vendor_confirm',
  'pending_submit_payment_request',
  'pending_submit_arrival_notify'
] as const satisfies readonly PoListPresetId[]

const documentPresets = [
  'has_purchase_order_docs',
  'no_purchase_order_docs'
] as const satisfies readonly PoListPresetId[]

const paymentPresets = [
  'pay_later',
  'confirmed_unpaid',
  'stocked_in_unpaid',
  'payment_partial',
  'payment_complete'
] as const satisfies readonly PoListPresetId[]

const stockInPresets = [
  'confirmed_pending_stock_in',
  'paid_pending_stock_in',
  'stocked_in'
] as const satisfies readonly PoListPresetId[]

function onPresetClick(id: PoListPresetId) {
  if (activePreset.value === id) {
    router.replace({ name: 'PurchaseOrderList', query: {} })
    return
  }
  const keywords = pickPoListKeywordQuery(route.query as Record<string, unknown>)
  router.replace({
    name: 'PurchaseOrderList',
    query: buildPoListRouteQuery({ preset: id, keywords })
  })
}
</script>

<template>
  <div class="po-search-panel">
    <div class="po-search-panel__head">{{ t('purchaseOrderList.searchPanel.title') }}</div>

    <section class="po-search-panel__group">
      <h4 class="po-search-panel__group-title">{{ t('purchaseOrderList.searchPanel.groups.time') }}</h4>
      <ul class="po-search-panel__list">
        <li v-for="id in timePresets" :key="id">
          <button
            type="button"
            class="po-search-panel__item"
            :class="{ 'is-active': activePreset === id }"
            @click="onPresetClick(id)"
          >
            {{ t(presetI18nKey(id)) }}
          </button>
        </li>
      </ul>
    </section>

    <section class="po-search-panel__group">
      <h4 class="po-search-panel__group-title">{{ t('purchaseOrderList.searchPanel.groups.todo') }}</h4>
      <ul class="po-search-panel__list">
        <li v-for="id in todoPresets" :key="id">
          <button
            type="button"
            class="po-search-panel__item"
            :class="{ 'is-active': activePreset === id }"
            @click="onPresetClick(id)"
          >
            {{ t(presetI18nKey(id)) }}
          </button>
        </li>
      </ul>
    </section>

    <section class="po-search-panel__group">
      <h4 class="po-search-panel__group-title">{{ t('purchaseOrderList.searchPanel.groups.docs') }}</h4>
      <ul class="po-search-panel__list">
        <li v-for="id in documentPresets" :key="id">
          <button
            type="button"
            class="po-search-panel__item"
            :class="{ 'is-active': activePreset === id }"
            @click="onPresetClick(id)"
          >
            {{ t(presetI18nKey(id)) }}
          </button>
        </li>
      </ul>
    </section>

    <section class="po-search-panel__group">
      <h4 class="po-search-panel__group-title">{{ t('purchaseOrderList.searchPanel.groups.payment') }}</h4>
      <ul class="po-search-panel__list">
        <li v-for="id in paymentPresets" :key="id">
          <button
            type="button"
            class="po-search-panel__item"
            :class="{ 'is-active': activePreset === id }"
            @click="onPresetClick(id)"
          >
            {{ t(presetI18nKey(id)) }}
          </button>
        </li>
      </ul>
    </section>

    <section class="po-search-panel__group">
      <h4 class="po-search-panel__group-title">{{ t('purchaseOrderList.searchPanel.groups.stockIn') }}</h4>
      <ul class="po-search-panel__list">
        <li v-for="id in stockInPresets" :key="id">
          <button
            type="button"
            class="po-search-panel__item"
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

.po-search-panel {
  min-height: 80px;
  font-size: 12px;
  color: $text-secondary;
}

.po-search-panel__head {
  font-weight: 600;
  color: $text-primary;
  margin-bottom: 12px;
  font-size: 13px;
}

.po-search-panel__group {
  margin-bottom: 14px;
}

.po-search-panel__group-title {
  margin: 0 0 6px;
  font-size: 11px;
  font-weight: 600;
  color: $text-muted;
}

.po-search-panel__list {
  list-style: none;
  margin: 0;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.po-search-panel__item {
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
