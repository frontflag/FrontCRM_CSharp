<template>
  <div class="create-page">
    <!-- 面包屑 + 操作栏 -->
    <div class="page-header">
      <div class="header-left">
        <el-button link @click="router.back()">
          <el-icon><ArrowLeft /></el-icon> 返回列表
        </el-button>
        <el-breadcrumb separator="/">
          <el-breadcrumb-item>订单管理</el-breadcrumb-item>
          <el-breadcrumb-item>销售管理</el-breadcrumb-item>
          <el-breadcrumb-item>新建销售订单</el-breadcrumb-item>
        </el-breadcrumb>
      </div>
      <div class="header-right">
        <el-button @click="router.back()">取消</el-button>
        <el-button type="primary" :loading="submitLoading" @click="handleSubmit">
          <el-icon><Check /></el-icon> 保存
        </el-button>
      </div>
    </div>

    <!-- 表单卡片 -->
    <el-form ref="formRef" :model="formData" :rules="formRules" label-width="100px" class="create-form">

      <!-- 基本信息 -->
      <div class="form-section">
        <div class="section-title">
          <span class="title-bar"></span>基本信息
        </div>
        <el-row :gutter="24">
          <el-col :span="12">
            <el-form-item label="订单号">
              <el-input v-model="formData.sellOrderCode" disabled placeholder="系统自动生成" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="客户" prop="customerName">
              <el-input v-model="formData.customerName" placeholder="请输入客户名称" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="24">
          <el-col :span="12">
            <el-form-item label="业务员" prop="salesUserName">
              <el-input v-model="formData.salesUserName" placeholder="请输入业务员" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="订单类型">
              <el-select v-model="formData.type" style="width: 100%">
                <el-option label="普通订单" :value="1" />
                <el-option label="紧急订单" :value="2" />
                <el-option label="样品订单" :value="3" />
              </el-select>
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="24">
          <el-col :span="12">
            <el-form-item label="币别">
              <el-select v-model="formData.currency" style="width: 100%">
                <el-option label="CNY 人民币" :value="1" />
                <el-option label="USD 美元" :value="2" />
                <el-option label="EUR 欧元" :value="3" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="交货日期">
              <el-date-picker
                v-model="formData.deliveryDate"
                type="date"
                placeholder="选择交货日期"
                style="width: 100%"
                value-format="YYYY-MM-DD"
              />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="24">
          <el-col :span="24">
            <el-form-item label="送货地址">
              <el-input v-model="formData.deliveryAddress" type="textarea" :rows="2" placeholder="请输入送货地址" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="24">
          <el-col :span="24">
            <el-form-item label="备注">
              <el-input v-model="formData.comment" type="textarea" :rows="2" placeholder="请输入备注" />
            </el-form-item>
          </el-col>
        </el-row>
      </div>

      <!-- 订单明细 -->
      <div class="form-section">
        <div class="section-title">
          <span class="title-bar"></span>订单明细
          <el-button type="primary" size="small" class="add-item-btn" @click="addItem">
            <el-icon><Plus /></el-icon> 添加明细
          </el-button>
        </div>
        <el-table :data="formData.items" border size="small" class="items-table">
          <el-table-column type="index" width="50" label="#" align="center" />
          <el-table-column label="物料型号" min-width="180">
            <template #default="{ $index }">
              <el-input v-model="formData.items[$index].pn" placeholder="请输入物料型号" />
            </template>
          </el-table-column>
          <el-table-column label="品牌" width="130">
            <template #default="{ $index }">
              <el-input v-model="formData.items[$index].brand" placeholder="品牌" />
            </template>
          </el-table-column>
          <el-table-column label="数量" width="120">
            <template #default="{ $index }">
              <el-input-number v-model="formData.items[$index].qty" :min="1" :controls="false" style="width: 100%" />
            </template>
          </el-table-column>
          <el-table-column label="单价" width="140">
            <template #default="{ $index }">
              <el-input-number v-model="formData.items[$index].price" :min="0" :precision="2" :controls="false" style="width: 100%" />
            </template>
          </el-table-column>
          <el-table-column label="小计" width="140" align="right">
            <template #default="{ row }">
              <span class="subtotal">{{ formatCurrency((row.qty || 0) * (row.price || 0), formData.currency) }}</span>
            </template>
          </el-table-column>
          <el-table-column label="操作" width="80" align="center">
            <template #default="{ $index }">
              <el-button link type="danger" @click="removeItem($index)">删除</el-button>
            </template>
          </el-table-column>
        </el-table>
        <div class="total-row">
          <span class="total-label">合计金额：</span>
          <span class="total-amount">{{ formatCurrency(calculateTotal, formData.currency) }}</span>
        </div>
      </div>

    </el-form>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { ArrowLeft, Check, Plus } from '@element-plus/icons-vue'
import { mockSalesOrderApi as salesOrderApi } from '@/api/mockSalesOrder'

const router = useRouter()
const formRef = ref()
const submitLoading = ref(false)

// 生成订单号
const genOrderCode = () => {
  const date = new Date().toISOString().slice(0, 10).replace(/-/g, '')
  const rand = String(Math.random()).slice(2, 5)
  return `SO${date}${rand}`
}

const formData = ref({
  sellOrderCode: genOrderCode(),
  customerName: '',
  salesUserName: '',
  type: 1,
  currency: 1,
  deliveryDate: '',
  deliveryAddress: '',
  comment: '',
  items: [] as any[]
})

const formRules = {
  customerName: [{ required: true, message: '请输入客户名称', trigger: 'blur' }],
  salesUserName: [{ required: true, message: '请输入业务员', trigger: 'blur' }]
}

const calculateTotal = computed(() =>
  formData.value.items.reduce((sum, item) => sum + (item.qty || 0) * (item.price || 0), 0)
)

const formatCurrency = (value: number, currency?: number) => {
  const symbol = currency === 2 ? '$' : currency === 3 ? '€' : '¥'
  return symbol + (value || 0).toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

const addItem = () => {
  formData.value.items.push({ pn: '', brand: '', qty: 1, price: 0, currency: formData.value.currency })
}

const removeItem = (index: number) => {
  formData.value.items.splice(index, 1)
}

const handleSubmit = async () => {
  await formRef.value.validate()
  submitLoading.value = true
  try {
    const data = {
      ...formData.value,
      total: calculateTotal.value,
      itemRows: formData.value.items.length
    }
    await salesOrderApi.create(data)
    ElMessage.success('销售订单创建成功')
    router.push({ name: 'SalesOrderList' })
  } catch (e) {
    ElMessage.error('创建失败，请重试')
  } finally {
    submitLoading.value = false
  }
}
</script>

<style scoped lang="scss">
/* SalesOrderCreate.vue — 独立新建页面，暗色科技风 */
.create-page {
  padding: 20px;
  min-height: 100%;
}

.page-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 20px;

  .header-left {
    display: flex;
    align-items: center;
    gap: 12px;

    .el-button.is-link {
      color: #5a7a9a;
      font-size: 13px;
      &:hover { color: #00c8ff; }
    }
  }

  .header-right {
    display: flex;
    gap: 10px;
  }
}

.create-form {
  .form-section {
    background: #0a1828;
    border: 1px solid #1a2d45;
    border-radius: 8px;
    padding: 20px 24px;
    margin-bottom: 16px;
  }

  .section-title {
    display: flex;
    align-items: center;
    gap: 8px;
    font-size: 14px;
    font-weight: 600;
    color: #c8dff0;
    margin-bottom: 20px;

    .title-bar {
      width: 3px;
      height: 16px;
      background: linear-gradient(180deg, #00c8ff, #0066cc);
      border-radius: 2px;
    }

    .add-item-btn {
      margin-left: auto;
    }
  }

  :deep(.el-form-item__label) {
    color: #5a7a9a;
    font-size: 13px;
  }

  :deep(.el-input__wrapper),
  :deep(.el-textarea__inner),
  :deep(.el-select .el-input__wrapper) {
    background: #0d1e35;
    border-color: #1a2d45;
    box-shadow: none;
    color: #c8dff0;
    &:hover { border-color: #2a4d75; }
    &.is-focus { border-color: #00c8ff; }
  }

  :deep(.el-input.is-disabled .el-input__wrapper) {
    background: #071220;
    border-color: #1a2d45;
    .el-input__inner { color: #3a5a7a; }
  }

  :deep(.el-input__inner),
  :deep(.el-textarea__inner) {
    color: #c8dff0;
    background: transparent;
    &::placeholder { color: #3a5a7a; }
  }

  :deep(.el-select-dropdown) {
    background: #0d1e35;
    border-color: #1a2d45;
  }

  :deep(.el-date-editor .el-input__wrapper) {
    background: #0d1e35;
    border-color: #1a2d45;
  }
}

.items-table {
  :deep(.el-table) {
    --el-table-border-color: #1a2d45;
    --el-table-header-bg-color: #0d1e35;
    --el-table-row-hover-bg-color: #0f2035;
    --el-table-bg-color: #0a1828;
    --el-table-tr-bg-color: #0a1828;
    background: #0a1828;
    color: #c8dff0;
  }
  :deep(.el-table th) {
    color: #5a7a9a;
    font-weight: 500;
    font-size: 12px;
  }
  :deep(.el-input-number .el-input__wrapper) {
    background: #0d1e35;
    border-color: #1a2d45;
  }
  :deep(.el-input-number .el-input__inner) {
    color: #c8dff0;
    background: transparent;
  }
}

.subtotal {
  color: #00c8ff;
  font-size: 13px;
}

.total-row {
  display: flex;
  justify-content: flex-end;
  align-items: center;
  padding: 12px 0 0;
  gap: 8px;

  .total-label {
    color: #5a7a9a;
    font-size: 13px;
  }

  .total-amount {
    color: #00c8ff;
    font-size: 16px;
    font-weight: 700;
  }
}
</style>
