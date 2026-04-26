<script setup lang="ts">
import { ref, onMounted, onBeforeUnmount } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { favoriteApi } from '@/api/favorite'
import { rfqApi } from '@/api/rfq'
import type { RFQ } from '@/types/rfq'
import { RFQ_FAVORITE_ENTITY_TYPE, RFQ_FAVORITES_CHANGED_EVENT } from '@/constants/rfqFavorites'

withDefaults(
  defineProps<{
    title?: string
  }>(),
  { title: '' }
)

const router = useRouter()
const { t } = useI18n()

const loading = ref(false)
const items = ref<RFQ[]>([])

function getStatusText(status: number) {
  const map: Record<number, string> = {
    0: t('rfqList.status.pending'),
    1: t('rfqList.status.assigned'),
    2: t('rfqList.status.processing'),
    3: t('rfqList.status.quoted'),
    4: t('rfqList.status.selected'),
    5: t('rfqList.status.converted'),
    6: t('rfqList.status.closed'),
    7: t('rfqList.status.closed'),
    8: t('rfqList.status.cancelled')
  }
  return map[status] ?? '—'
}

async function loadFavorites() {
  loading.value = true
  try {
    const ids = await favoriteApi.getFavoriteEntityIds(RFQ_FAVORITE_ENTITY_TYPE)
    if (ids.length === 0) {
      items.value = []
      return
    }
    const settled = await Promise.allSettled(ids.map((id) => rfqApi.getRFQById(id)))
    const list: RFQ[] = []
    for (const r of settled) {
      if (r.status !== 'fulfilled') continue
      list.push(r.value as RFQ)
    }
    items.value = list
  } catch {
    items.value = []
  } finally {
    loading.value = false
  }
}

function goDetail(row: RFQ) {
  router.push({ name: 'RFQDetail', params: { id: row.id } })
}

function onFavoritesChanged() {
  loadFavorites()
}

onMounted(() => {
  loadFavorites()
  window.addEventListener(RFQ_FAVORITES_CHANGED_EVENT, onFavoritesChanged)
})

onBeforeUnmount(() => {
  window.removeEventListener(RFQ_FAVORITES_CHANGED_EVENT, onFavoritesChanged)
})
</script>

<template>
  <div class="rfq-favorite-panel" v-loading="loading">
    <div class="rfq-favorite-panel__head">{{ title || t('leftPanel.rfqFavoritesTitle') }}</div>
    <div v-if="!loading && items.length === 0" class="rfq-favorite-panel__empty">
      {{ t('leftPanel.rfqFavoritesEmpty') }}
    </div>
    <table v-else class="rfq-favorite-panel__table">
      <thead>
        <tr>
          <th>{{ t('rfqList.columns.rfqCode') }}</th>
          <th>{{ t('rfqList.columns.customer') }}</th>
          <th>{{ t('rfqList.columns.status') }}</th>
        </tr>
      </thead>
      <tbody>
        <tr
          v-for="row in items"
          :key="row.id"
          class="rfq-favorite-panel__row"
          @click="goDetail(row)"
        >
          <td class="rfq-favorite-panel__code">{{ row.rfqCode || '—' }}</td>
          <td class="rfq-favorite-panel__name">{{ row.customerName || '—' }}</td>
          <td>{{ getStatusText(Number(row.status)) }}</td>
        </tr>
      </tbody>
    </table>
  </div>
</template>

<style scoped lang="scss">
@import url('https://fonts.googleapis.com/css2?family=Space+Mono&display=swap');

.rfq-favorite-panel {
  min-height: 80px;
}

.rfq-favorite-panel__head {
  font-weight: 600;
  color: #e8f4ff;
  margin-bottom: 10px;
  font-size: 13px;
}

.rfq-favorite-panel__empty {
  font-size: 12px;
  color: rgba(140, 180, 210, 0.75);
  line-height: 1.5;
}

.rfq-favorite-panel__table {
  width: 100%;
  border-collapse: collapse;
  font-size: 12px;
  color: rgba(200, 220, 240, 0.9);

  th {
    text-align: left;
    font-weight: 500;
    color: rgba(120, 190, 232, 0.65);
    padding: 6px 4px 8px;
    border-bottom: 1px solid rgba(0, 212, 255, 0.12);
    white-space: nowrap;
  }

  td {
    padding: 8px 4px;
    border-bottom: 1px solid rgba(0, 212, 255, 0.06);
    vertical-align: top;
  }
}

.rfq-favorite-panel__row {
  cursor: pointer;
  transition: background 0.12s;

  &:hover {
    background: rgba(0, 212, 255, 0.08);
  }
}

.rfq-favorite-panel__name {
  word-break: break-all;
  max-width: 120px;
}

.rfq-favorite-panel__code {
  font-family: 'Space Mono', ui-monospace, monospace;
  font-size: 11px;
  color: rgba(180, 210, 230, 0.85);
}
</style>
