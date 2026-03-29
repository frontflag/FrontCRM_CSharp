<template>
  <div class="qc-list-page">
    <div class="page-header">
      <h2>质检</h2>
      <div class="ops">
        <button type="button" class="btn-ghost btn-sm" :disabled="loading" @click="loadData">刷新</button>
      </div>
    </div>

    <!-- 搜索栏：与 CustomerList / ArrivalNoticeList 同款布局与控件皮肤 -->
    <div class="search-bar">
      <div class="search-left">
        <span class="filter-field-label">物料型号</span>
        <div class="search-input-wrap">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="search-icon">
            <circle cx="11" cy="11" r="8" />
            <line x1="21" y1="21" x2="16.65" y2="16.65" />
          </svg>
          <input
            v-model="filters.model"
            class="search-input"
            placeholder="物料型号(PN)"
            @keyup.enter="handleSearch"
          />
        </div>
        <span class="filter-field-label">供应商</span>
        <div class="search-input-wrap">
          <input
            v-model="filters.vendorName"
            class="search-input--plain"
            placeholder="供应商名称"
            @keyup.enter="handleSearch"
          />
        </div>
        <span class="filter-field-label">采购单号</span>
        <div class="search-input-wrap">
          <input
            v-model="filters.purchaseOrderCode"
            class="search-input--plain"
            placeholder="采购订单号"
            @keyup.enter="handleSearch"
          />
        </div>
        <span class="filter-field-label">销售单号</span>
        <div class="search-input-wrap">
          <input
            v-model="filters.salesOrderCode"
            class="search-input--plain"
            placeholder="销售订单号"
            @keyup.enter="handleSearch"
          />
        </div>
        <button type="button" class="btn-primary btn-sm" :disabled="loading" @click="handleSearch">搜索</button>
        <button type="button" class="btn-ghost btn-sm" :disabled="loading" @click="resetFilters">重置</button>
      </div>
    </div>

    <CrmDataTable :data="list" v-loading="loading" @row-dblclick="goView">
      <el-table-column prop="qcCode" label="质检单号" width="160" min-width="160" />
      <el-table-column label="状态" width="120">
        <template #default="{ row }">
          <el-tag effect="dark" :type="qcType(row.status)">{{ qcText(row.status) }}</el-tag>
        </template>
      </el-table-column>
      <el-table-column prop="stockInNotifyCode" label="到货通知单号" width="170" />
      <el-table-column prop="model" label="物料型号" min-width="160" show-overflow-tooltip />
      <el-table-column prop="brand" label="品牌" min-width="120" show-overflow-tooltip />
      <el-table-column prop="vendorName" label="供应商" min-width="160" show-overflow-tooltip />
      <el-table-column prop="purchaseOrderCode" label="采购订单号" width="170" show-overflow-tooltip />
      <el-table-column prop="salesOrderCode" label="销售订单号" width="170" show-overflow-tooltip />
      <el-table-column prop="passQty" label="通过数量" width="110" align="right" />
      <el-table-column prop="rejectQty" label="拒收数量" width="110" align="right" />
      <el-table-column label="入库状态" width="120">
        <template #default="{ row }">
          <el-tag effect="dark" :type="stockInType(displayStockInStatus(row))">{{ stockInText(displayStockInStatus(row)) }}</el-tag>
        </template>
      </el-table-column>
      <el-table-column prop="createTime" label="创建时间" width="170">
        <template #default="{ row }">{{ formatTime(row.createTime) }}</template>
      </el-table-column>
      <el-table-column label="创建人" width="120" show-overflow-tooltip>
        <template #default="{ row }">{{ row.createUserName || row.createdBy || '--' }}</template>
      </el-table-column>
      <!-- 操作：查看 +（条件）生成入库，收窄避免固定列右侧留白 -->
      <el-table-column label="操作" width="200" min-width="200" fixed="right" class-name="op-col" label-class-name="op-col">
        <template #default="{ row }">
          <div @click.stop @dblclick.stop>
            <div class="action-btns">
              <el-button link type="primary" @click.stop="goView(row)">查看</el-button>
              <el-button link type="warning" v-if="canCreateStockIn(row)" @click.stop="createStockIn(row)">生成入库</el-button>
            </div>
          </div>
        </template>
      </el-table-column>
    </CrmDataTable>
  </div>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { logisticsApi, type QcInfoDto } from '@/api/logistics'
import { stockInApi } from '@/api/stockIn'
import { inventoryCenterApi } from '@/api/inventoryCenter'
import { useRouter, useRoute } from 'vue-router'
import { getApiErrorMessage } from '@/utils/apiError'
import { formatDisplayDateTime2DigitYear } from '@/utils/displayDateTime'

const router = useRouter()
const route = useRoute()
const loading = ref(false)
const list = ref<QcInfoDto[]>([])
const filters = ref({
  model: '',
  vendorName: '',
  purchaseOrderCode: '',
  salesOrderCode: '',
})
const getYYMMDD = (d: Date) => {
  const yy = String(d.getFullYear()).slice(-2)
  const mm = String(d.getMonth() + 1).padStart(2, '0')
  const dd = String(d.getDate()).padStart(2, '0')
  return `${yy}${mm}${dd}`
}
const random4 = () => String(Math.floor(Math.random() * 10000)).padStart(4, '0')

const qcText = (s: number) => ({ [-1]: '未通过', 10: '部分通过', 100: '已通过' }[s] || '未知')
const qcType = (s: number) => ({ [-1]: 'danger', 10: 'warning', 100: 'success' }[s] || 'info')
const stockInText = (s: number) => ({ [-1]: '拒收', 1: '未入库', 10: '部分入库', 100: '全部入库' }[s] || '未知')
const stockInType = (s: number) => ({ [-1]: 'danger', 1: 'info', 10: 'warning', 100: 'success' }[s] || 'info')

/** 未绑定入库单时忽略历史脏数据（StockInStatus 误存为 10/100） */
const displayStockInStatus = (row: QcInfoDto) => {
  if (row.status === -1) return -1
  if (!row.stockInId) return 1
  return row.stockInStatus
}
const formatTime = (v?: string) => formatDisplayDateTime2DigitYear(v)

function syncFiltersFromRoute() {
  if (route.name !== 'QcList') return
  const q = route.query
  filters.value.model = typeof q.model === 'string' ? q.model : ''
  filters.value.vendorName = typeof q.vendorName === 'string' ? q.vendorName : ''
  filters.value.purchaseOrderCode = typeof q.purchaseOrderCode === 'string' ? q.purchaseOrderCode : ''
  filters.value.salesOrderCode = typeof q.salesOrderCode === 'string' ? q.salesOrderCode : ''
}

const loadData = () => {
  loading.value = true
  logisticsApi.getQcs({
    model: filters.value.model || undefined,
    vendorName: filters.value.vendorName || undefined,
    purchaseOrderCode: filters.value.purchaseOrderCode || undefined,
    salesOrderCode: filters.value.salesOrderCode || undefined,
  })
    .then(res => { list.value = (res || []).sort((a, b) => (a.createTime < b.createTime ? 1 : -1)) })
    .finally(() => { loading.value = false })
}

watch(
  () => [route.name, route.query] as const,
  () => {
    syncFiltersFromRoute()
    if (route.name === 'QcList') loadData()
  },
  { deep: true, immediate: true }
)

/** 与左侧检索面板共用 URL query */
const handleSearch = () => {
  const query: Record<string, string> = {}
  const m = filters.value.model.trim()
  if (m) query.model = m
  const v = filters.value.vendorName.trim()
  if (v) query.vendorName = v
  const p = filters.value.purchaseOrderCode.trim()
  if (p) query.purchaseOrderCode = p
  const s = filters.value.salesOrderCode.trim()
  if (s) query.salesOrderCode = s
  router.replace({ name: 'QcList', query })
}

const resetFilters = () => {
  filters.value = {
    model: '',
    vendorName: '',
    purchaseOrderCode: '',
    salesOrderCode: '',
  }
  router.replace({ name: 'QcList', query: {} })
}

const goView = (row: QcInfoDto) => {
  router.push({ name: 'QcCreate', query: { qcId: row.id } })
}

const canCreateStockIn = (row: QcInfoDto) => row.status !== -1 && !row.stockInId

const resolveWarehouseId = async () => {
  const warehouses = await inventoryCenterApi.getWarehouses()
  if (!warehouses.length) return 'WH-DEFAULT'
  const preferred = warehouses.find(w => (w.warehouseCode || '').trim().toUpperCase() === 'WH-DEFAULT')
  return preferred?.id || warehouses[0].id || 'WH-DEFAULT'
}

const createStockIn = async (row: QcInfoDto) => {
  try {
    await ElMessageBox.confirm(
      `确定为质检单「${row.qcCode}」生成入库单并完成过账吗？`,
      '生成入库',
      {
        confirmButtonText: '确定',
        cancelButtonText: '取消',
        type: 'warning',
        distinguishCancelAndClose: true,
      }
    )
  } catch {
    return
  }

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
    const warehouseId = await resolveWarehouseId()
    const payload = {
      stockInCode: `SI${getYYMMDD(new Date())}${random4()}`,
      purchaseOrderId: notice.purchaseOrderId,
      vendorId: notice.vendorId,
      warehouseId,
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
    // 质检生成的入库单默认直接完成入库，触发库存中心过账
    await stockInApi.updateStatus(stockInId, 2)
    await logisticsApi.bindQcStockIn(row.id, stockInId)
    loadData()
    ElMessage.success('已生成并过账入库单')
  } catch (error) {
    ElMessage.error(getApiErrorMessage(error, '生成入库失败'))
  } finally {
    loading.value = false
  }
}

</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.qc-list-page {
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

  h2 {
    margin: 0;
    color: $text-primary;
    font-size: 20px;
    font-weight: 600;
  }
}

.ops {
  display: flex;
  gap: 8px;
}

// ---- 搜索栏（与 CustomerList / ArrivalNoticeList 一致）----
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

.search-input-wrap {
  position: relative;
  display: flex;
  align-items: center;
}

.search-input--plain {
  width: 200px;
  padding: 7px 12px;
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
  letter-spacing: 0.5px;

  &:hover:not(:disabled) {
    transform: translateY(-1px);
    box-shadow: 0 4px 16px rgba(0, 212, 255, 0.25);
  }

  &:disabled {
    opacity: 0.55;
    cursor: not-allowed;
    transform: none;
    box-shadow: none;
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
  transition: all 0.2s;

  &:hover:not(:disabled) {
    border-color: rgba(0, 212, 255, 0.3);
    color: $text-secondary;
  }

  &:disabled {
    opacity: 0.55;
    cursor: not-allowed;
  }
}
</style>
