<template>
  <!-- 业务列表页：结构对齐《业务列表规范》《列表搜索栏规范》；表格见 CrmDataTable + 全局 crm-unified-list.scss -->
  <div class="finance-page customs-declaration-item-list-page">
    <div class="page-header-row">
      <h1 class="finance-list-page-title">{{ t('customsPages.items.title') }}</h1>
      <div class="count-badge">{{ t('customsPages.items.count', { count: listTotal }) }}</div>
    </div>

    <div class="search-bar">
      <div class="search-left">
        <div class="search-input-wrap">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="search-icon">
            <circle cx="11" cy="11" r="8" />
            <line x1="21" y1="21" x2="16.65" y2="16.65" />
          </svg>
          <input
            v-model="filters.declarationCode"
            class="search-input"
            :placeholder="t('customsPages.items.filterDecCode')"
            @keyup.enter="handleSearch"
          />
        </div>
        <input
          v-model="filters.purchasePn"
          class="search-input search-input--filter"
          :placeholder="t('customsPages.items.filterPn')"
          @keyup.enter="handleSearch"
        />
        <input
          v-model="filters.customer"
          class="search-input search-input--filter"
          :placeholder="t('customsPages.items.filterCustomer')"
          @keyup.enter="handleSearch"
        />
        <input
          v-model="filters.salesUserId"
          class="search-input search-input--filter"
          :placeholder="t('customsPages.items.filterSales')"
          @keyup.enter="handleSearch"
        />
        <input
          v-model="filters.sellOrderItemCode"
          class="search-input search-input--filter"
          :placeholder="t('customsPages.items.filterSoLine')"
          @keyup.enter="handleSearch"
        />
        <input
          v-model="filters.stockOutRequestId"
          class="search-input search-input--filter"
          :placeholder="t('customsPages.items.filterSor')"
          @keyup.enter="handleSearch"
        />
        <input
          v-if="!maskPurchaseSensitiveFields"
          v-model="filters.purchaseOrderItemCode"
          class="search-input search-input--filter"
          :placeholder="t('customsPages.items.filterPoLine')"
          @keyup.enter="handleSearch"
        />
        <button type="button" class="btn-primary btn-sm" :disabled="loading" @click="handleSearch">
          {{ t('customsPages.items.search') }}
        </button>
        <button type="button" class="btn-ghost btn-sm" :disabled="loading" @click="resetFilters">
          {{ t('customsPages.items.reset') }}
        </button>
      </div>
    </div>

    <CrmDataTable
      ref="dataTableRef"
      column-layout-key="customs-declaration-item-list-main-v4"
      :columns="tableColumns"
      :show-column-settings="false"
      :density-toggle-anchor-el="rowDensityToggleAnchorEl"
      :data="pagedRows"
      v-loading="loading"
      class="data-table"
      @row-dblclick="onRowDblClick"
    >
      <template #col-declareDate="{ row }">
        <span class="text-secondary">{{ row.declareDate ? formatDisplayDate(row.declareDate) : '—' }}</span>
      </template>
      <template #col-declarationCode="{ row }">
        <router-link
          v-if="row.declarationId"
          class="link-text"
          :to="{ name: 'CustomsDeclarationDetail', params: { id: row.declarationId } }"
          @click.stop
        >
          {{ row.declarationCode || '—' }}
        </router-link>
        <span v-else>{{ row.declarationCode || '—' }}</span>
      </template>
      <template #col-customerName="{ row }">
        <span>{{ row.customerName || '—' }}</span>
      </template>
      <template #col-salesUserName="{ row }">
        <span>{{ row.salesUserName || '—' }}</span>
      </template>
      <template #col-purchasePn="{ row }">
        <CrmListCopyableTextCell :text="row.purchasePn" />
      </template>
      <template #col-purchaseBrand="{ row }">
        <CrmListCopyableTextCell :text="row.purchaseBrand" />
      </template>
      <template #col-declareQty="{ row }">
        <span class="cdi-list-qty">{{ formatQtyCell(row.declareQty) }}</span>
      </template>
      <template #col-purchaseOrderItemCode="{ row }">
        <router-link
          v-if="canOpenPurchaseOrder(row)"
          class="link-text"
          :to="{ name: 'PurchaseOrderDetail', params: { id: row.purchaseOrderId } }"
          @click.stop
        >
          <CrmListCopyableTextCell :text="row.purchaseOrderItemCode" />
        </router-link>
        <CrmListCopyableTextCell v-else :text="row.purchaseOrderItemCode" />
      </template>
      <template #col-originalPurchasePrice="{ row }">
        <span v-if="row.originalPurchasePrice == null" class="cdi-list-dash">—</span>
        <div v-else class="cdi-list-amount-cell dock-tier-price-line">
          <template v-for="amt in [splitUnitPriceDockParts(row.originalPurchasePrice)]" :key="'p0-' + row.id">
            <span class="cdi-list-amt">
              <span class="cdi-list-amt-int">{{ amt.intPart }}</span><span class="cdi-list-amt-frac">{{ amt.fracPart }}</span>
            </span>
          </template>
          <span class="dock-tier-ccy-gap">&nbsp;</span>
          <span class="dock-tier-ccy" :class="listAmountCurrencyDockClass(row.purchaseCurrency)">
            {{ listAmountCurrencyIso(row.purchaseCurrency) }}
          </span>
        </div>
      </template>
      <template #col-originalPurchaseAmount="{ row }">
        <span v-if="row.originalPurchaseAmount == null" class="cdi-list-dash">—</span>
        <div v-else class="cdi-list-amount-cell dock-tier-price-line">
          <template v-for="amt in [splitListMoneyParts(Number(row.originalPurchaseAmount))]" :key="'p0a-' + row.id">
            <span class="cdi-list-amt">
              <span class="cdi-list-amt-int">{{ amt.intPart }}</span><span class="cdi-list-amt-frac">{{ amt.fracPart }}</span>
            </span>
          </template>
          <span class="dock-tier-ccy-gap">&nbsp;</span>
          <span class="dock-tier-ccy" :class="listAmountCurrencyDockClass(row.purchaseCurrency)">
            {{ listAmountCurrencyIso(row.purchaseCurrency) }}
          </span>
        </div>
      </template>
      <template #col-declareUnitPrice="{ row }">
        <span class="cdi-fee-amt" :class="{ 'cdi-fee-amt--zero': isZeroAmount(row.declareUnitPrice) }">
          {{ formatUnitPriceNumber(row.declareUnitPrice) }}
        </span>
      </template>
      <template #col-dutyAmount="{ row }">
        <span class="cdi-fee-amt" :class="{ 'cdi-fee-amt--zero': isZeroAmount(row.dutyAmount) }">
          {{ formatTotalAmountNumber(row.dutyAmount) }}
        </span>
      </template>
      <template #col-vatAmount="{ row }">
        <span class="cdi-fee-amt" :class="{ 'cdi-fee-amt--zero': isZeroAmount(row.vatAmount) }">
          {{ formatTotalAmountNumber(row.vatAmount) }}
        </span>
      </template>
      <template #col-customsPaymentGoods="{ row }">
        <span class="cdi-fee-amt" :class="{ 'cdi-fee-amt--zero': isZeroAmount(row.customsPaymentGoods) }">
          {{ formatTotalAmountNumber(row.customsPaymentGoods) }}
        </span>
      </template>
      <template #col-customsAgencyFee="{ row }">
        <span class="cdi-fee-amt" :class="{ 'cdi-fee-amt--zero': isZeroAmount(row.customsAgencyFee) }">
          {{ formatTotalAmountNumber(row.customsAgencyFee) }}
        </span>
      </template>
      <template #col-otherFee="{ row }">
        <span class="cdi-fee-amt" :class="{ 'cdi-fee-amt--zero': isZeroAmount(row.otherFee) }">
          {{ formatTotalAmountNumber(row.otherFee) }}
        </span>
      </template>
      <template #col-inspectionFee="{ row }">
        <span class="cdi-fee-amt" :class="{ 'cdi-fee-amt--zero': isZeroAmount(row.inspectionFee) }">
          {{ formatTotalAmountNumber(row.inspectionFee) }}
        </span>
      </template>
      <template #col-totalValueTax="{ row }">
        <span class="cdi-fee-amt" :class="{ 'cdi-fee-amt--zero': isZeroAmount(row.totalValueTax) }">
          {{ formatTotalAmountNumber(row.totalValueTax) }}
        </span>
      </template>
      <template #col-taxIncludedUnitPrice="{ row }">
        <span class="cdi-fee-amt" :class="{ 'cdi-fee-amt--zero': isZeroAmount(row.taxIncludedUnitPrice) }">
          {{ formatUnitPriceNumber(row.taxIncludedUnitPrice) }}
        </span>
      </template>
      <template #col-createTime="{ row }">
        <template v-for="parts in [row.createTime ? formatDisplayDateTime2DigitYearParts(row.createTime) : null]" :key="'ct-' + row.id">
          <span v-if="parts" class="crm-quote-create-time">
            <span class="crm-quote-create-time__ymd">{{ parts.date }}</span>
            <span class="crm-quote-create-time__hm">{{ parts.time }}</span>
          </span>
          <span v-else>—</span>
        </template>
      </template>
      <template #col-createUserDisplay="{ row }">
        <span>{{ row.createUserDisplay || '—' }}</span>
      </template>
    </CrmDataTable>

    <div class="pagination-wrap">
      <div class="list-footer-left">
        <el-tooltip :content="t('customsPages.items.columnSettings')" placement="top" :hide-after="0">
          <el-button
            class="list-settings-btn"
            link
            type="primary"
            :aria-label="t('customsPages.items.columnSettings')"
            @click="dataTableRef?.openColumnSettings?.()"
          >
            <el-icon><Setting /></el-icon>
          </el-button>
        </el-tooltip>
        <span ref="rowDensityToggleAnchorEl" class="list-footer-density-anchor" aria-hidden="true" />
        <div class="list-footer-spacer" aria-hidden="true" />
      </div>
      <el-pagination
        v-model:current-page="query.page"
        v-model:page-size="query.pageSize"
        :total="listTotal"
        :page-sizes="[10, 20, 50, 100]"
        layout="total, sizes, prev, pager, next, jumper"
        class="list-main-pagination"
        @size-change="onPageSizeChange"
        @current-change="clampPage"
      />
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { Setting } from '@element-plus/icons-vue'
import CrmDataTable from '@/components/CrmDataTable.vue'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'
import { fetchCustomsDeclarationItems, type CustomsDeclarationItemListItemDto } from '@/api/customs'
import { useAuthStore } from '@/stores/auth'
import { usePurchaseSensitiveFieldMask } from '@/composables/usePurchaseSensitiveFieldMask'
import { estimateListColumnHeaderMinWidth } from '@/utils/listColumnHeaderWidth'
import { formatDisplayDate, formatDisplayDateTime2DigitYearParts } from '@/utils/displayDateTime'
import {
  formatTotalAmountNumber,
  formatUnitPriceNumber,
  listAmountCurrencyDockClass,
  listAmountCurrencyIso,
  splitListMoneyParts,
  splitUnitPriceDockParts
} from '@/utils/moneyFormat'

const { t } = useI18n()
const router = useRouter()
const authStore = useAuthStore()
const { maskPurchaseSensitiveFields } = usePurchaseSensitiveFieldMask()

const loading = ref(false)
const allRows = ref<CustomsDeclarationItemListItemDto[]>([])
const dataTableRef = ref<{ openColumnSettings?: () => void } | null>(null)
const rowDensityToggleAnchorEl = ref<HTMLElement | null>(null)
const query = reactive({ page: 1, pageSize: 20 })

const filters = reactive({
  declarationCode: '',
  purchasePn: '',
  customer: '',
  salesUserId: '',
  sellOrderItemCode: '',
  stockOutRequestId: '',
  purchaseOrderItemCode: ''
})

function colW(label: string, extra = 0, align: 'left' | 'center' | 'right' = 'left') {
  return estimateListColumnHeaderMinWidth(label, { align, extra })
}

const tableColumns = computed<CrmTableColumnDef[]>(() => {
  const cols: CrmTableColumnDef[] = [
    {
      key: 'declareDate',
      label: t('customsPages.items.colDeclareDate'),
      prop: 'declareDate',
      width: colW(t('customsPages.items.colDeclareDate')),
      minWidth: colW(t('customsPages.items.colDeclareDate'))
    },
    {
      key: 'declarationCode',
      label: t('customsPages.items.colDecCode'),
      prop: 'declarationCode',
      width: colW(t('customsPages.items.colDecCode'), 8),
      minWidth: colW(t('customsPages.items.colDecCode'), 8)
    },
    {
      key: 'customerName',
      label: t('customsPages.items.colCustomer'),
      prop: 'customerName',
      minWidth: Math.max(280, colW(t('customsPages.items.colCustomer'))),
      showOverflowTooltip: true
    },
    {
      key: 'salesUserName',
      label: t('customsPages.items.colSales'),
      prop: 'salesUserName',
      minWidth: Math.max(140, colW(t('customsPages.items.colSales')))
    },
    {
      key: 'purchasePn',
      label: t('customsPages.items.colPn'),
      prop: 'purchasePn',
      minWidth: Math.max(200, colW(t('customsPages.items.colPn')))
    },
    {
      key: 'purchaseBrand',
      label: t('customsPages.items.colBrand'),
      prop: 'purchaseBrand',
      minWidth: Math.max(140, colW(t('customsPages.items.colBrand')))
    },
    {
      key: 'declareQty',
      label: t('customsPages.items.colQty'),
      prop: 'declareQty',
      width: colW(t('customsPages.items.colQty'), 0, 'right'),
      minWidth: colW(t('customsPages.items.colQty'), 0, 'right'),
      align: 'right'
    }
  ]

  if (!maskPurchaseSensitiveFields.value) {
    cols.push(
      {
        key: 'purchaseOrderItemCode',
        label: t('customsPages.items.colPoLine'),
        prop: 'purchaseOrderItemCode',
        minWidth: Math.max(180, colW(t('customsPages.items.colPoLine')))
      },
      {
        key: 'originalPurchasePrice',
        label: t('customsPages.items.colOrigPurchasePrice'),
        prop: 'originalPurchasePrice',
        width: colW(t('customsPages.items.colOrigPurchasePrice'), 24, 'right'),
        minWidth: colW(t('customsPages.items.colOrigPurchasePrice'), 24, 'right'),
        align: 'right'
      },
      {
        key: 'originalPurchaseAmount',
        label: t('customsPages.items.colOrigPurchaseAmount'),
        prop: 'originalPurchaseAmount',
        width: colW(t('customsPages.items.colOrigPurchaseAmount'), 24, 'right'),
        minWidth: colW(t('customsPages.items.colOrigPurchaseAmount'), 24, 'right'),
        align: 'right'
      }
    )
  }

  cols.push(
    {
      key: 'declareUnitPrice',
      label: t('customsPages.items.colUnitPrice'),
      prop: 'declareUnitPrice',
      width: colW(t('customsPages.items.colUnitPrice'), 0, 'right'),
      minWidth: colW(t('customsPages.items.colUnitPrice'), 0, 'right'),
      align: 'right'
    },
    {
      key: 'dutyAmount',
      label: t('customsPages.items.colDuty'),
      prop: 'dutyAmount',
      width: colW(t('customsPages.items.colDuty'), 0, 'right'),
      minWidth: colW(t('customsPages.items.colDuty'), 0, 'right'),
      align: 'right'
    },
    {
      key: 'vatAmount',
      label: t('customsPages.items.colVat'),
      prop: 'vatAmount',
      width: Math.max(120, colW(t('customsPages.items.colVat'), 0, 'right')),
      minWidth: Math.max(120, colW(t('customsPages.items.colVat'), 0, 'right')),
      align: 'right'
    },
    {
      key: 'customsPaymentGoods',
      label: t('customsPages.items.colGoods'),
      prop: 'customsPaymentGoods',
      width: colW(t('customsPages.items.colGoods'), 0, 'right'),
      minWidth: colW(t('customsPages.items.colGoods'), 0, 'right'),
      align: 'right'
    },
    {
      key: 'customsAgencyFee',
      label: t('customsPages.items.colAgency'),
      prop: 'customsAgencyFee',
      width: colW(t('customsPages.items.colAgency'), 0, 'right'),
      minWidth: colW(t('customsPages.items.colAgency'), 0, 'right'),
      align: 'right'
    },
    {
      key: 'otherFee',
      label: t('customsPages.items.colOther'),
      prop: 'otherFee',
      width: colW(t('customsPages.items.colOther'), 0, 'right'),
      minWidth: colW(t('customsPages.items.colOther'), 0, 'right'),
      align: 'right'
    },
    {
      key: 'inspectionFee',
      label: t('customsPages.items.colInspection'),
      prop: 'inspectionFee',
      width: colW(t('customsPages.items.colInspection'), 0, 'right'),
      minWidth: colW(t('customsPages.items.colInspection'), 0, 'right'),
      align: 'right'
    },
    {
      key: 'totalValueTax',
      label: t('customsPages.items.colTotalTax'),
      prop: 'totalValueTax',
      width: colW(t('customsPages.items.colTotalTax'), 0, 'right'),
      minWidth: colW(t('customsPages.items.colTotalTax'), 0, 'right'),
      align: 'right'
    },
    {
      key: 'taxIncludedUnitPrice',
      label: t('customsPages.items.colTaxUnit'),
      prop: 'taxIncludedUnitPrice',
      width: colW(t('customsPages.items.colTaxUnit'), 0, 'right'),
      minWidth: colW(t('customsPages.items.colTaxUnit'), 0, 'right'),
      align: 'right'
    },
    {
      key: 'createTime',
      label: t('customsPages.items.colCreateTime'),
      prop: 'createTime',
      width: Math.max(168, colW(t('customsPages.items.colCreateTime'), 16)),
      minWidth: Math.max(168, colW(t('customsPages.items.colCreateTime'), 16))
    },
    {
      key: 'createUserDisplay',
      label: t('customsPages.items.colCreator'),
      prop: 'createUserDisplay',
      width: colW(t('customsPages.items.colCreator')),
      minWidth: colW(t('customsPages.items.colCreator')),
      showOverflowTooltip: true
    }
  )
  return cols
})

const listTotal = computed(() => allRows.value.length)

const pagedRows = computed(() => {
  const start = (query.page - 1) * query.pageSize
  return allRows.value.slice(start, start + query.pageSize)
})

function formatQtyCell(v: unknown) {
  if (v == null || v === '') return '—'
  const n = Number(v)
  if (!Number.isFinite(n)) return '—'
  return n.toLocaleString('zh-CN')
}

function isZeroAmount(v: unknown) {
  const n = Number(v)
  return Number.isFinite(n) && n === 0
}

function canOpenPurchaseOrder(row: CustomsDeclarationItemListItemDto): boolean {
  return (
    !maskPurchaseSensitiveFields.value &&
    authStore.hasPermission('purchase-order.read') &&
    !!row.purchaseOrderId &&
    !!row.purchaseOrderItemCode
  )
}

function clampPage() {
  const maxPage = Math.max(1, Math.ceil(listTotal.value / query.pageSize) || 1)
  if (query.page > maxPage) query.page = maxPage
}

function onPageSizeChange() {
  query.page = 1
  clampPage()
}

function handleSearch() {
  query.page = 1
  void load()
}

function resetFilters() {
  filters.declarationCode = ''
  filters.purchasePn = ''
  filters.customer = ''
  filters.salesUserId = ''
  filters.sellOrderItemCode = ''
  filters.stockOutRequestId = ''
  filters.purchaseOrderItemCode = ''
  handleSearch()
}

async function load() {
  loading.value = true
  try {
    const params: Record<string, unknown> = { take: 500 }
    if (filters.declarationCode.trim()) params.declarationCode = filters.declarationCode.trim()
    if (filters.purchasePn.trim()) params.purchasePn = filters.purchasePn.trim()
    if (filters.customer.trim()) params.customer = filters.customer.trim()
    if (filters.salesUserId.trim()) params.salesUserId = filters.salesUserId.trim()
    if (filters.sellOrderItemCode.trim()) params.sellOrderItemCode = filters.sellOrderItemCode.trim()
    if (filters.stockOutRequestId.trim()) params.stockOutRequestId = filters.stockOutRequestId.trim()
    if (!maskPurchaseSensitiveFields.value && filters.purchaseOrderItemCode.trim()) {
      params.purchaseOrderItemCode = filters.purchaseOrderItemCode.trim()
    }
    allRows.value = await fetchCustomsDeclarationItems(params)
    clampPage()
  } catch (e: unknown) {
    ElMessage.error(e instanceof Error ? e.message : String(e))
  } finally {
    loading.value = false
  }
}

function onRowDblClick(row: CustomsDeclarationItemListItemDto) {
  if (!row.declarationId) return
  void router.push({ name: 'CustomsDeclarationDetail', params: { id: row.declarationId } })
}

onMounted(() => {
  void load()
})
</script>

<style lang="scss" scoped>
@import '../Finance/finance-common.scss';

.page-header-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
}

.count-badge {
  font-size: 12px;
  color: $text-muted;
  white-space: nowrap;
}

.search-bar {
  margin-bottom: 0;
}

.search-input-wrap {
  position: relative;
  display: flex;
  align-items: center;
}

.search-icon {
  position: absolute;
  left: 10px;
  color: $text-muted;
  pointer-events: none;
}

.search-input {
  width: 180px;
  padding: 7px 12px 7px 32px;
  background: $layer-2;
  border: 1px solid $border-panel;
  border-radius: $border-radius-md;
  color: $text-primary;
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
  outline: none;
  transition: border-color 0.2s;

  &::placeholder {
    color: $text-muted;
  }
  &:focus {
    border-color: rgba(0, 212, 255, 0.4);
  }
}

.search-input--filter {
  width: 160px;
  padding-left: 12px;
}

.btn-primary,
.btn-ghost {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  border-radius: $border-radius-md;
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  transition: all 0.2s;
}

.btn-sm {
  padding: 6px 12px;
  font-size: 12px;
}

.btn-primary {
  border: none;
  background: linear-gradient(135deg, #00d4ff 0%, #0099cc 100%);
  color: #fff;
  font-weight: 500;
  letter-spacing: 0.5px;

  &:hover:not(:disabled) {
    transform: translateY(-1px);
    box-shadow: 0 4px 12px rgba(0, 212, 255, 0.35);
  }
  &:disabled {
    opacity: 0.55;
    cursor: not-allowed;
  }
}

.btn-ghost {
  border: 1px solid $border-panel;
  background: transparent;
  color: $text-muted;

  &:hover:not(:disabled) {
    border-color: rgba(0, 212, 255, 0.4);
    color: $text-primary;
  }
  &:disabled {
    opacity: 0.55;
    cursor: not-allowed;
  }
}

.pagination-wrap {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  flex-wrap: wrap;
  gap: 8px;
}

.list-footer-left {
  display: inline-flex;
  align-items: flex-start;
  gap: 0;
}

.list-settings-btn {
  padding: 4px 6px !important;
  min-width: 28px;
}

.list-footer-density-anchor {
  display: inline-flex;
  align-items: center;
  min-width: 0;
  min-height: 0;
}

.list-footer-spacer {
  width: 26px;
  flex: 0 0 26px;
}

.list-main-pagination {
  margin-left: auto;
}

.text-secondary {
  color: $text-secondary;
}

.cdi-fee-amt {
  font-family: 'Noto Sans SC', sans-serif;
  font-variant-numeric: tabular-nums;
  font-size: 13px;
  font-weight: 400;
  color: #3db88a;
  white-space: nowrap;
}

.cdi-fee-amt--zero {
  color: $text-secondary;
}

.link-text {
  color: inherit;
  text-decoration: none;
  cursor: default;

  &:hover {
    color: var(--el-color-primary);
    text-decoration: underline;
    cursor: pointer;
  }
}

.cdi-list-qty {
  display: inline-block;
  max-width: 100%;
  font-weight: 400;
  color: #27292c;
  font-variant-numeric: tabular-nums;
  white-space: nowrap;
}

html[data-theme='dark'] .cdi-list-qty {
  color: $text-primary;
}

.cdi-list-dash {
  color: $text-muted;
}

.cdi-list-amount-cell {
  display: inline-flex;
  align-items: baseline;
  justify-content: flex-end;
  flex-wrap: nowrap;
}

.cdi-list-amt,
.cdi-list-amt-int,
.cdi-list-amt-frac {
  font-weight: 400;
  color: #27292c;
  font-variant-numeric: tabular-nums;
  white-space: nowrap;
}

html[data-theme='dark'] .cdi-list-amt,
html[data-theme='dark'] .cdi-list-amt-int,
html[data-theme='dark'] .cdi-list-amt-frac {
  color: $text-primary;
}

.crm-items-table--density-compact :deep(.el-table__body td .cdi-list-amount-cell) {
  flex-wrap: nowrap;
}
</style>
