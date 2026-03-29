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
    <el-form ref="formRef" :model="formData" :rules="formRules" label-width="108px" class="create-form">

      <!-- 基础信息 -->
      <div class="form-section">
        <div class="section-title">
          <span class="title-bar"></span>基础信息
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
            <el-form-item label="客户联系人">
              <el-select
                v-model="formData.contactId"
                placeholder="请先选择客户"
                clearable
                filterable
                style="width: 100%"
                class="q-select"
                :disabled="!formData.customerId"
                @change="onContactChange"
              >
                <el-option
                  v-for="c in contactOptions"
                  :key="c.value"
                  :label="c.label"
                  :value="c.value"
                />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="联系人邮箱">
              <el-input v-model="formData.contactEmail" placeholder="选择联系人可自动带出，也可手填" class="q-input" />
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
        </el-row>
      </div>

      <!-- 需求信息 -->
      <div class="form-section">
        <div class="section-title">
          <span class="title-bar"></span>需求信息
        </div>
        <el-row :gutter="24">
          <el-col :span="8">
            <el-form-item label="需求类型" prop="rfqType">
              <el-select
                v-model="formData.rfqType"
                placeholder="请选择需求类型"
                style="width: 100%"
                class="q-select"
              >
                <el-option v-for="o in RFQ_TYPE_OPTIONS" :key="o.value" :label="o.label" :value="o.value" />
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
            <el-form-item prop="quoteMethod">
              <template #label>
                <span class="rfq-field-label">
                  报价方式
                  <el-tooltip content="选择报价结果与通知的触达方式（系统推送 / 邮件 / 短信等）" placement="top">
                    <el-icon class="rfq-label-help" aria-hidden="true"><QuestionFilled /></el-icon>
                  </el-tooltip>
                </span>
              </template>
              <el-select
                v-model="formData.quoteMethod"
                placeholder="请选择报价方式"
                style="width: 100%"
                class="q-select"
              >
                <el-option v-for="o in QUOTE_METHOD_OPTIONS" :key="o.value" :label="o.label" :value="o.value" />
              </el-select>
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="24">
          <el-col :span="8">
            <el-form-item prop="assignMethod">
              <template #label>
                <span class="rfq-field-label">
                  分配方式
                  <el-tooltip content="询价明细如何分配给采购员（同一采购、多人、按品牌或指定人员）" placement="top">
                    <el-icon class="rfq-label-help" aria-hidden="true"><QuestionFilled /></el-icon>
                  </el-tooltip>
                </span>
              </template>
              <el-select
                v-model="formData.assignMethod"
                placeholder="请选择分配方式"
                style="width: 100%"
                class="q-select"
              >
                <el-option v-for="o in ASSIGN_METHOD_OPTIONS" :key="o.value" :label="o.label" :value="o.value" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="行业">
              <el-input v-model="formData.industry" placeholder="请输入行业" class="q-input" />
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="产品">
              <el-input v-model="formData.product" placeholder="请输入产品" class="q-input" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="24" class="rfq-row-bg-comp-importance">
          <el-col :span="8">
            <el-form-item label="背景">
              <el-input v-model="formData.projectBackground" type="textarea" :rows="2" placeholder="请输入背景" class="q-input" />
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="竞争对手">
              <el-input v-model="formData.competitor" placeholder="请输入竞争对手" class="q-input" />
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="重要程度" class="importance-inline-item">
              <el-rate
                v-model="formData.importance"
                :max="5"
                :colors="['#C99A45', '#C99A45', '#C99A45']"
                void-color="rgba(200,216,232,0.2)"
                class="q-rate"
              />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="24">
          <el-col :span="24">
            <el-form-item label="备注">
              <el-input v-model="formData.remark" type="textarea" :rows="2" placeholder="请输入备注" class="q-input" />
            </el-form-item>
          </el-col>
        </el-row>
      </div>

      <!-- 物料明细 -->
      <div class="form-section">
        <div class="section-title">
          <span class="title-bar"></span>物料明细
          <el-button type="primary" size="small" class="add-item-btn" @click="addItem">
            <el-icon><Plus /></el-icon> 添加明细
          </el-button>
        </div>
        <div class="items-table-wrap">
          <el-table :data="formData.items" size="small" class="items-table">
            <el-table-column label="客户物料型号" min-width="130">
              <template #default="{ $index }">
                <el-input v-model="formData.items[$index].customerMpn" placeholder="客户物料型号" class="q-input" />
              </template>
            </el-table-column>
            <el-table-column label="客户品牌" min-width="100">
              <template #default="{ $index }">
                <el-input v-model="formData.items[$index].customerBrand" placeholder="客户品牌" class="q-input" />
              </template>
            </el-table-column>
            <el-table-column label="物料型号" min-width="140">
              <template #default="{ $index }">
                <el-input v-model="formData.items[$index].mpn" placeholder="物料型号(MPN)" class="q-input" />
              </template>
            </el-table-column>
            <el-table-column label="品牌" min-width="100">
              <template #default="{ $index }">
                <el-input v-model="formData.items[$index].brand" placeholder="品牌" class="q-input" />
              </template>
            </el-table-column>
            <el-table-column label="目标价" width="118">
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
            <el-table-column label="数量" width="100">
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
            <el-table-column label="生产日期" min-width="120">
              <template #default="{ $index }">
                <el-input v-model="formData.items[$index].productionDate" placeholder="如 2年内、DC" class="q-input" />
              </template>
            </el-table-column>
            <el-table-column label="失效日期" width="138">
              <template #default="{ $index }">
                <el-date-picker
                  v-model="formData.items[$index].expiryDate"
                  type="date"
                  placeholder="选择日期"
                  value-format="YYYY-MM-DD"
                  style="width: 100%"
                  class="q-date"
                />
              </template>
            </el-table-column>
            <el-table-column label="最小包装" width="100">
              <template #default="{ $index }">
                <el-input-number
                  v-model="formData.items[$index].minPackageQty"
                  :min="0"
                  :controls="false"
                  style="width: 100%"
                  class="q-number"
                />
              </template>
            </el-table-column>
            <el-table-column label="最小起订量" width="100">
              <template #default="{ $index }">
                <el-input-number
                  v-model="formData.items[$index].minOrderQty"
                  :min="0"
                  :controls="false"
                  style="width: 100%"
                  class="q-number"
                />
              </template>
            </el-table-column>
            <el-table-column label="可替代料" min-width="120">
              <template #default="{ $index }">
                <el-input v-model="formData.items[$index].alternativeMaterials" placeholder="逗号分隔" class="q-input" />
              </template>
            </el-table-column>
            <el-table-column label="备注" min-width="100">
              <template #default="{ $index }">
                <el-input v-model="formData.items[$index].remark" placeholder="备注" class="q-input" />
              </template>
            </el-table-column>
            <el-table-column label="操作" width="72" align="center" fixed="right" class-name="op-col" label-class-name="op-col">
              <template #default="{ $index }">
                <el-button link type="danger" @click.stop="removeItem($index)">删除</el-button>
              </template>
            </el-table-column>
          </el-table>
        </div>
        <div v-if="formData.items.length === 0" class="empty-hint">
          暂无明细，点击「添加明细」添加
        </div>
      </div>

    </el-form>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { ArrowLeft, Check, Plus, QuestionFilled } from '@element-plus/icons-vue'
import { rfqApi } from '@/api/rfq'
import { customerApi, customerContactApi } from '@/api/customer'
import type { CreateRFQItemRequest, CreateRFQRequest, UpdateRFQRequest } from '@/types/rfq'
import { useAuthStore } from '@/stores/auth'
import { getApiErrorMessage } from '@/utils/apiError'
import { runValidatedFormSave } from '@/composables/useFormSubmit'
import SalesUserCascader from '@/components/SalesUserCascader.vue'
import {
  RFQ_TYPE_OPTIONS,
  QUOTE_METHOD_OPTIONS,
  ASSIGN_METHOD_OPTIONS
} from '@/constants/rfqFormEnums'

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

const contactOptions = ref<{ value: string; label: string; email?: string }[]>([])

async function loadContactsForCustomer(customerId: string) {
  if (!customerId) {
    contactOptions.value = []
    return
  }
  try {
    const list = await customerContactApi.getContactsByCustomerId(customerId)
    contactOptions.value = (list || []).map((c: any) => ({
      value: c.id,
      label: c.contactName || c.name || '联系人',
      email: c.email || undefined
    }))
  } catch {
    contactOptions.value = []
  }
}

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
  contactId: '' as string,
  salesUserId: '',
  salesUserName: '',
  contactEmail: '',
  product: '',
  industry: '',
  rfqType: 1,
  targetType: 1,
  quoteMethod: 2,
  assignMethod: 2,
  importance: 3,
  projectBackground: '',
  competitor: '',
  remark: '',
  items: [] as any[]
})

const formData = ref(emptyForm())

function resetFormForCreate() {
  formData.value = emptyForm()
  contactOptions.value = []
  const user = authStore.user
  if (user) {
    formData.value.salesUserId = user.id || ''
    formData.value.salesUserName = user.userName || ''
  }
}

/** 从路由 query（如客户详情「创建需求」）预填客户下拉与 customerId */
async function applyPrefillCustomerFromQuery() {
  const raw = route.query.customerId
  const cid = Array.isArray(raw) ? raw[0] : raw
  if (!cid || typeof cid !== 'string') return
  try {
    const c = await customerApi.getCustomerById(cid)
    const name =
      c.customerName ||
      (c as any).officialName ||
      c.customerShortName ||
      (c as any).nickName ||
      c.customerCode ||
      '客户'
    const id = String(c.id)
    customerOptions.value = [{ value: id, label: name }]
    formData.value.customerId = id
    formData.value.customerName = name
    await loadContactsForCustomer(id)
  } catch {
    ElMessage.warning('无法加载预选客户，请在「客户」中搜索选择')
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

function formatExpiryForPicker(v: unknown): string {
  if (v == null || v === '') return ''
  if (typeof v === 'string') return v.length >= 10 ? v.slice(0, 10) : v
  const d = v as Date
  if (d instanceof Date && !Number.isNaN(d.getTime())) {
    const y = d.getFullYear()
    const m = String(d.getMonth() + 1).padStart(2, '0')
    const day = String(d.getDate()).padStart(2, '0')
    return `${y}-${m}-${day}`
  }
  return ''
}

function mapItemsFromApi(items: any[]) {
  return items.map((raw: any) => ({
    customerMpn: raw.customerMpn || raw.customerMaterialModel || '',
    customerBrand: raw.customerBrand || '',
    mpn: raw.mpn || raw.materialModel || '',
    brand: raw.brand || '',
    quantity: raw.quantity ?? 1,
    targetPrice: raw.targetPrice,
    productionDate: raw.productionDate || '',
    expiryDate: formatExpiryForPicker(raw.expiryDate),
    minPackageQty: raw.minPackageQty != null ? Number(raw.minPackageQty) : undefined,
    minOrderQty: raw.moq != null ? Number(raw.moq) : raw.minOrderQty != null ? Number(raw.minOrderQty) : undefined,
    alternativeMaterials: raw.alternatives || raw.alternativeMaterials || '',
    remark: raw.remark || '',
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
    await loadContactsForCustomer(data.customerId || '')
    formData.value = {
      rfqCode: data.rfqCode || '',
      customerId: data.customerId || '',
      customerName: data.customerName || '',
      contactId: (d.contactId || d.contactPersonId || '') as string,
      salesUserId: data.salesUserId || '',
      salesUserName: data.salesUserName || '',
      contactEmail: d.contactEmail || d.contactPersonEmail || '',
      product: data.product || '',
      industry: data.industry || '',
      rfqType: data.rfqType ?? 1,
      targetType: data.targetType ?? 1,
      quoteMethod: d.quoteMethod ?? 2,
      assignMethod: d.assignMethod ?? 2,
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
  () => [route.name, route.params.id, route.query.customerId] as const,
  async () => {
    if (route.name === 'RFQEdit' && rfqId.value) {
      await loadRfqForEdit()
    } else if (route.name === 'RFQCreate') {
      resetFormForCreate()
      await applyPrefillCustomerFromQuery()
    }
  },
  { immediate: true }
)

const formRules = {
  customerId: [{ required: true, message: '请选择客户', trigger: 'change' }],
  rfqType: [{ required: true, message: '请选择需求类型', trigger: 'change' }],
  quoteMethod: [{ required: true, message: '请选择报价方式', trigger: 'change' }],
  assignMethod: [{ required: true, message: '请选择分配方式', trigger: 'change' }]
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

// 客户选择后同步 customerName、联系人列表
async function onCustomerChange(val: string) {
  const found = customerOptions.value.find(c => c.value === val)
  if (found) {
    formData.value.customerName = found.label
  }
  formData.value.contactId = ''
  await loadContactsForCustomer(val || '')
}

function onContactChange(contactId: string) {
  if (!contactId) return
  const row = contactOptions.value.find(c => c.value === contactId)
  if (row?.email) {
    formData.value.contactEmail = row.email
  }
}

// 添加/删除明细
const addItem = () => {
  formData.value.items.push({
    customerMpn: '',
    customerBrand: '',
    mpn: '',
    brand: '',
    quantity: 1,
    targetPrice: undefined,
    productionDate: '',
    expiryDate: '',
    minPackageQty: undefined,
    minOrderQty: undefined,
    alternativeMaterials: '',
    remark: '',
    priceCurrency: 1
  })
}

const removeItem = (index: number) => {
  formData.value.items.splice(index, 1)
}

function buildItemPayload(): CreateRFQItemRequest[] {
  return formData.value.items.map((it: any, idx: number) => {
    const qty = Math.max(1, Number(it.quantity) || 1)
    const moq =
      it.minOrderQty != null && it.minOrderQty !== ''
        ? Number(it.minOrderQty)
        : undefined
    const minPkg =
      it.minPackageQty != null && it.minPackageQty !== ''
        ? Number(it.minPackageQty)
        : undefined
    const expiryRaw = it.expiryDate
    const expiryDate =
      expiryRaw && typeof expiryRaw === 'string'
        ? expiryRaw
        : undefined
    return {
      lineNo: idx + 1,
      customerMpn: (it.customerMpn || '').trim() || undefined,
      mpn: (it.mpn || '').trim(),
      customerBrand: (it.customerBrand || '').trim(),
      brand: (it.brand || '').trim(),
      targetPrice: it.targetPrice != null ? Number(it.targetPrice) : undefined,
      priceCurrency: Number(it.priceCurrency) || 1,
      quantity: qty,
      productionDate: (it.productionDate || '').trim() || undefined,
      expiryDate,
      minPackageQty: minPkg,
      moq: moq,
      alternatives: (it.alternativeMaterials || '').trim() || undefined,
      remark: (it.remark || '').trim() || undefined
    } as CreateRFQItemRequest
  })
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
          contactId: formData.value.contactId || undefined,
          contactEmail: formData.value.contactEmail,
          salesUserId: formData.value.salesUserId,
          industry: formData.value.industry,
          product: formData.value.product,
          rfqType: formData.value.rfqType,
          targetType: formData.value.targetType,
          quoteMethod: formData.value.quoteMethod,
          assignMethod: formData.value.assignMethod,
          importance: formData.value.importance,
          projectBackground: formData.value.projectBackground,
          competitor: formData.value.competitor,
          remark: formData.value.remark,
          items: buildItemPayload()
        }
        await rfqApi.updateRFQ(id, payload)
        return 'edit' as const
      }
      const createPayload: CreateRFQRequest = {
        customerId: formData.value.customerId,
        contactId: formData.value.contactId || undefined,
        contactEmail: formData.value.contactEmail,
        salesUserId: formData.value.salesUserId,
        rfqType: formData.value.rfqType,
        quoteMethod: formData.value.quoteMethod,
        assignMethod: formData.value.assignMethod,
        industry: formData.value.industry,
        product: formData.value.product,
        targetType: formData.value.targetType,
        importance: formData.value.importance,
        projectBackground: formData.value.projectBackground,
        competitor: formData.value.competitor,
        remark: formData.value.remark,
        items: buildItemPayload()
      }
      await rfqApi.createRFQ(createPayload)
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

  .rfq-field-label {
    display: inline-flex;
    align-items: center;
    gap: 4px;
  }
  .rfq-label-help {
    font-size: 14px;
    color: rgba(0, 212, 255, 0.55);
    cursor: help;
    vertical-align: middle;
  }

  /* 背景 / 竞争对手 / 重要程度 同行：星级与单行输入区垂直对齐 */
  .rfq-row-bg-comp-importance {
    align-items: flex-start;
    .importance-inline-item :deep(.el-form-item__content) {
      padding-top: 6px;
    }
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

.q-rate {
  :deep(.el-rate__icon) {
    font-size: 20px;
  }
}

// 客户搜索提示
.customer-search-hint {
  padding: 8px 12px;
  color: $text-muted;
  font-size: 12px;
  text-align: center;
}

.items-table-wrap {
  width: 100%;
  overflow-x: auto;
}

.q-date {
  :deep(.el-input__wrapper) {
    background-color: $layer-3 !important;
    border: 1px solid $border-panel !important;
    border-radius: $border-radius-md !important;
    box-shadow: none !important;
    &.is-focus { border-color: rgba(0, 212, 255, 0.5) !important; }
  }
  :deep(.el-input__inner) {
    color: $text-primary !important;
  }
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
