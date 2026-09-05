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
      <div class="header-right">
        <router-link v-if="canWrite" to="/customer-quote-drafts" class="btn-ghost btn-sm">
          {{ t('customerQuoteList.openDrafts') }}
        </router-link>
      </div>
    </div>

    <div class="search-bar">
      <div class="search-input-wrap">
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
        class="status-select"
        :placeholder="t('customerQuoteList.allStatus')"
        :teleported="false"
      >
        <el-option :label="t('customerQuoteList.statusUnsent')" :value="0" />
        <el-option :label="t('customerQuoteList.statusSent')" :value="1" />
        <el-option :label="t('customerQuoteList.statusVoid')" :value="2" />
      </el-select>
      <button type="button" class="btn-primary btn-sm" @click="handleSearch">{{ t('customerQuoteList.query') }}</button>
      <button type="button" class="btn-ghost btn-sm" @click="handleReset">{{ t('customerQuoteList.reset') }}</button>
    </div>

    <div class="table-wrapper" v-loading="loading">
      <CrmDataTable
        column-layout-key="customer-quote-list-v1"
        :columns="tableColumns"
        :show-column-settings="false"
        :data="rows"
        row-key="id"
        highlight-current-row
        @row-dblclick="openEdit"
      >
        <template #col-displayCode="{ row }">
          <span class="code-cell">{{ displayCode(row) }}</span>
        </template>
        <template #col-status="{ row }">
          <el-tag effect="dark" size="small" :type="statusTagType(row.status)">
            {{ statusText(row.status) }}
          </el-tag>
        </template>
        <template #col-createTime="{ row }">
          {{ formatDate(row.createTime) }}
        </template>
        <template #col-profitFactor="{ row }">
          {{ Number(row.profitFactor ?? 1).toFixed(2) }}
        </template>
        <template #col-actions="{ row }">
          <el-button link type="primary" size="small" @click.stop="openEdit(row)">
            {{ canEdit(row) ? t('common.edit') : t('common.view') }}
          </el-button>
        </template>
      </CrmDataTable>
    </div>

    <div class="pagination-bar">
      <el-pagination
        v-model:current-page="pageInfo.page"
        v-model:page-size="pageInfo.pageSize"
        :total="pageInfo.total"
        :page-sizes="[20, 50, 100]"
        layout="total, sizes, prev, pager, next"
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

const tableColumns = computed((): CrmTableColumnDef[] => [
  { key: 'displayCode', label: t('customerQuoteList.colCode'), minWidth: 140 },
  { key: 'status', label: t('customerQuoteList.colStatus'), width: 100 },
  { key: 'customerName', label: t('customerQuoteList.colCustomer'), minWidth: 140, prop: 'customerName' },
  { key: 'contactName', label: t('customerQuoteList.colContact'), width: 100, prop: 'contactName' },
  { key: 'salesUserName', label: t('customerQuoteList.colSales'), width: 100, prop: 'salesUserName' },
  { key: 'profitFactor', label: t('customerQuoteList.colProfitFactor'), width: 100, align: 'right' },
  { key: 'createTime', label: t('customerQuoteList.colCreateTime'), width: 120 },
  { key: 'actions', label: t('customerQuoteList.colActions'), width: 80, fixed: 'right' }
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

function formatDate(v?: string | null) {
  if (!v) return '—'
  const p = formatDisplayDateTime2DigitYearParts(v)
  return p ? p.date : '—'
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
}

.page-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 16px;
}

.header-left,
.header-right {
  display: flex;
  align-items: center;
  gap: 12px;
}

.page-title-group {
  display: flex;
  align-items: center;
  gap: 10px;
}

.page-icon {
  width: 36px;
  height: 36px;
  border-radius: 8px;
  background: #fff7e6;
  color: #fa8c16;
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: 600;
}

.search-bar {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-bottom: 12px;
}

.table-wrapper {
  flex: 1;
  min-height: 0;
  overflow: auto;
}

.pagination-bar {
  display: flex;
  justify-content: flex-end;
  padding-top: 12px;
}

.code-cell {
  font-weight: 500;
}
</style>
