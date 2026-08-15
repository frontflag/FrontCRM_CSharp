<template>
  <div class="export-log-list-page">
    <div class="page-header">
      <div class="header-left">
        <div class="page-title-group">
          <div class="page-icon" aria-hidden="true">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
              <path d="M21 15v4a2 2 0 01-2 2H5a2 2 0 01-2-2v-4" />
              <polyline points="7 10 12 15 17 10" />
              <line x1="12" y1="15" x2="12" y2="3" />
            </svg>
          </div>
          <h1 class="page-title">{{ t('exportLog.title') }}</h1>
        </div>
        <div class="count-badge">{{ t('exportLog.count', { count: total }) }}</div>
      </div>
    </div>

    <div class="search-bar">
      <div class="search-left">
        <el-select
          v-model="filters.exportKind"
          clearable
          filterable
          :placeholder="t('exportLog.kindAny')"
          class="status-select status-select--kind"
          :teleported="false"
        >
          <el-option v-for="k in kindOptions" :key="k.kind" :label="k.name" :value="k.kind" />
        </el-select>

        <el-input
          v-model="filters.operatorUserName"
          clearable
          class="filter-el-input filter-el-input--sm"
          :placeholder="t('exportLog.operatorPlaceholder')"
          @keyup.enter="onQuery"
        />

        <el-date-picker
          v-model="dateRange"
          type="daterange"
          value-format="YYYY-MM-DD"
          range-separator="—"
          :start-placeholder="t('exportLog.dateFrom')"
          :end-placeholder="t('exportLog.dateTo')"
          clearable
          class="filter-date-range"
          :teleported="false"
        />

        <button type="button" class="btn-primary btn-sm" :disabled="loading" @click="onQuery">
          {{ t('exportLog.query') }}
        </button>
        <button type="button" class="btn-ghost btn-sm" :disabled="loading" @click="reset">
          {{ t('exportLog.reset') }}
        </button>
      </div>
    </div>

    <div class="table-wrapper" v-loading="loading">
      <CrmDataTable
        v-show="loading || rows.length > 0"
        ref="dataTableRef"
        row-key="id"
        column-layout-key="export-log-list-main-v1"
        :columns="tableColumns"
        :show-column-settings="false"
        :density-toggle-anchor-el="rowDensityToggleAnchorEl"
        :data="rows"
      >
        <template #col-operationTime="{ row }">
          {{ formatOpTime(row.operationTime) }}
        </template>
        <template #col-pageUrl="{ row }">
          <router-link v-if="pageHref(row)" class="link-text" :to="pageHref(row)!">
            {{ row.pageUrl }}
          </router-link>
          <span v-else>{{ row.pageUrl?.trim() || '—' }}</span>
        </template>
        <template #col-exportedCount="{ row }">
          {{ row.exportedCount == null ? '—' : row.exportedCount }}
        </template>
      </CrmDataTable>

      <div v-show="!loading && rows.length === 0" class="empty-state">
        <svg width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1" aria-hidden="true">
          <path d="M21 15v4a2 2 0 01-2 2H5a2 2 0 01-2-2v-4" />
          <polyline points="7 10 12 15 17 10" />
          <line x1="12" y1="15" x2="12" y2="3" />
        </svg>
        <p>{{ t('exportLog.empty') }}</p>
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
import { exportLogsApi, type ExportKindOption, type ExportLogRow } from '@/api/exportLogs'
import { formatDisplayDateTime } from '@/utils/displayDateTime'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'

const { t, locale } = useI18n()

const dataTableRef = ref<{ openColumnSettings?: () => void } | null>(null)
const rowDensityToggleAnchorEl = ref<HTMLElement | null>(null)

const loading = ref(false)
const rows = ref<ExportLogRow[]>([])
const kindOptions = ref<ExportKindOption[]>([])
const total = ref(0)
const page = ref(1)
const pageSize = ref(20)
const dateRange = ref<[string, string] | null>(null)

const filters = reactive({
  exportKind: '' as string,
  operatorUserName: ''
})

const tableColumns = computed<CrmTableColumnDef[]>(() => {
  void locale.value
  return [
    { key: 'operationTime', label: t('exportLog.colTime'), prop: 'operationTime', width: 168 },
    { key: 'operatorUserName', label: t('exportLog.colOperator'), prop: 'operatorUserName', width: 110, showOverflowTooltip: true },
    { key: 'exportKindName', label: t('exportLog.colKind'), prop: 'exportKindName', minWidth: 160, showOverflowTooltip: true },
    { key: 'pageTitle', label: t('exportLog.colPage'), prop: 'pageTitle', width: 140, showOverflowTooltip: true },
    { key: 'pageUrl', label: t('exportLog.colPageUrl'), prop: 'pageUrl', minWidth: 200 },
    { key: 'filterSummary', label: t('exportLog.colFilters'), prop: 'filterSummary', minWidth: 220, showOverflowTooltip: true },
    { key: 'exportedCount', label: t('exportLog.colCount'), prop: 'exportedCount', width: 100 },
    { key: 'sysRemark', label: t('exportLog.colRemark'), prop: 'sysRemark', minWidth: 180, showOverflowTooltip: true }
  ]
})

function formatOpTime(v?: string | null) {
  return v ? formatDisplayDateTime(v) : '—'
}

/** 仅站内相对路径可跳转，拒绝协议与协议相对 URL。 */
function safePagePath(raw?: string | null): string | null {
  const s = (raw ?? '').trim()
  if (!s || s.length > 256) return null
  if (!s.startsWith('/') || s.startsWith('//') || s.includes('://')) return null
  const path = s.split(/[?#]/, 1)[0]
  return path || null
}

function pageHref(row: ExportLogRow) {
  return safePagePath(row.pageUrl)
}

function buildParams() {
  const p: Record<string, string | number> = {
    page: page.value,
    pageSize: pageSize.value
  }
  if (filters.exportKind?.trim()) p.exportKind = filters.exportKind.trim()
  if (filters.operatorUserName?.trim()) p.operatorUserName = filters.operatorUserName.trim()
  if (dateRange.value?.length === 2) {
    p.operationTimeFrom = `${dateRange.value[0]}T00:00:00`
    p.operationTimeTo = `${dateRange.value[1]}T23:59:59`
  }
  return p
}

async function load() {
  loading.value = true
  try {
    const data = await exportLogsApi.list(buildParams())
    rows.value = data.items ?? []
    total.value = data.total ?? 0
  } catch (e: unknown) {
    ElMessage.error(e instanceof Error ? e.message : String(t('exportLog.loadFailed')))
  } finally {
    loading.value = false
  }
}

function onQuery() {
  page.value = 1
  void load()
}

function onSizeChange() {
  page.value = 1
  void load()
}

function reset() {
  filters.exportKind = ''
  filters.operatorUserName = ''
  dateRange.value = null
  page.value = 1
  pageSize.value = 20
  void load()
}

onMounted(() => {
  void exportLogsApi.kinds().then((list) => {
    kindOptions.value = list
  }).catch(() => {
    kindOptions.value = []
  })
  void load()
})
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.export-log-list-page {
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

.status-select {
  width: 200px;
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

.status-select--kind {
  width: 220px;
}

.filter-el-input {
  width: 160px;
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
  width: 280px;
  max-width: 100%;
  flex: 0 1 280px;
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
  &:hover:not(:disabled) {
    border-color: rgba(0, 212, 255, 0.3);
    color: $text-secondary;
  }
  &:disabled {
    opacity: 0.6;
    cursor: not-allowed;
  }
}

.export-log-list-page .table-wrapper {
  position: relative;
  :deep(.el-table .cell) {
    line-height: 1.35;
  }
}

.link-text {
  color: var(--el-color-primary);
  text-decoration: none;
  &:hover { text-decoration: underline; }
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
