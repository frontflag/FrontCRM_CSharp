<template>
  <div class="stockin-list-page">
    <div class="page-header">
      <div class="header-left">
        <div class="page-title-group">
          <div class="page-icon">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
              <rect x="3" y="3" width="18" height="18" rx="2" ry="2" />
              <path d="M3 9h18" />
              <path d="M9 21V9" />
            </svg>
          </div>
          <h1 class="page-title">{{ t('stockInList.title') }}</h1>
        </div>
        <div class="count-badge">{{ t('stockInList.count', { count: filteredList.length }) }}</div>
      </div>
    </div>

    <!-- 查询栏（与客户列表一致的结构与样式） -->
    <div class="search-bar">
      <div class="search-left">
        <span class="list-title">{{ t('stockInList.filters.title') }}</span>
        <span class="filter-field-label">{{ t('stockInList.filters.materialModel') }}</span>
        <input
          v-model="filters.model"
          class="search-input search-input--filter"
          :placeholder="t('stockInList.filters.materialModelPlaceholder')"
          @keyup.enter="handleSearch"
        />
        <input
          v-model="filters.vendorName"
          class="search-input search-input--filter"
          :placeholder="t('stockInList.filters.vendorName')"
          @keyup.enter="handleSearch"
        />
        <input
          v-model="filters.purchaseOrderCode"
          class="search-input search-input--filter"
          :placeholder="t('stockInList.filters.purchaseOrderCode')"
          @keyup.enter="handleSearch"
        />
        <input
          v-model="filters.salesOrderCode"
          class="search-input search-input--filter"
          :placeholder="t('stockInList.filters.salesOrderCode')"
          @keyup.enter="handleSearch"
        />
        <button type="button" class="btn-primary btn-sm" @click="handleSearch">{{ t('stockInList.filters.search') }}</button>
        <button type="button" class="btn-ghost btn-sm" @click="resetFilters">{{ t('stockInList.filters.reset') }}</button>
      </div>
    </div>

    <CrmDataTable
      ref="dataTableRef"
      column-layout-key="stock-in-list-main"
      :columns="stockInTableColumns"
      :show-column-settings="false"
      :data="filteredList"
      v-loading="loading"
      @row-dblclick="handleView"
    >
      <template #col-stockInCode="{ row }">
        <span class="code-link" @click.stop="handleView(row)">{{ row.stockInCode }}</span>
      </template>
      <template #col-status="{ row }">
        <span :class="['status-badge', `status-${row.status}`]">{{ statusLabel(row.status) }}</span>
      </template>
      <template #col-materialModel="{ row }">{{ stockInMaterialModel(row) }}</template>
      <template #col-materialBrand="{ row }">{{ stockInMaterialBrand(row) }}</template>
      <template #col-warehouseName="{ row }">{{ warehouseNameOf(row.warehouseId) }}</template>
      <template #col-stockInDate="{ row }">
        <span class="text-secondary">{{ formatDate(row.stockInDate) }}</span>
      </template>
      <template #col-totalQuantity="{ row }">{{ formatNum(row.totalQuantity) }}</template>
      <template #col-totalAmount="{ row }">{{ formatMoney(row.totalAmount) }}</template>
      <template #col-createTime="{ row }">{{ formatDate((row as any).createTime || (row as any).createdAt) }}</template>
      <template #col-createUser="{ row }">{{ (row as any).createUserName || (row as any).createdBy || t('quoteList.na') }}</template>
      <template #col-actions-header>
        <div class="op-col-header">
          <span class="op-col-header-text">{{ t('stockInList.columns.actions') }}</span>
          <button type="button" class="op-col-toggle-btn" @click.stop="toggleOpCol">
            {{ opColExpanded ? '>' : '<' }}
          </button>
        </div>
      </template>

      <template #col-actions="{ row }">
        <div @click.stop @dblclick.stop>
          <div v-if="opColExpanded" class="action-btns">
            <button type="button" class="action-btn action-btn--info" @click.stop="handleEditRemark(row)">{{ t('stockInList.actions.editRemark') }}</button>
            <button
              v-if="row.status !== 2 && row.status !== 3"
              type="button"
              class="action-btn action-btn--warning"
              @click.stop="handleFinish(row)"
            >
              {{ t('stockInList.actions.markStockedIn') }}
            </button>
          </div>

          <el-dropdown v-else trigger="click" placement="bottom-end">
            <div class="op-more-dropdown-trigger">
              <button type="button" class="op-more-trigger">...</button>
            </div>
            <template #dropdown>
              <el-dropdown-menu>
                <el-dropdown-item @click.stop="handleEditRemark(row)">
                  <span class="op-more-item op-more-item--info">{{ t('stockInList.actions.editRemark') }}</span>
                </el-dropdown-item>
                <el-dropdown-item
                  v-if="row.status !== 2 && row.status !== 3"
                  @click.stop="handleFinish(row)"
                >
                  <span class="op-more-item op-more-item--warning">{{ t('stockInList.actions.markStockedIn') }}</span>
                </el-dropdown-item>
              </el-dropdown-menu>
            </template>
          </el-dropdown>
        </div>
      </template>
    </CrmDataTable>
    <div class="pagination-wrapper">
      <div class="list-footer-left">
        <el-tooltip :content="t('systemUser.colSetting')" placement="top" :hide-after="0">
          <el-button class="list-settings-btn" link type="primary" :aria-label="t('systemUser.colSetting')" @click="dataTableRef?.openColumnSettings?.()">
            <el-icon><Setting /></el-icon>
          </el-button>
        </el-tooltip>
        <div class="list-footer-spacer" aria-hidden="true"></div>
      </div>
    </div>

    <el-dialog v-model="remarkDialogVisible" :title="t('stockInList.actions.editRemark')" width="420px">
      <el-input v-model="remarkForm.remark" type="textarea" :rows="4" :placeholder="t('stockInList.remarkPlaceholder')" />
      <template #footer>
        <button class="btn-secondary" @click="remarkDialogVisible = false">{{ t('common.cancel') }}</button>
        <button class="btn-primary" @click="submitRemark">{{ t('common.confirm') }}</button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { computed, reactive, ref, watch } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import { Setting } from '@element-plus/icons-vue'
import { stockInApi, type StockInListItemDto } from '@/api/stockIn'
import { inventoryCenterApi, type WarehouseInfo } from '@/api/inventoryCenter'
import { formatDisplayDateTime } from '@/utils/displayDateTime'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'

const router = useRouter()
const route = useRoute()
const { t } = useI18n()
const loading = ref(false)
const list = ref<StockInListItemDto[]>([])
const warehouses = ref<WarehouseInfo[]>([])
const dataTableRef = ref<{ openColumnSettings?: () => void } | null>(null)

// 列表操作列：默认收起（Collapsed）
const opColExpanded = ref(false)
const OP_COL_COLLAPSED_WIDTH = 96
const OP_COL_EXPANDED_WIDTH = 160
const OP_COL_EXPANDED_MIN_WIDTH = 160
const opColWidth = computed(() => (opColExpanded.value ? OP_COL_EXPANDED_WIDTH : OP_COL_COLLAPSED_WIDTH))
const opColMinWidth = computed(() => (opColExpanded.value ? OP_COL_EXPANDED_MIN_WIDTH : OP_COL_COLLAPSED_WIDTH))
function toggleOpCol() {
  opColExpanded.value = !opColExpanded.value
}

const stockInTableColumns = computed<CrmTableColumnDef[]>(() => [
  { key: 'stockInCode', label: t('stockInList.columns.stockInCode'), prop: 'stockInCode', width: 160, minWidth: 160, showOverflowTooltip: true },
  { key: 'status', label: t('stockInList.columns.status'), prop: 'status', width: 110, align: 'center' },
  { key: 'sourceDisplayNo', label: t('stockInList.columns.sourceCode'), prop: 'sourceDisplayNo', width: 160, showOverflowTooltip: true },
  { key: 'materialModel', label: t('stockInList.columns.materialModel'), minWidth: 140, showOverflowTooltip: true },
  { key: 'materialBrand', label: t('stockInList.columns.brand'), minWidth: 120, showOverflowTooltip: true },
  { key: 'warehouseName', label: t('stockInList.columns.warehouse'), minWidth: 160, showOverflowTooltip: true },
  { key: 'vendorName', label: t('stockInList.columns.vendor'), prop: 'vendorName', minWidth: 160, showOverflowTooltip: true },
  { key: 'salesOrderCode', label: t('stockInList.columns.salesOrderCode'), prop: 'salesOrderCode', minWidth: 170, showOverflowTooltip: true },
  { key: 'stockInDate', label: t('stockInList.columns.stockInDate'), prop: 'stockInDate', width: 160 },
  { key: 'totalQuantity', label: t('stockInList.columns.totalQuantity'), prop: 'totalQuantity', width: 110, align: 'right' },
  { key: 'totalAmount', label: t('stockInList.columns.totalAmount'), prop: 'totalAmount', width: 110, align: 'right' },
  { key: 'remark', label: t('stockInList.columns.remark'), prop: 'remark', minWidth: 160, showOverflowTooltip: true },
  { key: 'createTime', label: t('stockInList.columns.createTime'), width: 160 },
  { key: 'createUser', label: t('stockInList.columns.createUser'), width: 120, showOverflowTooltip: true },
  {
    key: 'actions',
    label: t('stockInList.columns.actions'),
    width: opColWidth.value,
    minWidth: opColMinWidth.value,
    fixed: 'right',
    hideable: false,
    pinned: 'end',
    reorderable: false,
    className: 'op-col',
    labelClassName: 'op-col'
  }
])

const warehouseNameOf = (warehouseId?: string) => {
  if (!warehouseId) return t('quoteList.na')
  const byId = warehouses.value.find(w => w.id === warehouseId)
  if (byId?.warehouseName) return byId.warehouseName
  const byCode = warehouses.value.find(w => (w.warehouseCode || '').trim() === warehouseId.trim())
  return byCode?.warehouseName || warehouseId
}
const filters = reactive({
  model: '',
  vendorName: '',
  purchaseOrderCode: '',
  salesOrderCode: ''
})

const remarkDialogVisible = ref(false)
const remarkForm = reactive<{ id: string; remark: string }>({
  id: '',
  remark: ''
})

const formatNum = (v: number) => (v == null ? t('quoteList.na') : Number(v).toLocaleString())
const formatMoney = (v: number) => (v == null ? t('quoteList.na') : Number(v).toFixed(2))
const formatDate = (v?: string) => formatDisplayDateTime(v)

function pickRowStr(row: Record<string, unknown>, camel: string, pascal: string): string {
  const v = row[camel] ?? row[pascal]
  return typeof v === 'string' ? v.trim() : ''
}

const stockInMaterialModel = (row: StockInListItemDto) => {
  const r = row as unknown as Record<string, unknown>
  const s = pickRowStr(r, 'materialModelSummary', 'MaterialModelSummary')
  return s || t('quoteList.na')
}

const stockInMaterialBrand = (row: StockInListItemDto) => {
  const r = row as unknown as Record<string, unknown>
  const s = pickRowStr(r, 'materialBrandSummary', 'MaterialBrandSummary')
  return s || t('quoteList.na')
}

const statusLabel = (s: number) => {
  switch (s) {
    case 0: return t('stockInList.status.draft')
    case 1: return t('stockInList.status.pending')
    case 2: return t('stockInList.status.done')
    case 3: return t('stockInList.status.cancelled')
    default: return t('rfqDetail.unknown')
  }
}

function syncFiltersFromRoute() {
  if (route.name !== 'StockInList') return
  const q = route.query
  filters.model = typeof q.model === 'string' ? q.model : ''
  filters.vendorName = typeof q.vendorName === 'string' ? q.vendorName : ''
  filters.purchaseOrderCode = typeof q.purchaseOrderCode === 'string' ? q.purchaseOrderCode : ''
  filters.salesOrderCode = typeof q.salesOrderCode === 'string' ? q.salesOrderCode : ''
}

const fetchList = async () => {
  loading.value = true
  try {
    if (!warehouses.value.length) {
      try {
        warehouses.value = await inventoryCenterApi.getWarehouses()
      } catch {
        warehouses.value = []
      }
    }
    list.value = await stockInApi.getAll({
      model: filters.model || undefined,
      vendorName: filters.vendorName || undefined,
      purchaseOrderCode: filters.purchaseOrderCode || undefined,
      salesOrderCode: filters.salesOrderCode || undefined
    })
  } catch (e) {
    console.error(e)
    ElMessage.error(t('stockInList.messages.loadFailed'))
  } finally {
    loading.value = false
  }
}

watch(
  () => [route.name, route.query] as const,
  () => {
    syncFiltersFromRoute()
    if (route.name === 'StockInList') fetchList()
  },
  { deep: true, immediate: true }
)

/** 与左侧检索面板共用 URL query */
const handleSearch = () => {
  const query: Record<string, string> = {}
  const m = filters.model.trim()
  if (m) query.model = m
  const v = filters.vendorName.trim()
  if (v) query.vendorName = v
  const p = filters.purchaseOrderCode.trim()
  if (p) query.purchaseOrderCode = p
  const s = filters.salesOrderCode.trim()
  if (s) query.salesOrderCode = s
  router.replace({ name: 'StockInList', query })
}

const keywordHit = (text: string | undefined, keyword: string): boolean => {
  if (!keyword) return true
  return (text ?? '').toLowerCase().includes(keyword.toLowerCase())
}

// 前端兜底过滤：避免后端筛选偶发不生效时页面无响应
const filteredList = computed(() => {
  const model = filters.model.trim()
  const vendorName = filters.vendorName.trim()
  const purchaseOrderCode = filters.purchaseOrderCode.trim()
  const salesOrderCode = filters.salesOrderCode.trim()

  return list.value.filter((row) => {
    const rowAny = row as any
    const modelText = `${rowAny.materialModelSummary ?? rowAny.MaterialModelSummary ?? ''} ${rowAny.materialBrandSummary ?? rowAny.MaterialBrandSummary ?? ''} ${rowAny.model ?? ''} ${rowAny.materialCode ?? ''} ${rowAny.remark ?? ''}`
    const poText = `${row.sourceDisplayNo ?? ''} ${row.stockInCode ?? ''}`
    return (
      keywordHit(modelText, model) &&
      keywordHit(row.vendorName, vendorName) &&
      keywordHit(poText, purchaseOrderCode) &&
      keywordHit(row.salesOrderCode, salesOrderCode)
    )
  })
})

const resetFilters = () => {
  filters.model = ''
  filters.vendorName = ''
  filters.purchaseOrderCode = ''
  filters.salesOrderCode = ''
  router.replace({ name: 'StockInList', query: {} })
}

const handleView = (row: StockInListItemDto) => {
  // 暂时直接进入编辑页查看
  router.push(`/inventory/stock-in/${row.id}`)
}

const handleEditRemark = (row: StockInListItemDto) => {
  remarkForm.id = row.id
  remarkForm.remark = row.remark || ''
  remarkDialogVisible.value = true
}

const submitRemark = async () => {
  try {
    await stockInApi.update(remarkForm.id, { remark: remarkForm.remark })
    ElMessage.success(t('stockInList.messages.remarkUpdated'))
    remarkDialogVisible.value = false
    fetchList()
  } catch (e) {
    console.error(e)
    ElMessage.error(t('stockInList.messages.remarkUpdateFailed'))
  }
}

const handleFinish = async (row: StockInListItemDto) => {
  try {
    await stockInApi.updateStatus(row.id, 2)
    ElMessage.success(t('stockInList.messages.markDoneSuccess'))
    fetchList()
  } catch (e) {
    console.error(e)
    ElMessage.error(t('stockInList.messages.updateStatusFailed'))
  }
}

</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.stockin-list-page {
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
  .header-left { display: flex; align-items: center; gap: 12px; }
}
.page-title-group {
  display: flex;
  align-items: center;
  gap: 10px;
  .page-icon {
    width: 36px;
    height: 36px;
    background: rgba(0, 212, 255, 0.1);
    border: 1px solid rgba(0, 212, 255, 0.25);
    border-radius: 10px;
    display: flex;
    align-items: center;
    justify-content: center;
    color: $cyan-primary;
  }
  .page-title { font-size: 20px; font-weight: 600; color: $text-primary; margin: 0; }
}
.count-badge {
  font-size: 12px;
  color: $text-muted;
  background: rgba(255, 255, 255, 0.05);
  border: 1px solid $border-panel;
  border-radius: 20px;
  padding: 3px 10px;
}

// ---- 查询栏（对齐客户列表 CustomerList.vue）----
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

.list-title {
  font-size: 14px;
  font-weight: 600;
  color: $text-primary;
  white-space: nowrap;
}

.filter-field-label {
  font-size: 12px;
  font-weight: 500;
  color: $text-muted;
  white-space: nowrap;
}

.search-input {
  width: 220px;
  padding: 7px 12px 7px 12px;
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

  &--filter {
    width: 160px;
  }
}

.btn-primary,
.btn-secondary {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 8px 14px;
  border-radius: $border-radius-md;
  font-size: 13px;
  cursor: pointer;
  border: 1px solid transparent;
  font-family: 'Noto Sans SC', sans-serif;
  transition: all 0.2s;
}
.btn-primary {
  background: linear-gradient(135deg, rgba(0, 102, 255, 0.8), rgba(0, 212, 255, 0.7));
  border-color: rgba(0, 212, 255, 0.4);
  color: #fff;
  letter-spacing: 0.5px;

  &:hover {
    transform: translateY(-1px);
    box-shadow: 0 4px 16px rgba(0, 212, 255, 0.25);
  }

  &.btn-sm {
    padding: 6px 12px;
    font-size: 12px;
  }
}
.btn-secondary {
  background: rgba(255, 255, 255, 0.05);
  border-color: $border-panel;
  color: $text-secondary;
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

  &:hover {
    border-color: rgba(0, 212, 255, 0.3);
    color: $text-secondary;
  }

  &.btn-sm {
    padding: 6px 12px;
    font-size: 12px;
  }
}
.code-link {
  color: $cyan-primary;
  cursor: pointer;
  &:hover { text-decoration: underline; }
}
.text-secondary { color: $text-muted; }
.status-badge {
  display: inline-block;
  padding: 2px 8px;
  border-radius: 4px;
  font-size: 12px;
  &.status-0 { background: rgba(255,255,255,0.05); color: $text-muted; }
  &.status-1 { background: rgba(255,193,7,0.15); color: #ffc107; }
  &.status-2 { background: rgba(70,191,145,0.18); color: #46BF91; }
  &.status-3 { background: rgba(201,87,69,0.18); color: #C95745; }
}
.action-btn {
  background: transparent;
  border: none;
  color: $cyan-primary;
  cursor: pointer;
  font-size: 12px;
  padding: 2px 6px;
  margin-right: 4px;
  white-space: nowrap;
  flex-shrink: 0;
  &:hover { text-decoration: underline; }
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

