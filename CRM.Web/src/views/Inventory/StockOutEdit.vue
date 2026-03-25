<template>
  <div class="stockout-edit-page">
    <div class="page-header">
      <div class="header-left">
        <div class="page-title-group">
          <div class="page-icon">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
              <path d="M3 3h7v7H3zM14 3h7v7h-7zM3 14h7v7H3zM17 14l4 4-4 4M10 17h11" />
            </svg>
          </div>
          <h1 class="page-title">执行出库</h1>
        </div>
      </div>
      <div class="header-right">
        <button class="btn-secondary" @click="goBack">返回列表</button>
        <button class="btn-secondary" @click="handleGeneratePicking">生成拣货任务</button>
        <button class="btn-primary" style="margin-left: 8px" @click="handleSubmit" :disabled="submitting">
          {{ submitting ? '执行中...' : '执行出库' }}
        </button>
      </div>
    </div>

    <div class="form-layout">
      <div class="form-card">
        <h3 class="section-title">基础信息</h3>
        <el-form :model="form" label-width="90px">
          <el-form-item label="出库单号" required>
            <el-input v-model="form.stockOutCode" placeholder="如：SOUT202603180001" />
          </el-form-item>
          <el-form-item label="仓库名称" required>
            <el-select v-model="form.warehouseId" placeholder="请选择仓库" style="width: 100%">
              <el-option
                v-for="w in warehouses"
                :key="w.id"
                :label="`${w.warehouseName}（${w.warehouseCode}）`"
                :value="w.id"
              />
            </el-select>
          </el-form-item>
          <el-form-item label="申请单ID">
            <el-input v-model="form.stockOutRequestId" placeholder="关联的出库申请ID（可选）" />
          </el-form-item>
          <el-form-item label="操作人">
            <el-input v-model="form.operatorId" placeholder="当前操作人ID（可选）" />
          </el-form-item>
          <el-form-item label="出库日期" required>
            <el-date-picker
              v-model="form.stockOutDate"
              type="datetime"
              format="YYYY-MM-DD HH:mm"
              value-format="YYYY-MM-DDTHH:mm:ss"
              style="width: 100%"
            />
          </el-form-item>
          <el-form-item label="备注">
            <el-input v-model="form.remark" type="textarea" :rows="2" placeholder="备注信息" />
          </el-form-item>
        </el-form>
      </div>

      <div class="form-card">
        <div class="section-header">
          <h3 class="section-title">出库明细</h3>
          <button class="btn-secondary btn-sm" @click="loadItemsFromRequest">从出库通知刷新明细</button>
        </div>
        <el-table :data="form.items" class="quantum-table">
          <el-table-column type="index" width="50" />
          <el-table-column label="物料编码" min-width="140">
            <template #default="{ row }">
              <el-input v-model="row.materialCode" placeholder="物料编码" readonly />
            </template>
          </el-table-column>
          <el-table-column label="物料名称" min-width="160">
            <template #default="{ row }">
              <el-input v-model="row.materialName" placeholder="物料名称" readonly />
            </template>
          </el-table-column>
          <el-table-column label="数量" width="110">
            <template #default="{ row }">
              <el-input-number v-model="row.quantity" :min="0" :step="1" :controls="false" readonly />
            </template>
          </el-table-column>
          <el-table-column label="批次号" width="140">
            <template #default="{ row }">
              <el-input v-model="row.batchNo" placeholder="批次号（可选）" />
            </template>
          </el-table-column>
          <el-table-column label="库位" width="140">
            <template #default="{ row }">
              <el-input v-model="row.warehouseLocation" placeholder="库位编码（可选）" />
            </template>
          </el-table-column>
          <el-table-column label="操作" width="80" fixed="right">
            <template #default="{ $index }">
              <button class="action-btn" disabled title="明细来自出库通知，不可手工删除">删除</button>
            </template>
          </el-table-column>
        </el-table>
        <div class="table-footer">
          <div class="total">
            合计出库数量：<span>{{ totalQuantity }}</span>
          </div>
        </div>
      </div>
    </div>

    <div class="form-card" v-if="pickingTasks.length">
      <h3 class="section-title">拣货任务</h3>
      <el-table :data="pickingTasks">
        <el-table-column prop="taskCode" label="任务号" width="160" />
        <el-table-column prop="warehouseId" label="仓库" width="120" />
        <el-table-column prop="status" label="状态" width="120">
          <template #default="{ row }">{{ pickingStatusText(row.status) }}</template>
        </el-table-column>
        <el-table-column prop="createTime" label="创建时间" width="180" />
        <el-table-column label="操作" width="120">
          <template #default="{ row }">
            <button class="action-btn" v-if="row.status !== 100" @click="completePicking(row.id)">完成拣货</button>
          </template>
        </el-table-column>
      </el-table>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, reactive, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { stockOutApi, type StockOutRequestDto } from '@/api/stockOut'
import { inventoryCenterApi, type PickingTask, type WarehouseInfo } from '@/api/inventoryCenter'
import { getApiErrorMessage } from '@/utils/apiError'

type ExecuteItem = {
  lineNo: number
  materialCode: string
  materialName: string
  quantity: number
  batchNo?: string
  warehouseLocation?: string
}

type ExecuteForm = {
  stockOutRequestId: string
  stockOutCode: string
  warehouseId: string
  operatorId?: string
  stockOutDate: string
  remark?: string
  items: ExecuteItem[]
}

const router = useRouter()
const route = useRoute()
const submitting = ref(false)
const pickingTasks = ref<PickingTask[]>([])
const warehouses = ref<WarehouseInfo[]>([])
const currentRequest = ref<StockOutRequestDto | null>(null)
const getYYMMDD = (d: Date) => {
  const yy = String(d.getFullYear()).slice(-2)
  const mm = String(d.getMonth() + 1).padStart(2, '0')
  const dd = String(d.getDate()).padStart(2, '0')
  return `${yy}${mm}${dd}`
}
const random4 = () => String(Math.floor(Math.random() * 10000)).padStart(4, '0')

const form = reactive<ExecuteForm>({
  stockOutRequestId: (route.query.requestId as string) || '',
  stockOutCode: `SOUT${getYYMMDD(new Date())}${random4()}`,
  warehouseId: '',
  operatorId: '',
  stockOutDate: new Date().toISOString(),
  remark: '',
  items: []
})

const totalQuantity = computed(() => form.items.reduce((sum, x) => sum + (x.quantity || 0), 0))
const pickingStatusText = (s: number) => ({ 1: '待拣货', 2: '拣货中', 100: '已完成', [-1]: '已取消' }[s] || '未知')

const loadWarehouses = async () => {
  try {
    warehouses.value = await inventoryCenterApi.getWarehouses()
    if (!form.warehouseId && warehouses.value.length) {
      form.warehouseId = warehouses.value[0].id || ''
    }
  } catch (e) {
    console.error(e)
    warehouses.value = []
  }
}

const loadRequest = async () => {
  if (!form.stockOutRequestId) return
  try {
    const requests = await stockOutApi.getRequestList()
    currentRequest.value = requests.find(x => x.id === form.stockOutRequestId) || null
  } catch (e) {
    console.error(e)
    currentRequest.value = null
  }
}

const loadItemsFromRequest = async () => {
  if (!form.stockOutRequestId) {
    ElMessage.warning('请先选择出库通知')
    return
  }
  if (!currentRequest.value?.salesOrderId) {
    await loadRequest()
  }
  const r = currentRequest.value
  if (!r?.salesOrderId) {
    ElMessage.warning('出库通知缺少销售订单信息')
    return
  }
  const materialCode = String(r.materialModel ?? '').trim()
  const qty = Number(r.outQuantity ?? 0)
  if (!materialCode || qty <= 0) {
    ElMessage.warning('出库通知缺少物料或数量，无法生成出库明细')
    return
  }
  form.items = [
    {
      lineNo: 1,
      materialCode,
      materialName: String(r.brand ?? '').trim() || '物料',
      quantity: qty,
      batchNo: '',
      warehouseLocation: ''
    }
  ]
  await pickRecommendedWarehouse()
}

const pickRecommendedWarehouse = async () => {
  if (!form.items.length || !warehouses.value.length) return
  try {
    const overview = await inventoryCenterApi.getOverview()
    const materialSet = new Set(form.items.map(x => x.materialCode))
    const candidates = overview
      .filter(x => materialSet.has(x.materialId) && Number(x.availableQty || 0) > 0)
      .sort((a, b) => Number(b.availableQty || 0) - Number(a.availableQty || 0))
    if (!candidates.length) return
    const bestWarehouseId = candidates[0].warehouseId
    if (!form.warehouseId || !candidates.some(x => x.warehouseId === form.warehouseId)) {
      form.warehouseId = bestWarehouseId
    }
  } catch {
    // 推荐仓库失败不阻断主流程
  }
}

const loadPickingTasks = async () => {
  try {
    const tasks = await inventoryCenterApi.getPickingTasks()
    const requestId = form.stockOutRequestId
    pickingTasks.value = (tasks || []).filter(x => requestId && x.stockOutRequestId === requestId)
  } catch {
    pickingTasks.value = []
  }
}

const handleGeneratePicking = async () => {
  if (!form.stockOutRequestId) {
    ElMessage.warning('请先填写出库申请单ID')
    return
  }
  if (!form.warehouseId || !form.items.length) {
    ElMessage.warning('请先填写仓库和出库明细')
    return
  }
  if (form.items.some(x => !x.materialCode || Number(x.quantity || 0) <= 0)) {
    ElMessage.warning('出库明细存在空物料或数量为0，请检查来源数据')
    return
  }
  try {
    await inventoryCenterApi.generatePickingTask({
      stockOutRequestId: form.stockOutRequestId,
      warehouseId: form.warehouseId,
      operatorId: form.operatorId,
      items: form.items.map(x => ({ materialId: x.materialCode, quantity: x.quantity }))
    })
    ElMessage.success('拣货任务已生成')
    await loadPickingTasks()
  } catch (e) {
    console.error(e)
    ElMessage.error(getApiErrorMessage(e, '生成拣货任务失败'))
  }
}

const completePicking = async (taskId: string) => {
  try {
    await inventoryCenterApi.completePickingTask(taskId)
    ElMessage.success('拣货已完成')
    await loadPickingTasks()
  } catch (e) {
    console.error(e)
    ElMessage.error('完成拣货失败')
  }
}

const handleSubmit = async () => {
  if (!form.stockOutCode || !form.warehouseId) {
    ElMessage.warning('请填写出库单号和仓库ID')
    return
  }
  if (!form.items.length) {
    ElMessage.warning('请至少添加一条出库明细')
    return
  }

  submitting.value = true
  try {
    await stockOutApi.execute({
      stockOutRequestId: form.stockOutRequestId,
      stockOutCode: form.stockOutCode,
      warehouseId: form.warehouseId,
      operatorId: form.operatorId,
      stockOutDate: form.stockOutDate,
      remark: form.remark,
      items: form.items
    })
    ElMessage.success('执行出库成功')
    router.push('/inventory/stock-out')
  } catch (e) {
    console.error(e)
    ElMessage.error('执行出库失败')
  } finally {
    submitting.value = false
  }
}

const goBack = () => {
  router.push('/inventory/stock-out')
}

const init = async () => {
  await loadWarehouses()
  await loadRequest()
  await loadItemsFromRequest()
  await loadPickingTasks()
}

init()
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.stockout-edit-page {
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
  .header-left { display: flex; align-items: center; gap: 12px; }
  .header-right { display: flex; align-items: center; gap: 8px; }
}
.page-title-group {
  display: flex;
  align-items: center;
  gap: 10px;
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
  .page-title { font-size: 20px; font-weight: 600; color: $text-primary; margin: 0; }
}
.btn-primary,
.btn-secondary {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 8px 14px;
  border-radius: $border-radius-md;
  font-size: 13px;
  cursor: pointer;
  border: 1px solid transparent;
}
.btn-primary {
  background: linear-gradient(135deg, rgba(0, 102, 255, 0.8), rgba(0, 212, 255, 0.7));
  border-color: rgba(0, 212, 255, 0.4);
  color: #fff;
}
.btn-secondary {
  background: rgba(255, 255, 255, 0.05);
  border-color: $border-panel;
  color: $text-secondary;
}
.btn-sm {
  padding: 6px 10px;
  font-size: 12px;
}
.form-layout {
  display: flex;
  flex-direction: column;
  gap: 16px;
}
.form-card {
  background: $layer-2;
  border-radius: 8px;
  border: 1px solid $border-panel;
  padding: 16px 18px;
}
.section-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 8px;
}
.section-title {
  font-size: 14px;
  font-weight: 500;
  color: $text-secondary;
  margin: 0 0 8px;
}
.table-footer {
  display: flex;
  justify-content: flex-end;
  margin-top: 8px;
  .total {
    font-size: 13px;
    color: $text-secondary;
    span {
      color: $cyan-primary;
      font-weight: 600;
      margin-left: 4px;
    }
  }
}
.action-btn {
  background: transparent;
  border: none;
  color: $cyan-primary;
  cursor: pointer;
  font-size: 12px;
  padding: 2px 6px;
  white-space: nowrap;
  flex-shrink: 0;
  &:hover { text-decoration: underline; }
}
</style>

