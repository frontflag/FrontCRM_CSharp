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

    <CrmDataTable :data="list" v-loading="loading" @row-dblclick="onRowDblclick">
      <el-table-column label="物料型号" min-width="160" show-overflow-tooltip>
        <template #default="{ row }">{{ materialModelDisplay(row) }}</template>
      </el-table-column>
      <el-table-column label="品牌" min-width="120" show-overflow-tooltip>
        <template #default="{ row }">{{ materialBrandDisplay(row) }}</template>
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
      <el-table-column
        label="操作"
        :width="opColMainWidth"
        :min-width="opColMainMinWidth"
        fixed="right"
        class-name="op-col"
        label-class-name="op-col"
      >
        <template #header>
          <div class="op-col-header">
            <span class="op-col-header-text">操作</span>
            <button type="button" class="op-col-toggle-btn" @click.stop="toggleOpColMain">
              {{ opColMainExpanded ? '>' : '<' }}
            </button>
          </div>
        </template>

        <template #default="{ row }">
          <div @click.stop @dblclick.stop>
            <div v-if="opColMainExpanded" class="action-btns">
              <button type="button" class="action-btn action-btn--info" @click.stop="openTrace(row.materialId)">入库追溯</button>
            </div>

            <el-dropdown v-else trigger="click" placement="bottom-end">
              <div class="op-more-dropdown-trigger">
                <button type="button" class="op-more-trigger">...</button>
              </div>
              <template #dropdown>
                <el-dropdown-menu>
                  <el-dropdown-item @click.stop="openTrace(row.materialId)">
                    <span class="op-more-item op-more-item--info">入库追溯</span>
                  </el-dropdown-item>
                </el-dropdown-menu>
              </template>
            </el-dropdown>
          </div>
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
          <button class="btn-primary" type="button" @click="saveWarehouse">
            {{ warehouseForm.id ? '保存修改' : '保存仓库' }}
          </button>
          <button class="btn-secondary" type="button" style="margin-left: 8px" @click="resetWarehouseForm">新建</button>
        </el-form-item>
      </el-form>
      <el-table :data="warehouses" class="warehouse-table">
        <el-table-column prop="warehouseCode" label="编码" width="140" />
        <el-table-column prop="warehouseName" label="名称" width="180" />
        <el-table-column prop="address" label="地址" min-width="200" />
        <el-table-column
          label="操作"
          :width="opColWarehouseWidth"
          :min-width="opColWarehouseMinWidth"
          align="center"
          fixed="right"
          class-name="op-col"
          label-class-name="op-col"
        >
          <template #header>
            <div class="op-col-header">
              <span class="op-col-header-text">操作</span>
              <button type="button" class="op-col-toggle-btn" @click.stop="toggleOpColWarehouse">
                {{ opColWarehouseExpanded ? '>' : '<' }}
              </button>
            </div>
          </template>
          <template #default="{ row }">
            <div @click.stop @dblclick.stop>
              <div v-if="opColWarehouseExpanded" class="action-btns">
                <button type="button" class="action-btn action-btn--primary" @click.stop="loadWarehouseForEdit(row)">编辑</button>
              </div>

              <el-dropdown v-else trigger="click" placement="bottom-end">
                <div class="op-more-dropdown-trigger">
                  <button type="button" class="op-more-trigger">...</button>
                </div>
                <template #dropdown>
                  <el-dropdown-menu>
                    <el-dropdown-item @click.stop="loadWarehouseForEdit(row)">
                      <span class="op-more-item op-more-item--primary">编辑</span>
                    </el-dropdown-item>
                  </el-dropdown-menu>
                </template>
              </el-dropdown>
            </div>
          </template>
        </el-table-column>
      </el-table>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
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

// 列表操作列：主表默认收起（Collapsed）
const opColMainExpanded = ref(false)
const OP_COL_MAIN_COLLAPSED_WIDTH = 96
const OP_COL_MAIN_EXPANDED_WIDTH = 100
const OP_COL_MAIN_EXPANDED_MIN_WIDTH = 100
const opColMainWidth = computed(() => (opColMainExpanded.value ? OP_COL_MAIN_EXPANDED_WIDTH : OP_COL_MAIN_COLLAPSED_WIDTH))
const opColMainMinWidth = computed(() =>
  opColMainExpanded.value ? OP_COL_MAIN_EXPANDED_MIN_WIDTH : OP_COL_MAIN_COLLAPSED_WIDTH
)
function toggleOpColMain() {
  opColMainExpanded.value = !opColMainExpanded.value
}

// 列表操作列：弹窗表格默认收起（Collapsed）
const opColWarehouseExpanded = ref(false)
const OP_COL_WAREHOUSE_COLLAPSED_WIDTH = 96
const OP_COL_WAREHOUSE_EXPANDED_WIDTH = 110
const OP_COL_WAREHOUSE_EXPANDED_MIN_WIDTH = 110
const opColWarehouseWidth = computed(() =>
  opColWarehouseExpanded.value ? OP_COL_WAREHOUSE_EXPANDED_WIDTH : OP_COL_WAREHOUSE_COLLAPSED_WIDTH
)
const opColWarehouseMinWidth = computed(() =>
  opColWarehouseExpanded.value ? OP_COL_WAREHOUSE_EXPANDED_MIN_WIDTH : OP_COL_WAREHOUSE_COLLAPSED_WIDTH
)
function toggleOpColWarehouse() {
  opColWarehouseExpanded.value = !opColWarehouseExpanded.value
}
const emptyWarehouseForm = (): WarehouseInfo => ({
  warehouseCode: '',
  warehouseName: '',
  address: '',
  status: 1
})

const warehouseForm = ref<WarehouseInfo>(emptyWarehouseForm())

/** 兼容接口 camelCase / PascalCase */
function normalizeWarehouseRow(row: WarehouseInfo): WarehouseInfo {
  const r = row as unknown as Record<string, unknown>
  const idRaw = r.id ?? r.Id
  const id = typeof idRaw === 'string' && idRaw.trim() ? idRaw.trim() : undefined
  const code = String(r.warehouseCode ?? r.WarehouseCode ?? '').trim()
  const name = String(r.warehouseName ?? r.WarehouseName ?? '').trim()
  const addr = String(r.address ?? r.Address ?? '')
  const st = r.status ?? r.Status
  const status = typeof st === 'number' ? st : 1
  return { id, warehouseCode: code, warehouseName: name, address: addr, status }
}

const resetWarehouseForm = () => {
  warehouseForm.value = emptyWarehouseForm()
}

const loadWarehouseForEdit = (row: WarehouseInfo) => {
  warehouseForm.value = normalizeWarehouseRow(row)
}

const formatNum = (v: number) => (v == null ? '--' : Number(v).toLocaleString())
const formatMoney = (v: number) => (v == null ? '--' : Number(v).toFixed(2))
const formatTime = (v?: string) => formatDisplayDateTime(v)
const warehouseNameOf = (warehouseId?: string) => {
  if (!warehouseId) return '--'
  const byId = warehouses.value.find(w => normalizeWarehouseRow(w).id === warehouseId)
  if (byId) {
    const n = normalizeWarehouseRow(byId)
    if (n.warehouseName) return n.warehouseName
  }
  const byCode = warehouses.value.find(w => normalizeWarehouseRow(w).warehouseCode === warehouseId.trim())
  if (byCode) {
    const n = normalizeWarehouseRow(byCode)
    if (n.warehouseName) return n.warehouseName
  }
  return warehouseId
}

function pickRowStr(row: Record<string, unknown>, camel: string, pascal: string): string {
  const v = row[camel] ?? row[pascal]
  return typeof v === 'string' ? v : ''
}

/** 规格型号；兼容 PascalCase；无型号时回退物料 ID */
const materialModelDisplay = (row: InventoryOverview) => {
  const r = row as unknown as Record<string, unknown>
  const model = pickRowStr(r, 'materialModel', 'MaterialModel').trim()
  const id = pickRowStr(r, 'materialId', 'MaterialId').trim()
  return model || id || '--'
}

/** 品牌（接口字段为 materialName，总览中常来自主数据名称或产品品牌）；兼容 PascalCase */
const materialBrandDisplay = (row: InventoryOverview) => {
  const r = row as unknown as Record<string, unknown>
  const name = pickRowStr(r, 'materialName', 'MaterialName').trim()
  return name || '--'
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

const onRowDblclick = (row: InventoryOverview) => openTrace(row.materialId)

const openWarehouseDialog = async () => {
  try {
    resetWarehouseForm()
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
    const shouldSendId =
      !!trimmedId && warehouses.value.some(w => normalizeWarehouseRow(w).id === trimmedId)
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
    resetWarehouseForm()
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
  background: $layer-3;
  border: 1px solid $border-card;
  border-radius: 8px;
  padding: 10px 12px;
  .label {
    color: $text-muted;
    font-size: 12px;
  }
  .value {
    color: $cyan-primary;
    font-size: 18px;
    font-weight: 600;
    margin-top: 4px;
  }
}

// 列表操作列规范（收起/展开）
.op-col-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0;
  width: 100%;
}

.op-col-header-text {
  font-size: 12px;
  line-height: 1;
  white-space: nowrap;
}

.op-col-toggle-btn {
  padding: 0;
  border: none;
  background: transparent;
  cursor: pointer;
  color: $cyan-primary;
  font-size: 16px;
  line-height: 1;
  flex: 0 0 auto;
}

.op-more-trigger {
  padding: 0;
  border: none;
  background: transparent;
  cursor: pointer;
  color: $cyan-primary;
  font-size: 16px;
  line-height: 1;
  opacity: 0;
  transition: opacity 0.15s;
}

:deep(.el-table__body-wrapper .el-table__body tr:hover .op-more-trigger),
:deep(.el-table__fixed-body-wrapper .el-table__body tr:hover .op-more-trigger),
:deep(.el-table__body-wrapper .el-table__body tr.hover-row .op-more-trigger),
:deep(.el-table__fixed-body-wrapper .el-table__body tr.hover-row .op-more-trigger) {
  opacity: 1;
}

.op-more-item {
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
}

.op-more-item--primary {
  color: $cyan-primary;
}

.op-more-item--warning {
  color: $color-amber;
}

.op-more-item--danger {
  color: $color-red-brown;
}

.op-more-item--success {
  color: $color-mint-green;
}

.op-more-item--info {
  color: rgba(200, 216, 232, 0.85);
}

</style>
