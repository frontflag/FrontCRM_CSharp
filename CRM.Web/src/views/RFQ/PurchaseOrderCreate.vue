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
          <el-breadcrumb-item>{{ pageTitle }}</el-breadcrumb-item>
        </el-breadcrumb>
      </div>
      <div class="header-right">
        <el-button plain class="po-header-btn-cancel" @click="router.back()">取消</el-button>
        <el-button type="primary" :loading="submitLoading" class="po-header-btn-save" @click="handleSubmit">
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
            <el-form-item v-if="allowManualVendorPick" label="供应商" prop="vendorId">
              <el-select
                v-model="formData.vendorId"
                class="po-vendor-select"
                placeholder="请搜索并选择供应商"
                filterable
                clearable
                :filter-method="onVendorFilterInput"
                :loading="vendorSearchLoading"
                loading-text="搜索中..."
                @change="onVendorChange"
              >
                <template #empty>
                  <div class="po-vendor-search-hint">输入关键字搜索供应商</div>
                </template>
                <el-option v-for="v in vendorOptions" :key="v.value" :label="v.label" :value="v.value" />
              </el-select>
            </el-form-item>
            <el-form-item v-else label="供应商">
              <el-input v-model="formData.vendorName" disabled placeholder="系统自动带出供应商" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="24">
          <el-col :span="12">
            <el-form-item v-if="allowManualVendorPick" label="供应商联系人">
              <el-select
                v-model="formData.vendorContactId"
                class="po-vendor-select"
                placeholder="请选择联系人（可选）"
                filterable
                clearable
                :disabled="!formData.vendorId"
                :loading="contactLoading"
                @change="onVendorContactChange"
              >
                <el-option v-for="c in vendorContactOptions" :key="c.value" :label="c.label" :value="c.value" />
              </el-select>
            </el-form-item>
            <el-form-item v-else label="供应商联系人">
              <el-input v-model="formData.vendorContactName" disabled placeholder="系统自动带出联系人" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="采购员">
              <purchaser-cascader
                v-model="formData.purchaseUserId"
                placeholder="请选择采购员（默认当前账号，可更换）"
                clearable
                @change="onPurchaserChange"
              />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="24">
          <el-col :span="24">
            <el-form-item label="订单类型">
              <el-select v-model="formData.type" style="width: 100%" disabled>
                <el-option :label="t('salesOrderCreate.orderTypes.normal')" :value="1" />
                <el-option :label="t('salesOrderCreate.orderTypes.urgent')" :value="2" />
                <el-option :label="t('salesOrderCreate.orderTypes.sample')" :value="3" />
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
          <el-button type="success" size="small" class="add-item-btn" @click="addItem">
            <el-icon><Plus /></el-icon> 添加明细
          </el-button>
        </div>
        <div v-if="formData.items.length === 0" class="items-empty">暂无明细</div>

        <div v-for="(item, index) in formData.items" :key="index" class="material-card">
          <div class="material-card-head">
            <span class="head-mpn">物料型号：{{ item.pn || '—' }}</span>
            <span class="head-quote">
              预期采购单价：{{ formatCurrencyUnitPrice(item.cost || 0, formData.currency) }}
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
                  <SettlementCurrencyAmountInput
                    v-model="item.targetPrice"
                    v-model:currency="item.currency"
                    :min="0"
                    :precision="6"
                  />
                </el-form-item>
              </el-col>
              <el-col :span="8">
                <el-form-item label="预期采购单价">
                  <el-input-number v-model="item.cost" :min="0" :precision="6" :controls="false" disabled style="width: 100%" />
                </el-form-item>
              </el-col>
              <el-col :span="8">
                <el-form-item label="数量">
                  <el-input-number v-model="item.qty" :min="1" :controls="false" style="width: 100%" />
                </el-form-item>
              </el-col>
            </el-row>

            <el-row :gutter="16">
              <el-col :span="12">
                <el-form-item label="生产日期要求">
                  <MaterialProductionDateSelect v-model="item.dateCode" placeholder="选填" />
                </el-form-item>
              </el-col>
              <el-col :span="12">
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
              <el-button link type="danger" size="small" @click="removeItem(index)">删除</el-button>
            </div>

            <div class="line-total-row">
              <span class="line-total-label">预计采购总额：</span>
              <span class="line-total-amount">{{ formatCurrencyTotal((item.qty || 0) * (item.targetPrice || 0), item.currency ?? formData.currency) }}</span>
            </div>
          </div>
        </div>

        <div class="total-row">
          <span class="total-label">合计金额：</span>
          <span class="total-amount">{{ formatCurrencyTotal(calculateTotal, formData.currency) }}</span>
        </div>
      </div>

    </el-form>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute, useRouter } from 'vue-router'
import { ArrowLeft, Check, Plus } from '@element-plus/icons-vue'
import type { FormInstance } from 'element-plus'
import { ElMessage } from 'element-plus'
import { purchaseOrderApi } from '@/api/purchaseOrder'
import { purchaseRequisitionApi } from '@/api/purchaseRequisition'
import { vendorApi, vendorContactApi } from '@/api/vendor'
import { runSaveTask, validateElFormOrWarn } from '@/composables/useFormSubmit'
import { getApiErrorMessage } from '@/utils/apiError'
import type { Vendor } from '@/types/vendor'
import { useAuthStore } from '@/stores/auth'
import { canSubmitPurchaseOrderCreate } from '@/utils/purchaseOrderCreateGate'
import PurchaserCascader from '@/components/PurchaserCascader.vue'
import MaterialProductionDateSelect from '@/components/MaterialProductionDateSelect.vue'
import SettlementCurrencyAmountInput from '@/components/SettlementCurrencyAmountInput.vue'
import { formatCurrencyTotal, formatCurrencyUnitPrice } from '@/utils/moneyFormat'
import { useMaterialProductionDateDict } from '@/composables/useMaterialProductionDateDict'

const router = useRouter()
const route = useRoute()
const { t } = useI18n()
const authStore = useAuthStore()
const { ensureLoaded: ensureMaterialPdDict, coerceProductionDateToCode: coercePd } = useMaterialProductionDateDict()

const editId = computed(() => (route.name === 'PurchaseOrderEdit' ? String(route.params.id || '').trim() : ''))
const pageTitle = computed(() => {
  if (editId.value) return '编辑采购订单'
  const qType = Number(route.query.type)
  if (qType === 2) return '新建备货采购订单'
  return '新建采购订单'
})

/** 手工录入时尚无供应商时的占位（满足后端非空）；销售明细无关联时不应再传占位 GUID */
const MANUAL_VENDOR_ID = '00000000-0000-0000-0000-000000000002'
const MANUAL_SELL_ORDER_ITEM_ID = '00000000-0000-0000-0000-000000000000'

/** 提交 API：无销售行或占位 GUID 时不传，后端存 NULL */
function linkedSellOrderItemIdForPayload(id: string | undefined): string | undefined {
  const t = id?.trim()
  if (!t || t.toLowerCase() === MANUAL_SELL_ORDER_ITEM_ID.toLowerCase()) return undefined
  return t
}

const formRef = ref<FormInstance>()
const submitLoading = ref(false)
const genLoading = ref(false)

const requisitionId = computed(() => {
  const v = route.query.requisitionId
  if (!v) return undefined
  return String(v)
})
const generatedFromRequisition = ref(false)

/** 无采购申请链路的纯新建：允许搜索选择供应商/联系人（含备货采购?type=2） */
const allowManualVendorPick = computed(() => !editId.value && !requisitionId.value)

const canSubmitPurchaseOrder = computed(() => {
  if (editId.value) return authStore.hasPermission('purchase-order.write')
  return canSubmitPurchaseOrderCreate({
    isSysAdmin: authStore.user?.isSysAdmin,
    identityType: authStore.user?.identityType,
    roleCodes: authStore.user?.roleCodes,
    hasPermission: (c) => authStore.hasPermission(c)
  })
})

const vendorOptions = ref<{ value: string; label: string }[]>([])
const vendorSearchLoading = ref(false)
let vendorSearchTimer: ReturnType<typeof setTimeout> | null = null
const vendorContactOptions = ref<{ value: string; label: string }[]>([])
const contactLoading = ref(false)

const getYYMMDD = (d: Date) => {
  const yy = String(d.getFullYear()).slice(-2)
  const mm = String(d.getMonth() + 1).padStart(2, '0')
  const dd = String(d.getDate()).padStart(2, '0')
  return `${yy}${mm}${dd}`
}

const genOrderCode = () => {
  const date = getYYMMDD(new Date())
  const seq = String(Math.floor(Math.random() * 10000)).padStart(4, '0')
  return `PO${date}${seq}`
}

const formData = ref({
  purchaseOrderCode: genOrderCode(),
  vendorName: '',
  vendorId: '' as string,
  vendorContactName: '',
  vendorContactId: '' as string,
  purchaseUserId: '' as string,
  purchaseUserName: '',
  type: 1,
  currency: 1,
  deliveryDate: '',
  deliveryAddress: '',
  comment: '',
  innerComment: '',
  items: [] as any[]
})

const formRules = computed(() => {
  if (!allowManualVendorPick.value) return {}
  return {
    vendorId: [{ required: true, message: '请选择供应商', trigger: 'change' }]
  }
})

const calculateTotal = computed(() =>
  formData.value.items.reduce((sum, item) => sum + (item.qty || 0) * (item.targetPrice || 0), 0)
)

function onPurchaserChange(payload: { id: string; label: string }) {
  formData.value.purchaseUserId = payload?.id || ''
  formData.value.purchaseUserName = payload?.label || ''
}

function syncLineVendorIds() {
  const vid = formData.value.vendorId?.trim()
  if (!vid) return
  formData.value.items.forEach((it) => {
    it.vendorId = vid
  })
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
  formData.value.vendorContactName = ''
  vendorContactOptions.value = []
  if (!val) {
    formData.value.vendorName = ''
    formData.value.vendorId = ''
    formData.value.items.forEach((it) => {
      it.vendorId = undefined
    })
    return
  }
  const found = vendorOptions.value.find((x) => x.value === val)
  if (found) formData.value.vendorName = found.label
  syncLineVendorIds()
  void loadVendorContacts(val)
}

async function loadVendorContacts(vendorId: string) {
  if (!vendorId) {
    vendorContactOptions.value = []
    return
  }
  contactLoading.value = true
  try {
    const list = await vendorContactApi.getContactsByVendorId(vendorId)
    vendorContactOptions.value = list.map((c) => ({
      value: c.id,
      label: [c.cName, c.mobile].filter(Boolean).join(' / ') || c.id
    }))
  } catch {
    vendorContactOptions.value = []
  } finally {
    contactLoading.value = false
  }
}

function onVendorContactChange(id: string | null | undefined) {
  if (!id) {
    formData.value.vendorContactName = ''
    return
  }
  const row = vendorContactOptions.value.find((c) => c.value === id)
  formData.value.vendorContactName = row?.label?.split(' / ')[0]?.trim() || ''
}

const addItem = () => {
  generatedFromRequisition.value = false
  formData.value.items.push({
    sellOrderItemId: undefined,
    vendorId: formData.value.vendorId?.trim() || undefined,
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

function buildItemsPayload() {
  const headerVendor = formData.value.vendorId?.trim() || ''
  return formData.value.items.map((it) => ({
    sellOrderItemId: linkedSellOrderItemIdForPayload(it.sellOrderItemId),
    vendorId: it.vendorId?.trim() || headerVendor || MANUAL_VENDOR_ID,
    pn: it.pn,
    brand: it.brand,
    qty: it.qty,
    cost: it.targetPrice,
    currency: it.currency ?? formData.value.currency,
    deliveryDate: it.deliveryDate || null,
    comment: it.comment || undefined,
    innerComment: it.innerComment || undefined
  }))
}

async function loadOrderForEdit(id: string) {
  const o = (await purchaseOrderApi.getById(id)) as Record<string, unknown>
  formData.value.purchaseOrderCode = String(o.purchaseOrderCode ?? formData.value.purchaseOrderCode)
  formData.value.vendorName = String(o.vendorName ?? '')
  formData.value.vendorId = String(o.vendorId ?? '')
  formData.value.vendorContactId = String(o.vendorContactId ?? '')
  formData.value.vendorContactName = String((o as { vendorContactName?: string }).vendorContactName ?? '')
  formData.value.purchaseUserId = String(o.purchaseUserId ?? '')
  formData.value.purchaseUserName = String(o.purchaseUserName ?? '')
  formData.value.type = Number(o.type ?? 1)
  formData.value.currency = Number(o.currency ?? 1)
  const dd = o.deliveryDate
  formData.value.deliveryDate =
    dd == null ? '' : typeof dd === 'string' ? dd.split('T')[0]! : String(dd)
  formData.value.deliveryAddress = String(o.deliveryAddress ?? '')
  formData.value.comment = String(o.comment ?? '')
  formData.value.innerComment = String(o.innerComment ?? '')
  const items = (o.items as Record<string, unknown>[] | undefined) || []
  formData.value.items = items.map((it) => {
    const cost = Number(it.cost) || 0
    const d = it.deliveryDate
    const deliveryDateStr =
      d == null ? '' : typeof d === 'string' ? d.split('T')[0]! : String(d)
    return {
      sellOrderItemId: it.sellOrderItemId as string | undefined,
      vendorId: it.vendorId as string | undefined,
      pn: String(it.pn ?? ''),
      brand: String(it.brand ?? ''),
      customerMaterialModel: '',
      targetPrice: cost,
      qty: Number(it.qty) || 1,
      cost,
      currency: Number(it.currency ?? formData.value.currency),
      dateCode: '',
      deliveryDate: deliveryDateStr,
      comment: String(it.comment ?? ''),
      innerComment: String(it.innerComment ?? '')
    }
  })
}

const handleSubmit = async () => {
  if (!canSubmitPurchaseOrder.value) {
    ElMessage.warning(
      editId.value
        ? '当前账号无权限保存采购订单'
        : '当前账号无权限创建采购订单，请由采购岗位从采购申请生成或新建'
    )
    return
  }
  if (allowManualVendorPick.value) {
    const ok = await validateElFormOrWarn(formRef)
    if (!ok) return
  }
  await runSaveTask({
    loading: submitLoading,
    successMessage: editId.value ? '采购订单已保存' : '采购订单创建成功',
    task: async () => {
      const uid = formData.value.purchaseUserId || authStore.user?.id || undefined
      const uname = formData.value.purchaseUserName || authStore.user?.userName || undefined
      if (editId.value) {
        const updateBody = {
          purchaseUserId: uid,
          purchaseUserName: uname,
          type: formData.value.type,
          currency: formData.value.currency,
          deliveryDate: formData.value.deliveryDate || null,
          deliveryAddress: formData.value.deliveryAddress || undefined,
          comment: formData.value.comment || undefined,
          innerComment: formData.value.innerComment || undefined,
          items: buildItemsPayload()
        }
        if (import.meta.env.DEV) {
          // eslint-disable-next-line no-console
          console.info('[PurchaseOrderCreate] PUT purchase-orders', editId.value, JSON.parse(JSON.stringify(updateBody)))
        }
        await purchaseOrderApi.update(editId.value, updateBody)
        return
      }
      const createBody = {
        purchaseOrderCode: formData.value.purchaseOrderCode,
        vendorId: formData.value.vendorId || MANUAL_VENDOR_ID,
        vendorName: formData.value.vendorName,
        purchaseUserId: uid,
        purchaseUserName: uname,
        vendorContactId: formData.value.vendorContactId || undefined,
        type: formData.value.type,
        currency: formData.value.currency,
        deliveryDate: formData.value.deliveryDate || null,
        deliveryAddress: formData.value.deliveryAddress || undefined,
        comment: formData.value.comment || undefined,
        innerComment: formData.value.innerComment || undefined,
        items: buildItemsPayload()
      }
      if (import.meta.env.DEV) {
        // eslint-disable-next-line no-console
        console.info('[PurchaseOrderCreate] POST purchase-orders', JSON.parse(JSON.stringify(createBody)))
      }
      await purchaseOrderApi.create(createBody)
    },
    onSuccess: () =>
      editId.value
        ? router.push({ name: 'PurchaseOrderDetail', params: { id: editId.value } })
        : router.push({ name: 'PurchaseOrderList' }),
    errorMessage: (err) => {
      if (import.meta.env.DEV) {
        // eslint-disable-next-line no-console
        console.error('[PurchaseOrderCreate] 保存/创建失败', err)
      }
      return getApiErrorMessage(err, editId.value ? '保存失败，请重试' : '创建失败，请重试')
    }
  })
}

async function handleGeneratePurchaseOrder() {
  if (!requisitionId.value) return
  genLoading.value = true
  try {
    const pr = await purchaseRequisitionApi.getById(requisitionId.value)
    const prExt = pr as Record<string, unknown>

    // 基于采购申请预填采购订单（PRD：预期采购价来自采购申请的 QuoteCost）
    formData.value.purchaseOrderCode = genOrderCode()
    // 订单类型：与销售订单一致（客单采购/备货采购/样品采购），非 PR 的专属/公开备货类型
    const poType = Number(prExt.prefillPurchaseOrderType ?? 0)
    formData.value.type = poType >= 1 && poType <= 3 ? poType : 1
    formData.value.vendorName = pr.intendedVendorName ?? ''
    formData.value.vendorId = pr.quoteVendorId ?? ''
    formData.value.vendorContactId = pr.intendedVendorContactId ?? ''
    formData.value.vendorContactName = pr.intendedVendorContactName ?? ''
    // 采购员：报价单采购员 → 需求明细询价采购员 → 采购申请采购员（均可于本页修改）
    const quoteUid = String(prExt.prefillPurchaseUserId ?? '').trim()
    const quoteName = String(prExt.prefillPurchaseUserName ?? '').trim()
    const rfqUid = String(prExt.prefillRfqPurchaserUserId ?? '').trim()
    const rfqName = String(prExt.prefillRfqPurchaserUserName ?? '').trim()
    const prUid = String(pr.purchaseUserId ?? '').trim()
    const prName = String(pr.purchaseUserName ?? '').trim()
    const pickUid = quoteUid || rfqUid || prUid
    const pickName = quoteUid ? quoteName : rfqUid ? rfqName : prName
    formData.value.purchaseUserId = pickUid || formData.value.purchaseUserId
    formData.value.purchaseUserName = pickName || formData.value.purchaseUserName
    formData.value.currency = pr.currency ?? formData.value.currency ?? 1

    const deliveryDateStr = pr.deliveryDate ? String(pr.deliveryDate).split('T')[0] : ''
    formData.value.deliveryDate = deliveryDateStr || (pr.expectedPurchaseTime ? String(pr.expectedPurchaseTime).split('T')[0] : '')
    // 基本信息-备注默认空白（PR里的备注/内部备注不回填到主表备注，避免干扰用户手工填写）
    formData.value.comment = ''
    formData.value.items = [
      {
        sellOrderItemId: pr.sellOrderItemId ? String(pr.sellOrderItemId).trim() : undefined,
        vendorId: pr.quoteVendorId ?? MANUAL_VENDOR_ID,
        pn: pr.pn ?? '',
        brand: pr.brand ?? '',
        customerMaterialModel: pr.customerMaterialModel ?? '',
        targetPrice: pr.targetPrice ?? 0,
        qty: pr.qty ?? 1,
        cost: pr.quoteCost ?? 0,
        currency: pr.currency ?? formData.value.currency,
        dateCode: coercePd(String(pr.dateCode ?? '').trim()),
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

onMounted(async () => {
  await ensureMaterialPdDict()
  if (editId.value) {
    try {
      await loadOrderForEdit(editId.value)
    } catch {
      ElMessage.error('加载采购订单失败')
    }
    return
  }
  if (requisitionId.value) {
    await handleGeneratePurchaseOrder()
    return
  }
  const u = authStore.user
  if (u?.id) {
    formData.value.purchaseUserId = u.id
    formData.value.purchaseUserName = u.userName || ''
  }
  const qType = Number(route.query.type)
  if (qType >= 1 && qType <= 3) {
    formData.value.type = qType
  }
})
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

/* PurchaseOrderCreate.vue — 独立新建页面，暗色科技风；页头/明细按钮见《列表操作按钮颜色规范》语义 */
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

.po-vendor-select {
  width: 100%;
}
.po-vendor-search-hint {
  padding: 8px 12px;
  font-size: 12px;
  color: $text-muted;
  text-align: center;
}

.create-form {
  .form-section {
    background: $layer-2;
    border: 1px solid $border-card;
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
    color: $text-primary;
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

    /* success：新增明细行，与规范「新建/新增/创建」同色 */
    :deep(.add-item-btn.el-button--success) {
      --el-button-bg-color: rgba(46, 160, 67, 0.2);
      --el-button-border-color: rgba(70, 191, 145, 0.45);
      --el-button-text-color: #6fe0a8;
      --el-button-hover-bg-color: rgba(46, 160, 67, 0.32);
      --el-button-hover-border-color: rgba(95, 212, 168, 0.55);
      --el-button-hover-text-color: #9af0c4;
    }
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

  .head-mpn {
    color: $text-primary;
    font-weight: 600;
  }
  .head-quote {
    color: $text-muted;
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
    color: $text-muted;
    font-size: 13px;
  }

  .total-amount {
    color: $cyan-primary;
    font-size: 16px;
    font-weight: 700;
  }
}

.subtotal {
  color: $cyan-primary;
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
  color: $text-muted;
  font-size: 13px;
}

.line-total-amount {
  color: $cyan-primary;
  font-size: 14px;
  font-weight: 700;
}
</style>
