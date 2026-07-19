<script setup lang="ts">
import { computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import {
  CUSTOMER_ATTENTION_PRESET_IDS,
  CUSTOMER_BUSINESS_PRESET_IDS,
  CUSTOMER_DEAL_PRESET_IDS,
  CUSTOMER_DEMAND_PRESET_IDS,
  CUSTOMER_TIME_PRESET_IDS,
  CUSTOMER_TODO_PRESET_IDS,
  type CustomerListPresetId,
  buildCustomerListRouteQuery,
  isCustomerListPresetId,
  pickCustomerKeywordQuery,
  presetI18nKey
} from '@/utils/customerListPreset'

const route = useRoute()
const router = useRouter()
const { t } = useI18n()

const activePreset = computed(() => {
  const p = route.query.preset
  return typeof p === 'string' && isCustomerListPresetId(p) ? p : null
})

function onPresetClick(id: CustomerListPresetId) {
  const keywords = pickCustomerKeywordQuery(route.query as Record<string, unknown>)

  if (route.name === 'CustomerList' && activePreset.value === id) {
    router.replace({ name: 'CustomerList', query: {} })
    return
  }

  const query = buildCustomerListRouteQuery({ preset: id, keywords })
  if (route.name === 'CustomerList') {
    router.replace({ name: 'CustomerList', query })
  } else {
    router.push({ name: 'CustomerList', query })
  }
}
</script>

<template>
  <div class="customer-search-panel">
    <div class="customer-search-panel__head">{{ t('customerList.searchPanel.title') }}</div>

    <section class="customer-search-panel__group">
      <h4 class="customer-search-panel__group-title">{{ t('customerList.searchPanel.groups.time') }}</h4>
      <ul class="customer-search-panel__list">
        <li v-for="id in CUSTOMER_TIME_PRESET_IDS" :key="id">
          <button
            type="button"
            class="customer-search-panel__item"
            :class="{ 'is-active': activePreset === id }"
            @click="onPresetClick(id)"
          >
            {{ t(presetI18nKey(id)) }}
          </button>
        </li>
      </ul>
    </section>

    <section class="customer-search-panel__group">
      <h4 class="customer-search-panel__group-title">{{ t('customerList.searchPanel.groups.attention') }}</h4>
      <ul class="customer-search-panel__list">
        <li v-for="id in CUSTOMER_ATTENTION_PRESET_IDS" :key="id">
          <button
            type="button"
            class="customer-search-panel__item"
            :class="{ 'is-active': activePreset === id }"
            @click="onPresetClick(id)"
          >
            {{ t(presetI18nKey(id)) }}
          </button>
        </li>
      </ul>
    </section>

    <section class="customer-search-panel__group">
      <h4 class="customer-search-panel__group-title">{{ t('customerList.searchPanel.groups.todo') }}</h4>
      <ul class="customer-search-panel__list">
        <li v-for="id in CUSTOMER_TODO_PRESET_IDS" :key="id">
          <button
            type="button"
            class="customer-search-panel__item"
            :class="{ 'is-active': activePreset === id }"
            @click="onPresetClick(id)"
          >
            {{ t(presetI18nKey(id)) }}
          </button>
        </li>
      </ul>
    </section>

    <section class="customer-search-panel__group">
      <h4 class="customer-search-panel__group-title">{{ t('customerList.searchPanel.groups.demand') }}</h4>
      <ul class="customer-search-panel__list">
        <li v-for="id in CUSTOMER_DEMAND_PRESET_IDS" :key="id">
          <button
            type="button"
            class="customer-search-panel__item"
            :class="{ 'is-active': activePreset === id }"
            @click="onPresetClick(id)"
          >
            {{ t(presetI18nKey(id)) }}
          </button>
        </li>
      </ul>
    </section>

    <section class="customer-search-panel__group">
      <h4 class="customer-search-panel__group-title">{{ t('customerList.searchPanel.groups.deal') }}</h4>
      <ul class="customer-search-panel__list">
        <li v-for="id in CUSTOMER_DEAL_PRESET_IDS" :key="id">
          <button
            type="button"
            class="customer-search-panel__item"
            :class="{ 'is-active': activePreset === id }"
            @click="onPresetClick(id)"
          >
            {{ t(presetI18nKey(id)) }}
          </button>
        </li>
      </ul>
    </section>

    <section class="customer-search-panel__group">
      <h4 class="customer-search-panel__group-title">{{ t('customerList.searchPanel.groups.business') }}</h4>
      <ul class="customer-search-panel__list">
        <li v-for="id in CUSTOMER_BUSINESS_PRESET_IDS" :key="id">
          <button
            type="button"
            class="customer-search-panel__item"
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

.customer-search-panel {
  min-height: 80px;
  font-size: 12px;
  color: $text-secondary;
}

.customer-search-panel__head {
  font-weight: 600;
  color: $text-primary;
  margin-bottom: 12px;
  font-size: 13px;
}

.customer-search-panel__group {
  margin-bottom: 14px;
}

.customer-search-panel__group-title {
  margin: 0 0 6px;
  font-size: 11px;
  font-weight: 600;
  color: $text-muted;
}

.customer-search-panel__list {
  list-style: none;
  margin: 0;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.customer-search-panel__item {
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
