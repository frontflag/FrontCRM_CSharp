<template>
  <div class="quote-upsert-page">
    <div class="page-header">
      <div class="header-left">
        <el-button link @click="handleBack">
          <el-icon><ArrowLeft /></el-icon>
          返回列表
        </el-button>
        <div class="page-title">新建报价</div>
      </div>
      <div class="header-right">
        <el-button @click="handleBack">取消</el-button>
        <el-button type="primary" :loading="submitLoading" @click="handleSubmit">
          <el-icon><Check /></el-icon> 保存
        </el-button>
      </div>
    </div>

    <el-card class="form-card" shadow="never">
      <el-alert
        v-if="linkSummary"
        type="info"
        :closable="false"
        show-icon
        class="link-alert"
        :title="linkSummary"
      />
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
            <el-form-item label="业务员" prop="salesUserName">
              <el-input v-model="formData.salesUserName" placeholder="请输入业务员" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="采购员">
              <el-input v-model="formData.purchaseUserName" placeholder="请输入采购员" />
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

            <el-table-column label="单价" width="110">
              <template #default="{ $index }">
                <el-input-number
                  v-model="formData.items[$index].unitPrice"
                  :min="0"
                  :precision="4"
                  :controls="false"
                  style="width: 100%"
                />
              </template>
            </el-table-column>

            <el-table-column label="币别" width="80">
              <template #default="{ $index }">
                <el-select v-model="formData.items[$index].currency" size="small" style="width: 100%">
                  <el-option label="USD" :value="1" />
                  <el-option label="CNY" :value="0" />
                </el-select>
              </template>
            </el-table-column>

            <el-table-column label="交期" width="100">
              <template #default="{ $index }">
                <el-input v-model="formData.items[$index].leadTime" placeholder="交期" size="small" />
              </template>
            </el-table-column>

            <el-table-column label="操作" width="80" align="center">
              <template #default="{ $index }">
                <el-button link type="danger" @click="removeItem($index)">删除</el-button>
              </template>
            </el-table-column>
          </el-table>
        </div>
      </el-form>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ArrowLeft, Check, Plus } from '@element-plus/icons-vue'
import { ElMessage } from 'element-plus'
import { mockQuoteApi as quoteApi } from '@/api/mockQuote'

const route = useRoute()
const router = useRouter()

/** 从需求明细页带入的上下文（后续可对接真实报价接口） */
const rfqLink = computed(() => {
  const rfqId = route.query.rfqId as string | undefined
  const rfqCode = route.query.rfqCode as string | undefined
  const rfqItemId = route.query.rfqItemId as string | undefined
  const raw = route.query.rfqItemIds as string | undefined
  const rfqItemIds = raw ? raw.split(',').map((s) => s.trim()).filter(Boolean) : []
  return { rfqId, rfqCode, rfqItemId, rfqItemIds }
})

const linkSummary = computed(() => {
  const { rfqId, rfqCode, rfqItemId, rfqItemIds } = rfqLink.value
  if (!rfqId) return ''
  const refLabel = rfqCode ? `需求 ${rfqCode}` : `需求 ID：${rfqId}`
  if (rfqItemIds.length > 1)
    return `已关联${refLabel}，明细 ${rfqItemIds.length} 条（批量报价）`
  if (rfqItemId) return `已关联${refLabel}，明细 ID：${rfqItemId}`
  if (rfqItemIds.length === 1) return `已关联${refLabel}，明细 ID：${rfqItemIds[0]}`
  return `已关联${refLabel}`
})

const submitLoading = ref(false)
const formRef = ref()

const formData = ref({
  quoteCode: '',
  mpn: '',
  salesUserName: '',
  purchaseUserName: '',
  remark: '',
  items: [] as any[]
})

const formRules = {
  mpn: [{ required: true, message: '请输入物料型号', trigger: 'blur' }],
  salesUserName: [{ required: true, message: '请输入业务员', trigger: 'blur' }]
}

const handleBack = () => {
  router.push({ name: 'QuoteList' })
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

const handleSubmit = async () => {
  await formRef.value.validate()
  submitLoading.value = true
  try {
    const data = {
      ...formData.value,
      quoteDate: new Date().toISOString().slice(0, 10)
    }
    const res = await quoteApi.create(data)
    const id = res?.data?.id
    if (id) router.push({ name: 'QuoteDetail', params: { id } })
    else router.push({ name: 'QuoteList' })
  } catch (e: any) {
    ElMessage.error(e?.message || '保存失败')
  } finally {
    submitLoading.value = false
  }
}
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

.link-alert {
  margin-bottom: 16px;
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

