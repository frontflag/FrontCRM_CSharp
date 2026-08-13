<template>
  <div class="finance-page">
    <div class="fsi-list-page-header">
      <h1 class="finance-list-page-title">{{ t('financeSellInvoiceList.pageTitle') }}</h1>
      <button type="button" class="btn-write-off-desktop" @click="goWriteOffDesktop">
        <span>{{ t('financeSellInvoiceList.goWriteOffDesktop') }}</span>
        <el-icon class="btn-write-off-desktop__arrow"><ArrowRight /></el-icon>
      </button>
    </div>

    <!-- 统计卡片（置顶） -->
    <div class="stat-cards">
      <div class="stat-card">
        <div class="stat-label">{{ t('financeSellInvoiceList.stats.totalAmount') }}</div>
        <div class="stat-value">{{ maskSaleSensitiveFields ? '—' : formatTotalAmountNumber(stats.totalAmount) }}</div>
      </div>
      <div class="stat-card">
        <div class="stat-label">{{ t('financeSellInvoiceList.stats.receivedAmount') }}</div>
        <div class="stat-value success">{{ maskSaleSensitiveFields ? '—' : formatTotalAmountNumber(stats.receivedAmount) }}</div>
      </div>
      <div class="stat-card">
        <div class="stat-label">{{ t('financeSellInvoiceList.stats.toReceiveAmount') }}</div>
        <div class="stat-value warning">{{ maskSaleSensitiveFields ? '—' : formatTotalAmountNumber(stats.toReceiveAmount) }}</div>
      </div>
      <div class="stat-card">
        <div class="stat-label">{{ t('financeSellInvoiceList.stats.invoicedCount') }}</div>
        <div class="stat-value">{{ stats.invoicedCount }}</div>
      </div>
    </div>

    <!-- 搜索栏 -->
    <div class="search-bar">
      <div class="search-left">
        <div class="search-input-wrap">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="search-icon">
            <circle cx="11" cy="11" r="8" />
            <line x1="21" y1="21" x2="16.65" y2="16.65" />
          </svg>
          <input
            v-model="query.keyword"
            class="search-input"
            :placeholder="t('financeSellInvoiceList.filters.keyword')"
            @keyup.enter="handleSearch"
          />
        </div>
        <el-select
          v-model="query.status"
          :placeholder="t('financeSellInvoiceList.filters.invoiceStatus')"
          clearable
          class="filter-select"
          :teleported="false"
          @change="handleSearch"
        >
          <el-option v-for="k in invoiceStatusSelectKeys" :key="k" :label="invoiceStatusLabel(k)" :value="k" />
        </el-select>
        <el-select
          v-model="filterMatchStatus"
          :placeholder="t('financeSellInvoiceList.filters.matchStatus')"
          clearable
          class="filter-select"
          :teleported="false"
          @change="handleSearch"
        >
          <el-option
            v-for="k in matchStatusSelectKeys"
            :key="k"
            :label="sellInvoiceMatchStatusLabel(k)"
            :value="k"
          />
        </el-select>
        <el-select
          v-model="filterReceiveStatus"
          :placeholder="t('financeSellInvoiceList.filters.receiveStatus')"
          clearable
          class="filter-select"
          :teleported="false"
          @change="handleSearch"
        >
          <el-option v-for="k in receiveStatusSelectKeys" :key="k" :label="receiveStatusLabel(k)" :value="k" />
        </el-select>
        <el-date-picker
          v-model="dateRange"
          type="daterange"
          :range-separator="t('financeSellInvoiceList.filters.to')"
          :start-placeholder="t('financeSellInvoiceList.filters.start')"
          :end-placeholder="t('financeSellInvoiceList.filters.end')"
          value-format="YYYY-MM-DD"
          clearable
          class="filter-date-range"
          :teleported="false"
          @change="handleSearch"
        />
        <button type="button" class="btn-primary btn-sm" :disabled="loading" @click="handleSearch">
          {{ t('financeSellInvoiceList.filters.search') }}
        </button>
        <button type="button" class="btn-ghost btn-sm" :disabled="loading" @click="handleReset">
          {{ t('financeSellInvoiceList.filters.reset') }}
        </button>
      </div>
      <div class="search-right">
        <button
          v-if="canWriteFinanceSellInvoice"
          type="button"
          class="btn-primary btn-sm"
          @click="openCreate"
        >
          <el-icon><Plus /></el-icon> {{ t('financeSellInvoiceList.create') }}
        </button>
      </div>
    </div>

    <!-- 数据表格 -->
    <CrmDataTable
      ref="dataTableRef"
      column-layout-key="finance-sell-invoice-list-main-v7"
      :columns="sellInvoiceTableColumns"
      :show-column-settings="false"
      :density-toggle-anchor-el="rowDensityToggleAnchorEl"
      :data="tableData"
      v-loading="loading"
      @row-dblclick="onSellInvoiceRowDblClick"
      row-class-name="table-row-pointer"
    >
      <template #col-invoiceCode="{ row }"><span class="code-text">{{ row.invoiceCode || '-' }}</span></template>
      <template #col-invoiceStatus="{ row }">
        <el-tag effect="dark" :type="invoiceStatusTag(row.invoiceStatus) as any" size="small">
          {{ invoiceStatusLabel(row.invoiceStatus) }}
        </el-tag>
      </template>
      <template #col-matchStatus="{ row }">
        <el-tag effect="dark" :type="sellInvoiceMatchStatusTag(row.matchStatus ?? 0) as any" size="small">
          {{ sellInvoiceMatchStatusLabel(row.matchStatus ?? 0) }}
        </el-tag>
      </template>
      <template #col-matchDone="{ row }">
        <template v-if="maskSaleSensitiveFields || !listTotalAmountHasValue(row.matchDone)">
          <span class="dock-tier-empty">—</span>
        </template>
        <div v-else class="dock-tier-price-line">
          <template v-for="amt in [splitListMoneyParts(Number(row.matchDone))]" :key="'inv-match-' + row.id">
            <span class="dock-tier-amt">
              <span class="dock-tier-amt-int">{{ amt.intPart }}</span><span class="dock-tier-amt-frac">{{ amt.fracPart }}</span>
            </span>
          </template>
          <span class="dock-tier-ccy-gap">&nbsp;</span>
          <span :class="['dock-tier-ccy', listAmountCurrencyDockClass(row.currency)]">{{ listAmountCurrencyIso(row.currency) }}</span>
        </div>
      </template>
      <template #col-invoiceNo="{ row }">{{ row.invoiceNo || '-' }}</template>
      <template #col-customerName="{ row }">
        <span>{{ maskSaleSensitiveFields ? '—' : (row.customerName || '—') }}</span>
      </template>
      <template #col-invoiceTotal="{ row }">
        <template v-if="maskSaleSensitiveFields || !listTotalAmountHasValue(row.invoiceTotal)">
          <span class="dock-tier-empty">—</span>
        </template>
        <div v-else class="dock-tier-price-line">
          <template v-for="amt in [splitListMoneyParts(Number(row.invoiceTotal))]" :key="'inv-total-' + row.id">
            <span class="dock-tier-amt">
              <span class="dock-tier-amt-int">{{ amt.intPart }}</span><span class="dock-tier-amt-frac">{{ amt.fracPart }}</span>
            </span>
          </template>
          <span class="dock-tier-ccy-gap">&nbsp;</span>
          <span :class="['dock-tier-ccy', listAmountCurrencyDockClass(row.currency)]">{{ listAmountCurrencyIso(row.currency) }}</span>
        </div>
      </template>
      <template #col-receiveDone="{ row }">
        <template v-if="maskSaleSensitiveFields || !listTotalAmountHasValue(row.receiveDone)">
          <span class="dock-tier-empty">—</span>
        </template>
        <div v-else class="dock-tier-price-line">
          <template v-for="amt in [splitListMoneyParts(Number(row.receiveDone))]" :key="'inv-recv-' + row.id">
            <span class="dock-tier-amt">
              <span class="dock-tier-amt-int">{{ amt.intPart }}</span><span class="dock-tier-amt-frac">{{ amt.fracPart }}</span>
            </span>
          </template>
          <span class="dock-tier-ccy-gap">&nbsp;</span>
          <span :class="['dock-tier-ccy', listAmountCurrencyDockClass(row.currency)]">{{ listAmountCurrencyIso(row.currency) }}</span>
        </div>
      </template>
      <template #col-receiveStatus="{ row }">
        <el-tag effect="dark" :type="receiveStatusTag(row.receiveStatus) as any" size="small">
          {{ receiveStatusLabel(row.receiveStatus) }}
        </el-tag>
      </template>
      <template #col-sellInvoiceType="{ row }">{{ sellInvoiceTypeLabel(row.sellInvoiceType) }}</template>
      <template #col-remark="{ row }">{{ row.remark || '-' }}</template>
      <template #col-makeInvoiceDate="{ row }">{{ row.makeInvoiceDate ? formatDisplayDate(row.makeInvoiceDate) : '-' }}</template>
      <template #col-createTime="{ row }">
        <template v-for="p in [formatDisplayDateTime2DigitYearParts(row.createdAt)]" :key="'ct-' + row.id">
          <span v-if="p" class="crm-quote-create-time">
            <span class="crm-quote-create-time__ymd">{{ p.date }}</span>
            <span class="crm-quote-create-time__hm">{{ p.time }}</span>
          </span>
          <span v-else>-</span>
        </template>
      </template>
      <template #col-createUser="{ row }">{{ (row as any).createUserName || (row as any).createdBy || '-' }}</template>
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
            <el-button size="small" text type="primary" @click.stop="openDetail(row)">{{ t('financeSellInvoiceList.actions.detail') }}</el-button>
            <el-button size="small" text type="primary" @click.stop="openEdit(row)" v-if="canWriteFinanceSellInvoice && row.invoiceStatus === 1">{{ t('financeSellInvoiceList.actions.edit') }}</el-button>
            <el-button size="small" text type="warning" @click.stop="applyInvoice(row)" v-if="canWriteFinanceSellInvoice && row.invoiceStatus === 1">{{ t('financeSellInvoiceList.actions.apply') }}</el-button>
            <el-button size="small" text type="success" @click.stop="markIssued(row)" v-if="canWriteFinanceSellInvoice && row.invoiceStatus === 2">{{ t('financeSellInvoiceList.actions.markIssued') }}</el-button>
            <el-button size="small" text type="warning" @click.stop="markIssueFailed(row)" v-if="canWriteFinanceSellInvoice && row.invoiceStatus === 2">{{ t('financeSellInvoiceList.actions.markIssueFailed') }}</el-button>
            <el-button size="small" text type="danger" @click.stop="voidInvoice(row)" v-if="canWriteFinanceSellInvoice && row.invoiceStatus === 100">{{ t('financeSellInvoiceList.actions.void') }}</el-button>
            <el-button v-if="canWriteFinanceSellInvoice" size="small" text type="danger" @click.stop="handleDeleteRow(row)">删除</el-button>
            <el-button v-if="canForceDelete" size="small" text type="danger" @click.stop="handleForceDeleteRow(row)">强制删除</el-button>
          </div>

          <el-dropdown v-else trigger="click" placement="bottom-end">
            <div class="op-more-dropdown-trigger">
              <button type="button" class="op-more-trigger">...</button>
            </div>
            <template #dropdown>
              <el-dropdown-menu>
                <el-dropdown-item @click.stop="openDetail(row)">
                  <span class="op-more-item op-more-item--primary">{{ t('financeSellInvoiceList.actions.detail') }}</span>
                </el-dropdown-item>
                <el-dropdown-item v-if="canWriteFinanceSellInvoice && row.invoiceStatus === 1" @click.stop="openEdit(row)">
                  <span class="op-more-item op-more-item--primary">{{ t('financeSellInvoiceList.actions.edit') }}</span>
                </el-dropdown-item>
                <el-dropdown-item v-if="canWriteFinanceSellInvoice && row.invoiceStatus === 1" @click.stop="applyInvoice(row)">
                  <span class="op-more-item op-more-item--warning">{{ t('financeSellInvoiceList.actions.apply') }}</span>
                </el-dropdown-item>
                <el-dropdown-item v-if="canWriteFinanceSellInvoice && row.invoiceStatus === 2" @click.stop="markIssued(row)">
                  <span class="op-more-item op-more-item--success">{{ t('financeSellInvoiceList.actions.markIssued') }}</span>
                </el-dropdown-item>
                <el-dropdown-item v-if="canWriteFinanceSellInvoice && row.invoiceStatus === 2" @click.stop="markIssueFailed(row)">
                  <span class="op-more-item op-more-item--warning">{{ t('financeSellInvoiceList.actions.markIssueFailed') }}</span>
                </el-dropdown-item>
                <el-dropdown-item v-if="canWriteFinanceSellInvoice && row.invoiceStatus === 100" @click.stop="voidInvoice(row)">
                  <span class="op-more-item op-more-item--danger">{{ t('financeSellInvoiceList.actions.void') }}</span>
                </el-dropdown-item>
                <el-dropdown-item v-if="canWriteFinanceSellInvoice" divided @click.stop="handleDeleteRow(row)">
                  <span class="op-more-item op-more-item--danger">删除</span>
                </el-dropdown-item>
                <el-dropdown-item v-if="canForceDelete" @click.stop="handleForceDeleteRow(row)">
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
        <el-tooltip :content="t('financeSellInvoiceList.columnSettings')" placement="top" :hide-after="0">
          <el-button
            class="list-settings-btn"
            link
            type="primary"
            :aria-label="t('financeSellInvoiceList.columnSettings')"
            @click="dataTableRef?.openColumnSettings?.()"
          >
            <el-icon><Setting /></el-icon>
          </el-button>
        </el-tooltip>
        <span ref="rowDensityToggleAnchorEl" class="list-footer-density-anchor" aria-hidden="true" />
        <div class="list-footer-spacer" aria-hidden="true"></div>
      </div>
      <el-pagination
        class="list-main-pagination"
        v-model:current-page="query.page"
        v-model:page-size="query.pageSize"
        :total="total"
        :page-sizes="[10, 20, 50, 100]"
        layout="total, sizes, prev, pager, next, jumper"
        @size-change="loadData"
        @current-change="loadData"
      />
    </div>

    <!-- 新建/编辑弹窗 -->
    <el-dialog
      v-model="dialogVisible"
      :title="editingId ? t('financeSellInvoiceList.dialogEdit') : t('financeSellInvoiceList.dialogCreate')"
      width="720px"
      class="crm-dialog"
      destroy-on-close
    >
      <el-form :model="form" label-width="100px" class="crm-form">
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item :label="t('financeSellInvoiceList.formCustomer')" required>
              <template v-if="maskSaleSensitiveFields">
                <el-input model-value="—" disabled style="width: 100%" />
              </template>
              <el-select
                v-else
                v-model="form.customerId"
                :placeholder="t('financeSellInvoiceList.customerPh')"
                style="width: 100%"
                filterable
                clearable
                :filter-method="onCustomerFilterInput"
                :loading="customerSearchLoading"
                :loading-text="t('financeSellInvoiceList.customerSearchLoading')"
                @change="onCustomerChange"
              >
                <template #empty>
                  <div class="select-hint">{{ t('financeSellInvoiceList.customerEmptyHint') }}</div>
                </template>
                <el-option
                  v-for="c in customerOptions"
                  :key="c.value"
                  :label="c.label"
                  :value="c.value"
                />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item :label="t('financeSellInvoiceList.formInvoiceNo')">
              <el-input v-model="form.invoiceNo" :placeholder="t('financeSellInvoiceList.formInvoiceNoPh')" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item :label="t('financeSellInvoiceList.formAmount')" required>
              <el-input v-if="maskSaleSensitiveFields" model-value="—" disabled style="width: 100%" />
              <el-input-number v-else v-model="form.invoiceTotal" :precision="2" :min="0" style="width:100%" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item :label="t('financeSellInvoiceList.formMakeDate')">
              <el-date-picker v-model="form.makeInvoiceDate" type="date" value-format="YYYY-MM-DD" style="width:100%" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item :label="t('financeSellInvoiceList.formInvoiceType')">
              <el-select v-model="form.sellInvoiceType" style="width:100%">
                <el-option v-for="k in sellInvoiceTypeKeys" :key="k" :label="sellInvoiceTypeLabel(k)" :value="k" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item :label="t('financeSellInvoiceList.formBlueRed')">
              <el-select v-model="form.type" style="width:100%">
                <el-option v-for="k in invoiceTypeKeys" :key="k" :label="invoiceTypeLabel(k)" :value="k" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item :label="t('financeSellInvoiceList.formCurrency')">
              <el-select v-model="form.currency" style="width:100%">
                <el-option
                  v-for="opt in SETTLEMENT_CURRENCY_OPTIONS"
                  :key="opt.value"
                  :label="opt.label"
                  :value="opt.value"
                />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="24">
            <el-form-item :label="t('financeSellInvoiceList.formRemark')">
              <el-input v-model="form.remark" type="textarea" :rows="2" :placeholder="t('financeSellInvoiceList.formRemarkPh')" />
            </el-form-item>
          </el-col>
        </el-row>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">{{ t('common.cancel') }}</el-button>
        <el-button v-if="canWriteFinanceSellInvoice" type="primary" @click="saveForm" :loading="saving">{{ t('financeSellInvoiceList.btnSave') }}</el-button>
      </template>
    </el-dialog>

  </div>
</template>

<script setup lang="ts">
import { computed, ref, reactive, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useFinanceEnumLabels } from '@/composables/useFinanceEnumLabels'
import { Plus, Setting, ArrowRight } from '@element-plus/icons-vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import {
  financeSellInvoiceApi,
  INVOICE_STATUS_MAP,
  RECEIVE_STATUS_MAP,
  SELL_INVOICE_MATCH_STATUS_MAP,
  SELL_INVOICE_TYPE_MAP,
  INVOICE_TYPE_MAP,
  type FinanceSellInvoice,
  type PageQuery,
} from '@/api/finance'
import { SETTLEMENT_CURRENCY_OPTIONS, DEFAULT_SETTLEMENT_CURRENCY_CODE } from '@/constants/currency'
import { formatDisplayDate, formatDisplayDateTime2DigitYearParts } from '@/utils/displayDateTime'
import {
  formatTotalAmountNumber,
  listAmountCurrencyDockClass,
  listAmountCurrencyIso,
  listTotalAmountHasValue,
  splitListMoneyParts,
} from '@/utils/moneyFormat'
import { onCrmDetailListRowDblClick } from '@/utils/crmDetailListRowDblClick'
import { customerApi } from '@/api/customer'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'
import { useSaleSensitiveFieldMask } from '@/composables/useSaleSensitiveFieldMask'
import { useAuthStore } from '@/stores/auth'
import { useFinanceWriteGate } from '@/composables/useDepartmentDataReadOnly'

const router = useRouter()
const { maskSaleSensitiveFields } = useSaleSensitiveFieldMask()
const { t } = useI18n()
const authStore = useAuthStore()
const { canWriteFinanceSellInvoice } = useFinanceWriteGate()
const canForceDelete = computed(() => authStore.canForceDelete())
const {
  invoiceStatusLabel,
  invoiceStatusTag,
  receiveStatusLabel,
  receiveStatusTag,
  sellInvoiceMatchStatusLabel,
  sellInvoiceMatchStatusTag,
  sellInvoiceTypeLabel,
  invoiceTypeLabel,
} = useFinanceEnumLabels()

const invoiceStatusSelectKeys = Object.keys(INVOICE_STATUS_MAP).map(k => Number(k))
const receiveStatusSelectKeys = Object.keys(RECEIVE_STATUS_MAP).map(k => Number(k))
const matchStatusSelectKeys = Object.keys(SELL_INVOICE_MATCH_STATUS_MAP).map(k => Number(k))
const sellInvoiceTypeKeys = Object.keys(SELL_INVOICE_TYPE_MAP).map(k => Number(k))
const invoiceTypeKeys = Object.keys(INVOICE_TYPE_MAP).map(k => Number(k))

type CustomerOption = { value: string; label: string }

const customerOptions = ref<CustomerOption[]>([])
const customerSearchLoading = ref(false)
let customerSearchTimer: ReturnType<typeof setTimeout> | null = null

async function onCustomerFilterInput(query: string) {
  if (customerSearchTimer) clearTimeout(customerSearchTimer)
  if (!query || query.trim().length < 1) return
  customerSearchTimer = setTimeout(async () => {
    customerSearchLoading.value = true
    try {
      const res = await customerApi.searchCustomers({
        pageNumber: 1,
        pageSize: 30,
        searchTerm: query.trim(),
      })
      customerOptions.value = (res.items || []).map((c) => ({
        value: c.id,
        label: c.customerName || (c as { officialName?: string }).officialName || t('financeSellInvoiceList.unknownCustomer'),
      }))
    } catch {
      customerOptions.value = []
    } finally {
      customerSearchLoading.value = false
    }
  }, 300)
}

function onCustomerChange(val: string | undefined) {
  const id = val?.trim() || ''
  if (!id) {
    form.customerName = ''
    return
  }
  const found = customerOptions.value.find((c) => c.value === id)
  if (found) form.customerName = found.label
}

const query = reactive<PageQuery & { page: number; pageSize: number }>({
  page: 1, pageSize: 20, keyword: '', status: undefined,
  startDate: undefined, endDate: undefined,
})
const dateRange = ref<[string, string] | null>(null)
const filterMatchStatus = ref<number | undefined>(undefined)
const filterReceiveStatus = ref<number | undefined>(undefined)
const total = ref(0)
const loading = ref(false)
const tableData = ref<FinanceSellInvoice[]>([])
const dataTableRef = ref<{ openColumnSettings?: () => void } | null>(null)
const rowDensityToggleAnchorEl = ref<HTMLElement | null>(null)

// 列表操作列：默认收起（Collapsed）
const opColExpanded = ref(false)
const OP_COL_COLLAPSED_WIDTH = 43
const OP_COL_EXPANDED_WIDTH = 173
const OP_COL_EXPANDED_MIN_WIDTH = 160
const opColWidth = computed(() => (opColExpanded.value ? OP_COL_EXPANDED_WIDTH : OP_COL_COLLAPSED_WIDTH))
const opColMinWidth = computed(() => (opColExpanded.value ? OP_COL_EXPANDED_MIN_WIDTH : OP_COL_COLLAPSED_WIDTH))
function toggleOpCol() {
  opColExpanded.value = !opColExpanded.value
}

const sellInvoiceTableColumns = computed<CrmTableColumnDef[]>(() => [
  {
    key: 'invoiceStatus',
    label: t('financeSellInvoiceList.columns.invoiceStatus'),
    prop: 'invoiceStatus',
    width: 132,
    minWidth: 132,
    align: 'center'
  },
  { key: 'invoiceCode', label: t('financeSellInvoiceList.columns.code'), prop: 'invoiceCode', width: 160, minWidth: 160, showOverflowTooltip: true },
  { key: 'makeInvoiceDate', label: t('financeSellInvoiceList.columns.makeDate'), prop: 'makeInvoiceDate', width: 120, minWidth: 120 },
  { key: 'customerName', label: t('financeSellInvoiceList.columns.customer'), prop: 'customerName', minWidth: 160, showOverflowTooltip: true },
  { key: 'invoiceNo', label: t('financeSellInvoiceList.columns.invoiceNo'), prop: 'invoiceNo', width: 140, showOverflowTooltip: true },
  { key: 'invoiceTotal', label: t('financeSellInvoiceList.columns.amount'), prop: 'invoiceTotal', width: 130, align: 'right' },
  {
    key: 'matchStatus',
    label: t('financeSellInvoiceList.columns.matchStatus'),
    prop: 'matchStatus',
    width: 128,
    minWidth: 128,
    align: 'center'
  },
  { key: 'matchDone', label: t('financeSellInvoiceList.columns.matchDone'), prop: 'matchDone', width: 130, align: 'right' },
  {
    key: 'receiveStatus',
    label: t('financeSellInvoiceList.columns.receiveStatus'),
    prop: 'receiveStatus',
    width: 128,
    minWidth: 128,
    align: 'center'
  },
  { key: 'receiveDone', label: t('financeSellInvoiceList.columns.received'), prop: 'receiveDone', width: 130, align: 'right' },
  { key: 'remark', label: t('financeSellInvoiceList.columns.remark'), prop: 'remark', minWidth: 160, showOverflowTooltip: true },
  { key: 'sellInvoiceType', label: t('financeSellInvoiceList.columns.invoiceType'), prop: 'sellInvoiceType', width: 140, minWidth: 140 },
  { key: 'createTime', label: t('financeSellInvoiceList.columns.createdAt'), width: 140, minWidth: 140 },
  { key: 'createUser', label: t('financeSellInvoiceList.columns.createUser'), width: 120, showOverflowTooltip: true },
  {
    key: 'actions',
    label: t('financeSellInvoiceList.columns.actions'),
    width: opColWidth.value,
    minWidth: opColMinWidth.value,
    fixed: 'right',
    hideable: false,
    pinned: 'end',
    reorderable: false,
    className: 'op-col',
    labelClassName: 'op-col',
  resizable: false
  }
])

const stats = reactive({ totalAmount: 0, receivedAmount: 0, toReceiveAmount: 0, invoicedCount: 0 })

const handleSearch = () => {
  query.page = 1
  void loadData()
}

const handleReset = () => {
  query.keyword = ''
  query.status = undefined
  filterMatchStatus.value = undefined
  filterReceiveStatus.value = undefined
  dateRange.value = null
  query.startDate = undefined
  query.endDate = undefined
  query.page = 1
  void loadData()
}

const loadData = async () => {
  loading.value = true
  if (dateRange.value) {
    query.startDate = dateRange.value[0]
    query.endDate = dateRange.value[1]
  } else {
    query.startDate = undefined
    query.endDate = undefined
  }
  try {
    const res = await financeSellInvoiceApi.getList({
      ...query,
      invoiceStatus: query.status,
      matchStatus: filterMatchStatus.value,
      receiveStatus: filterReceiveStatus.value,
    } as PageQuery & { invoiceStatus?: number; matchStatus?: number; receiveStatus?: number })
    tableData.value = res.items || []
    total.value = res.total || 0
  } catch {
    tableData.value = getMockData()
    total.value = tableData.value.length
  } finally {
    loading.value = false
    stats.totalAmount = tableData.value.reduce((s, r) => s + r.invoiceTotal, 0)
    stats.receivedAmount = tableData.value.reduce((s, r) => s + r.receiveDone, 0)
    stats.toReceiveAmount = tableData.value.reduce((s, r) => s + r.receiveToBe, 0)
    stats.invoicedCount = tableData.value.filter(r => r.invoiceStatus === 100).length
  }
}

const getMockData = (): FinanceSellInvoice[] => [
  { id: '1', invoiceCode: 'SIN-2026-0001', customerId: 'c1', customerName: '北京科技有限公司', invoiceNo: '41200000000001', invoiceTotal: 256000, makeInvoiceDate: '2026-03-12', receiveStatus: 2, receiveDone: 256000, receiveToBe: 0, currency: 1, type: 10, invoiceStatus: 100, sellInvoiceType: 100, remark: '', createdAt: '2026-03-12' },
  { id: '2', invoiceCode: 'SIN-2026-0002', customerId: 'c2', customerName: '上海智能制造股份', invoiceNo: '41200000000002', invoiceTotal: 88500, makeInvoiceDate: '2026-03-14', receiveStatus: 1, receiveDone: 50000, receiveToBe: 38500, currency: 1, type: 10, invoiceStatus: 100, sellInvoiceType: 200, remark: '', createdAt: '2026-03-14' },
  { id: '3', invoiceCode: 'SIN-2026-0003', customerId: 'c3', customerName: 'Acme Corp', invoiceNo: undefined, invoiceTotal: 15800, makeInvoiceDate: undefined, receiveStatus: 0, receiveDone: 0, receiveToBe: 15800, currency: 2, type: 10, invoiceStatus: 1, sellInvoiceType: 100, remark: '待申请开票', createdAt: '2026-03-16' },
  { id: '4', invoiceCode: 'SIN-2026-0004', customerId: 'c4', customerName: '广州电子科技', invoiceNo: '41200000000004', invoiceTotal: 43200, makeInvoiceDate: '2026-03-15', receiveStatus: 0, receiveDone: 0, receiveToBe: 43200, currency: 1, type: 10, invoiceStatus: 100, sellInvoiceType: 100, remark: '', createdAt: '2026-03-15' },
  { id: '5', invoiceCode: 'SIN-2026-0005', customerId: 'c1', customerName: '北京科技有限公司', invoiceNo: '41200000000005', invoiceTotal: 19800, makeInvoiceDate: '2026-03-09', receiveStatus: 2, receiveDone: 19800, receiveToBe: 0, currency: 1, type: 20, invoiceStatus: -1, sellInvoiceType: 200, remark: '已作废', createdAt: '2026-03-09' },
]

const dialogVisible = ref(false)
const editingId = ref<string | null>(null)
const saving = ref(false)
const form = reactive<Partial<FinanceSellInvoice>>({
  customerId: '',
  customerName: '',
  invoiceNo: '',
  invoiceTotal: 0,
  makeInvoiceDate: undefined,
  sellInvoiceType: 100,
  type: 10,
  currency: DEFAULT_SETTLEMENT_CURRENCY_CODE,
  remark: '',
})

const openCreate = () => {
  editingId.value = null
  customerOptions.value = []
  Object.assign(form, {
    customerId: '',
    customerName: '',
    invoiceNo: '',
    invoiceTotal: 0,
    makeInvoiceDate: undefined,
    sellInvoiceType: 100,
    type: 10,
    currency: DEFAULT_SETTLEMENT_CURRENCY_CODE,
    remark: '',
  })
  dialogVisible.value = true
}

const openEdit = (row: FinanceSellInvoice) => {
  editingId.value = row.id
  Object.assign(form, { ...row })
  customerOptions.value = row.customerId
    ? [{ value: row.customerId, label: row.customerName || t('financeSellInvoiceList.customerFallback') }]
    : []
  dialogVisible.value = true
}

const saveForm = async () => {
  if (!form.customerId?.trim()) {
    ElMessage.warning(t('financeSellInvoiceList.messages.selectCustomer'))
    return
  }
  saving.value = true
  try {
    if (editingId.value) {
      await financeSellInvoiceApi.update(editingId.value, form)
    } else {
      await financeSellInvoiceApi.create(form)
    }
    ElMessage.success(t('financeSellInvoiceList.messages.saveOk'))
    dialogVisible.value = false
    loadData()
  } catch {
    ElMessage.success(t('financeSellInvoiceList.messages.saveOkDemo'))
    dialogVisible.value = false
  } finally {
    saving.value = false
  }
}

const openDetail = (row: FinanceSellInvoice) => {
  router.push({ name: 'FinanceSellInvoiceDetail', params: { id: row.id } })
}

function onSellInvoiceRowDblClick(row: FinanceSellInvoice, _column: unknown, event?: MouseEvent) {
  onCrmDetailListRowDblClick(row, _column, event, {
    canEdit: canWriteFinanceSellInvoice.value && row.invoiceStatus === 1,
    onEdit: openEdit,
    onDefault: openDetail,
  })
}

const applyInvoice = async (row: FinanceSellInvoice) => {
  await ElMessageBox.confirm(
    t('financeSellInvoiceList.messages.applyMsg', { code: row.invoiceCode || '' }),
    t('financeSellInvoiceList.messages.applyTitle'),
    { type: 'info' }
  )
  await financeSellInvoiceApi.submitApplication(row.id)
  ElMessage.success(t('financeSellInvoiceList.messages.applied'))
  await loadData()
}

const markIssued = async (row: FinanceSellInvoice) => {
  await ElMessageBox.confirm(
    t('financeSellInvoiceList.messages.markIssuedMsg', { code: row.invoiceCode || '' }),
    t('financeSellInvoiceList.messages.markIssuedTitle'),
    { type: 'info' }
  )
  await financeSellInvoiceApi.markIssued(row.id)
  ElMessage.success(t('financeSellInvoiceList.messages.markIssuedOk'))
  await loadData()
}

const markIssueFailed = async (row: FinanceSellInvoice) => {
  await ElMessageBox.confirm(
    t('financeSellInvoiceList.messages.markIssueFailedMsg', { code: row.invoiceCode || '' }),
    t('financeSellInvoiceList.messages.markIssueFailedTitle'),
    { type: 'warning' }
  )
  await financeSellInvoiceApi.markIssueFailed(row.id)
  ElMessage.success(t('financeSellInvoiceList.messages.markIssueFailedOk'))
  await loadData()
}

const voidInvoice = async (row: FinanceSellInvoice) => {
  await ElMessageBox.confirm(
    t('financeSellInvoiceList.messages.voidMsg', { code: row.invoiceCode || '' }),
    t('financeSellInvoiceList.messages.voidTitle'),
    { type: 'warning' }
  )
  await financeSellInvoiceApi.void(row.id)
  ElMessage.success(t('financeSellInvoiceList.messages.voided'))
  await loadData()
}

const handleDeleteRow = async (row: FinanceSellInvoice) => {
  const code = row.invoiceCode || row.invoiceNo || row.id
  await ElMessageBox.confirm(
    `确认删除销项发票 ${code} 吗？`,
    '删除确认',
    { type: 'warning' }
  )
  await financeSellInvoiceApi.delete(row.id)
  ElMessage.success('删除成功')
  await loadData()
}

const handleForceDeleteRow = async (row: FinanceSellInvoice) => {
  const code = row.invoiceCode || row.id
  const entered = window.prompt('请输入发票单号以确认强制删除', String(code || ''))?.trim() ?? ''
  if (!entered) return
  if (entered !== String(code || '').trim()) {
    ElMessage.error('输入单号不匹配，已取消')
    return
  }
  try {
    await financeSellInvoiceApi.forceDelete(row.id, entered)
    ElMessage.success('强制删除成功')
    await loadData()
  } catch (e: any) {
    ElMessage.error(e?.message || '强制删除失败')
  }
}

onMounted(loadData)

function goWriteOffDesktop() {
  router.push({ name: 'SellInvoiceWriteOffDesktop' })
}
</script>

<style lang="scss" scoped>
@use '@/assets/styles/variables' as vars;
@import './finance-common.scss';

.fsi-list-page-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
}

.btn-write-off-desktop {
  display: inline-flex;
  align-items: center;
  gap: 10px;
  padding: 8px 16px 8px 18px;
  border: none;
  border-radius: 10px;
  background: #eaf5ff;
  color: #1a2332;
  font-size: 13px;
  font-weight: 500;
  font-family: 'Noto Sans SC', sans-serif;
  line-height: 1.2;
  cursor: pointer;
  transition: background 0.15s, color 0.15s;
  flex-shrink: 0;

  &:hover {
    background: #ddefff;
    color: #0f172a;
  }

  &:active {
    background: #d0e8ff;
  }

  &__arrow {
    font-size: 14px;
    color: #64748b;
  }
}

.search-input-wrap {
  position: relative;
  display: flex;
  align-items: center;
}

.search-icon {
  position: absolute;
  left: 10px;
  color: vars.$text-muted;
  pointer-events: none;
}

.search-input {
  width: 240px;
  padding: 7px 12px 7px 32px;
  background: vars.$layer-2;
  border: 1px solid vars.$border-panel;
  border-radius: vars.$border-radius-md;
  color: vars.$text-primary;
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
  outline: none;
  transition: border-color 0.2s;

  &::placeholder {
    color: vars.$text-muted;
  }

  &:focus {
    border-color: rgba(0, 212, 255, 0.4);
  }
}

.filter-select {
  width: 130px;

  :deep(.el-select__wrapper) {
    background: vars.$layer-2 !important;
    box-shadow: none !important;
    border: 1px solid vars.$border-panel !important;
    border-radius: vars.$border-radius-md !important;
  }
}

.filter-date-range {
  width: 260px;
  flex-shrink: 0;

  :deep(.el-input__wrapper) {
    background: vars.$layer-2 !important;
    box-shadow: none !important;
    border: 1px solid vars.$border-panel !important;
    border-radius: vars.$border-radius-md !important;
  }
}

.btn-primary {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 8px 16px;
  background: linear-gradient(135deg, rgba(0, 102, 255, 0.8), rgba(0, 212, 255, 0.7));
  border: 1px solid rgba(0, 212, 255, 0.4);
  border-radius: vars.$border-radius-md;
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
    letter-spacing: 0;
  }
}

.btn-ghost {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 8px 16px;
  background: transparent;
  border: 1px solid vars.$border-panel;
  border-radius: vars.$border-radius-md;
  color: vars.$text-muted;
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  transition: all 0.2s;

  &:hover:not(:disabled) {
    border-color: rgba(0, 212, 255, 0.35);
    color: vars.$text-primary;
  }

  &:disabled {
    opacity: 0.55;
    cursor: not-allowed;
  }

  &.btn-sm {
    padding: 6px 12px;
    font-size: 12px;
  }
}

.select-hint {
  padding: 8px 12px;
  color: rgba(80, 187, 227, 0.55);
  font-size: 12px;
}

.pagination-wrapper {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 12px;
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
}
</style>
