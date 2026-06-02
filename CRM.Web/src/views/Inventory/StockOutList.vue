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
      :data="list"
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
      <template #col-expectedStockOutDate="{ row }">
        <span class="text-secondary">{{ formatDate(row.expectedStockOutDate) }}</span>
      </template>
      <template #col-packingCount="{ row }">{{ formatPackingCount(row.packingCount) }}</template>
      <template #col-createTime="{ row }">{{ formatDate((row as any).createTime || (row as any).createdAt) }}</template>
      <template #col-createUser="{ row }">{{ row.createUserName || (row as any).createdBy || t('quoteList.na') }}</template>
      <template #col-customerName="{ row }">{{ maskSaleSensitiveFields ? '—' : (row.customerName || t('quoteList.na')) }}</template>
      <template #col-salesUserName="{ row }">{{ maskSaleSensitiveFields ? '—' : (row.salesUserName || t('quoteList.na')) }}</template>
      <template #col-shipmentMethod="{ row }">{{ shipmentMethodDisplay(row.shipmentMethod) }}</template>
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
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import { Setting } from '@element-plus/icons-vue'
import { stockOutApi, type StockOutDto, type StockOutMarkFinishContext } from '@/api/stockOut'
import { formatDisplayDateTime } from '@/utils/displayDateTime'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'
import { useSaleSensitiveFieldMask } from '@/composables/useSaleSensitiveFieldMask'
import { useDepartmentDataReadOnly } from '@/composables/useDepartmentDataReadOnly'
import { useLogisticsFormDict } from '@/composables/useLogisticsFormDict'
import { useAuthStore } from '@/stores/auth'

const { maskSaleSensitiveFields } = useSaleSensitiveFieldMask()

const route = useRoute()
const router = useRouter()
const { t } = useI18n()
const authStore = useAuthStore()
const { canWriteLogisticsData } = useDepartmentDataReadOnly()
const { ensureLoaded: ensureLogisticsDict, arrivalOptions } = useLogisticsFormDict()
const isSysAdmin = computed(() => authStore.user?.isSysAdmin === true)
const loading = ref(false)
const list = ref<StockOutDto[]>([])
const listTotal = ref(0)
const listPage = ref(1)
const listPageSize = ref(20)
const keyword = ref('')
const dataTableRef = ref<{ openColumnSettings?: () => void } | null>(null)
const rowDensityToggleAnchorEl = ref<HTMLElement | null>(null)

const opColExpanded = ref(false)
const OP_COL_COLLAPSED_WIDTH = 43
const OP_COL_EXPANDED_WIDTH = 120
const OP_COL_EXPANDED_MIN_WIDTH = 110
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

const stockOutTableColumns = computed<CrmTableColumnDef[]>(() => [
  { key: 'stockOutCode', label: t('stockOutList.columns.stockOutCode'), prop: 'stockOutCode', width: 160, minWidth: 160, showOverflowTooltip: true },
  { key: 'status', label: t('stockOutList.columns.status'), prop: 'status', width: 110, align: 'center' },
  { key: 'expectedStockOutDate', label: t('stockOutList.columns.expectedStockOutDate'), prop: 'expectedStockOutDate', width: 130 },
  { key: 'stockOutDate', label: t('stockOutList.columns.stockOutDate'), prop: 'stockOutDate', width: 170 },
  { key: 'shipmentMethod', label: t('stockOutList.columns.shipmentMethod'), prop: 'shipmentMethod', width: 120, minWidth: 100, showOverflowTooltip: true },
  { key: 'courierTrackingNo', label: t('stockOutList.columns.courierTrackingNo'), prop: 'courierTrackingNo', width: 140, minWidth: 120, showOverflowTooltip: true },
  { key: 'customerName', label: t('stockOutList.columns.customerName'), prop: 'customerName', width: 140, minWidth: 120, showOverflowTooltip: true },
  { key: 'salesUserName', label: t('stockOutList.columns.salesUserName'), prop: 'salesUserName', width: 110, minWidth: 100, showOverflowTooltip: true },
  { key: 'packingCount', label: t('stockOutList.columns.packingCount'), prop: 'packingCount', width: 100, align: 'right' },
  { key: 'remark', label: t('stockOutList.columns.remark'), prop: 'remark', minWidth: 160, showOverflowTooltip: true },
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
    labelClassName: 'op-col',
  resizable: false
  }
])

function syncKeywordFromRoute() {
  if (route.name !== 'StockOutList') return
  const q = route.query
  keyword.value = typeof q.keyword === 'string' ? q.keyword : ''
}

watch(
  () => route.query,
  () => {
    syncKeywordFromRoute()
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
  for (const o of arrivalOptions.value) {
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
    const kw = keyword.value.trim()
    const res = await stockOutApi.getListPaged({
      keyword: kw || undefined,
      page: listPage.value,
      pageSize: listPageSize.value
    })
    list.value = res.items
    listTotal.value = res.total
  } catch (e) {
    console.error(e)
    ElMessage.error(t('stockOutList.messages.loadFailed'))
  } finally {
    loading.value = false
  }
}

const refreshStockOutList = () => void runStockOutListFetch(false)

function onStockOutListPageSizeChange() {
  listPage.value = 1
  void runStockOutListFetch(false)
}

const handleSearch = () => {
  const k = keyword.value.trim()
  router.replace({ name: 'StockOutList', query: k ? { keyword: k } : {} })
}

function goDetail(row: StockOutDto) {
  if (!row?.id) return
  router.push({ name: 'StockOutDetail', params: { id: row.id } })
}

function onRowDblclick(row: StockOutDto) {
  goDetail(row)
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

</style>
