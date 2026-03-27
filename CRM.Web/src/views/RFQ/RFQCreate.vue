<template>
  <div class="create-page" v-loading="pageLoading">
    <!-- 面包屑 + 操作栏 -->
    <div class="page-header">
      <div class="header-left">
        <el-button link @click="router.back()">
          <el-icon><ArrowLeft /></el-icon> 返回列表
        </el-button>
        <el-breadcrumb separator="/">
          <el-breadcrumb-item>需求管理</el-breadcrumb-item>
          <el-breadcrumb-item>{{ isEditMode ? '编辑需求' : '新建需求' }}</el-breadcrumb-item>
        </el-breadcrumb>
      </div>
      <div class="header-right">
        <el-button @click="router.back()">取消</el-button>
        <el-button type="primary" :loading="submitLoading" @click="handleSubmit">
          <el-icon><Check /></el-icon> {{ isEditMode ? '保存修改' : '保存' }}
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
            <el-form-item label="需求编号">
              <el-input v-model="formData.rfqCode" disabled placeholder="系统自动生成" class="q-input" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="客户" prop="customerId">
              <el-select
                ref="customerSelectRef"
                v-model="formData.customerId"
                placeholder="请输入客户名称搜索"
                style="width: 100%"
                filterable
                :filter-method="onCustomerFilterInput"
                :loading="customerSearchLoading"
                loading-text="搜索中..."
                class="q-select"
                @change="onCustomerChange"
              >
                <template #empty>
                  <div class="customer-search-hint">
                    <span>请输入内容之后选择</span>
                  </div>
                </template>
                <el-option
                  v-for="c in customerOptions"
                  :key="c.value"
                  :label="c.label"
                  :value="c.value"
                />
              </el-select>
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="24">
          <el-col :span="12">
            <el-form-item label="业务员">
              <SalesUserCascader
                v-model="formData.salesUserId"
                placeholder="请选择业务员"
                class="q-input"
                @change="onRfqCreateSalesUserChange"
              />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="联系邮箱">
              <el-input v-model="formData.contactEmail" placeholder="请输入联系邮箱" class="q-input" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="24">
          <el-col :span="12">
            <el-form-item label="产品">
              <el-input v-model="formData.product" placeholder="请输入产品名称" class="q-input" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="行业">
              <el-input v-model="formData.industry" placeholder="请输入行业" class="q-input" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="24">
          <el-col :span="8">
            <el-form-item label="需求类型">
              <el-select v-model="formData.rfqType" style="width: 100%" class="q-select">
                <el-option label="现货" :value="1" />
                <el-option label="期货" :value="2" />
                <el-option label="样品" :value="3" />
                <el-option label="批量" :value="4" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="目标类型">
              <el-select v-model="formData.targetType" style="width: 100%" class="q-select">
                <el-option label="比价需求" :value="1" />
                <el-option label="独家需求" :value="2" />
                <el-option label="紧急需求" :value="3" />
                <el-option label="常规需求" :value="4" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="重要程度">
              <div class="importance-wrap">
                <el-rate
                  v-model="formData.importance"
                  :max="5"
                  :colors="['#C99A45', '#C99A45', '#C99A45']"
                  void-color="rgba(200,216,232,0.2)"
                  class="q-rate"
                />
                <span class="importance-label">{{ formData.importance }} 星</span>
              </div>
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="24">
          <el-col :span="24">
            <el-form-item label="项目背景">
              <el-input v-model="formData.projectBackground" type="textarea" :rows="2" placeholder="请输入项目背景" class="q-input" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="24">
          <el-col :span="12">
            <el-form-item label="竞争对手">
              <el-input v-model="formData.competitor" placeholder="请输入竞争对手" class="q-input" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="备注">
              <el-input v-model="formData.remark" placeholder="请输入备注" class="q-input" />
            </el-form-item>
          </el-col>
        </el-row>
      </div>

      <!-- 需求明细 -->
      <div class="form-section">
        <div class="section-title">
          <span class="title-bar"></span>需求明细
          <el-button type="primary" size="small" class="add-item-btn" @click="addItem">
            <el-icon><Plus /></el-icon> 添加明细
          </el-button>
        </div>
        <el-table :data="formData.items" size="small" class="items-table">
          <el-table-column type="index" width="50" label="#" align="center" />
          <el-table-column label="物料型号(MPN)" min-width="160">
            <template #default="{ $index }">
              <el-input v-model="formData.items[$index].mpn" placeholder="请输入MPN" class="q-input" />
            </template>
          </el-table-column>
          <el-table-column label="品牌" width="120">
            <template #default="{ $index }">
              <el-input v-model="formData.items[$index].brand" placeholder="品牌" class="q-input" />
            </template>
          </el-table-column>
          <el-table-column label="客户料号" width="120">
            <template #default="{ $index }">
              <el-input v-model="formData.items[$index].customerMpn" placeholder="客户料号" class="q-input" />
            </template>
          </el-table-column>
          <el-table-column label="数量" width="110">
            <template #default="{ $index }">
              <el-input-number
                v-model="formData.items[$index].quantity"
                :min="1"
                :controls="false"
                style="width: 100%"
                class="q-number"
              />
            </template>
          </el-table-column>
          <el-table-column label="目标价" width="120">
            <template #default="{ $index }">
              <el-input-number
                v-model="formData.items[$index].targetPrice"
                :min="0"
                :precision="4"
                :controls="false"
                style="width: 100%"
                class="q-number"
              />
            </template>
          </el-table-column>
          <el-table-column label="币别" width="110">
            <template #default="{ $index }">
              <el-select v-model="formData.items[$index].priceCurrency" style="width: 100%" class="q-select">
                <el-option label="RMB" :value="1" />
                <el-option label="USD" :value="2" />
                <el-option label="EUR" :value="3" />
              </el-select>
            </template>
          </el-table-column>
          <el-table-column label="操作" width="80" align="center">
            <template #default="{ $index }">
              <el-button link type="danger" @click="removeItem($index)">删除</el-button>
            </template>
          </el-table-column>
        </el-table>
        <div v-if="formData.items.length === 0" class="empty-hint">
          暂无明细，点击"添加明细"按钮添加
        </div>
      </div>

    </el-form>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { ArrowLeft, Check, Plus } from '@element-plus/icons-vue'
import { rfqApi } from '@/api/rfq'
import type { CreateRFQItemRequest, UpdateRFQRequest } from '@/types/rfq'
import { useAuthStore } from '@/stores/auth'
import { getApiErrorMessage } from '@/utils/apiError'
import { runValidatedFormSave } from '@/composables/useFormSubmit'
import SalesUserCascader from '@/components/SalesUserCascader.vue'

const route = useRoute()
const router = useRouter()
const formRef = ref()
const submitLoading = ref(false)
const pageLoading = ref(false)
const authStore = useAuthStore()

const rfqId = computed(() => {
  const id = route.params.id
  if (Array.isArray(id)) return id[0] || ''
  return String(id || '')
})

const isEditMode = computed(() => route.name === 'RFQEdit' && !!rfqId.value)

// 客户下拉搜索
const customerOptions = ref<{ value: string; label: string }[]>([])
const customerSearchLoading = ref(false)
const customerSelectRef = ref<any>(null)
let customerSearchTimer: ReturnType<typeof setTimeout> | null = null

// 生成需求编号
const getYYMMDD = (d: Date) => {
  const yy = String(d.getFullYear()).slice(-2)
  const mm = String(d.getMonth() + 1).padStart(2, '0')
  const dd = String(d.getDate()).padStart(2, '0')
  return `${yy}${mm}${dd}`
}

const genRfqCode = () => {
  const date = getYYMMDD(new Date())
  const seq = String(Math.floor(Math.random() * 10000)).padStart(4, '0')
  return `RFQ${date}${seq}`
}

const emptyForm = () => ({
  rfqCode: genRfqCode(),
  customerId: '',
  customerName: '',
  salesUserId: '',
  salesUserName: '',
  contactEmail: '',
  product: '',
  industry: '',
  rfqType: 1,
  targetType: 1,
  importance: 3,
  projectBackground: '',
  competitor: '',
  remark: '',
  items: [] as any[]
})

const formData = ref(emptyForm())

function resetFormForCreate() {
  formData.value = emptyForm()
  const user = authStore.user
  if (user) {
    formData.value.salesUserId = user.id || ''
    formData.value.salesUserName = user.userName || ''
  }
}

function normalizeImportance(v: unknown): number {
  const n = Number(v)
  if (!Number.isFinite(n) || n < 1) return 3
  if (n <= 5) return Math.round(n)
  return Math.min(5, Math.max(1, Math.round(n / 2)))
}

function mapCurrencyToPriceCurrency(c?: string | number): number {
  if (typeof c === 'number' && c >= 1 && c <= 4) return c
  const u = String(c || '').toUpperCase()
  if (u.includes('USD')) return 2
  if (u.includes('EUR')) return 3
  if (u.includes('HKD')) return 4
  return 1
}

function mapItemsFromApi(items: any[]) {
  return items.map((raw: any) => ({
    mpn: raw.mpn || raw.materialModel || '',
    brand: raw.brand || '',
    customerMpn: raw.customerMpn || raw.customerMaterialModel || '',
    quantity: raw.quantity ?? 1,
    targetPrice: raw.targetPrice,
    priceCurrency: mapCurrencyToPriceCurrency(raw.priceCurrency ?? raw.currency)
  }))
}

async function loadRfqForEdit() {
  if (!isEditMode.value || !rfqId.value) return
  pageLoading.value = true
  try {
    const data = await rfqApi.getRFQById(rfqId.value)
    const d = data as any
    if (data.customerId) {
      customerOptions.value = [
        { value: data.customerId, label: data.customerName || d.customerName || '客户' }
      ]
    } else {
      customerOptions.value = []
    }
    formData.value = {
      rfqCode: data.rfqCode || '',
      customerId: data.customerId || '',
      customerName: data.customerName || '',
      salesUserId: data.salesUserId || '',
      salesUserName: data.salesUserName || '',
      contactEmail: d.contactEmail || d.contactPersonEmail || '',
      product: data.product || '',
      industry: data.industry || '',
      rfqType: data.rfqType ?? 1,
      targetType: data.targetType ?? 1,
      importance: normalizeImportance(d.importanceLevel ?? d.importance),
      projectBackground: data.projectBackground || '',
      competitor: data.competitor || '',
      remark: data.remark || '',
      items: data.items?.length ? mapItemsFromApi(data.items) : []
    }
  } catch (e) {
    ElMessage.error(getApiErrorMessage(e, '加载需求失败'))
    router.push({ name: 'RFQList' })
  } finally {
    pageLoading.value = false
  }
}

watch(
  () => [route.name, route.params.id] as const,
  async () => {
    if (route.name === 'RFQEdit' && rfqId.value) {
      await loadRfqForEdit()
    } else if (route.name === 'RFQCreate') {
      resetFormForCreate()
    }
  },
  { immediate: true }
)

const formRules = {
  customerId: [{ required: true, message: '请选择客户', trigger: 'change' }]
}

function onRfqCreateSalesUserChange(p: { id: string; label: string }) {
  formData.value.salesUserName = p.label || ''
}

// 客户搜索防抖
async function onCustomerFilterInput(query: string) {
  if (customerSearchTimer) clearTimeout(customerSearchTimer)
  if (!query || query.trim().length < 1) {
    customerOptions.value = []
    return
  }
  customerSearchTimer = setTimeout(async () => {
    customerSearchLoading.value = true
    try {
      const { customerApi } = await import('@/api/customer')
      const res = await customerApi.searchCustomers({
        pageNumber: 1,
        pageSize: 30,
        searchTerm: query.trim()
      })
      customerOptions.value = (res.items || []).map((c: any) => ({
        value: c.id,
        label: c.customerName || (c as any).officialName || c.name || '未知客户'
      }))
    } catch {
      customerOptions.value = []
    } finally {
      customerSearchLoading.value = false
    }
  }, 300)
}

// 客户选择后同步 customerName
function onCustomerChange(val: string) {
  const found = customerOptions.value.find(c => c.value === val)
  if (found) {
    formData.value.customerName = found.label
  }
}

// 添加/删除明细
const addItem = () => {
  formData.value.items.push({
    mpn: '',
    brand: '',
    customerMpn: '',
    quantity: 1,
    targetPrice: undefined,
    priceCurrency: 1
  })
}

const removeItem = (index: number) => {
  formData.value.items.splice(index, 1)
}

function buildItemPayload(): CreateRFQItemRequest[] {
  return formData.value.items.map((it: Record<string, unknown>) => ({
    ...it,
    customerBrand: (it.customerBrand as string) || (it.brand as string) || '',
    brand: (it.brand as string) || '',
    quantity: Math.max(1, Number(it.quantity) || 1)
  })) as CreateRFQItemRequest[]
}

// 提交
const handleSubmit = async () => {
  const editMode = isEditMode.value
  const id = rfqId.value
  await runValidatedFormSave(formRef, {
    loading: submitLoading,
    task: async () => {
      if (editMode && id) {
        const payload: UpdateRFQRequest = {
          customerId: formData.value.customerId,
          contactEmail: formData.value.contactEmail,
          salesUserId: formData.value.salesUserId,
          industry: formData.value.industry,
          product: formData.value.product,
          rfqType: formData.value.rfqType,
          targetType: formData.value.targetType,
          importance: formData.value.importance,
          projectBackground: formData.value.projectBackground,
          competitor: formData.value.competitor,
          remark: formData.value.remark,
          items: buildItemPayload()
        }
        await rfqApi.updateRFQ(id, payload)
        return 'edit' as const
      }
      const data = {
        ...formData.value,
        items: buildItemPayload()
      }
      await rfqApi.createRFQ(data as any)
      return 'create' as const
    },
    formatSuccess: (mode) => (mode === 'edit' ? '需求已更新' : '需求创建成功'),
    onSuccess: () => router.push({ name: 'RFQList' }),
    errorMessage: (e) => getApiErrorMessage(e, editMode ? '保存失败，请重试' : '创建失败，请重试')
  })
}
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

/* RFQCreate.vue — 新建需求独立页面，暗色科技风 */
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

    :deep(.el-button.is-link) {
      color: $text-muted;
      font-size: 13px;
      &:hover { color: $cyan-primary; }
    }
  }

  .header-right {
    display: flex;
    gap: 10px;
  }
}

.create-form {
  .form-section {
    background: $layer-2;
    border: 1px solid $border-card;
    border-radius: $border-radius-md;
    padding: 20px 24px;
    margin-bottom: 16px;
  }

  .section-title {
    display: flex;
    align-items: center;
    gap: 8px;
    font-size: 14px;
    font-weight: 600;
    color: $text-primary;
    margin-bottom: 20px;

    .title-bar {
      width: 3px;
      height: 16px;
      background: linear-gradient(180deg, $cyan-primary, $blue-primary);
      border-radius: 2px;
    }

    .add-item-btn {
      margin-left: auto;
    }
  }

  :deep(.el-form-item__label) {
    color: $text-muted;
    font-size: 13px;
  }
}

// 输入框统一暗色风格（参考 CustomerEdit.vue）
.q-input {
  :deep(.el-input__wrapper),
  :deep(.el-textarea__inner) {
    background-color: $layer-3 !important;
    border: 1px solid $border-panel !important;
    border-radius: $border-radius-md !important;
    box-shadow: none !important;
    color: $text-primary !important;
    transition: border-color 0.2s;
    &:hover { border-color: rgba(0, 212, 255, 0.25) !important; }
    &.is-focus { border-color: rgba(0, 212, 255, 0.5) !important; box-shadow: 0 0 0 2px rgba(0,212,255,0.08) !important; }
  }
  :deep(.el-input__inner) {
    color: $text-primary !important;
    background: transparent !important;
    font-size: 13px;
    &::placeholder { color: $text-placeholder !important; }
  }
  :deep(.el-textarea__inner) {
    font-size: 13px;
    &::placeholder { color: $text-placeholder !important; }
  }
  :deep(.el-input__wrapper.is-disabled) {
    opacity: 0.5;
  }
}

.q-select {
  :deep(.el-select__wrapper) {
    background-color: $layer-3 !important;
    border: 1px solid $border-panel !important;
    border-radius: $border-radius-md !important;
    box-shadow: none !important;
    color: $text-primary !important;
    &.is-focused { border-color: rgba(0, 212, 255, 0.5) !important; }
  }
  :deep(.el-select__placeholder) { color: $text-placeholder !important; }
  :deep(.el-select__selected-item) { color: $text-primary !important; }
}

.q-number {
  :deep(.el-input__wrapper) {
    background-color: $layer-3 !important;
    border: 1px solid $border-panel !important;
    border-radius: $border-radius-md !important;
    box-shadow: none !important;
    &.is-focus { border-color: rgba(0, 212, 255, 0.5) !important; }
  }
  :deep(.el-input__inner) {
    color: $text-primary !important;
    background: transparent !important;
  }
}

// 重要程度星级
.importance-wrap {
  display: flex;
  align-items: center;
  gap: 10px;
}

.q-rate {
  :deep(.el-rate__icon) {
    font-size: 20px;
  }
}

.importance-label {
  color: $color-amber;
  font-size: 13px;
  font-weight: 500;
  min-width: 32px;
}

// 客户搜索提示
.customer-search-hint {
  padding: 8px 12px;
  color: $text-muted;
  font-size: 12px;
  text-align: center;
}

// 明细表格
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
}

.empty-hint {
  text-align: center;
  padding: 20px 0;
  color: $text-muted;
  font-size: 13px;
}
</style>
