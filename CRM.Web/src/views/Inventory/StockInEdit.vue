<template>
  <div class="stockin-edit-page">
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
          <h1 class="page-title">新建入库单</h1>
        </div>
      </div>
      <div class="header-right">
        <button class="btn-secondary" @click="goBack">返回列表</button>
        <button class="btn-primary" style="margin-left: 8px" @click="handleSubmit" :disabled="submitting">
          {{ submitting ? '保存中...' : '保存并入库' }}
        </button>
      </div>
    </div>

    <div class="form-layout">
      <div class="form-card">
        <h3 class="section-title">基础信息</h3>
        <el-form :model="form" label-width="90px" class="stockin-form">
          <el-form-item label="入库单号" required>
            <el-input v-model="form.stockInCode" placeholder="如：SIN202603180001" />
          </el-form-item>
          <el-form-item label="仓库ID" required>
            <el-input v-model="form.warehouseId" placeholder="目标仓库ID" />
          </el-form-item>
          <el-form-item label="供应商ID">
            <el-input v-model="form.vendorId" placeholder="供应商ID（可选）" />
          </el-form-item>
          <el-form-item label="来源单号">
            <el-input v-model="form.purchaseOrderId" placeholder="采购订单号（可选）" />
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
      </div>

      <div class="form-card">
        <div class="section-header">
          <h3 class="section-title">入库明细</h3>
          <button class="btn-secondary btn-sm" @click="addRow">新增一行</button>
        </div>
        <el-table :data="form.items" class="quantum-table" style="width: 100%">
          <el-table-column type="index" width="50" />
          <el-table-column label="物料编码" min-width="140">
            <template #default="{ row }">
              <el-input v-model="row.materialCode" placeholder="物料编码" />
            </template>
          </el-table-column>
          <el-table-column label="物料名称" min-width="160">
            <template #default="{ row }">
              <el-input v-model="row.materialName" placeholder="物料名称" />
            </template>
          </el-table-column>
          <el-table-column label="数量" width="110">
            <template #default="{ row }">
              <el-input-number v-model="row.quantity" :min="0" :step="1" />
            </template>
          </el-table-column>
          <el-table-column label="单位" width="90">
            <template #default="{ row }">
              <el-input v-model="row.unit" placeholder="PCS" />
            </template>
          </el-table-column>
          <el-table-column label="单价" width="110">
            <template #default="{ row }">
              <el-input-number v-model="row.unitPrice" :min="0" :step="0.01" />
            </template>
          </el-table-column>
          <el-table-column label="批次号" width="140">
            <template #default="{ row }">
              <el-input v-model="row.batchNo" placeholder="批次号" />
            </template>
          </el-table-column>
          <el-table-column label="库位" width="140">
            <template #default="{ row }">
              <el-input v-model="row.warehouseLocation" placeholder="库位编码" />
            </template>
          </el-table-column>
          <el-table-column label="操作" width="80" fixed="right">
            <template #default="{ $index }">
              <button class="action-btn" @click="removeRow($index)">删除</button>
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
import { computed, reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { stockInApi, type CreateStockInRequest, type StockInItemDto } from '@/api/stockIn'

const router = useRouter()
const submitting = ref(false)

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
}
.section-title {
  font-size: 14px;
  font-weight: 500;
  color: $text-secondary;
  margin: 0 0 8px;
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
  &:hover { text-decoration: underline; }
}
</style>

