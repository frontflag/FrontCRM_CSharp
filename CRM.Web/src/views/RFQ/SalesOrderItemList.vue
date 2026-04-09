<template>
  <div class="so-item-list-page">
    <div class="page-header">
      <div class="header-left">
        <div class="page-title-group">
          <div class="page-icon">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
              <path d="M8 6h13M8 12h13M8 18h13M3 6h.01M3 12h.01M3 18h.01" />
            </svg>
          </div>
          <h1 class="page-title">{{ t('salesOrderItemList.title') }}</h1>
        </div>
        <div class="list-count-badge">{{ t('salesOrderItemList.count', { count: total }) }}</div>
      </div>
    </div>

    <div class="search-bar">
      <div class="search-left">
        <span class="list-title">{{ t('salesOrderItemList.filters.title') }}</span>
        <el-date-picker
          v-model="dateRange"
          type="daterange"
          :range-separator="t('salesOrderItemList.filters.rangeTo')"
          :start-placeholder="t('salesOrderItemList.filters.dateStart')"
          :end-placeholder="t('salesOrderItemList.filters.dateEnd')"
          value-format="YYYY-MM-DD"
          class="so-date-range"
          clearable
        />
        <input
          v-model="filters.sellOrderCode"
          class="search-input so-filter-input"
          :placeholder="t('salesOrderItemList.filters.sellOrderCode')"
          @keyup.enter="loadList"
        />
        <input
          v-if="canViewCustomer"
          v-model="filters.customerName"
          class="search-input so-filter-input"
          :placeholder="t('salesOrderItemList.filters.customerName')"
          @keyup.enter="loadList"
        />
        <input
          v-model="filters.salesUserName"
          class="search-input so-filter-input"
          :placeholder="t('salesOrderItemList.filters.salesUserName')"
          @keyup.enter="loadList"
        />
        <input
          v-model="filters.pn"
          class="search-input so-filter-input"
          :placeholder="t('salesOrderItemList.filters.pn')"
          @keyup.enter="loadList"
        />
        <button type="button" class="btn-primary btn-sm" @click="loadList">{{ t('salesOrderItemList.filters.query') }}</button>
        <button type="button" class="btn-ghost btn-sm" @click="resetFilters">{{ t('salesOrderItemList.filters.reset') }}</button>
      </div>
    </div>

    <CrmDataTable
      ref="dataTableRef"
      class="quantum-table-block el-table-host"
      column-layout-key="sales-order-item-list-v2"
      :columns="salesOrderItemColumns"
      :show-column-settings="false"
      :data="list"
      v-loading="loading"
      row-key="sellOrderItemId"
      @selection-change="onSelectionChange"
      @row-dblclick="goDetail"
    >
      <template #col-orderStatus="{ row }">
        <el-tag effect="dark" :type="statusTagType(row.orderStatus)" size="small">{{ statusText(row.orderStatus) }}</el-tag>
      </template>
      <template #col-purchaseProgressStatus="{ row }">
        <el-tag effect="dark" :type="extendTriTagType(row.purchaseProgressStatus)" size="small">
          {{ extendTriLabel('purchase', row.purchaseProgressStatus) }}
        </el-tag>
      </template>
      <template #col-stockInProgressStatus="{ row }">
        <el-tag effect="dark" :type="extendTriTagType(row.stockInProgressStatus)" size="small">
          {{ extendTriLabel('stockIn', row.stockInProgressStatus) }}
        </el-tag>
      </template>
      <template #col-stockOutProgressStatus="{ row }">
        <el-tag effect="dark" :type="extendTriTagType(row.stockOutProgressStatus)" size="small">
          {{ extendTriLabel('stockOut', row.stockOutProgressStatus) }}
        </el-tag>
      </template>
      <template #col-receiptProgressStatus="{ row }">
        <el-tag effect="dark" :type="extendTriTagType(row.receiptProgressStatus)" size="small">
          {{ extendTriLabel('receipt', row.receiptProgressStatus) }}
        </el-tag>
      </template>
      <template #col-invoiceProgressStatus="{ row }">
        <el-tag effect="dark" :type="extendTriTagType(row.invoiceProgressStatus)" size="small">
          {{ extendTriLabel('invoice', row.invoiceProgressStatus) }}
        </el-tag>
      </template>
      <template #col-orderCreateTime="{ row }">{{ formatDt(row.orderCreateTime) }}</template>
      <template #col-currency="{ row }">{{ settlementCurrencyLabel(row.currency) }}</template>
      <template #col-price="{ row }">{{ formatUnitPriceNumber(row.price) }}</template>
      <template #col-lineTotal="{ row }">{{ formatTotalAmountNumber(row.lineTotal) }}</template>
      <template #col-usdUnitPrice="{ row }">{{ row.usdUnitPrice != null ? `$${Number(row.usdUnitPrice).toFixed(6)}` : '—' }}</template>
      <template #col-usdLineTotal="{ row }">{{ row.usdLineTotal != null ? `$${Number(row.usdLineTotal).toFixed(2)}` : '—' }}</template>
      <template #col-salesProfitExpected="{ row }">{{
        row.salesProfitExpected != null ? `$${Number(row.salesProfitExpected).toFixed(2)}` : '—'
      }}</template>
      <template #col-profitOutBizUsd="{ row }">{{
        row.profitOutBizUsd != null ? `$${Number(row.profitOutBizUsd).toFixed(2)}` : '—'
      }}</template>
      <template #col-profitOutRateBiz="{ row }">{{
        row.profitOutRateBiz != null ? Number(row.profitOutRateBiz).toFixed(6) : '—'
      }}</template>
      <template #col-createTime="{ row }">{{ formatDt(row.createTime || row.orderCreateTime) }}</template>
      <template #col-createUser="{ row }">{{ row.createUserName || row.createdBy || row.salesUserName || '—' }}</template>
      <template #col-actions-header>
        <div class="op-col-header">
          <span class="op-col-header-text">{{ t('salesOrderItemList.columns.actions') }}</span>
          <button type="button" class="op-col-toggle-btn" @click.stop="toggleOpCol">
            {{ opColExpanded ? '>' : '<' }}
          </button>
        </div>
      </template>
      <template #col-actions="{ row }">
        <div @click.stop @dblclick.stop>
          <div v-if="opColExpanded" class="action-btns">
            <el-button link type="primary" size="small" @click.stop="goDetail(row)">{{ t('salesOrderItemList.actions.detail') }}</el-button>
            <el-button v-if="canWriteSo" link type="primary" size="small" @click.stop="goEdit(row)">{{ t('salesOrderItemList.actions.edit') }}</el-button>
            <el-button
              v-if="canPurchaseReq && mainAllowsOps(row)"
              link
              type="warning"
              size="small"
              :disabled="applyPurchaseDisabled(row)"
              @click.stop="applyPurchaseOne(row)"
            >
              {{ t('salesOrderItemList.actions.applyPurchase') }}
            </el-button>
            <el-button
              v-if="canWriteSo && mainAllowsOps(row)"
              link
              type="warning"
              size="small"
              :disabled="applyStockOutDisabled(row) || !stockOutApplyPurchaseGateOk(row)"
              @click.stop="applyStockOutOne(row)"
            >
              {{ t('salesOrderItemList.actions.applyStockOut') }}
            </el-button>
          </div>

          <el-dropdown v-else trigger="click" placement="bottom-end">
            <div class="op-more-dropdown-trigger">
              <button type="button" class="op-more-trigger">...</button>
            </div>
            <template #dropdown>
              <el-dropdown-menu>
                <el-dropdown-item @click.stop="goDetail(row)">
                  <span class="op-more-item op-more-item--primary">{{ t('salesOrderItemList.actions.detail') }}</span>
                </el-dropdown-item>
                <el-dropdown-item v-if="canWriteSo" @click.stop="goEdit(row)">
                  <span class="op-more-item op-more-item--primary">{{ t('salesOrderItemList.actions.edit') }}</span>
                </el-dropdown-item>
                <el-dropdown-item
                  v-if="canPurchaseReq && mainAllowsOps(row)"
                  :disabled="applyPurchaseDisabled(row)"
                  @click.stop="applyPurchaseOne(row)"
                >
                  <span
                    class="op-more-item"
                    :class="applyPurchaseDisabled(row) ? 'op-more-item--disabled' : 'op-more-item--warning'"
                  >{{ t('salesOrderItemList.actions.applyPurchase') }}</span>
                </el-dropdown-item>
                <el-dropdown-item
                  v-if="canWriteSo && mainAllowsOps(row)"
                  :disabled="applyStockOutDisabled(row) || !stockOutApplyPurchaseGateOk(row)"
                  @click.stop="applyStockOutOne(row)"
                >
                  <span
                    class="op-more-item"
                    :class="
                      applyStockOutDisabled(row) || !stockOutApplyPurchaseGateOk(row)
                        ? 'op-more-item--disabled'
                        : 'op-more-item--warning'
                    "
                  >{{ t('salesOrderItemList.actions.applyStockOut') }}</span>
                </el-dropdown-item>
              </el-dropdown-menu>
            </template>
          </el-dropdown>
        </div>
      </template>
    </CrmDataTable>

    <div v-if="total > 0" class="table-footer-bar">
      <div class="basket-footer-left">
        <el-tooltip :content="t('salesOrderItemList.columnSettings')" placement="top" :hide-after="0">
          <el-button
            class="list-settings-btn"
            link
            type="primary"
            :aria-label="t('salesOrderItemList.columnSettings')"
            @click="dataTableRef?.openColumnSettings?.()"
          >
            <el-icon><Setting /></el-icon>
          </el-button>
        </el-tooltip>
        <div class="list-footer-spacer" aria-hidden="true"></div>
        <el-button class="basket-open-btn" link type="primary" @click="basketDrawerVisible = true">
          {{ t('salesOrderItemList.basket.open') }}<span v-if="basketCount" class="basket-count-label">（{{ basketCount }}）</span>
        </el-button>
        <el-button
          v-if="basketCount"
          class="basket-clear-btn"
          link
          type="warning"
          @click="handleClearBasket"
        >
          {{ t('salesOrderItemList.basket.clear') }}
        </el-button>
        <button
          v-if="canPurchaseReq"
          type="button"
          class="btn-primary btn-sm basket-batch-purchase-btn"
          :disabled="!basketCount || !basketItems.every((r) => mainAllowsOps(r))"
          @click="batchApplyPurchase"
        >
          {{ t('salesOrderItemList.basket.batchPurchase') }}
        </button>
      </div>
      <el-pagination
        v-model:current-page="page"
        v-model:page-size="pageSize"
        :total="total"
        :page-sizes="[10, 20, 50]"
        layout="total, prev, pager, next, sizes"
        class="quantum-pagination"
        @current-change="loadList"
        @size-change="loadList"
      />
    </div>

    <el-drawer
      v-model="basketDrawerVisible"
      :title="t('salesOrderItemList.basket.drawerTitle')"
      direction="rtl"
      size="min(560px, 94vw)"
      class="so-item-basket-drawer"
    >
      <p v-if="!basketCount" class="basket-drawer-hint">{{ t('salesOrderItemList.basket.emptyHint') }}</p>
      <template v-else>
        <p class="basket-drawer-summary">
          {{ t('salesOrderItemList.basket.summaryBeforeBtn', { count: basketCount }) }}
          <el-button
            class="basket-clear-btn basket-clear-btn--drawer-inline"
            link
            type="warning"
            @click="handleClearBasket"
          >
            {{ t('salesOrderItemList.basket.clear') }}
          </el-button>
          {{ t('salesOrderItemList.basket.summaryAfterBtn') }}
        </p>
        <div class="crm-items-table crm-data-table">
          <el-table :data="basketItems" max-height="70vh" size="small" border stripe>
            <el-table-column
              prop="sellOrderCode"
              :label="t('salesOrderItemList.columns.sellOrderCode')"
              min-width="140"
              show-overflow-tooltip
            />
            <el-table-column :label="t('salesOrderItemList.columns.status')" width="100" align="center">
              <template #default="{ row }">
                <el-tag effect="dark" :type="statusTagType(Number(row.orderStatus))" size="small">
                  {{ statusText(Number(row.orderStatus)) }}
                </el-tag>
              </template>
            </el-table-column>
            <el-table-column
              v-if="canViewCustomer"
              prop="customerName"
              :label="t('salesOrderItemList.columns.customerName')"
              min-width="120"
              show-overflow-tooltip
            />
            <el-table-column prop="pn" :label="t('salesOrderItemList.columns.pn')" min-width="130" show-overflow-tooltip />
            <el-table-column prop="qty" :label="t('salesOrderItemList.columns.qty')" width="72" align="right" />
            <el-table-column
              :label="t('salesOrderItemList.columns.actions')"
              width="88"
              fixed="right"
              align="center"
              class-name="op-col"
              label-class-name="op-col"
            >
              <template #default="{ row }">
                <div @click.stop @dblclick.stop>
                  <div class="action-btns">
                    <el-button
                      link
                      type="danger"
                      size="small"
                      @click.stop="removeOneFromBasket(String(row.sellOrderItemId ?? ''))"
                    >
                      {{ t('salesOrderItemList.actions.remove') }}
                    </el-button>
                  </div>
                </div>
              </template>
            </el-table-column>
          </el-table>
        </div>
      </template>
    </el-drawer>

    <!-- 新建采购申请弹窗 -->
    <el-dialog v-model="applyDialogVisible" :title="t('salesOrderItemList.dialog.createPrTitle')" width="720px" destroy-on-close>
      <el-form ref="applyFormRef" :model="applyForm" :rules="applyRules" label-width="140px" v-loading="applyLoading">
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item :label="t('salesOrderItemList.dialog.pn')">
              <el-input v-model="applyForm.pn" disabled />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item :label="t('salesOrderItemList.dialog.brand')">
              <el-input v-model="applyForm.brand" disabled />
            </el-form-item>
          </el-col>
        </el-row>

        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item :label="t('salesOrderItemList.dialog.orderLineQty')">
              <el-input :model-value="applyFormSalesOrderQtyText" disabled />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item :label="t('salesOrderItemList.dialog.purchasedQty')">
              <el-input :model-value="applyFormPurchasedQtyText" disabled />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item :label="t('salesOrderItemList.dialog.openPrQty')">
              <el-input :model-value="applyFormOpenPrQtyText" disabled />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item :label="t('salesOrderItemList.dialog.availableQty')">
              <el-input :model-value="applyFormRemainingQtyText" disabled />
            </el-form-item>
          </el-col>
        </el-row>

        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item :label="t('salesOrderItemList.dialog.requestQty')" prop="requestQty">
              <el-input-number
                v-model="applyForm.requestQty"
                :min="0"
                :precision="0"
                :step="1"
                :max="applyForm.remainingQty"
                controls-position="right"
                style="width: 100%"
              />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item :label="t('salesOrderItemList.dialog.expectedPurchaseDate')" prop="expectedPurchaseDate">
              <el-date-picker
                v-model="applyForm.expectedPurchaseDate"
                type="date"
                :placeholder="t('salesOrderItemList.dialog.expectedDatePlaceholder')"
                value-format="YYYY-MM-DD"
                style="width: 100%"
              />
            </el-form-item>
          </el-col>
        </el-row>

        <el-form-item :label="t('salesOrderItemList.dialog.remark')">
          <el-input v-model="applyForm.remark" type="textarea" rows="3" :placeholder="t('salesOrderItemList.dialog.remarkPlaceholder')" />
        </el-form-item>
      </el-form>
      <template #footer>
        <span class="dialog-footer">
          <el-button @click="applyDialogVisible = false">{{ t('salesOrderItemList.dialog.cancel') }}</el-button>
          <el-button type="primary" :loading="applySubmitting" @click="submitApply" :disabled="applyLoading">
            {{ t('salesOrderItemList.dialog.confirm') }}
          </el-button>
        </span>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted, nextTick } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { ElMessage, ElMessageBox, type FormInstance, type FormRules } from 'element-plus'
import { Setting } from '@element-plus/icons-vue'
import { storeToRefs } from 'pinia'
import { useAuthStore } from '@/stores/auth'
import { useSalesOrderItemListBasketStore } from '@/stores/salesOrderItemListBasket'
import CrmDataTable from '@/components/CrmDataTable.vue'
import salesOrderApi from '@/api/salesOrder'
import purchaseRequisitionApi from '@/api/purchaseRequisition'
import { runSaveTask, validateElFormOrWarn } from '@/composables/useFormSubmit'
import {
  translateSalesOrderStatus,
  salesOrderStatusTagType,
  salesOrderMainAllowsPurchaseAndStockOut,
  salesOrderLineApplyStockOutDisabled
} from '@/constants/salesOrderStatus'
import { formatDisplayDateTime } from '@/utils/displayDateTime'
import { formatTotalAmountNumber, formatUnitPriceNumber } from '@/utils/moneyFormat'
import { CURRENCY_CODE_TO_TEXT } from '@/constants/currency'
import type { SalesOrderItemLineRow } from '@/stores/salesOrderItemListBasket'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'

const router = useRouter()
const { t, locale } = useI18n()
const authStore = useAuthStore()

const basketStore = useSalesOrderItemListBasketStore()
const { count: basketCount, items: basketItems } = storeToRefs(basketStore)
const suppressBasketMerge = ref(false)
const basketDrawerVisible = ref(false)
const dataTableRef = ref<InstanceType<typeof CrmDataTable> | null>(null)
const canViewCustomer = computed(
  () => authStore.hasPermission('customer.info.read') || authStore.hasPermission('sales-order.read')
)
const canViewAmount = computed(() => authStore.hasPermission('sales.amount.read'))
const canWriteSo = computed(() => authStore.hasPermission('sales-order.write'))
/** 业务员可从销售明细发起采购申请，不必单独持有 purchase-requisition.write */
const canPurchaseReq = computed(
  () =>
    authStore.hasPermission('purchase-requisition.write') ||
    authStore.hasPermission('sales-order.write')
)

function stockOutApplyPurchaseGateOk(row: Record<string, unknown>) {
  return row.stockOutApplyPurchaseGateOk === true
}

function applyStockOutDisabled(row: Record<string, unknown>) {
  return salesOrderLineApplyStockOutDisabled(row)
}

/** 剩余可采为 0 时禁用「申请采购」（与行选项 / 服务端创建校验口径一致） */
function applyPurchaseDisabled(row: Record<string, unknown>) {
  const raw = (row as { purchaseRemainingQty?: unknown }).purchaseRemainingQty
  if (raw === undefined || raw === null) return false
  const n = Number(raw)
  if (!Number.isFinite(n)) return false
  return n <= 0
}

function mainAllowsOps(row: SalesOrderItemLineRow) {
  const os = row['orderStatus']
  return salesOrderMainAllowsPurchaseAndStockOut(Number(os))
}

/** 结算币别编码 → ISO 文案（与 SETTLEMENT_CURRENCY_OPTIONS 一致） */
function settlementCurrencyLabel(code: unknown): string {
  const c = Number(code)
  if (!Number.isFinite(c)) return '—'
  return CURRENCY_CODE_TO_TEXT[c as keyof typeof CURRENCY_CODE_TO_TEXT] ?? '—'
}

const loading = ref(false)
const list = ref<any[]>([])
const total = ref(0)
const page = ref(1)
const pageSize = ref(20)

// 规范：列表进入页面时“操作”列默认处于收起态（Collapsed）
const opColExpanded = ref(false)
const OP_COL_EXPANDED_WIDTH = 280 // 与原始配置一致
// 收起态需要同时显示列头「操作」与「<」按钮；
// 由于 el-table header/cell 默认 padding 较大，这里给一个偏保守的最小宽度，避免被裁剪。
const OP_COL_COLLAPSED_WIDTH = 96
const opColWidth = computed(() => (opColExpanded.value ? OP_COL_EXPANDED_WIDTH : OP_COL_COLLAPSED_WIDTH))
const opColMinWidth = computed(() => (opColExpanded.value ? 260 : OP_COL_COLLAPSED_WIDTH))
function toggleOpCol() {
  opColExpanded.value = !opColExpanded.value
}

const salesOrderItemColumns = computed<CrmTableColumnDef[]>(() => {
  void locale.value
  const cols: CrmTableColumnDef[] = [
    { key: 'selection', type: 'selection', width: 48, reserveSelection: true, fixed: 'left', hideable: false, reorderable: false },
    {
      key: 'sellOrderItemCode',
      label: t('salesOrderItemList.columns.sellOrderItemCode'),
      prop: 'sellOrderItemCode',
      width: 180,
      minWidth: 168,
      showOverflowTooltip: true
    },
    {
      key: 'sellOrderCode',
      label: t('salesOrderItemList.columns.sellOrderCode'),
      prop: 'sellOrderCode',
      width: 160,
      minWidth: 160,
      showOverflowTooltip: true
    },
    { key: 'orderStatus', label: t('salesOrderItemList.columns.status'), prop: 'orderStatus', width: 160, align: 'center' },
    { key: 'orderCreateTime', label: t('salesOrderItemList.columns.orderCreateDate'), prop: 'orderCreateTime', width: 160 },
    { key: 'salesUserName', label: t('salesOrderItemList.columns.salesUser'), prop: 'salesUserName', width: 100, showOverflowTooltip: true },
    { key: 'pn', label: t('salesOrderItemList.columns.pn'), prop: 'pn', minWidth: 130, showOverflowTooltip: true },
    { key: 'brand', label: t('salesOrderItemList.columns.brand'), prop: 'brand', width: 110, showOverflowTooltip: true },
    { key: 'qty', label: t('salesOrderItemList.columns.qty'), prop: 'qty', width: 100, align: 'right' },
    { key: 'createTime', label: t('salesOrderItemList.columns.createTime'), width: 160 },
    { key: 'createUser', label: t('salesOrderItemList.columns.createUser'), width: 120, showOverflowTooltip: true }
  ]
  if (canViewCustomer.value) {
    cols.splice(4, 0, {
      key: 'customerName',
      label: t('salesOrderItemList.columns.customerName'),
      prop: 'customerName',
      minWidth: 200,
      showOverflowTooltip: true
    })
  }
  const currencyColumn: CrmTableColumnDef = {
    key: 'currency',
    label: t('salesOrderItemList.columns.currency'),
    prop: 'currency',
    width: 72,
    minWidth: 72,
    align: 'center',
    hideable: false
  }

  if (canViewAmount.value) {
    cols.splice(cols.length - 2, 0,
      currencyColumn,
      {
        key: 'price',
        label: t('salesOrderItemList.columns.unitPrice'),
        prop: 'price',
        width: 128,
        minWidth: 112,
        align: 'right',
        hideable: false
      },
      {
        key: 'lineTotal',
        label: t('salesOrderItemList.columns.lineTotal'),
        prop: 'lineTotal',
        width: 132,
        minWidth: 120,
        align: 'right',
        hideable: false
      },
      {
        key: 'usdUnitPrice',
        label: t('salesOrderItemList.columns.usdUnitPrice'),
        width: 132,
        minWidth: 120,
        align: 'right',
        hideable: false
      },
      {
        key: 'usdLineTotal',
        label: t('salesOrderItemList.columns.usdLineTotal'),
        width: 132,
        minWidth: 120,
        align: 'right',
        hideable: false
      },
      {
        key: 'salesProfitExpected',
        label: t('salesOrderItemList.columns.salesProfitExpected'),
        prop: 'salesProfitExpected',
        width: 140,
        align: 'right'
      },
      {
        key: 'profitOutBizUsd',
        label: t('salesOrderItemList.columns.profitOutBizUsd'),
        prop: 'profitOutBizUsd',
        width: 120,
        align: 'right'
      },
      {
        key: 'profitOutRateBiz',
        label: t('salesOrderItemList.columns.profitOutRateBiz'),
        prop: 'profitOutRateBiz',
        width: 120,
        align: 'right'
      }
    )
  } else {
    cols.splice(cols.length - 2, 0, currencyColumn)
  }

  // 采购/入库/出库/收款/开票进度列：放在「利润率」之后（无金额列权限时仍在「数量」后）
  const progressFive: CrmTableColumnDef[] = [
    {
      key: 'purchaseProgressStatus',
      label: t('salesOrderItemList.columns.purchaseProgressStatus'),
      prop: 'purchaseProgressStatus',
      width: 108,
      align: 'center'
    },
    {
      key: 'stockInProgressStatus',
      label: t('salesOrderItemList.columns.stockInProgressStatus'),
      prop: 'stockInProgressStatus',
      width: 108,
      align: 'center'
    },
    {
      key: 'stockOutProgressStatus',
      label: t('salesOrderItemList.columns.stockOutProgressStatus'),
      prop: 'stockOutProgressStatus',
      width: 108,
      align: 'center'
    },
    {
      key: 'receiptProgressStatus',
      label: t('salesOrderItemList.columns.receiptProgressStatus'),
      prop: 'receiptProgressStatus',
      width: 108,
      align: 'center'
    },
    {
      key: 'invoiceProgressStatus',
      label: t('salesOrderItemList.columns.invoiceProgressStatus'),
      prop: 'invoiceProgressStatus',
      width: 108,
      align: 'center'
    }
  ]
  const profitIdx = cols.findIndex((c) => c.key === 'profitOutRateBiz')
  const qtyIdx = cols.findIndex((c) => c.key === 'qty')
  if (profitIdx >= 0) {
    cols.splice(profitIdx + 1, 0, ...progressFive)
  } else if (qtyIdx >= 0) {
    cols.splice(qtyIdx + 1, 0, ...progressFive)
  }

  cols.push({
    key: 'actions',
    label: t('salesOrderItemList.columns.actions'),
    width: opColWidth.value,
    minWidth: opColMinWidth.value,
    fixed: 'right',
    align: 'center',
    hideable: false,
    pinned: 'end',
    reorderable: false,
    className: 'op-col',
    labelClassName: 'op-col'
  })
  return cols
})

const dateRange = ref<[string, string] | null>(null)
const filters = reactive({
  sellOrderCode: '',
  customerName: '',
  salesUserName: '',
  pn: ''
})

// ==============================
// 新建采购申请弹窗
// ==============================
const applyDialogVisible = ref(false)
const applyLoading = ref(false)
const applySubmitting = ref(false)
const applyFormRef = ref<FormInstance>()
const applyForm = reactive({
  sellOrderItemId: '' as string,
  pn: '',
  brand: '',
  salesOrderQty: 0,
  purchasedQty: 0,
  openPurchaseRequisitionQty: 0,
  remainingQty: 0,
  requestQty: 0,
  expectedPurchaseDate: '' as string,
  remark: ''
})
const applyRules = computed<FormRules>(() => ({
  requestQty: [{ required: true, message: t('salesOrderItemList.validation.requestQtyRequired'), trigger: 'change' }],
  expectedPurchaseDate: [
    { required: true, message: t('salesOrderItemList.validation.expectedDateRequired'), trigger: 'change' }
  ]
}))

const applyFormReset = () => {
  applyForm.sellOrderItemId = ''
  applyForm.pn = ''
  applyForm.brand = ''
  applyForm.salesOrderQty = 0
  applyForm.purchasedQty = 0
  applyForm.openPurchaseRequisitionQty = 0
  applyForm.remainingQty = 0
  applyForm.requestQty = 0
  applyForm.remark = ''
  applyForm.expectedPurchaseDate = new Date().toISOString().slice(0, 10)
}

const submitApply = async () => {
  if (!applyFormRef.value) return
  const ok = await validateElFormOrWarn(applyFormRef)
  if (!ok) return

  // 附加校验：不能超过可申请数量
  if (applyForm.requestQty <= 0) {
    ElMessage.warning(t('salesOrderItemList.validation.requestQtyPositive'))
    return
  }
  if (applyForm.requestQty > applyForm.remainingQty) {
    ElMessage.warning(t('salesOrderItemList.validation.requestQtyMax'))
    return
  }
  if (!applyForm.expectedPurchaseDate) {
    ElMessage.warning(t('salesOrderItemList.validation.expectedDatePick'))
    return
  }

  const created = await runSaveTask({
    loading: applySubmitting,
    task: async () => {
      const expectedPurchaseTime = `${applyForm.expectedPurchaseDate}T00:00:00.000Z`
      return purchaseRequisitionApi.create({
        sellOrderItemId: applyForm.sellOrderItemId,
        qty: applyForm.requestQty,
        expectedPurchaseTime,
        type: 0, // 0=专属；该弹窗不做类型选择
        remark: applyForm.remark || undefined
      })
    },
    formatSuccess: () => t('salesOrderItemList.messages.prCreated'),
    errorMessage: (e: unknown) => {
      const err = e as { response?: { data?: { message?: string } }; message?: string }
      return err?.response?.data?.message || err?.message || t('salesOrderItemList.messages.createFailed')
    }
  })
  if (!created) return
  applyDialogVisible.value = false
  await loadList()
}

function normSellOrderItemId(s: unknown) {
  return String(s ?? '')
    .trim()
    .toLowerCase()
}

async function applyPurchaseOne(row: any) {
  if (applyPurchaseDisabled(row)) {
    ElMessage.warning(t('salesOrderItemList.messages.prLineNotAvailable'))
    return
  }
  if (!mainAllowsOps(row)) {
    ElMessage.warning(t('salesOrderItemList.messages.applyPurchaseNeedAudit'))
    return
  }
  applyFormReset()
  applyDialogVisible.value = true
  applyLoading.value = true
  try {
    const sellOrderId = row.sellOrderId as string
    const sellOrderItemId = String(row.sellOrderItemId ?? row.id ?? row.Id ?? '').trim()

    const options = (await purchaseRequisitionApi.getLineOptions(sellOrderId)) || []
    const line = options.find((x: any) => normSellOrderItemId(x.sellOrderItemId) === normSellOrderItemId(sellOrderItemId))
    if (!line) {
      ElMessage.warning(t('salesOrderItemList.messages.prLineNotAvailable'))
      applyDialogVisible.value = false
      return
    }

    applyForm.sellOrderItemId = sellOrderItemId
    applyForm.pn = line.pn ?? row.pn ?? ''
    applyForm.brand = line.brand ?? row.brand ?? ''
    const toInt = (v: unknown) => Math.trunc(Number(v) || 0)
    applyForm.salesOrderQty = toInt(line.salesOrderQty ?? row.qty ?? 0)
    applyForm.purchasedQty = toInt(line.purchasedQty ?? 0)
    applyForm.openPurchaseRequisitionQty = toInt(line.openPurchaseRequisitionQty ?? 0)
    applyForm.remainingQty = toInt(line.remainingQty)
    applyForm.requestQty = Math.max(0, applyForm.remainingQty)
  } catch (e: any) {
    ElMessage.error(e?.response?.data?.message || e?.message || t('salesOrderItemList.messages.loadLineFailed'))
    applyDialogVisible.value = false
  } finally {
    applyLoading.value = false
  }
}

// 将数字转为截图那种“输入框字符串效果”
const applyFormSalesOrderQtyText = computed(() => String(Math.trunc(Number(applyForm.salesOrderQty ?? 0) || 0)))
const applyFormPurchasedQtyText = computed(() => String(Math.trunc(Number(applyForm.purchasedQty ?? 0) || 0)))
const applyFormOpenPrQtyText = computed(() => String(Math.trunc(Number(applyForm.openPurchaseRequisitionQty ?? 0) || 0)))
const applyFormRemainingQtyText = computed(() => String(Math.trunc(Number(applyForm.remainingQty ?? 0) || 0)))

function statusText(s: number) {
  return translateSalesOrderStatus(s, t)
}

function statusTagType(s: number): '' | 'success' | 'warning' | 'info' | 'danger' {
  return salesOrderStatusTagType(s) as '' | 'success' | 'warning' | 'info' | 'danger'
}

/** 明细扩展进度 0=待 1=部分 2=完成 */
function extendTriTagType(v?: number): '' | 'success' | 'warning' | 'info' | 'danger' {
  const map: Record<number, '' | 'info' | 'success' | 'warning' | 'danger'> = {
    0: 'info',
    1: 'warning',
    2: 'success'
  }
  return v !== undefined && v !== null ? (map[v] ?? 'info') : 'info'
}

function extendTriLabel(
  kind: 'purchase' | 'stockIn' | 'stockOut' | 'receipt' | 'invoice',
  v?: number
): string {
  const slot = v === 2 ? 'complete' : v === 1 ? 'partial' : 'pending'
  return t(`salesOrderItemList.extendProgress.${kind}.${slot}`)
}

function formatDt(v: string) {
  if (!v) return '—'
  const s = formatDisplayDateTime(v)
  return s === '--' ? '—' : s
}

function onSelectionChange(rows: any[]) {
  if (suppressBasketMerge.value) return
  basketStore.mergePageSelection(list.value as any[], rows as any[])
}

async function restoreTableSelectionFromBasket() {
  const table = dataTableRef.value
  if (!table) return
  suppressBasketMerge.value = true
  await nextTick()
  table.clearSelection()
  await nextTick()
  for (const row of list.value) {
    const id = String((row as any).sellOrderItemId ?? '').trim()
    if (id && basketStore.has(id)) {
      table.toggleRowSelection(row, true)
    }
  }
  await nextTick()
  suppressBasketMerge.value = false
}

function removeOneFromBasket(sellOrderItemId: string) {
  if (!sellOrderItemId) return
  basketStore.remove(sellOrderItemId)
  suppressBasketMerge.value = true
  const row = list.value.find((r) => String((r as any).sellOrderItemId ?? '').trim() === sellOrderItemId)
  if (row) {
    dataTableRef.value?.toggleRowSelection(row, false)
  }
  void nextTick(() => {
    suppressBasketMerge.value = false
  })
}

async function handleClearBasket() {
  if (!basketStore.count) return
  try {
    await ElMessageBox.confirm(
      t('salesOrderItemList.messages.clearBasketConfirm'),
      t('salesOrderItemList.messages.clearBasketTitle'),
      {
        type: 'warning',
        confirmButtonText: t('salesOrderItemList.messages.clearButton'),
        cancelButtonText: t('common.cancel')
      }
    )
  } catch {
    return
  }
  basketStore.clear()
  suppressBasketMerge.value = true
  dataTableRef.value?.clearSelection()
  await nextTick()
  suppressBasketMerge.value = false
  ElMessage.success(t('salesOrderItemList.messages.basketCleared'))
}

async function loadList() {
  loading.value = true
  try {
    const params: Record<string, unknown> = {
      page: page.value,
      pageSize: pageSize.value
    }
    if (dateRange.value?.[0]) params.orderCreateStart = dateRange.value[0]
    if (dateRange.value?.[1]) params.orderCreateEnd = dateRange.value[1]
    const soc = String(filters.sellOrderCode ?? '').trim()
    if (soc) params.sellOrderCode = soc
    const cn = String(filters.customerName ?? '').trim()
    if (cn) params.customerName = cn
    const sun = String(filters.salesUserName ?? '').trim()
    if (sun) params.salesUserName = sun
    const pnk = String(filters.pn ?? '').trim()
    if (pnk) params.pn = pnk

    const data = await salesOrderApi.getItemLines(params)
    list.value = data?.items ?? []
    total.value = data?.total ?? 0
  } catch (e: any) {
    ElMessage.error(e?.message || t('salesOrderItemList.messages.loadListFailed'))
  } finally {
    loading.value = false
  }
  await nextTick()
  await restoreTableSelectionFromBasket()
}

function resetFilters() {
  dateRange.value = null
  filters.sellOrderCode = ''
  filters.customerName = ''
  filters.salesUserName = ''
  filters.pn = ''
  page.value = 1
  basketStore.clear()
  suppressBasketMerge.value = true
  dataTableRef.value?.clearSelection()
  void nextTick(() => {
    suppressBasketMerge.value = false
    loadList()
  })
}

function goDetail(row: any) {
  router.push({ name: 'SalesOrderDetail', params: { id: row.sellOrderId } })
}

function goEdit(row: any) {
  router.push({ path: `/sales-orders/${row.sellOrderId}`, query: { edit: '1' } })
}

function navigateNewPr(sellOrderId: string, itemIds: string[]) {
  const q: Record<string, string> = { sellOrderId }
  if (itemIds.length) q.itemIds = itemIds.join(',')
  router.push({ path: '/purchase-requisitions/new', query: q })
}

function batchApplyPurchase() {
  const rows = basketStore.items as any[]
  if (!rows.length) {
    ElMessage.warning(t('salesOrderItemList.messages.basketNeedRows'))
    return
  }
  if (!rows.every((r) => mainAllowsOps(r))) {
    ElMessage.warning(t('salesOrderItemList.messages.applyPurchaseNeedAudit'))
    return
  }
  if (rows.length === 1) {
    // 1条时走弹窗，避免跳到可能未完善的路由页面
    applyPurchaseOne(rows[0])
    return
  }

  ElMessage.warning(t('salesOrderItemList.messages.batchNotImplemented'))
  return

  const orderIds = new Set(rows.map((r) => r.sellOrderId))
  if (orderIds.size !== 1) {
    ElMessage.warning(t('salesOrderItemList.messages.batchSameOrderOnly'))
    return
  }
  if (canViewCustomer.value) {
    const cids = rows.map((r) => r.customerId).filter(Boolean)
    const names = rows.map((r) => r.customerName).filter(Boolean)
    if (cids.length === rows.length) {
      if (!cids.every((id) => id === cids[0])) {
        ElMessage.warning(t('salesOrderItemList.messages.sameCustomer'))
        return
      }
    } else if (names.length === rows.length) {
      if (!names.every((n) => n === names[0])) {
        ElMessage.warning(t('salesOrderItemList.messages.sameCustomer'))
        return
      }
    }
  }
  navigateNewPr(rows[0].sellOrderId, rows.map((r) => r.sellOrderItemId))
}

function applyStockOutOne(row: any) {
  if (applyStockOutDisabled(row)) {
    ElMessage.warning(t('salesOrderItemList.messages.applyStockOutDisabledByProgress'))
    return
  }
  if (!mainAllowsOps(row)) {
    ElMessage.warning(t('salesOrderItemList.messages.applyStockOutNeedAudit'))
    return
  }
  if (!stockOutApplyPurchaseGateOk(row)) {
    ElMessage.warning(t('salesOrderItemList.messages.applyStockOutNeedPurchaseGate'))
    return
  }
  router.push({
    path: `/sales-orders/${row.sellOrderId}`,
    query: { applyStockOut: '1' }
  })
}

onMounted(() => loadList())
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.so-item-list-page {
  padding: 24px;
  min-height: 100%;
  background: $layer-1;
}

.page-header {
  display: flex;
  align-items: center;
  margin-bottom: 20px;
}
.header-left {
  display: flex;
  align-items: center;
  gap: 12px;
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
  font-size: 20px;
  font-weight: 600;
  color: $text-primary;
  margin: 0;
}
.list-count-badge {
  font-size: 12px;
  color: $text-muted;
  background: rgba(255, 255, 255, 0.05);
  border: 1px solid $border-panel;
  border-radius: 20px;
  padding: 3px 10px;
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
  cursor: pointer;
  &:disabled {
    opacity: 0.45;
    cursor: not-allowed;
  }
  &.btn-sm {
    padding: 6px 12px;
    font-size: 12px;
  }
}
.btn-ghost {
  padding: 6px 12px;
  background: transparent;
  border: 1px solid $border-panel;
  border-radius: $border-radius-md;
  color: $text-muted;
  font-size: 12px;
  cursor: pointer;
}
.search-bar {
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
}
.search-input {
  padding: 7px 12px;
  background: $layer-2;
  border: 1px solid $border-panel;
  border-radius: $border-radius-md;
  color: $text-primary;
  font-size: 13px;
  outline: none;
}
.so-date-range {
  width: 260px;
}
.so-filter-input {
  width: 160px;
}
.table-wrapper {
  background: $layer-2;
  border: 1px solid $border-card;
  border-radius: $border-radius-lg;
  overflow: hidden;
}
.table-footer-bar {
  flex-shrink: 0;
  margin-top: 12px;
  padding-top: 4px;
  display: flex;
  align-items: center;
  justify-content: flex-start;
  gap: 12px 16px;
  flex-wrap: wrap;
}

.basket-footer-left {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  flex-wrap: nowrap;
  flex-shrink: 0;
}

.list-settings-btn {
  padding: 4px 6px !important;
  min-width: 28px;
}

.list-footer-spacer {
  width: 18px;
  flex: 0 0 18px;
}

.basket-open-btn {
  padding: 4px 6px 4px 8px !important;
  font-size: 13px;
  font-weight: 500;
}

.basket-clear-btn {
  padding: 4px 8px 4px 2px !important;
  font-size: 13px;
  font-weight: 500;
}

.basket-batch-purchase-btn {
  margin-left: 10px;
}

.basket-count-label {
  color: $cyan-primary;
  font-weight: 600;
  margin-left: 2px;
}

.table-footer-bar .quantum-pagination {
  margin-left: auto;
}

.quantum-pagination {
  :deep(.el-pagination__total) {
    color: $text-muted;
  }

  :deep(.el-pagination__sizes .el-select__wrapper) {
    background: $layer-2 !important;
    border: 1px solid $border-panel !important;
    box-shadow: none !important;
  }

  :deep(.el-pager li) {
    background: $layer-2;
    border: 1px solid $border-panel;
    color: $text-secondary;
    border-radius: 6px;
    margin: 0 2px;
  }

  :deep(.el-pager li.is-active) {
    background: rgba(0, 212, 255, 0.15);
    border-color: rgba(0, 212, 255, 0.4);
    color: $cyan-primary;
  }

  :deep(.btn-prev),
  :deep(.btn-next) {
    background: $layer-2 !important;
    border: 1px solid $border-panel !important;
    color: $text-secondary !important;
    border-radius: 6px !important;
  }
}

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
  overflow: hidden;
  text-overflow: ellipsis;
}

.op-col-toggle-btn {
  padding: 0;
  border: none;
  background: transparent;
  color: $cyan-primary;
  cursor: pointer;
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

.op-more-item--disabled {
  color: $text-muted !important;
}

/* 展开操作列：禁用仍用 type=warning 时文字改为灰色 */
.action-btns :deep(.el-button.is-disabled.is-link.el-button--warning) {
  color: $text-muted !important;
  --el-button-hover-link-text-color: #{$text-muted};
}

:deep(.el-table__body-wrapper .el-table__body tr:hover .op-more-trigger),
:deep(.el-table__fixed-body-wrapper .el-table__body tr:hover .op-more-trigger),
:deep(.el-table__body-wrapper .el-table__body tr.hover-row .op-more-trigger),
:deep(.el-table__fixed-body-wrapper .el-table__body tr.hover-row .op-more-trigger) {
  opacity: 1;
}
</style>

<!-- 抽屉挂载在 body，需单独样式块 -->
<style lang="scss">
@import '@/assets/styles/variables.scss';

.so-item-basket-drawer {
  .basket-drawer-hint {
    font-size: 13px;
    color: rgba(255, 255, 255, 0.55);
    line-height: 1.6;
    margin: 0 0 12px;
  }

  .basket-drawer-summary {
    font-size: 13px;
    color: rgba(232, 244, 255, 0.75);
    margin: 0 0 12px;
    line-height: 1.6;
  }

  .basket-clear-btn--drawer-inline {
    vertical-align: baseline;
    height: auto !important;
    min-height: 0 !important;
    padding: 0 2px !important;
    margin: 0 1px;
    font-size: 13px !important;
    font-weight: 500;
  }
}
</style>
