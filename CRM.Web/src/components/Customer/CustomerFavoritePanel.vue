<script setup lang="ts">
import { ref, onMounted, onBeforeUnmount } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { favoriteApi } from '@/api/favorite'
import { customerApi } from '@/api/customer'
import type { Customer } from '@/types/customer'
import { useAuthStore } from '@/stores/auth'
import { CUSTOMER_FAVORITES_CHANGED_EVENT } from '@/constants/customerFavorites'

withDefaults(
  defineProps<{
    /** 区块标题 */
    title?: string
  }>(),
  { title: '' }
)

const router = useRouter()
const { t } = useI18n()
const authStore = useAuthStore()
const canViewCustomerInfo = authStore.hasPermission('customer.info.read')

const loading = ref(false)
const items = ref<Customer[]>([])

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

/** 与 CustomerList 列表「级别」列展示一致 */
const getLevelLabel = (level: string | undefined) =>
  ({
    VIP: 'VIP',
    VPO: 'VPO',
    BPO: 'BPO',
    B: t('customerList.level.B'),
    C: t('customerList.level.C'),
    D: t('customerList.level.D'),
    Important: t('customerList.level.Important'),
    Normal: t('customerList.level.Normal'),
    Lead: t('customerList.level.Lead')
  }[level || ''] ||
    level ||
    '--')

/** 与 CustomerList 列表「类型」列展示一致 */
const getTypeLabel = (type: number | undefined) =>
  ({ 1: 'OEM', 2: 'ODM', 3: t('customerList.type.endUser'), 4: 'IDH', 5: t('customerList.type.trader'), 6: t('customerList.type.agency') }[type ?? 0] || t('rfqDetail.unknown'))

async function loadFavorites() {
  loading.value = true
  try {
    const ids = await favoriteApi.getFavoriteEntityIds('CUSTOMER')
    if (ids.length === 0) {
      items.value = []
      return
    }
    const settled = await Promise.allSettled(ids.map((id) => customerApi.getCustomerById(id)))
    const list: Customer[] = []
    for (const r of settled) {
      if (r.status !== 'fulfilled') continue
      list.push(normalizeCustomer(r.value as unknown as Record<string, unknown>))
    }
    items.value = list
  } catch {
    items.value = []
  } finally {
    loading.value = false
  }
}

function displayName(row: Customer) {
  if (!canViewCustomerInfo) return '—'
  return row.customerName || row.customerShortName || '—'
}

function goDetail(row: Customer) {
  router.push(`/customers/${row.id}`)
}

function onFavoritesChanged() {
  loadFavorites()
}

onMounted(() => {
  loadFavorites()
  window.addEventListener(CUSTOMER_FAVORITES_CHANGED_EVENT, onFavoritesChanged)
})

onBeforeUnmount(() => {
  window.removeEventListener(CUSTOMER_FAVORITES_CHANGED_EVENT, onFavoritesChanged)
})
</script>

<template>
  <div class="customer-favorite-panel" v-loading="loading">
    <div class="customer-favorite-panel__head">{{ title || t('leftPanel.customerFavoritesTitle') }}</div>
    <div v-if="!loading && items.length === 0" class="customer-favorite-panel__empty">
      {{ t('leftPanel.customerFavoritesEmpty') }}
    </div>
    <table v-else class="customer-favorite-panel__table">
      <thead>
        <tr>
          <th>{{ t('customerList.columns.customerName') }}</th>
          <th>{{ t('customerList.columns.customerCode') }}</th>
          <th>{{ t('customerList.columns.type') }}</th>
          <th>{{ t('customerList.columns.level') }}</th>
        </tr>
      </thead>
      <tbody>
        <tr
          v-for="row in items"
          :key="row.id"
          class="customer-favorite-panel__row"
          @click="goDetail(row)"
        >
          <td class="customer-favorite-panel__name">{{ displayName(row) }}</td>
          <td class="customer-favorite-panel__code">{{ row.customerCode || '—' }}</td>
          <td>{{ getTypeLabel(row.customerType) }}</td>
          <td>{{ getLevelLabel(row.customerLevel) }}</td>
        </tr>
      </tbody>
    </table>
  </div>
</template>

<style scoped lang="scss">
@import url('https://fonts.googleapis.com/css2?family=Space+Mono&display=swap');

.customer-favorite-panel {
  min-height: 80px;
}

.customer-favorite-panel__head {
  font-weight: 600;
  color: #e8f4ff;
  margin-bottom: 10px;
  font-size: 13px;
}

.customer-favorite-panel__empty {
  font-size: 12px;
  color: rgba(140, 180, 210, 0.75);
  line-height: 1.5;
}

.customer-favorite-panel__table {
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

.customer-favorite-panel__row {
  cursor: pointer;
  transition: background 0.12s;

  &:hover {
    background: rgba(0, 212, 255, 0.08);
  }
}

.customer-favorite-panel__name {
  word-break: break-all;
  max-width: 96px;
}

.customer-favorite-panel__code {
  font-family: 'Space Mono', ui-monospace, monospace;
  font-size: 11px;
  color: rgba(180, 210, 230, 0.85);
}
</style>
