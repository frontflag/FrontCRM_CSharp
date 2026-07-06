<template>
  <div class="stockout-list-page">
    <div class="page-header">
      <div class="header-left">
        <div class="page-title-group">
          <div class="page-icon">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
              <path d="M3 3h7v7H3zM14 3h7v7h-7zM3 14h7v7H3zM17 14l4 4-4 4M10 17h11" />
            </svg>
          </div>
          <h1 class="page-title">{{ t('stockOutList.title') }}</h1>
        </div>
        <div class="count-badge">{{ t('stockOutList.count', { count: listTotal }) }}</div>
      </div>
    </div>

    <div class="search-bar">
      <div class="search-left">
        <el-select
          v-model="filterForm.status"
          :placeholder="t('stockOutList.filters.status')"
          clearable
          class="filter-select filter-select--status"
          :teleported="false"
        >
          <el-option
            v-for="opt in statusFilterOptions"
            :key="opt.value"
            :label="opt.label"
            :value="opt.value"
          />
        </el-select>
        <el-select
          v-model="filterForm.stockOutType"
          :placeholder="t('stockOutList.filters.stockOutTypePlaceholder')"
          clearable
          class="filter-select filter-select--stock-out-type"
          :teleported="false"
        >
          <el-option
            v-for="v in STOCK_OUT_TYPE_FILTER_VALUES"
            :key="v"
            :label="listStockOutTypeLabel(v)"
            :value="v"
          />
        </el-select>
        <input
          v-model="filterForm.stockOutCode"
          class="search-input search-input--code"
          type="search"
          :placeholder="t('stockOutList.filters.stockOutCode')"
          @keyup.enter="handleSearch"
        />
        <input
          v-model="filterForm.packingCode"
          class="search-input search-input--code"
          type="search"
          :placeholder="t('stockOutList.filters.packingCode')"
          @keyup.enter="handleSearch"
        />
        <input
          v-model="filterForm.freightForwarderOrderNo"
          class="search-input search-input--code"
          type="search"
          :placeholder="t('common.freightForwarderOrderNoPlaceholder')"
          @keyup.enter="handleSearch"
        />
        <el-select
          v-model="filterForm.shipmentMethod"
          :placeholder="t('stockOutList.filters.shipmentMethod')"
          clearable
          filterable
          class="filter-select filter-select--shipment"
          :teleported="false"
        >
          <el-option v-for="o in shipmentArrivalOptions" :key="o.value" :label="o.label" :value="o.value" />
        </el-select>
        <input
          v-if="!maskSaleSensitiveFields"
          v-model="filterForm.customerName"
          class="search-input search-input--customer"
          type="search"
          :placeholder="t('stockOutList.filters.customerName')"
          @keyup.enter="handleSearch"
        />
        <input
          v-if="!maskSaleSensitiveFields"
          v-model="filterForm.salesUserName"
          class="search-input search-input--sales"
          type="search"
          :placeholder="t('stockOutList.filters.salesUserName')"
          @keyup.enter="handleSearch"
        />
        <input
          v-model="filterForm.remark"
          class="search-input search-input--remark"
          type="search"
          :placeholder="t('stockOutList.filters.remark')"
          @keyup.enter="handleSearch"
        />
        <el-date-picker
          v-model="filterForm.stockOutDateRange"
          type="daterange"
          :range-separator="t('stockOutList.filters.stockOutDateSep')"
          :start-placeholder="t('stockOutList.filters.stockOutDateFrom')"
          :end-placeholder="t('stockOutList.filters.stockOutDateTo')"
          value-format="YYYY-MM-DD"
          clearable
          class="filter-date-range"
          :teleported="false"
        />
        <button type="button" class="btn-primary btn-sm" @click="handleSearch">{{ t('stockOutList.filters.search') }}</button>
        <button type="button" class="btn-ghost btn-sm" @click="handleReset">{{ t('stockOutList.filters.reset') }}</button>
      </div>
    </div>

    <!-- 结构与 StockOutNotifyList / StockInList 一致：无 row-key、无额外包裹 -->
    <CrmDataTable
      ref="dataTableRef"
      column-layout-key="stock-out-list-main-v4"
      :columns="stockOutTableColumns"
      :show-column-settings="false"
      :density-toggle-anchor-el="rowDensityToggleAnchorEl"
      :data="list"
      row-key="id"
      v-loading="loading"
      @row-dblclick="onRowDblclick"
      @header-dragend="onStockOutTableHeaderDragEnd"
    >
      <template #col-status="{ row }">
        <span :class="['status-badge', `status-${row.status}`]">{{ statusLabel(row.status) }}</span>
      </template>
      <template #col-stockOutType="{ row }">
        <StockBizTypeTag biz="out" :type="row.stockOutType" />
      </template>
      <template #col-stockOutCode="{ row }">
        <span class="stock-out-code-cell">
          <span class="mono-cell">{{ row.stockOutCode || t('quoteList.na') }}</span>
          <el-tooltip
            v-if="isCustomsStockOut(row) && salesNotifyTooltip(row)"
            :content="salesNotifyTooltip(row)"
            placement="top"
            :hide-after="0"
          >
            <span class="customs-notify-tag">{{ t('stockOutList.customsNotifyTag') }}</span>
          </el-tooltip>
        </span>
      </template>
      <template #col-stockOutDate="{ row }">
        <span class="text-secondary">{{ formatDate(row.stockOutDate) }}</span>
      </template>
      <template #col-expectedStockOutDate="{ row }">
        <span class="text-secondary">{{ formatDate(row.expectedStockOutDate) }}</span>
      </template>
      <template #col-packingCount="{ row }">{{ formatPackingCount(row.packingCount) }}</template>
      <template #col-packingCodes="{ row }">
        <span class="mono-cell">{{ row.packingCodes?.trim() || t('quoteList.na') }}</span>
      </template>
      <template #col-createTime="{ row }">{{ formatDate((row as any).createTime || (row as any).createdAt) }}</template>
      <template #col-createUser="{ row }">{{ row.createUserName || (row as any).createdBy || t('quoteList.na') }}</template>
      <template #col-customer-header>
        <CustomerExtendColumnHeader
          :active-field="customerExtendActiveField"
          @set-active-field="setCustomerExtendActiveField"
        />
      </template>
      <template #col-customer="{ row }">
        <CustomerExtendCell
          :row="row"
          :active-field="customerExtendActiveField"
          :masked="maskSaleSensitiveFields"
          :empty-text="t('quoteList.na')"
        />
      </template>
      <template #col-salesUserName="{ row }">{{ maskSaleSensitiveFields ? '—' : (row.salesUserName || t('quoteList.na')) }}</template>
      <template #col-shipmentMethod="{ row }">{{ shipmentMethodDisplay(row.shipmentMethod) }}</template>
      <template #col-expressCompany="{ row }">{{ expressCompanyDisplay(row.expressCompany) }}</template>
      <template #col-courierTrackingNo="{ row }">{{ row.courierTrackingNo || t('quoteList.na') }}</template>
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
          <div v-if="opColExpanded" class="action-btns">
            <button type="button" class="action-btn" @click.stop="goDetail(row)">{{ t('stockOutList.actions.detail') }}</button>
            <button
              v-if="canWriteLogisticsData"
              type="button"
              class="action-btn"
              @click.stop="handleEdit(row)"
            >
              {{ t('stockOutList.actions.edit') }}
            </button>
            <button
              v-if="canWriteLogisticsData && row.status !== 4"
              type="button"
              class="action-btn action-btn--warning"
              @click.stop="handleMarkFinish(row)"
            >
              {{ t('stockOutList.actions.markFinished') }}
            </button>
            <button v-if="canWriteLogisticsData" type="button" class="action-btn action-btn--danger" @click.stop="handleDeleteRow(row)">删除</button>
            <button v-if="isSysAdmin" type="button" class="action-btn action-btn--danger" @click.stop="handleForceDeleteRow(row)">强制删除</button>
          </div>
          <el-dropdown v-else trigger="click" placement="bottom-end">
            <div class="op-more-dropdown-trigger">
              <button type="button" class="op-more-trigger">...</button>
            </div>
            <template #dropdown>
              <el-dropdown-menu>
                <el-dropdown-item @click.stop="goDetail(row)">
                  <span class="op-more-item op-more-item--primary">{{ t('stockOutList.actions.detail') }}</span>
                </el-dropdown-item>
                <el-dropdown-item v-if="canWriteLogisticsData" @click.stop="handleEdit(row)">
                  <span class="op-more-item">{{ t('stockOutList.actions.edit') }}</span>
                </el-dropdown-item>
                <el-dropdown-item v-if="canWriteLogisticsData && row.status !== 4" @click.stop="handleMarkFinish(row)">
                  <span class="op-more-item op-more-item--warning">{{ t('stockOutList.actions.markFinished') }}</span>
                </el-dropdown-item>
                <el-dropdown-item v-if="canWriteLogisticsData" divided @click.stop="handleDeleteRow(row)">
                  <span class="op-more-item op-more-item--danger">删除</span>
                </el-dropdown-item>
                <el-dropdown-item v-if="isSysAdmin" @click.stop="handleForceDeleteRow(row)">
                  <span class="op-more-item op-more-item--danger">强制删除</span>
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
      <el-pagination
        class="list-main-pagination"
        v-model:current-page="listPage"
        v-model:page-size="listPageSize"
        :total="listTotal"
        :page-sizes="[10, 20, 50, 100]"
        layout="total, sizes, prev, pager, next, jumper"
        @current-change="() => void runStockOutListFetch(false)"
        @size-change="onStockOutListPageSizeChange"
      />
    </div>

    <el-dialog
      v-model="markFinishDialogVisible"
      :title="t('stockOutList.markFinish.title')"
      width="560px"
      class="stock-out-mark-finish-dialog"
      @closed="resetMarkFinishDialog"
    >
      <div v-loading="markFinishLoading" class="stock-out-mark-finish-dialog__body">
        <dl class="stock-out-mark-finish-dialog__info">
          <div class="stock-out-mark-finish-dialog__row">
            <dt>{{ t('stockOutList.columns.customerName') }}</dt>
            <dd>{{ displayOrDash(markFinishContext?.customerName) }}</dd>
          </div>
          <div class="stock-out-mark-finish-dialog__row">
            <dt>{{ t('stockOutList.markFinish.shipAddress') }}</dt>
            <dd>{{ displayOrDash(markFinishContext?.shipAddress) }}</dd>
          </div>
          <div class="stock-out-mark-finish-dialog__row">
            <dt>{{ t('stockOutList.markFinish.packingSummary') }}</dt>
            <dd>
              <template v-if="markFinishContext?.packings?.length">
                {{ t('stockOutList.markFinish.packingCount', { count: markFinishContext.packings.length }) }}
                <ul class="stock-out-mark-finish-dialog__packing-list">
                  <li v-for="pk in markFinishContext.packings" :key="pk.id">
                    {{ pk.code || pk.id }}
                  </li>
                </ul>
              </template>
              <span v-else>{{ t('quoteList.na') }}</span>
            </dd>
          </div>
        </dl>
        <div class="stock-out-mark-finish-dialog__form">
          <label class="stock-out-mark-finish-dialog__label">
            {{ t('stockOutList.markFinish.actualStockOutDate') }}
            <el-date-picker
              v-model="markFinishForm.stockOutDate"
              type="date"
              value-format="YYYY-MM-DD"
              :placeholder="t('stockOutList.markFinish.actualStockOutDatePlaceholder')"
              :teleported="false"
              class="stock-out-mark-finish-dialog__field"
            />
          </label>
          <label class="stock-out-mark-finish-dialog__label">
            {{ t('stockOutList.columns.courierTrackingNo') }}
            <el-input
              v-model="markFinishForm.courierTrackingNo"
              :placeholder="t('stockOutList.markFinish.courierTrackingNoPlaceholder')"
              clearable
              class="stock-out-mark-finish-dialog__field"
            />
          </label>
          <label class="stock-out-mark-finish-dialog__label">
            {{ t('stockOutList.columns.remark') }}
            <el-input
              v-model="markFinishForm.remark"
              type="textarea"
              :rows="2"
              :placeholder="t('stockOutList.markFinish.remarkPlaceholder')"
              class="stock-out-mark-finish-dialog__field"
            />
          </label>
        </div>
      </div>
      <template #footer>
        <el-button @click="markFinishDialogVisible = false">{{ t('common.cancel') }}</el-button>
        <el-button
          type="primary"
          :disabled="!canSubmitMarkFinish"
          :loading="markFinishSubmitting"
          @click="() => void submitMarkFinish()"
        >
          {{ t('common.confirm') }}
        </el-button>
      </template>
    </el-dialog>

    <el-dialog
      v-model="editDialogVisible"
      :title="t('stockOutList.editDialog.title')"
      width="480px"
      class="stock-out-edit-header-dialog"
      @closed="resetEditDialog"
    >
      <div v-loading="editLoading" class="stock-out-edit-header-dialog__body">
        <el-form label-width="100px" class="stock-out-edit-header-dialog__form">
          <el-form-item :label="t('stockOutList.columns.stockOutDate')">
            <el-date-picker
              v-model="editForm.stockOutDate"
              type="date"
              value-format="YYYY-MM-DD"
              :placeholder="t('stockOutDetail.pickDate')"
              :teleported="false"
              style="width: 100%"
            />
          </el-form-item>
          <el-form-item :label="t('stockOutDetail.shipmentMethod')">
            <el-select
              v-model="editForm.shipmentMethod"
              clearable
              filterable
              :placeholder="t('stockOutDetail.shipmentPlaceholder')"
              :teleported="false"
              style="width: 100%"
            >
              <el-option v-for="o in shipmentArrivalOptions" :key="o.value" :label="o.label" :value="o.value" />
            </el-select>
          </el-form-item>
          <el-form-item :label="t('stockOutDetail.courierTrackingNo')">
            <el-input
              v-model="editForm.courierTrackingNo"
              clearable
              :placeholder="t('stockOutDetail.trackingPlaceholder')"
            />
          </el-form-item>
        </el-form>
      </div>
      <template #footer>
        <el-button @click="editDialogVisible = false">{{ t('common.cancel') }}</el-button>
        <el-button type="primary" :loading="editSubmitting" @click="() => void submitEditHeader()">
          {{ t('common.confirm') }}
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import { Setting } from '@element-plus/icons-vue'
import { stockOutApi, type StockOutDto, type StockOutListQuery, type StockOutMarkFinishContext } from '@/api/stockOut'
import { formatDisplayDateTime } from '@/utils/displayDateTime'
import { buildStockOutListColumns } from '@/composables/buildStockOutListColumns'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'
import { useSaleSensitiveFieldMask } from '@/composables/useSaleSensitiveFieldMask'
import { useDepartmentDataReadOnly } from '@/composables/useDepartmentDataReadOnly'
import { useLogisticsFormDict } from '@/composables/useLogisticsFormDict'
import { useAuthStore } from '@/stores/auth'
import StockBizTypeTag from '@/components/Inventory/StockBizTypeTag.vue'
import CustomerExtendColumnHeader from '@/components/list/CustomerExtendColumnHeader.vue'
import CustomerExtendCell from '@/components/list/CustomerExtendCell.vue'
import { useCustomerExtendColumn, isCustomerExtendTableColumn } from '@/composables/useCustomerExtendColumn'
import { StockOutTypeCode, STOCK_OUT_TYPE_FILTER_VALUES } from '@/constants/stockOutType'

const { maskSaleSensitiveFields } = useSaleSensitiveFieldMask()
const {
  expanded: customerExtendExpanded,
  activeField: customerExtendActiveField,
  colWidth: customerExtendColWidth,
  colMinWidth: customerExtendColMinWidth,
  setActiveField: setCustomerExtendActiveField,
  applyOuterWidthFromTable: applyCustomerExtendOuterWidth
} = useCustomerExtendColumn()

function onStockOutTableHeaderDragEnd(
  newWidth: number,
  _oldWidth: number,
  column: { property?: string; label?: string }
) {
  if (!isCustomerExtendTableColumn(column)) return
  applyCustomerExtendOuterWidth(newWidth)
}

const route = useRoute()
const router = useRouter()
const { t } = useI18n()
const authStore = useAuthStore()
const { canWriteLogisticsData } = useDepartmentDataReadOnly()
const { ensureLoaded: ensureLogisticsDict, shipmentArrivalOptions, expressOptions } = useLogisticsFormDict()
const isSysAdmin = computed(() => authStore.user?.isSysAdmin === true)
const loading = ref(false)
const list = ref<StockOutDto[]>([])
const listTotal = ref(0)
const listPage = ref(1)
const listPageSize = ref(20)
const filterForm = reactive({
  status: undefined as number | undefined,
  stockOutType: undefined as number | undefined,
  stockOutCode: '',
  packingCode: '',
  freightForwarderOrderNo: '',
  shipmentMethod: '',
  customerName: '',
  salesUserName: '',
  remark: '',
  stockOutDateRange: null as [string, string] | null
})
const statusFilterOptions = computed(() => [
  { value: 0, label: t('stockOutList.status.draft') },
  { value: 1, label: t('stockOutList.status.pending') },
  { value: 2, label: t('stockOutList.status.done') },
  { value: 3, label: t('stockOutList.status.cancelled') },
  { value: 4, label: t('stockOutList.status.finished') }
])
const dataTableRef = ref<{ openColumnSettings?: () => void } | null>(null)
const rowDensityToggleAnchorEl = ref<HTMLElement | null>(null)

const opColExpanded = ref(false)
const OP_COL_COLLAPSED_WIDTH = 43
const OP_COL_EXPANDED_WIDTH = 168
const OP_COL_EXPANDED_MIN_WIDTH = 156
const opColWidth = computed(() => (opColExpanded.value ? OP_COL_EXPANDED_WIDTH : OP_COL_COLLAPSED_WIDTH))
const opColMinWidth = computed(() =>
  opColExpanded.value ? OP_COL_EXPANDED_MIN_WIDTH : OP_COL_COLLAPSED_WIDTH
)
function toggleOpCol() {
  opColExpanded.value = !opColExpanded.value
}

const markFinishDialogVisible = ref(false)
const markFinishLoading = ref(false)
const markFinishSubmitting = ref(false)
const markFinishTargetId = ref('')
const markFinishContext = ref<StockOutMarkFinishContext | null>(null)
const markFinishForm = reactive({
  stockOutDate: '',
  courierTrackingNo: '',
  remark: ''
})

const editDialogVisible = ref(false)
const editLoading = ref(false)
const editSubmitting = ref(false)
const editTargetId = ref('')
const editForm = reactive({
  stockOutDate: '',
  shipmentMethod: '',
  courierTrackingNo: ''
})

const canSubmitMarkFinish = computed(
  () =>
    Boolean(markFinishForm.stockOutDate?.trim()) && Boolean(markFinishForm.courierTrackingNo?.trim())
)

function displayOrDash(value: string | null | undefined): string {
  const s = String(value ?? '').trim()
  return s || t('quoteList.na')
}

function resetMarkFinishDialog() {
  markFinishTargetId.value = ''
  markFinishContext.value = null
  markFinishForm.stockOutDate = ''
  markFinishForm.courierTrackingNo = ''
  markFinishForm.remark = ''
  markFinishLoading.value = false
  markFinishSubmitting.value = false
}

function resetEditDialog() {
  editTargetId.value = ''
  editForm.stockOutDate = ''
  editForm.shipmentMethod = ''
  editForm.courierTrackingNo = ''
  editLoading.value = false
  editSubmitting.value = false
}

const stockOutTableColumns = computed<CrmTableColumnDef[]>(() => {
  void customerExtendExpanded.value
  void customerExtendColWidth.value
  return buildStockOutListColumns({
    t,
    opColWidth: opColWidth.value,
    opColMinWidth: opColMinWidth.value,
    withSelection: false,
    withActions: true,
    withCustomerExtend: true,
    customerExtendColWidth: customerExtendColWidth.value,
    customerExtendColMinWidth: customerExtendColMinWidth.value
  })
})

function syncFiltersFromRoute() {
  if (route.name !== 'StockOutList') return
  const q = route.query
  const legacyKeyword = typeof q.keyword === 'string' ? q.keyword.trim() : ''
  filterForm.stockOutCode = typeof q.stockOutCode === 'string' ? q.stockOutCode : legacyKeyword
  filterForm.packingCode = typeof q.packingCode === 'string' ? q.packingCode : ''
  filterForm.freightForwarderOrderNo =
    typeof q.freightForwarderOrderNo === 'string' ? q.freightForwarderOrderNo : ''
  filterForm.shipmentMethod = typeof q.shipmentMethod === 'string' ? q.shipmentMethod : ''
  filterForm.customerName = typeof q.customerName === 'string' ? q.customerName : ''
  filterForm.salesUserName = typeof q.salesUserName === 'string' ? q.salesUserName : ''
  filterForm.remark = typeof q.remark === 'string' ? q.remark : ''
  const statusRaw = q.status
  filterForm.status =
    statusRaw === undefined || statusRaw === null || statusRaw === ''
      ? undefined
      : Number(statusRaw)
  const from = typeof q.stockOutDateFrom === 'string' ? q.stockOutDateFrom : ''
  const to = typeof q.stockOutDateTo === 'string' ? q.stockOutDateTo : ''
  filterForm.stockOutDateRange = from && to ? [from, to] : null
  const typeRaw = q.stockOutType
  filterForm.stockOutType =
    typeRaw === undefined || typeRaw === null || typeRaw === ''
      ? undefined
      : Number(typeRaw)
}

function listStockOutTypeLabel(type: number | undefined | null): string {
  const n = Number(type)
  if (n === StockOutTypeCode.Customs) return t('stockOutList.stockOutTypeLabels.customs')
  if (n === StockOutTypeCode.Return) return t('stockOutList.stockOutTypeLabels.return')
  if (n === StockOutTypeCode.Scrap) return t('stockOutList.stockOutTypeLabels.scrap')
  return t('stockOutList.stockOutTypeLabels.sales')
}

function isCustomsStockOut(row: StockOutDto): boolean {
  return Number(row.stockOutType) === StockOutTypeCode.Customs
}

function salesNotifyTooltip(row: StockOutDto): string {
  const code = String(row.salesStockOutNotifyCode ?? '').trim()
  if (!code) return ''
  return t('stockOutList.salesNotifyCodeTooltip', { code })
}

function buildListQuery(): StockOutListQuery {
  const q: StockOutListQuery = {
    page: listPage.value,
    pageSize: listPageSize.value
  }
  if (filterForm.status !== undefined && !Number.isNaN(filterForm.status)) q.status = filterForm.status
  if (filterForm.stockOutType !== undefined && !Number.isNaN(filterForm.stockOutType)) {
    q.stockOutType = filterForm.stockOutType
  }
  const code = filterForm.stockOutCode.trim()
  if (code) q.stockOutCode = code
  const packing = filterForm.packingCode.trim()
  if (packing) q.packingCode = packing
  const ff = filterForm.freightForwarderOrderNo.trim()
  if (ff) q.freightForwarderOrderNo = ff
  const ship = filterForm.shipmentMethod.trim()
  if (ship) q.shipmentMethod = ship
  const cust = filterForm.customerName.trim()
  if (cust) q.customerName = cust
  const sales = filterForm.salesUserName.trim()
  if (sales) q.salesUserName = sales
  const remark = filterForm.remark.trim()
  if (remark) q.remark = remark
  if (filterForm.stockOutDateRange?.length === 2) {
    q.stockOutDateFrom = filterForm.stockOutDateRange[0]
    q.stockOutDateTo = filterForm.stockOutDateRange[1]
  }
  return q
}

function buildRouteQuery(): Record<string, string> {
  const q: Record<string, string> = {}
  if (filterForm.status !== undefined && !Number.isNaN(filterForm.status)) q.status = String(filterForm.status)
  if (filterForm.stockOutType !== undefined && !Number.isNaN(filterForm.stockOutType)) {
    q.stockOutType = String(filterForm.stockOutType)
  }
  const code = filterForm.stockOutCode.trim()
  if (code) q.stockOutCode = code
  const packing = filterForm.packingCode.trim()
  if (packing) q.packingCode = packing
  const ff = filterForm.freightForwarderOrderNo.trim()
  if (ff) q.freightForwarderOrderNo = ff
  const ship = filterForm.shipmentMethod.trim()
  if (ship) q.shipmentMethod = ship
  const cust = filterForm.customerName.trim()
  if (cust) q.customerName = cust
  const sales = filterForm.salesUserName.trim()
  if (sales) q.salesUserName = sales
  const remark = filterForm.remark.trim()
  if (remark) q.remark = remark
  if (filterForm.stockOutDateRange?.length === 2) {
    q.stockOutDateFrom = filterForm.stockOutDateRange[0]
    q.stockOutDateTo = filterForm.stockOutDateRange[1]
  }
  return q
}

watch(
  () => route.query,
  () => {
    syncFiltersFromRoute()
    void runStockOutListFetch(true)
  },
  { deep: true, immediate: true }
)

const formatDate = (v?: string | null) => formatDisplayDateTime(v || undefined)
const formatPackingCount = (v?: number | null) =>
  v == null || Number.isNaN(Number(v)) ? t('quoteList.na') : String(Number(v))

/** LogisticsArrivalMethod ItemCode → 字典显示名 */
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
  if (code === null || code === undefined || code === '') return t('quoteList.na')
  const c = String(code).trim()
  if (!c) return t('quoteList.na')
  return arrivalLabelByCode.value.get(c.toLowerCase()) ?? c
}

function expressCompanyDisplay(code?: string | null): string {
  const c = String(code ?? '').trim()
  if (!c) return t('quoteList.na')
  return expressLabelByCode.value.get(c.toLowerCase()) ?? c
}

onMounted(async () => {
  try {
    await ensureLogisticsDict()
  } catch {
    /* 字典失败时 shipmentMethodDisplay 仍回退为原始码 */
  }
})

const statusLabel = (s: number) => {
  switch (s) {
    case 0:
      return t('stockOutList.status.draft')
    case 1:
      return t('stockOutList.status.pending')
    case 2:
      return t('stockOutList.status.done')
    case 3:
      return t('stockOutList.status.cancelled')
    case 4:
      return t('stockOutList.status.finished')
    default:
      return t('rfqDetail.unknown')
  }
}

watch(listTotal, () => {
  const maxP = Math.max(1, Math.ceil(listTotal.value / listPageSize.value) || 1)
  if (listPage.value > maxP) listPage.value = maxP
})

async function runStockOutListFetch(resetPage: boolean) {
  if (resetPage) listPage.value = 1
  loading.value = true
  try {
    const res = await stockOutApi.getListPaged(buildListQuery())
    list.value = res.items
    listTotal.value = res.total
  } catch (e) {
    console.error(e)
    ElMessage.error(t('stockOutList.messages.loadFailed'))
  } finally {
    loading.value = false
  }
}

function onStockOutListPageSizeChange() {
  listPage.value = 1
  void runStockOutListFetch(false)
}

const handleSearch = () => {
  router.replace({ name: 'StockOutList', query: buildRouteQuery() })
}

function handleReset() {
  filterForm.status = undefined
  filterForm.stockOutType = undefined
  filterForm.stockOutCode = ''
  filterForm.packingCode = ''
  filterForm.freightForwarderOrderNo = ''
  filterForm.shipmentMethod = ''
  filterForm.customerName = ''
  filterForm.salesUserName = ''
  filterForm.remark = ''
  filterForm.stockOutDateRange = null
  router.replace({ name: 'StockOutList', query: {} })
}

function goDetail(row: StockOutDto) {
  if (!row?.id) return
  router.push({ name: 'StockOutDetail', params: { id: row.id } })
}

function onRowDblclick(row: StockOutDto) {
  goDetail(row)
}

async function handleEdit(row: StockOutDto) {
  if (!row?.id) return
  editTargetId.value = row.id
  editDialogVisible.value = true
  editLoading.value = true
  editForm.stockOutDate = ''
  editForm.shipmentMethod = ''
  editForm.courierTrackingNo = ''
  try {
    await ensureLogisticsDict()
    const d = await stockOutApi.getById(row.id)
    if (!d) {
      ElMessage.error(t('stockOutDetail.notFound'))
      editDialogVisible.value = false
      return
    }
    editForm.stockOutDate = d.stockOutDate ? String(d.stockOutDate).slice(0, 10) : ''
    editForm.shipmentMethod = d.shipmentMethod?.trim() || ''
    editForm.courierTrackingNo = d.courierTrackingNo?.trim() || ''
  } catch (e) {
    console.error(e)
    ElMessage.error(t('stockOutList.editDialog.loadFailed'))
    editDialogVisible.value = false
  } finally {
    editLoading.value = false
  }
}

async function submitEditHeader() {
  if (!editTargetId.value) return
  if (!editForm.stockOutDate?.trim()) {
    ElMessage.warning(t('stockOutDetail.needDate'))
    return
  }
  editSubmitting.value = true
  try {
    const dateIso = `${editForm.stockOutDate.trim()}T00:00:00.000Z`
    await stockOutApi.updateHeader(editTargetId.value, {
      stockOutDate: dateIso,
      shipmentMethod: editForm.shipmentMethod?.trim() || null,
      courierTrackingNo: editForm.courierTrackingNo?.trim() || null
    })
    ElMessage.success(t('stockOutDetail.saveOk'))
    editDialogVisible.value = false
    await runStockOutListFetch(false)
  } catch (e: unknown) {
    console.error(e)
    const err = e as { response?: { data?: { message?: string } }; message?: string }
    ElMessage.error(err?.response?.data?.message || err?.message || t('stockOutDetail.saveFail'))
  } finally {
    editSubmitting.value = false
  }
}

const handleMarkFinish = async (row: StockOutDto) => {
  if (!row?.id) return
  markFinishTargetId.value = row.id
  markFinishDialogVisible.value = true
  markFinishLoading.value = true
  markFinishContext.value = null
  markFinishForm.stockOutDate = ''
  markFinishForm.courierTrackingNo = ''
  markFinishForm.remark = ''
  try {
    const ctx = await stockOutApi.getMarkFinishContext(row.id)
    markFinishContext.value = ctx
    markFinishForm.stockOutDate = ctx.stockOutDate ? String(ctx.stockOutDate).slice(0, 10) : ''
    markFinishForm.courierTrackingNo = ctx.courierTrackingNo?.trim() || ''
    markFinishForm.remark = ctx.remark?.trim() || ''
  } catch (e) {
    console.error(e)
    ElMessage.error(t('stockOutList.markFinish.loadContextFailed'))
    markFinishDialogVisible.value = false
  } finally {
    markFinishLoading.value = false
  }
}

async function submitMarkFinish() {
  if (!markFinishTargetId.value || !canSubmitMarkFinish.value) return
  markFinishSubmitting.value = true
  try {
    await stockOutApi.markFinished(markFinishTargetId.value, {
      stockOutDate: markFinishForm.stockOutDate,
      courierTrackingNo: markFinishForm.courierTrackingNo.trim(),
      remark: markFinishForm.remark.trim() || undefined
    })
    ElMessage.success(t('stockOutList.messages.markFinishedSuccess'))
    markFinishDialogVisible.value = false
    await runStockOutListFetch(false)
  } catch (e) {
    console.error(e)
    ElMessage.error(t('stockOutList.messages.updateStatusFailed'))
  } finally {
    markFinishSubmitting.value = false
  }
}

const handleDeleteRow = async (row: StockOutDto) => {
  const ok = window.confirm(`确认删除出库单 ${row.stockOutCode} 吗？`)
  if (!ok) return
  try {
    await stockOutApi.deleteStockOut(row.id)
    ElMessage.success('删除成功')
    await runStockOutListFetch(false)
  } catch (e) {
    console.error(e)
    ElMessage.error(e instanceof Error ? e.message : '删除失败')
  }
}

const handleForceDeleteRow = async (row: StockOutDto) => {
  const entered = window.prompt('请输入出库单号以确认强制删除', row.stockOutCode || '')?.trim() ?? ''
  if (!entered) return
  if (entered !== String(row.stockOutCode || '').trim()) {
    ElMessage.error('输入单号不匹配，已取消')
    return
  }
  try {
    await stockOutApi.forceDeleteStockOut(row.id, entered)
    ElMessage.success('强制删除成功')
    await runStockOutListFetch(false)
  } catch (e) {
    console.error(e)
    ElMessage.error(e instanceof Error ? e.message : '强制删除失败')
  }
}

</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

/* 与 StockOutNotifyList.vue 的 .stockout-notify-page 同一套布局 */
.stockout-list-page {
  padding: 24px;
  min-height: 100%;
  background: $layer-1;
}
.page-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 12px;
}
.header-left {
  display: flex;
  align-items: center;
  gap: 10px;
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

.filter-select {
  width: 130px;
}

.filter-select--shipment {
  width: 150px;
}

.search-input {
  width: 140px;
  padding: 7px 12px;
  background: $layer-2;
  border: 1px solid $border-panel;
  border-radius: $border-radius-md;
  color: $text-primary;
  font-size: 13px;
}

.search-input--code {
  width: 150px;
}

.search-input--customer,
.search-input--sales {
  width: 130px;
}

.search-input--remark {
  width: 160px;
}

.filter-date-range {
  max-width: 280px;
}

.btn-primary.btn-sm,
.btn-ghost.btn-sm {
  padding: 7px 14px;
  font-size: 13px;
  border-radius: $border-radius-md;
  cursor: pointer;
}

.btn-primary.btn-sm {
  background: rgba(0, 212, 255, 0.15);
  border: 1px solid rgba(0, 212, 255, 0.35);
  color: $cyan-primary;
}

.btn-ghost.btn-sm {
  background: transparent;
  border: 1px solid $border-panel;
  color: $text-secondary;
}

.mono-cell {
  font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace;
  font-size: 12px;
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
.btn-secondary {
  padding: 8px 14px;
  border-radius: $border-radius-md;
  border: 1px solid $border-panel;
  color: $text-secondary;
  font-size: 13px;
  background: rgba(255, 255, 255, 0.05);
  cursor: pointer;
}
.text-secondary {
  color: $text-muted;
}
.status-badge {
  display: inline-block;
  padding: 2px 8px;
  border-radius: 4px;
  font-size: 12px;
  &.status-0 {
    background: rgba(255, 255, 255, 0.05);
    color: $text-muted;
  }
  &.status-1 {
    background: rgba(255, 193, 7, 0.15);
    color: #ffc107;
  }
  &.status-2 {
    background: rgba(70, 191, 145, 0.18);
    color: #46bf91;
  }
  &.status-3 {
    background: rgba(201, 87, 69, 0.18);
    color: #c95745;
  }
  &.status-4 {
    background: rgba(0, 212, 255, 0.18);
    color: $cyan-primary;
  }
}
.action-btn {
  background: transparent;
  border: none;
  color: $cyan-primary;
  cursor: pointer;
  font-size: 12px;
  padding: 2px 6px;
  &:hover {
    text-decoration: underline;
  }
}

.action-btn--dropdown {
  display: inline-flex;
  align-items: center;
  gap: 0;
}

.action-btn__caret {
  font-size: 11px;
  margin-left: 2px;
}

.op-submenu-title {
  font-size: 12px;
  color: $text-muted;
}

.op-more-item--sub {
  padding-left: 8px;
}

.pagination-wrapper {
  margin-top: 12px;
  display: flex;
  align-items: flex-start;
  justify-content: flex-start;
  flex-wrap: wrap;
  gap: 12px 16px;
}

.list-main-pagination {
  margin-left: auto;
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
.op-more-item--warning {
  color: $color-amber;
}

.stock-out-mark-finish-dialog__info {
  margin: 0 0 16px;
}

.stock-out-mark-finish-dialog__row {
  display: grid;
  grid-template-columns: 108px 1fr;
  gap: 8px 12px;
  margin-bottom: 10px;
  font-size: 13px;
}

.stock-out-mark-finish-dialog__row dt {
  margin: 0;
  color: $text-muted;
}

.stock-out-mark-finish-dialog__row dd {
  margin: 0;
  color: $text-secondary;
  word-break: break-word;
}

.stock-out-mark-finish-dialog__packing-list {
  margin: 6px 0 0;
  padding-left: 18px;
}

.stock-out-mark-finish-dialog__form {
  display: flex;
  flex-direction: column;
  gap: 14px;
  padding-top: 4px;
  border-top: 1px solid $border-panel;
}

.stock-out-mark-finish-dialog__label {
  display: flex;
  flex-direction: column;
  gap: 6px;
  font-size: 13px;
  color: $text-secondary;
}

.stock-out-mark-finish-dialog__field {
  width: 100%;
}

.stock-out-code-cell {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  max-width: 100%;
}

.customs-notify-tag {
  flex: 0 0 auto;
  padding: 1px 6px;
  border-radius: 4px;
  font-size: 11px;
  line-height: 1.4;
  color: #ffb84d;
  background: rgba(255, 184, 77, 0.14);
  border: 1px solid rgba(255, 184, 77, 0.45);
  cursor: default;
  user-select: none;
}

</style>
