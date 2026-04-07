<template>
  <div class="arrival-notice-list-page">
    <div class="page-header">
      <h2>{{ t('arrivalNoticeList.title') }}</h2>
      <div class="ops">
        <el-button @click="loadData">{{ t('arrivalNoticeList.refresh') }}</el-button>
      </div>
    </div>

    <!-- 搜索栏：与客户列表 CustomerList 同款布局与控件皮肤 -->
    <div class="search-bar">
      <div class="search-left">
        <span class="filter-field-label">{{ t('arrivalNoticeList.filters.status') }}</span>
        <el-select
          v-model="filters.status"
          :placeholder="t('arrivalNoticeList.filters.allStatus')"
          clearable
          class="status-select"
          :teleported="false"
          @change="loadData"
        >
          <el-option :label="t('arrivalNoticeList.status.new')" :value="1" />
          <el-option :label="t('arrivalNoticeList.status.notArrived')" :value="10" />
          <el-option :label="t('arrivalNoticeList.status.pendingQc')" :value="20" />
          <el-option :label="t('arrivalNoticeList.status.qcDone')" :value="30" />
          <el-option :label="t('arrivalNoticeList.status.stocked')" :value="100" />
        </el-select>
        <span class="filter-field-label">{{ t('arrivalNoticeList.filters.poCode') }}</span>
        <div class="search-input-wrap">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="search-icon">
            <circle cx="11" cy="11" r="8" />
            <line x1="21" y1="21" x2="16.65" y2="16.65" />
          </svg>
          <input
            v-model="filters.purchaseOrderCode"
            class="search-input"
            :placeholder="t('arrivalNoticeList.filters.poCodePlaceholder')"
            @keyup.enter="loadData"
          />
        </div>
        <span class="filter-field-label">{{ t('arrivalNoticeList.filters.expectedDate') }}</span>
        <el-date-picker
          v-model="filters.expectedArrivalDate"
          type="date"
          value-format="YYYY-MM-DD"
          :placeholder="t('arrivalNoticeList.filters.datePlaceholder')"
          clearable
          class="filter-date-single"
          :teleported="false"
          @change="loadData"
        />
        <button type="button" class="btn-primary btn-sm" :disabled="loading" @click="loadData">
          {{ t('arrivalNoticeList.filters.search') }}
        </button>
        <button type="button" class="btn-ghost btn-sm" :disabled="loading" @click="resetFilters">
          {{ t('arrivalNoticeList.filters.reset') }}
        </button>
      </div>
    </div>

    <CrmDataTable
      ref="dataTableRef"
      column-layout-key="arrival-notice-list-main"
      :columns="arrivalNoticeColumns"
      :show-column-settings="false"
      :data="list"
      v-loading="loading"
    >
      <template #col-status="{ row }">
        <el-tag effect="dark" :type="statusType(row.status)">{{ statusText(row.status) }}</el-tag>
      </template>
      <template #col-pn="{ row }">{{ displayPn(row) }}</template>
      <template #col-brand="{ row }">{{ displayBrand(row) }}</template>
      <template #col-expectedArrivalDate="{ row }">{{ formatExpected(row.expectedArrivalDate) }}</template>
      <template #col-regionType="{ row }">{{ regionTypeLabel(row) }}</template>
      <template #col-expectQty="{ row }">{{ formatQtyCol(expectQty(row)) }}</template>
      <template #col-receiveQty="{ row }">{{ formatQtyCol(receiveQty(row)) }}</template>
      <template #col-passedQty="{ row }">{{ formatQtyCol(passedQty(row)) }}</template>
      <template #col-createTime="{ row }">{{ formatTime(row.createTime) }}</template>
      <template #col-createUser="{ row }">{{ row.createUserName || row.createdBy || row.purchaseUserName || '--' }}</template>
      <template #col-actions-header>
        <div class="op-col-header">
            <span class="op-col-header-text">{{ t('arrivalNoticeList.columns.actions') }}</span>
          <button type="button" class="op-col-toggle-btn" @click.stop="toggleOpCol">
            {{ opColExpanded ? '>' : '<' }}
          </button>
        </div>
      </template>
      <template #col-actions="{ row }">
        <div @click.stop @dblclick.stop>
          <div v-if="opColExpanded" class="action-btns">
            <button
              v-if="row.status === 10"
              type="button"
              class="action-btn action-btn--warning"
              @click.stop="markArrived(row)"
            >
              {{ t('arrivalNoticeList.actions.confirmArrived') }}
            </button>
            <button
              v-if="row.status === 20"
              type="button"
              class="action-btn action-btn--warning"
              @click.stop="goCreateQc(row)"
            >
              {{ t('arrivalNoticeList.actions.qc') }}
            </button>
            <button type="button" class="action-btn action-btn--info" @click.stop="viewItems(row)">
              {{ t('arrivalNoticeList.actions.detail') }}
            </button>
          </div>

          <el-dropdown v-else trigger="click" placement="bottom-end">
            <div class="op-more-dropdown-trigger">
              <button type="button" class="op-more-trigger">...</button>
            </div>
            <template #dropdown>
              <el-dropdown-menu>
                <el-dropdown-item v-if="row.status === 10" @click.stop="markArrived(row)">
                  <span class="op-more-item op-more-item--warning">{{ t('arrivalNoticeList.actions.confirmArrived') }}</span>
                </el-dropdown-item>
                <el-dropdown-item v-if="row.status === 20" @click.stop="goCreateQc(row)">
                  <span class="op-more-item op-more-item--warning">{{ t('arrivalNoticeList.actions.qc') }}</span>
                </el-dropdown-item>
                <el-dropdown-item @click.stop="viewItems(row)">
                  <span class="op-more-item op-more-item--info">{{ t('arrivalNoticeList.actions.detail') }}</span>
                </el-dropdown-item>
              </el-dropdown-menu>
            </template>
          </el-dropdown>
        </div>
      </template>
    </CrmDataTable>
    <div class="pagination-wrapper">
      <div class="list-footer-left">
        <el-tooltip :content="t('arrivalNoticeList.columnSettings')" placement="top" :hide-after="0">
          <el-button
            class="list-settings-btn"
            link
            type="primary"
            :aria-label="t('arrivalNoticeList.columnSettings')"
            @click="dataTableRef?.openColumnSettings?.()"
          >
            <el-icon><Setting /></el-icon>
          </el-button>
        </el-tooltip>
        <div class="list-footer-spacer" aria-hidden="true"></div>
      </div>
    </div>

    <el-dialog
      v-model="itemsVisible"
      :title="t('arrivalNoticeList.detailDialog.title')"
      width="720px"
      align-center
      destroy-on-close
      class="arrival-detail-dialog"
      @closed="onDetailClosed"
    >
      <el-descriptions
        v-if="detailNotice"
        :column="2"
        border
        size="small"
        class="arrival-detail-desc"
        :label-style="arrivalDetailLabelStyle"
      >
        <el-descriptions-item :label="t('arrivalNoticeList.detailDialog.vendorName')">
          {{ detailNotice.vendorName?.trim() || '—' }}
        </el-descriptions-item>
        <el-descriptions-item :label="t('arrivalNoticeList.detailDialog.vendorCode')">
          {{ detailNotice.vendorCode?.trim() || '—' }}
        </el-descriptions-item>
        <el-descriptions-item :label="t('arrivalNoticeList.detailDialog.purchaseOrderCode')">
          {{ detailNotice.purchaseOrderCode?.trim() || '—' }}
        </el-descriptions-item>
        <el-descriptions-item :label="t('arrivalNoticeList.detailDialog.pn')">
          {{ displayPn(detailNotice) }}
        </el-descriptions-item>
        <el-descriptions-item :label="t('arrivalNoticeList.detailDialog.brand')">
          {{ displayBrand(detailNotice) }}
        </el-descriptions-item>
        <el-descriptions-item :label="t('arrivalNoticeList.detailDialog.expectedArrivalDate')">
          {{ formatExpected(detailNotice.expectedArrivalDate) }}
        </el-descriptions-item>
        <el-descriptions-item :label="t('arrivalNoticeList.detailDialog.regionType')">
          {{ regionTypeLabel(detailNotice) }}
        </el-descriptions-item>
        <el-descriptions-item :label="t('arrivalNoticeList.detailDialog.purchaser')">
          {{ detailNotice.purchaseUserName?.trim() || '—' }}
        </el-descriptions-item>
        <el-descriptions-item :label="t('arrivalNoticeList.detailDialog.noticeQty')">
          {{ formatQtyCol(expectQty(detailNotice)) }}
        </el-descriptions-item>
        <el-descriptions-item :label="t('arrivalNoticeList.detailDialog.receivedQty')">
          {{ formatQtyCol(receiveQty(detailNotice)) }}
        </el-descriptions-item>
        <el-descriptions-item :label="t('arrivalNoticeList.detailDialog.passedQty')">
          {{ formatQtyCol(passedQty(detailNotice)) }}
        </el-descriptions-item>
        <el-descriptions-item :label="t('arrivalNoticeList.detailDialog.stockInQty')">
          {{ stockInQtyText(detailNotice) }}
        </el-descriptions-item>
      </el-descriptions>
    </el-dialog>

  </div>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import { Setting } from '@element-plus/icons-vue'
import { logisticsApi, type StockInNotifyDto, type StockInNotifyItemDto } from '@/api/logistics'
import { normalizeRegionType, REGION_TYPE_OVERSEAS } from '@/constants/regionType'
import { useRouter } from 'vue-router'
import { formatDisplayDate, formatDisplayDateTime } from '@/utils/displayDateTime'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'

const router = useRouter()
const { t, locale } = useI18n()
const loading = ref(false)
const list = ref<StockInNotifyDto[]>([])
const dataTableRef = ref<{ openColumnSettings?: () => void } | null>(null)

// 列表操作列：默认收起（Collapsed）
const opColExpanded = ref(false)
const OP_COL_COLLAPSED_WIDTH = 96
const OP_COL_EXPANDED_WIDTH = 220
const OP_COL_EXPANDED_MIN_WIDTH = 220
const opColWidth = computed(() => (opColExpanded.value ? OP_COL_EXPANDED_WIDTH : OP_COL_COLLAPSED_WIDTH))
const opColMinWidth = computed(() => (opColExpanded.value ? OP_COL_EXPANDED_MIN_WIDTH : OP_COL_COLLAPSED_WIDTH))
function toggleOpCol() {
  opColExpanded.value = !opColExpanded.value
}

const arrivalNoticeColumns = computed<CrmTableColumnDef[]>(() => {
  void locale.value
  return [
    { key: 'noticeCode', label: t('arrivalNoticeList.columns.noticeCode'), prop: 'noticeCode', width: 170 },
    { key: 'status', label: t('arrivalNoticeList.columns.status'), prop: 'status', width: 110, align: 'center' },
    { key: 'purchaseOrderCode', label: t('arrivalNoticeList.columns.purchaseOrderCode'), prop: 'purchaseOrderCode', width: 160 },
    { key: 'pn', label: t('arrivalNoticeList.columns.pn'), minWidth: 120, showOverflowTooltip: true },
    { key: 'brand', label: t('arrivalNoticeList.columns.brand'), width: 100, showOverflowTooltip: true },
    {
      key: 'expectedArrivalDate',
      label: t('arrivalNoticeList.columns.expectedArrivalDate'),
      width: 130,
      align: 'center'
    },
    { key: 'vendorName', label: t('arrivalNoticeList.columns.vendorName'), prop: 'vendorName', minWidth: 160 },
    { key: 'purchaseUserName', label: t('arrivalNoticeList.columns.purchaseUserName'), prop: 'purchaseUserName', width: 120 },
    { key: 'expectQty', label: t('arrivalNoticeList.columns.expectQty'), width: 100, align: 'right' },
    { key: 'receiveQty', label: t('arrivalNoticeList.columns.receiveQty'), width: 100, align: 'right' },
    { key: 'passedQty', label: t('arrivalNoticeList.columns.passedQty'), width: 100, align: 'right' },
    {
      key: 'regionType',
      label: t('arrivalNoticeList.columns.arrivalRegion'),
      width: 100,
      align: 'center'
    },
    { key: 'createTime', label: t('arrivalNoticeList.columns.createTime'), prop: 'createTime', width: 170 },
    { key: 'createUser', label: t('arrivalNoticeList.columns.createUser'), width: 120, showOverflowTooltip: true },
    {
      key: 'actions',
      label: t('arrivalNoticeList.columns.actions'),
      width: opColWidth.value,
      minWidth: opColMinWidth.value,
      fixed: 'right',
      hideable: false,
      pinned: 'end',
      reorderable: false,
      className: 'op-col',
      labelClassName: 'op-col'
    }
  ]
})

const itemsVisible = ref(false)
const detailNotice = ref<StockInNotifyDto | null>(null)

/** 标签区至少单行容纳 6 个汉字 */
const arrivalDetailLabelStyle = { minWidth: '8.5em', whiteSpace: 'nowrap' as const }
const filters = ref<{
  status?: number
  purchaseOrderCode: string
  expectedArrivalDate: string
}>({
  status: undefined,
  purchaseOrderCode: '',
  expectedArrivalDate: ''
})

const num = (v: unknown) => Number(v ?? 0)

const qtyFromItems = (items: StockInNotifyItemDto[] | undefined, key: 'arrivedQty' | 'qty' | 'passedQty') =>
  Number((items || []).reduce((s, x) => s + num(x?.[key]), 0).toFixed(4))

/** 行级优先，与单表到货通知模型一致；缺省再从 items 汇总 */
const pickQty = (
  rowVal: number | undefined | null,
  items: StockInNotifyItemDto[] | undefined,
  itemKey: 'qty' | 'arrivedQty' | 'passedQty'
) => (rowVal != null && !Number.isNaN(Number(rowVal)) ? Number(rowVal) : qtyFromItems(items, itemKey))

const expectQty = (row: StockInNotifyDto) => pickQty(row.expectQty, row.items, 'qty')
const receiveQty = (row: StockInNotifyDto) => pickQty(row.receiveQty, row.items, 'arrivedQty')
const passedQty = (row: StockInNotifyDto) => pickQty(row.passedQty, row.items, 'passedQty')

const rawPn = (row: StockInNotifyDto) => (row.pn != null && row.pn !== '' ? row.pn : row.items?.[0]?.pn) || ''
const rawBrand = (row: StockInNotifyDto) => (row.brand != null && row.brand !== '' ? row.brand : row.items?.[0]?.brand) || ''
const displayPn = (row: StockInNotifyDto) => rawPn(row) || '—'
const displayBrand = (row: StockInNotifyDto) => rawBrand(row) || '—'

const formatQtyCol = (n: number) => (Number.isInteger(n) ? String(n) : n.toFixed(4).replace(/\.?0+$/, '') || '0')

/** 已入库(100)后展示实收入库数量；此前尚无独立「入库数」字段时显示 — */
const stockInQtyText = (row: StockInNotifyDto) =>
  row.status === 100 ? formatQtyCol(receiveQty(row)) : '—'

const statusText = (s: number) => {
  const keyMap: Record<number, 'new' | 'notArrived' | 'pendingQc' | 'qcDone' | 'stocked'> = {
    1: 'new',
    10: 'notArrived',
    20: 'pendingQc',
    30: 'qcDone',
    100: 'stocked'
  }
  const k = keyMap[s]
  return k ? t(`arrivalNoticeList.status.${k}`) : t('arrivalNoticeList.statusUnknown')
}
const statusType = (s: number) => ({ 1: 'info', 10: 'warning', 20: 'primary', 30: 'success', 100: 'success' }[s] || 'info')
const formatTime = (v?: string) => formatDisplayDateTime(v)
const formatExpected = (v?: string | null) => (v ? formatDisplayDate(v) : '—')

const regionTypeLabel = (row: StockInNotifyDto) => {
  const r = row as unknown as Record<string, unknown>
  const n = normalizeRegionType(r.regionType ?? r.RegionType)
  return n === REGION_TYPE_OVERSEAS ? t('inventoryList.warehouse.regionOverseas') : t('inventoryList.warehouse.regionDomestic')
}

const loadData = () => {
  loading.value = true
  logisticsApi.getArrivalNotices({
    status: filters.value.status,
    purchaseOrderCode: filters.value.purchaseOrderCode.trim() || undefined,
    expectedArrivalDate: filters.value.expectedArrivalDate || undefined
  })
    .then(res => { list.value = (res || []).sort((a, b) => (a.createTime < b.createTime ? 1 : -1)) })
    .finally(() => { loading.value = false })
}

const resetFilters = () => {
  filters.value = {
    status: undefined,
    purchaseOrderCode: '',
    expectedArrivalDate: ''
  }
  loadData()
}

const markArrived = async (row: StockInNotifyDto) => {
  await logisticsApi.updateArrivalStatus(row.id, 20)
  loadData()
  ElMessage.success(t('arrivalNoticeList.messages.arrivedSuccess'))
}

const goCreateQc = (row: StockInNotifyDto) => {
  router.push({ name: 'QcCreate', query: { noticeId: row.id } })
}

const viewItems = (row: StockInNotifyDto) => {
  detailNotice.value = row
  itemsVisible.value = true
}

const onDetailClosed = () => {
  detailNotice.value = null
}

loadData()
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.arrival-notice-list-page {
  padding: 24px;
  min-height: 100%;
  background: $layer-1;
  font-family: 'Noto Sans SC', sans-serif;
}

.page-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 20px;

  h2 {
    margin: 0;
    color: $text-primary;
    font-size: 20px;
    font-weight: 600;
  }
}

.ops {
  display: flex;
  gap: 8px;
}

// ---- 搜索栏（与 CustomerList / PurchaseRequisitionListPage 一致）----
.search-bar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 12px;
}

.search-left {
  display: flex;
  align-items: center;
  gap: 10px;
  flex-wrap: wrap;
}

.filter-field-label {
  font-size: 12px;
  font-weight: 500;
  color: $text-muted;
  white-space: nowrap;
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
  width: 220px;
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

.status-select {
  width: 140px;
  :deep(.el-select__wrapper) {
    background: $layer-2 !important;
    box-shadow: none !important;
    border: 1px solid $border-panel !important;
    border-radius: $border-radius-md !important;
  }
  :deep(.el-select__placeholder) {
    color: $text-muted !important;
  }
  :deep(.el-select__selected-item) {
    color: $text-primary !important;
  }
}

.filter-date-single {
  width: 170px;
  flex-shrink: 0;
  :deep(.el-input__wrapper) {
    background: $layer-2 !important;
    box-shadow: none !important;
    border: 1px solid $border-panel !important;
    border-radius: $border-radius-md !important;
  }
  :deep(.el-input__inner) {
    color: $text-primary !important;
    font-size: 13px !important;
  }
  :deep(.el-input__prefix-inner .el-icon) {
    color: $text-muted !important;
  }
}

.btn-primary {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 8px 16px;
  background: linear-gradient(135deg, rgba(0, 102, 255, 0.8), rgba(0, 212, 255, 0.7));
  border: 1px solid rgba(0, 212, 255, 0.4);
  border-radius: $border-radius-md;
  color: #fff;
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  transition: all 0.2s;
  letter-spacing: 0.5px;

  &:hover:not(:disabled) {
    transform: translateY(-1px);
    box-shadow: 0 4px 16px rgba(0, 212, 255, 0.25);
  }

  &:disabled {
    opacity: 0.55;
    cursor: not-allowed;
    transform: none;
    box-shadow: none;
  }

  &.btn-sm {
    padding: 6px 12px;
    font-size: 12px;
  }
}

.btn-ghost {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 6px 12px;
  background: transparent;
  border: 1px solid $border-panel;
  border-radius: $border-radius-md;
  color: $text-muted;
  font-size: 12px;
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  transition: all 0.2s;

  &:hover:not(:disabled) {
    border-color: rgba(0, 212, 255, 0.3);
    color: $text-secondary;
  }

  &:disabled {
    opacity: 0.55;
    cursor: not-allowed;
  }
}

.arrival-detail-dialog {
  :deep(.arrival-detail-desc .el-descriptions__label) {
    font-weight: 500;
    color: $text-secondary;
  }
  :deep(.arrival-detail-desc .el-descriptions__content) {
    color: $text-primary;
  }
}

.pagination-wrapper {
  margin-top: 12px;
  display: flex;
  align-items: flex-start;
  justify-content: flex-start;
}

.list-footer-left {
  display: inline-flex;
  align-items: flex-start;
  gap: 6px;
}

.list-settings-btn {
  padding: 4px 6px !important;
  min-width: 28px;
}

.list-footer-spacer {
  width: 26px;
  flex: 0 0 26px;
}
</style>
