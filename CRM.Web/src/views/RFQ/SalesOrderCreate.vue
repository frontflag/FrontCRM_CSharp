<template>
  <div class="create-page">
    <div class="page-header">
      <div class="header-left">
        <el-button link @click="handleBack">
          <el-icon><ArrowLeft /></el-icon> {{ t('salesOrderCreate.backToList') }}
        </el-button>
        <el-breadcrumb separator="/">
          <el-breadcrumb-item>{{ t('salesOrderCreate.breadcrumb.orders') }}</el-breadcrumb-item>
          <el-breadcrumb-item>{{ t('salesOrderCreate.breadcrumb.sales') }}</el-breadcrumb-item>
          <el-breadcrumb-item>{{
            isEditMode ? t('salesOrderCreate.breadcrumb.editOrder') : t('salesOrderCreate.breadcrumb.newOrder')
          }}</el-breadcrumb-item>
        </el-breadcrumb>
      </div>
      <div class="header-right">
        <el-button @click="handleBack">{{ t('salesOrderCreate.cancel') }}</el-button>
        <el-button type="primary" :loading="submitLoading" @click="handleSubmit">
          <el-icon><Check /></el-icon> {{ t('salesOrderCreate.save') }}
        </el-button>
      </div>
    </div>

    <el-form ref="formRef" :model="formData" :rules="formRules" label-width="108px" class="create-form">
      <el-collapse v-model="collapseActive" class="so-collapse">
        <el-collapse-item :title="t('salesOrderCreate.sections.order')" name="order">
          <el-row :gutter="20">
            <el-col :span="12">
              <el-form-item :label="t('salesOrderCreate.fields.sellOrderCode')">
                <el-input v-model="formData.sellOrderCode" disabled :placeholder="t('salesOrderCreate.placeholders.autoCode')" />
              </el-form-item>
            </el-col>
            <el-col :span="12">
              <el-form-item :label="t('salesOrderCreate.fields.orderType')" prop="type">
                <el-select v-model="formData.type" :placeholder="t('salesOrderCreate.placeholders.orderType')" style="width: 100%">
                  <el-option :label="t('salesOrderCreate.orderTypes.normal')" :value="1" />
                  <el-option :label="t('salesOrderCreate.orderTypes.urgent')" :value="2" />
                  <el-option :label="t('salesOrderCreate.orderTypes.sample')" :value="3" />
                </el-select>
              </el-form-item>
            </el-col>
          </el-row>
          <el-row :gutter="20">
            <el-col :span="12">
              <el-form-item :label="t('salesOrderCreate.fields.salesUser')" prop="salesUserId">
                <sales-user-cascader
                  v-model="formData.salesUserId"
                  :placeholder="t('salesOrderCreate.placeholders.salesUser')"
                  clearable
                  @change="onSalesUserChange"
                />
              </el-form-item>
            </el-col>
            <el-col :span="12">
              <el-form-item :label="t('salesOrderCreate.fields.product')" prop="productKind">
                <el-select v-model="formData.productKind" :placeholder="t('salesOrderCreate.placeholders.product')" style="width: 100%">
                  <el-option :label="t('salesOrderCreate.productKinds.spot')" value="现货" />
                  <el-option :label="t('salesOrderCreate.productKinds.futures')" value="期货" />
                  <el-option :label="t('salesOrderCreate.productKinds.backlog')" value="排单" />
                  <el-option :label="t('salesOrderCreate.productKinds.sample')" value="样品" />
                </el-select>
              </el-form-item>
            </el-col>
          </el-row>
          <el-row :gutter="20">
            <el-col :span="24">
              <el-form-item :label="t('salesOrderCreate.fields.remark')">
                <el-input v-model="formData.orderRemark" type="textarea" :rows="2" :placeholder="t('salesOrderCreate.placeholders.orderRemark')" />
              </el-form-item>
            </el-col>
          </el-row>
        </el-collapse-item>

        <el-collapse-item :title="t('salesOrderCreate.sections.customer')" name="customer">
          <el-row :gutter="20">
            <el-col :span="12">
              <el-form-item :label="t('salesOrderCreate.fields.customer')" prop="customerId">
                <el-select
                  v-model="formData.customerId"
                  :placeholder="t('salesOrderCreate.placeholders.searchCustomer')"
                  style="width: 100%"
                  filterable
                  :filter-method="onCustomerFilterInput"
                  :loading="customerSearchLoading"
                  :loading-text="t('salesOrderCreate.placeholders.searching')"
                  @change="onCustomerChange"
                >
                  <template #empty>
                    <div class="select-hint">{{ t('salesOrderCreate.placeholders.customerSearchHint') }}</div>
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
              <el-form-item :label="t('salesOrderCreate.fields.customerContact')" prop="customerContactId">
                <el-select
                  v-model="formData.customerContactId"
                  :placeholder="t('salesOrderCreate.placeholders.contact')"
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
              <el-form-item :label="t('salesOrderCreate.fields.invoiceInfo')">
                <el-input v-model="formData.invoiceInfo" type="textarea" :rows="2" :placeholder="t('salesOrderCreate.placeholders.invoiceInfo')" />
              </el-form-item>
            </el-col>
            <el-col :span="12">
              <el-form-item :label="t('salesOrderCreate.fields.paymentTerms')" prop="paymentTermsLabel">
                <el-select
                  v-model="formData.paymentTermsLabel"
                  :placeholder="t('salesOrderCreate.placeholders.paymentTerms')"
                  style="width: 100%"
                  filterable
                  allow-create
                  default-first-option
                >
                  <el-option label="NET 15" value="NET 15" />
                  <el-option label="NET 30" value="NET 30" />
                  <el-option label="NET 45" value="NET 45" />
                  <el-option label="NET 60" value="NET 60" />
                  <el-option :label="t('salesOrderCreate.paymentTermsExtra.prepayment')" value="款到发货" />
                  <el-option :label="t('salesOrderCreate.paymentTermsExtra.cod')" value="货到付款" />
                </el-select>
              </el-form-item>
            </el-col>
          </el-row>
          <el-row :gutter="20">
            <el-col :span="24">
              <el-form-item :label="t('salesOrderCreate.fields.deliveryAddress')">
                <el-input v-model="formData.deliveryAddress" type="textarea" :rows="2" :placeholder="t('salesOrderCreate.placeholders.deliveryAddress')" />
              </el-form-item>
            </el-col>
          </el-row>
        </el-collapse-item>

        <el-collapse-item :title="t('salesOrderCreate.sections.items')" name="items">
          <div class="items-toolbar">
            <el-input
              v-model="itemFilterKeyword"
              class="item-filter"
              clearable
              :placeholder="t('salesOrderCreate.placeholders.filterMpn')"
            />
            <el-button type="primary" size="small" @click="addItem">
              <el-icon><Plus /></el-icon> {{ t('salesOrderCreate.itemsToolbar.addLine') }}
            </el-button>
          </div>

          <div v-if="filteredItemsView.length === 0" class="items-empty">{{ t('salesOrderCreate.itemsEmpty') }}</div>

          <div
            v-for="meta in filteredItemsView"
            v-else
            :key="meta.index"
            class="material-card"
          >
            <div class="material-card-head">
              <span class="head-mpn">{{ t('salesOrderCreate.cardHeadMpn') }}：{{ formData.items[meta.index].pn || '—' }}</span>
              <span class="head-quote">{{ formData.items[meta.index].purchaseQuoteLabel || t('salesOrderCreate.purchaseQuoteFallback') }}</span>
            </div>
            <div class="material-card-body">
              <el-row :gutter="16">
                <el-col :span="8">
                  <el-form-item
                    :label="t('salesOrderCreate.fields.mpn')"
                    :prop="'items.' + meta.index + '.pn'"
                    :rules="itemRules.pn"
                    label-width="100px"
                  >
                    <el-input v-model="formData.items[meta.index].pn" :placeholder="t('salesOrderCreate.placeholders.required')" />
                  </el-form-item>
                </el-col>
                <el-col :span="8">
                  <el-form-item :label="t('salesOrderCreate.fields.customerMpn')" :prop="'items.' + meta.index + '.customerMaterialModel'" label-width="112px">
                    <el-input v-model="formData.items[meta.index].customerMaterialModel" :placeholder="t('salesOrderCreate.placeholders.optional')" />
                  </el-form-item>
                </el-col>
                <el-col :span="8">
                  <el-form-item
                    :label="t('salesOrderCreate.fields.brand')"
                    :prop="'items.' + meta.index + '.brand'"
                    :rules="itemRules.brand"
                    label-width="72px"
                  >
                    <el-input v-model="formData.items[meta.index].brand" :placeholder="t('salesOrderCreate.placeholders.required')" />
                  </el-form-item>
                </el-col>
              </el-row>
              <el-row :gutter="16">
                <el-col :span="8">
                  <el-form-item
                    :label="t('salesOrderCreate.fields.customerPo')"
                    :prop="'items.' + meta.index + '.customerPo'"
                    :rules="itemRules.customerPo"
                    label-width="100px"
                  >
                    <el-input v-model="formData.items[meta.index].customerPo" :placeholder="t('salesOrderCreate.placeholders.required')" />
                  </el-form-item>
                </el-col>
                <el-col :span="8">
                  <el-form-item :label="t('salesOrderCreate.fields.unitPrice')" :prop="'items.' + meta.index + '.price'" label-width="100px">
                    <SettlementCurrencyAmountInput
                      v-model="formData.items[meta.index].price"
                      v-model:currency="formData.items[meta.index].currency"
                      :min="0"
                      :precision="6"
                    />
                  </el-form-item>
                </el-col>
                <el-col :span="8">
                  <el-form-item :label="t('salesOrderCreate.fields.purchasePrice')" label-width="96px">
                    <el-input
                      :model-value="
                        `${formatUnitPriceNumber(formData.items[meta.index].purchasePriceDisplay)} ${currencyCode(formData.items[meta.index].currency)}`
                      "
                      disabled
                    />
                  </el-form-item>
                </el-col>
              </el-row>
              <el-row :gutter="16">
                <el-col :span="8">
                  <el-form-item
                    :label="t('salesOrderCreate.fields.qty')"
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
                  <el-form-item :label="t('salesOrderCreate.fields.lineTotal')" label-width="100px">
                    <div class="total-inline">
                      <span>{{ formatTotalAmountNumber(lineLineTotal(meta.index)) }}</span>
                      <span class="ccy-tag">{{ currencyCode(formData.items[meta.index].currency) }}</span>
                    </div>
                  </el-form-item>
                </el-col>
                <el-col :span="8">
                  <el-form-item
                    :label="t('salesOrderCreate.fields.dateCode')"
                    :prop="'items.' + meta.index + '.dateCode'"
                    :rules="itemRules.dateCode"
                    label-width="100px"
                  >
                    <MaterialProductionDateSelect
                      v-model="formData.items[meta.index].dateCode"
                      :placeholder="t('salesOrderCreate.placeholders.dateCode')"
                      :clearable="false"
                    />
                  </el-form-item>
                </el-col>
              </el-row>
              <el-row :gutter="16">
                <el-col :span="8">
                  <el-form-item
                    :label="t('salesOrderCreate.fields.deliveryDate')"
                    :prop="'items.' + meta.index + '.deliveryDate'"
                    :rules="itemRules.deliveryDate"
                    label-width="100px"
                  >
                    <el-date-picker
                      v-model="formData.items[meta.index].deliveryDate"
                      type="date"
                      :placeholder="t('salesOrderCreate.placeholders.pickDeliveryDate')"
                      style="width: 100%"
                      value-format="YYYY-MM-DD"
                    />
                  </el-form-item>
                </el-col>
                <el-col :span="16">
                  <el-form-item :label="t('salesOrderCreate.fields.lineRemark')" label-width="100px">
                    <el-input v-model="formData.items[meta.index].comment" type="textarea" :rows="2" :placeholder="t('salesOrderCreate.placeholders.lineRemark')" />
                  </el-form-item>
                </el-col>
              </el-row>
              <div class="material-card-actions">
                <el-button link type="danger" size="small" @click="removeItem(meta.index)">{{ t('salesOrderCreate.deleteLine') }}</el-button>
              </div>
            </div>
          </div>

          <div class="grand-total-row">
            <span class="gt-label">{{ t('salesOrderCreate.grandTotal') }}</span>
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
import { useI18n } from 'vue-i18n'
import { ArrowLeft, Check, Plus } from '@element-plus/icons-vue'
import { ElMessage } from 'element-plus'
import type { FormRules } from 'element-plus'
import { salesOrderApi } from '@/api/salesOrder'
import { quoteApi } from '@/api/quote'
import { rfqApi } from '@/api/rfq'
import { customerApi } from '@/api/customer'
import type { RFQ, RFQItem } from '@/types/rfq'
import type { Customer } from '@/types/customer'
import { resolveCustomerIdFromQuoteDetail } from '@/utils/quoteSalesOrderPrefill'
import { runValidatedFormSave } from '@/composables/useFormSubmit'
import { getApiErrorMessage } from '@/utils/apiError'
import { useAuthStore } from '@/stores/auth'
import SalesUserCascader from '@/components/SalesUserCascader.vue'
import MaterialProductionDateSelect from '@/components/MaterialProductionDateSelect.vue'
import SettlementCurrencyAmountInput from '@/components/SettlementCurrencyAmountInput.vue'
import { useMaterialProductionDateDict } from '@/composables/useMaterialProductionDateDict'
import { CURRENCY_CODE_TO_TEXT } from '@/constants/currency'
import { formatTotalAmountNumber, formatUnitPriceNumber } from '@/utils/moneyFormat'

const router = useRouter()
const route = useRoute()
const { t, locale } = useI18n()
const authStore = useAuthStore()

/** 与采购订单一致：编辑走独立路由 `SalesOrderEdit`（/sales-orders/:id/edit） */
const editId = computed(() => (route.name === 'SalesOrderEdit' ? String(route.params.id || '').trim() : ''))
const isEditMode = computed(() => !!editId.value)
const { ensureLoaded: ensureMaterialPdDict, defaultCode: defaultProductionDateCode, coerceProductionDateToCode: coercePd } =
  useMaterialProductionDateDict()

/**
 * 报价明细 currency 与销售订单 settlement 使用同一套编码（与后端 QuoteItem / SellOrder 一致）：
 * 1=RMB 2=USD 3=EUR 4=HKD 5=JPY 6=GBP。历史上错误的 0→1、2→EUR 映射已移除。
 */
function mapQuoteCurrencyToOrderCurrency(c: unknown): number {
  const n = Number(c)
  if (!Number.isFinite(n)) return 1
  if (n >= 1 && n <= 6) return n
  if (n === 0) return 1
  return 1
}

/** 展示用 ISO 代码（含 HKD 等结算币种） */
function currencyCode(c?: number) {
  const n = Number(c)
  if (!Number.isFinite(n)) return 'RMB'
  return CURRENCY_CODE_TO_TEXT[n] ?? 'RMB'
}

/** 从报价行取单价，保留小数（兼容 camelCase / PascalCase） */
function quoteRowUnitPrice(row: Record<string, unknown> | undefined): number {
  if (!row) return 0
  const raw = row.unitPrice ?? row.UnitPrice
  if (raw == null || raw === '') return 0
  const n = typeof raw === 'number' ? raw : parseFloat(String(raw).replace(/,/g, ''))
  return Number.isFinite(n) ? n : 0
}

const MANUAL_CUSTOMER_ID = '00000000-0000-0000-0000-000000000001'
const formRef = ref()
const submitLoading = ref(false)
const prefillCustomerId = ref<string | undefined>(undefined)
const prefillSalesUserId = ref<string | undefined>(undefined)

const collapseActive = ref(['order', 'customer', 'items'])
const itemFilterKeyword = ref('')

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
    dateCode: '',
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

const itemRules = computed(() => ({
  pn: [{ required: true, message: t('salesOrderCreate.validation.pn'), trigger: 'blur' }],
  brand: [{ required: true, message: t('salesOrderCreate.validation.brand'), trigger: 'blur' }],
  customerPo: [{ required: true, message: t('salesOrderCreate.validation.customerPo'), trigger: 'blur' }],
  qty: [{ required: true, message: t('salesOrderCreate.validation.qty'), trigger: 'change' }],
  dateCode: [{ required: true, message: t('salesOrderCreate.validation.dateCode'), trigger: 'change' }],
  deliveryDate: [{ required: true, message: t('salesOrderCreate.validation.deliveryDate'), trigger: 'change' }]
}))

const formRules = computed<FormRules>(() => ({
  type: [{ required: true, message: t('salesOrderCreate.validation.type'), trigger: 'change' }],
  salesUserId: [{ required: true, message: t('salesOrderCreate.validation.salesUserId'), trigger: 'change' }],
  productKind: [{ required: true, message: t('salesOrderCreate.validation.productKind'), trigger: 'change' }],
  customerId: [{ required: true, message: t('salesOrderCreate.validation.customerId'), trigger: 'change' }],
  customerContactId: [{ required: true, message: t('salesOrderCreate.validation.customerContactId'), trigger: 'change' }],
  paymentTermsLabel: [{ required: true, message: t('salesOrderCreate.validation.paymentTermsLabel'), trigger: 'change' }]
}))

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
  const symbol =
    currency === 2 ? '$' : currency === 3 ? '€' : currency === 4 ? 'HK$' : '¥'
  const loc = locale.value === 'zh-CN' ? 'zh-CN' : 'en-US'
  return symbol + (value || 0).toLocaleString(loc, { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

function onSalesUserChange(payload: { id: string; label: string }) {
  formData.value.salesUserName = payload?.label || ''
  prefillSalesUserId.value = payload?.id || undefined
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
        label: c.customerName || (c as { officialName?: string }).officialName || t('salesOrderCreate.unknownCustomer')
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

async function loadCustomerDetail(id: string, opts?: { skipInvoiceSync?: boolean }) {
  if (!id) {
    contactOptions.value = []
    return
  }
  try {
    const c = await customerApi.getCustomerById(id)
    if (!opts?.skipInvoiceSync) {
      syncInvoiceFromCustomer(c)
    }
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
  line.dateCode = defaultProductionDateCode()
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
      { value: rfq.customerId, label: formData.value.customerName || t('salesOrderCreate.customerFallback') }
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
  lines.push(`${t('salesOrderCreate.comment.product')}：${formData.value.productKind}`)
  if (formData.value.customerContactName) {
    lines.push(`${t('salesOrderCreate.comment.contact')}：${formData.value.customerContactName}`)
  }
  if (formData.value.invoiceInfo?.trim()) {
    lines.push(`${t('salesOrderCreate.comment.invoice')}：${formData.value.invoiceInfo.trim()}`)
  }
  if (formData.value.paymentTermsLabel?.trim()) {
    lines.push(`${t('salesOrderCreate.comment.terms')}：${formData.value.paymentTermsLabel.trim()}`)
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
    parts.push(`${t('salesOrderCreate.comment.customerMpn')}：${it.customerMaterialModel.trim()}`)
  }
  if (it.comment?.trim()) {
    parts.push(it.comment.trim())
  }
  return parts.length ? parts.join('\n') : undefined
}

/** 与 buildItemComment 对称：从明细 comment 拆出「客户物料型号」与行备注，避免编辑加载后重复前缀、保存串味 */
function parseItemCommentForDraft(raw: string | null | undefined): { customerMaterialModel: string; comment: string } {
  const s = String(raw ?? '')
    .replace(/\r\n/g, '\n')
    .trim()
  if (!s) return { customerMaterialModel: '', comment: '' }
  const prefix = `${t('salesOrderCreate.comment.customerMpn')}：`
  if (s.startsWith(prefix)) {
    const rest = s.slice(prefix.length).trim()
    const nl = rest.indexOf('\n')
    if (nl >= 0) {
      return {
        customerMaterialModel: rest.slice(0, nl).trim(),
        comment: rest.slice(nl + 1).trim()
      }
    }
    return { customerMaterialModel: rest, comment: '' }
  }
  return { customerMaterialModel: '', comment: s }
}

const PRODUCT_KINDS = new Set(['现货', '期货', '排单', '样品'])

function pickRowStr(obj: Record<string, unknown>, ...keys: string[]): string {
  for (const k of keys) {
    const v = obj[k]
    if (v != null && v !== '') return String(v).trim()
  }
  return ''
}

/** 反向解析「订单信息」折叠区写入主表 comment 的结构化前缀行（与 buildHeaderComment 对应） */
function parseHeaderCommentBlocks(comment: string | null | undefined) {
  const blocks = {
    productKind: '',
    customerContactName: '',
    invoiceInfo: '',
    paymentTermsLabel: '',
    orderRemark: ''
  }
  const raw = String(comment ?? '')
    .replace(/\r\n/g, '\n')
    .trim()
  if (!raw) return blocks
  const loose: string[] = []
  const prefix = (k: 'product' | 'contact' | 'invoice' | 'terms') => `${t(`salesOrderCreate.comment.${k}`)}：`
  for (const line of raw.split('\n').map((l) => l.trim())) {
    if (!line) continue
    const pp = prefix('product')
    const pc = prefix('contact')
    const pi = prefix('invoice')
    const pt = prefix('terms')
    if (line.startsWith(pp)) blocks.productKind = line.slice(pp.length).trim()
    else if (line.startsWith(pc)) blocks.customerContactName = line.slice(pc.length).trim()
    else if (line.startsWith(pi)) blocks.invoiceInfo = line.slice(pi.length).trim()
    else if (line.startsWith(pt)) blocks.paymentTermsLabel = line.slice(pt.length).trim()
    else loose.push(line)
  }
  blocks.orderRemark = loose.join('\n').trim()
  return blocks
}

async function loadOrderForEdit(id: string) {
  const order = (await salesOrderApi.getById(id)) as Record<string, unknown>
  const itemsRaw = order.items ?? order.Items
  const items = Array.isArray(itemsRaw) ? (itemsRaw as Record<string, unknown>[]) : []

  formData.value.sellOrderCode = pickRowStr(order, 'sellOrderCode', 'SellOrderCode') || formData.value.sellOrderCode
  formData.value.type = Number(order.type ?? order.Type ?? 1) || 1
  formData.value.salesUserId = pickRowStr(order, 'salesUserId', 'SalesUserId')
  formData.value.salesUserName = pickRowStr(order, 'salesUserName', 'SalesUserName')
  formData.value.currency = Number(order.currency ?? order.Currency ?? 1) || 1
  formData.value.customerId = pickRowStr(order, 'customerId', 'CustomerId')
  formData.value.customerName = pickRowStr(order, 'customerName', 'CustomerName')
  formData.value.deliveryAddress = pickRowStr(order, 'deliveryAddress', 'DeliveryAddress')

  const rawOrderComment = pickRowStr(order, 'comment', 'Comment')
  /** 与 buildHeaderComment 一致的结构化主表备注：存在时禁止 loadCustomerDetail 用档案覆盖发票/账期 */
  const productPrefix = `${t('salesOrderCreate.comment.product')}：`
  const invoiceNeedle = `${t('salesOrderCreate.comment.invoice')}：`
  const hasStructuredHeaderComment =
    !!rawOrderComment.trim() &&
    (rawOrderComment.trim().startsWith(productPrefix) || rawOrderComment.includes(invoiceNeedle))

  const blocks = parseHeaderCommentBlocks(rawOrderComment || undefined)
  if (blocks.productKind && PRODUCT_KINDS.has(blocks.productKind)) {
    formData.value.productKind = blocks.productKind
  } else {
    formData.value.productKind = '现货'
  }
  formData.value.invoiceInfo = blocks.invoiceInfo
  formData.value.paymentTermsLabel = blocks.paymentTermsLabel?.trim() || 'NET 30'
  formData.value.orderRemark = blocks.orderRemark
  formData.value.customerContactId = ''
  formData.value.customerContactName = blocks.customerContactName

  if (formData.value.customerId) {
    customerOptions.value = [
      {
        value: formData.value.customerId,
        label: formData.value.customerName || t('salesOrderCreate.unknownCustomer')
      }
    ]
    await loadCustomerDetail(formData.value.customerId, { skipInvoiceSync: hasStructuredHeaderComment })
    if (formData.value.customerContactName) {
      const hit = contactOptions.value.find(
        (c) => (c.label || '').trim() === formData.value.customerContactName.trim()
      )
      if (hit) {
        formData.value.customerContactId = hit.value
      }
    }
    if (!formData.value.customerContactId && contactOptions.value.length === 1) {
      formData.value.customerContactId = contactOptions.value[0].value
      formData.value.customerContactName = contactOptions.value[0].label
    }
  }

  prefillCustomerId.value = formData.value.customerId || undefined
  prefillSalesUserId.value = formData.value.salesUserId || undefined

  if (!items.length) {
    addItem()
    return
  }

  formData.value.items = items.map((row) => {
    const quoteIdRaw = pickRowStr(row, 'quoteId', 'QuoteId')
    const dd = row.deliveryDate ?? row.DeliveryDate
    let deliveryDate = ''
    if (dd != null && dd !== '') {
      const s = String(dd)
      deliveryDate = s.includes('T') ? s.slice(0, 10) : s.slice(0, 10)
    }
    const dateCodeRaw = pickRowStr(row, 'dateCode', 'DateCode')
    const rawLineComment = pickRowStr(row, 'comment', 'Comment')
    const parsedLine = parseItemCommentForDraft(rawLineComment || undefined)
    const line: OrderLineDraft = {
      quoteId: quoteIdRaw || undefined,
      pn: pickRowStr(row, 'pn', 'PN'),
      customerMaterialModel: parsedLine.customerMaterialModel,
      brand: pickRowStr(row, 'brand', 'Brand'),
      customerPo: pickRowStr(row, 'customerPnNo', 'CustomerPnNo'),
      price: Number(row.price ?? row.Price ?? 0) || 0,
      currency: Number(row.currency ?? row.Currency ?? formData.value.currency) || 1,
      purchasePriceDisplay: 0,
      qty: Number(row.qty ?? row.Qty ?? 1) || 1,
      dateCode: dateCodeRaw ? coercePd(dateCodeRaw) : defaultProductionDateCode(),
      deliveryDate,
      comment: parsedLine.comment,
      purchaseQuoteLabel: ''
    }
    return line
  })
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
  const p = quoteRowUnitPrice(first)
  const cur = mapQuoteCurrencyToOrderCurrency(first.currency ?? first.Currency ?? 0)
  const code = currencyCode(cur)
  return `${t('salesOrderCreate.purchaseQuotePrefix')}：${formatUnitPriceNumber(p)} ${code}`
}

onMounted(async () => {
  await ensureMaterialPdDict()

  if (editId.value) {
    try {
      await loadOrderForEdit(editId.value)
    } catch (e) {
      ElMessage.error(getApiErrorMessage(e, t('salesOrderCreate.messages.loadOrderFailed')))
      await router.replace({ name: 'SalesOrderList' })
    }
    return
  }

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
      ElMessage.error(t('salesOrderCreate.messages.quotePartialMissing'))
      addItem()
      return
    }

    const customerIds = await Promise.all(quotes.map((q) => resolveCustomerIdFromQuoteDetail(q)))
    const nonempty = customerIds.filter(Boolean)
    if (nonempty.length >= 2 && new Set(nonempty).size > 1) {
      ElMessage.error(t('salesOrderCreate.messages.sameCustomerRequired'))
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
      let reqProductionDate = ''
      if (rfqItemId) {
        try {
          const ri = await rfqApi.getRFQItemById(rfqItemId)
          const riExt = ri as RFQItem & { mpn?: string }
          pn = String(ri.materialModel ?? riExt.mpn ?? pn)
          brand = String(ri.brand ?? '')
          reqQty = Math.max(1, Number(ri.quantity) || 1)
          custMpn = String(ri.customerMaterialModel ?? '')
          reqProductionDate = String(ri.productionDate ?? '').trim()
        } catch {
          /* 仅用报价头 */
        }
      }
      const rawItems = (q.items as Record<string, unknown>[] | undefined) || []
      const headerMpn = String(q.mpn ?? '')
      const first = rawItems[0]
      const purchase = quoteRowUnitPrice(first)
      const line: OrderLineDraft = {
        quoteId: qid,
        pn: String(first?.mpn ?? pn ?? headerMpn),
        customerMaterialModel: custMpn,
        brand: String(first?.brand ?? brand),
        customerPo: '',
        qty: Math.max(1, Number(first?.quantity) || reqQty),
        price: purchase,
        currency: first
          ? mapQuoteCurrencyToOrderCurrency(first.currency ?? first.Currency ?? 0)
          : formData.value.currency,
        purchasePriceDisplay: purchase,
        dateCode:
          coercePd(reqProductionDate) ||
          coercePd(String(first?.dateCode ?? first?.DateCode ?? '')) ||
          defaultProductionDateCode(),
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
    ElMessage.error(t('salesOrderCreate.messages.loadQuotesFailed'))
    addItem()
  }
})

function buildCreateOrUpdateLinePayloads(headerCurrency: number) {
  return formData.value.items.map((it) => ({
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
}

const handleSubmit = async () => {
  const firstLineCur = formData.value.items[0]?.currency
  const headerCurrency = firstLineCur ?? formData.value.currency

  if (editId.value) {
    await runValidatedFormSave(formRef, {
      loading: submitLoading,
      successMessage: t('salesOrderCreate.messages.updateSuccess'),
      task: async () => {
        await salesOrderApi.update(editId.value, {
          customerName: formData.value.customerName || undefined,
          salesUserId: formData.value.salesUserId || prefillSalesUserId.value || authStore.user?.id || undefined,
          salesUserName: formData.value.salesUserName || undefined,
          type: formData.value.type,
          currency: headerCurrency,
          deliveryDate: null,
          deliveryAddress: formData.value.deliveryAddress || undefined,
          comment: buildHeaderComment() || undefined,
          items: buildCreateOrUpdateLinePayloads(headerCurrency)
        })
      },
      onSuccess: () => {
        router.push({ name: 'SalesOrderDetail', params: { id: editId.value } })
      },
      errorMessage: (e) => getApiErrorMessage(e, t('salesOrderCreate.messages.updateFailed'))
    })
    return
  }

  await runValidatedFormSave(formRef, {
    loading: submitLoading,
    successMessage: t('salesOrderCreate.messages.createSuccess'),
    task: async () => {
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
        items: buildCreateOrUpdateLinePayloads(headerCurrency)
      })
    },
    onSuccess: () => {
      const back = parseReturnTo()
      if (back) router.push(back)
      else router.push({ name: 'SalesOrderList' })
    },
    errorMessage: (e) => getApiErrorMessage(e, t('salesOrderCreate.messages.createFailed'))
  })
}
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

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
  :deep(.so-collapse) {
    border: none;
    background: transparent;
  }
  :deep(.so-collapse .el-collapse-item) {
    background: $layer-2;
    border: 1px solid $border-card;
    border-radius: 8px;
    margin-bottom: 14px;
    overflow: hidden;
  }
  :deep(.so-collapse .el-collapse-item__header) {
    background: rgba(0, 212, 255, 0.06);
    border-bottom: 1px solid rgba(0, 212, 255, 0.12);
    color: $text-primary;
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
    color: $text-primary;
  }

  :deep(.el-form-item__label) {
    color: $text-muted;
    font-size: 13px;
  }

  :deep(.el-input__wrapper),
  :deep(.el-textarea__inner),
  :deep(.el-select .el-input__wrapper) {
    background: $layer-3;
    border-color: $border-panel;
    box-shadow: none;
    color: #c8dff0;
    &:hover { border-color: rgba(0, 212, 255, 0.35); }
    &.is-focus { border-color: $cyan-primary; }
  }

  :deep(.el-input.is-disabled .el-input__wrapper) {
    background: #071220;
    border-color: #1a2d45;
    .el-input__inner { color: #3a5a7a; }
  }

  :deep(.el-input__inner),
  :deep(.el-textarea__inner) {
    color: $text-primary;
    background: transparent;
    &::placeholder { color: $text-placeholder; }
  }

  :deep(.el-date-editor .el-input__wrapper) {
    background: #0d1e35;
    border-color: #1a2d45;
  }
}

.select-hint {
  padding: 8px 12px;
  color: $text-muted;
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
  color: $text-muted;
  font-size: 13px;
  padding: 16px 0;
}

.material-card {
  border: 1px solid rgba(0, 212, 255, 0.15);
  border-radius: 8px;
  margin-bottom: 14px;
  overflow: hidden;
  background: rgba(0, 212, 255, 0.03);
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
  .head-mpn { color: $text-primary; font-weight: 600; }
  .head-quote { color: $text-muted; }
}

.material-card-body {
  padding: 12px 14px 4px;
}

.total-inline {
  display: flex;
  align-items: center;
  gap: 8px;
  color: $cyan-primary;
  font-weight: 600;
  .ccy-tag {
    font-size: 12px;
    color: #5a7a9a;
    color: $text-muted;
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
  .gt-label { color: $text-muted; font-size: 13px; }
  .gt-amount { color: $cyan-primary; font-size: 16px; font-weight: 700; }
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
