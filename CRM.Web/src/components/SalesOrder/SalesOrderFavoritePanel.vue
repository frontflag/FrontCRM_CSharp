<script setup lang="ts">
import { ref, onMounted, onBeforeUnmount } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { favoriteApi } from '@/api/favorite'
import salesOrderApi from '@/api/salesOrder'
import { translateSalesOrderStatus } from '@/constants/salesOrderStatus'
import { SALES_ORDER_FAVORITE_ENTITY_TYPE, SALES_ORDER_FAVORITES_CHANGED_EVENT } from '@/constants/salesOrderFavorites'

withDefaults(
  defineProps<{
    title?: string
  }>(),
  { title: '' }
)

const router = useRouter()
const { t } = useI18n()

const loading = ref(false)
const items = ref<any[]>([])

function getStatusText(status: number) {
  try {
    return translateSalesOrderStatus(status, t)
  } catch {
    return '—'
  }
}

async function loadFavorites() {
  loading.value = true
  try {
    const ids = await favoriteApi.getFavoriteEntityIds(SALES_ORDER_FAVORITE_ENTITY_TYPE)
    if (ids.length === 0) {
      items.value = []
      return
    }
    const settled = await Promise.allSettled(ids.map((id) => salesOrderApi.getById(id)))
    const list: any[] = []
    for (const r of settled) {
      if (r.status !== 'fulfilled') continue
      list.push(r.value as any)
    }
    items.value = list
  } catch {
    items.value = []
  } finally {
    loading.value = false
  }
}

function goDetail(row: { id: string }) {
  router.push({ name: 'SalesOrderDetail', params: { id: row.id } })
}

function onFavoritesChanged() {
  loadFavorites()
}

onMounted(() => {
  loadFavorites()
  window.addEventListener(SALES_ORDER_FAVORITES_CHANGED_EVENT, onFavoritesChanged)
})

onBeforeUnmount(() => {
  window.removeEventListener(SALES_ORDER_FAVORITES_CHANGED_EVENT, onFavoritesChanged)
})
</script>

<template>
  <div class="so-favorite-panel" v-loading="loading">
    <div class="so-favorite-panel__head">{{ title || t('leftPanel.salesOrderFavoritesTitle') }}</div>
    <div v-if="!loading && items.length === 0" class="so-favorite-panel__empty">
      {{ t('leftPanel.salesOrderFavoritesEmpty') }}
    </div>
    <table v-else class="so-favorite-panel__table">
      <thead>
        <tr>
          <th>{{ t('salesOrderList.columns.orderCode') }}</th>
          <th>{{ t('salesOrderList.columns.customer') }}</th>
          <th>{{ t('salesOrderList.columns.status') }}</th>
        </tr>
      </thead>
      <tbody>
        <tr
          v-for="row in items"
          :key="row.id"
          class="so-favorite-panel__row"
          @click="goDetail(row)"
        >
          <td class="so-favorite-panel__code">{{ row.sellOrderCode || '—' }}</td>
          <td class="so-favorite-panel__name">{{ row.customerName || '—' }}</td>
          <td>{{ getStatusText(Number(row.status)) }}</td>
        </tr>
      </tbody>
    </table>
  </div>
</template>

<style scoped lang="scss">
.so-favorite-panel {
  min-height: 80px;
}

.so-favorite-panel__head {
  font-weight: 600;
  color: #e8f4ff;
  margin-bottom: 10px;
  font-size: 13px;
}

.so-favorite-panel__empty {
  font-size: 12px;
  color: rgba(140, 180, 210, 0.75);
  line-height: 1.5;
}

.so-favorite-panel__table {
  width: 100%;
  border-collapse: collapse;
  font-size: 11px;
}

.so-favorite-panel__table th {
  text-align: left;
  padding: 6px 4px;
  color: rgba(140, 180, 210, 0.75);
  font-weight: 500;
  border-bottom: 1px solid rgba(0, 212, 255, 0.12);
}

.so-favorite-panel__table td {
  padding: 8px 4px;
  border-bottom: 1px solid rgba(255, 255, 255, 0.06);
  color: rgba(224, 244, 255, 0.88);
}

.so-favorite-panel__row {
  cursor: pointer;
  transition: background 0.12s;

  &:hover {
    background: rgba(0, 212, 255, 0.06);
  }
}

.so-favorite-panel__code {
  font-family: 'Space Mono', ui-monospace, monospace;
  color: rgba(0, 212, 255, 0.95);
}

.so-favorite-panel__name {
  max-width: 120px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
</style>
