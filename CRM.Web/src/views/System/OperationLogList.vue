<template>
  <div class="operation-log-list-page">
    <div class="page-header">
      <div class="header-left">
        <div class="page-title-group">
          <div class="page-icon" aria-hidden="true">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
              <path d="M14 2H6a2 2 0 00-2 2v16a2 2 0 002 2h12a2 2 0 002-2V8z" />
              <polyline points="14 2 14 8 20 8" />
              <line x1="16" y1="13" x2="8" y2="13" />
              <line x1="16" y1="17" x2="8" y2="17" />
            </svg>
          </div>
          <h1 class="page-title">{{ t('operationLog.title') }}</h1>
        </div>
        <div class="count-badge">{{ t('operationLog.count', { count: total }) }}</div>
      </div>
    </div>

    <!-- 筛选：与业务列表 CustomerList / StockOutNotifyList 同一套 search-bar 结构 -->
    <div class="search-bar">
      <div class="search-left">
        <span class="filter-field-label">{{ t('operationLog.bizType') }}</span>
        <el-select
          v-model="filters.bizType"
          clearable
          filterable
          :placeholder="t('operationLog.any')"
          class="status-select status-select--biz"
          :teleported="false"
        >
          <el-option v-for="b in bizTypeOptions" :key="b" :label="operationBizTypeLabel(b)" :value="b" />
        </el-select>

        <span class="filter-field-label">{{ t('operationLog.actionType') }}</span>
        <el-input v-model="filters.actionType" clearable class="filter-el-input filter-el-input--sm" :placeholder="t('operationLog.contains')" @keyup.enter="onQuery" />

        <span class="filter-field-label">{{ t('operationLog.recordCode') }}</span>
        <el-input v-model="filters.recordCode" clearable class="filter-el-input filter-el-input--sm" :placeholder="t('operationLog.contains')" @keyup.enter="onQuery" />

        <span class="filter-field-label">{{ t('operationLog.operatorUserName') }}</span>
        <el-input v-model="filters.operatorUserName" clearable class="filter-el-input filter-el-input--sm" :placeholder="t('operationLog.contains')" @keyup.enter="onQuery" />

        <span class="filter-field-label">{{ t('operationLog.operationTime') }}</span>
        <el-date-picker
          v-model="timeRange"
          type="datetimerange"
          value-format="x"
          range-separator="—"
          :start-placeholder="t('operationLog.timeFrom')"
          :end-placeholder="t('operationLog.timeTo')"
          clearable
          class="filter-date-range"
          :teleported="false"
        />

        <span class="filter-field-label">{{ t('operationLog.reason') }}</span>
        <el-input v-model="filters.reason" clearable class="filter-el-input" :placeholder="t('operationLog.contains')" @keyup.enter="onQuery" />

        <button type="button" class="btn-primary btn-sm" :disabled="loading" @click="onQuery">{{ t('operationLog.query') }}</button>
        <button type="button" class="btn-ghost btn-sm" :disabled="loading" @click="reset">{{ t('operationLog.reset') }}</button>
      </div>
    </div>

    <div class="table-wrapper" v-loading="loading">
      <CrmDataTable
        v-show="loading || rows.length > 0"
        ref="dataTableRef"
        row-key="id"
        column-layout-key="operation-log-list-main"
        :columns="tableColumns"
        :show-column-settings="false"
        :density-toggle-anchor-el="rowDensityToggleAnchorEl"
        :data="rows"
      >
        <template #col-operationTime="{ row }">
          {{ formatOpTime(row.operationTime) }}
        </template>
        <template #col-bizType="{ row }">
          {{ operationBizTypeLabel(row.bizType) }}
        </template>
      </CrmDataTable>

      <div v-show="!loading && rows.length === 0" class="empty-state">
        <svg width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1" aria-hidden="true">
          <path d="M14 2H6a2 2 0 00-2 2v16a2 2 0 002 2h12a2 2 0 002-2V8z" />
          <polyline points="14 2 14 8 20 8" />
        </svg>
        <p>{{ t('operationLog.empty') }}</p>
      </div>
    </div>

    <div class="pagination-wrapper">
      <div class="list-footer-left">
        <el-tooltip :content="t('systemUser.colSetting')" placement="top" :hide-after="0">
          <el-button
            class="list-settings-btn"
            link
            type="primary"
            :aria-label="t('systemUser.colSetting')"
            @click="dataTableRef?.openColumnSettings?.()"
          >
            <el-icon><Setting /></el-icon>
          </el-button>
        </el-tooltip>
        <span ref="rowDensityToggleAnchorEl" class="list-footer-density-anchor" aria-hidden="true" />
        <div class="list-footer-spacer" aria-hidden="true" />
      </div>
      <el-pagination
        class="list-main-pagination"
        v-model:current-page="page"
        v-model:page-size="pageSize"
        :total="total"
        :page-sizes="[20, 50, 100]"
        layout="total, sizes, prev, pager, next"
        @size-change="onSizeChange"
        @current-change="load"
      />
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted, computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import { Setting } from '@element-plus/icons-vue'
import CrmDataTable from '@/components/CrmDataTable.vue'
import { operationLogsApi, type OperationLogRow } from '@/api/operationLogs'
import { formatDisplayDateTime } from '@/utils/displayDateTime'
import { operationBizTypeLabel } from '@/utils/businessLogLabels'
import { BusinessLogTypes } from '@/constants/businessLogTypes'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'

const { t, locale } = useI18n()

const bizTypeOptions = [
  BusinessLogTypes.Customer,
  BusinessLogTypes.CustomerContact,
  BusinessLogTypes.Vendor,
  BusinessLogTypes.VendorContact,
  BusinessLogTypes.SalesOrder,
  BusinessLogTypes.PurchaseOrder
]

const dataTableRef = ref<{ openColumnSettings?: () => void } | null>(null)
const rowDensityToggleAnchorEl = ref<HTMLElement | null>(null)

const loading = ref(false)
const rows = ref<OperationLogRow[]>([])
const total = ref(0)
const page = ref(1)
const pageSize = ref(20)
const timeRange = ref<[number, number] | null>(null)

const filters = reactive({
  bizType: '' as string,
  actionType: '',
  recordCode: '',
  operatorUserName: '',
  reason: ''
})

const tableColumns = computed<CrmTableColumnDef[]>(() => {
  void locale.value
  return [
    { key: 'operationTime', label: t('operationLog.colOperationTime'), prop: 'operationTime', width: 168 },
    { key: 'bizType', label: t('operationLog.colBizType'), prop: 'bizType', width: 120, showOverflowTooltip: true },
    { key: 'actionType', label: t('operationLog.colActionType'), prop: 'actionType', width: 120, showOverflowTooltip: true },
    { key: 'recordCode', label: t('operationLog.colRecordCode'), prop: 'recordCode', minWidth: 120, showOverflowTooltip: true },
    { key: 'operatorUserName', label: t('operationLog.colOperator'), prop: 'operatorUserName', width: 110, showOverflowTooltip: true },
    { key: 'reason', label: t('operationLog.colReason'), prop: 'reason', minWidth: 140, showOverflowTooltip: true },
    { key: 'operationDesc', label: t('operationLog.colOperationDesc'), prop: 'operationDesc', minWidth: 220, showOverflowTooltip: true }
  ]
})

function formatOpTime(v?: string | null) {
  return v ? formatDisplayDateTime(v) : '—'
}

function buildParams() {
  const p: Record<string, string | number> = {
    page: page.value,
    pageSize: pageSize.value
  }
  if (filters.bizType?.trim()) p.bizType = filters.bizType.trim()
  if (filters.actionType?.trim()) p.actionType = filters.actionType.trim()
  if (filters.recordCode?.trim()) p.recordCode = filters.recordCode.trim()
  if (filters.operatorUserName?.trim()) p.operatorUserName = filters.operatorUserName.trim()
  if (filters.reason?.trim()) p.reason = filters.reason.trim()
  if (timeRange.value?.length === 2) {
    p.operationTimeFrom = new Date(timeRange.value[0]).toISOString()
    p.operationTimeTo = new Date(timeRange.value[1]).toISOString()
  }
  return p
}

async function load() {
  loading.value = true
  try {
    const data = await operationLogsApi.list(buildParams())
    rows.value = data.items ?? []
    total.value = data.total ?? 0
  } catch (e: unknown) {
    ElMessage.error(e instanceof Error ? e.message : String(t('operationLog.loadFailed')))
  } finally {
    loading.value = false
  }
}

function onQuery() {
  page.value = 1
  load()
}

function onSizeChange() {
  page.value = 1
  load()
}

function reset() {
  filters.bizType = ''
  filters.actionType = ''
  filters.recordCode = ''
  filters.operatorUserName = ''
  filters.reason = ''
  timeRange.value = null
  page.value = 1
  pageSize.value = 20
  load()
}

onMounted(() => {
  load()
})
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';
// 表格皮肤：全局 main.scss 已引入 crm-unified-list.scss

.operation-log-list-page {
  padding: 24px;
  min-height: 100%;
  background: $layer-1;
  font-family: 'Noto Sans SC', sans-serif;
}

.page-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 20px;
}

.header-left {
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

.status-select {
  width: 120px;
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

.status-select--biz {
  width: 168px;
}

.filter-el-input {
  width: 200px;
  :deep(.el-input__wrapper) {
    background: $layer-2 !important;
    box-shadow: none !important;
    border: 1px solid $border-panel !important;
    border-radius: $border-radius-md !important;
  }
  &--sm {
    width: 140px;
  }
}

.filter-date-range {
  width: 340px;
  max-width: 100%;
  flex: 0 1 340px;
  :deep(.el-range-editor.el-input__wrapper) {
    background: $layer-2 !important;
    box-shadow: none !important;
    border: 1px solid $border-panel !important;
    border-radius: $border-radius-md !important;
  }
  :deep(.el-range-input) {
    color: $text-primary !important;
  }
  :deep(.el-range-separator) {
    color: $text-muted !important;
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
  cursor: pointer;
  transition: all 0.2s;
  &:hover:not(:disabled) {
    transform: translateY(-1px);
    box-shadow: 0 4px 16px rgba(0, 212, 255, 0.25);
  }
  &:disabled {
    opacity: 0.6;
    cursor: not-allowed;
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
  cursor: pointer;
  transition: all 0.2s;
  &:hover:not(:disabled) {
    border-color: rgba(0, 212, 255, 0.3);
    color: $text-secondary;
  }
  &:disabled {
    opacity: 0.6;
    cursor: not-allowed;
  }
}

.operation-log-list-page .table-wrapper {
  position: relative;
  :deep(.el-table .cell) {
    line-height: 1.35;
  }
}

.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 48px 24px;
  color: $text-muted;
  p {
    margin: 12px 0 0;
    font-size: 14px;
  }
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
</style>
