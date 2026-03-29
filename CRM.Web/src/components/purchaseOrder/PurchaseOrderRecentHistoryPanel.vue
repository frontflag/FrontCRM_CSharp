<script setup lang="ts">
import { ref, onMounted, onBeforeUnmount } from 'vue'
import { useRouter } from 'vue-router'
import { formatDisplayDateTime } from '@/utils/displayDateTime'
import {
  readPurchaseOrderRecentEntries,
  clearPurchaseOrderRecentHistory,
  PURCHASE_ORDER_RECENT_HISTORY_CHANGED_EVENT,
  type PurchaseOrderRecentEntry
} from '@/utils/purchaseOrderRecentHistory'

const props = withDefaults(
  defineProps<{
    title?: string
    take?: number
  }>(),
  { title: '最近打开的采购订单', take: 20 }
)

const router = useRouter()
const rows = ref<PurchaseOrderRecentEntry[]>([])

function reload() {
  rows.value = readPurchaseOrderRecentEntries(props.take)
}

function formatAt(iso: string) {
  const s = formatDisplayDateTime(iso)
  return s === '--' ? '—' : s
}

function goDetail(row: PurchaseOrderRecentEntry) {
  router.push({ name: 'PurchaseOrderDetail', params: { id: row.id } })
}

function handleClear() {
  clearPurchaseOrderRecentHistory()
}

function onRecentChanged() {
  reload()
}

onMounted(() => {
  reload()
  window.addEventListener(PURCHASE_ORDER_RECENT_HISTORY_CHANGED_EVENT, onRecentChanged)
})

onBeforeUnmount(() => {
  window.removeEventListener(PURCHASE_ORDER_RECENT_HISTORY_CHANGED_EVENT, onRecentChanged)
})
</script>

<template>
  <div class="po-recent-panel">
    <div class="po-recent-panel__head-row">
      <div class="po-recent-panel__head">{{ title }}</div>
      <button
        v-if="rows.length > 0"
        type="button"
        class="po-recent-panel__clear"
        title="清空本地浏览记录"
        @click="handleClear"
      >
        清空
      </button>
    </div>
    <div v-if="rows.length === 0" class="po-recent-panel__empty">
      暂无记录；打开采购订单详情后会出现在此列表（仅保存在本机浏览器）
    </div>
    <table v-else class="po-recent-panel__table">
      <thead>
        <tr>
          <th>订单号</th>
          <th>供应商</th>
          <th>时间</th>
        </tr>
      </thead>
      <tbody>
        <tr
          v-for="row in rows"
          :key="row.id + row.at"
          class="po-recent-panel__row"
          @click="goDetail(row)"
        >
          <td class="po-recent-panel__code">{{ row.purchaseOrderCode || '—' }}</td>
          <td class="po-recent-panel__name">{{ row.vendorName || '—' }}</td>
          <td class="po-recent-panel__time">{{ formatAt(row.at) }}</td>
        </tr>
      </tbody>
    </table>
  </div>
</template>

<style scoped lang="scss">
.po-recent-panel {
  min-height: 80px;
}

.po-recent-panel__head-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
  margin-bottom: 10px;
}

.po-recent-panel__head {
  font-weight: 600;
  color: #e8f4ff;
  font-size: 13px;
}

.po-recent-panel__clear {
  flex-shrink: 0;
  padding: 4px 8px;
  font-size: 11px;
  color: rgba(180, 210, 230, 0.85);
  background: rgba(255, 255, 255, 0.04);
  border: 1px solid rgba(0, 212, 255, 0.12);
  border-radius: 4px;
  cursor: pointer;

  &:hover {
    background: rgba(0, 212, 255, 0.08);
  }
}

.po-recent-panel__empty {
  font-size: 12px;
  color: rgba(140, 180, 210, 0.75);
  line-height: 1.5;
}

.po-recent-panel__table {
  width: 100%;
  border-collapse: collapse;
  font-size: 11px;
}

.po-recent-panel__table th {
  text-align: left;
  padding: 6px 4px;
  color: rgba(140, 180, 210, 0.75);
  font-weight: 500;
  border-bottom: 1px solid rgba(0, 212, 255, 0.12);
}

.po-recent-panel__table td {
  padding: 8px 4px;
  border-bottom: 1px solid rgba(255, 255, 255, 0.06);
  color: rgba(224, 244, 255, 0.88);
}

.po-recent-panel__row {
  cursor: pointer;
  transition: background 0.12s;

  &:hover {
    background: rgba(0, 212, 255, 0.06);
  }
}

.po-recent-panel__code {
  font-family: 'Space Mono', ui-monospace, monospace;
  color: rgba(0, 212, 255, 0.95);
}

.po-recent-panel__name {
  max-width: 90px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.po-recent-panel__time {
  color: rgba(160, 190, 220, 0.75);
  white-space: nowrap;
}
</style>
