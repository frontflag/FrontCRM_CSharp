<template>
  <div class="po-upsert-page">
    <!-- CaptionBar（《业务详情页面规范》§3 单据类） -->
    <div class="page-header">
      <div class="header-left">
        <button class="btn-back" type="button" @click="handleBack">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <polyline points="15 18 9 12 15 6" />
          </svg>
          返回列表
        </button>
        <div class="po-caption-title-group">
          <div class="caption-avatar-lg">{{ captionAvatarChar }}</div>
          <div>
            <div class="page-title-row">
              <div class="page-title-with-icons">
                <h1 class="page-title">
                  <template v-if="isEditMode && formData.purchaseOrderCode">
                    采购订单 {{ formData.purchaseOrderCode }}
                  </template>
                  <template v-else>{{ pageTitle }}</template>
                </h1>
              </div>
            </div>
            <div class="title-meta title-meta--caption po-header-meta-row">
              <el-tag effect="dark" :type="isEditMode ? 'warning' : 'primary'" size="small">
                {{ isEditMode ? '编辑' : '新建' }}
              </el-tag>
              <el-tag
                v-if="Number(formData.type) === 2"
                type="warning"
                effect="plain"
                size="small"
                class="po-stocking-tag"
                round
              >
                备货
              </el-tag>
              <span v-if="formData.purchaseOrderCode" class="po-caption-meta-text">
                单号 {{ formData.purchaseOrderCode }}
              </span>
            </div>
          </div>
        </div>
      </div>
      <div class="header-right">
        <button class="btn-secondary" type="button" @click="handleBack">取消</button>
        <button class="btn-primary" type="button" :disabled="submitLoading" @click="handleSubmit">
          <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <polyline points="20 6 9 17 4 12" />
          </svg>
          {{ isEditMode ? '保存修改' : '保存' }}
        </button>
      </div>
    </div>

    <el-alert
      v-if="vendorChangeTipVisible"
      :title="vendorChangeTipTitle"
      :type="vendorChangeTipType"
      :closable="false"
      show-icon
      class="po-vendor-change-hint"
    >
      <div
        v-for="(line, idx) in vendorChangeTipLines"
        :key="idx"
        class="po-vendor-change-hint__line"
      >
        {{ line }}
      </div>
    </el-alert>

    <div
      class="po-upsert-content"
      v-loading="genLoading || submitLoading"
      element-loading-background="rgba(10,22,40,0.8)"
    >
    <el-form ref="formRef" :model="formData" :rules="formRules" label-width="100px" class="upsert-form">

      <!-- 基本信息（§4 info-section） -->
      <div class="info-section basic-info-section">
        <div class="section-header">
          <div class="section-header__main">
            <div class="section-dot section-dot--cyan"></div>
            <span class="section-title">基本信息</span>
          </div>
          <div v-if="formData.purchaseUserName" class="section-header__meta">
            <span class="section-header-meta-item">
              <span class="section-header-meta-item__label">采购员</span>
              <span class="section-header-meta-item__value">{{ formData.purchaseUserName || '—' }}</span>
            </span>
          </div>
        </div>
        <div class="basic-info-section__body">
        <el-row :gutter="24">
          <el-col :span="12">
            <el-form-item v-if="showVendorPicker" label="供应商" prop="vendorId">
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
              <vendor-name-readonly-field
                :name-zh="formData.vendorName"
                :name-en="formData.vendorEnglishName"
                :masked="maskPurchaseSensitiveFields"
                mode="compact"
                class="po-vendor-name-input"
              />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item v-if="showVendorPicker" label="供应商联系人">
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
              <el-input
                :model-value="maskPurchaseSensitiveFields ? '—' : formData.vendorContactName"
                disabled
                placeholder="系统自动带出联系人"
              />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="24">
          <el-col :span="12">
            <el-form-item label="采购员">
              <el-select
                v-if="staffPickLocked || staffPickFree"
                v-model="formData.purchaseUserId"
                placeholder="请选择采购员"
                filterable
                clearable
                style="width: 100%"
                :loading="purchaseUserOptionsLoading"
                @change="onPurchaseUserSelectChange"
              >
                <el-option
                  v-for="u in purchaseUserSelectOptions"
                  :key="u.id"
                  :label="u.userName"
                  :value="u.id"
                />
              </el-select>
              <purchaser-cascader
                v-else
                v-model="formData.purchaseUserId"
                placeholder="请选择采购员（默认当前账号，可更换）"
                clearable
                @change="onPurchaserChange"
              />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="采购助理">
              <el-input
                v-if="staffPickLocked"
                :model-value="assistorReadonlyLabel"
                readonly
              />
              <purchase-ops-assistor-select
                v-else
                v-model="formData.assistor"
                placeholder="请选择采购助理（可选）"
                clearable
              />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="24">
          <el-col :span="12">
            <el-form-item label="订单类型">
              <el-select v-model="formData.type" style="width: 100%" disabled>
                <el-option :label="t('salesOrderCreate.orderTypes.normal')" :value="1" />
                <el-option :label="t('salesOrderCreate.orderTypes.urgent')" :value="2" />
                <el-option :label="t('salesOrderCreate.orderTypes.sample')" :value="3" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="付款约定">
              <div class="po-pay-later-row" :class="{ 'is-checked': formData.isPayLater }">
                <el-checkbox v-model="formData.isPayLater">后付款</el-checkbox>
                <el-tooltip content="客户付款后再给供应商付款" placement="top">
                  <el-icon class="po-pay-later-help" aria-label="后付款说明"><QuestionFilled /></el-icon>
                </el-tooltip>
              </div>
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
      </div>

      <!-- 订单明细 -->
      <div class="info-section items-section">
        <div class="section-header section-header--items">
          <div class="section-header__main">
            <div class="section-dot section-dot--amber"></div>
            <span class="section-title">订单明细</span>
          </div>
          <div class="section-header__actions">
            <button
              v-if="allowAddPoItem"
              type="button"
              class="btn-success add-item-btn"
              @click="addItem"
            >
              <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <line x1="12" y1="5" x2="12" y2="19" /><line x1="5" y1="12" x2="19" y2="12" />
              </svg>
              添加明细
            </button>
          </div>
        </div>
        <div class="items-section__body">
        <div v-if="formData.items.length === 0" class="items-empty">暂无明细</div>

        <div v-for="(item, index) in formData.items" :key="index" class="material-card">
          <div class="material-card-head">
            <span class="head-mpn">物料型号：{{ item.pn || '—' }}</span>
            <span class="head-quote">
              报价：{{ formatUnitPriceWithCurrencyCodeSuffix(item.cost || 0, item.quoteCurrency ?? item.currency) }}
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
                  <BizBrandSelect
                    v-model="item.brandId"
                    placeholder="请选择品牌"
                    @change="(p) => onItemBrandChange(item, p)"
                  />
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
                <el-form-item label="报价">
                  <el-input
                    :model-value="formatUnitPriceWithCurrencyCodeSuffix(item.cost || 0, item.quoteCurrency ?? item.currency)"
                    disabled
                  />
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
              <el-button
                v-if="canRemovePoItem"
                link
                type="danger"
                size="small"
                @click="removeItem(index)"
              >
                删除
              </el-button>
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
      </div>

    </el-form>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute, useRouter } from 'vue-router'
import type { FormInstance } from 'element-plus'
import { ElMessage, ElMessageBox } from 'element-plus'
import { QuestionFilled } from '@element-plus/icons-vue'
import { purchaseOrderApi, type PurchaseOrderVendorChangePreviewResult } from '@/api/purchaseOrder'
import { purchaseRequisitionApi } from '@/api/purchaseRequisition'
import { usePurchaseRequisitionPoBasketStore } from '@/stores/purchaseRequisitionPoBasket'
import {
  buildPoLineItemFromPr,
  getPrPrefillPoType,
  getPrQuoteCurrency,
  messageKeyForPrBatchValidateError,
  prBatchValidateMessageParams,
  resolveLatestDeliveryDate,
  resolvePurchaserFromPr,
  validatePrBatchForPoGeneration
} from '@/utils/purchaseRequisitionBatchPo'
import { vendorApi, vendorContactApi } from '@/api/vendor'
import VendorNameReadonlyField from '@/components/Vendor/VendorNameReadonlyField.vue'
import { runSaveTask, validateElFormOrWarn } from '@/composables/useFormSubmit'
import { getApiErrorMessage } from '@/utils/apiError'
import type { Vendor } from '@/types/vendor'
import { useAuthStore } from '@/stores/auth'
import { canSubmitPurchaseOrderCreate } from '@/utils/purchaseOrderCreateGate'
import {
  messageKeyForPoCustomerOrderValidateError,
  PO_TYPE_CUSTOMER,
  validateCustomerOrderItemsForSave
} from '@/utils/purchaseOrderItemLinkRules'
import PurchaserCascader from '@/components/PurchaserCascader.vue'
import PurchaseOpsAssistorSelect from '@/components/PurchaseOpsAssistorSelect.vue'
import { authApi, type PurchaseDeptStaffUserOption } from '@/api/auth'
import {
  canChangePurchaseOrderVendorOnOrder,
  canPickPurchaseOrderStaffFreely,
  isPurchaseOrderAssistorLockedMode
} from '@/utils/purchaseOrderStaffPickRules'
import MaterialProductionDateSelect from '@/components/MaterialProductionDateSelect.vue'
import SettlementCurrencyAmountInput from '@/components/SettlementCurrencyAmountInput.vue'
import { DEFAULT_SETTLEMENT_CURRENCY_CODE } from '@/constants/currency'
import BizBrandSelect from '@/components/Biz/BizBrandSelect.vue'
import { resolveBrandIdsForItems } from '@/utils/bizBrandMatch'
import { formatCurrencyTotal, formatUnitPriceWithCurrencyCodeSuffix } from '@/utils/moneyFormat'
import { useMaterialProductionDateDict } from '@/composables/useMaterialProductionDateDict'
import { usePurchaseSensitiveFieldMask } from '@/composables/usePurchaseSensitiveFieldMask'

const router = useRouter()
const route = useRoute()
const { t } = useI18n()
const authStore = useAuthStore()
const basketStore = usePurchaseRequisitionPoBasketStore()
const { maskPurchaseSensitiveFields } = usePurchaseSensitiveFieldMask()
const { ensureLoaded: ensureMaterialPdDict, coerceProductionDateToCode: coercePd } = useMaterialProductionDateDict()

const editId = computed(() => (route.name === 'PurchaseOrderEdit' ? String(route.params.id || '').trim() : ''))
const isEditMode = computed(() => !!editId.value)
const pageTitle = computed(() => {
  if (editId.value) return '编辑采购订单'
  const qType = Number(route.query.type)
  if (qType === 2) return '新建备货采购订单'
  return '新建采购订单'
})

const captionAvatarChar = computed(() => {
  if (!maskPurchaseSensitiveFields.value && formData.value.vendorName?.trim()) {
    return formData.value.vendorName.trim()[0] || '采'
  }
  const code = String(formData.value.purchaseOrderCode ?? '').trim()
  return (code && code[0]) || '采'
})

function handleBack() {
  router.back()
}

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

const staffPickLocked = computed(() => isPurchaseOrderAssistorLockedMode(authStore.user))
const staffPickFree = computed(() => canPickPurchaseOrderStaffFreely(authStore.user))
const originalVendorId = ref('')
const assistorReadonlyLabel = ref('')
const purchaseUserSelectOptions = ref<PurchaseDeptStaffUserOption[]>([])
const purchaseUserOptionsLoading = ref(false)

const requisitionId = computed(() => {
  const v = route.query.requisitionId
  if (!v) return undefined
  return String(v)
})
const requisitionIds = computed(() => {
  const raw = route.query.requisitionIds
  if (!raw) return [] as string[]
  return String(raw)
    .split(',')
    .map((s) => s.trim())
    .filter(Boolean)
})
const hasRequisitionPrefill = computed(() => !!requisitionId.value || requisitionIds.value.length > 0)
const generatedFromRequisition = ref(false)
const generatedFromRequisitionBatch = ref(false)

/** 客单采购(1) 不允许手工添加明细；备货/样品可添加 */
const allowAddPoItem = computed(() => formData.value.type !== PO_TYPE_CUSTOMER)

/** 客单采购至少保留一条明细 */
const canRemovePoItem = computed(
  () => formData.value.type !== PO_TYPE_CUSTOMER || formData.value.items.length > 1
)

/** 无采购申请链路的纯新建：允许搜索选择供应商/联系人（含备货采购?type=2） */
const allowManualVendorPick = computed(() => !editId.value && !hasRequisitionPrefill.value)
const loadedOrderStatus = ref<number | null>(null)
const canChangePoVendor = computed(() =>
  canChangePurchaseOrderVendorOnOrder(
    {
      isSysAdmin: authStore.user?.isSysAdmin,
      identityType: authStore.user?.identityType,
      roleCodes: authStore.user?.roleCodes,
      hasPermission: (c) => authStore.hasPermission(c)
    },
    loadedOrderStatus.value
  )
)
/** 销售等需脱敏身份时禁止供应商搜索，避免下拉暴露名称 */
const showVendorPicker = computed(() => {
  if (maskPurchaseSensitiveFields.value) return false
  if (allowManualVendorPick.value) return true
  return !!editId.value && canChangePoVendor.value
})
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
  vendorEnglishName: '',
  vendorId: '' as string,
  vendorContactName: '',
  vendorContactId: '' as string,
  purchaseUserId: '' as string,
  purchaseUserName: '',
  assistor: '' as string,
  type: 1,
  currency: DEFAULT_SETTLEMENT_CURRENCY_CODE,
  deliveryDate: '',
  deliveryAddress: '',
  comment: '',
  innerComment: '',
  isPayLater: false,
  items: [] as any[]
})

const formRules = computed(() => {
  if (!showVendorPicker.value) return {}
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

function normalizePurchaseDeptStaffUser(row: Record<string, unknown>): PurchaseDeptStaffUserOption | null {
  const id = String(row.id ?? row.Id ?? '').trim()
  if (!id) return null
  const userName = String(row.userName ?? row.UserName ?? row.label ?? row.Label ?? '').trim()
  return {
    id,
    userName: userName || id,
    realName: row.realName != null ? String(row.realName) : row.RealName != null ? String(row.RealName) : undefined,
    label: userName || id
  }
}

function findPurchaseUserOption(userId: string): PurchaseDeptStaffUserOption | undefined {
  const key = userId.trim().toLowerCase()
  return purchaseUserSelectOptions.value.find((u) => u.id.trim().toLowerCase() === key)
}

function reconcilePurchaseUserWithSelectOptions(allowExistingFromOrder = false) {
  const id = formData.value.purchaseUserId?.trim()
  if (!id) return

  const hit = findPurchaseUserOption(id)
  if (hit) {
    formData.value.purchaseUserName = hit.userName
    return
  }

  const name = formData.value.purchaseUserName?.trim()
  if (allowExistingFromOrder && name) {
    purchaseUserSelectOptions.value = [
      ...purchaseUserSelectOptions.value,
      { id, userName: name, label: name }
    ]
    return
  }

  if (staffPickLocked.value) {
    formData.value.purchaseUserId = ''
    formData.value.purchaseUserName = ''
    return
  }

  if (staffPickFree.value && name) {
    purchaseUserSelectOptions.value = [
      ...purchaseUserSelectOptions.value,
      { id, userName: name, realName: undefined, label: name }
    ]
  }
}

function onPurchaseUserSelectChange(userId: string | undefined) {
  const id = userId ? String(userId) : ''
  const row = findPurchaseUserOption(id)
  formData.value.purchaseUserName = row?.userName ?? ''
}

async function loadPurchaseUserSelectOptionsForAssistor(assistantUserId: string) {
  purchaseUserOptionsLoading.value = true
  try {
    const rows = await authApi.getPurchaseOrderMappedPurchasers(assistantUserId)
    purchaseUserSelectOptions.value = rows
      .map((u) => normalizePurchaseDeptStaffUser(u as unknown as Record<string, unknown>))
      .filter((u): u is PurchaseDeptStaffUserOption => u != null)
    reconcilePurchaseUserWithSelectOptions(!!editId.value)
  } catch (e: unknown) {
    purchaseUserSelectOptions.value = []
    reconcilePurchaseUserWithSelectOptions(false)
    const msg = e instanceof Error ? e.message : String(e)
    ElMessage.error(msg || '加载已配置采购员失败')
  } finally {
    purchaseUserOptionsLoading.value = false
  }
}

async function initStaffPickFields(orderForEdit?: Record<string, unknown>) {
  if (staffPickLocked.value) {
    const me = authStore.user
    if (!editId.value && me?.id) {
      formData.value.assistor = me.id
      assistorReadonlyLabel.value = me.userName || ''
    } else if (orderForEdit) {
      formData.value.assistor = String(orderForEdit.assistor ?? orderForEdit.Assistor ?? formData.value.assistor)
      assistorReadonlyLabel.value = String(
        orderForEdit.assistorUserName ?? orderForEdit.AssistorUserName ?? ''
      )
      if (!assistorReadonlyLabel.value && formData.value.assistor) {
        try {
          const staff = await authApi.getPurchaseOpsStaffUsers()
          const hit = staff.find((s) => s.id === formData.value.assistor)
          assistorReadonlyLabel.value = hit?.userName ?? ''
        } catch {
          /* ignore */
        }
      }
    }
    const assistantId = formData.value.assistor?.trim() || me?.id || ''
    if (assistantId) await loadPurchaseUserSelectOptionsForAssistor(assistantId)
    return
  }

  if (staffPickFree.value) {
    purchaseUserOptionsLoading.value = true
    try {
      const rows = await authApi.getPurchaseDeptStaffUsers()
      purchaseUserSelectOptions.value = rows
        .map((u) => normalizePurchaseDeptStaffUser(u as unknown as Record<string, unknown>))
        .filter((u): u is PurchaseDeptStaffUserOption => u != null)
      reconcilePurchaseUserWithSelectOptions(!!editId.value)
    } catch (e: unknown) {
      purchaseUserSelectOptions.value = []
      reconcilePurchaseUserWithSelectOptions(false)
      const msg = e instanceof Error ? e.message : String(e)
      ElMessage.error(msg || '加载采购部职员失败')
    } finally {
      purchaseUserOptionsLoading.value = false
    }
  }
}

function resolveSubmitPurchaseUserId(): string | undefined {
  const id = formData.value.purchaseUserId?.trim()
  if (id) return id
  if (staffPickLocked.value) return undefined
  return authStore.user?.id || undefined
}

function resolveSubmitPurchaseUserName(): string | undefined {
  const name = formData.value.purchaseUserName?.trim()
  if (name) return name
  if (staffPickLocked.value) return undefined
  return authStore.user?.userName || undefined
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
    clearVendorChangeTip()
    return
  }
  const found = vendorOptions.value.find((x) => x.value === val)
  if (found) formData.value.vendorName = found.label
  syncLineVendorIds()
  void loadVendorContacts(val)
  scheduleVendorChangeTipPreview()
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
    brandId: undefined as number | undefined,
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
  if (formData.value.type === PO_TYPE_CUSTOMER && formData.value.items.length <= 1) {
    ElMessage.warning(t('purchaseOrderCreate.validate.customerOrderCannotRemoveLast'))
    return
  }
  formData.value.items.splice(index, 1)
}

function validatePoItemsCustomerOrderLinks(): boolean {
  const err = validateCustomerOrderItemsForSave(formData.value.type, formData.value.items)
  if (!err) return true
  ElMessage.warning(t(messageKeyForPoCustomerOrderValidateError(err)))
  return false
}

function onItemBrandChange(
  row: { brand?: string; brandId?: number },
  payload: { id: number; standardBrand: string }
) {
  if (payload.id > 0) {
    row.brand = (payload.standardBrand || '').trim()
  } else {
    row.brand = ''
    row.brandId = undefined
  }
}

function validateItemsBrand(): boolean {
  if (!formData.value.items.length) {
    ElMessage.warning('请至少添加一条订单明细')
    return false
  }
  for (let i = 0; i < formData.value.items.length; i++) {
    const it = formData.value.items[i]
    if (!it.brandId || it.brandId <= 0) {
      ElMessage.warning(`明细 ${i + 1}：请选择品牌`)
      return false
    }
  }
  return true
}

function buildItemsPayload() {
  const headerVendor = formData.value.vendorId?.trim() || ''
  return formData.value.items.map((it) => ({
    purchaseOrderItemId: it.purchaseOrderItemId?.trim() || undefined,
    purchaseRequisitionId: it.purchaseRequisitionId?.trim() || undefined,
    sellOrderItemId: linkedSellOrderItemIdForPayload(it.sellOrderItemId),
    vendorId: it.vendorId?.trim() || headerVendor || MANUAL_VENDOR_ID,
    pn: it.pn,
    brand: it.brand,
    qty: it.qty,
    cost: it.targetPrice,
    currency: it.currency ?? formData.value.currency,
    deliveryDate: it.deliveryDate || null,
    dateCode: it.dateCode?.trim() || undefined,
    comment: it.comment || undefined,
    innerComment: it.innerComment || undefined
  }))
}

async function loadOrderForEdit(id: string) {
  const o = (await purchaseOrderApi.getById(id)) as Record<string, unknown>
  formData.value.purchaseOrderCode = String(o.purchaseOrderCode ?? formData.value.purchaseOrderCode)
  formData.value.vendorName = String(o.vendorName ?? '')
  formData.value.vendorEnglishName = String(o.vendorEnglishName ?? o.VendorEnglishName ?? '')
  formData.value.vendorId = String(o.vendorId ?? '')
  originalVendorId.value = formData.value.vendorId
  loadedOrderStatus.value = Number(o.status ?? o.Status ?? NaN)
  if (!Number.isFinite(loadedOrderStatus.value)) loadedOrderStatus.value = null
  clearVendorChangeTip()
  formData.value.vendorContactId = String(o.vendorContactId ?? '')
  formData.value.vendorContactName = String((o as { vendorContactName?: string }).vendorContactName ?? '')
  if (formData.value.vendorId && formData.value.vendorName) {
    vendorOptions.value = [{ value: formData.value.vendorId, label: formData.value.vendorName }]
  }
  if (formData.value.vendorId) {
    await loadVendorContacts(formData.value.vendorId)
  }
  formData.value.purchaseUserId = String(o.purchaseUserId ?? '')
  formData.value.purchaseUserName = String(o.purchaseUserName ?? '')
  formData.value.assistor = String(o.assistor ?? '')
  formData.value.type = Number(o.type ?? 1)
  formData.value.currency = Number(o.currency ?? DEFAULT_SETTLEMENT_CURRENCY_CODE)
  const dd = o.deliveryDate
  formData.value.deliveryDate =
    dd == null ? '' : typeof dd === 'string' ? dd.split('T')[0]! : String(dd)
  formData.value.deliveryAddress = String(o.deliveryAddress ?? '')
  formData.value.comment = String(o.comment ?? '')
  formData.value.innerComment = String(o.innerComment ?? '')
  formData.value.isPayLater = Boolean(o.isPayLater ?? o.IsPayLater)
  const items = ((o.items as Record<string, unknown>[] | undefined) || []).filter(
    (it) => !(it.isDeleted ?? it.IsDeleted)
  )
  await initStaffPickFields(o)

  formData.value.items = items.map((it) => {
    const cost = Number(it.cost) || 0
    const d = it.deliveryDate
    const deliveryDateStr =
      d == null ? '' : typeof d === 'string' ? d.split('T')[0]! : String(d)
    return {
      purchaseOrderItemId: String(it.id ?? it.Id ?? it.purchaseOrderItemId ?? '').trim() || undefined,
      purchaseRequisitionId: (() => {
        const s = String(it.purchaseRequisitionId ?? it.PurchaseRequisitionId ?? '').trim()
        return s || undefined
      })(),
      sellOrderItemId: it.sellOrderItemId as string | undefined,
      vendorId: it.vendorId as string | undefined,
      pn: String(it.pn ?? ''),
      brand: String(it.brand ?? ''),
      brandId: undefined as number | undefined,
      customerMaterialModel: '',
      targetPrice: cost,
      qty: Number(it.qty) || 1,
      cost,
      currency: Number(it.currency ?? formData.value.currency),
      dateCode: coercePd(String(it.dateCode ?? it.DateCode ?? '').trim()),
      deliveryDate: deliveryDateStr,
      comment: String(it.comment ?? ''),
      innerComment: String(it.innerComment ?? '')
    }
  })
  await resolveBrandIdsForItems(formData.value.items, { silent: true })
}

function escapeHtml(value: string) {
  return value
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
}

function resolveVendorChangeBlockDetails(p: PurchaseOrderVendorChangePreviewResult) {
  const raw = p as PurchaseOrderVendorChangePreviewResult & {
    BlockReason?: string | null
    BlockingDocuments?: string[] | null
  }
  const blockReason = (p.blockReason ?? raw.BlockReason ?? '').trim()
  const docs = (p.blockingDocuments ?? raw.BlockingDocuments ?? [])
    .map((d) => String(d || '').trim())
    .filter(Boolean)
  return { blockReason, docs }
}

const vendorChangeTipPreview = ref<PurchaseOrderVendorChangePreviewResult | null>(null)
const vendorChangeTipLoading = ref(false)
const vendorChangeTipError = ref('')
let vendorChangeTipTimer: ReturnType<typeof setTimeout> | null = null
let vendorChangeTipSeq = 0

const vendorChangedFromOriginal = computed(() => {
  if (!isEditMode.value || !canChangePoVendor.value) return false
  const cur = (formData.value.vendorId || '').trim()
  const orig = originalVendorId.value.trim()
  return !!cur && !!orig && cur.toLowerCase() !== orig.toLowerCase()
})

const vendorChangeTipVisible = computed(
  () =>
    isEditMode.value &&
    vendorChangedFromOriginal.value &&
    (vendorChangeTipLoading.value || !!vendorChangeTipError.value || !!vendorChangeTipPreview.value)
)

/** 与销售订单换客户 tip 一致：使用 warning 橙底（非 Element info 灰底） */
const vendorChangeTipType = computed<'warning' | 'error'>(() => {
  if (vendorChangeTipError.value) return 'error'
  return 'warning'
})

const vendorChangeTipTitle = computed(() => {
  if (vendorChangeTipLoading.value) return '正在检查下游供应商影响…'
  if (vendorChangeTipError.value) return '下游供应商预检失败'
  const p = vendorChangeTipPreview.value
  if (!p) return ''
  if (!p.canChange) return '当前供应商变更被阻断，当前无法保存供应商资料（仅提醒，尚未保存）'
  if (p.noOp) return '下游供应商已与目标一致，保存时仅更新订单供应商'
  return '更换供应商后，保存时将同步以下未完结下游（仅提醒，尚未保存）'
})

const vendorChangeTipLines = computed(() => {
  if (vendorChangeTipError.value) return [vendorChangeTipError.value]
  const p = vendorChangeTipPreview.value
  if (!p || vendorChangeTipLoading.value) return []
  if (!p.canChange) {
    const { blockReason, docs } = resolveVendorChangeBlockDetails(p)
    const lines: string[] = ['当前无法保存供应商资料。', '阻断原因：']
    if (docs.length > 0) {
      for (const d of docs) lines.push(`· ${d}`)
      if (blockReason && !docs.some((d) => blockReason.includes(d))) {
        lines.push(`· ${blockReason}`)
      }
    } else {
      lines.push(`· ${blockReason || '存在已完结下游单据，无法更换供应商'}`)
    }
    return lines
  }
  if (p.noOp) {
    return [
      `目标供应商：${p.newVendorName?.trim() || p.newVendorId || '—'}（原：${p.oldVendorName?.trim() || p.oldVendorId || '—'}）`
    ]
  }
  const lines = [
    `将由「${p.oldVendorName || p.oldVendorId || '—'}」更换为「${p.newVendorName || p.newVendorId || '—'}」`
  ]
  if ((p.poVendorNameToSync ?? 0) > 0) lines.push('· 采购订单供应商名称快照')
  if (p.poItemsToSync > 0) lines.push(`· 采购明细 ${p.poItemsToSync} 条`)
  if (p.arrivalNoticesToSync > 0) lines.push(`· 到货通知 ${p.arrivalNoticesToSync} 条`)
  if (p.stockInsToSync > 0) lines.push(`· 未过账入库单 ${p.stockInsToSync} 张`)
  if (p.paymentsToSync > 0) lines.push(`· 未完成付款单 ${p.paymentsToSync} 张`)
  if (p.purchaseInvoicesToSync > 0) lines.push(`· 未完成进项发票 ${p.purchaseInvoicesToSync} 张`)
  return lines
})

function clearVendorChangeTip() {
  vendorChangeTipPreview.value = null
  vendorChangeTipError.value = ''
  vendorChangeTipLoading.value = false
}

function scheduleVendorChangeTipPreview() {
  if (!isEditMode.value || !editId.value || !canChangePoVendor.value) {
    clearVendorChangeTip()
    return
  }
  if (!vendorChangedFromOriginal.value) {
    if (vendorChangeTipTimer) {
      clearTimeout(vendorChangeTipTimer)
      vendorChangeTipTimer = null
    }
    clearVendorChangeTip()
    return
  }
  if (vendorChangeTipTimer) clearTimeout(vendorChangeTipTimer)
  vendorChangeTipTimer = setTimeout(() => {
    void refreshVendorChangeTipPreview()
  }, 350)
}

async function refreshVendorChangeTipPreview() {
  if (!editId.value || !vendorChangedFromOriginal.value) {
    clearVendorChangeTip()
    return
  }
  const seq = ++vendorChangeTipSeq
  const proposedId = formData.value.vendorId?.trim() || ''
  vendorChangeTipLoading.value = true
  vendorChangeTipError.value = ''
  try {
    const preview = await purchaseOrderApi.previewVendorChange(editId.value, proposedId)
    if (seq !== vendorChangeTipSeq) return
    vendorChangeTipPreview.value = preview
  } catch (e) {
    if (seq !== vendorChangeTipSeq) return
    vendorChangeTipPreview.value = null
    vendorChangeTipError.value = getApiErrorMessage(e, '预检失败，请稍后重试')
  } finally {
    if (seq === vendorChangeTipSeq) vendorChangeTipLoading.value = false
  }
}

function buildVendorChangeConfirmMessage(preview: PurchaseOrderVendorChangePreviewResult) {
  const oldName = escapeHtml(preview.oldVendorName || preview.oldVendorId || '—')
  const newName = escapeHtml(preview.newVendorName || preview.newVendorId || '—')
  const lines = [
    preview.sameVendorId
      ? `将按供应商「${newName}」刷新名称快照并同步未完结下游。`
      : `将把供应商由「<span style="color:#F56C6C">${oldName}</span>」更换为「${newName}」。`,
    escapeHtml('确认后将一次保存订单供应商，并同步未完结下游。')
  ]
  if ((preview.poVendorNameToSync ?? 0) > 0) {
    lines.push(escapeHtml('同步采购订单供应商名称快照。'))
  }
  if (preview.poItemsToSync > 0) {
    lines.push(escapeHtml(`同步 ${preview.poItemsToSync} 条采购明细。`))
  }
  if (preview.arrivalNoticesToSync > 0) {
    lines.push(escapeHtml(`同步 ${preview.arrivalNoticesToSync} 条未到货完成的到货通知。`))
  }
  if (preview.stockInsToSync > 0) {
    lines.push(escapeHtml(`同步 ${preview.stockInsToSync} 张未过账入库单。`))
  }
  if (preview.paymentsToSync > 0) {
    lines.push(escapeHtml(`同步 ${preview.paymentsToSync} 张未完成付款单。`))
  }
  if (preview.purchaseInvoicesToSync > 0) {
    lines.push(escapeHtml(`同步 ${preview.purchaseInvoicesToSync} 张进项发票。`))
  }
  const completedDocs = (preview.completedDocuments ?? []).map((d) => String(d || '').trim()).filter(Boolean)
  if (completedDocs.length > 0) {
    lines.push(`<span style="color:#b45309;">以下已完结单据也会一并改写，请确认：</span>`)
    for (const d of completedDocs) {
      lines.push(escapeHtml(`· ${d}`))
    }
  }
  if (!preview.sameVendorId) {
    lines.push(escapeHtml('若原供应商联系人不属于新供应商，将自动清空。'))
  }
  return lines.join('<br/>')
}

async function confirmVendorChangeIfNeeded(): Promise<boolean> {
  if (!editId.value || !canChangePoVendor.value) return true
  const newVid = formData.value.vendorId?.trim()
  if (!newVid || newVid === originalVendorId.value.trim()) return true

  try {
    const preview = await purchaseOrderApi.previewVendorChange(editId.value, newVid)
    vendorChangeTipPreview.value = preview
    if (!preview.canChange) {
      const { blockReason, docs } = resolveVendorChangeBlockDetails(preview)
      const detail =
        docs.length > 0
          ? docs.map((d) => `· ${escapeHtml(d)}`).join('<br/>')
          : escapeHtml(blockReason || '存在已完结下游单据，无法更换供应商')
      await ElMessageBox.alert(
        `当前无法保存供应商资料。<br/><br/>阻断原因：<br/>${detail}`,
        '更换供应商',
        {
          confirmButtonText: '知道了',
          type: 'warning',
          dangerouslyUseHTMLString: true
        }
      )
      return false
    }
    if (preview.noOp) {
      await ElMessageBox.confirm(
        `将把供应商更换为「${preview.newVendorName || preview.newVendorId}」。下游无需同步，是否保存？`,
        '更换供应商确认',
        {
          type: 'warning',
          confirmButtonText: '保存',
          cancelButtonText: '取消'
        }
      )
      return true
    }
    await ElMessageBox.confirm(buildVendorChangeConfirmMessage(preview), '更换供应商确认', {
      type: 'warning',
      confirmButtonText: '刷新并保存',
      cancelButtonText: '取消',
      dangerouslyUseHTMLString: true
    })
    return true
  } catch (e) {
    if (e === 'cancel') return false
    ElMessage.error(getApiErrorMessage(e, '预检更换供应商失败'))
    return false
  }
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
  if (showVendorPicker.value) {
    const ok = await validateElFormOrWarn(formRef)
    if (!ok) return
  }
  if (!validateItemsBrand()) return
  if (!validatePoItemsCustomerOrderLinks()) return
  if (!(await confirmVendorChangeIfNeeded())) return
  await runSaveTask({
    loading: submitLoading,
    successMessage: editId.value ? '采购订单已保存' : '采购订单创建成功',
    task: async () => {
      const uid = resolveSubmitPurchaseUserId()
      const uname = resolveSubmitPurchaseUserName()
      if (editId.value) {
        const newVid = formData.value.vendorId?.trim()
        const vendorChanged = !!newVid && newVid !== originalVendorId.value.trim()
        const updateBody: Record<string, unknown> = {
          purchaseUserId: uid,
          purchaseUserName: uname,
          assistor: formData.value.assistor?.trim() || null,
          type: formData.value.type,
          currency: formData.value.currency,
          deliveryDate: formData.value.deliveryDate || null,
          deliveryAddress: formData.value.deliveryAddress || undefined,
          comment: formData.value.comment || undefined,
          innerComment: formData.value.innerComment || undefined,
          isPayLater: !!formData.value.isPayLater,
          items: buildItemsPayload()
        }
        if (vendorChanged) updateBody.vendorId = newVid
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
        assistor: formData.value.assistor?.trim() || undefined,
        vendorContactId: formData.value.vendorContactId || undefined,
        type: formData.value.type,
        currency: formData.value.currency,
        deliveryDate: formData.value.deliveryDate || null,
        deliveryAddress: formData.value.deliveryAddress || undefined,
        comment: formData.value.comment || undefined,
        innerComment: formData.value.innerComment || undefined,
        isPayLater: !!formData.value.isPayLater,
        items: buildItemsPayload()
      }
      if (import.meta.env.DEV) {
        // eslint-disable-next-line no-console
        console.info('[PurchaseOrderCreate] POST purchase-orders', JSON.parse(JSON.stringify(createBody)))
      }
      await purchaseOrderApi.create(createBody)
    },
    onSuccess: () => {
      if (!editId.value && (generatedFromRequisition.value || generatedFromRequisitionBatch.value)) {
        basketStore.clear()
      }
      if (editId.value) {
        router.push({ name: 'PurchaseOrderDetail', params: { id: editId.value } })
      } else {
        router.push({ name: 'PurchaseOrderList' })
      }
    },
    errorMessage: (err) => {
      if (import.meta.env.DEV) {
        // eslint-disable-next-line no-console
        console.error('[PurchaseOrderCreate] 保存/创建失败', err)
      }
      return getApiErrorMessage(err, editId.value ? '保存失败，请重试' : '创建失败，请重试')
    }
  })
}

async function applyPrsToPurchaseOrderForm(prs: Record<string, unknown>[]) {
  if (!prs.length) return
  const first = prs[0]!

  formData.value.purchaseOrderCode = genOrderCode()
  formData.value.type = getPrPrefillPoType(first)
  formData.value.vendorName = String(first.intendedVendorName ?? first.IntendedVendorName ?? '')
  formData.value.vendorId = String(first.quoteVendorId ?? first.QuoteVendorId ?? '')
  formData.value.vendorContactId = String(first.intendedVendorContactId ?? first.IntendedVendorContactId ?? '')
  formData.value.vendorContactName = String(first.intendedVendorContactName ?? first.IntendedVendorContactName ?? '')

  const purchaser = resolvePurchaserFromPr(first)
  if (!staffPickLocked.value) {
    formData.value.purchaseUserId = purchaser.id || formData.value.purchaseUserId
    formData.value.purchaseUserName = purchaser.name || formData.value.purchaseUserName
  }

  formData.value.currency = getPrQuoteCurrency(first)
  formData.value.deliveryDate = resolveLatestDeliveryDate(prs)
  formData.value.comment = ''

  const headerDelivery = formData.value.deliveryDate
  formData.value.items = prs.map((pr) =>
    buildPoLineItemFromPr(pr, {
      manualVendorId: MANUAL_VENDOR_ID,
      coercePd,
      headerDeliveryDate: headerDelivery
    })
  )

  await resolveBrandIdsForItems(formData.value.items, { silent: true })
  generatedFromRequisition.value = true
  generatedFromRequisitionBatch.value = prs.length > 1
  await initStaffPickFields()
  if (staffPickLocked.value) {
    const assistantId = formData.value.assistor?.trim() || authStore.user?.id || ''
    if (assistantId) await loadPurchaseUserSelectOptionsForAssistor(assistantId)
  } else if (staffPickFree.value) {
    reconcilePurchaseUserWithSelectOptions(false)
  }
}

async function handleGeneratePurchaseOrder() {
  if (!requisitionId.value) return
  genLoading.value = true
  try {
    const pr = await purchaseRequisitionApi.getById(requisitionId.value)
    await applyPrsToPurchaseOrderForm([pr as unknown as Record<string, unknown>])
  } catch (e) {
    // eslint-disable-next-line no-console
    console.error(e)
    ElMessage.error(t('purchaseRequisitionList.basket.prefillFailed'))
  } finally {
    genLoading.value = false
  }
}

async function handleGenerateFromRequisitions(ids: string[]) {
  if (!ids.length) return
  genLoading.value = true
  try {
    const prs = await Promise.all(ids.map((id) => purchaseRequisitionApi.getById(id)))
    const prRecords = prs.map((p) => p as unknown as Record<string, unknown>)
    const err = validatePrBatchForPoGeneration(prRecords)
    if (err) {
      await ElMessageBox.alert(
        t(messageKeyForPrBatchValidateError(err), prBatchValidateMessageParams(err)),
        t('purchaseRequisitionList.basket.batchValidateTitle'),
        { type: 'warning', confirmButtonText: t('common.confirm') }
      )
      router.back()
      return
    }
    await applyPrsToPurchaseOrderForm(prRecords)
  } catch (e) {
    // eslint-disable-next-line no-console
    console.error(e)
    ElMessage.error(t('purchaseRequisitionList.basket.prefillFailed'))
  } finally {
    genLoading.value = false
  }
}

onBeforeUnmount(() => {
  if (vendorChangeTipTimer) {
    clearTimeout(vendorChangeTipTimer)
    vendorChangeTipTimer = null
  }
})

onMounted(async () => {
  await ensureMaterialPdDict()
  try {
    await authStore.fetchCurrentUser()
  } catch {
    /* 未登录等由路由守卫处理 */
  }

  if (editId.value) {
    try {
      await loadOrderForEdit(editId.value)
    } catch {
      ElMessage.error('加载采购订单失败')
    }
    return
  }
  if (requisitionIds.value.length > 0) {
    await handleGenerateFromRequisitions(requisitionIds.value)
    return
  }
  if (requisitionId.value) {
    await handleGeneratePurchaseOrder()
    return
  }

  await initStaffPickFields()

  const u = authStore.user
  if (!staffPickLocked.value && u?.id) {
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

.po-upsert-page {
  padding: 24px;
  min-height: 100%;
  background: $layer-1;
  font-family: 'Noto Sans SC', sans-serif;
}

.po-vendor-change-hint {
  margin: 0 0 16px;
  align-items: flex-start;

  &__line {
    line-height: 1.55;
    font-size: 13px;
    white-space: pre-wrap;
    word-break: break-word;
  }

  :deep(.el-alert__icon) {
    font-size: 16px;
    width: 16px;
    height: 16px;
    margin-top: 2px;

    svg {
      width: 16px;
      height: 16px;
    }
  }

  :deep(.el-alert__title) {
    font-size: 14px;
    line-height: 16px;
  }

  :deep(.el-alert__content) {
    width: 100%;
  }

  :deep(.el-alert__description) {
    margin-top: 4px;
  }
}

.po-upsert-content {
  min-height: 120px;
}

.page-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 16px;
  margin-bottom: 20px;

  .header-left {
    display: flex;
    align-items: center;
    gap: 14px;
    min-width: 0;
    flex: 1;
  }

  .header-right {
    display: flex;
    align-items: center;
    gap: 10px;
    flex-shrink: 0;
  }
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
  flex-shrink: 0;

  &:hover {
    background: rgba(255, 255, 255, 0.07);
    color: $text-secondary;
    border-color: rgba(0, 212, 255, 0.2);
  }
}

.po-caption-title-group {
  display: flex;
  align-items: center;
  gap: 14px;
  min-width: 0;
}

.caption-avatar-lg {
  width: 48px;
  height: 48px;
  background: linear-gradient(135deg, rgba(0, 102, 255, 0.3), rgba(0, 212, 255, 0.2));
  border: 1px solid rgba(0, 212, 255, 0.25);
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 20px;
  font-weight: 700;
  color: $cyan-primary;
  flex-shrink: 0;
}

.page-title-row {
  display: flex;
  align-items: center;
  gap: 10px;
  margin-bottom: 6px;
}

.page-title-with-icons {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-wrap: wrap;
  min-width: 0;
}

.page-title {
  margin: 0;
  font-size: 20px;
  font-weight: 600;
  color: $text-primary;
}

.title-meta {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-wrap: wrap;
}

.title-meta--caption {
  margin-top: 4px;
}

.po-header-meta-row {
  min-height: 28px;
}

.po-caption-meta-text {
  font-size: 13px;
  color: $text-muted;
}

.po-pay-later-row {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  min-height: 24px;
  padding: 3px 12px;
  border-radius: 6px;
  border: 1px solid transparent;
  transition: background 0.15s ease, border-color 0.15s ease;

  &.is-checked {
    background: rgba(234, 179, 8, 0.28);
    border-color: rgba(234, 179, 8, 0.55);
  }

  :deep(.el-checkbox) {
    height: 24px;
  }
}

.po-pay-later-help {
  color: $text-muted;
  cursor: help;
  font-size: 15px;

  &:hover {
    color: $cyan-primary;
  }
}

.po-stocking-tag {
  margin-left: 2px;
}

.btn-secondary {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 8px 14px;
  background: transparent;
  border: 1px solid $border-panel;
  border-radius: $border-radius-md;
  color: $text-secondary;
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  transition: all 0.2s;

  &:hover {
    background: rgba(255, 255, 255, 0.06);
    border-color: rgba(0, 212, 255, 0.25);
    color: $text-primary;
  }
}

.btn-primary {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 8px 14px;
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

  &:disabled {
    opacity: 0.6;
    cursor: not-allowed;
  }
}

.btn-success {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 8px 14px;
  background: linear-gradient(135deg, rgba(46, 160, 67, 0.88), rgba(70, 191, 145, 0.78));
  border: 1px solid rgba(70, 191, 145, 0.45);
  border-radius: $border-radius-md;
  color: #fff;
  font-size: 13px;
  font-family: 'Noto Sans SC', sans-serif;
  cursor: pointer;
  transition: all 0.2s;

  &:hover {
    transform: translateY(-1px);
    box-shadow: 0 4px 16px rgba(70, 191, 145, 0.3);
  }
}

.info-section {
  background: $layer-2;
  border: 1px solid $border-card;
  border-radius: $border-radius-lg;
  margin-bottom: 16px;
  overflow: hidden;
}

.section-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  padding: 14px 20px;
  border-bottom: 1px solid rgba(255, 255, 255, 0.05);
  background: var(--crm-detail-section-header-bg);

  &--items {
    flex-wrap: wrap;
  }
}

.section-header__main {
  display: flex;
  align-items: center;
  gap: 10px;
  min-width: 0;
}

.section-header__meta,
.section-header__actions {
  display: flex;
  align-items: center;
  gap: 12px;
  flex-shrink: 0;
  margin-left: auto;
  flex-wrap: wrap;
}

.section-header-meta-item {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  font-size: 12px;
  white-space: nowrap;

  &__label {
    color: $text-muted;

    &::after {
      content: '：';
    }
  }

  &__value {
    color: $text-secondary;
  }
}

.section-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;

  &--cyan {
    background: $cyan-primary;
    box-shadow: 0 0 6px rgba(0, 212, 255, 0.6);
  }

  &--amber {
    background: $color-amber;
    box-shadow: 0 0 6px rgba(201, 154, 69, 0.5);
  }
}

.section-title {
  font-size: 14px;
  font-weight: 500;
  color: $text-primary;
}

.basic-info-section__body,
.items-section__body {
  padding: 16px 20px 20px;
}

.po-vendor-select {
  width: 100%;
}
.po-vendor-name-row {
  display: flex;
  align-items: center;
  gap: 8px;
  width: 100%;
}
.po-vendor-name-input {
  flex: 1;
  min-width: 0;
}
.po-vendor-search-hint {
  padding: 8px 12px;
  font-size: 12px;
  color: $text-muted;
  text-align: center;
}

.upsert-form {
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
