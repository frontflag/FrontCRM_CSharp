<script setup lang="ts">
import { ref, onMounted, onBeforeUnmount } from 'vue'
import { useRouter } from 'vue-router'
import { formatDisplayDateTime } from '@/utils/displayDateTime'
import {
  readSalesOrderRecentEntries,
  clearSalesOrderRecentHistory,
  SALES_ORDER_RECENT_HISTORY_CHANGED_EVENT,
  type SalesOrderRecentEntry
} from '@/utils/salesOrderRecentHistory'

const props = withDefaults(
  defineProps<{
    title?: string
    take?: number
  }>(),
  { title: '最近打开的销售订单', take: 20 }
)

const router = useRouter()
const rows = ref<SalesOrderRecentEntry[]>([])

function reload() {
  rows.value = readSalesOrderRecentEntries(props.take)
}

function formatAt(iso: string) {
  const s = formatDisplayDateTime(iso)
  return s === '--' ? '—' : s
}

function goDetail(row: SalesOrderRecentEntry) {
  router.push({ name: 'SalesOrderDetail', params: { id: row.id } })
}

function handleClear() {
  clearSalesOrderRecentHistory()
}

function onRecentChanged() {
  reload()
}

onMounted(() => {
  reload()
  window.addEventListener(SALES_ORDER_RECENT_HISTORY_CHANGED_EVENT, onRecentChanged)
})

onBeforeUnmount(() => {
  window.removeEventListener(SALES_ORDER_RECENT_HISTORY_CHANGED_EVENT, onRecentChanged)
})
</script>

<template>
  <div class="so-recent-panel">
    <div class="so-recent-panel__head-row">
      <div class="so-recent-panel__head">{{ title }}</div>
      <button
        v-if="rows.length > 0"
        type="button"
        class="so-recent-panel__clear"
        title="清空本地浏览记录"
        @click="handleClear"
      >
        清空
      </button>
    </div>
    <div v-if="rows.length === 0" class="so-recent-panel__empty">
      暂无记录；打开销售订单详情后会出现在此列表（仅保存在本机浏览器）
    </div>
    <table v-else class="so-recent-panel__table">
      <thead>
        <tr>
          <th>订单号</th>
          <th>客户</th>
          <th>时间</th>
        </tr>
      </thead>
      <tbody>
        <tr
          v-for="row in rows"
          :key="row.id + row.at"
          class="so-recent-panel__row"
          @click="goDetail(row)"
        >
          <td class="so-recent-panel__code">{{ row.sellOrderCode || '—' }}</td>
          <td class="so-recent-panel__name">{{ row.customerName || '—' }}</td>
          <td class="so-recent-panel__time">{{ formatAt(row.at) }}</td>
        </tr>
      </tbody>
    </table>
  </div>
</template>

<style scoped lang="scss">
.so-recent-panel {
  min-height: 80px;
}

.so-recent-panel__head-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
  margin-bottom: 10px;
}

.so-recent-panel__head {
  font-weight: 600;
  color: #e8f4ff;
  font-size: 13px;
}

.so-recent-panel__clear {
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

.so-recent-panel__empty {
  font-size: 12px;
  color: rgba(140, 180, 210, 0.75);
  line-height: 1.5;
}

.so-recent-panel__table {
  width: 100%;
  border-collapse: collapse;
  font-size: 11px;
}

.so-recent-panel__table th {
  text-align: left;
  padding: 6px 4px;
  color: rgba(140, 180, 210, 0.75);
  font-weight: 500;
  border-bottom: 1px solid rgba(0, 212, 255, 0.12);
}

.so-recent-panel__table td {
  padding: 8px 4px;
  border-bottom: 1px solid rgba(255, 255, 255, 0.06);
  color: rgba(224, 244, 255, 0.88);
}

.so-recent-panel__row {
  cursor: pointer;
  transition: background 0.12s;

  &:hover {
    background: rgba(0, 212, 255, 0.06);
  }
}

.so-recent-panel__code {
  font-family: 'Space Mono', ui-monospace, monospace;
  color: rgba(0, 212, 255, 0.95);
}

.so-recent-panel__name {
  max-width: 90px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.so-recent-panel__time {
  color: rgba(160, 190, 220, 0.75);
  white-space: nowrap;
}
</style>
