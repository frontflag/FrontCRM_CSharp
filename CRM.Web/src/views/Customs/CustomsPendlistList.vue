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

    <el-table :data="list" v-loading="loading" stripe border class="data-table">
      <el-table-column prop="salesStockOutNotifyCode" :label="t('customsPages.pendlists.colSalesSor')" min-width="140" />
      <el-table-column prop="salesOrderCode" :label="t('customsPages.pendlists.colSalesOrder')" min-width="120" />
      <el-table-column prop="sellOrderItemCode" :label="t('customsPages.pendlists.colSoLine')" min-width="120" />
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
      <el-table-column :label="t('customsPages.pendlists.colActions')" width="180" fixed="right">
        <template #default="{ row }">
          <el-button
            v-if="canWriteLogisticsData && row.status === CUSTOMS_PENDLIST_STATUS.Open"
            link
            type="primary"
            :loading="creatingId === row.id"
            @click.stop="onCreateCustomsOutNotify(row)"
          >
            {{ t('customsPages.pendlists.createCustomsOutNotify') }}
          </el-button>
        </template>
      </el-table-column>
    </el-table>
  </div>
</template>

<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage, ElMessageBox } from 'element-plus'
import {
  CUSTOMS_PENDLIST_STATUS,
  createCustomsOutNotifyFromPendlist,
  fetchCustomsPendlists,
  type CustomsPendlistListItemDto
} from '@/api/customs'
import { useDepartmentDataReadOnly } from '@/composables/useDepartmentDataReadOnly'

const { t } = useI18n()
const { canWriteLogisticsData } = useDepartmentDataReadOnly()
const loading = ref(false)
const creatingId = ref('')
const list = ref<CustomsPendlistListItemDto[]>([])
const filters = reactive<{ status?: number; keyword: string }>({
  status: CUSTOMS_PENDLIST_STATUS.Open,
  keyword: ''
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
  } catch (e: unknown) {
    ElMessage.error(e instanceof Error ? e.message : t('customsPages.pendlists.createFailed'))
  } finally {
    creatingId.value = ''
  }
}

onMounted(() => {
  void loadList()
})
</script>

<style scoped>
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
</style>
