<template>
  <div class="customs-page">
    <div class="page-header">
      <h1 class="page-title">{{ t('customsPages.transfers.title') }}</h1>
    </div>
    <el-form :inline="true" class="filter-form" @submit.prevent>
      <el-form-item :label="t('customsPages.transfers.filterStatus')">
        <el-select v-model="filters.status" clearable style="width: 140px">
          <el-option :label="t('customsPages.transfers.statusConfirmed')" :value="2" />
        </el-select>
      </el-form-item>
      <el-form-item :label="t('customsPages.transfers.filterPending')">
        <el-checkbox v-model="filters.pendingOnly">{{ t('customsPages.transfers.pendingOnly') }}</el-checkbox>
      </el-form-item>
      <el-form-item :label="t('customsPages.transfers.filterConfirmedRange')">
        <el-date-picker
          v-model="filters.confirmedRange"
          type="daterange"
          value-format="YYYY-MM-DD"
          unlink-panels
          style="width: 260px"
        />
      </el-form-item>
      <el-form-item :label="t('customsPages.transfers.filterDecCode')">
        <el-input v-model="filters.declarationCode" clearable style="width: 160px" />
      </el-form-item>
      <el-form-item :label="t('customsPages.transfers.filterFromWh')">
        <el-input v-model="filters.fromWarehouseId" clearable style="width: 160px" />
      </el-form-item>
      <el-form-item :label="t('customsPages.transfers.filterToWh')">
        <el-input v-model="filters.toWarehouseId" clearable style="width: 160px" />
      </el-form-item>
      <el-form-item>
        <el-button type="primary" @click="searchTransfers">{{ t('customsPages.declarations.search') }}</el-button>
      </el-form-item>
    </el-form>

    <el-table :data="list" v-loading="loading" stripe border class="data-table" @row-dblclick="onDblClick">
      <el-table-column :label="t('customsPages.transfers.colStatus')" width="100">
        <template #default="{ row }">{{ transferStatusLabel(row) }}</template>
      </el-table-column>
      <el-table-column prop="confirmedTime" :label="t('customsPages.transfers.colConfirmed')" width="170">
        <template #default="{ row }">{{ row.confirmedTime ? fmtDt(row.confirmedTime) : '—' }}</template>
      </el-table-column>
      <el-table-column prop="declarationCode" :label="t('customsPages.transfers.colDecCode')" width="130" />
      <el-table-column prop="fromWarehouseName" :label="t('customsPages.transfers.colFrom')" min-width="120" show-overflow-tooltip>
        <template #default="{ row }">{{ row.fromWarehouseName || row.fromWarehouseId }}</template>
      </el-table-column>
      <el-table-column prop="toWarehouseName" :label="t('customsPages.transfers.colTo')" min-width="120" show-overflow-tooltip>
        <template #default="{ row }">{{ row.toWarehouseName || row.toWarehouseId }}</template>
      </el-table-column>
      <el-table-column prop="transferCode" :label="t('customsPages.transfers.colCode')" width="140" />
      <el-table-column prop="createTime" :label="t('customsPages.transfers.colCreateTime')" width="170">
        <template #default="{ row }">{{ fmtDt(row.createTime) }}</template>
      </el-table-column>
      <el-table-column prop="createUserDisplay" :label="t('customsPages.transfers.colCreator')" width="110" />
      <el-table-column :label="t('customsPages.transfers.colActions')" width="120" fixed="right">
        <template #default="{ row }">
          <el-button link type="primary" :disabled="row.isConfirmed" @click.stop="confirmOne(row)">
            {{ t('customsPages.transfers.confirmBtn') }}
          </el-button>
        </template>
      </el-table-column>
    </el-table>
    <div class="pagination-bar">
      <el-pagination
        v-model:current-page="transferPage"
        v-model:page-size="transferPageSize"
        :total="listTotal"
        :page-sizes="[10, 20, 50, 100]"
        layout="total, sizes, prev, pager, next, jumper"
        @size-change="onTransferPageSizeChange"
        @current-change="loadTransfersPage"
      />
    </div>

    <el-dialog v-model="detailVisible" :title="t('customsPages.transfers.detailTitle')" width="560px" destroy-on-close>
      <el-descriptions v-if="detailRow" :column="1" border>
        <el-descriptions-item :label="t('customsPages.transfers.colCode')">{{ detailRow.transferCode }}</el-descriptions-item>
        <el-descriptions-item :label="t('customsPages.transfers.colDecCode')">{{ detailRow.declarationCode }}</el-descriptions-item>
        <el-descriptions-item :label="t('customsPages.transfers.colFrom')">{{ detailRow.fromWarehouseName || detailRow.fromWarehouseId }}</el-descriptions-item>
        <el-descriptions-item :label="t('customsPages.transfers.colTo')">{{ detailRow.toWarehouseName || detailRow.toWarehouseId }}</el-descriptions-item>
        <el-descriptions-item :label="t('customsPages.transfers.colStatus')">{{ transferStatusLabel(detailRow) }}</el-descriptions-item>
        <el-descriptions-item :label="t('customsPages.transfers.colConfirmed')">{{ detailRow.confirmedTime ? fmtDt(detailRow.confirmedTime) : '—' }}</el-descriptions-item>
      </el-descriptions>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage, ElMessageBox } from 'element-plus'
import { confirmStockTransfer, fetchStockTransfers, type StockTransferListItemDto } from '@/api/customs'

const { t } = useI18n()
const loading = ref(false)
const list = ref<StockTransferListItemDto[]>([])
const listTotal = ref(0)
const transferPage = ref(1)
const transferPageSize = ref(20)
const filters = reactive<{
  status?: number
  pendingOnly: boolean
  confirmedRange: string[] | null
  declarationCode: string
  fromWarehouseId: string
  toWarehouseId: string
}>({
  pendingOnly: false,
  confirmedRange: null,
  declarationCode: '',
  fromWarehouseId: '',
  toWarehouseId: ''
})

const detailVisible = ref(false)
const detailRow = ref<StockTransferListItemDto | null>(null)

function fmtDt(iso: string) {
  return iso ? iso.replace('T', ' ').slice(0, 19) : '—'
}

function transferStatusLabel(row: StockTransferListItemDto) {
  return row.isConfirmed ? t('customsPages.transfers.statusConfirmed') : t('customsPages.transfers.statusPending')
}

async function loadTransfers(resetPage: boolean) {
  if (resetPage) transferPage.value = 1
  loading.value = true
  try {
    const params: Record<string, unknown> = {
      page: transferPage.value,
      pageSize: transferPageSize.value
    }
    if (filters.status != null) params.status = filters.status
    if (filters.pendingOnly) params.pendingConfirm = true
    if (filters.confirmedRange?.length === 2) {
      params.confirmedFrom = filters.confirmedRange[0]
      params.confirmedTo = filters.confirmedRange[1]
    }
    if (filters.declarationCode.trim()) params.declarationCode = filters.declarationCode.trim()
    if (filters.fromWarehouseId.trim()) params.fromWarehouseId = filters.fromWarehouseId.trim()
    if (filters.toWarehouseId.trim()) params.toWarehouseId = filters.toWarehouseId.trim()
    const res = await fetchStockTransfers(params)
    list.value = res.items
    listTotal.value = res.total
  } catch (e: unknown) {
    ElMessage.error(e instanceof Error ? e.message : String(e))
    list.value = []
    listTotal.value = 0
  } finally {
    loading.value = false
  }
}

function searchTransfers() {
  void loadTransfers(true)
}

function onTransferPageSizeChange() {
  void loadTransfers(true)
}

function loadTransfersPage() {
  void loadTransfers(false)
}

async function confirmOne(row: StockTransferListItemDto) {
  if (row.isConfirmed) return
  try {
    await ElMessageBox.confirm(t('customsPages.transfers.confirmMsg'), t('customsPages.transfers.confirmBtn'), {
      type: 'warning'
    })
  } catch {
    return
  }
  try {
    await confirmStockTransfer(row.id)
    ElMessage.success(t('customsPages.transfers.confirmOk'))
    await loadTransfers(false)
  } catch (e: unknown) {
    ElMessage.error(e instanceof Error ? e.message : String(e))
  }
}

function onDblClick(row: StockTransferListItemDto) {
  detailRow.value = row
  detailVisible.value = true
}

onMounted(() => {
  void loadTransfers(true)
})
</script>

<style scoped>
.customs-page {
  padding: 20px 24px;
}
.page-header {
  margin-bottom: 16px;
}
.page-title {
  margin: 0;
  font-size: 20px;
  font-weight: 600;
}
.filter-form {
  margin-bottom: 12px;
}
.data-table {
  width: 100%;
}
.pagination-bar {
  display: flex;
  justify-content: flex-end;
  margin-top: 12px;
}
</style>
