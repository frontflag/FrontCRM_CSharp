<template>
  <div class="customs-page">
    <div class="page-header">
      <h1 class="page-title">{{ t('customsPages.pendlists.title') }}</h1>
    </div>

    <div class="filter-bar">
      <el-select v-model="filters.status" clearable :placeholder="t('customsPages.pendlists.filterStatus')" style="width: 160px">
        <el-option :label="t('customsPages.pendlists.filterStatusAll')" :value="undefined" />
        <el-option :label="t('customsPages.pendlists.statusOpen')" :value="CUSTOMS_PENDLIST_STATUS.Open" />
        <el-option
          :label="t('customsPages.pendlists.statusCustomsOutCreated')"
          :value="CUSTOMS_PENDLIST_STATUS.CustomsOutNotifyCreated"
        />
        <el-option :label="t('customsPages.pendlists.statusInProcess')" :value="CUSTOMS_PENDLIST_STATUS.InCustomsProcess" />
        <el-option :label="t('customsPages.pendlists.statusClosed')" :value="CUSTOMS_PENDLIST_STATUS.Closed" />
        <el-option :label="t('customsPages.pendlists.statusCancelled')" :value="CUSTOMS_PENDLIST_STATUS.Cancelled" />
      </el-select>
      <el-input
        v-model="filters.keyword"
        clearable
        :placeholder="t('customsPages.pendlists.filterKeywordPlaceholder')"
        style="width: 280px"
        @keyup.enter="loadList"
      />
      <el-button type="primary" @click="loadList">{{ t('customsPages.pendlists.search') }}</el-button>
      <el-button @click="resetFilters">{{ t('customsPages.pendlists.reset') }}</el-button>
    </div>

    <el-table
      :data="list"
      v-loading="loading"
      stripe
      border
      class="data-table crm-items-table"
      highlight-current-row
      :row-class-name="pendlistRowClassName"
      @row-click="onPendlistRowClick"
    >
      <el-table-column prop="salesStockOutNotifyCode" :label="t('customsPages.pendlists.colSalesSor')" min-width="140" />
      <el-table-column prop="salesOrderCode" :label="t('customsPages.pendlists.colSalesOrder')" min-width="120">
        <template #default="{ row }">
          <router-link
            v-if="row.salesOrderId && row.salesOrderCode"
            class="link-text"
            :to="`/sales-orders/${row.salesOrderId}`"
            @click.stop
          >
            {{ row.salesOrderCode }}
          </router-link>
          <span v-else>{{ row.salesOrderCode || '—' }}</span>
        </template>
      </el-table-column>
      <el-table-column prop="sellOrderItemCode" :label="t('customsPages.pendlists.colSoLine')" min-width="120">
        <template #default="{ row }">
          <router-link
            v-if="row.salesOrderId && row.sellOrderItemId && row.sellOrderItemCode"
            class="link-text"
            :to="{
              path: `/sales-orders/${row.salesOrderId}`,
              query: { sellOrderItemId: row.sellOrderItemId }
            }"
            @click.stop
          >
            {{ row.sellOrderItemCode }}
          </router-link>
          <span v-else>{{ row.sellOrderItemCode || '—' }}</span>
        </template>
      </el-table-column>
      <el-table-column prop="materialCode" :label="t('customsPages.pendlists.colMaterial')" min-width="140" show-overflow-tooltip />
      <el-table-column prop="materialName" :label="t('customsPages.pendlists.colBrand')" width="100" show-overflow-tooltip />
      <el-table-column prop="qty" :label="t('customsPages.pendlists.colQty')" width="80" align="right" />
      <el-table-column prop="overseasWarehouseName" :label="t('customsPages.pendlists.colOverseasWh')" min-width="120" show-overflow-tooltip />
      <el-table-column prop="status" :label="t('customsPages.pendlists.colStatus')" width="140">
        <template #default="{ row }">{{ statusLabel(row.status) }}</template>
      </el-table-column>
      <el-table-column prop="customsStockOutNotifyCode" :label="t('customsPages.pendlists.colCustomsSor')" min-width="140" />
      <el-table-column prop="customerName" :label="t('customsPages.pendlists.colCustomer')" min-width="120" show-overflow-tooltip />
      <el-table-column prop="createTime" :label="t('customsPages.pendlists.colCreateTime')" width="110">
        <template #default="{ row }">{{ formatDate(row.createTime) }}</template>
      </el-table-column>
      <el-table-column prop="createUserDisplay" :label="t('customsPages.pendlists.colCreator')" width="100" />
      <el-table-column
        :label="t('customsPages.pendlists.colActions')"
        :width="opColWidth"
        :min-width="opColMinWidth"
        fixed="right"
        align="center"
        class-name="op-col"
        label-class-name="op-col"
        :resizable="false"
      >
        <template #header>
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
        <template #default="{ row }">
          <div @click.stop @dblclick.stop>
            <div v-if="opColExpanded" class="action-btns">
              <button
                v-if="canForceDelete"
                type="button"
                class="action-btn action-btn--danger"
                @click.stop="handleForceDelete(row)"
              >
                {{ t('customsPages.pendlists.forceDelete') }}
              </button>
              <button
                v-if="canWriteLogisticsData && row.status === CUSTOMS_PENDLIST_STATUS.Open"
                type="button"
                class="action-btn action-btn--primary"
                :disabled="creatingId === row.id"
                @click.stop="onCreateCustomsOutNotify(row)"
              >
                {{ t('customsPages.pendlists.createCustomsOutNotify') }}
              </button>
            </div>
            <el-dropdown v-else trigger="click" placement="bottom-end">
              <div class="op-more-dropdown-trigger">
                <button type="button" class="op-more-trigger">...</button>
              </div>
              <template #dropdown>
                <el-dropdown-menu>
                  <el-dropdown-item
                    v-if="canForceDelete"
                    @click.stop="handleForceDelete(row)"
                  >
                    <span class="op-more-item op-more-item--danger">
                      {{ t('customsPages.pendlists.forceDelete') }}
                    </span>
                  </el-dropdown-item>
                  <el-dropdown-item
                    v-if="canWriteLogisticsData && row.status === CUSTOMS_PENDLIST_STATUS.Open"
                    @click.stop="onCreateCustomsOutNotify(row)"
                  >
                    <span class="op-more-item op-more-item--primary">
                      {{ t('customsPages.pendlists.createCustomsOutNotify') }}
                    </span>
                  </el-dropdown-item>
                </el-dropdown-menu>
              </template>
            </el-dropdown>
          </div>
        </template>
      </el-table-column>
    </el-table>

    <div v-if="refPanel.visible" class="so-item-line-detail-panel">
      <div class="so-item-line-detail-panel__head">
        <span class="so-item-line-detail-panel__title">{{ t('customsPages.pendlists.refPanelTitle') }}</span>
        <span class="so-item-line-detail-panel__code panel-hint__value">{{ refPanel.salesStockOutNotifyCode || '—' }}</span>
        <button type="button" class="so-item-line-detail-panel__close" @click="closeRefPanel">
          {{ t('customsPages.pendlists.refPanelClose') }}
        </button>
      </div>
      <el-alert
        v-if="refPanel.loadError"
        type="error"
        :closable="false"
        :title="refPanel.loadError"
        class="so-item-line-detail-panel__alert"
        show-icon
      />
      <div v-loading="refPanel.loading" class="so-item-line-detail-panel__body so-item-line-detail-panel__body--tabbed">
        <div class="tabs-section so-item-line-detail-tabs-section">
          <div class="tabs-nav">
            <button
              type="button"
              class="tab-btn"
              :class="{ 'tab-btn--active': refPanel.activeTab === 'stock' }"
              @click="refPanel.activeTab = 'stock'"
            >
              {{ formatRefTabLabel(t('customsPages.pendlists.refPanelTabStock'), stockItems.length) }}
            </button>
          </div>
          <div class="tabs-body">
            <div v-show="refPanel.activeTab === 'stock'">
              <SellOrderItemStockTabTable :items="stockItems" />
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, h, inject, onMounted, onUnmounted, reactive, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute } from 'vue-router'
import { ElButton, ElMessage, ElMessageBox } from 'element-plus'
import { DocumentCopy } from '@element-plus/icons-vue'
import {
  CUSTOMS_PENDLIST_STATUS,
  createCustomsOutNotifyFromPendlist,
  fetchCustomsPendlists,
  forceDeleteCustomsPendlist,
  type CustomsPendlistListItemDto
} from '@/api/customs'
import { salesOrderApi, type SellOrderItemStockTabRow } from '@/api/salesOrder'
import { useDepartmentDataReadOnly } from '@/composables/useDepartmentDataReadOnly'
import { useListRightOpsPanelInteraction } from '@/composables/useListRightOpsPanelInteraction'
import { getApiErrorMessage } from '@/utils/apiError'
import { copyTextToClipboard } from '@/utils/clipboard'
import SellOrderItemStockTabTable from '@/components/RFQ/SellOrderItemStockTabTable.vue'
import { useCrmListClickedRow } from '@/utils/crmListClickedRow'
import { useAuthStore } from '@/stores/auth'
import { useCustomsPendlistFlowPanelStore } from '@/stores/customsPendlistFlowPanel'
import { WorkspaceLayoutKey } from '@/composables/useWorkspaceLayout'

const { t } = useI18n()
const route = useRoute()
const authStore = useAuthStore()
const workspaceLayout = inject(WorkspaceLayoutKey, null)
const flowStore = useCustomsPendlistFlowPanelStore()
const { canWriteLogisticsData } = useDepartmentDataReadOnly()
const canForceDelete = computed(() => authStore.canForceDelete())
const loading = ref(false)
const creatingId = ref('')
const list = ref<CustomsPendlistListItemDto[]>([])

/** 《列表操作列规范》：默认收起；列头仅 `<`/`>` */
const OP_COL_EXPANDED_WIDTH = 168
const OP_COL_EXPANDED_MIN_WIDTH = 156
const OP_COL_COLLAPSED_WIDTH = 48
const opColExpanded = ref(false)
const opColWidth = computed(() =>
  opColExpanded.value ? OP_COL_EXPANDED_WIDTH : OP_COL_COLLAPSED_WIDTH
)
const opColMinWidth = computed(() =>
  opColExpanded.value ? OP_COL_EXPANDED_MIN_WIDTH : OP_COL_COLLAPSED_WIDTH
)
function toggleOpCol() {
  opColExpanded.value = !opColExpanded.value
}
const { markClickedRow, clickedRowClassName: pendlistClickedClass, clearClickedRow } = useCrmListClickedRow(
  list,
  'id'
)

function pendlistRowClassName({ row }: { row: CustomsPendlistListItemDto }) {
  const clicked = pendlistClickedClass({ row: row as unknown as Record<string, unknown> })
  const flowActive =
    flowStore.row && flowStore.rowKey(flowStore.row) === flowStore.rowKey(row)
      ? 'so-item-row--active'
      : ''
  return [clicked, flowActive, 'table-row-pointer'].filter(Boolean).join(' ')
}

const { onOpsPanelRowClick } = useListRightOpsPanelInteraction({
  workspaceLayout,
  isActiveRoute: () => route.name === 'CustomsPendlistList',
  hasSelectedRow: () => !!flowStore.row,
  setRowOnly: (row) => flowStore.setRowOnly(row as unknown as CustomsPendlistListItemDto),
  selectRow: (row) =>
    flowStore.selectRow(
      row as unknown as CustomsPendlistListItemDto,
      t('customsPages.pendlists.flowPanel.loadFailed')
    ),
  loadSelected: () => {
    void flowStore.loadSelected(t('customsPages.pendlists.flowPanel.loadFailed'))
  },
  dataTabIds: ['r-flow']
})
const stockItems = ref<SellOrderItemStockTabRow[]>([])
const filters = reactive<{ status?: number; keyword: string }>({
  status: CUSTOMS_PENDLIST_STATUS.Open,
  keyword: ''
})

const refPanel = reactive({
  visible: false,
  pendlistId: '',
  salesOrderId: '',
  sellOrderItemId: '',
  salesStockOutNotifyCode: '',
  activeTab: 'stock' as 'stock',
  loading: false,
  loadError: ''
})

function statusLabel(v: number) {
  if (v === CUSTOMS_PENDLIST_STATUS.Open) return t('customsPages.pendlists.statusOpen')
  if (v === CUSTOMS_PENDLIST_STATUS.CustomsOutNotifyCreated) return t('customsPages.pendlists.statusCustomsOutCreated')
  if (v === CUSTOMS_PENDLIST_STATUS.InCustomsProcess) return t('customsPages.pendlists.statusInProcess')
  if (v === CUSTOMS_PENDLIST_STATUS.Closed) return t('customsPages.pendlists.statusClosed')
  if (v === CUSTOMS_PENDLIST_STATUS.Cancelled) return t('customsPages.pendlists.statusCancelled')
  return String(v)
}

function formatDate(iso: string) {
  if (!iso) return '—'
  return iso.slice(0, 10)
}

function formatRefTabLabel(label: string, count: number) {
  return count > 0 ? `${label} (${count})` : label
}

function closeRefPanel() {
  refPanel.visible = false
  refPanel.loadError = ''
  stockItems.value = []
  clearClickedRow()
}

async function loadRefPanelStock(row: CustomsPendlistListItemDto) {
  const salesOrderId = String(row.salesOrderId ?? '').trim()
  const sellOrderItemId = String(row.sellOrderItemId ?? '').trim()
  if (!salesOrderId || !sellOrderItemId) {
    refPanel.loadError = t('customsPages.pendlists.refPanelMissingSo')
    stockItems.value = []
    return
  }

  refPanel.loading = true
  refPanel.loadError = ''
  stockItems.value = []
  try {
    const agg = await salesOrderApi.getSellOrderItemDetailTabAggregates(salesOrderId, sellOrderItemId)
    stockItems.value = agg.stockItems ?? []
  } catch (e: unknown) {
    refPanel.loadError = getApiErrorMessage(e, t('customsPages.pendlists.refPanelLoadFailed'))
  } finally {
    refPanel.loading = false
  }
}

async function onPendlistRowClick(row: CustomsPendlistListItemDto) {
  markClickedRow(row as unknown as Record<string, unknown>)
  refPanel.visible = true
  refPanel.pendlistId = row.id
  refPanel.salesOrderId = String(row.salesOrderId ?? '').trim()
  refPanel.sellOrderItemId = String(row.sellOrderItemId ?? '').trim()
  refPanel.salesStockOutNotifyCode = String(row.salesStockOutNotifyCode ?? row.salesStockOutNotifyId ?? '').trim()
  refPanel.activeTab = 'stock'
  await Promise.all([
    loadRefPanelStock(row),
    onOpsPanelRowClick(row as unknown as Record<string, unknown>)
  ])
}

async function handleForceDelete(row: CustomsPendlistListItemDto) {
  if (!canForceDelete.value) return
  const id = String(row.id || '').trim()
  if (!id) return
  let entered = ''
  try {
    const ret = await ElMessageBox.prompt(
      h('div', { class: 'pendlist-force-delete-msg' }, [
        h('p', { class: 'pendlist-force-delete-msg__text' }, t('customsPages.pendlists.forceDeletePrompt')),
        h('div', { class: 'pendlist-force-delete-msg__id-row' }, [
          h('code', { class: 'pendlist-force-delete-msg__id' }, id),
          h(
            ElButton,
            {
              size: 'small',
              type: 'primary',
              link: true,
              icon: DocumentCopy,
              onClick: (e: MouseEvent) => {
                e.preventDefault()
                e.stopPropagation()
                if (copyTextToClipboard(id)) {
                  ElMessage.success(t('common.copySuccess'))
                } else {
                  ElMessage.error(t('common.copyFailed'))
                }
              }
            },
            () => t('common.copy')
          )
        ])
      ]),
      t('customsPages.pendlists.forceDeleteTitle'),
      {
        inputPlaceholder: id,
        confirmButtonText: t('common.confirm'),
        cancelButtonText: t('common.cancel'),
        type: 'warning',
        customClass: 'pendlist-force-delete-box'
      }
    )
    entered = String(ret.value || '').trim()
  } catch {
    return
  }
  if (entered.toLowerCase() !== id.toLowerCase()) {
    ElMessage.error(t('customsPages.pendlists.forceDeleteMismatch'))
    return
  }
  try {
    await forceDeleteCustomsPendlist(id, entered)
    ElMessage.success(t('customsPages.pendlists.forceDeleteSuccess'))
    if (flowStore.row && flowStore.rowKey(flowStore.row) === id) flowStore.clear()
    if (refPanel.pendlistId === id) closeRefPanel()
    await loadList()
  } catch (e: unknown) {
    ElMessage.error(getApiErrorMessage(e, t('customsPages.pendlists.forceDeleteFailed')))
  }
}

async function loadList() {
  loading.value = true
  try {
    const params: { status?: number; keyword?: string; take?: number } = { take: 500 }
    if (filters.status != null) params.status = filters.status
    const kw = filters.keyword.trim()
    if (kw) params.keyword = kw
    list.value = await fetchCustomsPendlists(params)
  } catch (e: unknown) {
    ElMessage.error(e instanceof Error ? e.message : t('customsPages.pendlists.createFailed'))
  } finally {
    loading.value = false
  }
}

function resetFilters() {
  filters.status = CUSTOMS_PENDLIST_STATUS.Open
  filters.keyword = ''
  void loadList()
}

async function onCreateCustomsOutNotify(row: CustomsPendlistListItemDto) {
  if (row.status !== CUSTOMS_PENDLIST_STATUS.Open) {
    ElMessage.warning(t('customsPages.pendlists.onlyOpenCanCreate'))
    return
  }
  try {
    await ElMessageBox.confirm(t('customsPages.pendlists.createConfirm'), t('common.confirm'), {
      type: 'warning',
      confirmButtonText: t('common.confirm'),
      cancelButtonText: t('common.cancel')
    })
  } catch {
    return
  }

  creatingId.value = row.id
  try {
    const result = await createCustomsOutNotifyFromPendlist(row.id)
    ElMessage.success(
      t('customsPages.pendlists.createOk', { code: result.customsStockOutNotifyCode })
    )
    await loadList()
    if (refPanel.visible && refPanel.pendlistId === row.id) {
      const refreshed = list.value.find((x) => x.id === row.id)
      if (refreshed) await loadRefPanelStock(refreshed)
    }
  } catch (e: unknown) {
    ElMessage.error(e instanceof Error ? e.message : t('customsPages.pendlists.createFailed'))
  } finally {
    creatingId.value = ''
  }
}

onMounted(() => {
  void loadList()
})

onUnmounted(() => {
  flowStore.clear()
})
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.customs-page {
  padding: 16px 20px 24px;
}
.page-header {
  margin-bottom: 16px;
}
.page-title {
  margin: 0;
  font-size: 20px;
  font-weight: 600;
}
.filter-bar {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  margin-bottom: 12px;
  align-items: center;
}
.data-table {
  width: 100%;
}

.link-text {
  color: inherit;
  text-decoration: none;
  cursor: default;

  &:hover {
    color: var(--el-color-primary);
    text-decoration: underline;
    cursor: pointer;
  }
}

.tabs-section {
  background: $layer-2;
  border: 1px solid $border-card;
  border-radius: $border-radius-lg;
  padding: 0 20px 20px;
}

.tabs-nav {
  display: flex;
  border-bottom: 1px solid rgba(255, 255, 255, 0.06);
  padding: 0 16px;
  background: rgba(0, 0, 0, 0.1);
}

.tab-btn {
  padding: 12px 16px;
  background: transparent;
  border: none;
  border-bottom: 2px solid transparent;
  color: $text-muted;
  font-size: 13px;
  cursor: pointer;
  margin-bottom: -1px;
}

.tab-btn--active {
  color: $cyan-primary;
  border-bottom-color: $cyan-primary;
}

.tabs-body {
  padding: 20px;
}

.so-item-line-detail-panel {
  margin-top: 20px;
  border: 1px solid $border-panel;
  border-radius: $border-radius-md;
  background: $layer-2;
  overflow: hidden;
}

.so-item-line-detail-panel__head {
  display: flex;
  align-items: center;
  gap: 12px;
  flex-wrap: wrap;
  padding: 12px 16px;
  border-bottom: 1px solid $border-panel;
  background: rgba(0, 212, 255, 0.04);
}

.so-item-line-detail-panel__title {
  font-size: 14px;
  font-weight: 600;
  color: $text-primary;
}

.so-item-line-detail-panel__code {
  font-size: 14px;
  font-weight: 600;
  font-variant-numeric: tabular-nums;
}

.so-item-line-detail-panel__close {
  margin-left: auto;
  padding: 4px 12px;
  font-size: 13px;
  color: rgba(200, 220, 240, 0.9);
  background: transparent;
  border: 1px solid rgba(0, 212, 255, 0.25);
  border-radius: $border-radius-sm;
  cursor: pointer;
  &:hover {
    border-color: rgba(0, 212, 255, 0.45);
    color: #e8f4ff;
  }
}

.so-item-line-detail-panel__alert {
  margin: 12px 16px 0;
}

.so-item-line-detail-panel__body {
  padding: 12px 16px 16px;
}

.so-item-line-detail-panel__body--tabbed {
  padding: 0;
}

.so-item-line-detail-tabs-section.tabs-section {
  background: transparent;
  border: none;
  border-radius: 0;
  padding: 0;
  margin: 0;
}
</style>

<style lang="scss">
/* MessageBox 挂到 body，不可 scoped */
.pendlist-force-delete-msg {
  text-align: left;
}

.pendlist-force-delete-msg__text {
  margin: 0 0 10px;
  line-height: 1.5;
}

.pendlist-force-delete-msg__id-row {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 8px;
}

.pendlist-force-delete-msg__id {
  font-family: ui-monospace, SFMono-Regular, Menlo, Consolas, monospace;
  font-size: 12px;
  word-break: break-all;
  user-select: all;
}
</style>
