<template>
  <div class="page-wrap">
    <div class="page-header">
      <h2>到货通知</h2>
      <div class="ops">
        <el-button type="primary" @click="syncFromPurchaseOrders">从采购订单同步</el-button>
        <el-button @click="loadData">刷新</el-button>
      </div>
    </div>

    <el-card class="filter-card">
      <el-form :inline="true" class="filter-form">
        <el-form-item label="状态">
          <el-select v-model="filters.status" placeholder="全部状态" clearable style="width: 140px">
            <el-option label="新建" :value="1" />
            <el-option label="未到货" :value="10" />
            <el-option label="到货待检" :value="20" />
            <el-option label="已质检" :value="30" />
            <el-option label="已入库" :value="100" />
          </el-select>
        </el-form-item>
        <el-form-item label="采购单号">
          <el-input
            v-model="filters.purchaseOrderCode"
            placeholder="请输入采购单号"
            clearable
            style="width: 200px"
            @keyup.enter="loadData"
          />
        </el-form-item>
        <el-form-item label="预计到货日期">
          <el-date-picker
            v-model="filters.expectedArrivalDate"
            type="date"
            value-format="YYYY-MM-DD"
            placeholder="选择日期"
            clearable
            style="width: 170px"
          />
        </el-form-item>
        <el-form-item>
          <el-button type="primary" @click="loadData">查询</el-button>
          <el-button @click="resetFilters">重置</el-button>
        </el-form-item>
      </el-form>
    </el-card>

    <CrmDataTable :data="list" v-loading="loading">
      <el-table-column prop="noticeCode" label="到货通知号" width="170" />
      <el-table-column label="状态" width="110">
        <template #default="{ row }">
          <el-tag effect="dark" :type="statusType(row.status)">{{ statusText(row.status) }}</el-tag>
        </template>
      </el-table-column>
      <el-table-column prop="purchaseOrderCode" label="采购单号" width="160" />
      <el-table-column label="预计到货日期" width="130" align="center">
        <template #default="{ row }">{{ formatExpected(row.expectedArrivalDate) }}</template>
      </el-table-column>
      <el-table-column prop="vendorName" label="供应商" min-width="160" />
      <el-table-column prop="purchaseUserName" label="采购员" width="120" />
      <el-table-column label="到货数量" width="120" align="right">
        <template #default="{ row }">{{ sumQty(row.items, 'arrivedQty') }}</template>
      </el-table-column>
      <el-table-column prop="createTime" label="创建时间" width="170">
        <template #default="{ row }">{{ formatTime(row.createTime) }}</template>
      </el-table-column>
      <el-table-column label="创建人" width="120" show-overflow-tooltip>
        <template #default="{ row }">{{ row.createUserName || row.createdBy || row.purchaseUserName || '--' }}</template>
      </el-table-column>
      <el-table-column label="操作" width="220" fixed="right">
        <template #default="{ row }">
          <el-button link type="primary" v-if="row.status === 10" @click="markArrived(row)">确认到货</el-button>
          <el-button link type="success" v-if="row.status === 20" @click="goCreateQc(row)">质检</el-button>
          <el-button link type="info" @click="viewItems(row)">明细</el-button>
        </template>
      </el-table-column>
    </CrmDataTable>

    <el-dialog v-model="itemsVisible" title="到货通知明细" width="760px">
      <CrmDataTable :data="currentItems">
        <el-table-column prop="pn" label="型号" min-width="140" />
        <el-table-column prop="brand" label="品牌" width="120" />
        <el-table-column prop="qty" label="通知数量" width="100" align="right" />
        <el-table-column prop="arrivedQty" label="到货数量" width="100" align="right" />
        <el-table-column prop="passedQty" label="质检通过" width="100" align="right" />
      </CrmDataTable>
    </el-dialog>

  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { ElMessage } from 'element-plus'
import { logisticsApi, type StockInNotifyDto, type StockInNotifyItemDto } from '@/api/logistics'
import { useRouter } from 'vue-router'
import { formatDisplayDate, formatDisplayDateTime } from '@/utils/displayDateTime'

const router = useRouter()
const loading = ref(false)
const list = ref<StockInNotifyDto[]>([])
const itemsVisible = ref(false)
const currentItems = ref<StockInNotifyItemDto[]>([])
const filters = ref<{
  status?: number
  purchaseOrderCode: string
  expectedArrivalDate: string
}>({
  status: undefined,
  purchaseOrderCode: '',
  expectedArrivalDate: ''
})

const sumQty = (items: StockInNotifyItemDto[], key: 'arrivedQty' | 'qty' | 'passedQty') =>
  Number((items || []).reduce((s, x) => s + Number(x?.[key] || 0), 0).toFixed(4))

const statusText = (s: number) => ({ 1: '新建', 10: '未到货', 20: '到货待检', 30: '已质检', 100: '已入库' }[s] || '未知')
const statusType = (s: number) => ({ 1: 'info', 10: 'warning', 20: 'primary', 30: 'success', 100: 'success' }[s] || 'info')
const formatTime = (v?: string) => formatDisplayDateTime(v)
const formatExpected = (v?: string | null) => (v ? formatDisplayDate(v) : '—')

const loadData = () => {
  loading.value = true
  logisticsApi.getArrivalNotices({
    status: filters.value.status,
    purchaseOrderCode: filters.value.purchaseOrderCode.trim() || undefined,
    expectedArrivalDate: filters.value.expectedArrivalDate || undefined
  })
    .then(res => { list.value = (res || []).sort((a, b) => (a.createTime < b.createTime ? 1 : -1)) })
    .finally(() => { loading.value = false })
}

const resetFilters = () => {
  filters.value = {
    status: undefined,
    purchaseOrderCode: '',
    expectedArrivalDate: ''
  }
  loadData()
}

const syncFromPurchaseOrders = async () => {
  loading.value = true
  try {
    const r = await logisticsApi.autoGenerateArrivalNotices()
    loadData()
    ElMessage.success(`已处理 ${r.purchaseOrdersScanned} 张PO，新增 ${r.createdCount} 条，已存在 ${r.existingCount} 条`)
  } catch (e: any) {
    ElMessage.error(e?.message || '同步失败')
  } finally {
    loading.value = false
  }
}

const markArrived = async (row: StockInNotifyDto) => {
  await logisticsApi.updateArrivalStatus(row.id, 20)
  loadData()
  ElMessage.success('已确认到货，等待质检')
}

const goCreateQc = (row: StockInNotifyDto) => {
  router.push({ name: 'QcCreate', query: { noticeId: row.id } })
}

const viewItems = (row: StockInNotifyDto) => {
  currentItems.value = row.items || []
  itemsVisible.value = true
}

loadData()
</script>

<style scoped lang="scss">
.page-wrap { padding: 20px; }
.page-header { display: flex; align-items: center; justify-content: space-between; margin-bottom: 16px; }
.ops { display: flex; gap: 8px; }
.filter-card { margin-bottom: 12px; }
.filter-form { margin-bottom: -18px; }
h2 { margin: 0; }
</style>
