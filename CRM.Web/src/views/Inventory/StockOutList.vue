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
        <div class="count-badge">{{ t('stockOutList.count', { count: filteredList.length }) }}</div>
      </div>
      <div class="header-right">
        <el-input
          v-model="keyword"
          :placeholder="t('stockOutList.filters.keywordPlaceholder')"
          clearable
          style="width: 220px"
          @keyup.enter="handleSearch"
        />
        <button type="button" class="btn-secondary" @click="handleSearch">{{ t('stockOutList.filters.search') }}</button>
        <button type="button" class="btn-secondary" @click="refreshStockOutList">{{ t('stockOutList.filters.refresh') }}</button>
      </div>
    </div>

    <!-- 结构与 StockOutNotifyList / StockInList 一致：无 row-key、无额外包裹 -->
    <CrmDataTable
      ref="dataTableRef"
      column-layout-key="stock-out-list-main"
      :columns="stockOutTableColumns"
      :show-column-settings="false"
      :density-toggle-anchor-el="rowDensityToggleAnchorEl"
      :data="pagedFilteredList"
      row-key="id"
      v-loading="loading"
      @row-dblclick="onRowDblclick"
    >
      <template #col-status="{ row }">
        <span :class="['status-badge', `status-${row.status}`]">{{ statusLabel(row.status) }}</span>
      </template>
      <template #col-stockOutDate="{ row }">
        <span class="text-secondary">{{ formatDate(row.stockOutDate) }}</span>
      </template>
      <template #col-totalQuantity="{ row }">{{ formatNum(row.totalQuantity) }}</template>
      <template #col-createTime="{ row }">{{ formatDate((row as any).createTime || (row as any).createdAt) }}</template>
      <template #col-createUser="{ row }">{{ row.createUserName || (row as any).createdBy || t('quoteList.na') }}</template>
      <template #col-customerName="{ row }">{{ maskSaleSensitiveFields ? '—' : (row.customerName || t('quoteList.na')) }}</template>
      <template #col-salesUserName="{ row }">{{ maskSaleSensitiveFields ? '—' : (row.salesUserName || t('quoteList.na')) }}</template>
      <template #col-sellOrderItemCode="{ row }">{{ row.sellOrderItemCode || t('quoteList.na') }}</template>
      <template #col-shipmentMethod="{ row }">{{ row.shipmentMethod || t('quoteList.na') }}</template>
      <template #col-courierTrackingNo="{ row }">{{ row.courierTrackingNo || t('quoteList.na') }}</template>
      <template #col-actions-header>
        <div class="op-col-header">
          <span class="op-col-header-text">{{ t('stockOutList.columns.actions') }}</span>
          <button type="button" class="op-col-toggle-btn" @click.stop="toggleOpCol">
            {{ opColExpanded ? '>' : '<' }}
          </button>
        </div>
      </template>
      <template #col-actions="{ row }">
        <div @click.stop @dblclick.stop>
          <div v-if="opColExpanded" class="action-btns action-btns--stockout-wrap">
            <button type="button" class="action-btn" @click.stop="goDetail(row)">{{ t('stockOutList.actions.detail') }}</button>
            <button type="button" class="action-btn" @click.stop="goInvoiceReport(row)">
              {{ t('stockOutList.actions.printInvoice') }}
            </button>
            <el-dropdown trigger="click" @click.stop @command="(cmd: string) => onPackingMenuCommand(row, cmd)">
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
              v-if="row.status !== 4"
              type="button"
              class="action-btn action-btn--warning"
              @click.stop="handleMarkFinish(row)"
            >
              {{ t('stockOutList.actions.markFinished') }}
            </button>
            <button type="button" class="action-btn action-btn--danger" @click.stop="handleDeleteRow(row)">删除</button>
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
                <el-dropdown-item @click.stop="goInvoiceReport(row)">
                  <span class="op-more-item">{{ t('stockOutList.actions.printInvoice') }}</span>
                </el-dropdown-item>
                <el-dropdown-item disabled>
                  <span class="op-submenu-title">{{ t('stockOutList.actions.printPacking') }}</span>
                </el-dropdown-item>
                <el-dropdown-item divided @click.stop="goPackingReport(row, true)">
                  <span class="op-more-item op-more-item--sub">{{ t('stockOutList.actions.packingWithInspection') }}</span>
                </el-dropdown-item>
                <el-dropdown-item @click.stop="goPackingReport(row, false)">
                  <span class="op-more-item op-more-item--sub">{{ t('stockOutList.actions.packingWithoutInspection') }}</span>
                </el-dropdown-item>
                <el-dropdown-item v-if="row.status !== 4" @click.stop="handleMarkFinish(row)">
                  <span class="op-more-item op-more-item--warning">{{ t('stockOutList.actions.markFinished') }}</span>
                </el-dropdown-item>
                <el-dropdown-item divided @click.stop="handleDeleteRow(row)">
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
        :total="filteredListTotal"
        :page-sizes="[10, 20, 50, 100]"
        layout="total, sizes, prev, pager, next, jumper"
        @size-change="listPage = 1"
      />
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import { Setting, ArrowDown } from '@element-plus/icons-vue'
import { stockOutApi, type StockOutDto } from '@/api/stockOut'
import { formatDisplayDateTime } from '@/utils/displayDateTime'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'
import { useSaleSensitiveFieldMask } from '@/composables/useSaleSensitiveFieldMask'
import { useAuthStore } from '@/stores/auth'

const { maskSaleSensitiveFields } = useSaleSensitiveFieldMask()

const route = useRoute()
const router = useRouter()
const { t } = useI18n()
const authStore = useAuthStore()
const isSysAdmin = computed(() => authStore.user?.isSysAdmin === true)
const loading = ref(false)
const list = ref<StockOutDto[]>([])
const listPage = ref(1)
const listPageSize = ref(20)
const keyword = ref('')
const dataTableRef = ref<{ openColumnSettings?: () => void } | null>(null)
const rowDensityToggleAnchorEl = ref<HTMLElement | null>(null)

const opColExpanded = ref(false)
const OP_COL_COLLAPSED_WIDTH = 96
/** 三枚操作按钮；全局 .op-col 为 overflow:hidden + nowrap，列窄时会把中间的「打印 Invoice」裁掉，故加宽并允许换行 */
const OP_COL_EXPANDED_WIDTH = 400
const OP_COL_EXPANDED_MIN_WIDTH = 360
const opColWidth = computed(() => (opColExpanded.value ? OP_COL_EXPANDED_WIDTH : OP_COL_COLLAPSED_WIDTH))
const opColMinWidth = computed(() =>
  opColExpanded.value ? OP_COL_EXPANDED_MIN_WIDTH : OP_COL_COLLAPSED_WIDTH
)
function toggleOpCol() {
  opColExpanded.value = !opColExpanded.value
}

const stockOutTableColumns = computed<CrmTableColumnDef[]>(() => [
  { key: 'status', label: t('stockOutList.columns.status'), prop: 'status', width: 110, align: 'center' },
  { key: 'customerName', label: t('stockOutList.columns.customerName'), prop: 'customerName', width: 140, minWidth: 120, showOverflowTooltip: true },
  { key: 'salesUserName', label: t('stockOutList.columns.salesUserName'), prop: 'salesUserName', width: 110, minWidth: 100, showOverflowTooltip: true },
  { key: 'stockOutDate', label: t('stockOutList.columns.stockOutDate'), prop: 'stockOutDate', width: 170 },
  { key: 'shipmentMethod', label: t('stockOutList.columns.shipmentMethod'), prop: 'shipmentMethod', width: 120, minWidth: 100, showOverflowTooltip: true },
  { key: 'courierTrackingNo', label: t('stockOutList.columns.courierTrackingNo'), prop: 'courierTrackingNo', width: 140, minWidth: 120, showOverflowTooltip: true },
  { key: 'totalQuantity', label: t('stockOutList.columns.totalQuantity'), prop: 'totalQuantity', width: 110, align: 'right' },
  { key: 'remark', label: t('stockOutList.columns.remark'), prop: 'remark', minWidth: 160, showOverflowTooltip: true },
  { key: 'stockOutCode', label: t('stockOutList.columns.stockOutCode'), prop: 'stockOutCode', width: 160, minWidth: 160, showOverflowTooltip: true },
  { key: 'sourceCode', label: t('stockOutList.columns.sourceCode'), prop: 'sourceCode', width: 160, minWidth: 160, showOverflowTooltip: true },
  { key: 'sellOrderItemCode', label: t('stockOutList.columns.sellOrderItemCode'), prop: 'sellOrderItemCode', width: 160, minWidth: 140, showOverflowTooltip: true },
  { key: 'createTime', label: t('stockOutList.columns.createTime'), width: 170 },
  { key: 'createUser', label: t('stockOutList.columns.createUser'), width: 120, showOverflowTooltip: true },
  {
    key: 'actions',
    label: t('stockOutList.columns.actions'),
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

function syncKeywordFromRoute() {
  if (route.name !== 'StockOutList') return
  const q = route.query
  keyword.value = typeof q.keyword === 'string' ? q.keyword : ''
}

watch(
  () => route.query,
  () => syncKeywordFromRoute(),
  { deep: true, immediate: true }
)

watch(keyword, () => {
  listPage.value = 1
})

const formatNum = (v: number) => (v == null ? t('quoteList.na') : Number(v).toLocaleString())
const formatDate = (v?: string) => formatDisplayDateTime(v)

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

const filteredList = computed(() => {
  if (!keyword.value) return list.value
  const k = keyword.value.toLowerCase()
  return list.value.filter(
    (x) =>
      (x.stockOutCode || '').toLowerCase().includes(k) ||
      (x.sourceCode && x.sourceCode.toLowerCase().includes(k)) ||
      (x.sellOrderItemCode && String(x.sellOrderItemCode).toLowerCase().includes(k)) ||
      (x.shipmentMethod && String(x.shipmentMethod).toLowerCase().includes(k)) ||
      (x.courierTrackingNo && String(x.courierTrackingNo).toLowerCase().includes(k))
  )
})

const filteredListTotal = computed(() => filteredList.value.length)
const pagedFilteredList = computed(() => {
  const rows = filteredList.value
  const start = (listPage.value - 1) * listPageSize.value
  return rows.slice(start, start + listPageSize.value)
})

watch(filteredListTotal, () => {
  const maxP = Math.max(1, Math.ceil(filteredListTotal.value / listPageSize.value) || 1)
  if (listPage.value > maxP) listPage.value = maxP
})

async function runStockOutListFetch(resetPage: boolean) {
  if (resetPage) listPage.value = 1
  loading.value = true
  try {
    list.value = await stockOutApi.getAll()
  } catch (e) {
    console.error(e)
    ElMessage.error(t('stockOutList.messages.loadFailed'))
  } finally {
    loading.value = false
  }
}

const fetchList = () => void runStockOutListFetch(true)
const refreshStockOutList = () => void runStockOutListFetch(false)

const handleSearch = () => {
  const k = keyword.value.trim()
  router.replace({ name: 'StockOutList', query: k ? { keyword: k } : {} })
}

function goDetail(row: StockOutDto) {
  if (!row?.id) return
  router.push({ name: 'StockOutDetail', params: { id: row.id } })
}

function goInvoiceReport(row: StockOutDto) {
  if (!row?.id) return
  router.push({ name: 'StockOutInvoiceReport', params: { id: row.id } })
}

function goPackingReport(row: StockOutDto, withInspection: boolean) {
  if (!row?.id) return
  router.push({
    name: 'StockOutPackingReport',
    params: {
      id: row.id,
      packingInspection: withInspection ? 'with-inspection' : 'without-inspection'
    }
  })
}

function onPackingMenuCommand(row: StockOutDto, cmd: string) {
  if (cmd === 'with') goPackingReport(row, true)
  else if (cmd === 'without') goPackingReport(row, false)
}

function onRowDblclick(row: StockOutDto) {
  goDetail(row)
}

const handleMarkFinish = async (row: StockOutDto) => {
  try {
    await stockOutApi.updateStatus(row.id, 4)
    ElMessage.success(t('stockOutList.messages.markFinishedSuccess'))
    await runStockOutListFetch(false)
  } catch (e) {
    console.error(e)
    ElMessage.error(t('stockOutList.messages.updateStatusFailed'))
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
    ElMessage.error('删除失败')
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
    ElMessage.error('强制删除失败')
  }
}

onMounted(() => {
  void fetchList()
})
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
  margin-bottom: 20px;
}
.header-left,
.header-right {
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

/* 出库列表操作列：避免「打印 Invoice」被 .op-col overflow:hidden 裁切 */
.stockout-list-page :deep(.crm-data-table td.op-col .cell),
.stockout-list-page :deep(.crm-data-table th.op-col .cell) {
  overflow: visible;
}

.stockout-list-page :deep(.action-btns--stockout-wrap) {
  flex-wrap: wrap;
  white-space: normal;
  row-gap: 4px;
  column-gap: 6px;
  justify-content: flex-end;
}

.stockout-list-page :deep(.action-btns--stockout-wrap .action-btn) {
  white-space: nowrap;
}
</style>
