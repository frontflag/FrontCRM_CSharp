<template>
  <div class="create-page">
    <div class="page-header">
      <div class="header-left">
        <el-button link @click="handleBack">
          <el-icon><ArrowLeft /></el-icon> 返回列表
        </el-button>
        <el-breadcrumb separator="/">
          <el-breadcrumb-item>订单管理</el-breadcrumb-item>
          <el-breadcrumb-item>销售管理</el-breadcrumb-item>
          <el-breadcrumb-item>新建销售订单</el-breadcrumb-item>
        </el-breadcrumb>
      </div>
      <div class="header-right">
        <el-button @click="handleBack">取消</el-button>
        <el-button type="primary" :loading="submitLoading" @click="handleSubmit">
          <el-icon><Check /></el-icon> 保存
        </el-button>
      </div>
    </div>

    <el-form ref="formRef" :model="formData" :rules="formRules" label-width="108px" class="create-form">
      <el-collapse v-model="collapseActive" class="so-collapse">
        <!-- 订单信息 -->
        <el-collapse-item title="订单信息" name="order">
          <el-row :gutter="20">
            <el-col :span="12">
              <el-form-item label="订单号">
                <el-input v-model="formData.sellOrderCode" disabled placeholder="系统自动生成" />
              </el-form-item>
            </el-col>
            <el-col :span="12">
              <el-form-item label="订单类型" prop="type">
                <el-select v-model="formData.type" placeholder="请选择订单类型" style="width: 100%">
                  <el-option label="普通订单" :value="1" />
                  <el-option label="紧急订单" :value="2" />
                  <el-option label="样品订单" :value="3" />
                </el-select>
              </el-form-item>
            </el-col>
          </el-row>
          <el-row :gutter="20">
            <el-col :span="12">
              <el-form-item label="负责人" prop="salesUserId">
                <el-select
                  v-model="formData.salesUserId"
                  placeholder="请选择负责人"
                  style="width: 100%"
                  filterable
                  clearable
                  @change="onSalesUserChange"
                >
                  <el-option
                    v-for="u in salesUserOptions"
                    :key="u.value"
                    :label="u.label"
                    :value="u.value"
                  />
                </el-select>
              </el-form-item>
            </el-col>
            <el-col :span="12">
              <el-form-item label="产品" prop="productKind">
                <el-select v-model="formData.productKind" placeholder="请选择产品" style="width: 100%">
                  <el-option label="现货" value="现货" />
                  <el-option label="期货" value="期货" />
                  <el-option label="排单" value="排单" />
                  <el-option label="样品" value="样品" />
                </el-select>
              </el-form-item>
            </el-col>
          </el-row>
          <el-row :gutter="20">
            <el-col :span="12">
              <el-form-item label="归属">
                <el-input :model-value="formData.ownerEntity" disabled />
              </el-form-item>
            </el-col>
            <el-col :span="12">
              <el-form-item label="备注">
                <el-input v-model="formData.orderRemark" type="textarea" :rows="2" placeholder="订单备注" />
              </el-form-item>
            </el-col>
          </el-row>
        </el-collapse-item>

        <!-- 客户信息 -->
        <el-collapse-item title="客户信息" name="customer">
          <el-row :gutter="20">
            <el-col :span="12">
              <el-form-item label="客户" prop="customerId">
                <el-select
                  v-model="formData.customerId"
                  placeholder="请输入客户名称搜索"
                  style="width: 100%"
                  filterable
                  :filter-method="onCustomerFilterInput"
                  :loading="customerSearchLoading"
                  loading-text="搜索中..."
                  @change="onCustomerChange"
                >
                  <template #empty>
                    <div class="select-hint">请输入关键字搜索客户</div>
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
            <el-col :span="12">
              <el-form-item label="客户联系人" prop="customerContactId">
                <el-select
                  v-model="formData.customerContactId"
                  placeholder="请选择联系人"
                  style="width: 100%"
                  filterable
                  clearable
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
          </el-row>
          <el-row :gutter="20">
            <el-col :span="12">
              <el-form-item label="发票信息">
                <el-input v-model="formData.invoiceInfo" type="textarea" :rows="2" placeholder="公司名称与税号等" />
              </el-form-item>
            </el-col>
            <el-col :span="12">
              <el-form-item label="账期信息" prop="paymentTermsLabel">
                <el-select
                  v-model="formData.paymentTermsLabel"
                  placeholder="请选择账期"
                  style="width: 100%"
                  filterable
                  allow-create
                  default-first-option
                >
                  <el-option label="NET 15" value="NET 15" />
                  <el-option label="NET 30" value="NET 30" />
                  <el-option label="NET 45" value="NET 45" />
                  <el-option label="NET 60" value="NET 60" />
                  <el-option label="款到发货" value="款到发货" />
                  <el-option label="货到付款" value="货到付款" />
                </el-select>
              </el-form-item>
            </el-col>
          </el-row>
          <el-row :gutter="20">
            <el-col :span="24">
              <el-form-item label="送货地址">
                <el-input v-model="formData.deliveryAddress" type="textarea" :rows="2" placeholder="送货地址" />
              </el-form-item>
            </el-col>
          </el-row>
        </el-collapse-item>

        <!-- 物料明细 -->
        <el-collapse-item title="物料明细" name="items">
          <div class="items-toolbar">
            <el-input
              v-model="itemFilterKeyword"
              class="item-filter"
              clearable
              placeholder="筛选物料型号"
            />
            <el-button type="primary" size="small" @click="addItem">
              <el-icon><Plus /></el-icon> 添加明细
            </el-button>
          </div>

          <div v-if="filteredItemsView.length === 0" class="items-empty">暂无明细或没有匹配的型号</div>

          <div
            v-for="meta in filteredItemsView"
            v-else
            :key="meta.index"
            class="material-card"
          >
            <div class="material-card-head">
              <span class="head-mpn">物料型号：{{ formData.items[meta.index].pn || '—' }}</span>
              <span class="head-quote">{{ formData.items[meta.index].purchaseQuoteLabel || '采购报价：—' }}</span>
            </div>
            <div class="material-card-body">
              <el-row :gutter="16">
                <el-col :span="8">
                  <el-form-item
                    label="物料型号"
                    :prop="'items.' + meta.index + '.pn'"
                    :rules="itemRules.pn"
                    label-width="100px"
                  >
                    <el-input v-model="formData.items[meta.index].pn" placeholder="必填" />
                  </el-form-item>
                </el-col>
                <el-col :span="8">
                  <el-form-item label="客户物料型号" :prop="'items.' + meta.index + '.customerMaterialModel'" label-width="112px">
                    <el-input v-model="formData.items[meta.index].customerMaterialModel" placeholder="选填" />
                  </el-form-item>
                </el-col>
                <el-col :span="8">
                  <el-form-item
                    label="品牌"
                    :prop="'items.' + meta.index + '.brand'"
                    :rules="itemRules.brand"
                    label-width="72px"
                  >
                    <el-input v-model="formData.items[meta.index].brand" placeholder="必填" />
                  </el-form-item>
                </el-col>
              </el-row>
              <el-row :gutter="16">
                <el-col :span="8">
                  <el-form-item
                    label="客户订单号"
                    :prop="'items.' + meta.index + '.customerPo'"
                    :rules="itemRules.customerPo"
                    label-width="100px"
                  >
                    <el-input v-model="formData.items[meta.index].customerPo" placeholder="必填" />
                  </el-form-item>
                </el-col>
                <el-col :span="8">
                  <el-form-item label="销售单价" :prop="'items.' + meta.index + '.price'" label-width="100px">
                    <div class="price-currency-row">
                      <el-input-number
                        v-model="formData.items[meta.index].price"
                        :min="0"
                        :precision="4"
                        :controls="false"
                        class="price-input"
                      />
                      <el-select v-model="formData.items[meta.index].currency" class="currency-mini">
                        <el-option label="RMB" :value="1" />
                        <el-option label="USD" :value="2" />
                        <el-option label="EUR" :value="3" />
                      </el-select>
                    </div>
                  </el-form-item>
                </el-col>
                <el-col :span="8">
                  <el-form-item label="买入价" label-width="72px">
                    <el-input :model-value="formatMoney(formData.items[meta.index].purchasePriceDisplay)" disabled />
                  </el-form-item>
                </el-col>
              </el-row>
              <el-row :gutter="16">
                <el-col :span="8">
                  <el-form-item
                    label="销售数量"
                    :prop="'items.' + meta.index + '.qty'"
                    :rules="itemRules.qty"
                    label-width="100px"
                  >
                    <el-input-number
                      v-model="formData.items[meta.index].qty"
                      :min="1"
                      :controls="false"
                      style="width: 100%"
                    />
                  </el-form-item>
                </el-col>
                <el-col :span="8">
                  <el-form-item label="销售总额" label-width="100px">
                    <div class="total-inline">
                      <span>{{ formatMoney(lineLineTotal(meta.index)) }}</span>
                      <span class="ccy-tag">{{ currencyCode(formData.items[meta.index].currency) }}</span>
                    </div>
                  </el-form-item>
                </el-col>
                <el-col :span="8">
                  <el-form-item
                    label="生产日期"
                    :prop="'items.' + meta.index + '.dateCode'"
                    :rules="itemRules.dateCode"
                    label-width="100px"
                  >
                    <el-select v-model="formData.items[meta.index].dateCode" placeholder="必选" style="width: 100%" filterable allow-create>
                      <el-option label="2年内" value="2年内" />
                      <el-option label="1年内" value="1年内" />
                      <el-option label="无要求" value="无要求" />
                    </el-select>
                  </el-form-item>
                </el-col>
              </el-row>
              <el-row :gutter="16">
                <el-col :span="8">
                  <el-form-item
                    label="交期"
                    :prop="'items.' + meta.index + '.deliveryDate'"
                    :rules="itemRules.deliveryDate"
                    label-width="100px"
                  >
                    <el-date-picker
                      v-model="formData.items[meta.index].deliveryDate"
                      type="date"
                      placeholder="选择交期"
                      style="width: 100%"
                      value-format="YYYY-MM-DD"
                    />
                  </el-form-item>
                </el-col>
                <el-col :span="16">
                  <el-form-item label="备注" label-width="100px">
                    <el-input v-model="formData.items[meta.index].comment" type="textarea" :rows="2" placeholder="行备注" />
                  </el-form-item>
                </el-col>
              </el-row>
              <div class="material-card-actions">
                <el-button link type="danger" size="small" @click="removeItem(meta.index)">删除本行</el-button>
              </div>
            </div>
          </div>

          <div class="grand-total-row">
            <span class="gt-label">合计金额：</span>
            <span class="gt-amount">{{ formatCurrency(calculateTotal, formData.currency) }}</span>
          </div>
        </el-collapse-item>
      </el-collapse>
    </el-form>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { ArrowLeft, Check, Plus } from '@element-plus/icons-vue'
import { ElMessage } from 'element-plus'
import type { FormRules } from 'element-plus'
import { salesOrderApi } from '@/api/salesOrder'
import { quoteApi } from '@/api/quote'
import { rfqApi } from '@/api/rfq'
import { customerApi } from '@/api/customer'
import { authApi } from '@/api/auth'
import type { RFQ, RFQItem } from '@/types/rfq'
import type { Customer } from '@/types/customer'
import { resolveCustomerIdFromQuoteDetail } from '@/utils/quoteSalesOrderPrefill'
import { runValidatedFormSave } from '@/composables/useFormSubmit'
import { useAuthStore } from '@/stores/auth'

const router = useRouter()
const route = useRoute()
const authStore = useAuthStore()

const DEFAULT_OWNER = '鸿泰电子'

function mapQuoteCurrencyToOrderCurrency(c: number): number {
  const m: Record<number, number> = { 0: 1, 1: 2, 2: 3, 3: 2 }
  return m[c] ?? 1
}

function currencyCode(c?: number) {
  if (c === 2) return 'USD'
  if (c === 3) return 'EUR'
  return 'RMB'
}

function formatMoney(n: number) {
  return (n || 0).toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 6 })
}

const MANUAL_CUSTOMER_ID = '00000000-0000-0000-0000-000000000001'
const formRef = ref()
const submitLoading = ref(false)
const prefillCustomerId = ref<string | undefined>(undefined)
const prefillSalesUserId = ref<string | undefined>(undefined)

const collapseActive = ref(['order', 'customer', 'items'])
const itemFilterKeyword = ref('')

const salesUserOptions = ref<{ value: string; label: string }[]>([])
const customerOptions = ref<{ value: string; label: string }[]>([])
const customerSearchLoading = ref(false)
const contactOptions = ref<{ value: string; label: string }[]>([])
let customerSearchTimer: ReturnType<typeof setTimeout> | null = null

const getYYMMDD = (d: Date) => {
  const yy = String(d.getFullYear()).slice(-2)
  const mm = String(d.getMonth() + 1).padStart(2, '0')
  const dd = String(d.getDate()).padStart(2, '0')
  return `${yy}${mm}${dd}`
}

const genOrderCode = () => {
  const date = getYYMMDD(new Date())
  const seq = String(Math.floor(Math.random() * 10000)).padStart(4, '0')
  return `SO${date}${seq}`
}

type OrderLineDraft = {
  quoteId?: string
  pn: string
  customerMaterialModel: string
  brand: string
  customerPo: string
  price: number
  currency: number
  purchasePriceDisplay: number
  qty: number
  dateCode: string
  deliveryDate: string
  comment: string
  purchaseQuoteLabel: string
}

function emptyLine(): OrderLineDraft {
  return {
    quoteId: undefined,
    pn: '',
    customerMaterialModel: '',
    brand: '',
    customerPo: '',
    price: 0,
    currency: 1,
    purchasePriceDisplay: 0,
    qty: 1,
    dateCode: '2年内',
    deliveryDate: '',
    comment: '',
    purchaseQuoteLabel: ''
  }
}

const formData = ref({
  sellOrderCode: genOrderCode(),
  type: 1 as number,
  salesUserId: '' as string,
  salesUserName: '' as string,
  productKind: '现货',
  ownerEntity: DEFAULT_OWNER,
  orderRemark: '',
  customerId: '' as string,
  customerName: '' as string,
  customerContactId: '' as string,
  customerContactName: '' as string,
  invoiceInfo: '',
  paymentTermsLabel: 'NET 30',
  deliveryAddress: '',
  currency: 1,
  items: [] as OrderLineDraft[]
})

const itemRules = {
  pn: [{ required: true, message: '请输入物料型号', trigger: 'blur' }],
  brand: [{ required: true, message: '请输入品牌', trigger: 'blur' }],
  customerPo: [{ required: true, message: '请输入客户订单号', trigger: 'blur' }],
  qty: [{ required: true, message: '请输入数量', trigger: 'change' }],
  dateCode: [{ required: true, message: '请选择生产日期要求', trigger: 'change' }],
  deliveryDate: [{ required: true, message: '请选择交期', trigger: 'change' }]
}

const formRules: FormRules = {
  type: [{ required: true, message: '请选择订单类型', trigger: 'change' }],
  salesUserId: [{ required: true, message: '请选择负责人', trigger: 'change' }],
  productKind: [{ required: true, message: '请选择产品', trigger: 'change' }],
  customerId: [{ required: true, message: '请选择客户', trigger: 'change' }],
  customerContactId: [{ required: true, message: '请选择客户联系人', trigger: 'change' }],
  paymentTermsLabel: [{ required: true, message: '请填写或选择账期', trigger: 'change' }]
}

const filteredItemsView = computed(() => {
  const kw = itemFilterKeyword.value.trim().toLowerCase()
  return formData.value.items
    .map((it, index) => ({ index, pn: it.pn }))
    .filter((x) => !kw || String(x.pn).toLowerCase().includes(kw))
})

const calculateTotal = computed(() =>
  formData.value.items.reduce((sum, item) => sum + (item.qty || 0) * (item.price || 0), 0)
)

function lineLineTotal(index: number) {
  const it = formData.value.items[index]
  return (it?.qty || 0) * (it?.price || 0)
}

const formatCurrency = (value: number, currency?: number) => {
  const symbol = currency === 2 ? '$' : currency === 3 ? '€' : '¥'
  return symbol + (value || 0).toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

async function loadSalesUsers() {
  try {
    const raw = (await authApi.getUsers()) as unknown
    const list = Array.isArray(raw)
      ? raw
      : raw && typeof raw === 'object' && 'data' in raw && Array.isArray((raw as { data: unknown }).data)
        ? ((raw as { data: { id: string; label?: string; userName?: string; realName?: string }[] }).data)
        : []
    salesUserOptions.value = list.map((u) => ({
      value: u.id,
      label: u.realName || u.label || u.userName || u.id
    }))
  } catch {
    salesUserOptions.value = []
  }
}

function onSalesUserChange(id: string) {
  const u = salesUserOptions.value.find((x) => x.value === id)
  formData.value.salesUserName = u?.label || ''
  prefillSalesUserId.value = id || undefined
}

async function onCustomerFilterInput(query: string) {
  if (customerSearchTimer) clearTimeout(customerSearchTimer)
  if (!query || query.trim().length < 1) {
    return
  }
  customerSearchTimer = setTimeout(async () => {
    customerSearchLoading.value = true
    try {
      const res = await customerApi.searchCustomers({
        pageNumber: 1,
        pageSize: 30,
        searchTerm: query.trim()
      })
      customerOptions.value = (res.items || []).map((c) => ({
        value: c.id,
        label: c.customerName || (c as { officialName?: string }).officialName || '未知客户'
      }))
    } catch {
      customerOptions.value = []
    } finally {
      customerSearchLoading.value = false
    }
  }, 300)
}

function syncInvoiceFromCustomer(c: Customer) {
  const name = c.customerName || ''
  const tax = c.unifiedSocialCreditCode || ''
  formData.value.invoiceInfo = tax ? `${name} (${tax})` : name
  if (c.paymentTerms != null && c.paymentTerms > 0) {
    formData.value.paymentTermsLabel = `NET ${c.paymentTerms}`
  }
}

async function loadCustomerDetail(id: string) {
  if (!id) {
    contactOptions.value = []
    return
  }
  try {
    const c = await customerApi.getCustomerById(id)
    syncInvoiceFromCustomer(c)
    const list = c.contacts || []
    contactOptions.value = list.map((x) => ({
      value: x.id,
      label: x.contactName || x.mobilePhone || x.id
    }))
    if (!formData.value.customerContactId && list.length) {
      const def = list.find((x) => x.isDefault) || list[0]
      if (def) {
        formData.value.customerContactId = def.id
        formData.value.customerContactName = def.contactName || ''
      }
    }
  } catch {
    contactOptions.value = []
  }
}

function onCustomerChange(val: string) {
  const found = customerOptions.value.find((c) => c.value === val)
  if (found) formData.value.customerName = found.label
  prefillCustomerId.value = val || undefined
  formData.value.customerContactId = ''
  formData.value.customerContactName = ''
  void loadCustomerDetail(val)
}

function onContactChange(id: string) {
  const c = contactOptions.value.find((x) => x.value === id)
  formData.value.customerContactName = c?.label || ''
}

const addItem = () => {
  const line = emptyLine()
  line.currency = formData.value.currency
  formData.value.items.push(line)
}

const removeItem = (index: number) => {
  formData.value.items.splice(index, 1)
}

function applyRfqHeaderToForm(rfq: RFQ) {
  prefillCustomerId.value = rfq.customerId?.trim() || undefined
  prefillSalesUserId.value = rfq.salesUserId?.trim() || undefined
  if (rfq.customerId) {
    formData.value.customerId = rfq.customerId
    formData.value.customerName = String(rfq.customerName ?? '').trim()
    customerOptions.value = [
      { value: rfq.customerId, label: formData.value.customerName || '客户' }
    ]
    void loadCustomerDetail(rfq.customerId)
  }
  if (rfq.salesUserId) {
    formData.value.salesUserId = rfq.salesUserId
    formData.value.salesUserName = String(rfq.salesUserName ?? '').trim()
  }
}

function buildHeaderComment(): string {
  const lines: string[] = []
  lines.push(`归属：${formData.value.ownerEntity}`)
  lines.push(`产品：${formData.value.productKind}`)
  if (formData.value.customerContactName) {
    lines.push(`客户联系人：${formData.value.customerContactName}`)
  }
  if (formData.value.invoiceInfo?.trim()) {
    lines.push(`发票信息：${formData.value.invoiceInfo.trim()}`)
  }
  if (formData.value.paymentTermsLabel?.trim()) {
    lines.push(`账期：${formData.value.paymentTermsLabel.trim()}`)
  }
  if (formData.value.orderRemark?.trim()) {
    lines.push('')
    lines.push(formData.value.orderRemark.trim())
  }
  return lines.join('\n').trim()
}

function buildItemComment(it: OrderLineDraft): string | undefined {
  const parts: string[] = []
  if (it.customerMaterialModel?.trim()) {
    parts.push(`客户物料型号：${it.customerMaterialModel.trim()}`)
  }
  if (it.comment?.trim()) {
    parts.push(it.comment.trim())
  }
  return parts.length ? parts.join('\n') : undefined
}

function parseReturnTo(): string | null {
  const raw = route.query.returnTo
  const s = Array.isArray(raw) ? raw[0] : raw
  if (typeof s !== 'string' || !s.trim()) return null
  let path = s.trim()
  try {
    path = decodeURIComponent(path)
  } catch {
    return null
  }
  if (!path.startsWith('/') || path.startsWith('//')) return null
  if (/^[a-zA-Z][a-zA-Z0-9+.-]*:/.test(path)) return null
  return path
}

function handleBack() {
  const back = parseReturnTo()
  if (back) {
    router.push(back)
    return
  }
  router.push({ name: 'SalesOrderList' })
}

function parseQuoteIdsFromRoute(): string[] {
  const raw = route.query.quoteIds
  if (Array.isArray(raw)) {
    return [
      ...new Set(
        raw.flatMap((x) => String(x).split(',')).map((s) => s.trim()).filter(Boolean)
      )
    ]
  }
  if (typeof raw === 'string' && raw.trim()) {
    return [...new Set(raw.split(',').map((s) => s.trim()).filter(Boolean))]
  }
  const legacy = typeof route.query.quoteId === 'string' ? route.query.quoteId.trim() : ''
  return legacy ? [legacy] : []
}

function purchaseQuoteLabelFromRow(first: Record<string, unknown> | undefined): string {
  if (!first) return ''
  const p = Number(first.unitPrice) || 0
  const cur = mapQuoteCurrencyToOrderCurrency(Number(first.currency ?? 0))
  const code = currencyCode(cur)
  return `采购报价：${formatMoney(p)} ${code}`
}

onMounted(async () => {
  await loadSalesUsers()

  const user = authStore.user
  if (user?.id && !formData.value.salesUserId) {
    formData.value.salesUserId = user.id
    formData.value.salesUserName = user.userName || ''
    prefillSalesUserId.value = user.id
  }

  const ids = parseQuoteIdsFromRoute()
  if (!ids.length) {
    addItem()
    return
  }

  try {
    const quotesRes = await Promise.all(ids.map((id) => quoteApi.getById(id)))
    const quotes = quotesRes
      .map((r) => r?.data as Record<string, unknown> | undefined)
      .filter((q): q is Record<string, unknown> => q != null)

    if (quotes.length !== ids.length) {
      ElMessage.error('部分报价单不存在或无权访问')
      addItem()
      return
    }

    const customerIds = await Promise.all(quotes.map((q) => resolveCustomerIdFromQuoteDetail(q)))
    const nonempty = customerIds.filter(Boolean)
    if (nonempty.length >= 2 && new Set(nonempty).size > 1) {
      ElMessage.error('请选择同一家客户生成销售订单')
      addItem()
      return
    }

    const firstRfqId = String(quotes[0]?.rfqId ?? quotes[0]?.RFQId ?? '').trim()
    if (firstRfqId) {
      try {
        const rfq = await rfqApi.getRFQById(firstRfqId)
        applyRfqHeaderToForm(rfq)
      } catch {
        /* ignore */
      }
    }

    if (!formData.value.salesUserId && user?.id) {
      formData.value.salesUserId = user.id
      formData.value.salesUserName = user.userName || ''
    }

    const lines: OrderLineDraft[] = []
    for (let idx = 0; idx < quotes.length; idx++) {
      const q = quotes[idx]
      const qid = ids[idx]
      const rfqItemId = String(q.rfqItemId ?? q.RfqItemId ?? '').trim()
      let pn = String(q.mpn ?? '')
      let brand = ''
      let reqQty = 1
      let custMpn = ''
      if (rfqItemId) {
        try {
          const ri = await rfqApi.getRFQItemById(rfqItemId)
          const riExt = ri as RFQItem & { mpn?: string }
          pn = String(ri.materialModel ?? riExt.mpn ?? pn)
          brand = String(ri.brand ?? '')
          reqQty = Math.max(1, Number(ri.quantity) || 1)
          custMpn = String(ri.customerMaterialModel ?? '')
        } catch {
          /* 仅用报价头 */
        }
      }
      const rawItems = (q.items as Record<string, unknown>[] | undefined) || []
      const headerMpn = String(q.mpn ?? '')
      const first = rawItems[0]
      const purchase = Number(first?.unitPrice) || 0
      const line: OrderLineDraft = {
        quoteId: qid,
        pn: String(first?.mpn ?? pn ?? headerMpn),
        customerMaterialModel: custMpn,
        brand: String(first?.brand ?? brand),
        customerPo: '',
        qty: Math.max(1, Number(first?.quantity) || reqQty),
        price: purchase,
        currency: first ? mapQuoteCurrencyToOrderCurrency(Number(first.currency ?? 0)) : formData.value.currency,
        purchasePriceDisplay: purchase,
        dateCode: '2年内',
        deliveryDate: '',
        comment: '',
        purchaseQuoteLabel: purchaseQuoteLabelFromRow(first)
      }
      if (line.pn || headerMpn) {
        lines.push(line)
      }
    }

    if (lines.length) {
      formData.value.items = lines
      formData.value.currency = lines[0].currency ?? formData.value.currency
    } else {
      addItem()
    }
  } catch {
    ElMessage.error('加载报价失败')
    addItem()
  }
})

const handleSubmit = async () => {
  await runValidatedFormSave(formRef, {
    loading: submitLoading,
    successMessage: '销售订单创建成功',
    task: async () => {
      const firstLineCur = formData.value.items[0]?.currency
      const headerCurrency = firstLineCur ?? formData.value.currency
      await salesOrderApi.create({
        sellOrderCode: formData.value.sellOrderCode,
        customerId: formData.value.customerId || prefillCustomerId.value || MANUAL_CUSTOMER_ID,
        customerName: formData.value.customerName,
        salesUserId: formData.value.salesUserId || prefillSalesUserId.value || authStore.user?.id || undefined,
        salesUserName: formData.value.salesUserName,
        type: formData.value.type,
        currency: headerCurrency,
        deliveryDate: null,
        deliveryAddress: formData.value.deliveryAddress || undefined,
        comment: buildHeaderComment() || undefined,
        items: formData.value.items.map((it) => ({
          quoteId: it.quoteId,
          pn: it.pn,
          brand: it.brand,
          customerPnNo: it.customerPo,
          qty: it.qty,
          price: it.price,
          currency: it.currency ?? headerCurrency,
          dateCode: it.dateCode || undefined,
          deliveryDate: it.deliveryDate || null,
          comment: buildItemComment(it)
        }))
      })
    },
    onSuccess: () => {
      const back = parseReturnTo()
      if (back) router.push(back)
      else router.push({ name: 'SalesOrderList' })
    },
    errorMessage: () => '创建失败，请重试'
  })
}
</script>

<style scoped lang="scss">
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
  :deep(.so-collapse) {
    border: none;
    background: transparent;
  }
  :deep(.so-collapse .el-collapse-item) {
    background: #0a1828;
    border: 1px solid #1a2d45;
    border-radius: 8px;
    margin-bottom: 14px;
    overflow: hidden;
  }
  :deep(.so-collapse .el-collapse-item__header) {
    background: rgba(0, 212, 255, 0.06);
    border-bottom: 1px solid rgba(0, 212, 255, 0.12);
    color: #c8dff0;
    font-weight: 600;
    font-size: 14px;
    padding: 0 18px;
    height: 44px;
  }
  :deep(.so-collapse .el-collapse-item__wrap) {
    background: transparent;
    border: none;
  }
  :deep(.so-collapse .el-collapse-item__content) {
    padding: 16px 18px 8px;
    color: #c8dff0;
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

.select-hint {
  padding: 8px 12px;
  color: #5a7a9a;
  font-size: 12px;
}

.items-toolbar {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-bottom: 14px;
  .item-filter {
    max-width: 280px;
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
  .head-mpn { color: #c8dff0; font-weight: 600; }
  .head-quote { color: #5a7a9a; }
}

.material-card-body {
  padding: 12px 14px 4px;
}

.price-currency-row {
  display: flex;
  align-items: center;
  gap: 8px;
  width: 100%;
  .price-input { flex: 1; min-width: 0; }
  .currency-mini { width: 88px; flex-shrink: 0; }
}

.total-inline {
  display: flex;
  align-items: center;
  gap: 8px;
  color: #00c8ff;
  font-weight: 600;
  .ccy-tag {
    font-size: 12px;
    color: #5a7a9a;
    font-weight: 500;
  }
}

.material-card-actions {
  display: flex;
  justify-content: flex-end;
  padding-bottom: 8px;
}

.grand-total-row {
  display: flex;
  justify-content: flex-end;
  align-items: center;
  padding: 12px 4px 8px;
  gap: 8px;
  .gt-label { color: #5a7a9a; font-size: 13px; }
  .gt-amount { color: #00c8ff; font-size: 16px; font-weight: 700; }
}

:deep(.el-input-number .el-input__wrapper) {
  background: #0d1e35;
  border-color: #1a2d45;
}
:deep(.el-input-number .el-input__inner) {
  color: #c8dff0;
  text-align: left;
}
</style>
