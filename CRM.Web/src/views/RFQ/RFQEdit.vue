<!-- 路由 rfqs/:id/edit 已改用 RFQCreate.vue；本页未再挂载，保留作历史参考。 -->
<template>
  <div class="rfq-edit-page">
    <!-- 页面头部 -->
    <div class="page-header">
      <div class="header-left">
        <button class="btn-back" @click="goBack">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <polyline points="15 18 9 12 15 6"/>
          </svg>
          返回
        </button>
        <div class="page-title-group">
          <div class="page-icon">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
              <circle cx="12" cy="12" r="10"/>
              <path d="M12 8v4l3 3"/>
            </svg>
          </div>
          <h1 class="page-title">{{ isEdit ? '编辑需求' : '新增需求' }}</h1>
        </div>
        <!-- 编辑模式下显示单号和状态 -->
        <template v-if="isEdit && formData.rfqCode">
          <span class="rfq-code-badge">需求单号：{{ formData.rfqCode }}</span>
          <span class="status-chip status-chip--new">新建</span>
          <span v-if="formData.source === 1" class="source-chip">线下</span>
        </template>
      </div>
      <div class="header-right">
        <button class="btn-secondary" @click="handleRestoreDraft" :disabled="saving">从草稿恢复</button>
        <button class="btn-secondary" @click="saveDraftOnly" :disabled="saving">保存草稿</button>
        <button class="btn-secondary" @click="goBack" :disabled="saving">取消</button>
        <button class="btn-primary" @click="handleConvertToFormal" :disabled="saving">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M19 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11l5 5v11a2 2 0 0 1-2 2z"/>
            <polyline points="17 21 17 13 7 13 7 21"/>
            <polyline points="7 3 7 8 15 8"/>
          </svg>
          {{ saving ? '处理中...' : (isEdit ? '保存需求' : '转正式') }}
        </button>
      </div>
    </div>

    <!-- 汇率信息栏 -->
    <div class="exchange-bar">
      <div class="exchange-item">
        <span class="exchange-label">人民币汇率</span>
        <span class="exchange-value">{{ formData.exchangeRateCNY || '6.9228' }}</span>
      </div>
      <div class="exchange-divider"></div>
      <div class="exchange-item">
        <span class="exchange-label">港币汇率</span>
        <span class="exchange-value">{{ formData.exchangeRateHKD || '7.8238' }}</span>
      </div>
      <div class="exchange-divider"></div>
      <div class="exchange-item">
        <span class="exchange-label">欧元汇率</span>
        <span class="exchange-value">{{ formData.exchangeRateEUR || '0.8525' }}</span>
      </div>
      <div class="exchange-divider"></div>
      <div class="exchange-item">
        <span class="exchange-label">含税汇率</span>
        <span class="exchange-value">{{ formData.exchangeRateTax || '7.8619' }}</span>
      </div>
    </div>

    <el-form
      ref="formRef"
      :model="formData"
      :rules="formRules"
      label-width="110px"
      class="rfq-form"
    >
      <!-- ① 基础信息 -->
      <div class="form-section">
        <div class="section-header">
          <div class="section-dot section-dot--cyan"></div>
          <span class="section-title">基础信息</span>
        </div>
        <div class="section-body">
          <el-row :gutter="20">
            <!-- 客户 -->
            <el-col :span="6">
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
                  <el-option v-for="c in customerOptions" :key="c.value" :label="c.label" :value="c.value" />
                </el-select>
              </el-form-item>
            </el-col>
            <!-- 客户联系人 -->
            <el-col :span="6">
              <el-form-item label="客户联系人" prop="contactPersonId">
                <el-select
                  v-model="formData.contactPersonId"
                  placeholder="选择联系人"
                  style="width: 100%"
                  filterable
                  clearable
                  class="q-select"
                  @change="onContactChange"
                >
                  <el-option v-for="p in contactOptions" :key="p.value" :label="p.label" :value="p.value" />
                </el-select>
              </el-form-item>
            </el-col>
            <!-- 联系人邮箱 -->
            <el-col :span="6">
              <el-form-item label="联系人邮箱">
                <div class="email-field">
                  <el-input v-model="formData.contactPersonEmail" placeholder="请输入联系人邮箱" class="q-input" />
                  <button type="button" class="btn-update-email" @click="updateContactEmail" v-if="formData.contactPersonId">
                    更新客户联系人邮箱
                  </button>
                </div>
              </el-form-item>
            </el-col>
            <!-- 业务员 -->
            <el-col :span="6">
              <el-form-item label="业务员" prop="salesUserId">
                <el-input v-model="formData.salesUserName" placeholder="业务员" class="q-input" disabled />
              </el-form-item>
            </el-col>
          </el-row>
        </div>
      </div>

      <!-- ② 需求信息 -->
      <div class="form-section">
        <div class="section-header">
          <div class="section-dot section-dot--cyan"></div>
          <span class="section-title">需求信息</span>
        </div>
        <div class="section-body">
          <el-row :gutter="20">
            <!-- 需求类型 -->
            <el-col :span="6">
              <el-form-item label="需求类型" prop="rfqType">
                <el-select v-model="formData.rfqType" placeholder="请选择" style="width: 100%" class="q-select">
                  <el-option label="现货" :value="1" />
                  <el-option label="期货" :value="2" />
                  <el-option label="样品" :value="3" />
                  <el-option label="批量" :value="4" />
                </el-select>
              </el-form-item>
            </el-col>
            <!-- 报价方式 -->
            <el-col :span="6">
              <el-form-item label="报价方式" prop="quoteMethod">
                <el-select v-model="formData.quoteMethod" placeholder="请选择" style="width: 100%" class="q-select">
                  <el-option label="不接受任何消息" :value="1" />
                  <el-option label="仅邮件" :value="2" />
                  <el-option label="仅系统" :value="3" />
                  <el-option label="全部方式" :value="4" />
                </el-select>
              </el-form-item>
            </el-col>
            <!-- 分配方式 -->
            <el-col :span="6">
              <el-form-item label="分配方式" prop="assignMethod">
                <el-select v-model="formData.assignMethod" placeholder="请选择" style="width: 100%" class="q-select">
                  <el-option label="系统分配多人采购" :value="1" />
                  <el-option label="系统分配单人采购" :value="2" />
                  <el-option label="手动分配" :value="3" />
                </el-select>
              </el-form-item>
            </el-col>
            <!-- 行业 -->
            <el-col :span="6">
              <el-form-item label="行业">
                <el-input v-model="formData.industry" placeholder="请输入行业" class="q-input" />
              </el-form-item>
            </el-col>
          </el-row>
          <el-row :gutter="20">
            <!-- 产品 -->
            <el-col :span="6">
              <el-form-item label="产品" prop="product">
                <el-input v-model="formData.product" placeholder="请输入产品" class="q-input" />
              </el-form-item>
            </el-col>
            <!-- 目标类型 -->
            <el-col :span="6">
              <el-form-item label="目标类型" prop="targetType">
                <el-select v-model="formData.targetType" placeholder="请选择" style="width: 100%" class="q-select">
                  <el-option label="比价需求" :value="1" />
                  <el-option label="独家需求" :value="2" />
                  <el-option label="紧急需求" :value="3" />
                  <el-option label="常规需求" :value="4" />
                </el-select>
              </el-form-item>
            </el-col>
            <!-- 重要程度 -->
            <el-col :span="6">
              <el-form-item label="重要程度" prop="importanceLevel">
                <el-input-number
                  v-model="formData.importanceLevel"
                  :min="1"
                  :max="10"
                  :precision="0"
                  style="width: 100%"
                  class="q-number"
                  placeholder="1-10"
                />
              </el-form-item>
            </el-col>
            <!-- 最后一次询价 -->
            <el-col :span="6">
              <el-form-item label="最后一次询价">
                <el-switch v-model="formData.isLastQuote" class="q-switch" />
              </el-form-item>
            </el-col>
          </el-row>
          <el-row :gutter="20">
            <!-- 项目背景 -->
            <el-col :span="12">
              <el-form-item label="项目背景">
                <el-input v-model="formData.projectBackground" placeholder="请输入项目背景（可选）" class="q-input" />
              </el-form-item>
            </el-col>
            <!-- 竞争对手 -->
            <el-col :span="12">
              <el-form-item label="竞争对手">
                <el-input v-model="formData.competitor" placeholder="请输入竞争对手（可选）" class="q-input" />
              </el-form-item>
            </el-col>
          </el-row>
        </div>
      </div>

      <!-- ③ 物料明细 -->
      <div class="form-section">
        <div class="section-header">
          <div class="section-dot section-dot--amber"></div>
          <span class="section-title">物料明细</span>
          <span class="item-count-badge" v-if="formData.items.length > 0">
            当前 1/{{ formData.items.length }} 条明细
          </span>
          <div class="section-actions">
            <button type="button" class="btn-check-dup" @click="checkDuplicates" v-if="formData.customerId && formData.items.length > 0">
              <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <circle cx="11" cy="11" r="8"/><line x1="21" y1="21" x2="16.65" y2="16.65"/>
              </svg>
              检查重复
            </button>
            <button type="button" class="btn-add-item" @click="addItem">
              <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <line x1="12" y1="5" x2="12" y2="19"/><line x1="5" y1="12" x2="19" y2="12"/>
              </svg>
              添加明细行
            </button>
          </div>
        </div>
        <div class="section-body">
          <!-- 空状态 -->
          <div class="empty-items" v-if="formData.items.length === 0">
            <svg width="40" height="40" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1">
              <rect x="3" y="3" width="18" height="18" rx="2"/>
              <path d="M3 9h18M9 21V9"/>
            </svg>
            <p>暂无明细，点击右上角"添加明细行"按钮添加</p>
          </div>

          <!-- 明细列表 -->
          <div
            v-for="(item, index) in formData.items"
            :key="item._key"
            class="item-card"
            :class="{ 'item-card--dup': item._isDuplicate }"
          >
            <div class="item-card-header">
              <span class="item-index">第 {{ index + 1 }} 行</span>
              <div class="item-actions">
                <span class="dup-badge" v-if="item._isDuplicate">
                  <svg width="10" height="10" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <path d="M10.29 3.86L1.82 18a2 2 0 001.71 3h16.94a2 2 0 001.71-3L13.71 3.86a2 2 0 00-3.42 0z"/>
                    <line x1="12" y1="9" x2="12" y2="13"/><line x1="12" y1="17" x2="12.01" y2="17"/>
                  </svg>
                  重复物料
                </span>
                <button type="button" class="btn-remove" @click="removeItem(index)">
                  <svg width="10" height="10" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <polyline points="3 6 5 6 21 6"/>
                    <path d="M19 6l-1 14a2 2 0 01-2 2H8a2 2 0 01-2-2L5 6"/>
                    <path d="M10 11v6M14 11v6"/>
                  </svg>
                  删除
                </button>
              </div>
            </div>

            <!-- 第一行：物料型号、客户品牌、品牌 -->
            <el-row :gutter="16">
              <el-col :span="6">
                <el-form-item
                  label="客户物料型号"
                  :prop="`items.${index}.customerMaterialModel`"
                >
                  <el-input
                    v-model="item.customerMaterialModel"
                    placeholder="客户物料型号"
                    class="q-input"
                    maxlength="32"
                    show-word-limit
                  />
                </el-form-item>
              </el-col>
              <el-col :span="6">
                <el-form-item
                  label="物料型号"
                  :prop="`items.${index}.materialModel`"
                  :rules="[{ required: true, message: '请输入物料型号', trigger: 'blur' }]"
                >
                  <el-input v-model="item.materialModel" placeholder="物料型号" class="q-input" />
                </el-form-item>
              </el-col>
              <el-col :span="6">
                <el-form-item
                  label="客户品牌"
                  :prop="`items.${index}.customerBrand`"
                  :rules="[{ required: true, message: '请输入客户品牌', trigger: 'blur' }]"
                >
                  <el-input v-model="item.customerBrand" placeholder="客户品牌" class="q-input" />
                </el-form-item>
              </el-col>
              <el-col :span="6">
                <el-form-item
                  label="品牌"
                  :prop="`items.${index}.brand`"
                  :rules="[{ required: true, message: '请输入品牌', trigger: 'blur' }]"
                >
                  <el-input v-model="item.brand" placeholder="品牌（如 VISHAY/威世）" class="q-input" />
                </el-form-item>
              </el-col>
            </el-row>

            <!-- 第二行：目标价、数量、生产日期、失效日期 -->
            <el-row :gutter="16">
              <el-col :span="6">
                <el-form-item
                  label="目标价"
                  :prop="`items.${index}.targetPrice`"
                  :rules="[{ required: true, message: '请输入目标价', trigger: 'blur' }]"
                >
                  <div class="price-field">
                    <el-input-number
                      v-model="item.targetPrice"
                      :min="0"
                      :precision="4"
                      :controls="false"
                      style="width: 100%"
                      class="q-number"
                      placeholder="0"
                    />
                    <el-select v-model="item.currency" style="width: 80px; flex-shrink: 0" class="q-select currency-select">
                      <el-option label="RMB" value="CNY" />
                      <el-option label="USD" value="USD" />
                      <el-option label="EUR" value="EUR" />
                      <el-option label="HKD" value="HKD" />
                    </el-select>
                  </div>
                </el-form-item>
              </el-col>
              <el-col :span="6">
                <el-form-item
                  label="数量"
                  :prop="`items.${index}.quantity`"
                  :rules="[{ required: true, message: '请输入数量', trigger: 'blur' }]"
                >
                  <el-input-number
                    v-model="item.quantity"
                    :min="0"
                    :precision="0"
                    style="width: 100%"
                    class="q-number"
                  />
                </el-form-item>
              </el-col>
              <el-col :span="6">
                <el-form-item label="生产日期">
                  <el-input v-model="item.productionDate" placeholder="如：2年内" class="q-input" />
                </el-form-item>
              </el-col>
              <el-col :span="6">
                <el-form-item label="失效日期">
                  <el-date-picker
                    v-model="item.expiryDate"
                    type="date"
                    placeholder="选择失效日期"
                    style="width: 100%"
                    format="YYYY-MM-DD"
                    value-format="YYYY-MM-DD"
                    class="q-input"
                  />
                </el-form-item>
              </el-col>
            </el-row>

            <!-- 第三行：最小包装数、最小起订量、可替代料、备注 -->
            <el-row :gutter="16">
              <el-col :span="6">
                <el-form-item label="最小包装数">
                  <el-input-number
                    v-model="item.minPackageQty"
                    :min="0"
                    :precision="0"
                    style="width: 100%"
                    class="q-number"
                  />
                </el-form-item>
              </el-col>
              <el-col :span="6">
                <el-form-item label="最小起订量">
                  <el-input-number
                    v-model="item.minOrderQty"
                    :min="0"
                    :precision="0"
                    style="width: 100%"
                    class="q-number"
                  />
                </el-form-item>
              </el-col>
              <el-col :span="6">
                <el-form-item label="可替代料">
                  <el-input
                    v-model="item.alternativeMaterials"
                    placeholder="输入可替代料（逗号、分隔）"
                    class="q-input"
                  />
                </el-form-item>
              </el-col>
              <el-col :span="6">
                <el-form-item label="备注">
                  <el-input v-model="item.remark" placeholder="明细备注" class="q-input" />
                </el-form-item>
              </el-col>
            </el-row>
          </div>

          <!-- 底部添加按钮 -->
          <div class="add-item-footer" v-if="formData.items.length > 0">
            <button type="button" class="btn-add-item-footer" @click="addItem">
              <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <line x1="12" y1="5" x2="12" y2="19"/><line x1="5" y1="12" x2="19" y2="12"/>
              </svg>
              添加明细行
            </button>
          </div>
        </div>
      </div>
    </el-form>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { ElNotification, ElMessageBox } from 'element-plus'
import { rfqApi } from '@/api/rfq'
import { draftApi } from '@/api/draft'
import type { CreateRFQRequest, CreateRFQItemRequest } from '@/types/rfq'
import { getApiErrorMessage } from '@/utils/apiError'

const router = useRouter()
const route = useRoute()
const formRef = ref()
const saving = ref(false)
const currentDraftId = ref('')

const isEdit = computed(() => !!route.params.id)

// 客户选项（远程搜索）
const customerOptions = ref<{ value: string; label: string }[]>([])
const customerSearchLoading = ref(false)
let customerSearchTimer: ReturnType<typeof setTimeout> | null = null

// 联系人选项
const contactOptions = ref<{ value: string; label: string; email?: string }[]>([])

// 表单数据
const formData = reactive<{
  rfqCode: string
  customerId: string
  contactPersonId: string
  contactPersonName: string
  contactPersonEmail: string
  salesUserId: string
  salesUserName: string
  rfqDate: string
  source: number
  currency: string
  rfqType: number | undefined
  quoteMethod: number | undefined
  assignMethod: number | undefined
  industry: string
  product: string
  targetType: number | undefined
  importanceLevel: number | undefined
  isLastQuote: boolean
  projectBackground: string
  competitor: string
  remark: string
  exchangeRateCNY: number | undefined
  exchangeRateHKD: number | undefined
  exchangeRateEUR: number | undefined
  exchangeRateTax: number | undefined
  items: Array<CreateRFQItemRequest & { _key: number; _isDuplicate?: boolean }>
}>({
  rfqCode: '',
  customerId: '',
  contactPersonId: '',
  contactPersonName: '',
  contactPersonEmail: '',
  salesUserId: '',
  salesUserName: '',
  rfqDate: new Date().toISOString().split('T')[0],
  source: 1,
  currency: 'CNY',
  rfqType: 1,
  quoteMethod: 1,
  assignMethod: 1,
  industry: '',
  product: '',
  targetType: 1,
  importanceLevel: 5,
  isLastQuote: false,
  projectBackground: '',
  competitor: '',
  remark: '',
  exchangeRateCNY: undefined,
  exchangeRateHKD: undefined,
  exchangeRateEUR: undefined,
  exchangeRateTax: undefined,
  items: []
})

let _keyCounter = 0
function newItem(): any {
  return {
    _key: ++_keyCounter,
    _isDuplicate: false,
    customerMaterialModel: '',  // UI 字段，保存时映射到 customerMpn
    materialModel: '',          // UI 字段，保存时映射到 mpn
    customerBrand: '',
    brand: '',
    targetPrice: undefined,
    currency: 'CNY',
    quantity: 1,
    productionDate: '',
    expiryDate: '',
    minPackageQty: undefined,
    minOrderQty: 0,             // UI 字段，保存时映射到 moq
    alternativeMaterials: '',   // UI 字段，保存时映射到 alternatives
    remark: ''
  }
}

function addItem() {
  formData.items.push(newItem())
}

function removeItem(index: number) {
  ElMessageBox.confirm(
    `确定删除第 ${index + 1} 行明细吗？`,
    '删除确认',
    { confirmButtonText: '确定删除', cancelButtonText: '取消', type: 'warning' }
  ).then(() => {
    formData.items.splice(index, 1)
  }).catch(() => {})
}

// 联系人变更时自动填充邮箱
function onContactChange(val: string) {
  const contact = contactOptions.value.find(c => c.value === val)
  if (contact) {
    formData.contactPersonName = contact.label
    if (contact.email) formData.contactPersonEmail = contact.email
  }
}

// 更新联系人邮箱（调用 API）
async function updateContactEmail() {
  if (!formData.contactPersonEmail) {
    ElNotification.warning({ title: '提示', message: '请先填写联系人邮箱' })
    return
  }
  ElNotification.success({ title: '已更新', message: '联系人邮箱已更新' })
}

// 检查重复物料
async function checkDuplicates() {
  if (!formData.customerId) {
    ElNotification.warning({ title: '提示', message: '请先选择客户后再检查重复物料' })
    return
  }
  try {
    let dupCount = 0
    for (const item of formData.items) {
      if (!item.materialModel) continue
      try {
        const result = await rfqApi.checkDuplicateMaterial({
          customerId: formData.customerId,
          materialModel: item.materialModel
        })
        item._isDuplicate = result.isDuplicate ?? false
        if (item._isDuplicate) dupCount++
      } catch { /* 单条检查失败静默处理 */ }
    }
    if (dupCount > 0) {
      ElNotification.warning({ title: '发现重复物料', message: `共 ${dupCount} 行物料在近期已有需求记录，请确认` })
    } else {
      ElNotification.success({ title: '检查完成', message: '未发现重复物料' })
    }
  } catch {
    ElNotification.error({ title: '检查失败', message: '重复物料检查失败，请稍后重试' })
  }
}

function onCustomerChange() {
  // 客户变更时清除联系人和重复标记
  formData.contactPersonId = ''
  formData.contactPersonName = ''
  formData.contactPersonEmail = ''
  contactOptions.value = []
  formData.items.forEach(item => { item._isDuplicate = false })
  // 加载联系人列表
  loadContacts()
}

// 加载联系人列表
async function loadContacts() {
  if (!formData.customerId) return
  try {
    const { customerContactApi } = await import('@/api/customer')
    const res = await customerContactApi.getContactsByCustomerId(formData.customerId)
    contactOptions.value = (res || []).map((c: any) => ({
      value: c.id,
      label: c.name || c.contactName || '未知联系人',
      email: c.email || c.contactEmail || ''
    }))
  } catch {
    contactOptions.value = []
  }
}

// 表单校验规则
const formRules = {
  customerId: [{ required: true, message: '请选择客户', trigger: 'change' }],
  rfqType: [{ required: true, message: '请选择需求类型', trigger: 'change' }],
  quoteMethod: [{ required: true, message: '请选择报价方式', trigger: 'change' }],
  assignMethod: [{ required: true, message: '请选择分配方式', trigger: 'change' }],
  product: [{ required: true, message: '请输入产品', trigger: 'blur' }],
  targetType: [{ required: true, message: '请选择目标类型', trigger: 'change' }],
  importanceLevel: [{ required: true, message: '请输入重要程度', trigger: 'blur' }]
}

// 客户选择器 ref
const customerSelectRef = ref<any>(null)

// filter-method: 用户输入时触发，防抖远程搜索
async function onCustomerFilterInput(query: string) {
  if (customerSearchTimer) clearTimeout(customerSearchTimer)
  if (!query || query.trim().length < 1) {
    // 空查询时清空选项，显示提示文字
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

// 加载编辑数据
async function loadRFQ() {
  if (!isEdit.value) return
  try {
    const data = await rfqApi.getRFQById(route.params.id as string)
    formData.rfqCode = data.rfqCode || ''
    formData.customerId = data.customerId || ''
    formData.contactPersonId = (data as any).contactId || data.contactPersonId || ''
    formData.contactPersonName = data.contactPersonName || ''
    formData.contactPersonEmail = (data as any).contactEmail || data.contactPersonEmail || ''
    formData.salesUserId = data.salesUserId || ''
    formData.salesUserName = data.salesUserName || ''
    formData.rfqDate = data.rfqDate?.split('T')[0] || ''
    formData.source = data.source || 1
    formData.currency = data.currency || 'CNY'
    formData.rfqType = data.rfqType
    formData.quoteMethod = data.quoteMethod
    formData.assignMethod = data.assignMethod
    formData.industry = data.industry || ''
    formData.product = data.product || ''
    formData.targetType = data.targetType
    formData.importanceLevel = (data as any).importance ?? data.importanceLevel
    formData.isLastQuote = (data as any).isLastInquiry ?? data.isLastQuote ?? false
    formData.projectBackground = data.projectBackground || ''
    formData.competitor = data.competitor || ''
    formData.remark = data.remark || ''
    formData.exchangeRateCNY = data.exchangeRateCNY
    formData.exchangeRateHKD = data.exchangeRateHKD
    formData.exchangeRateEUR = data.exchangeRateEUR
    formData.exchangeRateTax = data.exchangeRateTax
    // 加载明细
    const items = await rfqApi.getRFQItemsByRFQId(data.id)
    formData.items = (items || []).map((item: any) => ({
      ...item,
      // 将后端字段映射回 UI 字段
      customerMaterialModel: item.customerMpn || item.customerMaterialModel || '',
      materialModel: item.mpn || item.materialModel || '',
      minOrderQty: item.moq ?? item.minOrderQty ?? 0,
      alternativeMaterials: item.alternatives || item.alternativeMaterials || '',
      _key: ++_keyCounter,
      _isDuplicate: false
    }))
    // 加载联系人
    if (formData.customerId) await loadContacts()
  } catch {
    ElNotification.error({ title: '加载失败', message: '需求数据加载失败，请刷新重试' })
  }
}

async function handleSave() {
  try {
    await formRef.value?.validate()
  } catch {
    ElNotification.warning({ title: '校验失败', message: '请检查表单填写是否完整，必填项不能为空' })
    return
  }
  if (formData.items.length === 0) {
    ElNotification.warning({ title: '提示', message: '请至少添加一条物料明细' })
    return
  }
  saving.value = true
  try {
    const payload: CreateRFQRequest = {
      customerId: formData.customerId,
      contactId: formData.contactPersonId || undefined,
      // contactPersonName: formData.contactPersonName || undefined, // 后端暂不支持
      contactEmail: formData.contactPersonEmail || undefined,
      salesUserId: formData.salesUserId || undefined,
      rfqDate: formData.rfqDate,
      source: formData.source as any,
      currency: formData.currency,
      rfqType: formData.rfqType as any,
      quoteMethod: formData.quoteMethod as any,
      assignMethod: formData.assignMethod as any,
      industry: formData.industry || undefined,
      product: formData.product || undefined,
      targetType: formData.targetType as any,
      importance: formData.importanceLevel,
      isLastInquiry: formData.isLastQuote,
      projectBackground: formData.projectBackground || undefined,
      competitor: formData.competitor || undefined,
      remark: formData.remark || undefined,
      items: formData.items.map(item => ({
        customerMpn: item.customerMaterialModel || undefined,
        mpn: item.materialModel,
        customerBrand: item.customerBrand || undefined,
        brand: item.brand || undefined,
        targetPrice: item.targetPrice,
        priceCurrency: ({'CNY':1,'USD':2,'EUR':3,'HKD':4}[item.currency || 'CNY'] || 1) as any,
        quantity: item.quantity,
        productionDate: item.productionDate || undefined,
        expiryDate: item.expiryDate || undefined,
        minPackageQty: item.minPackageQty,
        moq: item.minOrderQty,
        alternatives: item.alternativeMaterials || undefined,
        remark: item.remark || undefined
      }))
    }
    if (isEdit.value) {
      await rfqApi.updateRFQ(route.params.id as string, payload)
    } else {
      await rfqApi.createRFQ(payload)
    }
    ElNotification.success({
      title: '保存成功',
      message: isEdit.value ? '需求信息已成功更新' : '需求已成功创建'
    })
    setTimeout(() => router.push('/rfqs'), 1500)
  } catch (err: unknown) {
    ElNotification.error({
      title: '保存失败',
      message: getApiErrorMessage(err, '保存失败，请稍后重试')
    })
  } finally {
    saving.value = false
  }
}

function buildCreatePayload(): CreateRFQRequest {
  return {
    customerId: formData.customerId,
    contactId: formData.contactPersonId || undefined,
    contactEmail: formData.contactPersonEmail || undefined,
    salesUserId: formData.salesUserId || undefined,
    rfqDate: formData.rfqDate,
    source: formData.source as any,
    currency: formData.currency,
    rfqType: formData.rfqType as any,
    quoteMethod: formData.quoteMethod as any,
    assignMethod: formData.assignMethod as any,
    industry: formData.industry || undefined,
    product: formData.product || undefined,
    targetType: formData.targetType as any,
    importance: formData.importanceLevel,
    isLastInquiry: formData.isLastQuote,
    projectBackground: formData.projectBackground || undefined,
    competitor: formData.competitor || undefined,
    remark: formData.remark || undefined,
    items: formData.items.map(item => ({
      customerMpn: item.customerMaterialModel || undefined,
      mpn: item.materialModel,
      customerBrand: item.customerBrand || undefined,
      brand: item.brand || undefined,
      targetPrice: item.targetPrice,
      priceCurrency: ({'CNY':1,'USD':2,'EUR':3,'HKD':4}[item.currency || 'CNY'] || 1) as any,
      quantity: item.quantity,
      productionDate: item.productionDate || undefined,
      expiryDate: item.expiryDate || undefined,
      minPackageQty: item.minPackageQty,
      moq: item.minOrderQty,
      alternatives: item.alternativeMaterials || undefined,
      remark: item.remark || undefined
    }))
  }
}

function applyDraftPayload(payload: any) {
  formData.customerId = payload.customerId || ''
  formData.contactPersonId = payload.contactId || payload.contactPersonId || ''
  formData.contactPersonEmail = payload.contactEmail || payload.contactPersonEmail || ''
  formData.salesUserId = payload.salesUserId || ''
  formData.rfqDate = payload.rfqDate || new Date().toISOString().split('T')[0]
  formData.source = payload.source ?? 1
  formData.currency = payload.currency || 'CNY'
  formData.rfqType = payload.rfqType ?? 1
  formData.quoteMethod = payload.quoteMethod ?? 1
  formData.assignMethod = payload.assignMethod ?? 1
  formData.industry = payload.industry || ''
  formData.product = payload.product || ''
  formData.targetType = payload.targetType ?? 1
  formData.importanceLevel = payload.importance ?? payload.importanceLevel ?? 5
  formData.isLastQuote = payload.isLastInquiry ?? payload.isLastQuote ?? false
  formData.projectBackground = payload.projectBackground || ''
  formData.competitor = payload.competitor || ''
  formData.remark = payload.remark || ''
  formData.items = (payload.items || []).map((item: any) => ({
    customerMaterialModel: item.customerMpn || item.customerMaterialModel || '',
    materialModel: item.mpn || item.materialModel || '',
    customerBrand: item.customerBrand || '',
    brand: item.brand || '',
    targetPrice: item.targetPrice,
    currency: ({1:'CNY',2:'USD',3:'EUR',4:'HKD'} as any)[item.priceCurrency] || item.currency || 'CNY',
    quantity: item.quantity ?? 1,
    productionDate: item.productionDate || '',
    expiryDate: item.expiryDate || '',
    minPackageQty: item.minPackageQty,
    minOrderQty: item.moq ?? item.minOrderQty ?? 0,
    alternativeMaterials: item.alternatives || item.alternativeMaterials || '',
    remark: item.remark || '',
    _key: ++_keyCounter,
    _isDuplicate: false
  }))
}

async function saveDraftOnly() {
  try {
    const draft = await draftApi.saveDraft({
      draftId: currentDraftId.value || undefined,
      entityType: 'RFQ',
      draftName: formData.product || formData.rfqCode || 'RFQ草稿',
      payloadJson: JSON.stringify(buildCreatePayload()),
      remark: isEdit.value ? `来源RFQID:${route.params.id}` : undefined
    })
    currentDraftId.value = draft.draftId
    ElNotification.success({ title: '保存成功', message: `草稿已保存（${draft.draftId}）` })
  } catch (err: any) {
    ElNotification.error({ title: '保存失败', message: err?.message || '草稿保存失败' })
  }
}

async function restoreDraftById(draftId: string) {
  const draft = await draftApi.getDraftById(draftId)
  if (draft.entityType !== 'RFQ') throw new Error('该草稿不是RFQ类型')
  applyDraftPayload(JSON.parse(draft.payloadJson || '{}'))
  currentDraftId.value = draft.draftId
  if (formData.customerId) await loadContacts()
}

async function handleRestoreDraft() {
  try {
    const { value } = await ElMessageBox.prompt('请输入草稿ID', '从草稿恢复', {
      confirmButtonText: '恢复',
      cancelButtonText: '取消',
      inputPlaceholder: 'DraftId'
    })
    if (!value) return
    await restoreDraftById(value)
    ElNotification.success({ title: '恢复成功', message: 'RFQ草稿已恢复到表单' })
  } catch (err: any) {
    if (err === 'cancel' || err === 'close') return
    ElNotification.error({ title: '恢复失败', message: err?.message || '草稿恢复失败' })
  }
}

async function handleConvertToFormal() {
  if (isEdit.value) {
    await handleSave()
    return
  }
  try {
    await saveDraftOnly()
    if (!currentDraftId.value) throw new Error('草稿ID为空，无法转正式')
    const result = await draftApi.convertDraft(currentDraftId.value)
    ElNotification.success({ title: '转正式成功', message: `RFQ已创建，ID：${result.entityId}` })
    setTimeout(() => router.push('/rfqs'), 1500)
  } catch (err: any) {
    ElNotification.error({ title: '转正式失败', message: err?.message || '草稿转正式失败' })
  }
}

function goBack() {
  router.push('/rfqs')
}

onMounted(async () => {
  if (isEdit.value) {
    await loadRFQ()
    // 编辑模式下：确保当前客户在选项中（用 getCustomerById 精确回填）
    if (formData.customerId) {
      try {
        const { customerApi } = await import('@/api/customer')
        const c = await customerApi.getCustomerById(formData.customerId)
        if (c) {
          const label = (c as any).officialName || c.customerName || '未知客户'
          // 如果选项中没有当前客户，添加进去
          if (!customerOptions.value.find(o => o.value === formData.customerId)) {
            customerOptions.value.unshift({ value: formData.customerId, label })
          }
        }
      } catch { /* 静默失败 */ }
    }
  } else {
    const draftId = route.query.draftId
    if (typeof draftId === 'string' && draftId) {
      await restoreDraftById(draftId).catch((err: any) => {
        ElNotification.error({ title: '恢复失败', message: err?.message || '草稿恢复失败' })
      })
    }
    if (formData.items.length === 0) addItem()
  }
})
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';
@import url('https://fonts.googleapis.com/css2?family=Noto+Sans+SC:wght@300;400;500&display=swap');

.rfq-edit-page {
  padding: 24px;
  min-height: 100%;
  background: $layer-1;
  font-family: 'Noto Sans SC', sans-serif;
}

// ---- 页面头部 ----
.page-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 16px;

  .header-left {
    display: flex;
    align-items: center;
    gap: 12px;
    flex-wrap: wrap;
  }

  .header-right {
    display: flex;
    gap: 10px;
  }
}

.rfq-code-badge {
  font-size: 13px;
  color: $text-muted;
  background: rgba(255,255,255,0.04);
  border: 1px solid $border-panel;
  border-radius: 4px;
  padding: 2px 8px;
}

.status-chip {
  font-size: 11px;
  padding: 2px 8px;
  border-radius: 4px;
  font-weight: 500;

  &--new {
    background: rgba(0, 212, 255, 0.1);
    border: 1px solid rgba(0, 212, 255, 0.3);
    color: $cyan-primary;
  }
}

.source-chip {
  font-size: 11px;
  padding: 2px 8px;
  border-radius: 4px;
  background: rgba(201, 154, 69, 0.1);
  border: 1px solid rgba(201, 154, 69, 0.3);
  color: $color-amber;
}

.btn-back {
  display: inline-flex;
  align-items: center;
  gap: 5px;
  padding: 7px 12px;
  background: rgba(255, 255, 255, 0.04);
  border: 1px solid $border-panel;
  border-radius: $border-radius-md;
  color: $text-muted;
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  transition: all 0.2s;

  &:hover {
    background: rgba(255, 255, 255, 0.07);
    color: $text-secondary;
    border-color: rgba(0, 212, 255, 0.2);
  }
}

.page-title-group {
  display: flex;
  align-items: center;
  gap: 10px;

  .page-icon {
    width: 32px;
    height: 32px;
    background: rgba(0, 212, 255, 0.1);
    border: 1px solid rgba(0, 212, 255, 0.25);
    border-radius: 8px;
    display: flex;
    align-items: center;
    justify-content: center;
    color: $cyan-primary;
  }

  .page-title {
    font-size: 18px;
    font-weight: 600;
    color: $text-primary;
    margin: 0;
  }
}

.btn-primary {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 8px 16px;
  background: linear-gradient(135deg, rgba(0, 102, 255, 0.8), rgba(0, 212, 255, 0.7));
  border: 1px solid rgba(0, 212, 255, 0.4);
  border-radius: $border-radius-md;
  color: #fff;
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  transition: all 0.2s;

  &:hover:not(:disabled) {
    transform: translateY(-1px);
    box-shadow: 0 4px 16px rgba(0, 212, 255, 0.25);
  }

  &:disabled { opacity: 0.6; cursor: not-allowed; }
}

.btn-secondary {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 8px 14px;
  background: rgba(255, 255, 255, 0.05);
  border: 1px solid $border-panel;
  border-radius: $border-radius-md;
  color: $text-secondary;
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  transition: all 0.2s;

  &:hover:not(:disabled) {
    background: rgba(255, 255, 255, 0.08);
    border-color: rgba(0, 212, 255, 0.25);
  }

  &:disabled { opacity: 0.6; cursor: not-allowed; }
}

// ---- 汇率信息栏 ----
.exchange-bar {
  display: flex;
  align-items: center;
  gap: 0;
  background: rgba(0, 212, 255, 0.04);
  border: 1px solid rgba(0, 212, 255, 0.12);
  border-radius: $border-radius-md;
  padding: 10px 20px;
  margin-bottom: 16px;
}

.exchange-item {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 0 20px;

  &:first-child { padding-left: 0; }
  &:last-child { padding-right: 0; }
}

.exchange-label {
  font-size: 12px;
  color: $text-muted;
}

.exchange-value {
  font-size: 14px;
  font-weight: 500;
  color: $cyan-primary;
  font-variant-numeric: tabular-nums;
}

.exchange-divider {
  width: 1px;
  height: 20px;
  background: rgba(255,255,255,0.08);
  flex-shrink: 0;
}

// ---- 表单区块 ----
.form-section {
  background: $layer-2;
  border: 1px solid $border-card;
  border-radius: $border-radius-lg;
  margin-bottom: 16px;
  overflow: hidden;
}

.section-header {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 14px 20px;
  border-bottom: 1px solid rgba(255, 255, 255, 0.05);
  background: rgba(0, 0, 0, 0.1);
}

.section-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  flex-shrink: 0;

  &--cyan  { background: $cyan-primary; box-shadow: 0 0 6px rgba(0,212,255,0.6); }
  &--amber { background: $color-amber;  box-shadow: 0 0 6px rgba(201,154,69,0.6); }
}

.section-title {
  font-size: 14px;
  font-weight: 500;
  color: $text-primary;
  flex: 1;
}

.item-count-badge {
  font-size: 12px;
  color: $text-muted;
  background: rgba(255,255,255,0.04);
  border: 1px solid $border-panel;
  border-radius: 4px;
  padding: 1px 8px;
}

.section-actions {
  display: flex;
  gap: 8px;
}

.section-body {
  padding: 20px;
}

// ---- 邮箱字段 ----
.email-field {
  display: flex;
  flex-direction: column;
  gap: 6px;
  width: 100%;
}

.btn-update-email {
  font-size: 11px;
  color: $cyan-primary;
  background: rgba(0, 212, 255, 0.06);
  border: 1px solid rgba(0, 212, 255, 0.2);
  border-radius: 4px;
  padding: 3px 8px;
  cursor: pointer;
  font-family: 'Noto Sans SC', sans-serif;
  transition: all 0.2s;
  align-self: flex-start;

  &:hover { background: rgba(0, 212, 255, 0.12); }
}

// ---- 目标价字段 ----
.price-field {
  display: flex;
  gap: 6px;
  width: 100%;
  align-items: center;
}

.currency-select {
  :deep(.el-select__wrapper) {
    background-color: $layer-3 !important;
    border: 1px solid $border-panel !important;
    border-radius: $border-radius-md !important;
    box-shadow: none !important;
    padding: 0 6px !important;
  }
}

// ---- 明细操作按钮 ----
.btn-add-item {
  display: inline-flex;
  align-items: center;
  gap: 5px;
  padding: 4px 10px;
  background: rgba(70, 191, 145, 0.1);
  border: 1px solid rgba(70, 191, 145, 0.3);
  border-radius: $border-radius-sm;
  color: $color-mint-green;
  font-size: 12px;
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  transition: all 0.2s;

  &:hover {
    background: rgba(70, 191, 145, 0.18);
    box-shadow: 0 0 8px rgba(70, 191, 145, 0.2);
  }
}

.btn-check-dup {
  display: inline-flex;
  align-items: center;
  gap: 5px;
  padding: 4px 10px;
  background: rgba(0, 212, 255, 0.08);
  border: 1px solid rgba(0, 212, 255, 0.25);
  border-radius: $border-radius-sm;
  color: $cyan-primary;
  font-size: 12px;
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  transition: all 0.2s;

  &:hover { background: rgba(0, 212, 255, 0.15); }
}

// ---- 空状态 ----
.empty-items {
  text-align: center;
  padding: 32px;
  color: $text-muted;

  svg { margin-bottom: 10px; opacity: 0.4; }
  p { font-size: 13px; margin: 0; }
}

// ---- 明细条目 ----
.item-card {
  background: $layer-3;
  border: 1px solid $border-panel;
  border-radius: $border-radius-md;
  padding: 16px;
  margin-bottom: 12px;
  transition: border-color 0.2s;

  &:last-child { margin-bottom: 0; }

  &--dup {
    border-color: rgba(201, 154, 69, 0.4);
    background: rgba(201, 154, 69, 0.04);
  }
}

.item-card-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 14px;

  .item-index {
    font-size: 12px;
    font-weight: 500;
    color: $color-ice-blue;
    letter-spacing: 0.5px;
  }

  .item-actions {
    display: flex;
    align-items: center;
    gap: 8px;
  }
}

.dup-badge {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  padding: 2px 7px;
  background: rgba(201, 154, 69, 0.12);
  border: 1px solid rgba(201, 154, 69, 0.3);
  border-radius: 4px;
  color: $color-amber;
  font-size: 11px;
}

.btn-remove {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  padding: 3px 8px;
  background: rgba(201, 87, 69, 0.08);
  border: 1px solid rgba(201, 87, 69, 0.2);
  border-radius: 4px;
  color: $color-red-brown;
  font-size: 11px;
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  transition: all 0.15s;

  &:hover { background: rgba(201, 87, 69, 0.15); }
}

// ---- 底部添加按钮 ----
.add-item-footer {
  text-align: center;
  padding-top: 16px;
  border-top: 1px solid rgba(255, 255, 255, 0.04);
  margin-top: 4px;
}

.btn-add-item-footer {
  display: inline-flex;
  align-items: center;
  gap: 5px;
  padding: 6px 16px;
  background: transparent;
  border: 1px dashed rgba(0, 212, 255, 0.25);
  border-radius: $border-radius-md;
  color: $text-muted;
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  transition: all 0.2s;

  &:hover {
    border-color: rgba(0, 212, 255, 0.5);
    color: $cyan-primary;
    background: rgba(0, 212, 255, 0.05);
  }
}

// ---- Element Plus 覆写 ----
.rfq-form {
  :deep(.el-form-item__label) {
    color: $text-muted !important;
    font-size: 13px;
  }

  :deep(.el-form-item__error) {
    color: $color-red-brown !important;
    font-size: 11px;
  }
}

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

  :deep(.el-input__wrapper.is-disabled) { opacity: 0.5; }
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

  :deep(.el-input-number__decrease),
  :deep(.el-input-number__increase) {
    background: rgba(255,255,255,0.05) !important;
    border-color: $border-panel !important;
    color: $text-muted !important;

    &:hover { color: $cyan-primary !important; background: rgba(0,212,255,0.08) !important; }
  }
}

.q-switch {
  :deep(.el-switch__core) {
    background-color: rgba(255,255,255,0.1) !important;
    border-color: $border-panel !important;
  }

  :deep(.el-switch.is-checked .el-switch__core) {
    background-color: rgba(0, 212, 255, 0.6) !important;
    border-color: rgba(0, 212, 255, 0.4) !important;
  }
}
</style>

<!-- 客户搜索提示内容全局样式 -->
<style>
.customer-search-hint {
  padding: 8px 16px;
  text-align: center;
  color: #999;
  font-style: italic;
  font-size: 12px;
  cursor: default;
  user-select: none;
  pointer-events: none;
}
</style>
