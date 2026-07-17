<script setup lang="ts">
import { computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import {
  PO_ITEM_TIME_PRESET_IDS,
  type PoItemListPresetId,
  buildPoItemListRouteQuery,
  isPoItemListPresetId,
  pickPoItemKeywordQuery,
  presetI18nKey
} from '@/utils/purchaseOrderItemListPreset'

const route = useRoute()
const router = useRouter()
const { t } = useI18n()

const activePreset = computed(() => {
  const p = route.query.preset
  return typeof p === 'string' && isPoItemListPresetId(p) ? p : null
})

const timePresets = PO_ITEM_TIME_PRESET_IDS
const todoPresets = [
  'pending_submit_audit',
  'pending_vendor_confirm',
  'pending_submit_payment_request',
  'pending_submit_arrival_notify'
] as const satisfies readonly PoItemListPresetId[]

const paymentPresets = [
  'pay_later',
  'confirmed_unpaid',
  'stocked_in_unpaid',
  'payment_partial',
  'payment_complete'
] as const satisfies readonly PoItemListPresetId[]

const stockInPresets = [
  'confirmed_pending_stock_in',
  'paid_pending_stock_in',
  'stocked_in'
] as const satisfies readonly PoItemListPresetId[]

function onPresetClick(id: PoItemListPresetId) {
  if (route.name !== 'PurchaseOrderItemList') return
  if (activePreset.value === id) {
    router.replace({ name: 'PurchaseOrderItemList', query: {} })
    return
  }
  const keywords = pickPoItemKeywordQuery(route.query as Record<string, unknown>)
  router.replace({
    name: 'PurchaseOrderItemList',
    query: buildPoItemListRouteQuery({ preset: id, keywords })
  })
}
</script>

<template>
  <div class="po-item-search-panel">
    <div class="po-item-search-panel__head">{{ t('purchaseOrderItemList.searchPanel.title') }}</div>

    <section class="po-item-search-panel__group">
      <h4 class="po-item-search-panel__group-title">{{ t('purchaseOrderItemList.searchPanel.groups.time') }}</h4>
      <ul class="po-item-search-panel__list">
        <li v-for="id in timePresets" :key="id">
          <button
            type="button"
            class="po-item-search-panel__item"
            :class="{ 'is-active': activePreset === id }"
            @click="onPresetClick(id)"
          >
            {{ t(presetI18nKey(id)) }}
          </button>
        </li>
      </ul>
    </section>

    <section class="po-item-search-panel__group">
      <h4 class="po-item-search-panel__group-title">{{ t('purchaseOrderItemList.searchPanel.groups.todo') }}</h4>
      <ul class="po-item-search-panel__list">
        <li v-for="id in todoPresets" :key="id">
          <button
            type="button"
            class="po-item-search-panel__item"
            :class="{ 'is-active': activePreset === id }"
            @click="onPresetClick(id)"
          >
            {{ t(presetI18nKey(id)) }}
          </button>
        </li>
      </ul>
    </section>

    <section class="po-item-search-panel__group">
      <h4 class="po-item-search-panel__group-title">{{ t('purchaseOrderItemList.searchPanel.groups.payment') }}</h4>
      <ul class="po-item-search-panel__list">
        <li v-for="id in paymentPresets" :key="id">
          <button
            type="button"
            class="po-item-search-panel__item"
            :class="{ 'is-active': activePreset === id }"
            @click="onPresetClick(id)"
          >
            {{ t(presetI18nKey(id)) }}
          </button>
        </li>
      </ul>
    </section>

    <section class="po-item-search-panel__group">
      <h4 class="po-item-search-panel__group-title">{{ t('purchaseOrderItemList.searchPanel.groups.stockIn') }}</h4>
      <ul class="po-item-search-panel__list">
        <li v-for="id in stockInPresets" :key="id">
          <button
            type="button"
            class="po-item-search-panel__item"
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

.po-item-search-panel {
  min-height: 80px;
  font-size: 12px;
  color: $text-secondary;
}

.po-item-search-panel__head {
  font-weight: 600;
  color: $text-primary;
  margin-bottom: 12px;
  font-size: 13px;
}

.po-item-search-panel__group {
  margin-bottom: 14px;
}

.po-item-search-panel__group-title {
  margin: 0 0 6px;
  font-size: 11px;
  font-weight: 600;
  color: $text-muted;
}

.po-item-search-panel__list {
  list-style: none;
  margin: 0;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.po-item-search-panel__item {
  width: 100%;
  text-align: left;
  padding: 7px 10px;
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
