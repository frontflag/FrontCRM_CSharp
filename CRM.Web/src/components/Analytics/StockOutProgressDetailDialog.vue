<script setup lang="ts">
import { computed, nextTick, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import {
  salesAnalyticsApi,
  type SalesAnalyticsQuery,
  type SalesAnalyticsStockOutProgressDetail,
  type SalesAnalyticsStockOutProgressSummaryItem
} from '@/api/analytics/sales'
import { getApiErrorMessage } from '@/utils/apiError'
import { useAuthStore } from '@/stores/auth'
import { useResizableDialog } from '@/composables/useResizableDialog'

const open = defineModel<boolean>({ default: false })

const props = defineProps<{
  query: SalesAnalyticsQuery
}>()

const { t } = useI18n()
const router = useRouter()
const authStore = useAuthStore()
const canOpenItemList = computed(() => authStore.hasPermission('sales-order.read'))

type ElDialogExpose = {
  dialogContentRef?: {
    $el: HTMLElement
  }
}

const dialogRef = ref<ElDialogExpose | null>(null)
const { enableResizableDialogWithRetry, disableResizableDialog, fitDialogToContentWithRetry } =
  useResizableDialog({
    resolveDialogEl: () => dialogRef.value?.dialogContentRef?.$el ?? null,
    dialogClass: 'stock-out-progress-detail-dialog',
    minWidth: 960,
    minHeight: 420,
    // 自研标题栏拖拽（left/top），勿再用 el-dialog 的 draggable/transform
    draggable: true
  })

async function onDialogOpened() {
  await nextTick()
  enableResizableDialogWithRetry()
  // 建立固定高度 + 钉住像素位置，表格内滚且缩放后再拖不错位
  fitDialogToContentWithRetry()
}

function onDialogClosed() {
  disableResizableDialog()
}

function itemListHref(code: string): string {
  const c = String(code ?? '').trim()
  if (!c) return '#'
  return router.resolve({
    name: 'SalesOrderItemList',
    query: { sellOrderItemCode: c }
  }).href
}

const loading = ref(false)
const summary = ref<SalesAnalyticsStockOutProgressSummaryItem[]>([])
const items = ref<SalesAnalyticsStockOutProgressDetail['items']>([])
const canViewCustomer = ref(false)
const total = ref(0)
const page = ref(1)
const pageSize = ref(20)
/** 'all' = 全部；'0'|'1'|'2' = 单档 */
const activeTab = ref('all')

function formatQty(v: number | null | undefined) {
  if (v == null || Number.isNaN(Number(v))) return '—'
  const n = Number(v)
  return Number.isInteger(n) ? String(n) : String(n)
}

const viewLevelLabel = computed(() => {
  const level = props.query.viewLevel
  if (level === 'company') return t('salesAnalytics.tabs.company')
  if (level === 'department') return t('salesAnalytics.tabs.department')
  if (level === 'personal') return t('salesAnalytics.tabs.personal')
  return '—'
})

const dateRangeText = computed(() => {
  const from = String(props.query.dateFrom ?? '').trim()
  const to = String(props.query.dateTo ?? '').trim()
  const left = from || t('salesAnalytics.stockOutProgressDetail.scopeDateOpenStart')
  const right = to || t('salesAnalytics.stockOutProgressDetail.scopeDateOpenEnd')
  return `${left} ～ ${right}`
})

function statusFilter(): number | undefined {
  if (activeTab.value === 'all') return undefined
  const n = Number(activeTab.value)
  return Number.isFinite(n) ? n : undefined
}

async function load() {
  if (!open.value) return
  loading.value = true
  try {
    const data = await salesAnalyticsApi.getStockOutProgressDetail({
      ...props.query,
      stockOutProgressStatus: statusFilter(),
      page: page.value,
      pageSize: pageSize.value
    })
    summary.value = data.summary
    items.value = data.items
    canViewCustomer.value = !!data.canViewCustomer
    total.value = data.total
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('salesAnalytics.stockOutProgressDetail.loadFailed')))
  } finally {
    loading.value = false
  }
}

function onTabChange() {
  page.value = 1
  void load()
}

function onPageChange(p: number) {
  page.value = p
  void load()
}

function onSizeChange(s: number) {
  pageSize.value = s
  page.value = 1
  void load()
}

function formatAt(iso: string) {
  if (!iso) return '—'
  const d = new Date(iso)
  if (Number.isNaN(d.getTime())) return iso
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())} ${pad(d.getHours())}:${pad(d.getMinutes())}`
}

watch(open, (isOpen) => {
  if (isOpen) {
    activeTab.value = 'all'
    page.value = 1
    void load()
  }
})
</script>

<template>
  <el-dialog
    ref="dialogRef"
    v-model="open"
    :title="t('salesAnalytics.stockOutProgressDetail.title')"
    width="1100px"
    class="stock-out-progress-detail-dialog"
    destroy-on-close
    append-to-body
    :close-on-click-modal="false"
    align-center
    @opened="onDialogOpened"
    @closed="onDialogClosed"
  >
    <div v-loading="loading" class="sop-detail">
      <div class="sop-detail__top">
        <div class="scope-banner" role="status">
          <div class="scope-banner__main">
            <span>
              {{ t('salesAnalytics.stockOutProgressDetail.scopeViewLevel') }}：{{ viewLevelLabel }}
            </span>
            <span class="scope-banner__sep" aria-hidden="true">|</span>
            <span>
              {{ t('salesAnalytics.stockOutProgressDetail.scopeDateRange') }}：{{ dateRangeText }}
            </span>
          </div>
          <div class="scope-banner__hint">
            {{ t('salesAnalytics.stockOutProgressDetail.scopeDateHint') }}
          </div>
        </div>

        <div class="summary">
          <div v-for="row in summary" :key="row.status" class="summary-item">
            <div class="summary-label">{{ row.label }}</div>
            <div class="summary-count">
              {{ t('salesAnalytics.stockOutProgressDetail.count', { count: row.count }) }}
            </div>
            <div class="summary-ratio">{{ row.ratio }}%</div>
            <div class="bar-track">
              <div class="bar-fill" :style="{ width: `${row.ratio}%` }" />
            </div>
          </div>
        </div>

        <el-tabs v-model="activeTab" class="tabs" @tab-change="onTabChange">
          <el-tab-pane :label="t('salesAnalytics.stockOutProgressDetail.tabs.all')" name="all" />
          <el-tab-pane :label="t('salesAnalytics.stockOutProgressDetail.tabs.pending')" name="0" />
          <el-tab-pane :label="t('salesAnalytics.stockOutProgressDetail.tabs.partial')" name="1" />
          <el-tab-pane :label="t('salesAnalytics.stockOutProgressDetail.tabs.complete')" name="2" />
        </el-tabs>
      </div>

      <div class="sop-detail__table">
        <el-table :data="items" size="small" stripe border empty-text="—" height="100%">
          <el-table-column
            :label="t('salesAnalytics.stockOutProgressDetail.columns.itemCode')"
            min-width="140"
            show-overflow-tooltip
          >
            <template #default="{ row }">
              <a
                v-if="canOpenItemList && row.sellOrderItemCode"
                class="item-code-link"
                :href="itemListHref(row.sellOrderItemCode)"
                target="_blank"
                rel="noopener noreferrer"
                @click.stop
              >
                {{ row.sellOrderItemCode }}
              </a>
              <span v-else>{{ row.sellOrderItemCode || '—' }}</span>
            </template>
          </el-table-column>
          <el-table-column
            :label="t('salesAnalytics.stockOutProgressDetail.columns.orderCreateTime')"
            width="150"
          >
            <template #default="{ row }">{{ formatAt(row.orderCreateTime) }}</template>
          </el-table-column>
          <el-table-column
            v-if="canViewCustomer"
            :label="t('salesAnalytics.stockOutProgressDetail.columns.customer')"
            min-width="120"
            show-overflow-tooltip
          >
            <template #default="{ row }">{{ row.customerName || '—' }}</template>
          </el-table-column>
          <el-table-column
            :label="t('salesAnalytics.stockOutProgressDetail.columns.salesUser')"
            min-width="90"
            show-overflow-tooltip
          >
            <template #default="{ row }">{{ row.salesUserName || '—' }}</template>
          </el-table-column>
          <el-table-column
            :label="t('salesAnalytics.stockOutProgressDetail.columns.pn')"
            min-width="120"
            show-overflow-tooltip
          >
            <template #default="{ row }">{{ row.pn || '—' }}</template>
          </el-table-column>
          <el-table-column
            :label="t('salesAnalytics.stockOutProgressDetail.columns.brand')"
            min-width="90"
            show-overflow-tooltip
          >
            <template #default="{ row }">{{ row.brand || '—' }}</template>
          </el-table-column>
          <el-table-column
            :label="t('salesAnalytics.stockOutProgressDetail.columns.qty')"
            width="100"
            align="right"
          >
            <template #default="{ row }">{{ formatQty(row.qty) }}</template>
          </el-table-column>
          <el-table-column
            prop="stockOutProgressLabel"
            :label="t('salesAnalytics.stockOutProgressDetail.columns.status')"
            width="130"
          />
        </el-table>
      </div>

      <div class="sop-detail__pager">
        <el-pagination
          background
          layout="total, sizes, prev, pager, next"
          :total="total"
          :current-page="page"
          :page-size="pageSize"
          :page-sizes="[20, 50, 100]"
          @current-change="onPageChange"
          @size-change="onSizeChange"
        />
      </div>
    </div>
  </el-dialog>
</template>

<style lang="scss">
.stock-out-progress-detail-dialog.el-dialog {
  max-width: calc(100vw - 48px);
  box-sizing: border-box;
}

.stock-out-progress-detail-dialog.crm-dialog-draggable {
  .el-dialog__header {
    cursor: move;
    user-select: none;
  }

  .el-dialog__headerbtn {
    cursor: pointer;
  }
}

.stock-out-progress-detail-dialog {
  .el-dialog__header {
    flex: 0 0 auto;
  }

  .el-dialog__body {
    padding: 12px 16px 16px;
    box-sizing: border-box;
    /* 缩放后必须占满剩余高度并裁剪，避免内容画出白底 */
    flex: 1 1 auto !important;
    min-height: 0 !important;
    overflow: hidden !important;
    display: flex !important;
    flex-direction: column !important;
  }
}

.stock-out-progress-detail-dialog.crm-dialog-resizable {
  position: relative !important;
  display: flex !important;
  flex-direction: column !important;
  overflow: hidden !important;
  max-height: none !important;
  box-sizing: border-box !important;

  .el-dialog__header {
    flex: 0 0 auto;
  }

  .el-dialog__body {
    flex: 1 1 auto !important;
    min-height: 0 !important;
    overflow: hidden !important;
  }

  .el-dialog__footer {
    flex: 0 0 auto;
  }
}

.crm-dialog-resize-handle {
  position: absolute;
  z-index: 10;
  background: transparent;

  &:hover {
    background: rgba(64, 158, 255, 0.15);
  }

  &--e {
    top: 0;
    right: 0;
    width: 12px;
    height: 100%;
    cursor: ew-resize;
  }

  &--s {
    left: 0;
    bottom: 0;
    width: 100%;
    height: 12px;
    cursor: ns-resize;
  }

  &--se {
    right: 0;
    bottom: 0;
    width: 20px;
    height: 20px;
    cursor: nwse-resize;
  }
}
</style>

<style scoped lang="scss">
.sop-detail {
  flex: 1 1 auto;
  min-height: 0;
  height: 100%;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.sop-detail__top {
  flex: 0 0 auto;
}

.scope-banner {
  margin-bottom: 10px;
  padding: 8px 12px;
  border-radius: 6px;
  background: var(--el-color-info-light-9);
  border: 1px solid var(--el-color-info-light-7);
  font-size: 13px;
  line-height: 1.45;
  color: var(--el-text-color-primary);
}

.scope-banner__main {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 8px;
  font-weight: 500;
}

.scope-banner__sep {
  color: var(--el-text-color-secondary);
}

.scope-banner__hint {
  margin-top: 2px;
  font-size: 12px;
  font-weight: 400;
  color: var(--el-text-color-secondary);
}

.summary {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 12px;
  margin-bottom: 8px;
}

.summary-item {
  padding: 10px 12px;
  background: var(--el-fill-color-lighter);
  border-radius: 6px;
}

.summary-label {
  font-size: 13px;
  font-weight: 600;
  margin-bottom: 4px;
}

.summary-count {
  font-size: 12px;
  color: var(--el-text-color-regular);
}

.summary-ratio {
  font-size: 18px;
  font-weight: 600;
  margin: 4px 0;
  color: var(--el-color-success);
}

.bar-track {
  height: 6px;
  background: var(--el-fill-color);
  border-radius: 3px;
}

.bar-fill {
  height: 100%;
  background: var(--el-color-success);
  border-radius: 3px;
}

.tabs {
  margin-bottom: 8px;
}

.sop-detail__table {
  flex: 1 1 auto;
  min-height: 120px;
  overflow: hidden;
}

.sop-detail__pager {
  display: flex;
  justify-content: flex-end;
  margin-top: 12px;
  flex: 0 0 auto;
}

.item-code-link {
  color: var(--el-color-primary);
  text-decoration: none;
}

.item-code-link:hover {
  text-decoration: underline;
}
</style>
