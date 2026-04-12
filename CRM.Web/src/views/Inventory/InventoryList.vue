<template>
  <div class="inventory-list-page">
    <div class="page-header">
      <div class="header-left">
        <div class="page-title-group">
          <div class="page-icon">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
              <path d="M21 16V8a2 2 0 00-1-1.73l-7-4a2 2 0 00-2 0l-7 4A2 2 0 003 8v8a2 2 0 001 1.73l7 4a2 2 0 002 0l7-4A2 2 0 0021 16z"/>
              <polyline points="3.27 6.96 12 12.01 20.73 6.96"/>
              <line x1="12" y1="22.08" x2="12" y2="12"/>
            </svg>
          </div>
          <h1 class="page-title">{{ t('inventoryList.title') }}</h1>
        </div>
        <div class="count-badge">{{ t('inventoryList.count', { count: filteredInventoryList.length }) }}</div>
      </div>
      <div class="header-right">
        <button class="btn-primary" type="button" @click="openWarehouseDialog">
          {{ t('inventoryList.actions.warehouseManagement') }}
        </button>
      </div>
    </div>

    <div class="stat-row" v-if="finance">
      <div class="stat-card">
        <div class="label">{{ t('inventoryList.stats.capitalOccupied') }}</div>
        <div class="value">{{ formatMoney(finance.inventoryCapital) }}</div>
      </div>
      <div class="stat-card">
        <div class="label">{{ t('inventoryList.stats.monthlyOutCost') }}</div>
        <div class="value">{{ formatMoney(finance.monthlyOutCost) }}</div>
      </div>
      <div class="stat-card">
        <div class="label">{{ t('inventoryList.stats.turnoverDays') }}</div>
        <div class="value">{{ finance.turnoverDays?.toFixed(2) || '0.00' }}</div>
      </div>
      <div class="stat-card">
        <div class="label">{{ t('inventoryList.stats.stagnantCount') }}</div>
        <div class="value">{{ finance.stagnantMaterialCount }}</div>
      </div>
    </div>

    <!-- 搜索栏：与客户列表 search-bar / search-left / status-select 一致 -->
    <div class="search-bar">
      <div class="search-left">
        <el-select
          v-model="stockTypeFilter"
          :placeholder="t('inventoryList.filters.allOrderTypes')"
          clearable
          :filterable="false"
          class="status-select status-select--inv-order"
          :teleported="false"
          @change="fetchList"
        >
          <el-option :label="t('inventoryList.stockTypes.customer')" :value="1" />
          <el-option :label="t('inventoryList.stockTypes.stocking')" :value="2" />
          <el-option :label="t('inventoryList.stockTypes.sample')" :value="3" />
        </el-select>
        <el-select
          v-model="warehouseFilter"
          :placeholder="t('inventoryList.filters.allInventoryCodes')"
          clearable
          :filterable="false"
          class="status-select status-select--inv-warehouse"
          :teleported="false"
          @change="fetchList"
        >
          <el-option
            v-for="opt in warehouseSelectOptions"
            :key="opt.value"
            :label="opt.label"
            :value="opt.value"
          />
        </el-select>
        <button type="button" class="btn-primary btn-sm" @click="fetchList">
          {{ t('inventoryList.filters.search') }}
        </button>
        <button type="button" class="btn-ghost btn-sm" @click="resetInventorySearch">
          {{ t('inventoryList.filters.reset') }}
        </button>
      </div>
    </div>

    <CrmDataTable
      ref="dataTableRef"
      column-layout-key="inventory-list-main-v5"
      :columns="inventoryTableColumns"
      :show-column-settings="false"
      :density-toggle-anchor-el="rowDensityToggleAnchorEl"
      :data="filteredInventoryList"
      v-loading="loading"
      @row-dblclick="onRowDblclick"
    >
      <template #col-stockCode="{ row }">{{ stockCodeDisplay(row) }}</template>
      <template #col-materialModel="{ row }">{{ materialModelDisplay(row) }}</template>
      <template #col-materialBrand="{ row }">{{ materialBrandDisplay(row) }}</template>
      <template #col-warehouseName="{ row }">{{ warehouseNameOf(row.warehouseId) }}</template>
      <template #col-region="{ row }">{{ regionLabel(row) }}</template>
      <template #col-stockType="{ row }">
        <span
          class="inv-stock-type-cell"
          :class="{ 'inv-stock-type-cell--stocking': rowStockTypeNum(row) === 2 }"
        >
          <span>{{ stockTypeLabel(row) }}</span>
          <el-icon v-if="rowStockTypeNum(row) === 2" class="inv-stock-type-icon" aria-hidden="true">
            <Box />
          </el-icon>
        </span>
      </template>
      <template #col-onHandQty="{ row }">{{ formatNum(row.onHandQty) }}</template>
      <template #col-availableQty="{ row }">{{ formatNum(row.availableQty) }}</template>
      <template #col-lockedQty="{ row }">{{ formatNum(row.lockedQty) }}</template>
      <template #col-inventoryAmount="{ row }">{{ formatInventoryAmount(row) }}</template>
      <template #col-lastMoveTime="{ row }">{{ formatTime(row.lastMoveTime) }}</template>
      <template #col-createTime="{ row }">{{ formatTime((row as any).createTime || (row as any).createdAt) }}</template>
      <template #col-createUser="{ row }">{{ (row as any).createUserName || (row as any).createdBy || '--' }}</template>
      <template #col-actions-header>
        <div class="op-col-header">
          <span class="op-col-header-text">{{ t('inventoryList.columns.actions') }}</span>
          <button type="button" class="op-col-toggle-btn" @click.stop="toggleOpColMain">
            {{ opColMainExpanded ? '>' : '<' }}
          </button>
        </div>
      </template>
      <template #col-actions="{ row }">
        <div @click.stop @dblclick.stop>
          <div v-if="opColMainExpanded" class="action-btns">
            <button type="button" class="action-btn action-btn--info" @click.stop="openTrace(row.materialId)">{{ t('inventoryList.actions.trace') }}</button>
          </div>

          <el-dropdown v-else trigger="click" placement="bottom-end">
            <div class="op-more-dropdown-trigger">
              <button type="button" class="op-more-trigger">...</button>
            </div>
            <template #dropdown>
              <el-dropdown-menu>
                <el-dropdown-item @click.stop="openTrace(row.materialId)">
                  <span class="op-more-item op-more-item--info">{{ t('inventoryList.actions.trace') }}</span>
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
        <span ref="rowDensityToggleAnchorEl" class="list-footer-density-anchor" aria-hidden="true" />
        <div class="list-footer-spacer" aria-hidden="true"></div>
      </div>
    </div>

    <el-dialog v-model="warehouseVisible" :title="t('inventoryList.warehouse.title')" width="720px">
      <el-form :model="warehouseForm" inline>
        <el-form-item :label="t('inventoryList.warehouse.code')">
          <el-input v-model="warehouseForm.warehouseCode" />
        </el-form-item>
        <el-form-item :label="t('inventoryList.warehouse.name')">
          <el-input v-model="warehouseForm.warehouseName" />
        </el-form-item>
        <el-form-item :label="t('inventoryList.warehouse.address')">
          <el-input v-model="warehouseForm.address" />
        </el-form-item>
        <el-form-item :label="t('inventoryList.warehouse.regionType')">
          <el-select v-model="warehouseForm.regionType" style="width: 200px" :teleported="false">
            <el-option :value="REGION_TYPE_DOMESTIC" :label="t('inventoryList.warehouse.regionDomestic')" />
            <el-option :value="REGION_TYPE_OVERSEAS" :label="t('inventoryList.warehouse.regionOverseas')" />
          </el-select>
        </el-form-item>
        <el-form-item>
          <button class="btn-primary" type="button" @click="saveWarehouse">
            {{ warehouseForm.id ? t('inventoryList.warehouse.saveEdit') : t('inventoryList.warehouse.saveNew') }}
          </button>
          <button class="btn-secondary" type="button" style="margin-left: 8px" @click="resetWarehouseForm">{{ t('inventoryList.warehouse.new') }}</button>
        </el-form-item>
      </el-form>
      <el-table :data="warehouses" class="warehouse-table">
        <el-table-column prop="warehouseCode" :label="t('inventoryList.warehouse.codeShort')" width="140" />
        <el-table-column prop="warehouseName" :label="t('inventoryList.warehouse.nameShort')" width="180" />
        <el-table-column :label="t('inventoryList.warehouse.regionTypeShort')" width="100" align="center">
          <template #default="{ row }">{{ warehouseRegionTypeLabel(row) }}</template>
        </el-table-column>
        <el-table-column prop="address" :label="t('inventoryList.warehouse.address')" min-width="200" />
        <el-table-column
          :label="t('inventoryList.columns.actions')"
          :width="opColWarehouseWidth"
          :min-width="opColWarehouseMinWidth"
          align="center"
          fixed="right"
          class-name="op-col"
          label-class-name="op-col"
        >
          <template #header>
            <div class="op-col-header">
              <span class="op-col-header-text">{{ t('inventoryList.columns.actions') }}</span>
              <button type="button" class="op-col-toggle-btn" @click.stop="toggleOpColWarehouse">
                {{ opColWarehouseExpanded ? '>' : '<' }}
              </button>
            </div>
          </template>
          <template #default="{ row }">
            <div @click.stop @dblclick.stop>
              <div v-if="opColWarehouseExpanded" class="action-btns">
                <button type="button" class="action-btn action-btn--primary" @click.stop="loadWarehouseForEdit(row)">{{ t('inventoryList.actions.edit') }}</button>
              </div>

              <el-dropdown v-else trigger="click" placement="bottom-end">
                <div class="op-more-dropdown-trigger">
                  <button type="button" class="op-more-trigger">...</button>
                </div>
                <template #dropdown>
                  <el-dropdown-menu>
                    <el-dropdown-item @click.stop="loadWarehouseForEdit(row)">
                      <span class="op-more-item op-more-item--primary">{{ t('inventoryList.actions.edit') }}</span>
                    </el-dropdown-item>
                  </el-dropdown-menu>
                </template>
              </el-dropdown>
            </div>
          </template>
        </el-table-column>
      </el-table>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import { Box, Setting } from '@element-plus/icons-vue'
import { inventoryCenterApi, type FinanceSummary, type InventoryOverview, type WarehouseInfo } from '@/api/inventoryCenter'
import { REGION_TYPE_DOMESTIC, REGION_TYPE_OVERSEAS, normalizeRegionType } from '@/constants/regionType'
import { CURRENCY_CODE_TO_TEXT } from '@/constants/currency'
import { getApiErrorMessage } from '@/utils/apiError'
import { formatDisplayDateTime } from '@/utils/displayDateTime'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'

const router = useRouter()
const { t } = useI18n()
const loading = ref(false)
const list = ref<InventoryOverview[]>([])
/** 库存类型 1/2/3，空为全部（仅前端筛选当前已加载总览） */
const stockTypeFilter = ref<number | undefined>(undefined)
const dataTableRef = ref<{ openColumnSettings?: () => void } | null>(null)
const rowDensityToggleAnchorEl = ref<HTMLElement | null>(null)
/** 筛选总览用的仓库主键或编码（与 stock.warehouseId 一致） */
const warehouseFilter = ref<string | undefined>(undefined)
const finance = ref<FinanceSummary | null>(null)
const warehouseVisible = ref(false)
const warehouses = ref<WarehouseInfo[]>([])

// 列表操作列：主表默认收起（Collapsed）
const opColMainExpanded = ref(false)
const OP_COL_MAIN_COLLAPSED_WIDTH = 96
const OP_COL_MAIN_EXPANDED_WIDTH = 100
const OP_COL_MAIN_EXPANDED_MIN_WIDTH = 100
const opColMainWidth = computed(() => (opColMainExpanded.value ? OP_COL_MAIN_EXPANDED_WIDTH : OP_COL_MAIN_COLLAPSED_WIDTH))
const opColMainMinWidth = computed(() =>
  opColMainExpanded.value ? OP_COL_MAIN_EXPANDED_MIN_WIDTH : OP_COL_MAIN_COLLAPSED_WIDTH
)
function toggleOpColMain() {
  opColMainExpanded.value = !opColMainExpanded.value
}

const inventoryTableColumns = computed<CrmTableColumnDef[]>(() => [
  { key: 'stockType', label: t('inventoryList.columns.stockType'), width: 138, showOverflowTooltip: true },
  { key: 'materialModel', label: t('inventoryList.columns.materialModel'), minWidth: 160, showOverflowTooltip: true },
  { key: 'materialBrand', label: t('inventoryList.columns.brand'), minWidth: 120, showOverflowTooltip: true },
  { key: 'onHandQty', label: t('inventoryList.columns.onHandQty'), prop: 'onHandQty', width: 110, align: 'right' },
  { key: 'availableQty', label: t('inventoryList.columns.availableQty'), prop: 'availableQty', width: 110, align: 'right' },
  { key: 'lockedQty', label: t('inventoryList.columns.lockedQty'), prop: 'lockedQty', width: 110, align: 'right' },
  { key: 'inventoryAmount', label: t('inventoryList.columns.inventoryAmount'), prop: 'inventoryAmount', width: 120, align: 'right' },
  { key: 'warehouseName', label: t('inventoryList.columns.warehouseName'), width: 160, showOverflowTooltip: true },
  { key: 'region', label: t('inventoryList.columns.region'), width: 88, align: 'center', showOverflowTooltip: true },
  { key: 'lastMoveTime', label: t('inventoryList.columns.lastMoveTime'), prop: 'lastMoveTime', width: 170 },
  { key: 'stockCode', label: t('inventoryList.columns.stockCode'), width: 132, showOverflowTooltip: true },
  { key: 'createTime', label: t('inventoryList.columns.createTime'), width: 160 },
  { key: 'createUser', label: t('inventoryList.columns.createUser'), width: 120, showOverflowTooltip: true },
  {
    key: 'actions',
    label: t('inventoryList.columns.actions'),
    width: opColMainWidth.value,
    minWidth: opColMainMinWidth.value,
    fixed: 'right',
    hideable: false,
    pinned: 'end',
    reorderable: false,
    className: 'op-col',
    labelClassName: 'op-col'
  }
])

// 列表操作列：弹窗表格默认收起（Collapsed）
const opColWarehouseExpanded = ref(false)
const OP_COL_WAREHOUSE_COLLAPSED_WIDTH = 96
const OP_COL_WAREHOUSE_EXPANDED_WIDTH = 110
const OP_COL_WAREHOUSE_EXPANDED_MIN_WIDTH = 110
const opColWarehouseWidth = computed(() =>
  opColWarehouseExpanded.value ? OP_COL_WAREHOUSE_EXPANDED_WIDTH : OP_COL_WAREHOUSE_COLLAPSED_WIDTH
)
const opColWarehouseMinWidth = computed(() =>
  opColWarehouseExpanded.value ? OP_COL_WAREHOUSE_EXPANDED_MIN_WIDTH : OP_COL_WAREHOUSE_COLLAPSED_WIDTH
)
function toggleOpColWarehouse() {
  opColWarehouseExpanded.value = !opColWarehouseExpanded.value
}
const emptyWarehouseForm = (): WarehouseInfo => ({
  warehouseCode: '',
  warehouseName: '',
  address: '',
  regionType: REGION_TYPE_DOMESTIC,
  status: 1
})

const warehouseForm = ref<WarehouseInfo>(emptyWarehouseForm())

/** 兼容接口 camelCase / PascalCase */
function normalizeWarehouseRow(row: WarehouseInfo): WarehouseInfo {
  const r = row as unknown as Record<string, unknown>
  const idRaw = r.id ?? r.Id
  const id = typeof idRaw === 'string' && idRaw.trim() ? idRaw.trim() : undefined
  const code = String(r.warehouseCode ?? r.WarehouseCode ?? '').trim()
  const name = String(r.warehouseName ?? r.WarehouseName ?? '').trim()
  const addr = String(r.address ?? r.Address ?? '')
  const st = r.status ?? r.Status
  const status = typeof st === 'number' ? st : 1
  const regionType = normalizeRegionType(r.regionType ?? r.RegionType)
  return { id, warehouseCode: code, warehouseName: name, address: addr, regionType, status }
}

function warehouseRegionTypeLabel(row: WarehouseInfo): string {
  const n = normalizeWarehouseRow(row).regionType
  if (n === REGION_TYPE_OVERSEAS) return t('inventoryList.warehouse.regionOverseas')
  return t('inventoryList.warehouse.regionDomestic')
}

const resetWarehouseForm = () => {
  warehouseForm.value = emptyWarehouseForm()
}

const loadWarehouseForEdit = (row: WarehouseInfo) => {
  warehouseForm.value = normalizeWarehouseRow(row)
}

/** 库存编号下拉：仓库编码 + 名称，值为 id 优先否则编码 */
const warehouseSelectOptions = computed(() => {
  const rows = warehouses.value.map(normalizeWarehouseRow)
  const opts = rows
    .map((n) => {
      const value = (n.id?.trim() || n.warehouseCode || '').trim()
      if (!value) return null
      const code = n.warehouseCode?.trim()
      const name = n.warehouseName?.trim()
      const label =
        code && name ? `${code} · ${name}` : code || name || value
      return { value, label }
    })
    .filter((x): x is { value: string; label: string } => x != null)
  const byVal = new Map<string, { value: string; label: string }>()
  for (const o of opts) {
    if (!byVal.has(o.value)) byVal.set(o.value, o)
  }
  return [...byVal.values()].sort((a, b) => a.label.localeCompare(b.label, 'zh-CN'))
})

function rowStockTypeNum(row: InventoryOverview): number {
  const r = row as unknown as Record<string, unknown>
  const n = Number(r.stockType ?? r.StockType ?? 1)
  return n >= 1 && n <= 3 ? n : 1
}

const stockTypeLabel = (row: InventoryOverview) => {
  const n = rowStockTypeNum(row)
  if (n === 2) return t('inventoryList.stockTypes.stocking')
  if (n === 3) return t('inventoryList.stockTypes.sample')
  if (n === 1) return t('inventoryList.stockTypes.customer')
  return t('inventoryList.stockTypes.unknown')
}

const filteredInventoryList = computed(() => {
  const rows = list.value
  const ft = stockTypeFilter.value
  if (ft === undefined || ft === null) return rows
  return rows.filter(r => rowStockTypeNum(r) === ft)
})

function resetInventorySearch() {
  stockTypeFilter.value = undefined
  warehouseFilter.value = undefined
  void fetchList()
}

const formatNum = (v: number) => (v == null ? t('quoteList.na') : Number(v).toLocaleString())
const formatMoney = (v: number) => (v == null ? t('quoteList.na') : Number(v).toFixed(2))

/** 库存金额：币别代码 + 金额（币别来自最近一次采购入库关联采购单） */
const formatInventoryAmount = (row: InventoryOverview) => {
  const v = row.inventoryAmount
  if (v == null) return t('quoteList.na')
  const r = row as unknown as Record<string, unknown>
  const codeNum = Number(r.currency ?? r.Currency ?? 1)
  const iso = CURRENCY_CODE_TO_TEXT[Number.isFinite(codeNum) ? codeNum : 1] ?? 'RMB'
  const amt = Number(v).toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
  return `${iso} ${amt}`
}
const formatTime = (v?: string) => formatDisplayDateTime(v)
/** 列表「地域」：stock.RegionType（接口 camelCase / PascalCase） */
const regionLabel = (row: InventoryOverview) => {
  const r = row as unknown as Record<string, unknown>
  const n = normalizeRegionType(r.regionType ?? r.RegionType)
  return n === REGION_TYPE_OVERSEAS ? t('inventoryList.warehouse.regionOverseas') : t('inventoryList.warehouse.regionDomestic')
}

const warehouseNameOf = (warehouseId?: string) => {
  if (!warehouseId) return t('quoteList.na')
  const byId = warehouses.value.find(w => normalizeWarehouseRow(w).id === warehouseId)
  if (byId) {
    const n = normalizeWarehouseRow(byId)
    if (n.warehouseName) return n.warehouseName
  }
  const byCode = warehouses.value.find(w => normalizeWarehouseRow(w).warehouseCode === warehouseId.trim())
  if (byCode) {
    const n = normalizeWarehouseRow(byCode)
    if (n.warehouseName) return n.warehouseName
  }
  return warehouseId
}

function pickRowStr(row: Record<string, unknown>, camel: string, pascal: string): string {
  const v = row[camel] ?? row[pascal]
  return typeof v === 'string' ? v : ''
}

const stockCodeDisplay = (row: InventoryOverview) => {
  const r = row as unknown as Record<string, unknown>
  const code = pickRowStr(r, 'stockCode', 'StockCode').trim()
  return code || '--'
}

/** 规格型号；兼容 PascalCase；无型号时回退物料 ID */
const materialModelDisplay = (row: InventoryOverview) => {
  const r = row as unknown as Record<string, unknown>
  const model = pickRowStr(r, 'materialModel', 'MaterialModel').trim()
  const id = pickRowStr(r, 'materialId', 'MaterialId').trim()
  return model || id || t('quoteList.na')
}

/** 品牌（接口 materialName：优先 stock 冗余 purchase_brand）；兼容 PascalCase */
const materialBrandDisplay = (row: InventoryOverview) => {
  const r = row as unknown as Record<string, unknown>
  const name = pickRowStr(r, 'materialName', 'MaterialName').trim()
  return name || t('quoteList.na')
}

/** 最后移动时间降序；无时间排后 */
const sortByLastMoveDesc = (rows: InventoryOverview[]) =>
  [...rows].sort((a, b) => {
    const ta = a.lastMoveTime ? new Date(a.lastMoveTime).getTime() : null
    const tb = b.lastMoveTime ? new Date(b.lastMoveTime).getTime() : null
    if (ta == null && tb == null) {
      return pickRowStr(a as unknown as Record<string, unknown>, 'warehouseId', 'WarehouseId').localeCompare(
        pickRowStr(b as unknown as Record<string, unknown>, 'warehouseId', 'WarehouseId')
      )
    }
    if (ta == null) return 1
    if (tb == null) return -1
    if (tb !== ta) return tb - ta
    return pickRowStr(a as unknown as Record<string, unknown>, 'warehouseId', 'WarehouseId').localeCompare(
      pickRowStr(b as unknown as Record<string, unknown>, 'warehouseId', 'WarehouseId')
    )
  })

const fetchList = async () => {
  loading.value = true
  try {
    const [overviewRes, summaryRes, warehouseRes] = await Promise.allSettled([
      inventoryCenterApi.getOverview(warehouseFilter.value?.trim() || undefined),
      inventoryCenterApi.getFinanceSummary(),
      inventoryCenterApi.getWarehouses()
    ])

    if (overviewRes.status === 'fulfilled') {
      list.value = sortByLastMoveDesc(overviewRes.value)
    } else {
      list.value = []
      ElMessage.error(getApiErrorMessage(overviewRes.reason, t('inventoryList.messages.loadOverviewFailed')))
    }

    if (summaryRes.status === 'fulfilled') {
      finance.value = summaryRes.value
    } else {
      finance.value = null
      ElMessage.warning(getApiErrorMessage(summaryRes.reason, t('inventoryList.messages.loadFinanceFailed')))
    }

    if (warehouseRes.status === 'fulfilled') {
      warehouses.value = warehouseRes.value
    }
  } catch (e) {
    console.error(e)
    ElMessage.error(getApiErrorMessage(e, t('inventoryList.messages.loadCenterFailed')))
    list.value = []
  } finally {
    loading.value = false
  }
}

const openTrace = (materialId: string) => {
  router.push(`/inventory/traces/${encodeURIComponent(materialId)}`)
}

const onRowDblclick = (row: InventoryOverview) => openTrace(row.materialId)

const openWarehouseDialog = async () => {
  try {
    resetWarehouseForm()
    warehouseVisible.value = true
    warehouses.value = await inventoryCenterApi.getWarehouses()
  } catch (e) {
    console.error(e)
    ElMessage.error(getApiErrorMessage(e, t('inventoryList.messages.loadWarehouseFailed')))
  }
}

const saveWarehouse = async () => {
  try {
    const form = warehouseForm.value
    const trimmedId = form.id?.trim()
    const shouldSendId =
      !!trimmedId && warehouses.value.some(w => normalizeWarehouseRow(w).id === trimmedId)
    const rt = normalizeRegionType(form.regionType)
    const payload: WarehouseInfo = shouldSendId
      ? {
          id: trimmedId,
          warehouseCode: form.warehouseCode.trim(),
          warehouseName: form.warehouseName.trim(),
          address: form.address?.trim() || undefined,
          regionType: rt,
          status: form.status ?? 1
        }
      : {
          warehouseCode: form.warehouseCode.trim(),
          warehouseName: form.warehouseName.trim(),
          address: form.address?.trim() || undefined,
          regionType: rt,
          status: form.status ?? 1
        }

    await inventoryCenterApi.saveWarehouse(payload)
    ElMessage.success(t('inventoryList.messages.saveWarehouseSuccess'))
    resetWarehouseForm()
    warehouses.value = await inventoryCenterApi.getWarehouses()
  } catch (e) {
    console.error(e)
    ElMessage.error(getApiErrorMessage(e, t('inventoryList.messages.saveWarehouseFailed')))
  }
}

onMounted(() => fetchList())
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.inventory-list-page {
  padding: 24px;
  min-height: 100%;
  background: $layer-1;
  font-family: 'Noto Sans SC', sans-serif;
}
.page-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  margin-bottom: 20px;
  .header-left { display: flex; align-items: center; gap: 12px; }
  .header-right {
    display: flex;
    align-items: center;
    flex-shrink: 0;
  }
}

// ---- 搜索栏（与客户列表 CustomerList.vue 一致）----
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

.status-select {
  width: 120px;
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

.status-select--inv-order {
  width: 148px;
}

.status-select--inv-warehouse {
  width: 220px;
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

.inv-stock-type-cell {
  display: inline-flex;
  align-items: center;
  gap: 6px;
}
.inv-stock-type-cell--stocking {
  color: #ffc107;
  font-weight: 600;
}
.inv-stock-type-icon {
  font-size: 16px;
  flex-shrink: 0;
}

.btn-primary,
.btn-secondary {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 8px 14px;
  border-radius: $border-radius-md;
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  border: 1px solid transparent;
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
.btn-secondary {
  background: rgba(255, 255, 255, 0.05);
  border-color: $border-panel;
  color: $text-secondary;
}
.action-btn {
  background: transparent;
  border: none;
  color: $cyan-primary;
  cursor: pointer;
  font-size: 12px;
}
.stat-row {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 10px;
  margin-bottom: 12px;
}
.stat-card {
  background: $layer-3;
  border: 1px solid $border-card;
  border-radius: 8px;
  padding: 10px 12px;
  .label {
    color: $text-muted;
    font-size: 12px;
  }
  .value {
    color: $cyan-primary;
    font-size: 18px;
    font-weight: 600;
    margin-top: 4px;
  }
}

// 列表操作列规范（收起/展开）
.op-col-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0;
  width: 100%;
}

.op-col-header-text {
  font-size: 12px;
  line-height: 1;
  white-space: nowrap;
}

.op-col-toggle-btn {
  padding: 0;
  border: none;
  background: transparent;
  cursor: pointer;
  color: $cyan-primary;
  font-size: 16px;
  line-height: 1;
  flex: 0 0 auto;
}

.op-more-trigger {
  padding: 0;
  border: none;
  background: transparent;
  cursor: pointer;
  color: $cyan-primary;
  font-size: 16px;
  line-height: 1;
  opacity: 0;
  transition: opacity 0.15s;
}

:deep(.el-table__body-wrapper .el-table__body tr:hover .op-more-trigger),
:deep(.el-table__fixed-body-wrapper .el-table__body tr:hover .op-more-trigger),
:deep(.el-table__body-wrapper .el-table__body tr.hover-row .op-more-trigger),
:deep(.el-table__fixed-body-wrapper .el-table__body tr.hover-row .op-more-trigger) {
  opacity: 1;
}

.op-more-item {
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
}

.op-more-item--primary {
  color: $cyan-primary;
}

.op-more-item--warning {
  color: $color-amber;
}

.op-more-item--danger {
  color: $color-red-brown;
}

.op-more-item--success {
  color: $color-mint-green;
}

.op-more-item--info {
  color: rgba(200, 216, 232, 0.85);
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
