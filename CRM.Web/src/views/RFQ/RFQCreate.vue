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
                :placeholder="contactSelectPlaceholder"
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
                  <el-tooltip content="询价明细按采购轮询分配给采购员" placement="top">
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
              <el-select
                v-model="formData.industry"
                placeholder="请选择或输入行业"
                style="width: 100%"
                class="q-select"
                filterable
                allow-create
                default-first-option
                clearable
              >
                <el-option
                  v-for="opt in customerDict.industryOptions"
                  :key="opt.value"
                  :label="opt.label"
                  :value="opt.label"
                />
              </el-select>
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
                :max="3"
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

      <!-- 物料明细：面板（默认） / 列表 可切换 -->
      <div class="form-section">
        <div class="section-title section-title--items">
          <div class="section-title__left">
            <span class="title-bar"></span>物料明细
          </div>
          <div class="section-title__right">
            <el-radio-group v-model="materialItemsViewMode" size="small" class="items-view-toggle">
              <el-radio-button label="panel">面板</el-radio-button>
              <el-radio-button label="list">列表</el-radio-button>
            </el-radio-group>
            <el-button type="primary" size="small" class="add-item-btn" @click="addItem">
              <el-icon><Plus /></el-icon> 添加明细
            </el-button>
          </div>
        </div>

        <!-- 面板：每行 4 个字段（span=6） -->
        <div v-if="materialItemsViewMode === 'panel' && formData.items.length > 0" class="items-panel-list">
          <div
            v-for="(row, idx) in formData.items"
            :key="'panel-' + idx"
            class="item-panel-card"
          >
            <div class="item-panel-card__head">
              <span class="item-panel-card__idx">明细 {{ idx + 1 }}</span>
              <el-button link type="danger" @click.stop="removeItem(idx)">删除</el-button>
            </div>
            <el-row :gutter="16" class="item-panel-row">
              <el-col :span="6">
                <div class="item-panel-field">
                  <div class="item-panel-field__label">客户物料型号</div>
                  <el-input v-model="row.customerMpn" placeholder="客户物料型号" class="q-input" />
                </div>
              </el-col>
              <el-col :span="6">
                <div class="item-panel-field">
                  <div class="item-panel-field__label">客户品牌</div>
                  <el-input v-model="row.customerBrand" placeholder="客户品牌" class="q-input" />
                </div>
              </el-col>
              <el-col :span="6">
                <div class="item-panel-field">
                  <div class="item-panel-field__label">物料型号</div>
                  <el-input v-model="row.mpn" placeholder="物料型号(MPN)" class="q-input" />
                </div>
              </el-col>
              <el-col :span="6">
                <div class="item-panel-field">
                  <div class="item-panel-field__label">品牌</div>
                  <el-input v-model="row.brand" placeholder="品牌" class="q-input" />
                </div>
              </el-col>
            </el-row>
            <el-row :gutter="16" class="item-panel-row">
              <el-col :span="6">
                <div class="item-panel-field">
                  <div class="item-panel-field__label">目标价 / 币别</div>
                  <SettlementCurrencyAmountInput
                    v-model="row.targetPrice"
                    v-model:currency="row.priceCurrency"
                    :min="0"
                    :precision="6"
                    class="q-number rfq-target-price-ccy"
                  />
                </div>
              </el-col>
              <el-col :span="6">
                <div class="item-panel-field">
                  <div class="item-panel-field__label">数量</div>
                  <el-input-number
                    v-model="row.quantity"
                    :min="1"
                    :controls="false"
                    style="width: 100%"
                    class="q-number"
                  />
                </div>
              </el-col>
              <el-col :span="6">
                <div class="item-panel-field">
                  <div class="item-panel-field__label">生产日期</div>
                  <MaterialProductionDateSelect v-model="row.productionDate" select-class="q-select" />
                </div>
              </el-col>
              <el-col :span="6">
                <div class="item-panel-field">
                  <div class="item-panel-field__label">失效日期</div>
                  <el-date-picker
                    v-model="row.expiryDate"
                    type="date"
                    placeholder="选择日期"
                    value-format="YYYY-MM-DD"
                    style="width: 100%"
                    class="q-date"
                  />
                </div>
              </el-col>
            </el-row>
            <el-row :gutter="16" class="item-panel-row">
              <el-col :span="8">
                <div class="item-panel-field">
                  <div class="item-panel-field__label">最小包装（PCS）</div>
                  <el-input-number
                    v-model="row.minPackageQty"
                    :min="0"
                    :controls="false"
                    style="width: 100%"
                    class="q-number"
                  />
                </div>
              </el-col>
              <el-col :span="8">
                <div class="item-panel-field">
                  <div class="item-panel-field__label">最小起订量（PCS）</div>
                  <el-input-number
                    v-model="row.minOrderQty"
                    :min="0"
                    :controls="false"
                    style="width: 100%"
                    class="q-number"
                  />
                </div>
              </el-col>
              <el-col :span="8">
                <div class="item-panel-field">
                  <div class="item-panel-field__label">可替代料</div>
                  <el-input v-model="row.alternativeMaterials" placeholder="逗号分隔" class="q-input" />
                </div>
              </el-col>
            </el-row>
            <el-row :gutter="16" class="item-panel-row">
              <el-col :span="24">
                <div class="item-panel-field item-panel-field--remark">
                  <div class="item-panel-field__label">备注</div>
                  <el-input
                    v-model="row.remark"
                    type="textarea"
                    :rows="2"
                    placeholder="备注"
                    class="q-input"
                  />
                </div>
              </el-col>
            </el-row>
          </div>
        </div>

        <!-- 列表：横向表格 -->
        <div v-if="materialItemsViewMode === 'list' && formData.items.length > 0" class="items-table-wrap">
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
            <el-table-column label="目标价 / 币别" min-width="200" class-name="rfq-table-target-ccy-col">
              <template #default="{ $index }">
                <SettlementCurrencyAmountInput
                  v-model="formData.items[$index].targetPrice"
                  v-model:currency="formData.items[$index].priceCurrency"
                  :min="0"
                  :precision="6"
                  size="small"
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
            <el-table-column label="生产日期" min-width="140">
              <template #default="{ $index }">
                <MaterialProductionDateSelect v-model="formData.items[$index].productionDate" select-class="q-select" />
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
            <el-table-column label="最小包装（PCS）" width="100">
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
            <el-table-column label="最小起订量（PCS）" width="100">
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
import MaterialProductionDateSelect from '@/components/MaterialProductionDateSelect.vue'
import SettlementCurrencyAmountInput from '@/components/SettlementCurrencyAmountInput.vue'
import { useMaterialProductionDateDict } from '@/composables/useMaterialProductionDateDict'
import { useCustomerDictStore } from '@/stores/customerDict'

const route = useRoute()
const router = useRouter()
const formRef = ref()
const submitLoading = ref(false)
const pageLoading = ref(false)
const authStore = useAuthStore()
const { ensureLoaded: ensureMaterialPdDict, defaultCode: defaultProductionDateCode, coerceProductionDateToCode: coercePd } =
  useMaterialProductionDateDict()
const customerDict = useCustomerDictStore()

/** 物料明细展示：面板（默认，每行 4 字段） / 列表（横向表格） */
const materialItemsViewMode = ref<'panel' | 'list'>('panel')

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

const contactOptions = ref<
  { value: string; label: string; email?: string; isDefault?: boolean }[]
>([])

const contactSelectPlaceholder = computed(() =>
  formData.value.customerId ? '请选择联系人' : '请先选择客户'
)

function contactEmailFromRaw(c: Record<string, unknown>): string {
  const v = c.email ?? c.Email
  return typeof v === 'string' ? v.trim() : ''
}

/** 从客户主档带出行业（新建客户在客户资料中维护的行业） */
function pickIndustryFromCustomerRecord(c: Record<string, unknown>): string {
  const raw = c.industry ?? c.Industry
  return typeof raw === 'string' ? raw.trim() : raw != null ? String(raw).trim() : ''
}

async function applyIndustryFromCustomer(customerId: string) {
  const id = customerId?.trim()
  if (!id) {
    formData.value.industry = ''
    return
  }
  try {
    const c = await customerApi.getCustomerById(id)
    const ext = c as unknown as Record<string, unknown>
    const fromCustomer = pickIndustryFromCustomerRecord(ext)
    formData.value.industry = await customerDict.resolveIndustryStorageLabel(fromCustomer || undefined)
  } catch {
    /* 接口失败时不覆盖用户已填行业 */
  }
}

/** 拉取联系人；切换客户时由 watch(customerId) 触发，保证与 el-select 同步 */
async function loadContactsForCustomer(customerId: string) {
  if (!customerId) {
    contactOptions.value = []
    return
  }
  try {
    const list = await customerContactApi.getContactsByCustomerId(customerId)
    const rows = Array.isArray(list) ? list : []
    contactOptions.value = rows
      .map((c: any) => {
        const raw = c as Record<string, unknown>
        const id = String(c.id ?? raw.contactId ?? '').trim()
        if (!id) return null
        const email = contactEmailFromRaw(raw)
        return {
          value: id,
          label: String(c.contactName ?? c.name ?? '联系人'),
          email: email || undefined,
          isDefault: !!(c.isDefault ?? c.isMain)
        }
      })
      .filter(Boolean) as { value: string; label: string; email?: string; isDefault?: boolean }[]
  } catch {
    contactOptions.value = []
  }
}

/** 未选手动联系人时，默认选主联系人/第一条并带出邮箱 */
function applyDefaultContactAndEmail() {
  if (formData.value.contactId) return
  const opts = contactOptions.value
  if (!opts.length) return
  const preferred = opts.find((o) => o.isDefault) || opts[0]
  if (!preferred) return
  formData.value.contactId = preferred.value
  if (preferred.email) {
    formData.value.contactEmail = preferred.email
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

/** 一条空白物料明细（新建默认 1 条，与「添加明细」结构一致） */
function createEmptyRfqItem() {
  return {
    customerMpn: '',
    customerBrand: '',
    mpn: '',
    brand: '',
    quantity: 1,
    targetPrice: undefined,
    productionDate: defaultProductionDateCode(),
    expiryDate: '',
    minPackageQty: undefined,
    minOrderQty: undefined,
    alternativeMaterials: '',
    remark: '',
    priceCurrency: 1
  }
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
  importance: 1,
  projectBackground: '',
  competitor: '',
  remark: '',
  items: [] as any[]
})

const formData = ref(emptyForm())

function resetFormForCreate() {
  formData.value = {
    ...emptyForm(),
    items: [createEmptyRfqItem()]
  }
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
    const ext = c as unknown as Record<string, unknown>
    const fromCustomer = pickIndustryFromCustomerRecord(ext)
    formData.value.industry = await customerDict.resolveIndustryStorageLabel(fromCustomer || undefined)
  } catch {
    ElMessage.warning('无法加载预选客户，请在「客户」中搜索选择')
  }
}

/** 重要程度：界面为 1–3 星；兼容历史 1–5 星或约 1–10 的存盘值 */
function normalizeImportance(v: unknown): number {
  const n = Number(v)
  if (!Number.isFinite(n) || n < 1) return 1
  if (n <= 3) return Math.round(n)
  if (n <= 5) return Math.min(3, Math.max(1, Math.round(n)))
  return Math.max(1, Math.min(3, Math.round((n * 3) / 10)))
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
    productionDate: coercePd(raw.productionDate || ''),
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
      industry: '',
      rfqType: data.rfqType ?? 1,
      targetType: data.targetType ?? 1,
      quoteMethod: d.quoteMethod ?? 2,
      assignMethod: 2,
      importance: normalizeImportance(d.importanceLevel ?? d.importance),
      projectBackground: data.projectBackground || '',
      competitor: data.competitor || '',
      remark: data.remark || '',
      items: data.items?.length ? mapItemsFromApi(data.items) : []
    }
    formData.value.industry = await customerDict.resolveIndustryStorageLabel(data.industry || '')
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
    await ensureMaterialPdDict()
    await customerDict.ensureLoaded()
    if (route.name === 'RFQEdit' && rfqId.value) {
      await loadRfqForEdit()
    } else if (route.name === 'RFQCreate') {
      resetFormForCreate()
      await applyPrefillCustomerFromQuery()
    }
  },
  { immediate: true }
)

watch(
  () => formData.value.customerId,
  async (id, oldId) => {
    if (!id) {
      contactOptions.value = []
      formData.value.contactId = ''
      formData.value.contactEmail = ''
      formData.value.industry = ''
      return
    }
    if (oldId && oldId !== id) {
      formData.value.contactId = ''
      formData.value.contactEmail = ''
    }
    const found = customerOptions.value.find((c) => c.value === id)
    if (found) {
      formData.value.customerName = found.label
    }
    await Promise.all([loadContactsForCustomer(id), applyIndustryFromCustomer(id)])
    applyDefaultContactAndEmail()
  }
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

function onContactChange(contactId: string | null | undefined) {
  if (!contactId) {
    formData.value.contactEmail = ''
    return
  }
  const row = contactOptions.value.find((c) => c.value === contactId)
  if (row?.email) {
    formData.value.contactEmail = row.email
  }
}

// 添加/删除明细
const addItem = () => {
  formData.value.items.push(createEmptyRfqItem())
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

    &--items {
      flex-wrap: wrap;
      justify-content: space-between;
      gap: 12px;

      .section-title__left {
        display: flex;
        align-items: center;
        gap: 8px;
      }

      .section-title__right {
        display: flex;
        align-items: center;
        gap: 10px;
        flex-wrap: wrap;
        margin-left: auto;
      }
    }
  }

  .items-view-toggle {
    :deep(.el-radio-button__inner) {
      background: $layer-3;
      border-color: $border-panel;
      color: $text-muted;
      font-size: 12px;
      padding: 5px 12px;
    }
    :deep(.el-radio-button__original-radio:checked + .el-radio-button__inner) {
      background: rgba(0, 212, 255, 0.12);
      border-color: rgba(0, 212, 255, 0.45);
      color: $cyan-primary;
      box-shadow: none;
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

.items-panel-list {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.item-panel-card {
  background: rgba(0, 212, 255, 0.028);
  border: 1px solid rgba(0, 212, 255, 0.14);
  border-radius: $border-radius-md;
  padding: 14px 16px 16px;
}

.item-panel-card__head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 12px;
  padding-bottom: 8px;
  border-bottom: 1px solid rgba(255, 255, 255, 0.06);
}

.item-panel-card__idx {
  font-size: 12px;
  font-weight: 600;
  color: rgba(200, 216, 232, 0.75);
  letter-spacing: 0.3px;
}

.item-panel-row {
  margin-bottom: 4px;
  &:last-child {
    margin-bottom: 0;
  }
}

.item-panel-field {
  margin-bottom: 10px;
  min-width: 0;
}

.item-panel-field__label {
  font-size: 12px;
  color: $text-muted;
  margin-bottom: 4px;
  line-height: 1.3;
}

.rfq-target-price-ccy {
  width: 100%;
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
  :deep(.rfq-table-target-ccy-col .cell) {
    overflow: visible;
    white-space: normal;
  }
}

.empty-hint {
  text-align: center;
  padding: 20px 0;
  color: $text-muted;
  font-size: 13px;
}
</style>
