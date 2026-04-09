<template>
  <div class="purchase-requisition-list-page">
    <div class="page-header">
      <h2>{{ t('purchaseRequisitionList.title') }}</h2>
      <el-button type="primary" @click="handleCreate">
        <el-icon><Plus /></el-icon>{{ t('purchaseRequisitionList.create') }}
      </el-button>
    </div>

    <!-- 搜索栏：对齐客户列表 CustomerList -->
    <div class="search-bar">
      <div class="search-left">
        <span class="filter-field-label">{{ t('purchaseRequisitionList.filters.billCode') }}</span>
        <div class="search-input-wrap">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="search-icon">
            <circle cx="11" cy="11" r="8" />
            <line x1="21" y1="21" x2="16.65" y2="16.65" />
          </svg>
          <input
            v-model="filterForm.billCode"
            class="search-input"
            :placeholder="t('purchaseRequisitionList.filters.billCodePlaceholder')"
            @keyup.enter="handleSearch"
          />
        </div>
        <span class="filter-field-label">{{ t('purchaseRequisitionList.filters.sellOrder') }}</span>
        <div class="search-input-wrap">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="search-icon">
            <circle cx="11" cy="11" r="8" />
            <line x1="21" y1="21" x2="16.65" y2="16.65" />
          </svg>
          <input
            v-model="filterForm.sellOrderId"
            class="search-input search-input--narrow"
            :placeholder="t('purchaseRequisitionList.filters.sellOrderPlaceholder')"
            @keyup.enter="handleSearch"
          />
        </div>
        <el-select
          v-model="filterForm.status"
          :placeholder="t('purchaseRequisitionList.filters.allStatus')"
          clearable
          class="status-select"
          :teleported="false"
          @change="handleSearch"
        >
          <el-option :value="0" :label="t('purchaseRequisitionList.status.new')" />
          <el-option :value="1" :label="t('purchaseRequisitionList.status.partialDone')" />
          <el-option :value="2" :label="t('purchaseRequisitionList.status.allDone')" />
          <el-option :value="3" :label="t('purchaseRequisitionList.status.cancelled')" />
        </el-select>
        <button class="btn-primary btn-sm" type="button" :disabled="loading" @click="handleSearch">
          {{ t('purchaseRequisitionList.filters.search') }}
        </button>
        <button class="btn-ghost btn-sm" type="button" :disabled="loading" @click="handleReset">
          {{ t('purchaseRequisitionList.filters.reset') }}
        </button>
      </div>
    </div>

    <el-card class="table-card">
      <CrmDataTable
        ref="dataTableRef"
        column-layout-key="purchase-requisition-list-main"
        :columns="purchaseReqColumns"
        :show-column-settings="false"
        :data="list"
        v-loading="loading"
        highlight-current-row
        @row-dblclick="handleView"
      >
        <template #col-status="{ row }">
          <el-tag effect="dark" :type="getStatusTagType(row.status)" size="small">
            {{ getStatusText(row.status) }}
          </el-tag>
        </template>
        <template #col-expectedPurchaseTime="{ row }">{{ formatDisplayDateTime(row.expectedPurchaseTime) }}</template>
        <template #col-type="{ row }">{{ getPrTypeLabel(row.type) }}</template>
        <template #col-purchaseUserId="{ row }">{{ row.purchaseUserAccount || row.purchaseUserId || '--' }}</template>
        <template #col-createTime="{ row }">{{ row.createTime ? formatDisplayDateTime(row.createTime) : '--' }}</template>
        <template #col-createUser="{ row }">{{ row.createUserAccount || row.createUserName || row.createdBy || '--' }}</template>
        <template #col-actions-header>
          <div class="op-col-header">
            <span class="op-col-header-text">{{ t('purchaseRequisitionList.columns.actions') }}</span>
            <button type="button" class="op-col-toggle-btn" @click.stop="toggleOpCol">
              {{ opColExpanded ? '>' : '<' }}
            </button>
          </div>
        </template>
        <template #col-actions="{ row }">
          <div @click.stop @dblclick.stop>
            <div v-if="opColExpanded" class="action-btns">
              <el-button link type="primary" @click.stop="handleView(row)">{{ t('purchaseRequisitionList.actions.view') }}</el-button>
              <el-tooltip
                v-if="!canGeneratePurchaseOrder"
                :content="t('purchaseRequisitionList.actions.generatePoDeniedTip')"
                placement="top"
              >
                <span class="inline-flex">
                  <el-button link type="warning" size="small" disabled>
                    {{ t('purchaseRequisitionList.actions.generatePo') }}
                  </el-button>
                </span>
              </el-tooltip>
              <el-button
                v-else
                link
                type="warning"
                size="small"
                @click.stop="handleGeneratePurchaseOrder(row)"
              >
                {{ t('purchaseRequisitionList.actions.generatePo') }}
              </el-button>
            </div>

            <el-dropdown v-else trigger="click" placement="bottom-end">
              <div class="op-more-dropdown-trigger">
                <button type="button" class="op-more-trigger">...</button>
              </div>
              <template #dropdown>
                <el-dropdown-menu>
                  <el-dropdown-item @click.stop="handleView(row)">
                    <span class="op-more-item op-more-item--primary">{{ t('purchaseRequisitionList.actions.view') }}</span>
                  </el-dropdown-item>
                  <el-dropdown-item v-if="canGeneratePurchaseOrder" @click.stop="handleGeneratePurchaseOrder(row)">
                    <span class="op-more-item op-more-item--warning">{{ t('purchaseRequisitionList.actions.generatePo') }}</span>
                  </el-dropdown-item>
                  <el-dropdown-item v-else disabled>
                    <span class="op-more-item op-more-item--muted">{{ t('purchaseRequisitionList.actions.generatePo') }}</span>
                  </el-dropdown-item>
                </el-dropdown-menu>
              </template>
            </el-dropdown>
          </div>
        </template>
      </CrmDataTable>

      <div class="pagination-wrapper">
        <div class="list-footer-left">
          <el-tooltip :content="t('purchaseRequisitionList.columnSettings')" placement="top" :hide-after="0">
            <el-button
              class="list-settings-btn"
              link
              type="primary"
              :aria-label="t('purchaseRequisitionList.columnSettings')"
              @click="dataTableRef?.openColumnSettings?.()"
            >
              <el-icon><Setting /></el-icon>
            </el-button>
          </el-tooltip>
          <div class="list-footer-spacer" aria-hidden="true"></div>
        </div>
        <el-pagination
          v-model:current-page="page"
          v-model:page-size="pageSize"
          :total="total"
          :page-sizes="[10, 20, 50, 100]"
          layout="total, sizes, prev, pager, next, jumper"
          @current-change="loadList"
          @size-change="loadList"
        />
      </div>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { computed, ref, reactive, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { Plus, Setting } from '@element-plus/icons-vue'
import { purchaseRequisitionApi } from '@/api/purchaseRequisition'
import { useAuthStore } from '@/stores/auth'
import { canGeneratePurchaseOrderFromRequisition } from '@/utils/purchaseOrderCreateGate'
import CrmDataTable from '@/components/CrmDataTable.vue'
import { formatDisplayDateTime } from '@/utils/displayDateTime'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'

const router = useRouter()
const { t, locale } = useI18n()
const authStore = useAuthStore()

const canGeneratePurchaseOrder = computed(() =>
  canGeneratePurchaseOrderFromRequisition({
    isSysAdmin: authStore.user?.isSysAdmin,
    identityType: authStore.user?.identityType,
    roleCodes: authStore.user?.roleCodes,
    hasPermission: (code) => authStore.hasPermission(code)
  })
)

const loading = ref(false)
const list = ref<any[]>([])
const total = ref(0)
const page = ref(1)
const pageSize = ref(20)
const dataTableRef = ref<InstanceType<typeof CrmDataTable> | null>(null)

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

const purchaseReqColumns = computed<CrmTableColumnDef[]>(() => {
  void locale.value
  return [
    {
      key: 'billCode',
      label: t('purchaseRequisitionList.columns.billCode'),
      prop: 'billCode',
      width: 160,
      minWidth: 160,
      showOverflowTooltip: true
    },
    { key: 'status', label: t('purchaseRequisitionList.columns.status'), prop: 'status', width: 160, align: 'center' },
    {
      key: 'sellOrderCode',
      label: t('purchaseRequisitionList.columns.sellOrder'),
      prop: 'sellOrderCode',
      width: 160,
      minWidth: 160,
      showOverflowTooltip: true
    },
    { key: 'pn', label: t('purchaseRequisitionList.columns.pn'), prop: 'pn', minWidth: 140, showOverflowTooltip: true },
    { key: 'brand', label: t('purchaseRequisitionList.columns.brand'), prop: 'brand', minWidth: 120, showOverflowTooltip: true },
    { key: 'qty', label: t('purchaseRequisitionList.columns.qty'), prop: 'qty', width: 120, align: 'right' },
    {
      key: 'expectedPurchaseTime',
      label: t('purchaseRequisitionList.columns.expectedPurchaseTime'),
      prop: 'expectedPurchaseTime',
      width: 160
    },
    { key: 'type', label: t('purchaseRequisitionList.columns.type'), prop: 'type', width: 110, align: 'center' },
    {
      key: 'purchaseUserId',
      label: t('purchaseRequisitionList.columns.purchaseUserId'),
      prop: 'purchaseUserId',
      width: 140,
      showOverflowTooltip: true
    },
    { key: 'remark', label: t('purchaseRequisitionList.columns.remark'), prop: 'remark', minWidth: 180, showOverflowTooltip: true },
    { key: 'createTime', label: t('purchaseRequisitionList.columns.createTime'), width: 160 },
    { key: 'createUser', label: t('purchaseRequisitionList.columns.createUser'), width: 120, showOverflowTooltip: true },
    {
      key: 'actions',
      label: t('purchaseRequisitionList.columns.actions'),
      width: opColWidth.value,
      minWidth: opColMinWidth.value,
      fixed: 'right',
      hideable: false,
      pinned: 'end',
      reorderable: false,
      className: 'op-col',
      labelClassName: 'op-col'
    }
  ]
})

const filterForm = reactive({
  billCode: '',
  sellOrderId: '',
  status: undefined as number | undefined
})

function getStatusText(s: number) {
  const m: Record<number, string> = {
    0: t('purchaseRequisitionList.status.new'),
    1: t('purchaseRequisitionList.status.partialDone'),
    2: t('purchaseRequisitionList.status.allDone'),
    3: t('purchaseRequisitionList.status.cancelled')
  }
  return m[s] ?? String(s)
}

function getStatusTagType(s: number): '' | 'success' | 'warning' | 'info' | 'danger' {
  if (s === 0) return 'info'
  if (s === 1 || s === 2) return 'success'
  if (s === 3) return 'danger'
  return ''
}

function getPrTypeLabel(typeVal: number) {
  const m: Record<number, string> = {
    0: t('purchaseRequisitionList.type.exclusive'),
    1: t('purchaseRequisitionList.type.publicStock')
  }
  return m[typeVal] ?? String(typeVal)
}

async function loadList() {
  loading.value = true
  try {
    const data = await purchaseRequisitionApi.getList({
      keyword: filterForm.billCode.trim() || undefined,
      sellOrderId: filterForm.sellOrderId.trim() || undefined,
      status: filterForm.status,
      page: page.value,
      pageSize: pageSize.value
    })
    list.value = data?.items ?? []
    total.value = data?.total ?? 0
  } catch (e: any) {
    // eslint-disable-next-line no-console
    console.error(e)
  } finally {
    loading.value = false
  }
}

function handleSearch() {
  page.value = 1
  loadList()
}

function handleReset() {
  filterForm.billCode = ''
  filterForm.sellOrderId = ''
  filterForm.status = undefined
  page.value = 1
  loadList()
}

function handleCreate() {
  router.push('/purchase-requisitions/new')
}

function handleView(row: any) {
  router.push(`/purchase-requisitions/${row.id}`)
}

function handleGeneratePurchaseOrder(row: any) {
  if (!canGeneratePurchaseOrder.value) {
    return
  }
  router.push({ name: 'PurchaseOrderCreate', query: { requisitionId: row?.id } })
}

onMounted(loadList)
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.purchase-requisition-list-page {
  padding: 24px;
  min-height: 100%;
  background: $layer-1;
  font-family: 'Noto Sans SC', sans-serif;
}

.page-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 20px;
}

.page-header h2 {
  margin: 0;
  color: $text-primary;
  font-size: 20px;
  font-weight: 600;
}

// ---- 搜索栏（与 CustomerList 一致）----
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

.filter-field-label {
  font-size: 12px;
  font-weight: 500;
  color: $text-muted;
  white-space: nowrap;
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
  width: 220px;
  padding: 7px 12px 7px 32px;
  background: $layer-2;
  border: 1px solid $border-panel;
  border-radius: $border-radius-md;
  color: $text-primary;
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
  outline: none;
  transition: border-color 0.2s;

  &--narrow {
    width: 180px;
  }

  &::placeholder {
    color: $text-muted;
  }
  &:focus {
    border-color: rgba(0, 212, 255, 0.4);
  }
}

.status-select {
  width: 140px;
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

  &:hover:not(:disabled) {
    border-color: rgba(0, 212, 255, 0.3);
    color: $text-secondary;
  }

  &:disabled {
    opacity: 0.55;
    cursor: not-allowed;
  }
}

.table-card {
  margin-top: 0;
  background: #0a1628;
  border: 1px solid rgba(0, 212, 255, 0.1);
  :deep(.el-table) {
    background: transparent;
    --el-table-header-bg-color: rgba(0, 212, 255, 0.1);
    --el-table-tr-bg-color: transparent;
    --el-table-border-color: rgba(0, 212, 255, 0.1);
    color: #e8f4ff;

    .el-table__cell .el-button {
      white-space: nowrap !important;
    }
  }
}

.pagination-wrapper {
  margin-top: 20px;
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
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

.list-footer-spacer {
  width: 26px;
  flex: 0 0 26px;
}

.inline-flex {
  display: inline-flex;
}

.op-more-item--muted {
  opacity: 0.45;
}
</style>

