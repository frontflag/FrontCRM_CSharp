<template>
  <div class="qc-list-page">
    <div class="page-header">
      <h2>{{ t('qcList.title') }}</h2>
      <div class="ops">
        <button type="button" class="btn-ghost btn-sm" :disabled="loading" @click="loadData">{{ t('qcList.refresh') }}</button>
      </div>
    </div>

    <!-- 搜索栏：与 CustomerList / ArrivalNoticeList 同款布局与控件皮肤 -->
    <div class="search-bar">
      <div class="search-left">
        <span class="filter-field-label">{{ t('qcList.filters.model') }}</span>
        <div class="search-input-wrap">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="search-icon">
            <circle cx="11" cy="11" r="8" />
            <line x1="21" y1="21" x2="16.65" y2="16.65" />
          </svg>
          <input
            v-model="filters.model"
            class="search-input"
            :placeholder="t('qcList.filters.modelPlaceholder')"
            @keyup.enter="handleSearch"
          />
        </div>
        <span class="filter-field-label">{{ t('qcList.filters.vendor') }}</span>
        <div class="search-input-wrap">
          <input
            v-model="filters.vendorName"
            class="search-input--plain"
            :placeholder="t('qcList.filters.vendorPlaceholder')"
            @keyup.enter="handleSearch"
          />
        </div>
        <span class="filter-field-label">{{ t('qcList.filters.purchaseOrderCode') }}</span>
        <div class="search-input-wrap">
          <input
            v-model="filters.purchaseOrderCode"
            class="search-input--plain"
            :placeholder="t('qcList.filters.purchaseOrderPlaceholder')"
            @keyup.enter="handleSearch"
          />
        </div>
        <span class="filter-field-label">{{ t('qcList.filters.salesOrderCode') }}</span>
        <div class="search-input-wrap">
          <input
            v-model="filters.salesOrderCode"
            class="search-input--plain"
            :placeholder="t('qcList.filters.salesOrderPlaceholder')"
            @keyup.enter="handleSearch"
          />
        </div>
        <button type="button" class="btn-primary btn-sm" :disabled="loading" @click="handleSearch">
          {{ t('qcList.filters.search') }}
        </button>
        <button type="button" class="btn-ghost btn-sm" :disabled="loading" @click="resetFilters">
          {{ t('qcList.filters.reset') }}
        </button>
      </div>
    </div>

    <CrmDataTable
      ref="dataTableRef"
      column-layout-key="qc-list-main"
      :columns="qcTableColumns"
      :show-column-settings="false"
      :density-toggle-anchor-el="rowDensityToggleAnchorEl"
      :data="list"
      v-loading="loading"
      @row-dblclick="goView"
    >
      <template #col-status="{ row }">
        <el-tag effect="dark" :type="qcType(row.status)">{{ qcText(row.status) }}</el-tag>
      </template>
      <template #col-stockInStatus="{ row }">
        <el-tag effect="dark" :type="stockInType(displayStockInStatus(row))">{{ stockInText(displayStockInStatus(row)) }}</el-tag>
      </template>
      <template #col-createTime="{ row }">{{ formatTime(row.createTime) }}</template>
      <template #col-createUser="{ row }">{{ row.createUserName || row.createdBy || '--' }}</template>
      <template #col-actions-header>
        <div class="op-col-header">
            <span class="op-col-header-text">{{ t('qcList.columns.actions') }}</span>
          <button type="button" class="op-col-toggle-btn" @click.stop="toggleOpCol">
            {{ opColExpanded ? '>' : '<' }}
          </button>
        </div>
      </template>
      <template #col-actions="{ row }">
        <div @click.stop @dblclick.stop>
          <div v-if="opColExpanded" class="action-btns">
            <el-button link type="primary" @click.stop="goView(row)">{{ t('qcList.actions.view') }}</el-button>
            <el-button
              link
              type="warning"
              v-if="canCreateStockIn(row)"
              @click.stop="createStockIn(row)"
            >
              {{ t('qcList.actions.createStockIn') }}
            </el-button>
          </div>

          <el-dropdown v-else trigger="click" placement="bottom-end">
            <div class="op-more-dropdown-trigger">
              <button type="button" class="op-more-trigger">...</button>
            </div>
            <template #dropdown>
              <el-dropdown-menu>
                <el-dropdown-item @click.stop="goView(row)">
                  <span class="op-more-item op-more-item--primary">{{ t('qcList.actions.view') }}</span>
                </el-dropdown-item>
                <el-dropdown-item v-if="canCreateStockIn(row)" @click.stop="createStockIn(row)">
                  <span class="op-more-item op-more-item--warning">{{ t('qcList.actions.createStockIn') }}</span>
                </el-dropdown-item>
              </el-dropdown-menu>
            </template>
          </el-dropdown>
        </div>
      </template>
    </CrmDataTable>
    <div class="pagination-wrapper">
      <div class="list-footer-left">
        <el-tooltip :content="t('qcList.columnSettings')" placement="top" :hide-after="0">
          <el-button
            class="list-settings-btn"
            link
            type="primary"
            :aria-label="t('qcList.columnSettings')"
            @click="dataTableRef?.openColumnSettings?.()"
          >
            <el-icon><Setting /></el-icon>
          </el-button>
        </el-tooltip>
        <span ref="rowDensityToggleAnchorEl" class="list-footer-density-anchor" aria-hidden="true" />
        <div class="list-footer-spacer" aria-hidden="true"></div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Setting } from '@element-plus/icons-vue'
import { logisticsApi, type QcInfoDto } from '@/api/logistics'
import { stockInApi } from '@/api/stockIn'
import { inventoryCenterApi } from '@/api/inventoryCenter'
import { useRouter, useRoute } from 'vue-router'
import { getApiErrorMessage } from '@/utils/apiError'
import { useAuthStore } from '@/stores/auth'
import { formatDisplayDateTime2DigitYear } from '@/utils/displayDateTime'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'

const router = useRouter()
const route = useRoute()
const authStore = useAuthStore()
const { t, locale } = useI18n()
const loading = ref(false)
const list = ref<QcInfoDto[]>([])
const dataTableRef = ref<{ openColumnSettings?: () => void } | null>(null)
const rowDensityToggleAnchorEl = ref<HTMLElement | null>(null)

// 列表操作列：默认收起（Collapsed）
const opColExpanded = ref(false)
const OP_COL_COLLAPSED_WIDTH = 96
const OP_COL_EXPANDED_WIDTH = 200
const OP_COL_EXPANDED_MIN_WIDTH = 200
const opColWidth = computed(() => (opColExpanded.value ? OP_COL_EXPANDED_WIDTH : OP_COL_COLLAPSED_WIDTH))
const opColMinWidth = computed(() => (opColExpanded.value ? OP_COL_EXPANDED_MIN_WIDTH : OP_COL_COLLAPSED_WIDTH))
function toggleOpCol() {
  opColExpanded.value = !opColExpanded.value
}

const qcTableColumns = computed<CrmTableColumnDef[]>(() => {
  void locale.value
  return [
    { key: 'status', label: t('qcList.columns.status'), prop: 'status', width: 120, align: 'center' },
    { key: 'model', label: t('qcList.columns.model'), prop: 'model', minWidth: 160, showOverflowTooltip: true },
    { key: 'brand', label: t('qcList.columns.brand'), prop: 'brand', minWidth: 120, showOverflowTooltip: true },
    { key: 'vendorName', label: t('qcList.columns.vendorName'), prop: 'vendorName', minWidth: 160, showOverflowTooltip: true },
    { key: 'passQty', label: t('qcList.columns.passQty'), prop: 'passQty', width: 110, align: 'right' },
    { key: 'rejectQty', label: t('qcList.columns.rejectQty'), prop: 'rejectQty', width: 110, align: 'right' },
    { key: 'stockInStatus', label: t('qcList.columns.stockInStatus'), width: 120, align: 'center' },
    { key: 'qcCode', label: t('qcList.columns.qcCode'), prop: 'qcCode', width: 160, minWidth: 160 },
    {
      key: 'stockInNotifyCode',
      label: t('qcList.columns.stockInNotifyCode'),
      prop: 'stockInNotifyCode',
      width: 170
    },
    {
      key: 'purchaseOrderCode',
      label: t('qcList.columns.purchaseOrderCode'),
      prop: 'purchaseOrderCode',
      width: 170,
      showOverflowTooltip: true
    },
    {
      key: 'salesOrderCode',
      label: t('qcList.columns.salesOrderCode'),
      prop: 'salesOrderCode',
      width: 170,
      showOverflowTooltip: true
    },
    { key: 'createTime', label: t('qcList.columns.createTime'), prop: 'createTime', width: 170 },
    { key: 'createUser', label: t('qcList.columns.createUser'), width: 120, showOverflowTooltip: true },
    {
      key: 'actions',
      label: t('qcList.columns.actions'),
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

const filters = ref({
  model: '',
  vendorName: '',
  purchaseOrderCode: '',
  salesOrderCode: '',
})
const getYYMMDD = (d: Date) => {
  const yy = String(d.getFullYear()).slice(-2)
  const mm = String(d.getMonth() + 1).padStart(2, '0')
  const dd = String(d.getDate()).padStart(2, '0')
  return `${yy}${mm}${dd}`
}
const random4 = () => String(Math.floor(Math.random() * 10000)).padStart(4, '0')

const qcText = (s: number) => {
  const keyMap: Record<number, 'failed' | 'partial' | 'passed'> = {
    [-1]: 'failed',
    10: 'partial',
    100: 'passed'
  }
  const k = keyMap[s]
  return k ? t(`qcList.qcStatus.${k}`) : t('qcList.qcStatus.unknown')
}
const qcType = (s: number) => ({ [-1]: 'danger', 10: 'warning', 100: 'success' }[s] || 'info')
const stockInText = (s: number | undefined) => {
  const keyMap: Record<number, 'rejected' | 'notStocked' | 'partial' | 'all'> = {
    [-1]: 'rejected',
    1: 'notStocked',
    10: 'partial',
    100: 'all'
  }
  if (s === undefined || s === null) return t('qcList.stockInStatus.unknown')
  const k = keyMap[s]
  return k ? t(`qcList.stockInStatus.${k}`) : t('qcList.stockInStatus.unknown')
}
const stockInType = (s: number | undefined) =>
  (s === undefined || s === null ? 'info' : ({ [-1]: 'danger', 1: 'info', 10: 'warning', 100: 'success' }[s] || 'info'))

/** 未绑定入库单时忽略历史脏数据（StockInStatus 误存为 10/100） */
const displayStockInStatus = (row: QcInfoDto) => {
  if (row.status === -1) return -1
  if (!row.stockInId) return 1
  return row.stockInStatus
}
const formatTime = (v?: string) => formatDisplayDateTime2DigitYear(v)

function syncFiltersFromRoute() {
  if (route.name !== 'QcList') return
  const q = route.query
  filters.value.model = typeof q.model === 'string' ? q.model : ''
  filters.value.vendorName = typeof q.vendorName === 'string' ? q.vendorName : ''
  filters.value.purchaseOrderCode = typeof q.purchaseOrderCode === 'string' ? q.purchaseOrderCode : ''
  filters.value.salesOrderCode = typeof q.salesOrderCode === 'string' ? q.salesOrderCode : ''
}

const loadData = () => {
  loading.value = true
  logisticsApi.getQcs({
    model: filters.value.model || undefined,
    vendorName: filters.value.vendorName || undefined,
    purchaseOrderCode: filters.value.purchaseOrderCode || undefined,
    salesOrderCode: filters.value.salesOrderCode || undefined,
  })
    .then(res => { list.value = (res || []).sort((a, b) => (a.createTime < b.createTime ? 1 : -1)) })
    .finally(() => { loading.value = false })
}

watch(
  () => [route.name, route.query] as const,
  () => {
    syncFiltersFromRoute()
    if (route.name === 'QcList') loadData()
  },
  { deep: true, immediate: true }
)

/** 与左侧检索面板共用 URL query */
const handleSearch = () => {
  const query: Record<string, string> = {}
  const m = filters.value.model.trim()
  if (m) query.model = m
  const v = filters.value.vendorName.trim()
  if (v) query.vendorName = v
  const p = filters.value.purchaseOrderCode.trim()
  if (p) query.purchaseOrderCode = p
  const s = filters.value.salesOrderCode.trim()
  if (s) query.salesOrderCode = s
  router.replace({ name: 'QcList', query })
}

const resetFilters = () => {
  filters.value = {
    model: '',
    vendorName: '',
    purchaseOrderCode: '',
    salesOrderCode: '',
  }
  router.replace({ name: 'QcList', query: {} })
}

const goView = (row: QcInfoDto) => {
  router.push({ name: 'QcCreate', query: { qcId: row.id } })
}

const canCreateStockIn = (row: QcInfoDto) => row.status !== -1 && !row.stockInId

const resolveWarehouseId = async () => {
  const warehouses = await inventoryCenterApi.getWarehouses()
  if (!warehouses.length) return 'WH-DEFAULT'
  const preferred = warehouses.find(w => (w.warehouseCode || '').trim().toUpperCase() === 'WH-DEFAULT')
  return preferred?.id || warehouses[0].id || 'WH-DEFAULT'
}

const createStockIn = async (row: QcInfoDto) => {
  try {
    await ElMessageBox.confirm(
      t('qcList.messages.stockInConfirm', { code: row.qcCode }),
      t('qcList.messages.stockInTitle'),
      {
        confirmButtonText: t('qcList.messages.confirm'),
        cancelButtonText: t('common.cancel'),
        type: 'warning',
        distinguishCancelAndClose: true,
      }
    )
  } catch {
    return
  }

  const notices = await logisticsApi.getArrivalNotices()
  const notice = notices.find(x => x.id === row.stockInNotifyId)
  if (!notice) {
    ElMessage.error(t('qcList.messages.noticeMissing'))
    return
  }
  const buildMaterialCode = (x: { purchaseOrderItemId?: string; pn?: string }, idx: number) => {
    const code = (x.purchaseOrderItemId || x.pn || `MAT-${idx + 1}`).trim()
    // 后端 StockInItem.MaterialId 最大长度 36，避免超长导致 SaveChanges 失败
    return code.slice(0, 36)
  }

  const items = (notice.items || [])
    .filter(x => Number(x.passedQty || 0) > 0)
    .map((x, idx) => ({
      lineNo: idx + 1,
      materialCode: buildMaterialCode(x, idx),
      materialName: x.pn || t('qcList.messages.defaultMaterialName'),
      quantity: Number(x.passedQty || 0),
      unit: 'PCS',
      unitPrice: 0
    }))

  if (!items.length) {
    ElMessage.warning(t('qcList.messages.noPassingLines'))
    return
  }

  loading.value = true
  try {
    const warehouseId = await resolveWarehouseId()
    const uid = (authStore.user?.id || '').trim()
    const payload = {
      stockInCode: `SI${getYYMMDD(new Date())}${random4()}`,
      purchaseOrderId: notice.purchaseOrderId,
      vendorId: notice.vendorId,
      warehouseId,
      ...(uid && uid !== '0' ? { operatorId: uid } : {}),
      stockInDate: new Date().toISOString(),
      totalQuantity: Number(items.reduce((s, x) => s + Number(x.quantity || 0), 0).toFixed(4)),
      remark: t('qcList.messages.remarkFromQc', { code: row.qcCode }),
      items
    }
    const created = await stockInApi.create(payload as any)
    const stockInId = (created as any)?.id || ''
    if (!stockInId) {
      throw new Error(t('qcList.messages.createStockInFailedNoId'))
    }
    // 质检生成的入库单默认直接完成入库，触发库存中心过账
    await stockInApi.updateStatus(stockInId, 2)
    await logisticsApi.bindQcStockIn(row.id, stockInId)
    loadData()
    ElMessage.success(t('qcList.messages.successPosted'))
  } catch (error) {
    ElMessage.error(getApiErrorMessage(error, t('qcList.messages.stockInErrorFallback')))
  } finally {
    loading.value = false
  }
}

</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.qc-list-page {
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

// ---- 搜索栏（与 CustomerList / ArrivalNoticeList 一致）----
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

.search-input--plain {
  width: 200px;
  padding: 7px 12px;
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
</style>
