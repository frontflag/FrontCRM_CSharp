<template>
  <!-- 业务列表页：对齐《业务列表规范》《列表搜索栏规范》 -->
  <div class="system-error-list-page">
    <div class="page-header">
      <div class="header-left">
        <div class="page-title-group">
          <div class="page-icon" aria-hidden="true">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
              <circle cx="12" cy="12" r="10" />
              <line x1="12" y1="8" x2="12" y2="12" />
              <line x1="12" y1="16" x2="12.01" y2="16" />
            </svg>
          </div>
          <h1 class="page-title">{{ t('systemError.title') }}</h1>
        </div>
        <div class="count-badge">{{ t('systemError.count', { count: total }) }}</div>
      </div>
    </div>

    <div class="search-bar">
      <div class="search-left">
        <el-select
          v-model="resolvedFilter"
          clearable
          :placeholder="t('systemError.filterResolved')"
          class="status-select"
          :teleported="false"
          @change="onFilterChange"
        >
          <el-option :label="t('systemError.unresolved')" value="open" />
          <el-option :label="t('systemError.resolved')" value="resolved" />
          <el-option :label="t('systemError.ignore')" value="ignored" />
        </el-select>
        <el-input
          v-model="filters.keyword"
          clearable
          :placeholder="t('systemError.keywordPh')"
          class="keyword-input"
          @keyup.enter="onQuery"
        />
        <button type="button" class="btn-primary btn-sm" :disabled="loading" @click="onQuery">
          {{ t('systemError.query') }}
        </button>
        <button type="button" class="btn-ghost btn-sm" :disabled="loading" @click="reset">
          {{ t('systemError.reset') }}
        </button>
      </div>
    </div>

    <div class="table-wrapper" v-loading="loading">
      <CrmDataTable
        v-show="loading || rows.length > 0"
        ref="dataTableRef"
        row-key="id"
        column-layout-key="ops-system-error-list-main"
        :columns="tableColumns"
        :show-column-settings="false"
        :density-toggle-anchor-el="rowDensityToggleAnchorEl"
        :data="rows"
        row-class-name="table-row-pointer"
        @row-dblclick="openDetail"
      >
        <template #col-errorId="{ row }">
          <span class="code-text">{{ row.errorId }}</span>
        </template>
        <template #col-occurredAt="{ row }">
          {{ formatTime(row.occurredAt) }}
        </template>
        <template #col-errorMessage="{ row }">
          <span class="cell-ellipsis" :title="row.errorMessage">{{ row.errorMessage }}</span>
        </template>
        <template #col-requestPath="{ row }">
          <span class="cell-ellipsis" :title="row.requestPath || undefined">{{ row.requestPath || '—' }}</span>
        </template>
        <template #col-userName="{ row }">
          <span class="cell-ellipsis" :title="row.userName || undefined">{{ row.userName || '—' }}</span>
        </template>
        <template #col-resolveRemark="{ row }">
          <span class="cell-ellipsis" :title="row.resolveRemark || undefined">{{ row.resolveRemark || '—' }}</span>
        </template>
        <template #col-isResolved="{ row }">
          <el-tag :type="statusTagType(row)" size="small" effect="plain">
            {{ statusLabel(row) }}
          </el-tag>
        </template>
      </CrmDataTable>

      <div v-show="!loading && rows.length === 0" class="empty-state">
        <p>{{ t('systemError.empty') }}</p>
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

    <el-drawer v-model="detailOpen" :title="t('systemError.detailTitle')" size="560px" append-to-body>
      <div v-if="detail" class="detail-body" v-loading="detailLoading">
        <div class="detail-meta">
          <span class="detail-kv-label">{{ t('systemError.colErrorId') }}：</span>
          <span class="detail-kv-value code-text">{{ detail.errorId }}</span>
          <span class="detail-kv-label">{{ t('systemError.colTime') }}：</span>
          <span class="detail-kv-value">{{ formatTime(detail.occurredAt) }}</span>
          <span class="detail-kv-label">{{ t('systemError.colModule') }}：</span>
          <span class="detail-kv-value">{{ detail.moduleName }}</span>
          <span class="detail-kv-label">{{ t('systemError.colPath') }}：</span>
          <span class="detail-kv-value">{{ detail.requestPath || '—' }}</span>
          <span class="detail-kv-label">{{ t('systemError.colUser') }}：</span>
          <span class="detail-kv-value">{{ detail.userName || '—' }}</span>
        </div>

        <div class="detail-blocks">
          <div class="detail-block">
            <div class="detail-block-label">{{ t('systemError.colMessage') }}：</div>
            <div class="detail-block-value">{{ detail.errorMessage }}</div>
          </div>
          <div class="detail-block">
            <div class="detail-block-header">
              <div class="detail-block-label">{{ t('systemError.colDetail') }}：</div>
              <button
                type="button"
                class="detail-copy-btn"
                :disabled="!detail.errorDetail"
                @click="copyErrorDetail"
              >
                {{ t('common.copy') }}
              </button>
            </div>
            <pre class="detail-pre detail-block-value">{{ detail.errorDetail || '—' }}</pre>
          </div>
        </div>

        <div v-if="!detail.isResolved && canResolve" class="detail-actions">
          <el-input v-model="resolveRemark" type="textarea" :rows="2" :placeholder="t('systemError.resolveRemarkPh')" />
          <div class="detail-actions-footer">
            <el-button :loading="ignoring" :disabled="resolving" @click="ignoreError">
              {{ t('systemError.ignore') }}
            </el-button>
            <el-button type="primary" :loading="resolving" :disabled="ignoring" @click="resolve">
              {{ t('systemError.markResolved') }}
            </el-button>
          </div>
        </div>
        <div v-else-if="detail.isResolved && isIgnoredDetail(detail)" class="resolved-hint">
          {{ t('systemError.alreadyIgnored') }}
        </div>
        <div v-else-if="detail.isResolved" class="resolved-hint">
          {{ t('systemError.alreadyResolved') }}
          <span v-if="detail.resolveRemark"> — {{ detail.resolveRemark }}</span>
        </div>
      </div>
    </el-drawer>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import { Setting } from '@element-plus/icons-vue'
import CrmDataTable from '@/components/CrmDataTable.vue'
import {
  errorLogsApi,
  ERROR_LOG_IGNORE_REMARK,
  resolveErrorLogStatus,
  type ErrorLogDetail,
  type ErrorLogListItem,
  type ErrorLogStatus
} from '@/api/errorLogs'
import { getApiErrorMessage } from '@/utils/apiError'
import { copyTextToClipboard } from '@/utils/clipboard'
import { formatDisplayDateTime } from '@/utils/displayDateTime'
import { useAuthStore } from '@/stores'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'

const { t, locale } = useI18n()
const authStore = useAuthStore()

const dataTableRef = ref<{ openColumnSettings?: () => void } | null>(null)
const rowDensityToggleAnchorEl = ref<HTMLElement | null>(null)

const loading = ref(false)
const rows = ref<ErrorLogListItem[]>([])
const total = ref(0)
const page = ref(1)
const pageSize = ref(20)
const resolvedFilter = ref<string | undefined>('open')
const filters = reactive({ keyword: '' })

const detailOpen = ref(false)
const detailLoading = ref(false)
const resolving = ref(false)
const ignoring = ref(false)
const detail = ref<ErrorLogDetail | null>(null)
const resolveRemark = ref('')

const canResolve = computed(
  () => authStore.hasPermission('sys.errorlog.resolve') || authStore.user?.isSysAdmin === true
)

const tableColumns = computed<CrmTableColumnDef[]>(() => {
  void locale.value
  return [
    { key: 'errorId', label: t('systemError.colErrorId'), prop: 'errorId', width: 132 },
    { key: 'occurredAt', label: t('systemError.colTime'), prop: 'occurredAt', width: 168 },
    { key: 'moduleName', label: t('systemError.colModule'), prop: 'moduleName', width: 120 },
    { key: 'errorMessage', label: t('systemError.colMessage'), prop: 'errorMessage', minWidth: 220, showOverflowTooltip: true },
    { key: 'requestPath', label: t('systemError.colPath'), prop: 'requestPath', minWidth: 160, showOverflowTooltip: true },
    { key: 'userName', label: t('systemError.colUser'), prop: 'userName', width: 110, showOverflowTooltip: true },
    { key: 'resolveRemark', label: t('systemError.colResolveRemark'), prop: 'resolveRemark', minWidth: 140, showOverflowTooltip: true },
    { key: 'isResolved', label: t('systemError.colStatus'), prop: 'isResolved', width: 100, align: 'center' }
  ]
})

function statusOf(row: ErrorLogListItem | ErrorLogDetail): ErrorLogStatus {
  return resolveErrorLogStatus(row)
}

function isIgnoredDetail(row: ErrorLogDetail) {
  return statusOf(row) === 'ignored'
}

function statusLabel(row: ErrorLogListItem | ErrorLogDetail) {
  const status = statusOf(row)
  if (status === 'ignored') return t('systemError.ignore')
  if (status === 'resolved') return t('systemError.resolved')
  return t('systemError.unresolved')
}

function statusTagType(row: ErrorLogListItem | ErrorLogDetail): 'success' | 'warning' | 'danger' {
  const status = statusOf(row)
  if (status === 'ignored') return 'warning'
  if (status === 'resolved') return 'success'
  return 'danger'
}

function formatTime(v?: string | null) {
  return v ? formatDisplayDateTime(v) : '—'
}

function statusQueryValue(): ErrorLogStatus | undefined {
  if (resolvedFilter.value === 'open') return 'open'
  if (resolvedFilter.value === 'resolved') return 'resolved'
  if (resolvedFilter.value === 'ignored') return 'ignored'
  return undefined
}

function onFilterChange() {
  page.value = 1
  void load()
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
  filters.keyword = ''
  resolvedFilter.value = 'open'
  page.value = 1
  pageSize.value = 20
  void load()
}

async function load() {
  loading.value = true
  try {
    const res = await errorLogsApi.list({
      keyword: filters.keyword?.trim() || undefined,
      status: statusQueryValue(),
      page: page.value,
      pageSize: pageSize.value
    })
    rows.value = res.items || []
    total.value = res.total || 0
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('systemError.loadFailed')))
  } finally {
    loading.value = false
  }
}

async function openDetail(row: ErrorLogListItem) {
  detailOpen.value = true
  detailLoading.value = true
  detail.value = null
  resolveRemark.value = ''
  try {
    detail.value = await errorLogsApi.detail(row.id)
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('systemError.loadFailed')))
    detailOpen.value = false
  } finally {
    detailLoading.value = false
  }
}

async function copyErrorDetail() {
  const text = detail.value?.errorDetail?.trim()
  if (!text) return

  if (copyTextToClipboard(text)) {
    ElMessage.success(t('common.copySuccess'))
    return
  }
  if (typeof navigator !== 'undefined' && navigator.clipboard?.writeText) {
    try {
      await navigator.clipboard.writeText(text)
      ElMessage.success(t('common.copySuccess'))
      return
    } catch {
      /* fall through */
    }
  }
  ElMessage.error(t('common.copyFailed'))
}

async function ignoreError() {
  if (!detail.value) return
  ignoring.value = true
  try {
    await errorLogsApi.resolve(detail.value.id, ERROR_LOG_IGNORE_REMARK)
    ElMessage.success(t('systemError.ignoreOk'))
    detail.value = await errorLogsApi.detail(detail.value.id)
    void load()
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('systemError.ignoreFailed')))
  } finally {
    ignoring.value = false
  }
}

async function resolve() {
  if (!detail.value) return
  resolving.value = true
  try {
    await errorLogsApi.resolve(detail.value.id, resolveRemark.value.trim() || undefined)
    ElMessage.success(t('systemError.resolveOk'))
    detail.value = await errorLogsApi.detail(detail.value.id)
    void load()
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('systemError.resolveFailed')))
  } finally {
    resolving.value = false
  }
}

onMounted(() => {
  void load()
})
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.system-error-list-page {
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
  margin-bottom: 12px;
}

.search-left {
  display: flex;
  align-items: center;
  gap: 10px;
  flex-wrap: wrap;
}

.status-select {
  width: 140px;
  :deep(.el-select__wrapper) {
    background: $layer-2 !important;
    box-shadow: none !important;
    border: 1px solid $border-panel !important;
    border-radius: $border-radius-md !important;
  }
}

.keyword-input {
  width: 240px;
  :deep(.el-input__wrapper) {
    background: $layer-2 !important;
    box-shadow: none !important;
    border: 1px solid $border-panel !important;
    border-radius: $border-radius-md !important;
  }
}

.btn-primary {
  display: inline-flex;
  align-items: center;
  padding: 6px 12px;
  background: linear-gradient(135deg, rgba(0, 102, 255, 0.8), rgba(0, 212, 255, 0.7));
  border: 1px solid rgba(0, 212, 255, 0.4);
  border-radius: $border-radius-md;
  color: #fff;
  font-size: 12px;
  cursor: pointer;
  &:disabled {
    opacity: 0.6;
    cursor: not-allowed;
  }
}

.btn-ghost {
  display: inline-flex;
  align-items: center;
  padding: 6px 12px;
  background: transparent;
  border: 1px solid $border-panel;
  border-radius: $border-radius-md;
  color: $text-muted;
  font-size: 12px;
  cursor: pointer;
}

.table-wrapper {
  position: relative;
  min-height: 200px;
  :deep(.table-row-pointer) {
    cursor: pointer;
  }
}

.cell-ellipsis {
  display: block;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.code-text {
  font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace;
  font-size: 12px;
}

.empty-state {
  padding: 48px;
  text-align: center;
  color: $text-muted;
}

.pagination-wrapper {
  margin-top: 12px;
  display: flex;
  align-items: flex-start;
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
  min-width: 0;
}

.list-footer-spacer {
  width: 26px;
  flex: 0 0 26px;
}

.detail-pre {
  margin: 0;
  white-space: pre-wrap;
  word-break: break-word;
  font-size: 12px;
  max-height: 280px;
  overflow: auto;
}

.detail-meta {
  display: grid;
  grid-template-columns: max-content 1fr;
  column-gap: 12px;
  row-gap: 10px;
  align-items: start;
  padding: 12px 14px;
  border: 1px solid $border-panel;
  border-radius: $border-radius-md;
  background: rgba(255, 255, 255, 0.02);
  font-size: 13px;
  line-height: 1.5;
}

.detail-kv-label {
  color: $text-muted;
  white-space: nowrap;
  text-align: left;
}

.detail-kv-value {
  min-width: 0;
  color: $text-primary;
  word-break: break-word;
  text-align: left;
}

.detail-blocks {
  margin-top: 16px;
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.detail-block {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.detail-block-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
}

.detail-copy-btn {
  flex: 0 0 auto;
  padding: 2px 10px;
  border: 1px solid $border-panel;
  border-radius: $border-radius-md;
  background: rgba(255, 255, 255, 0.04);
  color: $text-muted;
  font-size: 12px;
  line-height: 1.5;
  cursor: pointer;

  &:hover:not(:disabled) {
    color: $text-primary;
    border-color: rgba(0, 212, 255, 0.35);
  }

  &:disabled {
    opacity: 0.45;
    cursor: not-allowed;
  }
}

.detail-block-label {
  font-size: 13px;
  color: $text-muted;
  line-height: 1.5;
}

.detail-block-value {
  font-size: 13px;
  color: $text-primary;
  line-height: 1.6;
  word-break: break-word;
  padding: 10px 12px;
  border-radius: $border-radius-md;
  background: #fef9e7;
  border: 1px solid rgba(234, 179, 8, 0.22);
}

.detail-actions {
  margin-top: 16px;
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.detail-actions-footer {
  display: flex;
  align-items: center;
  justify-content: space-between;
  width: 100%;
}

.resolved-hint {
  margin-top: 16px;
  font-size: 13px;
  color: var(--el-text-color-secondary);
}
</style>
