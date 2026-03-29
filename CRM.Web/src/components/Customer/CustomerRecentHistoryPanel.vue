<script setup lang="ts">
import { ref, watch, onMounted, onBeforeUnmount } from 'vue'
import { useRouter } from 'vue-router'
import { customerApi } from '@/api/customer'
import type { Customer } from '@/types/customer'
import type { LogRecentItem } from '@/api/logRecent'
import { useAuthStore } from '@/stores/auth'
import { useRecentHistoryList } from '@/composables/useRecentHistoryList'
import { CUSTOMER_RECENT_HISTORY_CHANGED_EVENT } from '@/constants/customerRecentHistory'

/** 与后端 CRM.Core.Constants.BusinessLogTypes.Customer 一致 */
const BIZ_CUSTOMER = 'Customer'

const props = withDefaults(
  defineProps<{
    title?: string
    take?: number
  }>(),
  { title: '最近打开的客户', take: 20 }
)

const router = useRouter()
const authStore = useAuthStore()
const canViewCustomerInfo = authStore.hasPermission('customer.info.read')

const { loading, items, reload } = useRecentHistoryList(() => BIZ_CUSTOMER, () => props.take)

const rowLoading = ref(false)
const rows = ref<Customer[]>([])

function normalizeCustomer(raw: Record<string, unknown>): Customer {
  const levelNum = raw.level as number | undefined
  const levelFromNum =
    levelNum && levelNum >= 1 && levelNum <= 6
      ? (['', 'D', 'C', 'B', 'BPO', 'VIP', 'VPO'] as const)[levelNum]
      : undefined
  return {
    ...(raw as unknown as Customer),
    id: String(raw.id),
    customerCode: String(raw.customerCode ?? ''),
    customerName: (raw.customerName as string) || (raw.officialName as string),
    customerShortName: (raw.customerShortName as string) || (raw.nickName as string),
    customerLevel: (raw.customerLevel as string) || levelFromNum,
    customerType: (raw.customerType as number) ?? (raw.type as number) ?? 0
  }
}

const getLevelLabel = (level: string | undefined) =>
  ({
    VIP: 'VIP',
    VPO: 'VPO',
    BPO: 'BPO',
    B: 'B级',
    C: 'C级',
    D: 'D级',
    Important: '重要',
    Normal: '普通',
    Lead: '潜在'
  }[level || ''] ||
    level ||
    '--')

const getTypeLabel = (type: number | undefined) =>
  ({ 1: 'OEM', 2: 'ODM', 3: '终端用户', 4: 'IDH', 5: '贸易商', 6: '代理商' }[type ?? 0] || '未知')

async function resolveCustomersFromLog(logItems: LogRecentItem[]) {
  if (logItems.length === 0) {
    rows.value = []
    return
  }
  rowLoading.value = true
  try {
    const settled = await Promise.allSettled(logItems.map((it) => customerApi.getCustomerById(it.recordId)))
    const list: Customer[] = []
    for (let i = 0; i < settled.length; i++) {
      const r = settled[i]
      if (r.status !== 'fulfilled') continue
      list.push(normalizeCustomer(r.value as unknown as Record<string, unknown>))
    }
    rows.value = list
  } catch {
    rows.value = []
  } finally {
    rowLoading.value = false
  }
}

watch(items, (v) => void resolveCustomersFromLog(v), { immediate: true, deep: true })

function displayName(row: Customer) {
  if (!canViewCustomerInfo) return '—'
  return row.customerName || row.customerShortName || '—'
}

function goDetail(row: Customer) {
  router.push(`/customers/${row.id}`)
}

function onRecentChanged() {
  void reload()
}

onMounted(() => {
  window.addEventListener(CUSTOMER_RECENT_HISTORY_CHANGED_EVENT, onRecentChanged)
})

onBeforeUnmount(() => {
  window.removeEventListener(CUSTOMER_RECENT_HISTORY_CHANGED_EVENT, onRecentChanged)
})
</script>

<template>
  <div class="customer-recent-panel" v-loading="loading || rowLoading">
    <div class="customer-recent-panel__head">{{ title }}</div>
    <div v-if="!loading && !rowLoading && items.length === 0" class="customer-recent-panel__empty">
      暂无最近打开的详情或编辑记录
    </div>
    <table v-else class="customer-recent-panel__table">
      <thead>
        <tr>
          <th>客户名称</th>
          <th>客户编号</th>
          <th>类型</th>
          <th>级别</th>
        </tr>
      </thead>
      <tbody>
        <tr
          v-for="row in rows"
          :key="row.id"
          class="customer-recent-panel__row"
          @click="goDetail(row)"
        >
          <td class="customer-recent-panel__name">{{ displayName(row) }}</td>
          <td class="customer-recent-panel__code">{{ row.customerCode || '—' }}</td>
          <td>{{ getTypeLabel(row.customerType) }}</td>
          <td>{{ getLevelLabel(row.customerLevel) }}</td>
        </tr>
      </tbody>
    </table>
  </div>
</template>

<style scoped lang="scss">
@import url('https://fonts.googleapis.com/css2?family=Space+Mono&display=swap');

.customer-recent-panel {
  min-height: 80px;
}

.customer-recent-panel__head {
  font-weight: 600;
  color: #e8f4ff;
  margin-bottom: 10px;
  font-size: 13px;
}

.customer-recent-panel__empty {
  font-size: 12px;
  color: rgba(140, 180, 210, 0.75);
  line-height: 1.5;
}

.customer-recent-panel__table {
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

.customer-recent-panel__row {
  cursor: pointer;
  transition: background 0.12s;

  &:hover {
    background: rgba(0, 212, 255, 0.08);
  }
}

.customer-recent-panel__name {
  word-break: break-all;
  max-width: 96px;
}

.customer-recent-panel__code {
  font-family: 'Space Mono', ui-monospace, monospace;
  font-size: 11px;
  color: rgba(180, 210, 230, 0.85);
}
</style>
