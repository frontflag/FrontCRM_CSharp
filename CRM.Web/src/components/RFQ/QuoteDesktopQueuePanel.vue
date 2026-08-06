<template>
  <div class="quote-desktop-queue-panel">
    <div class="qd-queue-filters">
      <el-select
        :model-value="dateFilter"
        size="small"
        class="qd-queue-filters__select"
        @update:model-value="onDateFilter"
      >
        <el-option :label="t('quoteDesktop.dateFilter.all')" value="" />
        <el-option :label="t('quoteDesktop.dateFilter.today')" value="today" />
        <el-option :label="t('quoteDesktop.dateFilter.yesterday')" value="yesterday" />
        <el-option :label="t('quoteDesktop.dateFilter.dayBefore')" value="dayBefore" />
        <el-option :label="t('quoteDesktop.dateFilter.before3')" value="before3" />
      </el-select>
    </div>

    <div ref="listEl" class="quote-desktop-queue-panel__list" v-loading="loading">
      <button
        v-for="item in items"
        :key="item.id"
        type="button"
        class="qd-queue-item"
        :class="{ 'is-selected': selectedId === item.id }"
        :data-qd-key="item.id"
        @click="onSelect(item)"
      >
        <div class="qd-queue-item__row qd-queue-item__row--head">
          <span class="qd-queue-item__code" :title="item.rfqCode">{{ item.rfqCode || '—' }}</span>
          <span class="qd-queue-item__time">{{ formatDate(item.createTime) }}</span>
        </div>
        <div class="qd-queue-item__row">
          <span class="qd-queue-item__mpn" :title="item.mpn">{{ item.mpn || '—' }}</span>
          <span class="qd-queue-item__brand" :title="item.brand">{{ item.brand || '—' }}</span>
        </div>
        <div class="qd-queue-item__row qd-queue-item__meta">
          <span :title="item.salesUserName">{{ item.salesUserName || '—' }}</span>
          <span :title="item.purchaserNames">{{ item.purchaserNames || '—' }}</span>
        </div>
      </button>
      <div v-if="!loading && !items.length" class="qd-queue-empty">
        {{ t('quoteDesktop.empty.queue') }}
      </div>
    </div>

    <div v-if="total > 0" class="qd-queue-pager">
      <el-pagination
        small
        layout="prev, pager, next"
        :current-page="page"
        :page-size="pageSize"
        :total="total"
        @current-change="onPageChange"
      />
    </div>
  </div>
</template>

<script setup lang="ts">
import { nextTick, onMounted, ref, watch } from 'vue'
import { storeToRefs } from 'pinia'
import { useI18n } from 'vue-i18n'
import {
  useQuoteDesktopQueueStore,
  type QuoteDesktopDateFilter,
  type QuoteDesktopQueueItem
} from '@/stores/quoteDesktopQueue'
import { formatDisplayDateTime } from '@/utils/displayDateTime'

const { t } = useI18n()
const queueStore = useQuoteDesktopQueueStore()
const { loading, items, selectedId, dateFilter, page, pageSize, total, scrollToSelectedNonce } =
  storeToRefs(queueStore)
const listEl = ref<HTMLElement | null>(null)

function formatDate(v?: string) {
  if (!v) return '—'
  return formatDisplayDateTime(v) || '—'
}

function onSelect(item: QuoteDesktopQueueItem) {
  queueStore.selectItem(item)
}

async function onDateFilter(v: QuoteDesktopDateFilter | string) {
  await queueStore.setDateFilter((v || '') as QuoteDesktopDateFilter)
}

async function onPageChange(p: number) {
  await queueStore.setPage(p)
}

async function scrollSelectedIntoView() {
  await nextTick()
  const key = selectedId.value
  if (!key || !listEl.value) return
  const el = listEl.value.querySelector(`[data-qd-key="${CSS.escape(key)}"]`) as HTMLElement | null
  el?.scrollIntoView({ block: 'nearest', behavior: 'smooth' })
}

watch(
  () => `${selectedId.value}:${scrollToSelectedNonce.value}`,
  () => {
    void scrollSelectedIntoView()
  }
)

onMounted(() => {
  void queueStore.refreshAll().catch(() => {
    /* 主区提示 */
  })
  void scrollSelectedIntoView()
})
</script>

<style scoped lang="scss">
.quote-desktop-queue-panel {
  display: flex;
  flex-direction: column;
  height: 100%;
  min-height: 0;
}

.qd-queue-filters {
  padding: 8px 10px 6px;
  flex-shrink: 0;

  &__select {
    width: 100%;
  }
}

.quote-desktop-queue-panel__list {
  flex: 1;
  min-height: 0;
  overflow: auto;
  padding: 0 6px 8px;
}

.qd-queue-item {
  display: block;
  width: 100%;
  text-align: left;
  border: 1px solid transparent;
  border-radius: 8px;
  background: transparent;
  padding: 8px 10px;
  margin-bottom: 4px;
  cursor: pointer;
  color: inherit;
  font: inherit;

  &:hover {
    background: rgba(64, 158, 255, 0.06);
  }

  &.is-selected {
    background: rgba(64, 158, 255, 0.12);
    border-color: rgba(64, 158, 255, 0.35);
  }

  &__row {
    display: flex;
    align-items: baseline;
    justify-content: space-between;
    gap: 8px;
    min-width: 0;
  }

  &__row--head {
    margin-bottom: 2px;
  }

  &__code {
    font-weight: 600;
    font-size: 13px;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
  }

  &__time {
    flex-shrink: 0;
    font-size: 11px;
    opacity: 0.7;
  }

  &__mpn {
    font-size: 12px;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
  }

  &__brand {
    flex-shrink: 0;
    font-size: 12px;
    opacity: 0.85;
  }

  &__meta {
    margin-top: 2px;
    font-size: 11px;
    opacity: 0.65;

    span {
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
      max-width: 48%;
    }
  }
}

.qd-queue-empty {
  padding: 24px 12px;
  text-align: center;
  font-size: 12px;
  opacity: 0.65;
}

.qd-queue-pager {
  flex-shrink: 0;
  padding: 6px 8px 10px;
  display: flex;
  justify-content: center;
  border-top: 1px solid rgba(0, 0, 0, 0.06);
}
</style>
