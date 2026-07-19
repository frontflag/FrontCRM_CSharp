<script setup lang="ts">
import { computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import {
  ARRIVAL_NOTICE_ARRIVED_PRESET_IDS,
  ARRIVAL_NOTICE_EXPECTED_PRESET_IDS,
  ARRIVAL_NOTICE_OVERDUE_PRESET_IDS,
  ARRIVAL_NOTICE_STATUS_PRESET_IDS,
  ARRIVAL_NOTICE_TODO_PRESET_IDS,
  ARRIVAL_NOTICE_TYPE_PRESET_IDS,
  type ArrivalNoticeListPresetId,
  buildArrivalNoticeListRouteQuery,
  isArrivalNoticeListPresetId,
  pickArrivalNoticeKeywordQuery,
  presetI18nKey
} from '@/utils/arrivalNoticeListPreset'

const route = useRoute()
const router = useRouter()
const { t } = useI18n()

const activePreset = computed(() => {
  const p = route.query.preset
  return typeof p === 'string' && isArrivalNoticeListPresetId(p) ? p : null
})

function onPresetClick(id: ArrivalNoticeListPresetId) {
  if (route.name !== 'ArrivalNoticeList') return
  if (activePreset.value === id) {
    router.replace({ name: 'ArrivalNoticeList', query: {} })
    return
  }
  const keywords = pickArrivalNoticeKeywordQuery(route.query as Record<string, unknown>)
  router.replace({
    name: 'ArrivalNoticeList',
    query: buildArrivalNoticeListRouteQuery({ preset: id, keywords })
  })
}
</script>

<template>
  <div class="an-search-panel">
    <div class="an-search-panel__head">{{ t('arrivalNoticeList.searchPanel.title') }}</div>

    <section class="an-search-panel__group">
      <h4 class="an-search-panel__group-title">{{ t('arrivalNoticeList.searchPanel.groups.overdue') }}</h4>
      <ul class="an-search-panel__list">
        <li v-for="id in ARRIVAL_NOTICE_OVERDUE_PRESET_IDS" :key="id">
          <button
            type="button"
            class="an-search-panel__item"
            :class="{ 'is-active': activePreset === id }"
            @click="onPresetClick(id)"
          >
            {{ t(presetI18nKey(id)) }}
          </button>
        </li>
      </ul>
    </section>

    <section class="an-search-panel__group">
      <h4 class="an-search-panel__group-title">{{ t('arrivalNoticeList.searchPanel.groups.expected') }}</h4>
      <ul class="an-search-panel__list">
        <li v-for="id in ARRIVAL_NOTICE_EXPECTED_PRESET_IDS" :key="id">
          <button
            type="button"
            class="an-search-panel__item"
            :class="{ 'is-active': activePreset === id }"
            @click="onPresetClick(id)"
          >
            {{ t(presetI18nKey(id)) }}
          </button>
        </li>
      </ul>
    </section>

    <section class="an-search-panel__group">
      <h4 class="an-search-panel__group-title">{{ t('arrivalNoticeList.searchPanel.groups.arrived') }}</h4>
      <ul class="an-search-panel__list">
        <li v-for="id in ARRIVAL_NOTICE_ARRIVED_PRESET_IDS" :key="id">
          <button
            type="button"
            class="an-search-panel__item"
            :class="{ 'is-active': activePreset === id }"
            @click="onPresetClick(id)"
          >
            {{ t(presetI18nKey(id)) }}
          </button>
        </li>
      </ul>
    </section>

    <section class="an-search-panel__group">
      <h4 class="an-search-panel__group-title">{{ t('arrivalNoticeList.searchPanel.groups.type') }}</h4>
      <ul class="an-search-panel__list">
        <li v-for="id in ARRIVAL_NOTICE_TYPE_PRESET_IDS" :key="id">
          <button
            type="button"
            class="an-search-panel__item"
            :class="{ 'is-active': activePreset === id }"
            @click="onPresetClick(id)"
          >
            {{ t(presetI18nKey(id)) }}
          </button>
        </li>
      </ul>
    </section>

    <section class="an-search-panel__group">
      <h4 class="an-search-panel__group-title">{{ t('arrivalNoticeList.searchPanel.groups.todo') }}</h4>
      <ul class="an-search-panel__list">
        <li v-for="id in ARRIVAL_NOTICE_TODO_PRESET_IDS" :key="id">
          <button
            type="button"
            class="an-search-panel__item"
            :class="{ 'is-active': activePreset === id }"
            @click="onPresetClick(id)"
          >
            {{ t(presetI18nKey(id)) }}
          </button>
        </li>
      </ul>
    </section>

    <section class="an-search-panel__group">
      <h4 class="an-search-panel__group-title">{{ t('arrivalNoticeList.searchPanel.groups.status') }}</h4>
      <ul class="an-search-panel__list">
        <li v-for="id in ARRIVAL_NOTICE_STATUS_PRESET_IDS" :key="id">
          <button
            type="button"
            class="an-search-panel__item"
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

.an-search-panel {
  min-height: 80px;
  font-size: 12px;
  color: $text-secondary;
}

.an-search-panel__head {
  font-weight: 600;
  color: $text-primary;
  margin-bottom: 12px;
  font-size: 13px;
}

.an-search-panel__group {
  margin-bottom: 14px;
}

.an-search-panel__group-title {
  margin: 0 0 6px;
  font-size: 11px;
  font-weight: 600;
  color: $text-muted;
}

.an-search-panel__list {
  list-style: none;
  margin: 0;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.an-search-panel__item {
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
