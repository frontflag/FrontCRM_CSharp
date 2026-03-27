<template>
  <div class="quote-upsert-page">
    <div class="page-header">
      <div class="header-left">
        <el-button link @click="handleBack">
          <el-icon><ArrowLeft /></el-icon>
          返回列表
        </el-button>
        <div class="page-title">{{ upsertTitle }}</div>
      </div>
      <div class="header-right">
        <el-button @click="handleBack">取消</el-button>
        <el-button type="primary" :loading="submitLoading" @click="handleSubmit">
          <el-icon><Check /></el-icon> 保存
        </el-button>
      </div>
    </div>

    <el-card class="form-card" shadow="never" v-loading="pageLoading">
      <el-alert
        v-if="hasRfqLinkAlert"
        type="info"
        :closable="false"
        show-icon
        class="link-alert"
      >
        <template #title>
          <div class="link-alert-title-row">
            <span class="la-block-rfq">
              <span class="la-muted">报价需求</span><span class="la-pre">{{ linkAlertGap2 }}</span><span class="la-strong la-rfq-val">{{ linkAlertRfqDisplay }}</span>
              <template v-if="linkAlertBatchCount > 0">
                <span class="la-pre">{{ linkAlertSep2 }}</span>
                <span class="la-muted">明细 {{ linkAlertBatchCount }} 条（批量报价）</span>
              </template>
            </span>
            <span class="la-pre">{{ linkAlertSep8Ideo }}</span>
            <span class="la-block-detail"><span class="la-muted">物料号</span><span class="la-pre">{{ linkAlertGap2 }}</span><span class="la-value-green">{{ formData.mpn || '—' }}</span><span class="la-pre">{{ linkAlertSep4Ideo }}</span><span class="la-muted">品牌</span><span class="la-pre">{{ linkAlertGap2 }}</span><span class="la-value-green">{{ formData.brand || '—' }}</span><span class="la-pre">{{ linkAlertSep4Ideo }}</span><span class="la-muted">数量</span><span class="la-pre">{{ linkAlertGap2 }}</span><span class="la-value-green">{{ formatNumber(formData.quantity) }}</span><span class="la-pre">{{ linkAlertSep4Ideo }}</span><span class="la-muted">目标价</span><span class="la-pre">{{ linkAlertGap2 }}</span><span class="la-value-green">{{ targetPriceText }}</span></span>
          </div>
        </template>
      </el-alert>

      <el-form ref="formRef" :model="formData" :rules="formRules" label-width="128px" class="upsert-form">
        <!-- 基础 -->
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="报价编号">
              <el-input v-model="formData.quoteCode" placeholder="系统自动生成" disabled />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="报价日期">
              <el-input v-model="formData.quoteDate" disabled />
            </el-form-item>
          </el-col>
        </el-row>

        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="供应商" prop="vendorId">
              <el-select
                v-model="formData.vendorId"
                class="q-select"
                placeholder="请选择供应商"
                style="width: 100%"
                filterable
                clearable
                :filter-method="onVendorFilterInput"
                :loading="vendorSearchLoading"
                loading-text="搜索中..."
                @change="onVendorChange"
              >
                <template #empty>
                  <div class="vendor-search-hint">
                    <span>请输入内容之后选择</span>
                  </div>
                </template>
                <el-option v-for="v in vendorOptions" :key="v.value" :label="v.label" :value="v.value" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="联系人" prop="vendorContactId">
              <el-select
                v-model="formData.vendorContactId"
                class="q-select"
                placeholder="请先选择供应商"
                style="width: 100%"
                filterable
                clearable
                :disabled="!formData.vendorId"
                :loading="contactLoading"
                @change="onContactChange"
              >
                <el-option v-for="c in contactOptions" :key="c.value" :label="c.label" :value="c.value" />
              </el-select>
            </el-form-item>
          </el-col>
        </el-row>

        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="价格类型" prop="priceType">
              <el-select v-model="formData.priceType" placeholder="请选择价格类型" class="q-select" style="width: 100%">
                <el-option label="现货价" value="现货价" />
                <el-option label="期货价" value="期货价" />
                <el-option label="样品价" value="样品价" />
                <el-option label="排单价" value="排单价" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="失效日期" prop="expiryDate">
              <el-date-picker
                v-model="formData.expiryDate"
                type="date"
                placeholder="请选择失效日期"
                value-format="YYYY-MM-DD"
                style="width: 100%"
                class="q-date"
              />
            </el-form-item>
          </el-col>
        </el-row>

        <el-row :gutter="12" class="quote-triple-row">
          <el-col :span="8">
            <el-form-item label="物料型号" prop="mpn">
              <el-input
                v-model="formData.mpn"
                :placeholder="rfqDetailLocked ? '来自需求明细' : '请输入MPN'"
                :disabled="rfqDetailLocked"
              />
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="品牌" prop="brand">
              <el-input
                v-model="formData.brand"
                :placeholder="rfqDetailLocked ? '来自需求明细' : '请输入品牌'"
                :disabled="rfqDetailLocked"
                clearable
              />
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="品牌属地">
              <el-input v-model="formData.brandOrigin" placeholder="如：韩国" disabled />
            </el-form-item>
          </el-col>
        </el-row>

        <el-row :gutter="12" class="quote-triple-row">
          <el-col :span="12">
            <el-form-item label="生产日期/DC" prop="productionDate">
              <el-input v-model="formData.productionDate" placeholder="如：2年内" clearable />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="交期">
              <el-input v-model="formData.leadTime" placeholder="请输入交期" clearable />
            </el-form-item>
          </el-col>
        </el-row>

        <el-row :gutter="12" class="quote-origin-row">
          <el-col :span="8">
            <el-form-item label="涂标">
              <el-radio-group v-model="formData.labelType" class="seg-group">
                <el-radio-button :label="0">不涂标</el-radio-button>
                <el-radio-button :label="1">涂标</el-radio-button>
                <el-radio-button :label="2">待确定</el-radio-button>
              </el-radio-group>
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="报价晶圆产地" prop="waferOrigin">
              <el-radio-group v-model="formData.waferOrigin" class="seg-group">
                <el-radio-button :label="0">美产</el-radio-button>
                <el-radio-button :label="1">非美产</el-radio-button>
                <el-radio-button :label="2">待确定</el-radio-button>
              </el-radio-group>
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="报价封装产地" prop="packageOrigin">
              <div class="pkg-origin-row">
                <el-radio-group v-model="formData.packageOrigin" class="seg-group">
                  <el-radio-button :label="0">美产</el-radio-button>
                  <el-radio-button :label="1">非美产</el-radio-button>
                  <el-radio-button :label="2">待确定</el-radio-button>
                </el-radio-group>
                <span class="free-ship-label">包邮</span>
                <el-switch v-model="formData.freeShipping" />
              </div>
            </el-form-item>
          </el-col>
        </el-row>

        <el-row :gutter="12" class="quote-triple-row">
          <el-col :span="8">
            <el-form-item label="最小包装(MPQ)">
              <el-input-number v-model="formData.minPackageQty" :min="0" :controls="false" style="width: 100%" />
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="库存">
              <el-input-number v-model="formData.stockQty" :min="0" :controls="false" style="width: 100%" />
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="起订量(MOQ)">
              <el-input-number v-model="formData.moq" :min="0" :controls="false" style="width: 100%" />
            </el-form-item>
          </el-col>
        </el-row>

        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="业务员" prop="salesUserId">
              <SalesUserCascader
                v-model="formData.salesUserId"
                placeholder="请选择业务员"
                @change="onSalesUserChange"
              />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="采购员">
              <PurchaserCascader
                v-model="formData.purchaseUserId"
                placeholder="请选择采购员"
                @change="onPurchaseUserChange"
              />
            </el-form-item>
          </el-col>
        </el-row>

        <el-form-item label="备注">
          <el-input v-model="formData.remark" type="textarea" :rows="2" placeholder="请输入备注" />
        </el-form-item>

        <!-- 供应商报价：数量 / 价格 / 折算价（可增删行，默认一行空白） -->
        <div class="price-tier-panel">
          <div class="price-tier-header">
            <h4 class="price-tier-title">供应商报价</h4>
          </div>
          <CrmDataTable class="price-tier-table" :data="formData.quotePriceRows" size="small">
              <el-table-column label="数量" min-width="120">
                <template #default="{ $index }">
                  <el-input-number
                    v-model="formData.quotePriceRows[$index].quantity"
                    :min="0"
                    :controls="false"
                    style="width: 100%"
                  />
                </template>
              </el-table-column>
              <el-table-column label="价格" min-width="140">
                <template #default="{ $index }">
                  <el-input-number
                    v-model="formData.quotePriceRows[$index].unitPrice"
                    :min="0"
                    :precision="4"
                    :controls="false"
                    style="width: 100%"
                  />
                </template>
              </el-table-column>
              <el-table-column label="币别" min-width="128" class-name="tier-col-currency">
                <template #default="{ $index }">
                  <el-select
                    v-model="formData.quotePriceRows[$index].currency"
                    class="q-select tier-currency-select"
                    size="small"
                  >
                    <el-option
                      v-for="opt in CURRENCY_ISO_OPTIONS"
                      :key="opt.value"
                      :label="opt.label"
                      :value="opt.value"
                    />
                  </el-select>
                </template>
              </el-table-column>
              <el-table-column label="折算价" min-width="140">
                <template #default="{ $index }">
                  <el-input-number
                    v-model="formData.quotePriceRows[$index].convertedPrice"
                    :min="0"
                    :precision="4"
                    :controls="false"
                    placeholder="折算价"
                    style="width: 100%"
                  />
                </template>
              </el-table-column>
              <el-table-column label="" width="108" align="center" fixed="right">
                <template #default="{ $index }">
                  <div class="tier-actions">
                    <el-button
                      type="danger"
                      link
                      :disabled="formData.quotePriceRows.length <= 1"
                      @click="removePriceRow($index)"
                      title="删除本行"
                    >
                      <el-icon><Minus /></el-icon>
                    </el-button>
                    <el-button type="primary" link @click="insertPriceRowAfter($index)" title="下方插入一行">
                      <el-icon><Plus /></el-icon>
                    </el-button>
                  </div>
                </template>
              </el-table-column>
          </CrmDataTable>
        </div>
      </el-form>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ArrowLeft, Check, Plus, Minus } from '@element-plus/icons-vue'
import { ElMessage } from 'element-plus'
import { quoteApi } from '@/api/quote'
import { vendorApi, vendorContactApi } from '@/api/vendor'
import { rfqApi } from '@/api/rfq'
import type { Vendor } from '@/types/vendor'
import { getApiErrorMessage } from '@/utils/apiError'
import { runValidatedFormSave } from '@/composables/useFormSubmit'
import {
  extractMpn,
  extractBrand,
  mapCurrencyLabelFromRaw,
  fetchLinkedRfqItemRecord
} from '@/utils/rfqLinkedItemSummary'
import { useAuthStore } from '@/stores/auth'
import { CURRENCY_ISO_OPTIONS } from '@/constants/currency'
import SalesUserCascader from '@/components/SalesUserCascader.vue'
import PurchaserCascader from '@/components/PurchaserCascader.vue'

const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()

const isEditMode = computed(() => route.name === 'QuoteEdit')
const upsertTitle = computed(() => (isEditMode.value ? '编辑报价' : '新建报价'))

const rfqLink = computed(() => {
  const rfqId = route.query.rfqId as string | undefined
  const rfqCode = route.query.rfqCode as string | undefined
  const rfqItemId = route.query.rfqItemId as string | undefined
  const raw = route.query.rfqItemIds as string | undefined
  const rfqItemIds = raw ? raw.split(',').map((s) => s.trim()).filter(Boolean) : []
  return { rfqId, rfqCode, rfqItemId, rfqItemIds }
})

/** 顶部提示：不展示明细 ID，仅报价需求编号 + 物料号/品牌/数量/目标价（单行） */
const hasRfqLinkAlert = computed(() => !!rfqLink.value.rfqId)

const linkAlertRfqDisplay = computed(() => {
  const { rfqId, rfqCode } = rfqLink.value
  return (rfqCode || rfqId || '').trim() || '—'
})

const linkAlertBatchCount = computed(() => {
  const ids = rfqLink.value.rfqItemIds
  return ids.length > 1 ? ids.length : 0
})

/** 提示栏间距：报价需求↔RFQ 为 2 个半角空格；黄框↔红框 8 个汉字宽（全角空格）；红框内四段之间 4 个全角空格 */
const linkAlertGap2 = '  '
const linkAlertSep2 = '  '
const linkAlertSep8Ideo = '\u3000'.repeat(8)
const linkAlertSep4Ideo = '\u3000'.repeat(4)

const submitLoading = ref(false)
const pageLoading = ref(false)
const formRef = ref()
const rfqDetailLocked = ref(false)

const vendorOptions = ref<{ value: string; label: string }[]>([])
const vendorSearchLoading = ref(false)
let vendorSearchTimer: ReturnType<typeof setTimeout> | null = null

const contactOptions = ref<{ value: string; label: string }[]>([])
const contactLoading = ref(false)

function todayStr() {
  return new Date().toISOString().slice(0, 10)
}

function emptyPriceRow() {
  return {
    quantity: 0,
    unitPrice: 0,
    /** 与历史主体字段一致：0=RMB，1=USD */
    currency: 1,
    convertedPrice: undefined as number | undefined
  }
}

/** 列表/模拟数据里可能出现 2=USD，与表单选项对齐为 1 */
function normalizeQuoteCurrency(c: unknown): number {
  if (c === 0 || c === 1) return c
  if (c === 2) return 1
  return 1
}

const formData = ref({
  quoteCode: '',
  quoteDate: todayStr(),
  rfqId: '',
  rfqItemId: '',
  mpn: '',
  brand: '',
  brandOrigin: '',
  /** 需求明细数量（摘要条展示），与下方报价阶梯行独立 */
  quantity: 1,
  targetPrice: undefined as number | undefined,
  currencyLabel: 'RMB',

  vendorId: '',
  vendorName: '',
  vendorContactId: '',
  contactName: '',
  priceType: '',
  expiryDate: '',
  productionDate: '',
  leadTime: '',
  labelType: 0,
  waferOrigin: 2,
  packageOrigin: 2,
  freeShipping: false,
  minPackageQty: 0,
  stockQty: 0,
  moq: 0,
  salesUserId: '',
  purchaseUserId: '',
  salesUserName: '',
  purchaseUserName: '',
  remark: '',
  /** 供应商报价阶梯：数量 / 价格 / 币别 / 折算价，新建默认一行空白 */
  quotePriceRows: [emptyPriceRow()]
})

const targetPriceText = computed(() => {
  const p = formData.value.targetPrice
  if (p == null || p === ('' as any)) return '—'
  const n = Number(p)
  if (Number.isNaN(n)) return '—'
  return `${n.toLocaleString('zh-CN', { minimumFractionDigits: 4, maximumFractionDigits: 4 })} ${formData.value.currencyLabel || 'RMB'}`
})

function formatNumber(n: number) {
  if (n == null || Number.isNaN(Number(n))) return '—'
  return Number(n).toLocaleString('zh-CN')
}

async function loadLinkedRfqItem() {
  const { rfqId, rfqItemId, rfqItemIds } = rfqLink.value
  const itemId = rfqItemId || (rfqItemIds.length === 1 ? rfqItemIds[0] : '')
  rfqDetailLocked.value = false
  if (!itemId) {
    if (formData.value.quotePriceRows.length === 0) {
      formData.value.quotePriceRows = [emptyPriceRow()]
    }
    return
  }

  pageLoading.value = true
  try {
    const loaded = await fetchLinkedRfqItemRecord(rfqId || '', itemId)
    if (!loaded) {
      ElMessage.warning('未找到需求明细，请手动填写物料型号与品牌')
      return
    }
    const { item, rfqHeader } = loaded

    const mpn = extractMpn(item)
    const brand = extractBrand(item)
    const qty = Number(item['quantity'] ?? item['Quantity'] ?? 1) || 1
    const tp = item['targetPrice'] ?? item['TargetPrice']
    const targetPrice = tp != null && tp !== '' ? Number(tp) : undefined

    formData.value.mpn = mpn
    formData.value.brand = brand
    formData.value.rfqItemId = itemId
    formData.value.rfqId = String(item['rfqId'] ?? item['RfqId'] ?? rfqId ?? '')
    formData.value.quantity = qty
    formData.value.targetPrice = targetPrice
    formData.value.currencyLabel = mapCurrencyLabelFromRaw(item)
    formData.value.productionDate = String(item.productionDate ?? item.ProductionDate ?? '') || ''
    const exp = item.expiryDate ?? item.ExpiryDate
    if (exp) {
      formData.value.expiryDate = String(exp).slice(0, 10)
    }
    formData.value.minPackageQty = Number(item.minPackageQty ?? item.MinPackageQty ?? 0) || 0
    formData.value.moq = Number(item.minOrderQty ?? item.MinOrderQty ?? 0) || 0
    formData.value.stockQty = qty
    formData.value.brandOrigin = ''
    rfqDetailLocked.value = true

    formData.value.quotePriceRows = [emptyPriceRow()]

    if (rfqId) {
      try {
        const rfq = rfqHeader ?? (await rfqApi.getRFQById(rfqId))
        if (rfq.salesUserId) formData.value.salesUserId = String(rfq.salesUserId)
        formData.value.salesUserName = rfq.salesUserName || formData.value.salesUserName
      } catch {
        /* 主表失败时保留手工业务员 */
      }
    }
  } catch (e) {
    ElMessage.warning(getApiErrorMessage(e, '加载需求明细失败，请手动填写物料型号与品牌'))
    rfqDetailLocked.value = false
  } finally {
    pageLoading.value = false
  }
}

watch(
  () => `${route.query.rfqId || ''}|${route.query.rfqItemId || ''}|${route.query.rfqItemIds || ''}`,
  () => {
    if (route.name === 'QuoteEdit') return
    void loadLinkedRfqItem()
  },
  { immediate: true }
)

const formRules = {
  mpn: [{ required: true, message: '请输入物料型号', trigger: 'blur' }],
  vendorId: [{ required: true, message: '请选择供应商', trigger: 'change' }],
  vendorContactId: [{ required: true, message: '请选择联系人', trigger: 'change' }],
  priceType: [{ required: true, message: '请选择价格类型', trigger: 'change' }],
  expiryDate: [{ required: true, message: '请选择失效日期', trigger: 'change' }],
  brand: [{ required: true, message: '请输入品牌', trigger: 'blur' }],
  productionDate: [{ required: true, message: '请输入生产日期/DC', trigger: 'blur' }],
  waferOrigin: [{ required: true, message: '请选择晶圆产地', trigger: 'change' }],
  packageOrigin: [{ required: true, message: '请选择封装产地', trigger: 'change' }],
  salesUserId: [{ required: true, message: '请选择业务员', trigger: 'change' }]
}

function onSalesUserChange(p: { id: string; label: string }) {
  formData.value.salesUserName = p.label || ''
}

function onPurchaseUserChange(p: { id: string; label: string }) {
  formData.value.purchaseUserName = p.label || ''
}

function onVendorFilterInput(query: string) {
  if (vendorSearchTimer) clearTimeout(vendorSearchTimer)
  if (!query || query.trim().length < 1) {
    if (formData.value.vendorId && formData.value.vendorName) {
      vendorOptions.value = [{ value: formData.value.vendorId, label: formData.value.vendorName }]
    } else {
      vendorOptions.value = []
    }
    return
  }
  vendorSearchTimer = setTimeout(async () => {
    vendorSearchLoading.value = true
    try {
      const res = await vendorApi.searchVendors({
        pageNumber: 1,
        pageSize: 30,
        keyword: query.trim()
      })
      vendorOptions.value = (res.items || []).map((v: Vendor) => ({
        value: v.id,
        label: v.officialName || v.nickName || v.code || '供应商'
      }))
    } catch {
      vendorOptions.value = []
    } finally {
      vendorSearchLoading.value = false
    }
  }, 300)
}

function onVendorChange(val: string | null | undefined) {
  formData.value.vendorContactId = ''
  formData.value.contactName = ''
  contactOptions.value = []
  if (!val) {
    formData.value.vendorName = ''
    return
  }
  const found = vendorOptions.value.find((x) => x.value === val)
  if (found) formData.value.vendorName = found.label
  void loadVendorContacts(val)
}

async function loadVendorContacts(vendorId: string) {
  if (!vendorId) {
    contactOptions.value = []
    return
  }
  contactLoading.value = true
  try {
    const list = await vendorContactApi.getContactsByVendorId(vendorId)
    contactOptions.value = list.map((c) => ({
      value: c.id,
      label: [c.cName, c.mobile].filter(Boolean).join(' / ') || c.id
    }))
  } catch {
    contactOptions.value = []
  } finally {
    contactLoading.value = false
  }
}

function onContactChange(id: string | undefined) {
  if (!id) {
    formData.value.contactName = ''
    return
  }
  const row = contactOptions.value.find((c) => c.value === id)
  formData.value.contactName = row?.label?.split(' / ')[0] || ''
}

/** 将列表/详情中的报价主表 + 明细映射为新建页表单（与保存时结构一致） */
function applyQuoteToForm(q: Record<string, unknown>) {
  const prRows = q.quotePriceRows as unknown
  let rows: ReturnType<typeof emptyPriceRow>[] = []
  if (Array.isArray(prRows) && prRows.length > 0) {
    rows = prRows.map((r: Record<string, unknown>) => ({
      quantity: Number(r.quantity) || 0,
      unitPrice: r.unitPrice != null && r.unitPrice !== '' ? Number(r.unitPrice) : 0,
      currency: normalizeQuoteCurrency(r.currency),
      convertedPrice:
        r.convertedPrice != null && r.convertedPrice !== '' ? Number(r.convertedPrice) : undefined
    }))
  } else {
    const items = q.items as unknown
    if (Array.isArray(items) && items.length > 0) {
      rows = items.map((it: Record<string, unknown>) => ({
        quantity: Number(it.quantity) || 0,
        unitPrice: it.unitPrice != null && it.unitPrice !== '' ? Number(it.unitPrice) : 0,
        currency: normalizeQuoteCurrency(it.currency),
        convertedPrice:
          it.convertedPrice != null && it.convertedPrice !== ''
            ? Number(it.convertedPrice)
            : undefined
      }))
    }
  }
  if (rows.length === 0) rows = [emptyPriceRow()]

  formData.value.quoteCode = String(q.quoteCode ?? q.quoteNumber ?? q.QuoteCode ?? '')
  formData.value.quoteDate = String(q.quoteDate ?? todayStr()).slice(0, 10)
  formData.value.rfqId = String(q.rfqId ?? q.RfqId ?? '')
  formData.value.rfqItemId = String(q.rfqItemId ?? q.RfqItemId ?? '')
  formData.value.mpn = String(q.mpn ?? q.Mpn ?? '')
  formData.value.remark = String(q.remark ?? '')
  formData.value.salesUserId = String(q.salesUserId ?? q.SalesUserId ?? '')
  formData.value.purchaseUserId = String(q.purchaseUserId ?? q.PurchaseUserId ?? '')
  formData.value.salesUserName = String(q.salesUserName ?? '')
  formData.value.purchaseUserName = String(q.purchaseUserName ?? '')
  formData.value.quotePriceRows = rows

  const items = q.items as Record<string, unknown>[] | undefined
  const first = items?.[0]
  if (first) {
    formData.value.vendorId = String(first.vendorId ?? first.VendorId ?? '')
    formData.value.vendorName = String(first.vendorName ?? first.VendorName ?? '')
    formData.value.contactName = String(first.contactName ?? first.ContactName ?? '')
    formData.value.vendorContactId = ''
    formData.value.priceType = String(first.priceType ?? first.PriceType ?? '')
    const exp = first.expiryDate ?? first.ExpiryDate
    formData.value.expiryDate = exp ? String(exp).slice(0, 10) : ''
    formData.value.productionDate = String(
      first.productionDate ?? first.ProductionDate ?? first.dateCode ?? first.DateCode ?? ''
    )
    formData.value.leadTime = String(first.leadTime ?? first.LeadTime ?? '')
    formData.value.labelType = Number(first.labelType ?? first.LabelType ?? 0)
    formData.value.waferOrigin = Number(first.waferOrigin ?? first.WaferOrigin ?? 2)
    formData.value.packageOrigin = Number(first.packageOrigin ?? first.PackageOrigin ?? 2)
    formData.value.freeShipping = Boolean(first.freeShipping ?? first.FreeShipping)
    formData.value.minPackageQty = Number(first.minPackageQty ?? first.MinPackageQty ?? 0)
    formData.value.stockQty = Number(first.stockQty ?? first.StockQty ?? 0)
    formData.value.moq = Number(first.moq ?? first.Moq ?? first.minOrderQty ?? 0)
    formData.value.brand = String(first.brand ?? first.Brand ?? '')
    formData.value.brandOrigin = String(first.brandOrigin ?? first.BrandOrigin ?? '')
    formData.value.quantity = Number(q.quantity ?? first.quantity ?? formData.value.quantity) || 1
    const tp = q.targetPrice ?? first.targetPrice
    formData.value.targetPrice =
      tp != null && tp !== '' ? Number(tp as number) : formData.value.targetPrice

    if (formData.value.vendorId) {
      vendorOptions.value = [
        { value: formData.value.vendorId, label: formData.value.vendorName || formData.value.vendorId }
      ]
      void loadVendorContacts(formData.value.vendorId)
    }
  } else {
    formData.value.brand = String(q.brand ?? q.Brand ?? '')
    formData.value.brandOrigin = String(q.brandOrigin ?? q.BrandOrigin ?? '')
  }
}

async function loadQuoteForEdit() {
  const id = route.params.id as string
  if (!id) return
  pageLoading.value = true
  try {
    const res = await quoteApi.getById(id)
    const q = res?.data as Record<string, unknown> | undefined
    if (!q) {
      ElMessage.error('报价单不存在')
      router.push({ name: 'QuoteList' })
      return
    }
    applyQuoteToForm(q)
  } catch {
    ElMessage.error('加载报价失败')
    router.push({ name: 'QuoteList' })
  } finally {
    pageLoading.value = false
  }
}

/** 从需求明细等入口携带 returnTo，保存/返回时回到来源页（仅允许站内 path） */
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

const handleBack = () => {
  const back = parseReturnTo()
  if (back) {
    router.push(back)
    return
  }
  router.push({ name: 'QuoteList' })
}

function insertPriceRowAfter(index: number) {
  formData.value.quotePriceRows.splice(index + 1, 0, emptyPriceRow())
}

function removePriceRow(index: number) {
  if (formData.value.quotePriceRows.length <= 1) return
  formData.value.quotePriceRows.splice(index, 1)
}

onMounted(async () => {
  if (isEditMode.value) {
    await loadQuoteForEdit()
    return
  }
  const u = authStore.user
  if (u?.id && !formData.value.salesUserId) {
    formData.value.salesUserId = u.id
  }
  if (u?.userName && !formData.value.salesUserName) {
    formData.value.salesUserName = u.userName
  }
})

const handleSubmit = async () => {
  const rows = formData.value.quotePriceRows
  await runValidatedFormSave(formRef, {
    loading: submitLoading,
    afterValidate: async () => {
      const hasValidTier = rows.some(
        (r) => Number(r.quantity) >= 1 && r.unitPrice != null && !Number.isNaN(Number(r.unitPrice))
      )
      if (!hasValidTier) {
        ElMessage.warning('请在「供应商报价」列表中至少填写一行：数量≥1 且 价格有效')
        return false
      }
      return true
    },
    task: async () => {
      const first = rows[0]
      const data = {
        ...formData.value,
        quoteDate: formData.value.quoteDate || todayStr(),
        rfqId: formData.value.rfqId || rfqLink.value.rfqId,
        rfqItemId: formData.value.rfqItemId || rfqLink.value.rfqItemId,
        quotePriceRows: rows.map((r) => ({ ...r })),
        quoteCurrency: first?.currency ?? 1,
        unitPrice: first?.unitPrice ?? 0,
        convertedPrice: first?.convertedPrice,
        quoteLineQuantity: first?.quantity,
        items: [] as any[]
      }
      if (isEditMode.value) {
        const id = route.params.id as string
        await quoteApi.update(id, data)
        return { kind: 'edit' as const, id }
      }
      const res = await quoteApi.create(data)
      const id = (res?.data as { id?: string } | undefined)?.id
      return { kind: 'create' as const, id, back: parseReturnTo() }
    },
    onSuccess: (r) => {
      if (r.kind === 'edit') {
        router.push({ name: 'QuoteDetail', params: { id: r.id } })
        return
      }
      if (r.back) {
        router.push(r.back)
        return
      }
      if (r.id) router.push({ name: 'QuoteDetail', params: { id: r.id } })
      else router.push({ name: 'QuoteList' })
    },
    errorMessage: (e) => getApiErrorMessage(e, '保存失败')
  })
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
    color: #e8f4ff;
    font-size: 20px;
    font-weight: 600;
  }
}

.form-card {
  background: #0a1628;
  border: 1px solid rgba(0, 212, 255, 0.1);
}

.link-alert {
  margin-bottom: 16px;

  /* 与 Element Plus 标题区字号一致，整行同一字号 */
  .link-alert-title-row {
    display: flex;
    flex-wrap: wrap;
    align-items: baseline;
    gap: 0;
    line-height: 1.55;
    font-size: inherit;
    font-weight: inherit;
  }

  .la-pre {
    white-space: pre;
    font-size: inherit;
  }

  .la-muted {
    color: rgba(0, 0, 0, 0.55);
    font-size: inherit;
    font-weight: 400;
  }

  .la-strong {
    color: rgba(0, 0, 0, 0.88);
    font-size: inherit;
    font-weight: 600;
    font-family: inherit;
  }

  .la-rfq-val {
    font-size: inherit;
  }

  .la-block-detail {
    font-size: inherit;
    font-family: inherit;
  }

  /* 物料号/品牌/数量/目标价 的数值；标题与「报价需求」同为 .la-muted */
  .la-value-green {
    color: #0a6439;
    font-size: inherit;
    font-weight: 600;
    font-family: inherit;
  }

  :deep(.el-alert__title) {
    width: 100%;
    font-size: 14px;
    line-height: 1.55;
  }

  :deep(.el-alert__content) {
    width: 100%;
  }

  :deep(.el-alert__description) {
    display: none;
  }
}

.price-tier-panel {
  margin-top: 8px;
  margin-bottom: 8px;
  padding-top: 16px;
  border-top: 1px solid rgba(0, 212, 255, 0.1);

  .price-tier-header {
    margin-bottom: 10px;
  }

  .price-tier-title {
    margin: 0;
    color: #e8f4ff;
    font-size: 14px;
    font-weight: 600;
  }

  .tier-actions {
    display: inline-flex;
    align-items: center;
    justify-content: flex-end;
    gap: 2px;
    width: 100%;
  }

  /* 币别下拉：列宽与 select 最小宽度保证 USD/RMB 不被截断 */
  :deep(.tier-col-currency .cell) {
    overflow: visible;
  }

  .tier-currency-select {
    width: 100%;
    min-width: 112px;
  }

  .tier-currency-select :deep(.el-select__wrapper) {
    min-width: 112px;
  }
}

.upsert-form {
  :deep(.el-input__inner) {
    background: rgba(255, 255, 255, 0.03);
    border-color: rgba(0, 212, 255, 0.2);
    color: #e8f4ff;
  }

  :deep(.el-form-item__label) {
    color: rgba(200, 216, 232, 0.7);
  }

  .q-select {
    :deep(.el-select__wrapper) {
      background: rgba(255, 255, 255, 0.03);
      box-shadow: 0 0 0 1px rgba(0, 212, 255, 0.2);
    }
    :deep(.el-select__placeholder) {
      color: rgba(200, 216, 232, 0.45);
    }
    :deep(.el-select__selected-item) {
      color: #e8f4ff;
    }
  }

  .q-date {
    :deep(.el-input__wrapper) {
      background: rgba(255, 255, 255, 0.03);
      box-shadow: 0 0 0 1px rgba(0, 212, 255, 0.2);
    }
  }

  .quote-origin-row,
  .quote-triple-row {
    :deep(.el-col) {
      min-width: 0;
    }
  }

  .seg-group {
    flex-wrap: wrap;
    :deep(.el-radio-button__inner) {
      background: rgba(255, 255, 255, 0.04);
      border-color: rgba(0, 212, 255, 0.2);
      color: rgba(200, 216, 232, 0.85);
    }
    :deep(.el-radio-button.is-active .el-radio-button__inner) {
      background: rgba(0, 102, 255, 0.35);
      border-color: rgba(0, 212, 255, 0.45);
      color: #e8f4ff;
    }
  }

  .pkg-origin-row {
    display: flex;
    align-items: center;
    flex-wrap: wrap;
    gap: 12px;
    width: 100%;
  }

  .free-ship-label {
    font-size: 13px;
    color: rgba(200, 216, 232, 0.7);
    margin-left: 8px;
  }
}

.vendor-search-hint {
  padding: 8px 12px;
  font-size: 13px;
  color: rgba(200, 216, 232, 0.55);
  text-align: center;
}
</style>
