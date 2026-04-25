<template>
  <!-- 业务列表页：结构对齐《业务页面规范》索引的《业务列表规范》《列表搜索栏规范》；表格皮肤见全局 crm-unified-list.scss -->
  <div class="login-log-list-page">
    <div class="page-header">
      <div class="header-left">
        <div class="page-title-group">
          <div class="page-icon" aria-hidden="true">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
              <path d="M15 3h4a2 2 0 012 2v14a2 2 0 01-2 2h-4M10 17l5-5-5-5M15 12H3" />
            </svg>
          </div>
          <h1 class="page-title">{{ t('loginLog.title') }}</h1>
        </div>
        <div class="count-badge">{{ t('loginLog.count', { count: total }) }}</div>
      </div>
    </div>

    <!-- 筛选：与 CustomerList / OperationLogList 同一套 .search-bar 结构（列表搜索栏规范） -->
    <div class="search-bar">
      <div class="search-left">
        <el-select
          v-model="filters.userId"
          clearable
          filterable
          :placeholder="t('loginLog.anyEmployee')"
          class="status-select status-select--user"
          :teleported="false"
          @change="onFilterChange"
        >
          <el-option v-for="u in userOptions" :key="u.id" :label="userOptionLabel(u)" :value="u.id" />
        </el-select>

        <el-date-picker
          v-model="timeRange"
          type="datetimerange"
          value-format="x"
          :range-separator="t('customerList.filters.to')"
          :start-placeholder="t('loginLog.timeFrom')"
          :end-placeholder="t('loginLog.timeTo')"
          clearable
          class="filter-date-range"
          :teleported="false"
          @change="onFilterChange"
        />

        <button type="button" class="btn-primary btn-sm" :disabled="loading" @click="onQuery">{{ t('loginLog.query') }}</button>
        <button type="button" class="btn-ghost btn-sm" :disabled="loading" @click="reset">{{ t('loginLog.reset') }}</button>
      </div>
    </div>

    <div class="table-wrapper" v-loading="loading">
      <CrmDataTable
        v-show="loading || rows.length > 0"
        ref="dataTableRef"
        row-key="id"
        column-layout-key="login-log-list-main"
        :columns="tableColumns"
        :show-column-settings="false"
        :density-toggle-anchor-el="rowDensityToggleAnchorEl"
        :data="rows"
      >
        <template #col-loginAt="{ row }">
          {{ formatLoginAt(row.loginAt) }}
        </template>
        <template #col-userName="{ row }">
          <span class="cell-login-user" :title="row.userName">{{ row.userName }}</span>
        </template>
        <template #col-loginMethod="{ row }">
          {{ loginMethodLabel(row.loginMethod) }}
        </template>
        <template #col-addressLine="{ row }">
          <span class="cell-login-addr" :title="(row.addressLine || '').trim() || undefined">
            {{ row.addressLine?.trim() || '—' }}
          </span>
        </template>
      </CrmDataTable>

      <div v-show="!loading && rows.length === 0" class="empty-state">
        <svg width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1" aria-hidden="true">
          <path d="M15 3h4a2 2 0 012 2v14a2 2 0 01-2 2h-4M10 17l5-5-5-5M15 12H3" />
        </svg>
        <p>{{ t('loginLog.empty') }}</p>
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
import { loginLogsApi, type LoginLogRow } from '@/api/loginLogs'
import { rbacAdminApi, type AdminUserDto } from '@/api/rbacAdmin'
import { formatDisplayDateTime } from '@/utils/displayDateTime'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'

const { t, locale } = useI18n()

const dataTableRef = ref<{ openColumnSettings?: () => void } | null>(null)
const rowDensityToggleAnchorEl = ref<HTMLElement | null>(null)

const loading = ref(false)
const rows = ref<LoginLogRow[]>([])
const total = ref(0)
const page = ref(1)
const pageSize = ref(20)
const timeRange = ref<[number, number] | null>(null)
const userOptions = ref<AdminUserDto[]>([])

const filters = reactive({
  userId: '' as string
})

const tableColumns = computed<CrmTableColumnDef[]>(() => {
  void locale.value
  return [
    { key: 'loginAt', label: t('loginLog.colLoginAt'), prop: 'loginAt', width: 168 },
    { key: 'userName', label: t('loginLog.colUserName'), prop: 'userName', width: 120, showOverflowTooltip: true },
    { key: 'clientIp', label: t('loginLog.colClientIp'), prop: 'clientIp', width: 140, showOverflowTooltip: true },
    { key: 'addressLine', label: t('loginLog.colAddress'), prop: 'addressLine', minWidth: 160, showOverflowTooltip: true },
    { key: 'loginMethod', label: t('loginLog.colLoginMethod'), prop: 'loginMethod', width: 120 }
  ]
})

function userOptionLabel(u: AdminUserDto) {
  const name = (u.realName || '').trim()
  return name ? `${name} (${u.userName})` : u.userName
}

function formatLoginAt(v?: string | null) {
  return v ? formatDisplayDateTime(v) : '—'
}

function loginMethodLabel(v: number) {
  if (v === 1) return t('loginLog.methodPassword')
  if (v === 2) return t('loginLog.methodImpersonate')
  if (v === 3) return t('loginLog.methodWechat')
  return '—'
}

function buildParams() {
  const p: Record<string, string | number> = {
    page: page.value,
    pageSize: pageSize.value
  }
  if (filters.userId?.trim()) p.userId = filters.userId.trim()
  if (timeRange.value?.length === 2) {
    p.loginAtFrom = new Date(timeRange.value[0]).toISOString()
    p.loginAtTo = new Date(timeRange.value[1]).toISOString()
  }
  return p
}

async function load() {
  loading.value = true
  try {
    const data = await loginLogsApi.list(buildParams())
    rows.value = data.items ?? []
    total.value = data.total ?? 0
  } catch (e: unknown) {
    ElMessage.error(e instanceof Error ? e.message : String(t('loginLog.loadFailed')))
  } finally {
    loading.value = false
  }
}

function onQuery() {
  page.value = 1
  load()
}

/** 列表搜索栏规范：下拉 / 日期区间变更即刷新列表 */
function onFilterChange() {
  page.value = 1
  load()
}

function onSizeChange() {
  page.value = 1
  load()
}

function reset() {
  filters.userId = ''
  timeRange.value = null
  page.value = 1
  pageSize.value = 20
  load()
}

onMounted(async () => {
  try {
    userOptions.value = await rbacAdminApi.getUsers()
  } catch {
    userOptions.value = []
  }
  load()
})
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';
// 表格皮肤：全局 main.scss 已引入 crm-unified-list.scss

.login-log-list-page {
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

.status-select--user {
  width: 200px;
  min-width: 200px;
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

.login-log-list-page .table-wrapper {
  position: relative;
  min-height: 200px;
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

/* 业务列表规范 §2.5：紧密下行高下列格内尽量单行展示 */
.login-log-list-page :deep(.crm-items-table--density-compact .cell-login-user),
.login-log-list-page :deep(.crm-items-table--density-compact .cell-login-addr) {
  display: block;
  min-width: 0;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}
</style>
