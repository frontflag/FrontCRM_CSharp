<script setup lang="ts">
import { computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import {
  QC_STATUS_PRESET_IDS,
  QC_TIME_PRESET_IDS,
  type QcListPresetId,
  buildQcListRouteQuery,
  isQcListPresetId,
  pickQcKeywordQuery,
  presetI18nKey
} from '@/utils/qcListPreset'

const route = useRoute()
const router = useRouter()
const { t } = useI18n()

const activePreset = computed(() => {
  const p = route.query.preset
  return typeof p === 'string' && isQcListPresetId(p) ? p : null
})

function onPresetClick(id: QcListPresetId) {
  if (route.name !== 'QcList') return
  if (activePreset.value === id) {
    router.replace({ name: 'QcList', query: pickQcKeywordQuery(route.query as Record<string, unknown>) })
    return
  }
  const keywords = pickQcKeywordQuery(route.query as Record<string, unknown>)
  router.replace({
    name: 'QcList',
    query: buildQcListRouteQuery({ preset: id, keywords })
  })
}
</script>

<template>
  <div class="qc-search-panel">
    <div class="qc-search-panel__head">{{ t('qcList.searchPanel.title') }}</div>

    <section class="qc-search-panel__group">
      <h4 class="qc-search-panel__group-title">{{ t('qcList.searchPanel.groups.time') }}</h4>
      <ul class="qc-search-panel__list">
        <li v-for="id in QC_TIME_PRESET_IDS" :key="id">
          <button
            type="button"
            class="qc-search-panel__item"
            :class="{ 'is-active': activePreset === id }"
            @click="onPresetClick(id)"
          >
            {{ t(presetI18nKey(id)) }}
          </button>
        </li>
      </ul>
    </section>

    <section class="qc-search-panel__group">
      <h4 class="qc-search-panel__group-title">{{ t('qcList.searchPanel.groups.status') }}</h4>
      <ul class="qc-search-panel__list">
        <li v-for="id in QC_STATUS_PRESET_IDS" :key="id">
          <button
            type="button"
            class="qc-search-panel__item"
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

.qc-search-panel {
  min-height: 80px;
  font-size: 12px;
  color: $text-secondary;
}

.qc-search-panel__head {
  font-weight: 600;
  color: $text-primary;
  margin-bottom: 12px;
  font-size: 13px;
}

.qc-search-panel__group {
  margin-bottom: 14px;
}

.qc-search-panel__group-title {
  margin: 0 0 6px;
  font-size: 11px;
  font-weight: 600;
  color: $text-muted;
}

.qc-search-panel__list {
  list-style: none;
  margin: 0;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.qc-search-panel__item {
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
