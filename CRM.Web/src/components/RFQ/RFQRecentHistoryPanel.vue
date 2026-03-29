<script setup lang="ts">
import { ref, onMounted, onBeforeUnmount } from 'vue'
import { useRouter } from 'vue-router'
import { formatDisplayDateTime } from '@/utils/displayDateTime'
import {
  readRfqRecentEntries,
  clearRfqRecentHistory,
  RFQ_RECENT_HISTORY_CHANGED_EVENT,
  type RfqRecentEntry
} from '@/utils/rfqRecentHistory'

const props = withDefaults(
  defineProps<{
    title?: string
    take?: number
  }>(),
  { title: '最近打开的需求', take: 20 }
)

const router = useRouter()
const rows = ref<RfqRecentEntry[]>([])

function reload() {
  rows.value = readRfqRecentEntries(props.take)
}

function formatAt(iso: string) {
  const s = formatDisplayDateTime(iso)
  return s === '--' ? '—' : s
}

function goDetail(row: RfqRecentEntry) {
  router.push({ name: 'RFQDetail', params: { id: row.id } })
}

function handleClear() {
  clearRfqRecentHistory()
}

function onRecentChanged() {
  reload()
}

onMounted(() => {
  reload()
  window.addEventListener(RFQ_RECENT_HISTORY_CHANGED_EVENT, onRecentChanged)
})

onBeforeUnmount(() => {
  window.removeEventListener(RFQ_RECENT_HISTORY_CHANGED_EVENT, onRecentChanged)
})
</script>

<template>
  <div class="rfq-recent-panel">
    <div class="rfq-recent-panel__head-row">
      <div class="rfq-recent-panel__head">{{ title }}</div>
      <button
        v-if="rows.length > 0"
        type="button"
        class="rfq-recent-panel__clear"
        title="清空本地浏览记录"
        @click="handleClear"
      >
        清空
      </button>
    </div>
    <div v-if="rows.length === 0" class="rfq-recent-panel__empty">
      暂无记录；打开需求详情后会出现在此列表（仅保存在本机浏览器）
    </div>
    <table v-else class="rfq-recent-panel__table">
      <thead>
        <tr>
          <th>需求编号</th>
          <th>客户</th>
          <th>时间</th>
        </tr>
      </thead>
      <tbody>
        <tr
          v-for="row in rows"
          :key="row.id + row.at"
          class="rfq-recent-panel__row"
          @click="goDetail(row)"
        >
          <td class="rfq-recent-panel__code">{{ row.rfqCode || '—' }}</td>
          <td class="rfq-recent-panel__name">{{ row.customerName || '—' }}</td>
          <td class="rfq-recent-panel__time">{{ formatAt(row.at) }}</td>
        </tr>
      </tbody>
    </table>
  </div>
</template>

<style scoped lang="scss">
@import url('https://fonts.googleapis.com/css2?family=Space+Mono&display=swap');

.rfq-recent-panel {
  min-height: 80px;
}

.rfq-recent-panel__head-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
  margin-bottom: 10px;
}

.rfq-recent-panel__head {
  font-weight: 600;
  color: #e8f4ff;
  font-size: 13px;
}

.rfq-recent-panel__clear {
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

.rfq-recent-panel__empty {
  font-size: 12px;
  color: rgba(140, 180, 210, 0.75);
  line-height: 1.5;
}

.rfq-recent-panel__table {
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

.rfq-recent-panel__row {
  cursor: pointer;
  transition: background 0.12s;

  &:hover {
    background: rgba(0, 212, 255, 0.08);
  }
}

.rfq-recent-panel__name {
  word-break: break-all;
  max-width: 88px;
}

.rfq-recent-panel__code {
  font-family: 'Space Mono', ui-monospace, monospace;
  font-size: 11px;
  color: rgba(180, 210, 230, 0.85);
}

.rfq-recent-panel__time {
  font-size: 11px;
  color: rgba(160, 190, 215, 0.8);
  white-space: nowrap;
}
</style>
