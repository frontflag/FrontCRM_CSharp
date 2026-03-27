<template>
  <div class="inventory-list-page">
    <div class="page-header">
      <div class="header-left">
        <div class="page-title-group">
          <div class="page-icon">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
              <path d="M21 16V8a2 2 0 00-1-1.73l-7-4a2 2 0 00-2 0l-7 4A2 2 0 003 8v8a2 2 0 001 1.73l7 4a2 2 0 002 0l7-4A2 2 0 0021 16z"/>
              <polyline points="3.27 6.96 12 12.01 20.73 6.96"/>
              <line x1="12" y1="22.08" x2="12" y2="12"/>
            </svg>
          </div>
          <h1 class="page-title">库存中心</h1>
        </div>
        <div class="count-badge">共 {{ list.length }} 条</div>
      </div>
      <div class="header-right">
        <el-input
          v-model="warehouseFilter"
          placeholder="仓库ID 筛选"
          clearable
          style="width: 180px; margin-right: 8px;"
          @keyup.enter="fetchList"
        />
        <button class="btn-secondary" @click="openWarehouseDialog">仓库管理</button>
        <button class="btn-primary" @click="fetchList">刷新</button>
      </div>
    </div>

    <div class="stat-row" v-if="finance">
      <div class="stat-card">
        <div class="label">在库资金占用</div>
        <div class="value">{{ formatMoney(finance.inventoryCapital) }}</div>
      </div>
      <div class="stat-card">
        <div class="label">本月出库成本</div>
        <div class="value">{{ formatMoney(finance.monthlyOutCost) }}</div>
      </div>
      <div class="stat-card">
        <div class="label">周转天数</div>
        <div class="value">{{ finance.turnoverDays?.toFixed(2) || '0.00' }}</div>
      </div>
      <div class="stat-card">
        <div class="label">呆滞料数</div>
        <div class="value">{{ finance.stagnantMaterialCount }}</div>
      </div>
    </div>

    <CrmDataTable :data="list" v-loading="loading">
      <el-table-column label="物料型号" min-width="220" show-overflow-tooltip>
        <template #default="{ row }">{{ materialModelAndName(row) }}</template>
      </el-table-column>
      <el-table-column label="仓库名称" width="160" show-overflow-tooltip>
        <template #default="{ row }">{{ warehouseNameOf(row.warehouseId) }}</template>
      </el-table-column>
      <el-table-column prop="onHandQty" label="在库数量" width="110" align="right">
        <template #default="{ row }">{{ formatNum(row.onHandQty) }}</template>
      </el-table-column>
      <el-table-column prop="availableQty" label="可用数量" width="110" align="right">
        <template #default="{ row }">{{ formatNum(row.availableQty) }}</template>
      </el-table-column>
      <el-table-column prop="lockedQty" label="占用数量" width="110" align="right">
        <template #default="{ row }">{{ formatNum(row.lockedQty) }}</template>
      </el-table-column>
      <el-table-column prop="inventoryAmount" label="库存金额" width="120" align="right">
        <template #default="{ row }">{{ formatMoney(row.inventoryAmount) }}</template>
      </el-table-column>
      <el-table-column prop="lastMoveTime" label="最后移动时间" width="170">
        <template #default="{ row }">{{ formatTime(row.lastMoveTime) }}</template>
      </el-table-column>
      <el-table-column label="创建时间" width="160">
        <template #default="{ row }">{{ formatTime((row as any).createTime || (row as any).createdAt) }}</template>
      </el-table-column>
      <el-table-column label="创建人" width="120" show-overflow-tooltip>
        <template #default="{ row }">{{ (row as any).createUserName || (row as any).createdBy || '--' }}</template>
      </el-table-column>
      <el-table-column label="操作" width="100" fixed="right">
        <template #default="{ row }">
          <button class="action-btn" @click="openTrace(row.materialId)">入库追溯</button>
        </template>
      </el-table-column>
    </CrmDataTable>

    <el-dialog v-model="warehouseVisible" title="仓库管理" width="720px">
      <el-form :model="warehouseForm" inline>
        <el-form-item label="仓库编码">
          <el-input v-model="warehouseForm.warehouseCode" />
        </el-form-item>
        <el-form-item label="仓库名称">
          <el-input v-model="warehouseForm.warehouseName" />
        </el-form-item>
        <el-form-item label="地址">
          <el-input v-model="warehouseForm.address" />
        </el-form-item>
        <el-form-item>
          <button class="btn-primary" type="button" @click="saveWarehouse">保存仓库</button>
        </el-form-item>
      </el-form>
      <el-table :data="warehouses">
        <el-table-column prop="id" label="库存ID" width="220" show-overflow-tooltip />
        <el-table-column prop="warehouseCode" label="编码" width="140" />
        <el-table-column prop="warehouseName" label="名称" width="180" />
        <el-table-column prop="address" label="地址" min-width="200" />
      </el-table>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { inventoryCenterApi, type FinanceSummary, type InventoryOverview, type WarehouseInfo } from '@/api/inventoryCenter'
import { getApiErrorMessage } from '@/utils/apiError'
import { formatDisplayDateTime } from '@/utils/displayDateTime'

const router = useRouter()
const loading = ref(false)
const list = ref<InventoryOverview[]>([])
const warehouseFilter = ref('')
const finance = ref<FinanceSummary | null>(null)
const warehouseVisible = ref(false)
const warehouses = ref<WarehouseInfo[]>([])
const warehouseForm = ref<WarehouseInfo>({
  warehouseCode: '',
  warehouseName: '',
  address: '',
  status: 1
})

const formatNum = (v: number) => (v == null ? '--' : Number(v).toLocaleString())
const formatMoney = (v: number) => (v == null ? '--' : Number(v).toFixed(2))
const formatTime = (v?: string) => formatDisplayDateTime(v)
const warehouseNameOf = (warehouseId?: string) => {
  if (!warehouseId) return '--'
  const byId = warehouses.value.find(w => w.id === warehouseId)
  if (byId?.warehouseName) return byId.warehouseName
  const byCode = warehouses.value.find(w => (w.warehouseCode || '').trim() === warehouseId.trim())
  return byCode?.warehouseName || warehouseId
}

function pickRowStr(row: Record<string, unknown>, camel: string, pascal: string): string {
  const v = row[camel] ?? row[pascal]
  return typeof v === 'string' ? v : ''
}

/** 展示：规格型号 + 名称；兼容 PascalCase；无主数据时回退物料 ID */
const materialModelAndName = (row: InventoryOverview) => {
  const r = row as unknown as Record<string, unknown>
  const model = pickRowStr(r, 'materialModel', 'MaterialModel').trim()
  const name = pickRowStr(r, 'materialName', 'MaterialName').trim()
  const id = pickRowStr(r, 'materialId', 'MaterialId').trim()
  if (model && name) return `${model} ${name}`
  if (model) return model
  if (name) return name
  return id || '--'
}

/** 最后移动时间降序；无时间排后 */
const sortByLastMoveDesc = (rows: InventoryOverview[]) =>
  [...rows].sort((a, b) => {
    const ta = a.lastMoveTime ? new Date(a.lastMoveTime).getTime() : null
    const tb = b.lastMoveTime ? new Date(b.lastMoveTime).getTime() : null
    if (ta == null && tb == null) {
      return pickRowStr(a as unknown as Record<string, unknown>, 'warehouseId', 'WarehouseId').localeCompare(
        pickRowStr(b as unknown as Record<string, unknown>, 'warehouseId', 'WarehouseId')
      )
    }
    if (ta == null) return 1
    if (tb == null) return -1
    if (tb !== ta) return tb - ta
    return pickRowStr(a as unknown as Record<string, unknown>, 'warehouseId', 'WarehouseId').localeCompare(
      pickRowStr(b as unknown as Record<string, unknown>, 'warehouseId', 'WarehouseId')
    )
  })

const fetchList = async () => {
  loading.value = true
  try {
    const [overviewRes, summaryRes, warehouseRes] = await Promise.allSettled([
      inventoryCenterApi.getOverview(warehouseFilter.value || undefined),
      inventoryCenterApi.getFinanceSummary(),
      inventoryCenterApi.getWarehouses()
    ])

    if (overviewRes.status === 'fulfilled') {
      list.value = sortByLastMoveDesc(overviewRes.value)
    } else {
      list.value = []
      ElMessage.error(getApiErrorMessage(overviewRes.reason, '加载库存总览失败'))
    }

    if (summaryRes.status === 'fulfilled') {
      finance.value = summaryRes.value
    } else {
      finance.value = null
      ElMessage.warning(getApiErrorMessage(summaryRes.reason, '加载库存分析失败'))
    }

    if (warehouseRes.status === 'fulfilled') {
      warehouses.value = warehouseRes.value
    }
  } catch (e) {
    console.error(e)
    ElMessage.error(getApiErrorMessage(e, '加载库存中心数据失败'))
    list.value = []
  } finally {
    loading.value = false
  }
}

const openTrace = (materialId: string) => {
  router.push(`/inventory/traces/${encodeURIComponent(materialId)}`)
}

const openWarehouseDialog = async () => {
  try {
    warehouseVisible.value = true
    warehouses.value = await inventoryCenterApi.getWarehouses()
  } catch (e) {
    console.error(e)
    ElMessage.error(getApiErrorMessage(e, '加载仓库列表失败'))
  }
}

const saveWarehouse = async () => {
  try {
    const form = warehouseForm.value
    const trimmedId = form.id?.trim()
    const shouldSendId = !!trimmedId && warehouses.value.some(w => w.id === trimmedId)
    const payload: WarehouseInfo = shouldSendId
      ? { ...form, id: trimmedId }
      : {
          warehouseCode: form.warehouseCode,
          warehouseName: form.warehouseName,
          address: form.address,
          status: form.status
        }

    await inventoryCenterApi.saveWarehouse(payload)
    ElMessage.success('仓库保存成功')
    warehouseForm.value = { warehouseCode: '', warehouseName: '', address: '', status: 1 }
    warehouses.value = await inventoryCenterApi.getWarehouses()
  } catch (e) {
    console.error(e)
    ElMessage.error(getApiErrorMessage(e, '仓库保存失败'))
  }
}

onMounted(() => fetchList())
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.inventory-list-page {
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
.count-badge {
  font-size: 12px;
  color: $text-muted;
  background: rgba(255, 255, 255, 0.05);
  border: 1px solid $border-panel;
  border-radius: 20px;
  padding: 3px 10px;
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
.action-btn {
  background: transparent;
  border: none;
  color: $cyan-primary;
  cursor: pointer;
  font-size: 12px;
}
.stat-row {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 10px;
  margin-bottom: 12px;
}
.stat-card {
  background: $layer-2;
  border: 1px solid $border-panel;
  border-radius: 8px;
  padding: 10px 12px;
  .label { color: $text-muted; font-size: 12px; }
  .value { color: $cyan-primary; font-size: 18px; font-weight: 600; margin-top: 4px; }
}
</style>
