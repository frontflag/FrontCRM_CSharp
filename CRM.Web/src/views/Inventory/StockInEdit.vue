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
        <el-form v-if="isCreateMode" :model="form" label-width="90px" class="stockin-form">
          <el-form-item label="入库单号" required>
            <el-input v-model="form.stockInCode" placeholder="如：SIN202603180001" />
          </el-form-item>
          <el-form-item label="仓库ID" required>
            <el-input v-model="form.warehouseId" placeholder="目标仓库ID" />
          </el-form-item>
          <el-form-item label="供应商ID">
            <el-input v-model="form.vendorId" placeholder="供应商ID（可选）" />
          </el-form-item>
          <el-form-item label="到货通知号">
            <el-input v-model="form.purchaseOrderId" placeholder="到货通知/采购行号等（可选）" />
          </el-form-item>
          <el-form-item label="入库日期" required>
            <el-date-picker
              v-model="form.stockInDate"
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
        <dl v-else class="stockin-report-dl" aria-label="基础信息">
          <div class="stockin-report-row">
            <dt>入库单号</dt>
            <dd>{{ reportCellText(form.stockInCode) }}</dd>
          </div>
          <div class="stockin-report-row">
            <dt>仓库编号</dt>
            <dd>{{ reportCellText(displayWarehouseCode) }}</dd>
          </div>
          <div class="stockin-report-row">
            <dt>供应商名称</dt>
            <dd>{{ reportCellText(displayVendorName) }}</dd>
          </div>
          <div class="stockin-report-row">
            <dt>到货通知号</dt>
            <dd>{{ reportCellText(form.purchaseOrderId) }}</dd>
          </div>
          <div class="stockin-report-row">
            <dt>入库日期</dt>
            <dd>{{ reportDateTimeText(form.stockInDate) }}</dd>
          </div>
          <div class="stockin-report-row stockin-report-row--block">
            <dt>备注</dt>
            <dd class="stockin-report-multiline">{{ reportCellText(form.remark) }}</dd>
          </div>
        </dl>
      </div>

      <div class="form-card">
        <div class="section-header">
          <h3 class="section-title">入库明细</h3>
          <button v-if="isCreateMode" type="button" class="btn-secondary btn-sm" @click="addRow">新增一行</button>
        </div>
        <div class="detail-items-table-wrap">
          <el-table :data="form.items" class="items-table quantum-table" style="width: 100%">
            <el-table-column type="index" width="50" align="center" />
            <el-table-column label="入库明细编号" min-width="168" show-overflow-tooltip>
              <template #default="{ row }">
                <span v-if="isCreateMode" class="stockin-report-cell">—</span>
                <span v-else class="stockin-report-cell">{{ reportCellText(row.stockInItemCode) }}</span>
              </template>
            </el-table-column>
            <el-table-column label="物料型号" min-width="168" show-overflow-tooltip>
              <template #default="{ row }">
                <el-input
                  v-if="isCreateMode"
                  v-model="row.materialCode"
                  placeholder="物料主数据 Id（UUID）或采购明细行 Id"
                />
                <span v-else class="stockin-report-cell">{{ reportCellText(row.materialName) }}</span>
              </template>
            </el-table-column>
            <el-table-column label="品牌" min-width="120" show-overflow-tooltip>
              <template #default="{ row }">
                <el-input v-if="isCreateMode" v-model="row.materialBrand" placeholder="可选" />
                <span v-else class="stockin-report-cell">{{ reportCellText(row.materialBrand) }}</span>
              </template>
            </el-table-column>
            <el-table-column label="数量" width="110" align="right" header-align="right">
              <template #default="{ row }">
                <el-input-number v-if="isCreateMode" v-model="row.quantity" :min="0" :step="1" />
                <span v-else class="stockin-report-cell stockin-report-cell--num">{{ reportQtyText(row.quantity) }}</span>
              </template>
            </el-table-column>
            <el-table-column label="单位" width="90" align="center" header-align="center">
              <template #default="{ row }">
                <el-input v-if="isCreateMode" v-model="row.unit" placeholder="PCS" />
                <span v-else class="stockin-report-cell">{{ reportCellText(row.unit) }}</span>
              </template>
            </el-table-column>
            <el-table-column label="单价" width="120" align="right" header-align="right">
              <template #default="{ row }">
                <el-input-number
                  v-if="isCreateMode"
                  v-model="row.unitPrice"
                  :min="0"
                  :precision="6"
                  :controls="false"
                />
                <span v-else class="stockin-report-cell stockin-report-cell--num">{{ reportUnitPriceText(row.unitPrice) }}</span>
              </template>
            </el-table-column>
            <el-table-column label="批次号" width="140" show-overflow-tooltip>
              <template #default="{ row }">
                <el-input v-if="isCreateMode" v-model="row.batchNo" placeholder="批次号" />
                <span v-else class="stockin-report-cell">{{ reportCellText(row.batchNo) }}</span>
              </template>
            </el-table-column>
            <el-table-column label="库位" width="140" show-overflow-tooltip>
              <template #default="{ row }">
                <el-input v-if="isCreateMode" v-model="row.warehouseLocation" placeholder="库位编码" />
                <span v-else class="stockin-report-cell">{{ reportCellText(row.warehouseLocation) }}</span>
              </template>
            </el-table-column>
            <el-table-column v-if="isCreateMode" label="操作" width="80" fixed="right" class-name="op-col" label-class-name="op-col">
              <template #default="{ $index }">
                <button type="button" class="action-btn action-btn--danger" @click.stop="removeRow($index)">删除</button>
              </template>
            </el-table-column>
          </el-table>
        </div>
        <div class="table-footer">
          <div class="total">
            合计数量：<span>{{ totalQuantityDisplay }}</span>
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
    const model =
      pickStr(it, 'detailMaterialModel', 'DetailMaterialModel') ||
      pickStr(it, 'purchasePn', 'PurchasePn') ||
      pickStr(it, 'detailMaterialName', 'DetailMaterialName')
    const brand =
      pickStr(it, 'detailMaterialBrand', 'DetailMaterialBrand') ||
      pickStr(it, 'purchaseBrand', 'PurchaseBrand')
    const unit = pickStr(it, 'detailUnit', 'DetailUnit') || 'PCS'
    const qty = Number(it.quantity ?? it.Quantity) || 0
    const price = Number(it.price ?? it.Price) || 0
    return {
      lineNo: i + 1,
      stockInItemCode: pickStr(it, 'stockInItemCode', 'StockInItemCode'),
      materialCode: code,
      materialName: model,
      materialBrand: brand,
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
    materialBrand: '',
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
/** 与业务列表数量展示一致（千分位） */
const totalQuantityDisplay = computed(() => totalQuantity.value.toLocaleString('zh-CN'))

/** 详情只读报表：空值统一为 — */
function reportCellText(v: unknown): string {
  if (v === null || v === undefined) return '—'
  const s = String(v).trim()
  return s ? s : '—'
}

function reportDateTimeText(iso: string | undefined | null): string {
  if (!iso || typeof iso !== 'string') return '—'
  const t = iso.includes('T') ? iso.slice(0, 16).replace('T', ' ') : iso.trim().slice(0, 16)
  return t || '—'
}

function reportQtyText(n: unknown): string {
  const x = Number(n)
  if (!Number.isFinite(x)) return '—'
  return x.toLocaleString('zh-CN')
}

function reportUnitPriceText(n: unknown): string {
  const x = Number(n)
  if (!Number.isFinite(x)) return '—'
  return x.toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 6 })
}

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
      items: (form.items || []).map(({ materialBrand: _brand, ...rest }) => ({ ...rest }))
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

/* 详情：基础信息只读报表（非输入框外观） */
.stockin-report-dl {
  margin: 0;
  max-width: 720px;
}
.stockin-report-row {
  display: grid;
  grid-template-columns: 96px 1fr;
  gap: 10px 16px;
  align-items: start;
  padding: 8px 0;
  border-bottom: 1px solid $border-panel;
  font-size: 13px;
  &:last-child {
    border-bottom: none;
  }
  dt {
    margin: 0;
    color: $text-muted;
    font-weight: 500;
    white-space: nowrap;
  }
  dd {
    margin: 0;
    color: $text-primary;
    word-break: break-word;
  }
}
.stockin-report-row--block {
  grid-template-columns: 96px 1fr;
}
.stockin-report-multiline {
  white-space: pre-wrap;
  line-height: 1.5;
}

.stockin-report-cell {
  display: inline-block;
  font-size: 13px;
  color: $text-primary;
  line-height: 1.5;
  &--num {
    font-variant-numeric: tabular-nums;
  }
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

/* 与订单详情「订单明细」表头/行样式一致（业务列表范式） */
.detail-items-table-wrap {
  margin-top: 4px;
}

.items-table {
  --el-table-border-color: transparent;
  --el-table-header-bg-color: var(--crm-table-header-bg);
  --el-table-row-hover-bg-color: var(--crm-table-row-hover);
  --el-table-bg-color: transparent;
  --el-table-tr-bg-color: transparent;
  --el-table-text-color: #{$text-primary};
  --el-table-header-text-color: #{$text-muted};
  --el-table-fixed-box-shadow: none;
  background: transparent !important;
  :deep(.el-table) {
    --el-table-text-color: #{$text-primary};
    color: $text-primary;
  }
  :deep(.el-table__inner-wrapper) {
    background: transparent;
    &::before {
      display: none !important;
    }
    &::after {
      display: none !important;
    }
  }
  :deep(.el-table__border-left-patch) {
    display: none !important;
  }
  :deep(.el-table__header-wrapper) {
    th.el-table__cell {
      background: var(--crm-table-header-bg) !important;
      border-bottom: 1px solid var(--crm-table-header-line) !important;
      border-right: none !important;
      color: $text-muted !important;
      font-size: 12px;
      font-weight: 500;
      letter-spacing: 0.3px;
    }
    th.el-table__cell .cell {
      color: inherit !important;
    }
  }
  :deep(.el-table__body-wrapper .el-table__body tr.el-table__row td.el-table__cell),
  :deep(.el-table__fixed-body-wrapper .el-table__body tr.el-table__row td.el-table__cell) {
    color: $text-primary !important;
    font-size: 13px;
  }
  :deep(.el-table__body-wrapper .el-table__body tr.el-table__row td.el-table__cell .cell),
  :deep(.el-table__fixed-body-wrapper .el-table__body tr.el-table__row td.el-table__cell .cell) {
    color: $text-primary !important;
  }
  :deep(.el-table__cell) {
    .el-button {
      white-space: nowrap !important;
    }
    .cell {
      white-space: nowrap;
    }
  }
}
</style>

