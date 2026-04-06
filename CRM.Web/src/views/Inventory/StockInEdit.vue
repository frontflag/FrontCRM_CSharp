<template>
  <div class="stockin-edit-page" v-loading="detailLoading">
    <div class="page-header">
      <div class="header-left">
        <div class="page-title-group">
          <div class="page-icon">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
              <rect x="3" y="3" width="18" height="18" rx="2" ry="2" />
              <path d="M3 9h18" />
              <path d="M9 21V9" />
            </svg>
          </div>
          <h1 class="page-title">{{ isCreateMode ? '新建入库单' : '入库单详情' }}</h1>
          <span v-if="!isCreateMode && detailStatus !== null" :class="['status-badge', `status-${detailStatus}`]">{{ statusLabel(detailStatus) }}</span>
        </div>
      </div>
      <div class="header-right">
        <button class="btn-secondary" @click="goBack">返回列表</button>
        <button
          v-if="isCreateMode"
          class="btn-primary"
          style="margin-left: 8px"
          @click="handleSubmit"
          :disabled="submitting"
        >
          {{ submitting ? '保存中...' : '保存并入库' }}
        </button>
      </div>
    </div>

    <div class="form-layout">
      <div class="form-card">
        <h3 class="section-title">基础信息</h3>
        <el-form :model="form" label-width="90px" class="stockin-form">
          <el-form-item label="入库单号" required>
            <el-input v-model="form.stockInCode" placeholder="如：SIN202603180001" :readonly="!isCreateMode" />
          </el-form-item>
          <el-form-item v-if="isCreateMode" label="仓库ID" required>
            <el-input v-model="form.warehouseId" placeholder="目标仓库ID" />
          </el-form-item>
          <el-form-item v-else label="仓库编号" required>
            <el-input :model-value="displayWarehouseCode" readonly />
          </el-form-item>
          <el-form-item v-if="isCreateMode" label="供应商ID">
            <el-input v-model="form.vendorId" placeholder="供应商ID（可选）" />
          </el-form-item>
          <el-form-item v-else label="供应商名称">
            <el-input :model-value="displayVendorName" readonly />
          </el-form-item>
          <el-form-item label="来源单号">
            <el-input v-model="form.purchaseOrderId" placeholder="到货通知/采购行号等（可选）" :readonly="!isCreateMode" />
          </el-form-item>
          <el-form-item label="入库日期" required>
            <el-date-picker
              v-model="form.stockInDate"
              type="datetime"
              format="YYYY-MM-DD HH:mm"
              value-format="YYYY-MM-DDTHH:mm:ss"
              style="width: 100%"
              :disabled="!isCreateMode"
            />
          </el-form-item>
          <el-form-item label="备注">
            <el-input v-model="form.remark" type="textarea" :rows="2" placeholder="备注信息" :readonly="!isCreateMode" />
          </el-form-item>
        </el-form>
      </div>

      <div class="form-card">
        <div class="section-header">
          <h3 class="section-title">入库明细</h3>
          <button v-if="isCreateMode" type="button" class="btn-secondary btn-sm" @click="addRow">新增一行</button>
        </div>
        <el-table :data="form.items" class="quantum-table" style="width: 100%">
          <el-table-column type="index" width="50" />
          <el-table-column label="物料编号" min-width="140">
            <template #default="{ row }">
              <el-input v-model="row.materialCode" placeholder="物料编号 / 物料ID" :readonly="!isCreateMode" />
            </template>
          </el-table-column>
          <el-table-column label="物料名称" min-width="160">
            <template #default="{ row }">
              <el-input v-model="row.materialName" placeholder="物料名称" :readonly="!isCreateMode" />
            </template>
          </el-table-column>
          <el-table-column label="数量" width="110">
            <template #default="{ row }">
              <el-input-number v-model="row.quantity" :min="0" :step="1" :disabled="!isCreateMode" />
            </template>
          </el-table-column>
          <el-table-column label="单位" width="90">
            <template #default="{ row }">
              <el-input v-model="row.unit" placeholder="PCS" :readonly="!isCreateMode" />
            </template>
          </el-table-column>
          <el-table-column label="单价" width="110">
            <template #default="{ row }">
              <el-input-number v-model="row.unitPrice" :min="0" :precision="6" :controls="false" :disabled="!isCreateMode" />
            </template>
          </el-table-column>
          <el-table-column label="批次号" width="140">
            <template #default="{ row }">
              <el-input v-model="row.batchNo" placeholder="批次号" :readonly="!isCreateMode" />
            </template>
          </el-table-column>
          <el-table-column label="库位" width="140">
            <template #default="{ row }">
              <el-input v-model="row.warehouseLocation" placeholder="库位编码" :readonly="!isCreateMode" />
            </template>
          </el-table-column>
          <el-table-column v-if="isCreateMode" label="操作" width="80" fixed="right" class-name="op-col" label-class-name="op-col">
            <template #default="{ $index }">
              <button type="button" class="action-btn action-btn--danger" @click.stop="removeRow($index)">删除</button>
            </template>
          </el-table-column>
        </el-table>
        <div class="table-footer">
          <div class="total">
            合计数量：<span>{{ totalQuantity }}</span>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, reactive, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { stockInApi, type CreateStockInRequest, type StockInDto, type StockInItemDto } from '@/api/stockIn'

const router = useRouter()
const route = useRoute()
const submitting = ref(false)
const detailLoading = ref(false)
const detailStatus = ref<number | null>(null)
/** 详情页展示：仓库编号（非 UUID） */
const displayWarehouseCode = ref('')
/** 详情页展示：供应商名称 */
const displayVendorName = ref('')

const isCreateMode = computed(() => route.name === 'StockInCreate')

const form = reactive<CreateStockInRequest>({
  stockInCode: '',
  purchaseOrderId: '',
  vendorId: '',
  warehouseId: '',
  operatorId: '',
  stockInDate: new Date().toISOString(),
  totalQuantity: 0,
  remark: '',
  items: []
})

function resetCreateForm() {
  detailStatus.value = null
  displayWarehouseCode.value = ''
  displayVendorName.value = ''
  form.stockInCode = ''
  form.purchaseOrderId = ''
  form.vendorId = ''
  form.warehouseId = ''
  form.operatorId = ''
  form.stockInDate = new Date().toISOString()
  form.totalQuantity = 0
  form.remark = ''
  form.items = []
}

function normalizeDateForPicker(iso: string | undefined | null): string {
  if (!iso || typeof iso !== 'string') return new Date().toISOString().slice(0, 19)
  const t = iso.includes('T') ? iso.slice(0, 19) : iso.replace(' ', 'T').slice(0, 19)
  return t || new Date().toISOString().slice(0, 19)
}

function pickStr(obj: Record<string, unknown>, ...keys: string[]): string {
  for (const k of keys) {
    const v = obj[k]
    if (v != null && String(v).trim() !== '') return String(v).trim()
  }
  return ''
}

function extractDetailItemRows(d: StockInDto): Record<string, unknown>[] {
  const r = d as unknown as Record<string, unknown>
  const raw = r.items ?? r.Items
  return Array.isArray(raw) ? (raw as Record<string, unknown>[]) : []
}

function applyDetailToForm(d: StockInDto) {
  const r = d as unknown as Record<string, unknown>
  detailStatus.value = d.status ?? null
  form.stockInCode = d.stockInCode ?? ''
  form.warehouseId = d.warehouseId ?? ''
  form.vendorId = d.vendorId ?? ''
  const wh = pickStr(r, 'detailWarehouseCode', 'DetailWarehouseCode')
  displayWarehouseCode.value = wh || (form.warehouseId ? String(form.warehouseId) : '—')
  const vn = pickStr(r, 'detailVendorName', 'DetailVendorName')
  displayVendorName.value = vn || (form.vendorId ? String(form.vendorId) : '—')
  const parts = [d.sourceCode, d.purchaseOrderItemCode].filter(x => x != null && String(x).trim() !== '')
  form.purchaseOrderId = parts.length ? parts.map(x => String(x).trim()).join(' / ') : ''
  form.stockInDate = normalizeDateForPicker(d.stockInDate)
  form.remark = d.remark ?? ''
  form.totalQuantity = d.totalQuantity ?? 0
  form.operatorId = ''

  const rawItems = extractDetailItemRows(d)
  form.items = rawItems.map((it, i): StockInItemDto => {
    const code =
      pickStr(it, 'detailMaterialCode', 'DetailMaterialCode') ||
      pickStr(it, 'materialId', 'MaterialId')
    const name = pickStr(it, 'detailMaterialName', 'DetailMaterialName')
    const unit = pickStr(it, 'detailUnit', 'DetailUnit') || 'PCS'
    const qty = Number(it.quantity ?? it.Quantity) || 0
    const price = Number(it.price ?? it.Price) || 0
    return {
      lineNo: i + 1,
      materialCode: code,
      materialName: name,
      specification: '',
      quantity: qty,
      unit,
      unitPrice: price,
      batchNo: pickStr(it, 'batchNo', 'BatchNo'),
      warehouseLocation: pickStr(it, 'locationId', 'LocationId')
    }
  })
}

async function loadStockInDetail(id: string) {
  detailLoading.value = true
  try {
    const data = await stockInApi.getById(id)
    if (!data) {
      ElMessage.error('入库单不存在或无权查看')
      router.replace('/inventory/stock-in')
      return
    }
    applyDetailToForm(data)
  } catch (e) {
    console.error(e)
    ElMessage.error('加载入库单失败')
    router.replace('/inventory/stock-in')
  } finally {
    detailLoading.value = false
  }
}

watch(
  () => ({ name: route.name, id: route.params.id }),
  async ({ name, id }) => {
    if (name === 'StockInCreate') {
      resetCreateForm()
      return
    }
    if (name === 'StockInDetail' && typeof id === 'string' && id) {
      await loadStockInDetail(id)
    }
  },
  { immediate: true }
)

const statusLabel = (s: number) => {
  switch (s) {
    case 0:
      return '草稿'
    case 1:
      return '待入库'
    case 2:
      return '已入库'
    case 3:
      return '已取消'
    default:
      return '未知'
  }
}

const addRow = () => {
  const lineNo = (form.items?.length ?? 0) + 1
  const item: StockInItemDto = {
    lineNo,
    materialCode: '',
    materialName: '',
    specification: '',
    quantity: 0,
    unit: 'PCS',
    unitPrice: 0,
    batchNo: '',
    warehouseLocation: ''
  }
  form.items = [...(form.items || []), item]
}

const removeRow = (index: number) => {
  if (!form.items) return
  const items = [...form.items]
  items.splice(index, 1)
  form.items = items.map((x, i) => ({ ...x, lineNo: i + 1 }))
}

const totalQuantity = computed(() => (form.items || []).reduce((sum, x) => sum + (x.quantity || 0), 0))

const handleSubmit = async () => {
  if (!form.stockInCode || !form.warehouseId) {
    ElMessage.warning('请填写入库单号和仓库ID')
    return
  }
  if (!form.items || form.items.length === 0) {
    ElMessage.warning('请至少添加一条入库明细')
    return
  }

  submitting.value = true
  try {
    form.totalQuantity = totalQuantity.value
    const payload: CreateStockInRequest = {
      ...form,
      items: form.items
    }
    await stockInApi.create(payload)
    ElMessage.success('入库单创建成功')
    router.push('/inventory/stock-in')
  } catch (e) {
    console.error(e)
    ElMessage.error('保存入库单失败')
  } finally {
    submitting.value = false
  }
}

const goBack = () => {
  router.push('/inventory/stock-in')
}
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.stockin-edit-page {
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
  .section-title {
    margin: 0;
  }
}
.section-title {
  font-size: 14px;
  font-weight: 500;
  color: $text-secondary;
  margin: 0 0 8px;
}
.status-badge {
  display: inline-block;
  padding: 2px 8px;
  border-radius: 4px;
  font-size: 12px;
  &.status-0 { background: rgba(255,255,255,0.05); color: $text-muted; }
  &.status-1 { background: rgba(255,193,7,0.15); color: #ffc107; }
  &.status-2 { background: rgba(70,191,145,0.18); color: #46BF91; }
  &.status-3 { background: rgba(201,87,69,0.18); color: #C95745; }
}
.stockin-form {
  max-width: 600px;
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

