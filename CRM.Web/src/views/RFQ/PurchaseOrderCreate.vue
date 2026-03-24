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
          <el-breadcrumb-item>采购管理</el-breadcrumb-item>
          <el-breadcrumb-item>新建采购订单</el-breadcrumb-item>
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
              <el-input v-model="formData.purchaseOrderCode" disabled placeholder="系统自动生成" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="供应商">
              <el-input v-model="formData.vendorName" disabled placeholder="系统自动带出供应商" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="24">
          <el-col :span="12">
            <el-form-item label="供应商联系人">
              <el-input v-model="formData.vendorContactName" disabled placeholder="系统自动带出联系人" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="采购员">
              <el-input v-model="formData.purchaseUserName" disabled placeholder="系统自动带出采购员" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="24">
          <el-col :span="24">
            <el-form-item label="订单类型">
              <el-select v-model="formData.type" style="width: 100%">
                <el-option label="普通订单" :value="1" />
                <el-option label="紧急订单" :value="2" />
                <el-option label="样品订单" :value="3" />
              </el-select>
            </el-form-item>
          </el-col>
        </el-row>
        <!-- 基本信息-备注/内部备注：合并为一行显现 -->
        <el-row :gutter="24">
          <el-col :span="12">
            <el-form-item label="备注" label-width="80px">
              <el-input v-model="formData.comment" type="textarea" :rows="2" placeholder="请输入备注" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="内部备注" label-width="90px">
              <el-input v-model="formData.innerComment" type="textarea" :rows="2" placeholder="内部备注（仅内部可见）" />
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
        <div v-if="formData.items.length === 0" class="items-empty">暂无明细</div>

        <div v-for="(item, index) in formData.items" :key="index" class="material-card">
          <div class="material-card-head">
            <span class="head-mpn">物料型号：{{ item.pn || '—' }}</span>
            <span class="head-quote">
              预期采购单价：{{ formatCurrency(item.cost || 0, formData.currency) }}
            </span>
          </div>
          <div class="material-card-body">
            <el-row :gutter="16">
              <el-col :span="12">
                <el-form-item label="物料型号">
                  <el-input v-model="item.pn" placeholder="请输入物料型号" />
                </el-form-item>
              </el-col>
              <el-col :span="12">
                <el-form-item label="品牌">
                  <el-input v-model="item.brand" placeholder="品牌" />
                </el-form-item>
              </el-col>
            </el-row>

            <el-row :gutter="16">
              <el-col :span="8">
                <el-form-item label="采购单价">
                  <el-input-number v-model="item.targetPrice" :precision="4" :controls="false" style="width: 100%" />
                </el-form-item>
              </el-col>
              <el-col :span="8">
                <el-form-item label="预期采购单价">
                  <el-input-number v-model="item.cost" :min="0" :precision="2" :controls="false" disabled style="width: 100%" />
                </el-form-item>
              </el-col>
              <el-col :span="8">
                <el-form-item label="数量">
                  <el-input-number v-model="item.qty" :min="1" :controls="false" style="width: 100%" />
                </el-form-item>
              </el-col>
            </el-row>

            <el-row :gutter="16">
              <el-col :span="8">
                <el-form-item label="币别">
                  <el-select v-model="item.currency" style="width: 100%">
                    <el-option label="CNY" :value="1" />
                    <el-option label="USD" :value="2" />
                    <el-option label="EUR" :value="3" />
                  </el-select>
                </el-form-item>
              </el-col>
              <el-col :span="8">
                <el-form-item label="生产日期要求">
                  <el-select v-model="item.dateCode" style="width: 100%">
                    <el-option label="2年内" value="2年内" />
                    <el-option label="1年内" value="1年内" />
                    <el-option label="无要求" value="无要求" />
                    <el-option label="—" value="" />
                  </el-select>
                </el-form-item>
              </el-col>
              <el-col :span="8">
                <el-form-item label="交货日期">
                  <el-date-picker
                    v-model="item.deliveryDate"
                    type="date"
                    placeholder="选择交货日期"
                    value-format="YYYY-MM-DD"
                    style="width: 100%"
                  />
                </el-form-item>
              </el-col>
            </el-row>

            <el-row :gutter="16">
              <el-col :span="12">
                <el-form-item label="备注" label-width="80px">
                  <el-input v-model="item.comment" type="textarea" :rows="2" placeholder="备注" />
                </el-form-item>
              </el-col>
              <el-col :span="12">
                <el-form-item label="内部备注" label-width="90px">
                  <el-input v-model="item.innerComment" type="textarea" :rows="2" placeholder="内部备注" />
                </el-form-item>
              </el-col>
            </el-row>

            <div class="material-card-actions">
              <el-button link type="danger" @click="removeItem(index)">删除</el-button>
            </div>

            <div class="line-total-row">
              <span class="line-total-label">预计采购总额：</span>
              <span class="line-total-amount">{{ formatCurrency((item.qty || 0) * (item.targetPrice || 0), item.currency ?? formData.currency) }}</span>
            </div>
          </div>
        </div>

        <div class="total-row">
          <span class="total-label">合计金额：</span>
          <span class="total-amount">{{ formatCurrency(calculateTotal, formData.currency) }}</span>
        </div>
      </div>

    </el-form>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ArrowLeft, Check, Plus } from '@element-plus/icons-vue'
import { purchaseOrderApi } from '@/api/purchaseOrder'
import { purchaseRequisitionApi } from '@/api/purchaseRequisition'
import { runSaveTask } from '@/composables/useFormSubmit'

const router = useRouter()
const route = useRoute()

/** 手工录入时尚无供应商/销售明细主键时的占位（满足后端非空；以销定采时应填真实 ID） */
const MANUAL_VENDOR_ID = '00000000-0000-0000-0000-000000000002'
const MANUAL_SELL_ORDER_ITEM_ID = '00000000-0000-0000-0000-000000000000'
const formRef = ref()
const submitLoading = ref(false)
const genLoading = ref(false)

const requisitionId = computed(() => {
  const v = route.query.requisitionId
  if (!v) return undefined
  return String(v)
})
const generatedFromRequisition = ref(false)

const genOrderCode = () => {
  const date = new Date().toISOString().slice(0, 10).replace(/-/g, '')
  const rand = String(Math.random()).slice(2, 5)
  return `PO${date}${rand}`
}

const formData = ref({
  purchaseOrderCode: genOrderCode(),
  vendorName: '',
  vendorId: '' as string,
  vendorContactName: '',
  vendorContactId: '' as string,
  purchaseUserName: '',
  type: 1,
  currency: 1,
  deliveryDate: '',
  deliveryAddress: '',
  comment: '',
  innerComment: '',
  items: [] as any[]
})

const formRules = {
  // 供应商/采购员由“生成采购订单”自动带出，不参与必填校验
}

const calculateTotal = computed(() =>
  formData.value.items.reduce((sum, item) => sum + (item.qty || 0) * (item.targetPrice || 0), 0)
)

const formatCurrency = (value: number, currency?: number) => {
  const symbol = currency === 2 ? '$' : currency === 3 ? '€' : '¥'
  return symbol + (value || 0).toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

const addItem = () => {
  generatedFromRequisition.value = false
  formData.value.items.push({
    sellOrderItemId: undefined,
    vendorId: undefined,
    pn: '',
    brand: '',
    customerMaterialModel: '',
    targetPrice: 0,
    qty: 1,
    cost: 0,
    currency: formData.value.currency,
    dateCode: '',
    deliveryDate: formData.value.deliveryDate || '',
    comment: '',
    innerComment: ''
  })
}

const removeItem = (index: number) => {
  formData.value.items.splice(index, 1)
}

const handleSubmit = async () => {
  await runSaveTask({
    loading: submitLoading,
    successMessage: '采购订单创建成功',
    task: async () => {
      await purchaseOrderApi.create({
        purchaseOrderCode: formData.value.purchaseOrderCode,
        vendorId: formData.value.vendorId || MANUAL_VENDOR_ID,
        vendorName: formData.value.vendorName,
        purchaseUserName: formData.value.purchaseUserName,
        vendorContactId: formData.value.vendorContactId || undefined,
        type: formData.value.type,
        currency: formData.value.currency,
        deliveryDate: formData.value.deliveryDate || null,
        deliveryAddress: formData.value.deliveryAddress || undefined,
        comment: formData.value.comment || undefined,
        innerComment: formData.value.innerComment || undefined,
        items: formData.value.items.map((it) => ({
          sellOrderItemId: it.sellOrderItemId ?? MANUAL_SELL_ORDER_ITEM_ID,
          vendorId: it.vendorId ?? MANUAL_VENDOR_ID,
          pn: it.pn,
          brand: it.brand,
          qty: it.qty,
          cost: it.targetPrice,
          currency: it.currency ?? formData.value.currency,
          deliveryDate: it.deliveryDate || null,
          comment: it.comment || undefined,
          innerComment: it.innerComment || undefined
        }))
      })
    },
    onSuccess: () => router.push({ name: 'PurchaseOrderList' }),
    errorMessage: () => '创建失败，请重试'
  })
}

async function handleGeneratePurchaseOrder() {
  if (!requisitionId.value) return
  genLoading.value = true
  try {
    const pr = await purchaseRequisitionApi.getById(requisitionId.value)

    // 基于采购申请预填采购订单（PRD：预期采购价来自采购申请的 QuoteCost）
    formData.value.purchaseOrderCode = genOrderCode()
    formData.value.type = pr.type ?? 1
    formData.value.vendorName = pr.intendedVendorName ?? ''
    formData.value.vendorId = pr.quoteVendorId ?? ''
    formData.value.vendorContactId = pr.intendedVendorContactId ?? ''
    formData.value.vendorContactName = pr.intendedVendorContactName ?? ''
    formData.value.purchaseUserName = pr.purchaseUserName ?? pr.purchaseUserId ?? ''
    formData.value.currency = pr.currency ?? formData.value.currency ?? 1

    const deliveryDateStr = pr.deliveryDate ? String(pr.deliveryDate).split('T')[0] : ''
    formData.value.deliveryDate = deliveryDateStr || (pr.expectedPurchaseTime ? String(pr.expectedPurchaseTime).split('T')[0] : '')
    // 基本信息-备注默认空白（PR里的备注/内部备注不回填到主表备注，避免干扰用户手工填写）
    formData.value.comment = ''
    formData.value.items = [
      {
        sellOrderItemId: pr.sellOrderItemId ?? MANUAL_SELL_ORDER_ITEM_ID,
        vendorId: pr.quoteVendorId ?? MANUAL_VENDOR_ID,
        pn: pr.pn ?? '',
        brand: pr.brand ?? '',
        customerMaterialModel: pr.customerMaterialModel ?? '',
        targetPrice: pr.targetPrice ?? 0,
        qty: pr.qty ?? 1,
        cost: pr.quoteCost ?? 0,
        currency: pr.currency ?? formData.value.currency,
        dateCode: pr.dateCode ?? '',
        deliveryDate: deliveryDateStr || formData.value.deliveryDate || '',
        comment: pr.itemRemark ?? '',
        innerComment: ''
      }
    ]

    generatedFromRequisition.value = true
  } catch (e) {
    // eslint-disable-next-line no-console
    console.error(e)
  } finally {
    genLoading.value = false
  }
}

onMounted(() => {
  if (requisitionId.value) handleGeneratePurchaseOrder()
})
</script>

<style scoped lang="scss">
/* PurchaseOrderCreate.vue — 独立新建页面，暗色科技风 */
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

  :deep(.el-date-editor .el-input__wrapper) {
    background: #0d1e35;
    border-color: #1a2d45;
  }
}

.items-table {
  // 无外边框，行间细线分隔，对标客户管理列表风格
  --el-table-border-color: transparent;
  --el-table-header-bg-color: rgba(0, 212, 255, 0.04);
  --el-table-row-hover-bg-color: rgba(0, 212, 255, 0.04);
  --el-table-bg-color: transparent;
  --el-table-tr-bg-color: transparent;
  --el-table-fixed-box-shadow: none;
  background: transparent !important;
  :deep(.el-table__inner-wrapper) {
    background: transparent;
    &::before { display: none !important; }
    &::after  { display: none !important; }
  }
  :deep(.el-table__border-left-patch) { display: none !important; }
  :deep(.el-table__header-wrapper) {
    th.el-table__cell {
      background: rgba(0, 212, 255, 0.04) !important;
      border-bottom: 1px solid rgba(0, 212, 255, 0.1) !important;
      border-right: none !important;
      color: rgba(200, 216, 232, 0.55);
      font-size: 12px;
      font-weight: 500;
      letter-spacing: 0.3px;
    }
  }
  :deep(.el-table__row) {
    background: transparent !important;
    td.el-table__cell {
      background: transparent !important;
      border-bottom: 1px solid rgba(255, 255, 255, 0.04) !important;
      border-right: none !important;
      color: rgba(224, 244, 255, 0.85);
      font-size: 13px;
    }
    &:last-child td.el-table__cell { border-bottom: none !important; }
    &:hover td.el-table__cell { background: rgba(0, 212, 255, 0.04) !important; }
  }
  :deep(.el-table__cell) {
    .el-button { white-space: nowrap !important; }
    .cell { white-space: nowrap; }
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

.items-empty {
  color: #5a7a9a;
  font-size: 13px;
  padding: 16px 0;
}

.material-card {
  border: 1px solid rgba(0, 212, 255, 0.15);
  border-radius: 8px;
  margin-bottom: 14px;
  overflow: hidden;
  background: rgba(0, 30, 60, 0.25);
}

.material-card-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  padding: 8px 14px;
  background: rgba(0, 200, 255, 0.08);
  border-bottom: 1px solid rgba(0, 212, 255, 0.12);
  font-size: 13px;

  .head-mpn {
    color: #c8dff0;
    font-weight: 600;
  }
  .head-quote {
    color: #5a7a9a;
  }
}

.material-card-body {
  padding: 12px 14px 4px;
}

.material-card-actions {
  display: flex;
  justify-content: flex-end;
  padding-bottom: 8px;
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

.subtotal {
  color: #00c8ff;
  font-size: 13px;
}

.line-total-row {
  display: flex;
  justify-content: flex-end;
  align-items: center;
  padding: 8px 0 12px;
  gap: 8px;
}

.line-total-label {
  color: #5a7a9a;
  font-size: 13px;
}

.line-total-amount {
  color: #00c8ff;
  font-size: 14px;
  font-weight: 700;
}
</style>
