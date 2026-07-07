<template>
  <div class="packing-list-page stockout-notify-page">
    <div class="page-header">
      <div class="header-left">
        <div class="page-title-group">
          <div class="page-icon">箱</div>
          <h1 class="page-title">{{ t('packingList.title') }}</h1>
        </div>
        <div class="count-badge">{{ t('packingList.count', { count: listTotal }) }}</div>
      </div>
    </div>

    <!-- 搜索栏：与 CustomerList / StockOutNotifyList 同一套结构 -->
    <div class="search-bar">
      <div class="search-left">
        <div class="search-input-wrap">
          <input
            v-model="filterForm.packingCode"
            class="search-input search-input--code"
            type="search"
            :placeholder="t('packingList.filters.packingCodePlaceholder')"
            @keyup.enter="handleSearch"
          />
        </div>
        <el-select
          v-model="filterForm.status"
          :placeholder="t('packingList.filters.statusPlaceholder')"
          clearable
          class="status-select"
          :teleported="false"
        >
          <el-option
            v-for="v in statusFilterOptions"
            :key="v"
            :label="packingStatusLabel(v)"
            :value="v"
          />
        </el-select>
        <el-select
          v-model="filterForm.stockOutType"
          :placeholder="t('packingList.filters.stockOutTypePlaceholder')"
          clearable
          class="status-select status-select--type"
          :teleported="false"
        >
          <el-option
            v-for="v in stockOutTypeFilterOptions"
            :key="v"
            :label="packingStockOutTypeLabel(v)"
            :value="v"
          />
        </el-select>
        <el-select
          v-model="filterForm.materialType"
          :placeholder="t('packingList.filters.materialTypePlaceholder')"
          clearable
          class="status-select status-select--type"
          :teleported="false"
        >
          <el-option
            v-for="v in materialTypeFilterOptions"
            :key="v"
            :label="packingMaterialTypeLabel(v)"
            :value="v"
          />
        </el-select>
        <div v-if="!maskSaleSensitiveFields" class="search-input-wrap">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="search-icon" aria-hidden="true">
            <circle cx="11" cy="11" r="8" />
            <line x1="21" y1="21" x2="16.65" y2="16.65" />
          </svg>
          <input
            v-model="filterForm.customerName"
            class="search-input search-input--customer"
            type="search"
            :placeholder="t('packingList.filters.customerNamePlaceholder')"
            @keyup.enter="handleSearch"
          />
        </div>
        <div v-if="!maskSaleSensitiveFields" class="search-input-wrap">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="search-icon" aria-hidden="true">
            <circle cx="11" cy="11" r="8" />
            <line x1="21" y1="21" x2="16.65" y2="16.65" />
          </svg>
          <input
            v-model="filterForm.salesUserName"
            class="search-input search-input--sales"
            type="search"
            :placeholder="t('packingList.filters.salesUserNamePlaceholder')"
            @keyup.enter="handleSearch"
          />
        </div>
        <el-date-picker
          v-model="filterForm.createDateRange"
          type="daterange"
          :range-separator="t('packingList.filters.dateTo')"
          :start-placeholder="t('packingList.filters.dateStart')"
          :end-placeholder="t('packingList.filters.dateEnd')"
          value-format="YYYY-MM-DD"
          clearable
          class="filter-date-range"
          :teleported="false"
        />
        <button type="button" class="btn-primary btn-sm" @click="handleSearch">{{ t('packingList.filters.search') }}</button>
        <button type="button" class="btn-ghost btn-sm" @click="handleReset">{{ t('packingList.filters.reset') }}</button>
      </div>
    </div>

    <CrmDataTable
      ref="dataTableRef"
      column-layout-key="packing-list-main-v8"
      :columns="packingColumns"
      :show-column-settings="false"
      :density-toggle-anchor-el="rowDensityToggleAnchorEl"
      :data="list"
      row-key="id"
      v-loading="loading"
      @selection-change="onSelectionChange"
      @row-dblclick="onRowDblClick"
    >
      <template #col-packingCode="{ row }">
        <span class="packing-code-cell">{{ row.code?.trim() || '—' }}</span>
      </template>
      <template #col-status="{ row }">
        <span :class="['status-badge', `packing-status-${row.status}`]">{{ packingStatusLabel(row.status) }}</span>
      </template>
      <template #col-stockOutType="{ row }">
        <StockBizTypeTag
          biz="out"
          :type="row.stockOutType"
          :customs-declaration-id="row.customsDeclarationId"
          :customs-declaration-code="row.customsDeclarationCode"
        />
      </template>
      <template #col-materialType="{ row }">{{ packingMaterialTypeLabel(row.materialType) }}</template>
      <template #col-customerName="{ row }">
        <span>{{ maskSaleSensitiveFields ? '—' : (row.customerName?.trim() || '—') }}</span>
      </template>
      <template #col-salesUserName="{ row }">
        <span>{{ maskSaleSensitiveFields ? '—' : (row.salesUserName?.trim() || '—') }}</span>
      </template>
      <template #col-warehouseName="{ row }">{{ row.warehouseName?.trim() || '—' }}</template>
      <template #col-requestDate="{ row }">
        <template v-for="p in [formatCreateTimeParts(row.requestDate)]" :key="`rd-${row.id}`">
          <span v-if="p" class="crm-quote-create-time">
            <span class="crm-quote-create-time__ymd">{{ p.date }}</span>
            <span v-if="p.time" class="crm-quote-create-time__hm">{{ p.time }}</span>
          </span>
          <span v-else>—</span>
        </template>
      </template>
      <template #col-shipmentMethod="{ row }">{{ shipmentMethodDisplay(row.shipmentMethod) }}</template>
      <template #col-expressCompany="{ row }">{{ expressCompanyDisplay(row.expressCompany) }}</template>
      <template #col-itemRows="{ row }">
        <span class="qty-cell">{{ row.itemRows ?? 0 }}</span>
      </template>
      <template #col-remark="{ row }">
        <span>{{ row.comment?.trim() || '—' }}</span>
      </template>
      <template #col-createTime="{ row }">
        <template v-for="p in [formatCreateTimeParts(row.createTime)]" :key="`ct-${row.id}`">
          <span v-if="p" class="crm-quote-create-time">
            <span class="crm-quote-create-time__ymd">{{ p.date }}</span>
            <span class="crm-quote-create-time__hm">{{ p.time }}</span>
          </span>
          <span v-else>—</span>
        </template>
      </template>
      <template #col-createUserName="{ row }">{{ row.createUserName?.trim() || '—' }}</template>
      <template #col-actions-header>
        <div class="list-op-col-header--icon-only">
          <button
            type="button"
            class="op-col-toggle-btn list-op-col-toggle"
            :aria-label="opColExpanded ? t('common.listOpCol.collapse') : t('common.listOpCol.expand')"
            @click.stop="toggleOpCol"
          >
            {{ opColExpanded ? '>' : '<' }}
          </button>
        </div>
      </template>
      <template #col-actions="{ row }">
        <div @click.stop @dblclick.stop>
          <div v-if="opColExpanded" class="action-btns action-btns--packing-wrap">
            <button type="button" class="action-btn" @click.stop="goDetail(row)">{{ t('packingList.actions.detail') }}</button>
            <button
              v-if="canWriteLogisticsData"
              type="button"
              class="action-btn"
              :disabled="!canConfirmPacking(row)"
              @click.stop="() => void confirmPacking(row)"
            >
              {{ t('packingList.actions.confirm') }}
            </button>
            <button
              v-if="canWriteLogisticsData"
              type="button"
              class="action-btn"
              :disabled="!canPickPacking(row)"
              @click.stop="() => void goPick(row)"
            >
              {{ t('packingList.actions.pick') }}
            </button>
            <button
              v-if="canWriteLogisticsData"
              type="button"
              class="action-btn"
              :disabled="!canMarkPackingReady(row)"
              @click.stop="() => void markPackingReady(row)"
            >
              {{ t('packingList.actions.ready') }}
            </button>
            <button
              v-if="canWriteLogisticsData && canRegenerateCustomsDeclaration(row)"
              type="button"
              class="action-btn action-btn--primary"
              @click.stop="() => void regenerateCustomsDeclaration(row)"
            >
              {{ t('packingList.actions.regenerateCustomsDeclaration') }}
            </button>
            <button
              v-if="canWriteLogisticsData"
              type="button"
              class="action-btn action-btn--primary"
              @click.stop="openOutBatchImport(row)"
            >
              {{ t('packingList.actions.outBatch') }}
            </button>
            <button type="button" class="action-btn" @click.stop="() => void goInvoiceReport(row)">
              {{ t('stockOutList.actions.printInvoice') }}
            </button>
            <el-dropdown trigger="click" @click.stop @command="(cmd: string) => onPackingPrintCommand(row, cmd)">
              <button type="button" class="action-btn action-btn--dropdown">
                {{ t('stockOutList.actions.printPacking') }}
                <el-icon class="action-btn__caret"><ArrowDown /></el-icon>
              </button>
              <template #dropdown>
                <el-dropdown-menu>
                  <el-dropdown-item command="with">{{ t('stockOutList.actions.packingWithInspection') }}</el-dropdown-item>
                  <el-dropdown-item command="without">{{ t('stockOutList.actions.packingWithoutInspection') }}</el-dropdown-item>
                </el-dropdown-menu>
              </template>
            </el-dropdown>
            <button
              v-if="canWriteLogisticsData"
              type="button"
              class="action-btn action-btn--danger packing-delete-btn"
              :disabled="!canDeletePacking(row)"
              @click.stop="() => void deletePacking(row)"
            >
              {{ t('packingList.actions.delete') }}
            </button>
            <button
              v-if="isSysAdmin"
              type="button"
              class="action-btn action-btn--danger"
              @click.stop="() => void forceDeletePacking(row)"
            >
              {{ t('packingList.actions.forceDelete') }}
            </button>
          </div>
          <el-dropdown v-else trigger="click" placement="bottom-end">
            <div class="op-more-dropdown-trigger">
              <button type="button" class="op-more-trigger">...</button>
            </div>
            <template #dropdown>
              <el-dropdown-menu>
                <el-dropdown-item @click.stop="goDetail(row)">
                  <span class="op-more-item op-more-item--primary">{{ t('packingList.actions.detail') }}</span>
                </el-dropdown-item>
                <el-dropdown-item v-if="canWriteLogisticsData" :disabled="!canConfirmPacking(row)" @click.stop="() => void confirmPacking(row)">
                  <span class="op-more-item">{{ t('packingList.actions.confirm') }}</span>
                </el-dropdown-item>
                <el-dropdown-item v-if="canWriteLogisticsData" :disabled="!canPickPacking(row)" @click.stop="() => void goPick(row)">
                  <span class="op-more-item">{{ t('packingList.actions.pick') }}</span>
                </el-dropdown-item>
                <el-dropdown-item v-if="canWriteLogisticsData" :disabled="!canMarkPackingReady(row)" @click.stop="() => void markPackingReady(row)">
                  <span class="op-more-item">{{ t('packingList.actions.ready') }}</span>
                </el-dropdown-item>
                <el-dropdown-item v-if="canWriteLogisticsData && canRegenerateCustomsDeclaration(row)" @click.stop="() => void regenerateCustomsDeclaration(row)">
                  <span class="op-more-item op-more-item--primary">{{ t('packingList.actions.regenerateCustomsDeclaration') }}</span>
                </el-dropdown-item>
                <el-dropdown-item v-if="canWriteLogisticsData" @click.stop="openOutBatchImport(row)">
                  <span class="op-more-item op-more-item--primary">{{ t('packingList.actions.outBatch') }}</span>
                </el-dropdown-item>
                <el-dropdown-item @click.stop="() => void goInvoiceReport(row)">
                  <span class="op-more-item">{{ t('stockOutList.actions.printInvoice') }}</span>
                </el-dropdown-item>
                <el-dropdown-item disabled>
                  <span class="op-submenu-title">{{ t('stockOutList.actions.printPacking') }}</span>
                </el-dropdown-item>
                <el-dropdown-item divided @click.stop="() => void goPackingReport(row, true)">
                  <span class="op-more-item op-more-item--sub">{{ t('stockOutList.actions.packingWithInspection') }}</span>
                </el-dropdown-item>
                <el-dropdown-item @click.stop="() => void goPackingReport(row, false)">
                  <span class="op-more-item op-more-item--sub">{{ t('stockOutList.actions.packingWithoutInspection') }}</span>
                </el-dropdown-item>
                <el-dropdown-item v-if="canWriteLogisticsData" divided :disabled="!canDeletePacking(row)" @click.stop="() => void deletePacking(row)">
                  <span
                    class="op-more-item"
                    :class="canDeletePacking(row) ? 'op-more-item--danger' : 'op-more-item--muted'"
                  >{{ t('packingList.actions.delete') }}</span>
                </el-dropdown-item>
                <el-dropdown-item v-if="isSysAdmin" @click.stop="() => void forceDeletePacking(row)">
                  <span class="op-more-item op-more-item--danger">{{ t('packingList.actions.forceDelete') }}</span>
                </el-dropdown-item>
              </el-dropdown-menu>
            </template>
          </el-dropdown>
        </div>
      </template>
    </CrmDataTable>

    <div v-if="listTotal > 0" class="pagination-wrapper">
      <div class="list-footer-left">
        <el-tooltip :content="t('packingList.columnSettings')" placement="top" :hide-after="0">
          <el-button
            class="list-settings-btn"
            link
            type="primary"
            :aria-label="t('packingList.columnSettings')"
            @click="dataTableRef?.openColumnSettings?.()"
          >
            <el-icon><Setting /></el-icon>
          </el-button>
        </el-tooltip>
        <span ref="rowDensityToggleAnchorEl" class="list-footer-density-anchor" aria-hidden="true" />
        <div class="list-footer-spacer" aria-hidden="true"></div>
        <el-button class="basket-open-btn" link type="primary" @click="basketDrawerVisible = true">
          {{ t('packingList.basket.open') }}<span v-if="basketCount" class="basket-count-label">（{{ basketCount }}）</span>
        </el-button>
        <el-button
          v-if="basketCount"
          class="basket-clear-btn"
          link
          type="warning"
          @click="handleClearBasket"
        >
          {{ t('packingList.basket.clear') }}
        </el-button>
        <button
          type="button"
          class="btn-primary btn-sm basket-stock-out-btn"
          :disabled="!basketCount"
          @click="handleBatchStockOut"
        >
          {{ t('packingList.actions.stockOut') }}
        </button>
      </div>
      <el-pagination
        class="list-main-pagination quantum-pagination"
        v-model:current-page="listPage"
        v-model:page-size="listPageSize"
        :total="listTotal"
        :page-sizes="[10, 20, 50, 100]"
        layout="total, sizes, prev, pager, next, jumper"
        @current-change="() => void fetchList(false)"
        @size-change="onPageSizeChange"
      />
    </div>

    <el-drawer
      v-model="basketDrawerVisible"
      :title="t('packingList.basket.drawerTitle')"
      direction="rtl"
      size="420px"
      class="packing-list-basket-drawer"
    >
      <p v-if="!basketCount" class="basket-drawer-hint">{{ t('packingList.basket.emptyHint') }}</p>
      <template v-else>
        <p class="basket-drawer-summary">
          {{ t('packingList.basket.summary', { count: basketCount }) }}
        </p>
        <el-table :data="basketItems" max-height="70vh" size="small" border stripe>
          <el-table-column prop="code" :label="t('packingList.columns.packingCode')" min-width="140" show-overflow-tooltip />
          <el-table-column prop="customerName" :label="t('packingList.columns.customerName')" min-width="120" show-overflow-tooltip />
          <el-table-column width="72" align="center" :label="t('packingList.basket.remove')">
            <template #default="{ row }">
              <el-button link type="danger" size="small" @click="removeOneFromBasket(row.id)">
                {{ t('packingList.basket.remove') }}
              </el-button>
            </template>
          </el-table-column>
        </el-table>
      </template>
    </el-drawer>

    <el-dialog
      v-model="markReadyDialogVisible"
      :title="t('packingList.ready.title')"
      width="520px"
      class="packing-ready-dialog"
      @closed="resetMarkReadyDialog"
    >
      <p class="packing-ready-dialog__intro">{{ t('packingList.ready.checkIntro') }}</p>
      <el-checkbox-group v-model="markReadyCheckedKeys" class="packing-ready-dialog__group">
        <el-checkbox
          v-for="key in PACKING_READY_CHECK_ITEM_KEYS"
          :key="key"
          :value="key"
          class="packing-ready-dialog__item"
        >
          {{ t(key) }}
        </el-checkbox>
      </el-checkbox-group>
      <p class="packing-ready-dialog__footer">{{ t('packingList.ready.message') }}</p>
      <template #footer>
        <el-button @click="markReadyDialogVisible = false">{{ t('common.cancel') }}</el-button>
        <el-button
          type="primary"
          :disabled="!markReadyAllChecked"
          :loading="markReadySubmitting"
          @click="() => void submitMarkReady()"
        >
          {{ t('common.confirm') }}
        </el-button>
      </template>
    </el-dialog>

    <el-dialog
      v-model="batchStockOutDialogVisible"
      :title="t('packingList.stockOut.batchTitle')"
      width="520px"
      class="packing-batch-stock-out-dialog"
      @closed="resetBatchStockOutDialog"
    >
      <p class="packing-batch-stock-out-dialog__intro">
        {{ t('packingList.stockOut.batchConfirmIntro', { count: batchStockOutRows.length }) }}
      </p>
      <div v-for="row in batchStockOutRows" :key="resolvePackingId(row)" class="packing-batch-stock-out-dialog__block">
        <p>{{ t('packingList.stockOut.confirmPackingCode', { code: displayOrDash(row.code || resolvePackingId(row)) }) }}</p>
        <p>{{ t('packingList.stockOut.confirmShipCompany') }}：{{ displayOrDash(row.shipCompany) }}</p>
        <p>{{ t('packingList.stockOut.confirmShipAddress') }}：{{ displayOrDash(row.shipAddress) }}</p>
      </div>
      <div class="packing-batch-stock-out-dialog__date">
        <label class="packing-batch-stock-out-dialog__date-label">{{ t('packingList.stockOut.expectedStockOutDate') }}</label>
        <el-date-picker
          v-model="batchStockOutExpectedDate"
          type="date"
          value-format="YYYY-MM-DD"
          :placeholder="t('packingList.stockOut.expectedStockOutDatePlaceholder')"
          :teleported="false"
          class="packing-batch-stock-out-dialog__date-picker"
        />
      </div>
      <template #footer>
        <el-button @click="batchStockOutDialogVisible = false">{{ t('packingList.messages.clearBasketCancel') }}</el-button>
        <el-button
          type="primary"
          :disabled="!batchStockOutExpectedDate"
          :loading="batchStockOutSubmitting"
          @click="() => void submitBatchStockOut()"
        >
          {{ t('packingList.stockOut.batchOk') }}
        </el-button>
      </template>
    </el-dialog>

    <StockOutBatchImportDialog
      v-model="outBatchImportVisible"
      :packing-id="outBatchImportPackingId"
      :packing-code="outBatchImportPackingCode"
      @success="() => void fetchList(false)"
    />
  </div>
</template>

<script setup lang="ts">
import { computed, nextTick, onMounted, reactive, ref, watch } from 'vue'
import { useAuthStore } from '@/stores/auth'
import { getApiErrorMessage } from '@/utils/apiError'
import { storeToRefs } from 'pinia'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { ElLoading, ElMessage, ElMessageBox } from 'element-plus'
import { ArrowDown, Setting } from '@element-plus/icons-vue'
import CrmDataTable from '@/components/CrmDataTable.vue'
import {
  packingApi,
  packingMaterialTypeLabel,
  packingStatusLabel,
  packingStockOutTypeLabel,
  PACKING_MATERIAL_TYPE_FILTER_VALUES,
  PACKING_STATUS_FILTER_VALUES,
  PACKING_STOCK_OUT_TYPE_FILTER_VALUES,
  PackingStatusCode,
  StockOutTypeCode,
  type PackingListItem,
  type PackingListQuery
} from '@/api/packing'
import { formatDisplayDateTime2DigitYearParts } from '@/utils/displayDateTime'
import { buildPackingListColumns } from '@/composables/buildPackingListColumns'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'
import { useLogisticsFormDict } from '@/composables/useLogisticsFormDict'
import { useSaleSensitiveFieldMask } from '@/composables/useSaleSensitiveFieldMask'
import { useDepartmentDataReadOnly } from '@/composables/useDepartmentDataReadOnly'
import { isPackingEligibleForStockOut, usePackingListBasketStore } from '@/stores/packingListBasket'
import StockBizTypeTag from '@/components/Inventory/StockBizTypeTag.vue'
import StockOutBatchImportDialog from '@/components/Inventory/StockOutBatchImportDialog.vue'

const router = useRouter()
const authStore = useAuthStore()
const isSysAdmin = computed(() => authStore.user?.isSysAdmin === true)
const { t, locale } = useI18n()
const { maskSaleSensitiveFields } = useSaleSensitiveFieldMask()
const { canWriteLogisticsData } = useDepartmentDataReadOnly()
const { ensureLoaded: ensureLogisticsDict, shipmentArrivalOptions, expressOptions } = useLogisticsFormDict()

const loading = ref(false)
const list = ref<PackingListItem[]>([])
const listTotal = ref(0)
const listPage = ref(1)
const listPageSize = ref(20)
const dataTableRef = ref<{
  openColumnSettings?: () => void
  clearSelection?: () => void
  toggleRowSelection?: (row: PackingListItem, selected?: boolean) => void
} | null>(null)
const rowDensityToggleAnchorEl = ref<HTMLElement | null>(null)

const basketStore = usePackingListBasketStore()
const { count: basketCount, items: basketItems } = storeToRefs(basketStore)
const basketDrawerVisible = ref(false)
const suppressBasketMerge = ref(false)

const opColExpanded = ref(false)
const OP_COL_COLLAPSED_WIDTH = 43
const OP_COL_EXPANDED_WIDTH = 220
const OP_COL_EXPANDED_MIN_WIDTH = 160
const opColWidth = computed(() => (opColExpanded.value ? OP_COL_EXPANDED_WIDTH : OP_COL_COLLAPSED_WIDTH))
const opColMinWidth = computed(() =>
  opColExpanded.value ? OP_COL_EXPANDED_MIN_WIDTH : OP_COL_COLLAPSED_WIDTH
)
function toggleOpCol() {
  opColExpanded.value = !opColExpanded.value
}

const statusFilterOptions = PACKING_STATUS_FILTER_VALUES
const stockOutTypeFilterOptions = PACKING_STOCK_OUT_TYPE_FILTER_VALUES
const materialTypeFilterOptions = PACKING_MATERIAL_TYPE_FILTER_VALUES

type FilterForm = {
  packingCode: string
  status?: number
  stockOutType?: number
  materialType?: number
  customerName: string
  salesUserName: string
  createDateRange: [string, string] | null
}

function defaultFilterForm(): FilterForm {
  return {
    packingCode: '',
    status: undefined,
    stockOutType: undefined,
    materialType: undefined,
    customerName: '',
    salesUserName: '',
    createDateRange: null
  }
}

const filterForm = reactive<FilterForm>(defaultFilterForm())

const packingColumns = computed<CrmTableColumnDef[]>(() => {
  void locale.value
  return buildPackingListColumns({
    t,
    opColWidth: opColWidth.value,
    opColMinWidth: opColMinWidth.value,
    withSelection: true,
    withActions: true
  })
})

function formatCreateTimeParts(v?: string | null) {
  if (!v) return null
  return formatDisplayDateTime2DigitYearParts(v)
}

const arrivalLabelByCode = computed(() => {
  const m = new Map<string, string>()
  for (const o of shipmentArrivalOptions.value) {
    const k = String(o.value ?? '').trim()
    if (k) m.set(k.toLowerCase(), o.label)
  }
  return m
})

const expressLabelByCode = computed(() => {
  const m = new Map<string, string>()
  for (const o of expressOptions.value) {
    const k = String(o.value ?? '').trim()
    if (k) m.set(k.toLowerCase(), o.label)
  }
  return m
})

function shipmentMethodDisplay(code?: string | number | null): string {
  if (code === null || code === undefined || code === '') return '—'
  const c = String(code).trim()
  if (!c) return '—'
  return arrivalLabelByCode.value.get(c.toLowerCase()) ?? c
}

function expressCompanyDisplay(code?: string | null): string {
  const c = String(code ?? '').trim()
  if (!c) return '—'
  return expressLabelByCode.value.get(c.toLowerCase()) ?? c
}

function buildListQuery(): PackingListQuery {
  const q: PackingListQuery = {
    page: listPage.value,
    pageSize: listPageSize.value
  }
  const code = filterForm.packingCode.trim()
  if (code) q.packingCode = code
  if (filterForm.status != null) q.status = filterForm.status
  if (filterForm.stockOutType != null) q.stockOutType = filterForm.stockOutType
  if (filterForm.materialType != null) q.materialType = filterForm.materialType
  if (!maskSaleSensitiveFields.value) {
    const customer = filterForm.customerName.trim()
    if (customer) q.customerName = customer
    const sales = filterForm.salesUserName.trim()
    if (sales) q.salesUserName = sales
  }
  const range = filterForm.createDateRange
  if (range?.[0]) q.createTimeFrom = range[0]
  if (range?.[1]) q.createTimeTo = range[1]
  return q
}

async function fetchList(resetPage = true) {
  if (resetPage) listPage.value = 1
  loading.value = true
  try {
    const res = await packingApi.getListPaged(buildListQuery())
    list.value = res.items
    listTotal.value = res.total
    await restoreTableSelectionFromBasket()
  } catch (e) {
    console.error(e)
    ElMessage.error(t('packingList.messages.loadFailed'))
  } finally {
    loading.value = false
  }
}

function handleSearch() {
  void fetchList(true)
}

function onPageSizeChange() {
  listPage.value = 1
  void fetchList(false)
}

function handleReset() {
  Object.assign(filterForm, defaultFilterForm())
  void fetchList(true)
}

watch(listTotal, () => {
  const maxP = Math.max(1, Math.ceil(listTotal.value / listPageSize.value) || 1)
  if (listPage.value > maxP) listPage.value = maxP
})

function canConfirmPacking(row: PackingListItem): boolean {
  return Number(row?.status) === PackingStatusCode.New
}

function canRegenerateCustomsDeclaration(row: PackingListItem): boolean {
  if (Number(row?.stockOutType) !== StockOutTypeCode.Customs) return false
  const status = Number(row?.status)
  return status >= PackingStatusCode.Confirmed && status < PackingStatusCode.StockOutFinished
}

function openOutBatchImport(row: PackingListItem) {
  const id = resolvePackingId(row)
  if (!id) return
  outBatchImportPackingId.value = id
  outBatchImportPackingCode.value = String(row.code ?? '').trim()
  outBatchImportVisible.value = true
}

function canDeletePacking(row: PackingListItem): boolean {
  return Number(row?.status) === PackingStatusCode.New
}

async function deletePacking(row: PackingListItem) {
  const id = resolvePackingId(row)
  if (!id) return

  if (Number(row.status) !== PackingStatusCode.New) {
    ElMessage.error(t('packingList.delete.notNewStatus'))
    return
  }

  try {
    await ElMessageBox.confirm(t('packingList.delete.message'), t('packingList.delete.title'), {
      type: 'warning',
      confirmButtonText: t('common.confirm'),
      cancelButtonText: t('common.cancel')
    })
  } catch {
    return
  }

  try {
    await packingApi.delete(id)
    ElMessage.success(t('packingList.delete.success'))
    await fetchList(false)
  } catch (e) {
    ElMessage.error(e instanceof Error ? e.message : t('packingList.delete.failed'))
  }
}

async function forceDeletePacking(row: PackingListItem) {
  const id = resolvePackingId(row)
  if (!id) return

  const expectedCode = String(row.code ?? '').trim()
  let entered = ''
  try {
    const ret = await ElMessageBox.prompt(
      t('packingList.forceDelete.prompt'),
      t('packingList.forceDelete.title'),
      {
        inputPlaceholder: expectedCode,
        confirmButtonText: t('common.confirm'),
        cancelButtonText: t('common.cancel')
      }
    )
    entered = String(ret.value || '').trim()
  } catch {
    return
  }

  if (entered !== expectedCode) {
    ElMessage.error(t('packingList.forceDelete.codeMismatch'))
    return
  }

  try {
    await packingApi.forceDelete(id, entered)
    ElMessage.success(t('packingList.forceDelete.success'))
    await fetchList(false)
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('packingList.forceDelete.failed')))
  }
}

async function confirmPacking(row: PackingListItem) {
  const id = resolvePackingId(row)
  if (!id) return

  if (Number(row.status) !== PackingStatusCode.New) {
    ElMessage.error(t('packingList.confirm.notNewStatus'))
    return
  }

  try {
    await ElMessageBox.confirm(t('packingList.confirm.message'), t('packingList.confirm.title'), {
      type: 'warning',
      confirmButtonText: t('common.confirm'),
      cancelButtonText: t('common.cancel')
    })
  } catch {
    return
  }

  try {
    await packingApi.confirm(id)
    ElMessage.success(t('packingList.confirm.success'))
    await fetchList(false)
  } catch (e) {
    ElMessage.error(e instanceof Error ? e.message : t('packingList.confirm.failed'))
  }
}

async function regenerateCustomsDeclaration(row: PackingListItem) {
  const id = resolvePackingId(row)
  if (!id) return

  if (!canRegenerateCustomsDeclaration(row)) {
    ElMessage.error(t('packingList.regenerateCustomsDeclaration.failed'))
    return
  }

  try {
    await ElMessageBox.confirm(
      t('packingList.regenerateCustomsDeclaration.message'),
      t('packingList.regenerateCustomsDeclaration.title'),
      {
        type: 'warning',
        confirmButtonText: t('common.confirm'),
        cancelButtonText: t('common.cancel')
      }
    )
  } catch {
    return
  }

  try {
    await packingApi.regenerateCustomsDeclaration(id)
    ElMessage.success(t('packingList.regenerateCustomsDeclaration.success'))
    await fetchList(false)
  } catch (e) {
    ElMessage.error(e instanceof Error ? e.message : t('packingList.regenerateCustomsDeclaration.failed'))
  }
}

function canMarkPackingReady(row: PackingListItem): boolean {
  return Number(row?.status) === PackingStatusCode.Picked
}

const PACKING_READY_CHECK_ITEM_KEYS = [
  'packingList.ready.checkItemGoods',
  'packingList.ready.checkItemPackaging',
  'packingList.ready.checkItemLabel',
  'packingList.ready.checkItemDocuments',
  'packingList.ready.checkItemCourier'
] as const

const markReadyDialogVisible = ref(false)
const markReadyTargetId = ref('')
const markReadyCheckedKeys = ref<string[]>([])
const markReadySubmitting = ref(false)

const batchStockOutDialogVisible = ref(false)
const outBatchImportVisible = ref(false)
const outBatchImportPackingId = ref('')
const outBatchImportPackingCode = ref('')
const batchStockOutRows = ref<PackingListItem[]>([])
const batchStockOutExpectedDate = ref('')
const batchStockOutSubmitting = ref(false)

const markReadyAllChecked = computed(
  () =>
    PACKING_READY_CHECK_ITEM_KEYS.length > 0 &&
    PACKING_READY_CHECK_ITEM_KEYS.every((key) => markReadyCheckedKeys.value.includes(key))
)

function resetMarkReadyDialog() {
  markReadyTargetId.value = ''
  markReadyCheckedKeys.value = []
  markReadySubmitting.value = false
}

async function markPackingReady(row: PackingListItem) {
  const id = resolvePackingId(row)
  if (!id) return

  if (Number(row.status) !== PackingStatusCode.Picked) {
    ElMessage.error(t('packingList.ready.notPickedStatus'))
    return
  }

  markReadyTargetId.value = id
  markReadyCheckedKeys.value = []
  markReadyDialogVisible.value = true
}

async function submitMarkReady() {
  const id = markReadyTargetId.value.trim()
  if (!id || !markReadyAllChecked.value) return

  markReadySubmitting.value = true
  try {
    await packingApi.markReady(id)
    ElMessage.success(t('packingList.ready.success'))
    markReadyDialogVisible.value = false
    await fetchList(false)
  } catch (e) {
    ElMessage.error(e instanceof Error ? e.message : t('packingList.ready.failed'))
  } finally {
    markReadySubmitting.value = false
  }
}

function goDetail(row: PackingListItem) {
  const id = String(row?.id || '').trim()
  if (!id) {
    ElMessage.warning(t('packingDetail.missingId'))
    return
  }
  router.push({ name: 'PackingDetail', params: { id } })
}

function canPickPacking(row: PackingListItem): boolean {
  return Number(row?.status) === PackingStatusCode.Confirmed
}

async function goPick(row: PackingListItem) {
  const packingId = resolvePackingId(row)
  if (!packingId) return

  if (Number(row.status) !== PackingStatusCode.Confirmed) {
    ElMessage.error(t('packingList.pick.notConfirmedStatus'))
    return
  }

  try {
    const warehouseId = String(row.storageId ?? '').trim()
    router.push({
      path: '/inventory/pick/create',
      query: {
        packingId,
        ...(warehouseId ? { warehouseId } : {})
      }
    })
  } catch (e) {
    console.error(e)
    ElMessage.error(e instanceof Error ? e.message : t('packingList.pick.resolveFailed'))
  }
}

function onRowDblClick(row: PackingListItem) {
  goDetail(row)
}

function resolvePackingId(row: PackingListItem): string {
  return String(row?.id || '').trim()
}

async function onSelectionChange(rows: PackingListItem[]) {
  if (suppressBasketMerge.value) return
  const eligible = rows.filter(isPackingEligibleForStockOut)
  const ineligible = rows.filter((r) => !isPackingEligibleForStockOut(r))
  if (ineligible.length > 0) {
    suppressBasketMerge.value = true
    for (const row of ineligible) {
      dataTableRef.value?.toggleRowSelection?.(row, false)
    }
    await nextTick()
    suppressBasketMerge.value = false
    ElMessage.warning(t('packingList.stockOut.onlyEligibleSelectable'))
  }
  basketStore.mergePageSelection(list.value, eligible)
}

async function restoreTableSelectionFromBasket() {
  const table = dataTableRef.value
  if (!table) return
  suppressBasketMerge.value = true
  table.clearSelection?.()
  await nextTick()
  for (const row of list.value) {
    const id = resolvePackingId(row)
    if (id && basketStore.has(id)) {
      table.toggleRowSelection?.(row, true)
    }
  }
  await nextTick()
  suppressBasketMerge.value = false
}

function removeOneFromBasket(id: string) {
  const rid = String(id || '').trim()
  if (!rid) return
  basketStore.remove(rid)
  suppressBasketMerge.value = true
  const row = list.value.find((r) => resolvePackingId(r) === rid)
  if (row) {
    dataTableRef.value?.toggleRowSelection?.(row, false)
  }
  void nextTick(() => {
    suppressBasketMerge.value = false
  })
}

async function handleClearBasket() {
  if (!basketStore.count) return
  try {
    await ElMessageBox.confirm(
      t('packingList.messages.clearBasketConfirm'),
      t('packingList.messages.clearBasketTitle'),
      {
        type: 'warning',
        confirmButtonText: t('packingList.messages.clearBasketOk'),
        cancelButtonText: t('packingList.messages.clearBasketCancel')
      }
    )
  } catch {
    return
  }
  basketStore.clear()
  suppressBasketMerge.value = true
  dataTableRef.value?.clearSelection?.()
  await nextTick()
  suppressBasketMerge.value = false
  ElMessage.success(t('packingList.messages.basketCleared'))
}

type StockOutSelectionValidation = { ok: true } | { ok: false; reasons: string[] }

function normalizeShipCompareValue(value: string | null | undefined): string {
  return String(value ?? '').trim()
}

function validateStockOutSelection(rows: PackingListItem[]): StockOutSelectionValidation {
  const reasons: string[] = []
  if (rows.length === 0) {
    reasons.push(t('packingList.stockOut.noSelection'))
    return { ok: false, reasons }
  }
  const notReady = rows.filter((r) => Number(r.status) !== PackingStatusCode.Ready)
  if (notReady.length > 0) {
    reasons.push(t('packingList.stockOut.ruleMustBeReady'))
  }
  const customerIds = rows.map((r) => String(r.customerId || '').trim())
  const uniqueCustomers = new Set(customerIds.filter(Boolean))
  if (uniqueCustomers.size !== 1 || customerIds.some((id) => !id)) {
    reasons.push(t('packingList.stockOut.ruleSameCustomer'))
  }
  const warehouseNames = rows.map((r) => normalizeShipCompareValue(r.warehouseName))
  if (new Set(warehouseNames).size !== 1 || warehouseNames.some((name) => !name)) {
    reasons.push(t('packingList.stockOut.ruleSameWarehouse'))
  }
  const stockOutTypes = rows.map((r) => Number(r.stockOutType))
  if (new Set(stockOutTypes).size !== 1) {
    reasons.push(t('packingList.stockOut.ruleSameStockOutType'))
  }
  if (reasons.length > 0) return { ok: false, reasons }
  return { ok: true }
}

function displayOrDash(value: string | null | undefined): string {
  const s = String(value ?? '').trim()
  return s || '—'
}

function resetBatchStockOutDialog() {
  batchStockOutRows.value = []
  batchStockOutExpectedDate.value = ''
  batchStockOutSubmitting.value = false
}

async function showStockOutValidationAlert(reasons: string[]) {
  const body =
    reasons.length === 1 && reasons[0] === t('packingList.stockOut.noSelection')
      ? reasons[0]
      : `${t('packingList.stockOut.cannotIntro')}\n\n${reasons.map((r) => `• ${r}`).join('\n')}`
  await ElMessageBox.alert(body, t('packingList.stockOut.cannotTitle'), {
    confirmButtonText: t('packingList.stockOut.cannotOk'),
    type: 'warning'
  })
}

async function handleBatchStockOut() {
  const rows = basketStore.items
  const validation = validateStockOutSelection(rows)
  if (!validation.ok) {
    await showStockOutValidationAlert(validation.reasons)
    return
  }
  batchStockOutRows.value = [...rows]
  batchStockOutExpectedDate.value = ''
  batchStockOutDialogVisible.value = true
}

async function submitBatchStockOut() {
  if (!batchStockOutExpectedDate.value) {
    ElMessage.warning(t('packingList.stockOut.expectedStockOutDateRequired'))
    return
  }
  const packingIds = batchStockOutRows.value.map((r) => resolvePackingId(r)).filter(Boolean)
  if (!packingIds.length) return

  batchStockOutSubmitting.value = true
  const loading = ElLoading.service({
    lock: true,
    text: t('packingList.stockOut.batchProcessing')
  })
  try {
    const result = await packingApi.batchStockOut(packingIds, batchStockOutExpectedDate.value)
    const codes = result.lines.map((l) => l.stockOutCode || l.packingCode).filter(Boolean)
    ElMessage.success(
      t('packingList.stockOut.batchSuccess', {
        count: result.lines.length,
        codes: codes.join('、')
      })
    )
    batchStockOutDialogVisible.value = false
    basketStore.clear()
    suppressBasketMerge.value = true
    dataTableRef.value?.clearSelection?.()
    await nextTick()
    suppressBasketMerge.value = false
    await fetchList(false)
  } catch (e) {
    console.error(e)
    ElMessage.error(e instanceof Error ? e.message : t('packingList.stockOut.batchFailed'))
  } finally {
    loading.close()
    batchStockOutSubmitting.value = false
  }
}

function goInvoiceReport(row: PackingListItem) {
  const packingId = resolvePackingId(row)
  if (!packingId) return
  router.push({ name: 'PackingInvoiceReport', params: { packingId } })
}

async function goPackingReport(row: PackingListItem, withInspection: boolean) {
  const packingId = String(row?.id || '').trim()
  if (!packingId) return
  router.push({
    name: 'PackingReport',
    params: {
      packingId,
      packingInspection: withInspection ? 'with-inspection' : 'without-inspection'
    }
  })
}

function onPackingPrintCommand(row: PackingListItem, cmd: string) {
  if (cmd === 'with') void goPackingReport(row, true)
  else if (cmd === 'without') void goPackingReport(row, false)
}

onMounted(() => {
  void ensureLogisticsDict()
  void fetchList(true)
})
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.packing-list-page {
  padding: 24px;
  min-height: 100%;
  background: $layer-1;

  // 表头单行：压过 main.scss 中表头允许换行的全局规则
  :deep(.el-table__header-wrapper th.el-table__cell .cell),
  :deep(.el-table__fixed-header-wrapper th.el-table__cell .cell) {
    white-space: nowrap !important;
    word-break: keep-all !important;
    overflow-wrap: normal !important;
  }
}

.page-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 20px;
}

.header-left {
  display: flex;
  align-items: center;
  gap: 10px;
}

.page-title-group {
  display: flex;
  align-items: center;
  gap: 10px;
}

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
  font-size: 14px;
  font-weight: 600;
}

.page-title {
  margin: 0;
  font-size: 20px;
  font-weight: 600;
  color: $text-primary;
}

.count-badge {
  font-size: 12px;
  color: $text-muted;
  background: rgba(255, 255, 255, 0.05);
  border: 1px solid $border-panel;
  border-radius: 20px;
  padding: 3px 10px;
}

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

  &--code {
    width: 160px;
  }

  &--customer {
    width: 160px;
    padding-left: 32px;
  }

  &--sales {
    width: 120px;
    padding-left: 32px;
  }
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

  &--type {
    width: 130px;
  }
}

.filter-date-range {
  width: 260px !important;

  :deep(.el-input__wrapper) {
    background: $layer-2 !important;
    box-shadow: none !important;
    border: 1px solid $border-panel !important;
    border-radius: $border-radius-md !important;
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
}

.packing-code-cell {
  color: $text-primary;
}

.qty-cell {
  font-weight: 700;
  color: $text-primary;
}

.status-badge {
  display: inline-block;
  padding: 2px 8px;
  border-radius: 4px;
  font-size: 12px;
  white-space: nowrap;

  &.packing-status-10 {
    background: rgba(255, 193, 7, 0.15);
    color: #ffc107;
  }
  &.packing-status-20 {
    background: rgba(0, 212, 255, 0.15);
    color: $cyan-primary;
  }
  &.packing-status-30 {
    background: rgba(100, 149, 237, 0.18);
    color: #8eb4ff;
  }
  &.packing-status-40 {
    background: rgba(255, 193, 7, 0.2);
    color: #ffc107;
  }
  &.packing-status-50 {
    background: rgba(255, 152, 0, 0.15);
    color: #ff9800;
  }
  &.packing-status-100 {
    background: rgba(70, 191, 145, 0.22);
    color: #46bf91;
  }
}

.pagination-wrapper {
  margin-top: 12px;
  display: flex;
  align-items: flex-start;
  justify-content: flex-start;
  flex-wrap: wrap;
  gap: 12px 16px;
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

.list-main-pagination {
  margin-left: auto;
  align-self: flex-start;
}

.op-more-dropdown-trigger {
  display: inline-flex;
}
.op-more-trigger {
  background: transparent;
  border: none;
  cursor: pointer;
  color: $cyan-primary;
  font-size: 16px;
  line-height: 1;
  padding: 2px 6px;
}
.op-more-item {
  font-size: 13px;
}
.op-more-item--primary {
  color: $cyan-primary;
}
.op-submenu-title {
  font-size: 12px;
  color: $text-muted;
}
.op-more-item--sub {
  padding-left: 8px;
}

.packing-list-page :deep(.crm-data-table td.op-col .cell),
.packing-list-page :deep(.crm-data-table th.op-col .cell) {
  overflow: visible;
}

.packing-list-page :deep(.action-btns--packing-wrap) {
  flex-wrap: wrap;
  white-space: normal;
  row-gap: 4px;
  column-gap: 6px;
  justify-content: flex-end;
}

.packing-list-page :deep(.action-btns--packing-wrap .action-btn) {
  white-space: nowrap;
}

.packing-list-page :deep(.action-btns--packing-wrap .packing-delete-btn:disabled) {
  color: $text-muted;
  background: rgba(255, 255, 255, 0.03);
  border: 1px solid $border-panel;
  cursor: not-allowed;
  opacity: 1;

  &:hover {
    color: $text-muted;
    background: rgba(255, 255, 255, 0.03);
    border-color: $border-panel;
    text-decoration: none;
  }
}

.op-more-item--muted {
  color: $text-muted;
}

.packing-ready-dialog__intro {
  margin: 0 0 12px;
  color: var(--el-text-color-primary);
  line-height: 1.6;
}

.packing-ready-dialog__group {
  display: flex;
  flex-direction: column;
  gap: 10px;
  width: 100%;
}

.packing-ready-dialog__item {
  display: flex;
  align-items: flex-start;
  margin-right: 0;
  height: auto;
  white-space: normal;
}

.packing-ready-dialog__item :deep(.el-checkbox__label) {
  white-space: normal;
  line-height: 1.6;
}

.packing-ready-dialog__footer {
  margin: 14px 0 0;
  color: var(--el-text-color-secondary);
  font-size: 13px;
  line-height: 1.6;
}

.packing-batch-stock-out-dialog__intro {
  margin: 0 0 12px;
  color: var(--el-text-color-regular);
  line-height: 1.6;
}

.packing-batch-stock-out-dialog__block {
  margin-bottom: 12px;
  padding: 10px 12px;
  border-radius: 8px;
  background: rgba(255, 255, 255, 0.04);
  border: 1px solid rgba(255, 255, 255, 0.08);
}

.packing-batch-stock-out-dialog__block p {
  margin: 0 0 4px;
  line-height: 1.5;
  font-size: 13px;
  color: var(--el-text-color-regular);
}

.packing-batch-stock-out-dialog__block p:last-child {
  margin-bottom: 0;
}

.packing-batch-stock-out-dialog__date {
  margin-top: 16px;
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.packing-batch-stock-out-dialog__date-label {
  font-size: 13px;
  color: var(--el-text-color-regular);
}

.packing-batch-stock-out-dialog__date-picker {
  width: 100%;
}

:deep(.stock-out-type-col .cell) {
  overflow: visible;
}
</style>
