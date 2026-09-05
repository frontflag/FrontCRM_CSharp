<template>
  <div class="customer-quote-list-page">
    <div class="page-header">
      <div class="header-left">
        <div class="page-title-group">
          <div class="page-icon">客</div>
          <h1 class="page-title">{{ t('customerQuoteList.title') }}</h1>
        </div>
        <div class="count-badge">{{ t('customerQuoteList.count', { count: totalCount }) }}</div>
      </div>
      <div v-if="canWrite" class="header-right">
        <router-link to="/customer-quote-drafts" class="btn-ghost btn-sm">
          {{ t('customerQuoteList.openDrafts') }}
        </router-link>
      </div>
    </div>

    <div class="search-bar">
      <div class="search-left">
        <div class="search-input-wrap">
          <svg
            width="14"
            height="14"
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            stroke-width="2"
            class="search-icon"
            aria-hidden="true"
          >
            <circle cx="11" cy="11" r="8" />
            <line x1="21" y1="21" x2="16.65" y2="16.65" />
          </svg>
          <input
            v-model="keyword"
            type="search"
            class="search-input search-input--w280"
            :placeholder="t('customerQuoteList.keywordPlaceholder')"
            @keyup.enter="handleSearch"
          />
        </div>
        <el-select
          v-model="statusFilter"
          clearable
          class="status-select status-select--customer-quote"
          :placeholder="t('customerQuoteList.allStatus')"
          :teleported="false"
        >
          <el-option :label="t('customerQuoteList.statusUnsent')" :value="0" />
          <el-option :label="t('customerQuoteList.statusSent')" :value="1" />
          <el-option :label="t('customerQuoteList.statusVoid')" :value="2" />
        </el-select>
        <button type="button" class="btn-primary btn-sm" @click="handleSearch">
          <el-icon><Search /></el-icon>{{ t('customerQuoteList.query') }}
        </button>
        <button type="button" class="btn-ghost btn-sm" @click="handleReset">
          {{ t('customerQuoteList.reset') }}
        </button>
      </div>
    </div>

    <div class="table-wrapper customer-quote-list-table-scroll" v-loading="loading">
      <CrmDataTable
        ref="dataTableRef"
        column-layout-key="customer-quote-list-v2"
        :columns="tableColumns"
        :show-column-settings="false"
        :density-toggle-anchor-el="rowDensityToggleAnchorEl"
        :data="rows"
        row-key="id"
        highlight-current-row
        @row-dblclick="openEdit"
      >
        <template #col-displayCode="{ row }">
          <span class="quote-code-cell">{{ displayCode(row) }}</span>
        </template>
        <template #col-status="{ row }">
          <el-tag effect="dark" size="small" :type="statusTagType(row.status)">
            {{ statusText(row.status) }}
          </el-tag>
        </template>
        <template #col-createTime="{ row }">
          <template v-for="p in [formatDateTimeParts(row.createTime)]" :key="`ct-${row.id}`">
            <span v-if="p" class="crm-quote-create-time">
              <span class="crm-quote-create-time__ymd">{{ p.date }}</span>
              <span class="crm-quote-create-time__hm">{{ p.time }}</span>
            </span>
            <span v-else>—</span>
          </template>
        </template>
        <template #col-profitFactor="{ row }">
          {{ Number(row.profitFactor ?? 1).toFixed(2) }}
        </template>
        <template #col-actions="{ row }">
          <div class="action-btns" @click.stop @dblclick.stop>
            <el-button class="action-btn action-btn--primary" link type="primary" size="small" @click.stop="openEdit(row)">
              {{ canEdit(row) ? t('common.edit') : t('common.view') }}
            </el-button>
          </div>
        </template>
      </CrmDataTable>
    </div>

    <div v-if="pageInfo.total > 0" class="pagination-wrapper">
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
        v-model:current-page="pageInfo.page"
        v-model:page-size="pageInfo.pageSize"
        class="quantum-pagination"
        :total="pageInfo.total"
        :page-sizes="[10, 20, 50, 100]"
        layout="total, sizes, prev, pager, next, jumper"
        @size-change="handleSizeChange"
        @current-change="handlePageChange"
      />
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import { Search, Setting } from '@element-plus/icons-vue'
import CrmDataTable from '@/components/CrmDataTable.vue'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'
import { customerQuoteApi, type CustomerQuoteRow } from '@/api/customerQuote'
import { useAuthStore } from '@/stores/auth'
import { formatDisplayDateTime2DigitYearParts } from '@/utils/displayDateTime'
import { getApiErrorMessage } from '@/utils/apiError'

const { t } = useI18n()
const router = useRouter()
const authStore = useAuthStore()
const canWrite = computed(() => authStore.hasPermission('customer-quote.write'))

const loading = ref(false)
const rows = ref<CustomerQuoteRow[]>([])
const keyword = ref('')
const statusFilter = ref<number | undefined>(undefined)
const pageInfo = ref({ page: 1, pageSize: 20, total: 0 })
const totalCount = computed(() => pageInfo.value.total)
const dataTableRef = ref<InstanceType<typeof CrmDataTable> | null>(null)
const rowDensityToggleAnchorEl = ref<HTMLElement | null>(null)

const tableColumns = computed((): CrmTableColumnDef[] => [
  { key: 'displayCode', label: t('customerQuoteList.colCode'), minWidth: 140 },
  { key: 'status', label: t('customerQuoteList.colStatus'), width: 100 },
  { key: 'customerName', label: t('customerQuoteList.colCustomer'), minWidth: 140, prop: 'customerName' },
  { key: 'contactName', label: t('customerQuoteList.colContact'), width: 100, prop: 'contactName' },
  { key: 'salesUserName', label: t('customerQuoteList.colSales'), width: 100, prop: 'salesUserName' },
  { key: 'profitFactor', label: t('customerQuoteList.colProfitFactor'), width: 100, align: 'right' },
  { key: 'createTime', label: t('customerQuoteList.colCreateTime'), width: 140 },
  {
    key: 'actions',
    label: t('customerQuoteList.colActions'),
    width: 88,
    fixed: 'right',
    className: 'op-col',
    labelClassName: 'op-col'
  }
])

function displayCode(row: CustomerQuoteRow) {
  return row.displayCode || `${row.customerQuoteCode}-${row.versionNo}`
}

function statusText(status: number) {
  if (status === 1) return t('customerQuoteList.statusSent')
  if (status === 2) return t('customerQuoteList.statusVoid')
  return t('customerQuoteList.statusUnsent')
}

function statusTagType(status: number) {
  if (status === 1) return 'success'
  if (status === 2) return 'info'
  return 'warning'
}

function canEdit(row: CustomerQuoteRow) {
  return canWrite.value && row.status === 0
}

function formatDateTimeParts(v?: string | null) {
  if (!v) return null
  return formatDisplayDateTime2DigitYearParts(v)
}

async function loadData() {
  loading.value = true
  try {
    const res = await customerQuoteApi.getQuotes({
      page: pageInfo.value.page,
      pageSize: pageInfo.value.pageSize,
      keyword: keyword.value.trim() || undefined,
      status: statusFilter.value
    })
    rows.value = res.items || []
    pageInfo.value.total = res.total || 0
    const maxPage = Math.max(1, Math.ceil(pageInfo.value.total / pageInfo.value.pageSize) || 1)
    if (pageInfo.value.page > maxPage) {
      pageInfo.value.page = maxPage
      return loadData()
    }
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, t('customerQuoteList.loadFailed')))
  } finally {
    loading.value = false
  }
}

function handleSearch() {
  pageInfo.value.page = 1
  void loadData()
}

function handleReset() {
  keyword.value = ''
  statusFilter.value = undefined
  pageInfo.value.page = 1
  void loadData()
}

function handleSizeChange(val: number) {
  pageInfo.value.pageSize = val
  pageInfo.value.page = 1
  void loadData()
}

function handlePageChange(val: number) {
  pageInfo.value.page = val
  void loadData()
}

function openEdit(row: CustomerQuoteRow) {
  void router.push({ name: 'CustomerQuoteEdit', params: { id: row.id } })
}

onMounted(() => {
  void loadData()
})
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.customer-quote-list-page {
  display: flex;
  flex-direction: column;
  height: 100%;
  min-height: 0;
  padding: 24px;
  box-sizing: border-box;
  background: $layer-1;
  font-family: 'Noto Sans SC', sans-serif;
}

.page-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 20px;
  gap: 12px;
  flex-wrap: wrap;

  .header-left,
  .header-right {
    display: flex;
    align-items: center;
    gap: 12px;
  }

  .page-title {
    margin: 0;
    color: $text-primary;
    font-size: 20px;
    font-weight: 600;
    letter-spacing: 0.5px;
  }

  .count-badge {
    padding: 3px 10px;
    background: rgba(255, 255, 255, 0.05);
    border: 1px solid $border-panel;
    border-radius: 20px;
    font-size: 12px;
    color: $text-muted;
  }
}

.page-title-group {
  display: flex;
  align-items: center;
  gap: 10px;

  .page-icon {
    width: 36px;
    height: 36px;
    border-radius: 10px;
    display: flex;
    align-items: center;
    justify-content: center;
    background: rgba(0, 212, 255, 0.1);
    border: 1px solid rgba(0, 212, 255, 0.25);
    color: $cyan-primary;
    font-weight: 700;
    font-size: 15px;
  }
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

  &::placeholder {
    color: $text-muted;
  }

  &:focus {
    border-color: rgba(0, 212, 255, 0.4);
  }
}

.search-input--w280 {
  width: 280px;
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

  &:disabled {
    opacity: 0.45;
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
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  text-decoration: none;
  transition: all 0.2s;

  &:hover {
    border-color: rgba(0, 212, 255, 0.3);
    color: $text-secondary;
  }

  &.btn-sm {
    padding: 6px 12px;
    font-size: 12px;
  }
}

.table-wrapper {
  flex: 1;
  min-height: 120px;
  min-height: 0;
  overflow: auto;
}

.quote-code-cell {
  color: $text-primary !important;
}

.pagination-wrapper {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 12px 16px;
  flex-wrap: wrap;
  margin-top: 12px;
}

.list-footer-left {
  display: inline-flex;
  align-items: flex-start;
  gap: 6px;
  flex-shrink: 0;
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

.pagination-wrapper .quantum-pagination {
  margin-left: auto;
  align-self: flex-start;

  :deep(.el-pagination__total) {
    color: $text-muted;
  }
}
</style>
