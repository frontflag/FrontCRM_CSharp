<template>
  <div class="quote-upsert-page">
    <div class="page-header">
      <div class="header-left">
        <el-button link @click="handleBack">
          <el-icon><ArrowLeft /></el-icon>
          返回详情
        </el-button>
        <div class="page-title">编辑报价</div>
      </div>
      <div class="header-right">
        <el-button @click="handleBack">取消</el-button>
        <el-button type="primary" :loading="submitLoading" @click="handleSubmit">
          <el-icon><Check /></el-icon> 保存
        </el-button>
      </div>
    </div>

    <el-card class="form-card" shadow="never" v-loading="loading" element-loading-background="rgba(10,22,40,0.8)">
      <el-form ref="formRef" :model="formData" :rules="formRules" label-width="100px" class="upsert-form">
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="报价编号">
              <el-input v-model="formData.quoteCode" placeholder="系统自动生成" disabled />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="物料型号" prop="mpn">
              <el-input v-model="formData.mpn" placeholder="请输入MPN" />
            </el-form-item>
          </el-col>
        </el-row>

        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="业务员" prop="salesUserId">
              <SalesUserCascader
                v-model="formData.salesUserId"
                placeholder="请选择业务员"
                @change="onQuoteEditSalesUserChange"
              />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="采购员">
              <PurchaserCascader
                v-model="formData.purchaseUserId"
                placeholder="请选择采购员"
                @change="onQuoteEditPurchaseUserChange"
              />
            </el-form-item>
          </el-col>
        </el-row>

        <el-form-item label="备注">
          <el-input v-model="formData.remark" type="textarea" rows="2" />
        </el-form-item>

        <!-- 供应商报价明细 -->
        <div class="items-section">
          <div class="items-header">
            <h4>供应商报价明细</h4>
            <el-button type="primary" size="small" @click="addItem">
              <el-icon><Plus /></el-icon>添加供应商报价
            </el-button>
          </div>
          <el-table :data="formData.items" border size="small">
            <el-table-column type="index" width="50" />

            <el-table-column label="供应商" min-width="140">
              <template #default="{ $index }">
                <el-input v-model="formData.items[$index].vendorName" placeholder="供应商名称" />
              </template>
            </el-table-column>

            <el-table-column label="联系人" width="120">
              <template #default="{ $index }">
                <el-input v-model="formData.items[$index].contactName" placeholder="联系人" />
              </template>
            </el-table-column>

            <el-table-column label="品牌" width="100">
              <template #default="{ $index }">
                <el-input v-model="formData.items[$index].brand" placeholder="品牌" />
              </template>
            </el-table-column>

            <el-table-column label="数量" width="80">
              <template #default="{ $index }">
                <el-input-number
                  v-model="formData.items[$index].quantity"
                  :min="1"
                  :controls="false"
                  style="width: 100%"
                />
              </template>
            </el-table-column>

            <el-table-column label="单价 / 币别" min-width="200" class-name="quote-item-price-ccy-col">
              <template #default="{ $index }">
                <SettlementCurrencyAmountInput
                  v-model="formData.items[$index].unitPrice"
                  v-model:currency="formData.items[$index].currency"
                  :min="0"
                  :precision="6"
                  size="small"
                  style="width: 100%"
                />
              </template>
            </el-table-column>

            <el-table-column label="交期" width="100">
              <template #default="{ $index }">
                <el-input v-model="formData.items[$index].leadTime" placeholder="交期" size="small" />
              </template>
            </el-table-column>

            <el-table-column label="操作" width="80" align="center" class-name="op-col" label-class-name="op-col">
              <template #default="{ $index }">
                <el-button link type="danger" @click.stop="removeItem($index)">删除</el-button>
              </template>
            </el-table-column>
          </el-table>
        </div>
      </el-form>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { ArrowLeft, Check, Plus } from '@element-plus/icons-vue'
import { ElMessage } from 'element-plus'
import { quoteApi } from '@/api/quote'
import { runValidatedFormSave } from '@/composables/useFormSubmit'
import SalesUserCascader from '@/components/SalesUserCascader.vue'
import SettlementCurrencyAmountInput from '@/components/SettlementCurrencyAmountInput.vue'
import PurchaserCascader from '@/components/PurchaserCascader.vue'

const router = useRouter()
const route = useRoute()

const quoteId = computed(() => route.params.id as string)

function onQuoteEditSalesUserChange(p: { id: string; label: string }) {
  formData.value.salesUserName = p.label || ''
}

function onQuoteEditPurchaseUserChange(p: { id: string; label: string }) {
  formData.value.purchaseUserName = p.label || ''
}

const loading = ref(false)
const submitLoading = ref(false)
const formRef = ref()

const formData = ref({
  quoteCode: '',
  mpn: '',
  salesUserId: '',
  purchaseUserId: '',
  salesUserName: '',
  purchaseUserName: '',
  remark: '',
  items: [] as any[]
})

const formRules = {
  mpn: [{ required: true, message: '请输入物料型号', trigger: 'blur' }],
  salesUserId: [{ required: true, message: '请选择业务员', trigger: 'change' }]
}

const handleBack = () => {
  if (!quoteId.value) router.push({ name: 'QuoteList' })
  else router.push({ name: 'QuoteDetail', params: { id: quoteId.value } })
}

const addItem = () => {
  formData.value.items.push({
    vendorName: '',
    contactName: '',
    brand: '',
    quantity: 1,
    unitPrice: 0,
    currency: 1,
    leadTime: '',
    stockQty: 0
  })
}

const removeItem = (index: number) => {
  formData.value.items.splice(index, 1)
}

const load = async () => {
  if (!quoteId.value) {
    ElMessage.error('无效的报价 ID')
    router.push({ name: 'QuoteList' })
    return
  }
  loading.value = true
  try {
    const res = await quoteApi.getById(quoteId.value)
    const q = res?.data as Record<string, unknown> | undefined
    if (!q) {
      ElMessage.error('报价单不存在')
      router.push({ name: 'QuoteList' })
      return
    }
    formData.value = {
      quoteCode: String(q.quoteCode ?? q.QuoteCode ?? ''),
      mpn: String(q.mpn ?? q.Mpn ?? ''),
      salesUserId: String(q.salesUserId ?? q.SalesUserId ?? ''),
      purchaseUserId: String(q.purchaseUserId ?? q.PurchaseUserId ?? ''),
      salesUserName: String(q.salesUserName ?? ''),
      purchaseUserName: String(q.purchaseUserName ?? ''),
      remark: String(q.remark ?? ''),
      items: q.items ? JSON.parse(JSON.stringify(q.items)) : []
    }
  } catch (e: any) {
    ElMessage.error(e?.message || '加载报价失败')
    router.push({ name: 'QuoteList' })
  } finally {
    loading.value = false
  }
}

const handleSubmit = async () => {
  await runValidatedFormSave(formRef, {
    loading: submitLoading,
    task: async () => {
      const data = {
        ...formData.value,
        quoteDate: new Date().toISOString().slice(0, 10)
      }
      const res = await quoteApi.update(quoteId.value, data)
      return (res?.data as { id?: string } | undefined)?.id
    },
    onSuccess: (id) => {
      if (id) router.push({ name: 'QuoteDetail', params: { id } })
      else router.push({ name: 'QuoteList' })
    }
  })
}

onMounted(load)
</script>

<style scoped lang="scss">
.quote-upsert-page {
  padding: 20px;
}

.page-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 20px;

  .page-title {
    margin: 0;
    color: #E8F4FF;
    font-size: 20px;
    font-weight: 600;
  }
}

.form-card {
  background: #0A1628;
  border: 1px solid rgba(0, 212, 255, 0.1);
}

.items-section {
  margin-top: 20px;
  padding-top: 20px;
  border-top: 1px solid rgba(0, 212, 255, 0.1);

  .items-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 10px;

    h4 {
      margin: 0;
      color: #E8F4FF;
      font-size: 14px;
      font-weight: 600;
    }
  }

  :deep(.quote-item-price-ccy-col .cell) {
    overflow: visible;
  }
}

.upsert-form {
  :deep(.el-input__inner) {
    background: rgba(255, 255, 255, 0.03);
    border-color: rgba(0, 212, 255, 0.2);
    color: #E8F4FF;
  }

  :deep(.el-form-item__label) {
    color: rgba(200, 216, 232, 0.7);
  }
}
</style>

