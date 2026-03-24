<template>
  <div class="page-wrap">
    <div class="page-header">
      <h2>质检</h2>
      <el-button @click="loadData">刷新</el-button>
    </div>

    <CrmDataTable :data="list" v-loading="loading">
      <el-table-column prop="qcCode" label="质检单号" width="170" />
      <el-table-column prop="arrivalNoticeCode" label="到货通知号" width="170" />
      <el-table-column label="质检结果" width="120">
        <template #default="{ row }">
          <el-tag effect="dark" :type="qcType(row.status)">{{ qcText(row.status) }}</el-tag>
        </template>
      </el-table-column>
      <el-table-column label="入库状态" width="120">
        <template #default="{ row }">
          <el-tag effect="dark" :type="stockInType(row.stockInStatus)">{{ stockInText(row.stockInStatus) }}</el-tag>
        </template>
      </el-table-column>
      <el-table-column prop="passQty" label="通过数量" width="110" align="right" />
      <el-table-column prop="rejectQty" label="拒收数量" width="110" align="right" />
      <el-table-column prop="createTime" label="创建时间" width="170">
        <template #default="{ row }">{{ formatTime(row.createTime) }}</template>
      </el-table-column>
      <el-table-column label="操作" width="260" fixed="right">
        <template #default="{ row }">
          <el-button link type="primary" @click="goView(row)">查看</el-button>
          <el-button link type="success" v-if="canCreateStockIn(row)" @click="createStockIn(row)">生成入库</el-button>
        </template>
      </el-table-column>
    </CrmDataTable>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { ElMessage } from 'element-plus'
import { logisticsApi, type QcInfoDto } from '@/api/logistics'
import { stockInApi } from '@/api/stockIn'
import { useRouter } from 'vue-router'
import { getApiErrorMessage } from '@/utils/apiError'

const router = useRouter()
const loading = ref(false)
const list = ref<QcInfoDto[]>([])

const qcText = (s: number) => ({ [-1]: '未通过', 10: '部分通过', 100: '已通过' }[s] || '未知')
const qcType = (s: number) => ({ [-1]: 'danger', 10: 'warning', 100: 'success' }[s] || 'info')
const stockInText = (s: number) => ({ [-1]: '拒收', 1: '未入库', 10: '部分入库', 100: '全部入库' }[s] || '未知')
const stockInType = (s: number) => ({ [-1]: 'danger', 1: 'info', 10: 'warning', 100: 'success' }[s] || 'info')
const formatTime = (v?: string) => (v ? v.replace('T', ' ').slice(0, 16) : '--')

const loadData = () => {
  loading.value = true
  logisticsApi.getQcs()
    .then(res => { list.value = (res || []).sort((a, b) => (a.createTime < b.createTime ? 1 : -1)) })
    .finally(() => { loading.value = false })
}

const goView = (row: QcInfoDto) => {
  router.push({ name: 'QcCreate', query: { qcId: row.id } })
}

const canCreateStockIn = (row: QcInfoDto) => row.status !== -1 && !row.stockInId

const createStockIn = async (row: QcInfoDto) => {
  const notices = await logisticsApi.getArrivalNotices()
  const notice = notices.find(x => x.id === row.stockInNotifyId)
  if (!notice) {
    ElMessage.error('关联到货通知不存在')
    return
  }
  const buildMaterialCode = (x: { purchaseOrderItemId?: string; pn?: string }, idx: number) => {
    const code = (x.purchaseOrderItemId || x.pn || `MAT-${idx + 1}`).trim()
    // 后端 StockInItem.MaterialId 最大长度 36，避免超长导致 SaveChanges 失败
    return code.slice(0, 36)
  }

  const items = (notice.items || [])
    .filter(x => Number(x.passedQty || 0) > 0)
    .map((x, idx) => ({
      lineNo: idx + 1,
      materialCode: buildMaterialCode(x, idx),
      materialName: x.pn || '物料',
      quantity: Number(x.passedQty || 0),
      unit: 'PCS',
      unitPrice: 0
    }))

  if (!items.length) {
    ElMessage.warning('无可入库的通过明细')
    return
  }

  loading.value = true
  try {
    const payload = {
      stockInCode: `SI${new Date().toISOString().replace(/[-:TZ.]/g, '').slice(2, 14)}`,
      purchaseOrderId: notice.purchaseOrderId,
      vendorId: notice.vendorId,
      warehouseId: 'WH-DEFAULT',
      stockInDate: new Date().toISOString(),
      totalQuantity: Number(items.reduce((s, x) => s + Number(x.quantity || 0), 0).toFixed(4)),
      remark: `由质检单 ${row.qcCode} 生成`,
      items
    }
    const created = await stockInApi.create(payload as any)
    const stockInId = (created as any)?.id || ''
    if (!stockInId) {
      throw new Error('生成入库单失败：未返回入库单ID')
    }
    await logisticsApi.bindQcStockIn(row.id, stockInId)
    loadData()
    ElMessage.success('已生成入库单')
  } catch (error) {
    ElMessage.error(getApiErrorMessage(error, '生成入库失败'))
  } finally {
    loading.value = false
  }
}

loadData()
</script>

<style scoped lang="scss">
.page-wrap { padding: 20px; }
.page-header { display: flex; align-items: center; justify-content: space-between; margin-bottom: 16px; }
h2 { margin: 0; }
</style>
