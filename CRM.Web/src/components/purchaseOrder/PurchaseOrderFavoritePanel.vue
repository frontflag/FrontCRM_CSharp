<script setup lang="ts">
import { ref, onMounted, onBeforeUnmount } from 'vue'
import { useRouter } from 'vue-router'
import { favoriteApi } from '@/api/favorite'
import { purchaseOrderApi } from '@/api/purchaseOrder'
import {
  PURCHASE_ORDER_FAVORITE_ENTITY_TYPE,
  PURCHASE_ORDER_FAVORITES_CHANGED_EVENT
} from '@/constants/purchaseOrderFavorites'

withDefaults(
  defineProps<{
    title?: string
  }>(),
  { title: '收藏的采购订单' }
)

const router = useRouter()

const loading = ref(false)
const items = ref<any[]>([])

function getStatusText(status: number) {
  const map: Record<number, string> = {
    1: '新建',
    2: '待审核',
    10: '审核通过',
    20: '待确认',
    30: '已确认',
    50: '进行中',
    100: '采购完成',
    [-1]: '审核失败',
    [-2]: '取消'
  }
  return map[status] ?? '—'
}

async function loadFavorites() {
  loading.value = true
  try {
    const ids = await favoriteApi.getFavoriteEntityIds(PURCHASE_ORDER_FAVORITE_ENTITY_TYPE)
    if (ids.length === 0) {
      items.value = []
      return
    }
    const settled = await Promise.allSettled(ids.map((id) => purchaseOrderApi.getById(id)))
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
  router.push({ name: 'PurchaseOrderDetail', params: { id: row.id } })
}

function onFavoritesChanged() {
  loadFavorites()
}

onMounted(() => {
  loadFavorites()
  window.addEventListener(PURCHASE_ORDER_FAVORITES_CHANGED_EVENT, onFavoritesChanged)
})

onBeforeUnmount(() => {
  window.removeEventListener(PURCHASE_ORDER_FAVORITES_CHANGED_EVENT, onFavoritesChanged)
})
</script>

<template>
  <div class="po-favorite-panel" v-loading="loading">
    <div class="po-favorite-panel__head">{{ title }}</div>
    <div v-if="!loading && items.length === 0" class="po-favorite-panel__empty">
      暂无收藏；可在采购订单详情页标题旁点击星标收藏
    </div>
    <table v-else class="po-favorite-panel__table">
      <thead>
        <tr>
          <th>订单号</th>
          <th>供应商</th>
          <th>状态</th>
        </tr>
      </thead>
      <tbody>
        <tr
          v-for="row in items"
          :key="row.id"
          class="po-favorite-panel__row"
          @click="goDetail(row)"
        >
          <td class="po-favorite-panel__code">{{ row.purchaseOrderCode || '—' }}</td>
          <td class="po-favorite-panel__name">{{ row.vendorName || '—' }}</td>
          <td>{{ getStatusText(Number(row.status)) }}</td>
        </tr>
      </tbody>
    </table>
  </div>
</template>

<style scoped lang="scss">
.po-favorite-panel {
  min-height: 80px;
}

.po-favorite-panel__head {
  font-weight: 600;
  color: #e8f4ff;
  margin-bottom: 10px;
  font-size: 13px;
}

.po-favorite-panel__empty {
  font-size: 12px;
  color: rgba(140, 180, 210, 0.75);
  line-height: 1.5;
}

.po-favorite-panel__table {
  width: 100%;
  border-collapse: collapse;
  font-size: 11px;
}

.po-favorite-panel__table th {
  text-align: left;
  padding: 6px 4px;
  color: rgba(140, 180, 210, 0.75);
  font-weight: 500;
  border-bottom: 1px solid rgba(0, 212, 255, 0.12);
}

.po-favorite-panel__table td {
  padding: 8px 4px;
  border-bottom: 1px solid rgba(255, 255, 255, 0.06);
  color: rgba(224, 244, 255, 0.88);
}

.po-favorite-panel__row {
  cursor: pointer;
  transition: background 0.12s;

  &:hover {
    background: rgba(0, 212, 255, 0.06);
  }
}

.po-favorite-panel__code {
  font-family: 'Space Mono', ui-monospace, monospace;
  color: rgba(0, 212, 255, 0.95);
}

.po-favorite-panel__name {
  max-width: 120px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
</style>
