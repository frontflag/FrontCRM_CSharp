<template>
  <div class="quote-list-page">
    <!-- 页面头部 -->
    <div class="page-header">
      <div class="header-left">
        <h1 class="page-title">{{ t('quoteList.title') }}</h1>
        <div class="count-badge">{{ t('quoteList.count', { count: totalCount }) }}</div>
      </div>
      <div class="header-right">
        <el-button
          type="primary"
          :disabled="!selectedQuotes.length"
          :loading="salesOrderPreflightLoading"
          @click="handleGenerateSalesOrder"
        >
          {{ t('quoteList.generateSalesOrder') }}
        </el-button>
      </div>
    </div>

    <!-- 统计卡片 -->
    <el-row :gutter="20" class="stat-row">
      <el-col :span="6">
        <el-card class="stat-card">
          <div class="stat-value">{{ stats.total }}</div>
          <div class="stat-label">{{ t('quoteList.stats.total') }}</div>
        </el-card>
      </el-col>
      <el-col :span="6">
        <el-card class="stat-card stat-warning">
          <div class="stat-value">{{ stats.pending }}</div>
          <div class="stat-label">{{ t('quoteList.stats.pending') }}</div>
        </el-card>
      </el-col>
      <el-col :span="6">
        <el-card class="stat-card stat-cyan">
          <div class="stat-value">{{ stats.sent }}</div>
          <div class="stat-label">{{ t('quoteList.stats.sent') }}</div>
        </el-card>
      </el-col>
      <el-col :span="6">
        <el-card class="stat-card stat-success">
          <div class="stat-value">{{ stats.accepted }}</div>
          <div class="stat-label">{{ t('quoteList.stats.accepted') }}</div>
        </el-card>
      </el-col>
    </el-row>

    <!-- 搜索面板 -->
    <el-card class="filter-card">
      <el-form :inline="true" :model="searchForm">
        <el-form-item :label="t('quoteList.filters.search')">
          <el-input 
            v-model="searchForm.keyword" 
            :placeholder="t('quoteList.filters.placeholder')"
            clearable
            @keyup.enter="handleSearch"
            style="width: 280px"
          />
        </el-form-item>
        <el-form-item :label="t('quoteList.filters.status')">
          <el-select v-model="searchForm.status" :placeholder="t('quoteList.filters.allStatus')" clearable style="width: 140px">
            <el-option :label="t('quoteList.status.draft')" :value="0" />
            <el-option :label="t('quoteList.status.pending')" :value="1" />
            <el-option :label="t('quoteList.status.approved')" :value="2" />
            <el-option :label="t('quoteList.status.sent')" :value="3" />
            <el-option :label="t('quoteList.status.accepted')" :value="4" />
            <el-option :label="t('quoteList.status.rejected')" :value="5" />
            <el-option :label="t('quoteList.status.expired')" :value="6" />
            <el-option :label="t('quoteList.status.closed')" :value="7" />
          </el-select>
        </el-form-item>
        <el-form-item>
          <el-button type="primary" @click="handleSearch">
            <el-icon><Search /></el-icon>{{ t('quoteList.filters.query') }}
          </el-button>
          <el-button @click="handleReset">{{ t('quoteList.filters.reset') }}</el-button>
        </el-form-item>
      </el-form>
    </el-card>

    <!-- 数据表格 -->
    <el-card class="table-card">
      <CrmDataTable
        ref="dataTableRef"
        column-layout-key="quote-list-main"
        :columns="quoteTableColumns"
        :show-column-settings="false"
        :density-toggle-anchor-el="rowDensityToggleAnchorEl"
        :data="quoteList"
        v-loading="loading"
        row-key="id"
        highlight-current-row
        @selection-change="onQuoteSelectionChange"
        @row-dblclick="handleEdit"
      >
        <template #col-quoteCode="{ row }">
          <span class="quote-code-cell">{{ displayQuoteCode(row) }}</span>
        </template>
        <template #col-status="{ row }">
          <el-tag effect="dark" :type="getStatusType(row.status)" size="small">
            {{ getStatusText(row.status) }}
          </el-tag>
        </template>
        <template #col-rfqCode="{ row }">
          <span>{{ displayRfqCode(row) }}</span>
        </template>
        <template #col-brand="{ row }">
          <span>{{ displayFirstItemBrand(row) }}</span>
        </template>
        <template #col-lineUnitPrice="{ row }">
          <span>{{ displayFirstItemUnitPrice(row) }}</span>
        </template>
        <template #col-lineQuantity="{ row }">
          <span>{{ displayFirstItemQuantity(row) }}</span>
        </template>
        <template #col-vendorCount="{ row }">
          {{ maskPurchaseSensitiveFields ? '—' : (row.items?.length || 0) }}
        </template>
        <template #col-quoteDate="{ row }">
          {{ formatDisplayDate(row.quoteDate) }}
        </template>
        <template #col-createTime="{ row }">
          <template v-for="p in [formatDisplayDateTime2DigitYearParts(row.createTime)]" :key="`ct-${row.id}`">
            <span v-if="p" class="crm-quote-create-time">
              <span class="crm-quote-create-time__ymd">{{ p.date }}</span>
              <span class="crm-quote-create-time__hm">{{ p.time }}</span>
            </span>
            <span v-else>—</span>
          </template>
        </template>
        <template #col-createUser="{ row }">
          {{
            row.createUserName ||
              row.createdBy ||
              (!maskSaleSensitiveFields ? row.salesUserName : '') ||
              row.purchaseUserName ||
              '—'
          }}
        </template>
        <template #col-actions-header>
          <div class="op-col-header">
            <span class="op-col-header-text">{{ t('quoteList.actions.column') }}</span>
            <button type="button" class="op-col-toggle-btn" @click.stop="toggleOpCol">
              {{ opColExpanded ? '>' : '<' }}
            </button>
          </div>
        </template>
        <template #col-actions="{ row }">
          <div v-if="opColExpanded" @click.stop @dblclick.stop>
            <div class="action-btns">
              <el-button link type="primary" @click.stop="handleEdit(row)">{{ t('quoteList.actions.edit') }}</el-button>
              <el-button link type="danger" @click.stop="handleDelete(row)">{{ t('quoteList.actions.delete') }}</el-button>
            </div>
          </div>
          <el-dropdown v-else trigger="click" placement="bottom-end">
            <div class="op-more-dropdown-trigger" @click.stop @dblclick.stop>
              <button type="button" class="op-more-trigger">...</button>
            </div>
            <template #dropdown>
              <el-dropdown-menu>
                <el-dropdown-item @click.stop="handleEdit(row)">
                  <span class="op-more-item op-more-item--primary">{{ t('quoteList.actions.edit') }}</span>
                </el-dropdown-item>
                <el-dropdown-item @click.stop="handleDelete(row)">
                  <span class="op-more-item op-more-item--danger">{{ t('quoteList.actions.delete') }}</span>
                </el-dropdown-item>
              </el-dropdown-menu>
            </template>
          </el-dropdown>
        </template>
      </CrmDataTable>

      <!-- 底栏：列设置（图标+Tip+Spacer） + 分页（顶对齐） -->
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
          v-model:current-page="pageInfo.page"
          v-model:page-size="pageInfo.pageSize"
          :total="pageInfo.total"
          :page-sizes="[10, 20, 50, 100]"
          layout="total, sizes, prev, pager, next, jumper"
          @size-change="handleSizeChange"
          @current-change="handlePageChange"
        />
      </div>
    </el-card>

  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { Search, Setting } from '@element-plus/icons-vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { quoteApi } from '@/api/quote'
import { CURRENCY_CODE_TO_TEXT } from '@/constants/currency'
import { assertQuotesSameCustomer } from '@/utils/quoteSalesOrderPrefill'
import { formatDisplayDate, formatDisplayDateTime2DigitYearParts } from '@/utils/displayDateTime'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'
import CrmDataTable from '@/components/CrmDataTable.vue'
import { usePurchaseSensitiveFieldMask } from '@/composables/usePurchaseSensitiveFieldMask'
import { useSaleSensitiveFieldMask } from '@/composables/useSaleSensitiveFieldMask'

const { maskPurchaseSensitiveFields } = usePurchaseSensitiveFieldMask()
const { maskSaleSensitiveFields } = useSaleSensitiveFieldMask()
const router = useRouter()
const route = useRoute()
const { t } = useI18n()

const loading = ref(false)
const dataTableRef = ref<InstanceType<typeof CrmDataTable> | null>(null)
const rowDensityToggleAnchorEl = ref<HTMLElement | null>(null)
const selectedQuotes = ref<any[]>([])
const salesOrderPreflightLoading = ref(false)
const quoteList = ref<any[]>([])
const stats = ref({ total: 0, pending: 0, sent: 0, accepted: 0 })

// 搜索表单
const searchForm = ref({
  keyword: '',
  status: undefined as number | undefined
})

// 分页信息
const pageInfo = ref({
  page: 1,
  pageSize: 10,
  total: 0
})

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

/** 报价列表主表可配置列（localStorage：crm-table-columns:v1:quote-list-main） */
const quoteTableColumns = computed<CrmTableColumnDef[]>(() => {
  const cols: CrmTableColumnDef[] = [
    {
      key: 'sel',
      type: 'selection',
      width: 48,
      hideable: false,
      pinned: 'start',
      resizable: false,
      reserveSelection: true
    },
    { key: 'status', label: t('quoteList.columns.status'), prop: 'status', width: 160, align: 'center' },
    { key: 'mpn', label: t('quoteList.columns.mpn'), prop: 'mpn', minWidth: 150, showOverflowTooltip: true },
    { key: 'brand', label: t('quoteList.columns.brand'), width: 100, minWidth: 90, showOverflowTooltip: true },
    {
      key: 'lineUnitPrice',
      label: t('quoteList.columns.unitPrice'),
      width: 130,
      minWidth: 120,
      align: 'right',
      showOverflowTooltip: true
    },
    { key: 'lineQuantity', label: t('quoteList.columns.quantity'), width: 88, minWidth: 72, align: 'right' }
  ]
  if (!maskSaleSensitiveFields.value) {
    cols.push(
      { key: 'customerName', label: t('quoteList.columns.customer'), prop: 'customerName', minWidth: 200, showOverflowTooltip: true },
      { key: 'salesUserName', label: t('quoteList.columns.salesUser'), prop: 'salesUserName', width: 100 }
    )
  }
  cols.push(
    { key: 'purchaseUserName', label: t('quoteList.columns.purchaseUser'), prop: 'purchaseUserName', width: 100 },
    { key: 'vendorCount', label: t('quoteList.columns.vendorCount'), width: 90, align: 'center' },
    { key: 'quoteDate', label: t('quoteList.columns.quoteDate'), prop: 'quoteDate', width: 160 },
    {
      key: 'quoteCode',
      label: t('quoteList.columns.quoteCode'),
      prop: 'quoteCode',
      width: 160,
      minWidth: 160,
      showOverflowTooltip: true,
      sortable: true
    },
    { key: 'rfqCode', label: t('quoteList.columns.rfqCode'), width: 160, minWidth: 160, showOverflowTooltip: true },
    { key: 'createTime', label: t('quoteList.columns.createTime'), prop: 'createTime', width: 160 },
    { key: 'createUser', label: t('quoteList.columns.createUser'), width: 120, showOverflowTooltip: true },
    {
      key: 'actions',
      label: t('quoteList.actions.column'),
      width: opColWidth.value,
      minWidth: opColMinWidth.value,
      fixed: 'right',
      hideable: false,
      pinned: 'end',
      reorderable: false,
      className: 'op-col',
      labelClassName: 'op-col'
    }
  )
  return cols
})

const totalCount = computed(() => quoteList.value.length)

/** 兼容 camelCase / PascalCase / 后端字段，避免编号列空白 */
function displayQuoteCode(row: Record<string, unknown>) {
  const v =
    row.quoteCode ??
    row.quoteNumber ??
    row.QuoteCode ??
    row.QuoteNumber
  if (v != null && String(v).trim() !== '') return String(v)
  return t('quoteList.na')
}

/** 主需求单需求编号（与明细关联的 rfqId 对应主表 rfqCode；后端可直接返回 rfqCode） */
function displayRfqCode(row: Record<string, unknown>) {
  const v =
    row.rfqCode ??
    row.RfqCode ??
    row.RFQCode ??
    row.rfqNumber ??
    row.RfqNumber
  if (v != null && String(v).trim() !== '') return String(v)
  return t('quoteList.na')
}

function firstQuoteItem(row: Record<string, unknown>): Record<string, unknown> | null {
  const items = row.items ?? row.Items
  if (!Array.isArray(items) || items.length === 0) return null
  return items[0] as Record<string, unknown>
}

function displayFirstItemBrand(row: Record<string, unknown>) {
  const it = firstQuoteItem(row)
  if (!it) return t('quoteList.na')
  const b = it.brand ?? it.Brand
  if (b != null && String(b).trim() !== '') return String(b)
  return t('quoteList.na')
}

function displayFirstItemUnitPrice(row: Record<string, unknown>) {
  const it = firstQuoteItem(row)
  if (!it) return t('quoteList.na')
  const p = it.unitPrice ?? it.UnitPrice
  if (p == null || p === '') return t('quoteList.na')
  const n = Number(p)
  if (Number.isNaN(n)) return t('quoteList.na')
  const ccy = Number(it.currency ?? it.Currency ?? 1)
  const ccyLabel = CURRENCY_CODE_TO_TEXT[ccy] ?? String(ccy)
  return `${n.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 6 })} ${ccyLabel}`
}

function displayFirstItemQuantity(row: Record<string, unknown>) {
  const it = firstQuoteItem(row)
  if (!it) return t('quoteList.na')
  const q = it.quantity ?? it.Quantity
  if (q == null || q === '') return t('quoteList.na')
  const n = Number(q)
  if (Number.isNaN(n)) return t('quoteList.na')
  return String(n)
}

// 状态处理
const getStatusType = (status: number) => {
  const map: Record<number, string> = { 
    0: 'info', 1: 'warning', 2: 'primary', 3: 'success',
    4: 'success', 5: 'danger', 6: 'info', 7: 'info'
  }
  return map[status] || 'info'
}

const getStatusText = (status: number) => {
  const map: Record<number, string> = {
    0: t('quoteList.status.draft'), 1: t('quoteList.status.pending'), 2: t('quoteList.status.approved'), 3: t('quoteList.status.sent'),
    4: t('quoteList.status.accepted'), 5: t('quoteList.status.rejected'), 6: t('quoteList.status.expired'), 7: t('quoteList.status.closed')
  }
  return map[status] || t('quoteList.status.unknown')
}

// 计算统计
const calculateStats = () => {
  stats.value = {
    total: quoteList.value.length,
    pending: quoteList.value.filter(q => q.status === 0 || q.status === 1).length,
    sent: quoteList.value.filter(q => q.status === 3).length,
    accepted: quoteList.value.filter(q => q.status === 4).length
  }
}

// 加载数据
const loadData = async () => {
  loading.value = true
  try {
    const res = await quoteApi.getList(searchForm.value)
    quoteList.value = res.data || []
    pageInfo.value.total = res.total || 0
    calculateStats()
  } catch (error) {
    ElMessage.error(t('quoteList.loadFailed'))
  } finally {
    loading.value = false
  }
}

// 搜索和重置
const handleSearch = () => {
  pageInfo.value.page = 1
  loadData()
}

const handleReset = () => {
  searchForm.value = { keyword: '', status: undefined }
  loadData()
}

// 分页
const handleSizeChange = (val: number) => {
  pageInfo.value.pageSize = val
  loadData()
}

const handlePageChange = (val: number) => {
  pageInfo.value.page = val
  loadData()
}

function onQuoteSelectionChange(rows: any[]) {
  selectedQuotes.value = rows
}

function resolveQuoteId(row: Record<string, unknown>): string {
  const id = row.id ?? row.Id
  return id != null ? String(id).trim() : ''
}

/** PRD：quoteIds[] + returnTo；跳转前校验同一客户 */
async function handleGenerateSalesOrder() {
  const rows = selectedQuotes.value
  if (!rows.length) {
    ElMessage.warning(t('quoteList.warnings.selectFirst'))
    return
  }
  const ids = [...new Set(rows.map((r) => resolveQuoteId(r)).filter(Boolean))]
  if (!ids.length) {
    ElMessage.warning(t('quoteList.warnings.invalidId'))
    return
  }
  salesOrderPreflightLoading.value = true
  try {
    const check = await assertQuotesSameCustomer(ids)
    if (!check.ok) {
      ElMessage.error(check.message)
      return
    }
    router.push({
      name: 'SalesOrderCreate',
      query: { quoteIds: ids.join(','), returnTo: route.fullPath }
    })
  } finally {
    salesOrderPreflightLoading.value = false
  }
}

// 编辑
const handleEdit = (row: any) => {
  router.push({ name: 'QuoteEdit', params: { id: String(row.id) } })
}

// 删除
const handleDelete = async (row: any) => {
  try {
    await ElMessageBox.confirm(
      t('quoteList.deleteConfirm', { code: displayQuoteCode(row) }),
      t('quoteList.deleteTitle'),
      { type: 'warning' }
    )
    await quoteApi.delete(row.id)
    loadData()
  } catch {
    // 取消
  }
}

onMounted(loadData)
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.quote-list-page {
  padding: 20px;
}

.page-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 20px;
  .header-right {
    display: flex;
    align-items: center;
    gap: 10px;
  }
  .page-title {
    margin: 0;
    color: #E8F4FF;
    font-size: 20px;
  }
  .count-badge {
    margin-left: 10px;
    padding: 2px 10px;
    background: rgba(0, 212, 255, 0.1);
    border: 1px solid rgba(0, 212, 255, 0.3);
    border-radius: 12px;
    font-size: 12px;
    color: #00D4FF;
  }
}

.stat-row {
  margin-bottom: 20px;
}

.stat-card {
  text-align: center;
  background: $layer-3;
  border: 1px solid $border-card;
  :deep(.el-card__body) {
    padding: 15px;
  }
  .stat-value {
    font-size: 24px;
    font-weight: bold;
    color: $cyan-primary;
    margin-bottom: 5px;
  }
  .stat-label {
    font-size: 14px;
    color: $text-muted;
  }
  &.stat-warning .stat-value {
    color: $warning-color;
  }
  &.stat-cyan .stat-value {
    color: $cyan-primary;
  }
  &.stat-success .stat-value {
    color: $success-color;
  }
}

.filter-card {
  margin-bottom: 20px;
  background: #0A1628;
  border: 1px solid rgba(0, 212, 255, 0.1);
}

.table-card {
  background: #0A1628;
  border: 1px solid rgba(0, 212, 255, 0.1);
  :deep(.el-table) {
    background: transparent;
    --el-table-header-bg-color: rgba(0, 212, 255, 0.1);
    --el-table-tr-bg-color: transparent;
    --el-table-border-color: rgba(0, 212, 255, 0.1);
    color: #E8F4FF;

    // 操作列按钮禁止折行
    .el-table__cell .el-button {
      white-space: nowrap !important;
    }
    .el-table__cell .cell {
      white-space: nowrap;
    }
  }
}

.pagination-wrapper {
  margin-top: 20px;
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 12px 16px;
  flex-wrap: wrap;
}

.list-footer-left {
  display: inline-flex;
  align-items: flex-start;
  gap: 6px;
  flex-shrink: 0;
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

/* 与 .crm-items-table 正文色一致，避免偏青或与链接触觉混淆 */
.quote-code-cell {
  color: $text-primary !important;
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

.op-more-item--danger {
  color: $color-red-brown;
}

</style>
