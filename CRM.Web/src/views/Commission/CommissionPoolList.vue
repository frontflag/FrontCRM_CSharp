<template>
  <div class="crm-biz-list-page">
    <div class="page-header">
      <div class="header-left">
        <div class="page-title-group">
          <div class="page-icon" aria-hidden="true">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
              <ellipse cx="12" cy="6" rx="8" ry="3" />
              <path d="M4 6v6c0 1.7 3.6 3 8 3s8-1.3 8-3V6" />
              <path d="M4 12v6c0 1.7 3.6 3 8 3s8-1.3 8-3v-6" />
            </svg>
          </div>
          <h1 class="page-title">{{ t('commissionPool.title') }}</h1>
        </div>
        <div class="count-badge">{{ t('commissionResult.count', { count: total }) }}</div>
      </div>
    </div>

    <div class="search-bar">
      <div class="search-left">
        <div class="search-input-wrap">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="search-icon">
            <circle cx="11" cy="11" r="8" />
            <line x1="21" y1="21" x2="16.65" y2="16.65" />
          </svg>
          <input
            v-model="keyword"
            class="search-input"
            :placeholder="t('commissionPool.keywordPlaceholder')"
            @keyup.enter="reload"
          />
        </div>
        <div class="search-input-wrap">
          <input
            v-model="purchasePn"
            class="search-input search-input--pn"
            :placeholder="t('commissionPool.materialPn')"
            @keyup.enter="reload"
          />
        </div>
        <el-select
          v-if="canFilterPoolUsers"
          v-model="purchaseUserId"
          class="status-select status-select--purchaser"
          clearable
          filterable
          :placeholder="t('commissionResult.purchaser')"
          :teleported="false"
          @change="reload"
        >
          <el-option v-for="u in purchaseUsers" :key="u.id" :label="accountLabel(u)" :value="u.id" />
        </el-select>
        <el-select
          v-if="canFilterPoolUsers"
          v-model="salesUserId"
          class="status-select status-select--sales"
          clearable
          filterable
          :placeholder="t('commissionResult.salesperson')"
          :teleported="false"
          @change="reload"
        >
          <el-option v-for="u in salesUsers" :key="u.id" :label="accountLabel(u)" :value="u.id" />
        </el-select>
        <el-select
          v-model="receiptStatus"
          class="status-select status-select--wide"
          clearable
          :placeholder="t('commissionResult.receiptProgress')"
          :teleported="false"
          @change="reload"
        >
          <el-option :label="t('commissionResult.receiptPending')" :value="0" />
          <el-option :label="t('commissionResult.receiptPartial')" :value="1" />
          <el-option :label="t('commissionResult.receiptComplete')" :value="2" />
        </el-select>
        <el-select
          v-model="salesStatus"
          class="status-select"
          clearable
          :placeholder="t('commissionPool.salesStatus')"
          :teleported="false"
          @change="reload"
        >
          <el-option :label="t('commissionPool.statusOpen')" :value="0" />
          <el-option :label="t('commissionPool.statusDone')" :value="1" />
        </el-select>
        <el-select
          v-model="purchaseStatus"
          class="status-select status-select--wide"
          clearable
          :placeholder="t('commissionPool.purchaseStatus')"
          :teleported="false"
          @change="reload"
        >
          <el-option :label="t('commissionPool.statusOpen')" :value="0" />
          <el-option :label="t('commissionPool.statusDone')" :value="1" />
          <el-option :label="t('commissionPool.statusNa')" :value="2" />
        </el-select>
        <el-date-picker
          v-model="stockOutRange"
          class="status-select status-select--wide"
          type="daterange"
          value-format="YYYY-MM-DD"
          :start-placeholder="t('commissionPool.stockOutFrom')"
          :end-placeholder="t('commissionPool.stockOutTo')"
          :range-separator="t('commissionResult.dateSep')"
          :teleported="false"
          @change="reload"
        />
        <button type="button" class="btn-primary btn-sm" @click="reload">{{ t('commissionResult.search') }}</button>
        <button type="button" class="btn-ghost btn-sm" @click="resetSearch">{{ t('commissionResult.reset') }}</button>
      </div>
    </div>

    <div class="table-wrapper" v-loading="loading">
      <CrmDataTable
        v-show="loading || rows.length > 0"
        ref="dataTableRef"
        :column-layout-key="columnKey"
        :columns="tableColumns"
        :show-column-settings="false"
        :density-toggle-anchor-el="rowDensityToggleAnchorEl"
        :data="rows"
        row-key="id"
        highlight-current-row
      >
        <template #col-stockOutItemCode="{ row }">
          <router-link
            v-if="canOpenStockOut && row.stockOutId && formatCommissionDocCode(row.stockOutItemCode, row.stockOutCode)"
            class="link-text"
            :to="`/inventory/stock-out/${row.stockOutId}`"
            target="_blank"
            rel="noopener noreferrer"
            @click.stop
          >
            {{ formatCommissionDocCode(row.stockOutItemCode, row.stockOutCode) }}
          </router-link>
          <span v-else>{{ formatCommissionDocCode(row.stockOutItemCode, row.stockOutCode) || '—' }}</span>
        </template>
        <template #col-sellOrderItemCode="{ row }">
          <router-link
            v-if="canOpenSales && row.sellOrderId && formatCommissionDocCode(row.sellOrderItemCode, row.sellOrderCode)"
            class="link-text"
            :to="`/sales-orders/${row.sellOrderId}`"
            target="_blank"
            rel="noopener noreferrer"
            @click.stop
          >
            {{ formatCommissionDocCode(row.sellOrderItemCode, row.sellOrderCode) }}
          </router-link>
          <span v-else>{{ formatCommissionDocCode(row.sellOrderItemCode, row.sellOrderCode) || '—' }}</span>
        </template>
        <template #col-purchaseOrderItemCode="{ row }">
          <router-link
            v-if="canOpenPurchase && row.purchaseOrderId && formatCommissionDocCode(row.purchaseOrderItemCode, row.purchaseOrderCode)"
            class="link-text"
            :to="`/purchase-orders/${row.purchaseOrderId}`"
            target="_blank"
            rel="noopener noreferrer"
            @click.stop
          >
            {{ formatCommissionDocCode(row.purchaseOrderItemCode, row.purchaseOrderCode) }}
          </router-link>
          <span v-else>{{ formatCommissionDocCode(row.purchaseOrderItemCode, row.purchaseOrderCode) || '—' }}</span>
        </template>
        <template #col-stockOutDate="{ row }">
          {{ formatCommissionListDate(row.stockOutDate) }}
        </template>
        <template #col-receiptDate="{ row }">
          {{ formatCommissionListDate(row.receiptDate) }}
        </template>
        <template #col-receiptProgressStatus="{ row }">
          <el-tag effect="dark" size="small" :type="receiptTagType(row.receiptProgressStatus)">
            {{ receiptText(row.receiptProgressStatus) }}
          </el-tag>
        </template>
        <template #col-purchasePn="{ row }">
          <CrmListCopyableTextCell :text="row.purchasePn?.trim() || ''" empty-text="—" />
        </template>
        <template #col-purchaseBrand="{ row }">
          <CrmListCopyableTextCell :text="row.purchaseBrand?.trim() || ''" empty-text="—" />
        </template>
        <template #col-purchasePrice="{ row }">
          <ReceivableStatementMoneyCell
            unit-price
            :amount="row.purchasePrice"
            :currency="row.purchaseCurrency ?? CurrencyCode.RMB"
          />
        </template>
        <template #col-purchasePriceUsd="{ row }">
          <ReceivableStatementMoneyCell unit-price :amount="row.purchasePriceUsd" :currency="usdCurrency" />
        </template>
        <template #col-salesPrice="{ row }">
          <ReceivableStatementMoneyCell
            unit-price
            :amount="row.salesPrice"
            :currency="row.salesCurrency ?? CurrencyCode.USD"
          />
        </template>
        <template #col-salesPriceUsd="{ row }">
          <ReceivableStatementMoneyCell unit-price :amount="row.salesPriceUsd" :currency="usdCurrency" />
        </template>
        <template #col-qtyStockOut="{ row }">
          {{ qtyText(row.qtyStockOut) }}
        </template>
        <template #col-gpUsd="{ row }">
          <ReceivableStatementMoneyCell :amount="row.gpUsd" :currency="usdCurrency" />
        </template>
      </CrmDataTable>
      <div v-show="!loading && rows.length === 0" class="empty-state">
        <p>{{ t('commissionPool.empty') }}</p>
      </div>
    </div>

    <div class="pagination-wrapper">
      <div class="list-footer-left">
        <el-tooltip :content="t('systemUser.colSetting')" placement="top" :hide-after="0">
          <el-button
            class="list-settings-btn"
            link
            type="primary"
            :aria-label="t('systemUser.colSetting')"
            @click="dataTableRef?.openColumnSettings?.()"
          >
            <el-icon><Setting /></el-icon>
          </el-button>
        </el-tooltip>
        <span ref="rowDensityToggleAnchorEl" class="list-footer-density-anchor" aria-hidden="true" />
        <div class="list-footer-spacer" aria-hidden="true" />
      </div>
      <el-pagination
        class="list-main-pagination"
        v-model:current-page="page"
        v-model:page-size="pageSize"
        :total="total"
        :page-sizes="[10, 20, 50, 100]"
        layout="total, sizes, prev, pager, next, jumper"
        @current-change="load"
        @size-change="onPageSizeChange"
      />
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { ElMessage } from 'element-plus'
import { Setting } from '@element-plus/icons-vue'
import { useI18n } from 'vue-i18n'
import CrmDataTable from '@/components/CrmDataTable.vue'
import CrmListCopyableTextCell from '@/components/CrmListCopyableTextCell.vue'
import ReceivableStatementMoneyCell from '@/views/Finance/ReceivableStatementMoneyCell.vue'
import { CurrencyCode } from '@/constants/currency'
import { useAuthStore } from '@/stores'
import {
  commissionApi,
  formatCommissionDocCode,
  formatCommissionListDate,
  type CommissionPoolRow
} from '@/api/commission'
import { estimateListColumnHeaderMinWidth } from '@/utils/listColumnHeaderWidth'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'
import { authApi, type PurchaseUserSelectOption, type SalesUserSelectOption } from '@/api/auth'
import { canSeeAllCommissionPoolRows } from '@/utils/commissionPoolAccess'

const { t } = useI18n()
const authStore = useAuthStore()
const usdCurrency = CurrencyCode.USD
const columnKey = 'commission-pool-v6'
const canOpenStockOut = computed(() => authStore.hasPermission('inventory.read'))
const canOpenSales = computed(() => authStore.hasPermission('sales-order.read'))
const canOpenPurchase = computed(() => authStore.hasPermission('purchase-order.read'))
const canFilterPoolUsers = computed(() => canSeeAllCommissionPoolRows(authStore.user))

const loading = ref(false)
const keyword = ref('')
const purchasePn = ref('')
const salesUserId = ref<string | undefined>(undefined)
const purchaseUserId = ref<string | undefined>(undefined)
const salesUsers = ref<SalesUserSelectOption[]>([])
const purchaseUsers = ref<PurchaseUserSelectOption[]>([])
const salesStatus = ref<number | undefined>(undefined)
const purchaseStatus = ref<number | undefined>(undefined)
const receiptStatus = ref<number | undefined>(undefined)
const stockOutRange = ref<[string, string] | undefined>(undefined)
const page = ref(1)
const pageSize = ref(20)
const total = ref(0)
const rows = ref<CommissionPoolRow[]>([])
const dataTableRef = ref<{ openColumnSettings?: () => void } | null>(null)
const rowDensityToggleAnchorEl = ref<HTMLElement | null>(null)

function headerMin(label: string) {
  return estimateListColumnHeaderMinWidth(label)
}

function salesStatusText(status: number) {
  if (status === 1) return t('commissionPool.statusDone')
  return t('commissionPool.statusOpen')
}

function purchaseStatusText(status: number) {
  if (status === 1) return t('commissionPool.statusDone')
  if (status === 2) return t('commissionPool.statusNa')
  return t('commissionPool.statusOpen')
}

function receiptText(status: number) {
  if (status === 1) return t('commissionResult.receiptPartial')
  if (status === 2) return t('commissionResult.receiptComplete')
  return t('commissionResult.receiptPending')
}

function receiptTagType(status: number): 'success' | 'warning' | 'info' {
  if (status === 1) return 'warning'
  if (status === 2) return 'success'
  return 'info'
}

function dashName(value: string | null | undefined) {
  const name = value?.trim()
  return name ? name : '—'
}

function accountLabel(u: { userName?: string; label?: string; id: string }) {
  return (u.userName || u.label || u.id).trim()
}

function qtyText(value?: number | null) {
  if (value == null || Number.isNaN(Number(value))) return '—'
  return Number(value).toLocaleString('zh-CN')
}

const tableColumns = computed<CrmTableColumnDef[]>(() => {
  const outDate = t('commissionResult.stockOutDateFull')
  const outCode = t('commissionResult.stockOutItem')
  const sell = t('commissionResult.sellOrderItem')
  const purchase = t('commissionResult.purchaseOrderItem')
  const pn = t('commissionPool.materialPn')
  const brand = t('commissionPool.brand')
  const purchasePrice = t('commissionPool.purchasePrice')
  const purchasePriceUsd = t('commissionPool.purchasePriceUsd')
  const salesPrice = t('commissionPool.salesPrice')
  const salesPriceUsd = t('commissionPool.salesPriceUsd')
  const qty = t('commissionPool.qty')
  const gp = t('commissionPool.gp')
  const sales = t('commissionResult.salesperson')
  const purchaser = t('commissionResult.purchaser')
  const salesSt = t('commissionPool.salesStatus')
  const purchaseSt = t('commissionPool.purchaseStatus')
  const receipt = t('commissionResult.receiptProgress')
  const receiptDate = t('commissionResult.receiptDateFull')
  return [
    { key: 'stockOutDate', label: outDate, width: Math.max(112, headerMin(outDate)), align: 'center' },
    { key: 'stockOutItemCode', label: outCode, minWidth: Math.max(140, headerMin(outCode)), showOverflowTooltip: true },
    { key: 'sellOrderItemCode', label: sell, minWidth: Math.max(140, headerMin(sell)), showOverflowTooltip: true },
    { key: 'purchaseOrderItemCode', label: purchase, minWidth: Math.max(140, headerMin(purchase)), showOverflowTooltip: true },
    { key: 'purchasePn', label: pn, minWidth: Math.max(140, headerMin(pn)), showOverflowTooltip: true },
    { key: 'purchaseBrand', label: brand, minWidth: Math.max(110, headerMin(brand)), showOverflowTooltip: true },
    { key: 'purchasePrice', label: purchasePrice, minWidth: Math.max(120, headerMin(purchasePrice)), align: 'right' },
    { key: 'salesPrice', label: salesPrice, minWidth: Math.max(120, headerMin(salesPrice)), align: 'right' },
    {
      key: 'purchasePriceUsd',
      label: purchasePriceUsd,
      minWidth: Math.max(148, headerMin(purchasePriceUsd)),
      align: 'right'
    },
    {
      key: 'salesPriceUsd',
      label: salesPriceUsd,
      minWidth: Math.max(148, headerMin(salesPriceUsd)),
      align: 'right'
    },
    { key: 'qtyStockOut', label: qty, width: Math.max(100, headerMin(qty)), align: 'right' },
    { key: 'gpUsd', label: gp, minWidth: Math.max(100, headerMin(gp)), align: 'right' },
    {
      key: 'salesUserName',
      prop: 'salesUserName',
      label: sales,
      minWidth: Math.max(110, headerMin(sales)),
      showOverflowTooltip: true,
      formatter: (row: unknown) => dashName((row as CommissionPoolRow).salesUserName)
    },
    {
      key: 'purchaseUserName',
      prop: 'purchaseUserName',
      label: purchaser,
      minWidth: Math.max(110, headerMin(purchaser)),
      showOverflowTooltip: true,
      formatter: (row: unknown) => dashName((row as CommissionPoolRow).purchaseUserName)
    },
    {
      key: 'receiptProgressStatus',
      label: receipt,
      width: Math.max(112, headerMin(receipt)),
      align: 'center'
    },
    { key: 'receiptDate', label: receiptDate, width: Math.max(128, headerMin(receiptDate)), align: 'center' },
    {
      key: 'salesCommissionStatus',
      label: salesSt,
      width: Math.max(112, headerMin(salesSt)),
      align: 'center',
      formatter: (row: unknown) => salesStatusText((row as CommissionPoolRow).salesCommissionStatus)
    },
    {
      key: 'purchaseCommissionStatus',
      label: purchaseSt,
      width: Math.max(128, headerMin(purchaseSt)),
      align: 'center',
      formatter: (row: unknown) => purchaseStatusText((row as CommissionPoolRow).purchaseCommissionStatus)
    }
  ]
})

function queryParams() {
  const sales = salesStatus.value
  const purchase = purchaseStatus.value
  const receipt = receiptStatus.value
  return {
    keyword: keyword.value.trim() || undefined,
    purchasePn: purchasePn.value.trim() || undefined,
    salesUserId: canFilterPoolUsers.value ? salesUserId.value?.trim() || undefined : undefined,
    purchaseUserId: canFilterPoolUsers.value ? purchaseUserId.value?.trim() || undefined : undefined,
    salesStatus: sales === 0 || sales === 1 ? sales : undefined,
    purchaseStatus: purchase === 0 || purchase === 1 || purchase === 2 ? purchase : undefined,
    receiptStatus: receipt === 0 || receipt === 1 || receipt === 2 ? receipt : undefined,
    stockOutDateFrom: stockOutRange.value?.[0],
    stockOutDateTo: stockOutRange.value?.[1],
    page: page.value,
    pageSize: pageSize.value
  }
}

async function load() {
  loading.value = true
  try {
    const data = await commissionApi.pool(queryParams())
    rows.value = data.items || []
    total.value = data.totalCount
  } catch (e: unknown) {
    ElMessage.error(e instanceof Error ? e.message : t('commissionPool.loadFailed'))
  } finally {
    loading.value = false
  }
}

function reload() {
  page.value = 1
  void load()
}

function resetSearch() {
  keyword.value = ''
  purchasePn.value = ''
  salesUserId.value = undefined
  purchaseUserId.value = undefined
  salesStatus.value = undefined
  purchaseStatus.value = undefined
  receiptStatus.value = undefined
  stockOutRange.value = undefined
  reload()
}

function onPageSizeChange() {
  page.value = 1
  void load()
}

onMounted(() => {
  if (canFilterPoolUsers.value) void loadUserOptions()
  void load()
})

async function loadUserOptions() {
  try {
    const [sales, purchase] = await Promise.all([
      authApi.getSalesUsersForSelect(),
      authApi.getPurchaseUsersForSelect()
    ])
    salesUsers.value = sales
    purchaseUsers.value = purchase
  } catch {
    salesUsers.value = []
    purchaseUsers.value = []
  }
}
</script>

<style lang="scss">
@import '@/assets/styles/crm-biz-list-page.scss';
</style>
