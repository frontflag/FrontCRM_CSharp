<script setup lang="ts">
import { computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import {
  RFQ_ITEM_DEMAND_TIME_PRESET_IDS,
  RFQ_ITEM_QUOTE_TIME_PRESET_IDS,
  type RfqItemListPresetId,
  buildRfqItemListRouteQuery,
  isRfqItemListPresetId,
  pickRfqItemKeywordQuery,
  presetI18nKey
} from '@/utils/rfqItemListPreset'

const route = useRoute()
const router = useRouter()
const { t } = useI18n()

const activePreset = computed(() => {
  const p = route.query.preset
  return typeof p === 'string' && isRfqItemListPresetId(p) ? p : null
})

const demandTimePresets = RFQ_ITEM_DEMAND_TIME_PRESET_IDS
const demandStatusPresets = ['important', 'converted'] as const satisfies readonly RfqItemListPresetId[]
const quoteTimePresets = RFQ_ITEM_QUOTE_TIME_PRESET_IDS
const quoteStatusPresets = [
  'pending_quote',
  'no_quote',
  'multi_quote',
  'has_deleted_quote'
] as const satisfies readonly RfqItemListPresetId[]

function onPresetClick(id: RfqItemListPresetId) {
  if (route.name !== 'RFQItemList') return
  if (activePreset.value === id) {
    router.replace({ name: 'RFQItemList', query: {} })
    return
  }
  const keywords = pickRfqItemKeywordQuery(route.query as Record<string, unknown>)
  router.replace({
    name: 'RFQItemList',
    query: buildRfqItemListRouteQuery({ preset: id, keywords })
  })
}
</script>

<template>
  <div class="rfq-item-search-panel">
    <div class="rfq-item-search-panel__head">{{ t('rfqItemList.searchPanel.title') }}</div>

    <section class="rfq-item-search-panel__group">
      <h4 class="rfq-item-search-panel__group-title">{{ t('rfqItemList.searchPanel.groups.demand') }}</h4>
      <ul class="rfq-item-search-panel__list">
        <li v-for="id in demandTimePresets" :key="id">
          <button
            type="button"
            class="rfq-item-search-panel__item"
            :class="{ 'is-active': activePreset === id }"
            @click="onPresetClick(id)"
          >
            {{ t(presetI18nKey(id)) }}
          </button>
        </li>
      </ul>
    </section>

    <section class="rfq-item-search-panel__group">
      <h4 class="rfq-item-search-panel__group-title">{{ t('rfqItemList.searchPanel.groups.demandStatus') }}</h4>
      <ul class="rfq-item-search-panel__list">
        <li v-for="id in demandStatusPresets" :key="id">
          <button
            type="button"
            class="rfq-item-search-panel__item"
            :class="{ 'is-active': activePreset === id }"
            @click="onPresetClick(id)"
          >
            {{ t(presetI18nKey(id)) }}
          </button>
        </li>
      </ul>
    </section>

    <section class="rfq-item-search-panel__group">
      <h4 class="rfq-item-search-panel__group-title">{{ t('rfqItemList.searchPanel.groups.quote') }}</h4>
      <ul class="rfq-item-search-panel__list">
        <li v-for="id in quoteTimePresets" :key="id">
          <button
            type="button"
            class="rfq-item-search-panel__item"
            :class="{ 'is-active': activePreset === id }"
            @click="onPresetClick(id)"
          >
            {{ t(presetI18nKey(id)) }}
          </button>
        </li>
      </ul>
    </section>

    <section class="rfq-item-search-panel__group">
      <h4 class="rfq-item-search-panel__group-title">{{ t('rfqItemList.searchPanel.groups.quoteStatus') }}</h4>
      <ul class="rfq-item-search-panel__list">
        <li v-for="id in quoteStatusPresets" :key="id">
          <button
            type="button"
            class="rfq-item-search-panel__item"
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

.rfq-item-search-panel {
  min-height: 80px;
  font-size: 12px;
  color: $text-secondary;
}

.rfq-item-search-panel__head {
  font-weight: 600;
  color: $text-primary;
  margin-bottom: 12px;
  font-size: 13px;
}

.rfq-item-search-panel__group {
  margin-bottom: 14px;
}

.rfq-item-search-panel__group-title {
  margin: 0 0 6px;
  font-size: 11px;
  font-weight: 600;
  color: $text-muted;
}

.rfq-item-search-panel__list {
  list-style: none;
  margin: 0;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.rfq-item-search-panel__item {
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
