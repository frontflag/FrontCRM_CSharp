<template>
  <div class="rfq-item-list-page customer-list-theme">
    <div class="rfq-item-main">
    <div class="page-header">
      <div class="header-left">
        <div class="page-title-group">
          <div class="page-icon">明</div>
          <h1 class="page-title">需求明细</h1>
        </div>
        <div class="count-badge">共 {{ totalCount }} 条</div>
      </div>
      <div class="header-right">
        <el-button class="btn-primary" type="primary" :disabled="!selectedRows.length" @click="handleBatchQuote">
          批量报价
        </el-button>
      </div>
    </div>

    <el-card class="filter-card search-bar">
      <el-form :inline="true" :model="searchForm">
        <el-form-item label="需求创建日期">
          <el-date-picker
            v-model="dateRange"
            type="daterange"
            range-separator="至"
            start-placeholder="开始"
            end-placeholder="结束"
            value-format="YYYY-MM-DD"
            clearable
          />
        </el-form-item>
        <el-form-item label="客户">
          <el-input v-model="searchForm.customerKeyword" placeholder="客户名称模糊" clearable style="width: 180px" />
        </el-form-item>
        <el-form-item label="物料型号">
          <el-input v-model="searchForm.materialModel" placeholder="MPN / 客户料号" clearable style="width: 160px" />
        </el-form-item>
        <el-form-item label="业务员">
          <el-input v-model="searchForm.salesUserKeyword" placeholder="姓名/账号模糊" clearable style="width: 160px" />
        </el-form-item>
        <el-form-item>
          <el-button class="btn-primary btn-sm" type="primary" @click="handleSearch">查询</el-button>
          <el-button class="btn-ghost btn-sm" @click="handleReset">重置</el-button>
        </el-form-item>
      </el-form>
    </el-card>

    <el-card class="table-card table-wrapper">
      <CrmDataTable
        :data="tableData"
        v-loading="loading"
        row-key="id"
        highlight-current-row
        @row-click="onItemRowClick"
        @selection-change="onSelectionChange"
      >
        <el-table-column label="明细状态" width="160" min-width="160" align="center" resizable>
          <template #default="{ row }">
            <el-tag size="small" effect="dark">{{ itemStatusText(row.status) }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column type="selection" width="48" :resizable="false" :reserve-selection="true" />
        <el-table-column label="需求创建" width="160" min-width="160" resizable>
          <template #default="{ row }">{{ formatDate(row.rfqCreateTime) }}</template>
        </el-table-column>
        <el-table-column prop="rfqCode" label="需求编号" width="160" min-width="160" show-overflow-tooltip resizable />
        <el-table-column prop="customerName" label="客户" min-width="200" show-overflow-tooltip resizable />
        <el-table-column label="物料型号" min-width="120" show-overflow-tooltip resizable>
          <template #default="{ row }">{{ row.materialModel || row.mpn || '—' }}</template>
        </el-table-column>
        <el-table-column label="客户料号" width="120" min-width="88" show-overflow-tooltip resizable>
          <template #default="{ row }">{{ row.customerMaterialModel || row.customerMpn || '—' }}</template>
        </el-table-column>
        <el-table-column prop="quantity" label="数量" width="90" min-width="72" align="right" resizable />
        <el-table-column prop="salesUserName" label="业务员" width="100" min-width="80" show-overflow-tooltip resizable />
        <el-table-column label="询价采购员" min-width="160" show-overflow-tooltip resizable>
          <template #default="{ row }">{{ formatAssignedPurchasers(row) }}</template>
        </el-table-column>
        <el-table-column label="报价条目" width="96" min-width="80" align="center" resizable>
          <template #default="{ row }">
            {{ quoteRecordCountByRfqItemId[row.id] ?? 0 }}
          </template>
        </el-table-column>
        <el-table-column label="创建时间" width="160" show-overflow-tooltip resizable>
          <template #default="{ row }">{{ formatDate(row.createTime || row.rfqCreateTime) }}</template>
        </el-table-column>
        <el-table-column label="创建人" width="120" show-overflow-tooltip resizable>
          <template #default="{ row }">{{ row.createUserName || row.createdBy || row.salesUserName || '—' }}</template>
        </el-table-column>
        <el-table-column label="操作" width="200" min-width="200" fixed="right" resizable>
          <template #default="{ row }">
            <el-button class="action-btn" link type="primary" @click="goDetail(row)">详情</el-button>
            <el-button class="action-btn" link type="success" @click="goQuote(row)">报价</el-button>
          </template>
        </el-table-column>
      </CrmDataTable>

      <div class="pagination-wrapper">
        <el-pagination
          class="quantum-pagination"
          v-model:current-page="pageInfo.page"
          v-model:page-size="pageInfo.pageSize"
          :total="pageInfo.total"
          :page-sizes="[10, 20, 50, 100]"
          layout="total, sizes, prev, pager, next, jumper"
          @size-change="loadData"
          @current-change="loadData"
        />
      </div>
    </el-card>
    </div>

    <!-- 底部：供应商报价（当前选中需求明细对应的报价列表） -->
    <div class="supplier-quote-dock" :class="{ collapsed: !supplierPanelExpanded }">
      <div class="dock-header">
        <div class="dock-header-top">
          <span class="dock-title">供应商报价</span>
          <div class="dock-header-actions">
            <el-button
              class="dock-toggle"
              text
              circle
              :title="supplierPanelExpanded ? '收起' : '展开'"
              @click="supplierPanelExpanded = !supplierPanelExpanded"
            >
              <el-icon :size="18">
                <ArrowUp v-if="supplierPanelExpanded" />
                <ArrowDown v-else />
              </el-icon>
            </el-button>
          </div>
        </div>
        <!-- 与新建报价页提示栏同一套字段与拉数逻辑 -->
        <div
          v-show="supplierPanelExpanded && selectedRfqItem"
          v-loading="dockSummaryLoading"
          class="dock-link-alert-wrap"
        >
          <div v-if="dockLinkAlert" class="dock-link-alert-title-row">
            <span class="la-block-rfq">
              <span class="la-muted">报价需求</span><span class="la-pre">{{ linkAlertGap2 }}</span
              ><span class="la-strong la-rfq-val">{{ dockLinkAlert.linkAlertRfqDisplay }}</span>
            </span>
            <span class="la-pre">{{ linkAlertSep8Ideo }}</span>
            <span class="la-block-detail">
              <span class="la-muted">物料号</span><span class="la-pre">{{ linkAlertGap2 }}</span
              ><span class="la-value-green">{{ dockLinkAlert.mpn || '—' }}</span
              ><span class="la-pre">{{ linkAlertSep4Ideo }}</span><span class="la-muted">品牌</span
              ><span class="la-pre">{{ linkAlertGap2 }}</span
              ><span class="la-value-green">{{ dockLinkAlert.brand || '—' }}</span
              ><span class="la-pre">{{ linkAlertSep4Ideo }}</span><span class="la-muted">数量</span
              ><span class="la-pre">{{ linkAlertGap2 }}</span
              ><span class="la-value-green">{{ dockLinkAlert.quantityDisplay }}</span
              ><span class="la-pre">{{ linkAlertSep4Ideo }}</span><span class="la-muted">目标价</span
              ><span class="la-pre">{{ linkAlertGap2 }}</span
              ><span class="la-value-green">{{ dockLinkAlert.targetPriceText }}</span>
            </span>
          </div>
        </div>
      </div>
      <div v-show="supplierPanelExpanded" class="dock-body">
        <div v-if="!selectedRfqItem" class="dock-placeholder">请先点击上方需求明细行，查看对应报价</div>
        <template v-else>
          <div v-loading="quotesLoading" class="dock-table-wrap">
            <el-empty
              v-if="!quotesLoading && !quotesForItem.length"
              description="尚未报价"
              :image-size="72"
            />
            <CrmDataTable
              v-else-if="quotesForItem.length"
              embedded
              class="dock-quote-table"
              :data="quotesForItem"
              size="small"
              stripe
              max-height="208"
              :row-key="dockQuoteRowKey"
            >
              <el-table-column label="报价编号" width="160" min-width="160" show-overflow-tooltip>
                <template #default="{ row }">{{ displayQuoteCode(row) }}</template>
              </el-table-column>
              <el-table-column prop="mpn" label="物料型号" min-width="120" show-overflow-tooltip />
              <el-table-column label="状态" width="96" align="center">
                <template #default="{ row }">
                  <el-tag effect="dark" :type="quoteStatusType(row.status)" size="small">
                    {{ quoteStatusText(row.status) }}
                  </el-tag>
                </template>
              </el-table-column>
              <el-table-column prop="quoteDate" label="报价日期" width="110" />
              <el-table-column label="供应商数" width="88" align="center">
                <template #default="{ row }">{{ row.items?.length || 0 }}</template>
              </el-table-column>
              <el-table-column prop="salesUserName" label="业务员" width="100" show-overflow-tooltip />
              <el-table-column prop="createTime" label="创建时间" width="150" show-overflow-tooltip />
              <el-table-column label="操作" width="128" align="center" fixed="right">
                <template #default="{ row }">
                  <el-button
                    link
                    type="primary"
                    size="small"
                    :loading="dockRowSalesOrderQuoteId === resolveQuoteRowId(row)"
                    @click="handleDockRowGenerateSalesOrder(row)"
                  >
                    生成销售订单
                  </el-button>
                </template>
              </el-table-column>
            </CrmDataTable>
          </div>
        </template>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { ArrowUp, ArrowDown } from '@element-plus/icons-vue'
import { ElMessage } from 'element-plus'
import { rfqApi } from '@/api/rfq'
import { quoteApi } from '@/api/quote'
import { buildLinkAlertFieldsFromItem, fetchLinkedRfqItemRecord } from '@/utils/rfqLinkedItemSummary'
import { assertQuotesSameCustomer } from '@/utils/quoteSalesOrderPrefill'
import type { RFQItem } from '@/types/rfq'
import { formatDisplayDateTime } from '@/utils/displayDateTime'

const router = useRouter()
const route = useRoute()

const loading = ref(false)
const tableData = ref<RFQItem[]>([])
const totalCount = ref(0)
const selectedRows = ref<RFQItem[]>([])
const dateRange = ref<[string, string] | null>(null)

const searchForm = reactive({
  customerKeyword: '',
  materialModel: '',
  salesUserKeyword: ''
})

const pageInfo = reactive({
  page: 1,
  pageSize: 20,
  total: 0
})

/** 当前点击选中的需求明细（用于底部供应商报价面板） */
const selectedRfqItem = ref<RFQItem | null>(null)
const supplierPanelExpanded = ref(true)
const quotesForItem = ref<Record<string, unknown>[]>([])
const quotesLoading = ref(false)
/** 正在预检并跳转生成销售订单的报价行 id（行内按钮 loading） */
const dockRowSalesOrderQuoteId = ref<string | null>(null)

/** 每条需求明细对应的报价单数量（报价头 rfqItemId 与明细 id 一致） */
const quoteRecordCountByRfqItemId = ref<Record<string, number>>({})

/** 底部面板提示条（与 QuoteCreate 提示栏字段一致） */
const dockSummaryLoading = ref(false)
const dockLinkAlert = ref<{
  linkAlertRfqDisplay: string
  mpn: string
  brand: string
  quantityDisplay: string
  targetPriceText: string
} | null>(null)

/** 与 QuoteCreate 提示栏排版一致 */
const linkAlertGap2 = '  '
const linkAlertSep8Ideo = '\u3000'.repeat(8)
const linkAlertSep4Ideo = '\u3000'.repeat(4)

function formatDate(val?: string) {
  if (!val) return '—'
  const s = formatDisplayDateTime(val)
  return s === '--' ? '—' : s
}

/** 轮询分配的两名询价采购员展示 */
function formatAssignedPurchasers(row: RFQItem) {
  const n1 = row.assignedPurchaserName1?.trim()
  const n2 = row.assignedPurchaserName2?.trim()
  const parts = [n1, n2].filter((x): x is string => !!x)
  return parts.length ? parts.join('、') : '—'
}

function itemStatusText(s?: number) {
  const map: Record<number, string> = {
    0: '待报价',
    1: '已报价',
    2: '已接受',
    3: '已拒绝',
    4: '已关闭'
  }
  return s !== undefined ? map[s] ?? '—' : '—'
}

function quoteStatusText(status: number) {
  const map: Record<number, string> = {
    0: '草稿',
    1: '待审核',
    2: '已审核',
    3: '已发送',
    4: '已接受',
    5: '已拒绝',
    6: '已过期',
    7: '已关闭'
  }
  return map[status] ?? '—'
}

function quoteStatusType(status: number) {
  const map: Record<number, string> = {
    0: 'info',
    1: 'warning',
    2: 'primary',
    3: 'success',
    4: 'success',
    5: 'danger',
    6: 'info',
    7: 'info'
  }
  return map[status] || 'info'
}

function displayQuoteCode(row: Record<string, unknown>) {
  const v = row.quoteCode ?? row.quoteNumber ?? row.QuoteCode
  if (v != null && String(v).trim() !== '') return String(v)
  return '—'
}

function mapRow(row: any): RFQItem {
  return {
    ...row,
    rfqCreateTime: row.rfqCreateTime,
    materialModel: row.mpn ?? row.materialModel,
    customerMaterialModel: row.customerMpn ?? row.customerMaterialModel
  }
}

function aggregateQuoteCountByRfqItemId(quotes: unknown[]): Record<string, number> {
  const counts: Record<string, number> = {}
  for (const q of quotes) {
    const row = q as Record<string, unknown>
    const rid = row.rfqItemId ?? row.RfqItemId
    if (rid == null || String(rid).trim() === '') continue
    const k = String(rid)
    counts[k] = (counts[k] || 0) + 1
  }
  return counts
}

async function loadData() {
  loading.value = true
  try {
    const [res, quoteRes] = await Promise.all([
      rfqApi.searchRFQItems({
        pageNumber: pageInfo.page,
        pageSize: pageInfo.pageSize,
        startDate: dateRange.value?.[0],
        endDate: dateRange.value?.[1],
        customerKeyword: searchForm.customerKeyword || undefined,
        materialModel: searchForm.materialModel || undefined,
        salesUserKeyword: searchForm.salesUserKeyword || undefined
      }),
      quoteApi.getList({}).catch(() => ({ data: [] as unknown[] }))
    ])
    quoteRecordCountByRfqItemId.value = aggregateQuoteCountByRfqItemId(quoteRes.data || [])
    tableData.value = (res.items || []).map(mapRow)
    totalCount.value = res.totalCount ?? 0
    pageInfo.total = res.totalCount ?? 0

    const selId = selectedRfqItem.value?.id
    if (selId) {
      const found = tableData.value.find((r) => r.id === selId)
      if (found) {
        selectedRfqItem.value = found
        await loadQuotesForRfqItem(found)
        await refreshDockLinkAlert(found)
      } else {
        selectedRfqItem.value = null
        quotesForItem.value = []
        dockLinkAlert.value = null
      }
    }
  } catch (e: unknown) {
    tableData.value = []
    totalCount.value = 0
    pageInfo.total = 0
    quoteRecordCountByRfqItemId.value = {}
    selectedRfqItem.value = null
    quotesForItem.value = []
    dockLinkAlert.value = null
    const msg = e instanceof Error ? e.message : '加载需求明细失败'
    ElMessage.error(msg)
  } finally {
    loading.value = false
  }
}

function handleSearch() {
  pageInfo.page = 1
  loadData()
}

function handleReset() {
  dateRange.value = null
  searchForm.customerKeyword = ''
  searchForm.materialModel = ''
  searchForm.salesUserKeyword = ''
  pageInfo.page = 1
  loadData()
}

function onSelectionChange(rows: RFQItem[]) {
  selectedRows.value = rows
}

function dockQuoteRowKey(row: Record<string, unknown>) {
  return resolveQuoteRowId(row) ?? ''
}

async function loadQuotesForRfqItem(item: RFQItem | null) {
  if (!item?.id) {
    quotesForItem.value = []
    return
  }
  quotesLoading.value = true
  try {
    const res = await quoteApi.getList({ rfqItemId: item.id })
    quotesForItem.value = (res.data || []) as Record<string, unknown>[]
  } catch {
    quotesForItem.value = []
  } finally {
    quotesLoading.value = false
  }
}

async function refreshDockLinkAlert(row: RFQItem) {
  dockSummaryLoading.value = true
  dockLinkAlert.value = null
  try {
    const loaded = await fetchLinkedRfqItemRecord(row.rfqId || '', row.id)
    const raw = loaded?.item ?? (row as unknown as Record<string, unknown>)
    dockLinkAlert.value = buildLinkAlertFieldsFromItem(raw, {
      rfqCode: row.rfqCode,
      rfqId: row.rfqId,
      rfqHeader: loaded?.rfqHeader ?? undefined
    })
  } finally {
    dockSummaryLoading.value = false
  }
}

function onItemRowClick(row: RFQItem) {
  selectedRfqItem.value = row
  void loadQuotesForRfqItem(row)
  void refreshDockLinkAlert(row)
}

function goDetail(row: RFQItem) {
  if (!row.rfqId) return
  router.push({ name: 'RFQDetail', params: { id: row.rfqId } })
}

function goQuote(row: RFQItem) {
  if (!row.rfqId || !row.id) {
    ElMessage.warning('缺少需求或明细标识，无法打开新建报价')
    return
  }
  router.push({
    name: 'QuoteCreate',
    query: {
      rfqId: row.rfqId,
      rfqItemId: row.id,
      ...(row.rfqCode ? { rfqCode: row.rfqCode } : {}),
      returnTo: route.fullPath
    }
  })
}

function resolveQuoteRowId(row: Record<string, unknown>): string | undefined {
  const id = row.id ?? row.Id
  if (id != null && String(id).trim() !== '') return String(id)
  return undefined
}

async function handleDockRowGenerateSalesOrder(row: Record<string, unknown>) {
  const id = resolveQuoteRowId(row)
  if (!id) {
    ElMessage.warning('无法识别报价主键')
    return
  }
  dockRowSalesOrderQuoteId.value = id
  try {
    const check = await assertQuotesSameCustomer([id])
    if (!check.ok) {
      ElMessage.error(check.message)
      return
    }
    router.push({
      name: 'SalesOrderCreate',
      query: { quoteIds: id, returnTo: route.fullPath }
    })
  } finally {
    dockRowSalesOrderQuoteId.value = null
  }
}

function handleBatchQuote() {
  const rows = selectedRows.value
  if (!rows.length) {
    ElMessage.warning('请先勾选需求明细')
    return
  }
  const rfqIds = new Set(rows.map((r) => r.rfqId).filter(Boolean))
  if (rfqIds.size !== 1) {
    ElMessage.warning('批量报价仅支持同一需求下的多条明细，请重新选择')
    return
  }
  const rfqId = [...rfqIds][0]!
  const ids = rows.map((r) => r.id).filter(Boolean)
  const code = rows[0]?.rfqCode
  router.push({
    name: 'QuoteCreate',
    query: {
      rfqId,
      rfqItemIds: ids.join(','),
      ...(code ? { rfqCode: code } : {}),
      returnTo: route.fullPath
    }
  })
}

onMounted(() => {
  loadData()
})
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';
@import url('https://fonts.googleapis.com/css2?family=Space+Mono&family=Noto+Sans+SC:wght@300;400;500&display=swap');

.rfq-item-list-page {
  display: flex;
  flex-direction: column;
  min-height: calc(100vh - 120px);
  padding: 24px;
  padding-bottom: 12px;
  box-sizing: border-box;
  background: $layer-1;
  font-family: 'Noto Sans SC', sans-serif;
}

.rfq-item-main {
  flex: 1 1 auto;
  min-height: 0;
  overflow: auto;
  padding-bottom: 8px;
}

.supplier-quote-dock {
  flex: 0 0 auto;
  margin-top: auto;
  background: #0a1628;
  border: 1px solid rgba(0, 212, 255, 0.15);
  border-radius: 8px;
  overflow: hidden;
}

.dock-header {
  padding: 10px 14px;
  background: rgba(0, 212, 255, 0.06);
  border-bottom: 1px solid rgba(0, 212, 255, 0.12);
}

.dock-header-top {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.dock-header-actions {
  display: flex;
  align-items: center;
  gap: 8px;
}

.dock-title {
  font-size: 15px;
  font-weight: 600;
  color: #e8f4ff;
}

.dock-toggle {
  color: #00d4ff !important;
}

.dock-link-alert-wrap {
  margin-top: 10px;
  min-height: 28px;
}

.dock-link-alert-title-row {
  display: flex;
  flex-wrap: wrap;
  align-items: baseline;
  gap: 0;
  line-height: 1.55;
  font-size: 14px;
  font-weight: 400;
  color: #e8f4ff;
}

.dock-link-alert-title-row .la-pre {
  white-space: pre;
  font-size: inherit;
}

.dock-link-alert-title-row .la-muted {
  color: rgba(200, 216, 232, 0.55);
  font-weight: 400;
}

.dock-link-alert-title-row .la-strong {
  color: #e8f4ff;
  font-weight: 600;
}

.dock-link-alert-title-row .la-value-green {
  color: #5fd89a;
  font-weight: 600;
}

.supplier-quote-dock.collapsed .dock-header {
  border-bottom: none;
}

.supplier-quote-dock.collapsed .dock-link-alert-wrap {
  display: none;
}

.dock-body {
  padding: 12px 14px 14px;
  min-height: 0;
}

.dock-placeholder {
  padding: 24px 12px;
  text-align: center;
  font-size: 13px;
  color: rgba(200, 216, 232, 0.55);
}

.dock-table-wrap {
  min-height: 120px;
}

.dock-quote-table {
  width: 100%;
  --el-table-header-bg-color: rgba(0, 212, 255, 0.1);
  --el-table-tr-bg-color: transparent;
  --el-table-border-color: rgba(0, 212, 255, 0.12);
  :deep(.el-table__inner-wrapper::before) {
    display: none;
  }
  :deep(.el-table__body-wrapper) {
    background: transparent;
  }
  :deep(th.el-table__cell) {
    color: rgba(200, 216, 232, 0.85);
  }
  :deep(td.el-table__cell) {
    color: #e8f4ff;
  }
}

.page-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 20px;

  .header-left {
    display: flex;
    align-items: center;
  }

  .page-title {
    margin: 0;
    color: $text-primary;
    font-size: 20px;
  }

  .count-badge {
    margin-left: 10px;
    padding: 2px 10px;
    background: rgba(255, 255, 255, 0.05);
    border: 1px solid $border-panel;
    border-radius: 20px;
    font-size: 12px;
    color: $text-muted;
  }
}

.page-title-group {
  display: flex; align-items: center; gap: 10px;
  .page-icon {
    width: 36px; height: 36px; border-radius: 10px; display: flex; align-items: center; justify-content: center;
    background: rgba(0, 212, 255, 0.1); border: 1px solid rgba(0, 212, 255, 0.25); color: $cyan-primary; font-weight: 700;
  }
}

.filter-card {
  margin-bottom: 20px;
  background: $layer-2;
  border: 1px solid $border-panel;
}

.table-card {
  background: $layer-2;
  border: 1px solid $border-panel;
  :deep(.el-table) {
    background: transparent;
    --el-table-header-bg-color: rgba(255, 255, 255, 0.03);
    --el-table-tr-bg-color: transparent;
    --el-table-border-color: $border-panel;
    color: $text-primary;
  }
}

.pagination-wrapper {
  margin-top: 16px;
  display: flex;
  justify-content: flex-end;
}

.btn-primary { border-radius: $border-radius-md; }
.action-btn { color: $cyan-primary !important; }
.quantum-pagination { :deep(.el-pagination__total) { color: $text-muted; } }
</style>
