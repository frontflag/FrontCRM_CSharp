<template>
  <el-dialog
    :model-value="modelValue"
    :title="dialogTitle"
    width="min(96vw, 1200px)"
    class="crm-dialog payment-request-edit-dialog"
    destroy-on-close
    @update:model-value="emit('update:modelValue', $event)"
  >
    <div v-if="loading" class="dialog-loading">
      <el-skeleton :rows="6" animated />
    </div>
    <template v-else-if="payment">
      <el-alert
        v-if="(payment.items?.length ?? 0) > 1"
        type="warning"
        :closable="false"
        show-icon
        class="whole-payment-alert"
        :title="t('financePaymentList.editRequest.wholePaymentHint')"
      />
      <el-form label-width="120px" class="crm-form">
        <el-row :gutter="12">
          <el-col :span="12">
            <el-form-item :label="t('financePaymentList.formVendorId')">
              <el-input :model-value="maskPurchaseSensitiveFields ? '—' : (vendorCodeDisplay || '—')" readonly />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item :label="t('financePaymentList.columns.code')">
              <el-input :model-value="payment.financePaymentCode" readonly />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="12">
          <el-col :span="24">
            <el-form-item :label="t('financePaymentList.formVendorName')">
              <vendor-name-readonly-field
                :name-zh="payment.vendorName"
                :name-en="payment.vendorEnglishName"
                :masked="maskPurchaseSensitiveFields"
                mode="compact"
              />
            </el-form-item>
          </el-col>
        </el-row>

        <payment-request-vendor-bank-section
          v-model="form.vendorBankId"
          :vendor-id="payment.vendorId"
          :banks="vendorBanks"
          :masked="maskPurchaseSensitiveFields"
        >
          <template #trailing>
            <el-form-item :label="t('financePaymentList.formMode')" required>
              <el-select v-model="form.paymentMode" style="width: 100%" :disabled="!canEdit">
                <el-option
                  v-for="k in paymentModeKeys"
                  :key="k"
                  :label="paymentModeLabel(k)"
                  :value="k"
                />
              </el-select>
            </el-form-item>
          </template>
        </payment-request-vendor-bank-section>

        <el-form-item :label="t('financePaymentList.formRemark')">
          <el-input v-model="form.requestRemark" type="textarea" :rows="2" :disabled="!canEdit" />
        </el-form-item>

        <div class="section-title">{{ t('financePaymentList.editRequest.feeSection') }}</div>
        <el-row :gutter="12">
          <el-col :span="8">
            <el-form-item :label="t('financePaymentList.editRequest.intermediateBankFee')">
              <SettlementCurrencyAmountInput
                v-model="form.fee.intermediateBankFee"
                v-model:currency="form.paymentCurrency"
                :min="0"
                :precision="2"
                :disabled="!canEdit"
              />
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item :label="t('financePaymentList.editRequest.bankCharge')">
              <SettlementCurrencyAmountInput
                v-model="form.fee.bankCharge"
                v-model:currency="form.paymentCurrency"
                :min="0"
                :precision="2"
                :disabled="!canEdit"
              />
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item :label="t('financePaymentList.editRequest.freight')">
              <SettlementCurrencyAmountInput
                v-model="form.fee.freight"
                v-model:currency="form.paymentCurrency"
                :min="0"
                :precision="2"
                :disabled="!canEdit"
              />
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item :label="t('financePaymentList.editRequest.miscFee')">
              <SettlementCurrencyAmountInput
                v-model="form.fee.miscFee"
                v-model:currency="form.paymentCurrency"
                :min="0"
                :precision="2"
                :disabled="!canEdit"
              />
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item :label="t('financePaymentList.editRequest.rounding')">
              <SettlementCurrencyAmountInput
                v-model="form.fee.rounding"
                v-model:currency="form.paymentCurrency"
                :min="0"
                :precision="2"
                :disabled="!canEdit"
              />
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item :label="t('financePaymentList.editRequest.feePayer')">
              <el-radio-group v-model="form.fee.intermediateBankFeePayer" :disabled="!canEdit">
                <el-radio label="我方">{{ t('financePaymentList.editRequest.payerUs') }}</el-radio>
                <el-radio label="供应商">{{ t('financePaymentList.editRequest.payerVendor') }}</el-radio>
              </el-radio-group>
            </el-form-item>
          </el-col>
        </el-row>

        <div class="section-title">{{ t('financePaymentList.editRequest.linesSection') }}</div>
        <CrmDataTable :data="form.lines" size="small">
          <el-table-column prop="purchaseOrderCode" :label="t('financePaymentList.editRequest.colPoCode')" width="160" show-overflow-tooltip />
          <CrmCopyableTableColumn prop="pn" :label="t('financePaymentList.editRequest.colPn')" min-width="120" />
          <CrmCopyableTableColumn prop="brand" :label="t('financePaymentList.editRequest.colBrand')" width="100" />
          <el-table-column prop="qty" :label="t('financePaymentList.editRequest.colQty')" width="90" align="right" />
          <el-table-column :label="t('financePaymentList.editRequest.colPending')" width="140" align="right">
            <template #default="{ row }">{{ formatMoney(row.pendingRequested, form.paymentCurrency) }}</template>
          </el-table-column>
          <el-table-column :label="t('financePaymentList.editRequest.colAmount')" min-width="220">
            <template #default="{ row }">
              <SettlementCurrencyAmountInput
                v-model="row.requestAmount"
                v-model:currency="form.paymentCurrency"
                :min="0"
                :max="lineAmountMax(row)"
                :precision="2"
                :disabled="!canEdit"
              />
            </template>
          </el-table-column>
          <el-table-column :label="t('financePaymentList.editRequest.colLineRemark')" min-width="140">
            <template #default="{ row }">
              <el-input v-model="row.lineRemark" :disabled="!canEdit" />
            </template>
          </el-table-column>
        </CrmDataTable>

        <el-alert :closable="false" type="info" style="margin-top: 8px">
          <template #title>
            {{ t('financePaymentList.editRequest.totalAlert', { amount: formatMoney(totalAmount, form.paymentCurrency) }) }}
          </template>
        </el-alert>
      </el-form>
    </template>

    <template #footer>
      <el-button @click="emit('update:modelValue', false)">{{ t('common.cancel') }}</el-button>
      <el-button type="primary" :loading="saving" :disabled="!canEdit" @click="save">
        {{ t('financePaymentList.btnSave') }}
      </el-button>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { computed, reactive, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import { useFinanceEnumLabels } from '@/composables/useFinanceEnumLabels'
import {
  financePaymentApi,
  PAYMENT_MODE_MAP,
  type FinancePayment,
  type UpdateFinancePaymentRequestBody,
} from '@/api/finance'
import { purchaseOrderApi } from '@/api/purchaseOrder'
import { vendorBankApi } from '@/api/vendor'
import type { VendorBankInfo } from '@/types/vendor'
import { usePurchaseSensitiveFieldMask } from '@/composables/usePurchaseSensitiveFieldMask'
import { useFinanceWriteGate, usePurchaseOrderWriteGate } from '@/composables/useDepartmentDataReadOnly'
import { formatCurrencyTotal } from '@/utils/moneyFormat'
import PaymentRequestVendorBankSection from '@/components/Vendor/PaymentRequestVendorBankSection.vue'
import VendorNameReadonlyField from '@/components/Vendor/VendorNameReadonlyField.vue'
import SettlementCurrencyAmountInput from '@/components/SettlementCurrencyAmountInput.vue'

const props = defineProps<{
  modelValue: boolean
  paymentId: string | null
}>()

const emit = defineEmits<{
  'update:modelValue': [value: boolean]
  success: []
}>()

const { t } = useI18n()
const { paymentModeLabel } = useFinanceEnumLabels()
const { maskPurchaseSensitiveFields } = usePurchaseSensitiveFieldMask()
const { canWriteFinancePayment: canFinancePaymentWrite } = useFinanceWriteGate()
const { canWritePo } = usePurchaseOrderWriteGate()

const paymentModeKeys = Object.keys(PAYMENT_MODE_MAP).map((k) => Number(k))
const loading = ref(false)
const saving = ref(false)
const payment = ref<FinancePayment | null>(null)
const vendorBanks = ref<VendorBankInfo[]>([])
const vendorCodeDisplay = ref('')

const form = reactive({
  vendorBankId: '',
  paymentMode: 1,
  paymentCurrency: 1 as number,
  requestRemark: '',
  fee: {
    intermediateBankFee: 0,
    bankCharge: 0,
    freight: 0,
    miscFee: 0,
    rounding: 0,
    intermediateBankFeePayer: '我方',
  },
  lines: [] as Array<{
    id: string
    purchaseOrderCode: string
    pn: string
    brand: string
    qty: number
    pendingRequested: number
    requestAmount: number
    lineRemark: string
  }>,
})

const canEdit = computed(() => {
  if (!payment.value) return false
  const s = payment.value.status
  if (s !== 1 && s !== -1) return false
  return canFinancePaymentWrite.value || canWritePo.value
})

const dialogTitle = computed(() => {
  const code = payment.value?.financePaymentCode
  return code
    ? `${t('financePaymentList.actions.editRequest')} · ${code}`
    : t('financePaymentList.actions.editRequest')
})

const totalAmount = computed(() => {
  const linesTotal = form.lines.reduce((sum, line) => sum + Number(line.requestAmount || 0), 0)
  const fee = form.fee
  const feeTotal =
    Number(fee.intermediateBankFee || 0) +
    Number(fee.bankCharge || 0) +
    Number(fee.freight || 0) +
    Number(fee.miscFee || 0) +
    Number(fee.rounding || 0)
  return Math.max(0, linesTotal + feeTotal)
})

function formatMoney(amount: number, currency: number) {
  return formatCurrencyTotal(amount, currency)
}

function lineAmountMax(row: { pendingRequested?: number; requestAmount?: number }) {
  const pending = Number(row?.pendingRequested ?? 0)
  const current = Number(row?.requestAmount ?? 0)
  const max = pending + current
  return max > 0 ? max : undefined
}

async function loadPayment(id: string) {
  loading.value = true
  payment.value = null
  form.lines = []
  try {
    const detail = await financePaymentApi.getById(id)
    payment.value = detail
    vendorCodeDisplay.value = String(detail.vendorCode ?? '').trim()

    if (detail.vendorId) {
      try {
        vendorBanks.value = await vendorBankApi.getBanksByVendorId(detail.vendorId)
      } catch {
        vendorBanks.value = []
      }
    } else {
      vendorBanks.value = []
    }

    form.vendorBankId = String(detail.vendorBankId ?? '').trim()
    form.paymentMode = Number(detail.paymentMode ?? 1)
    form.paymentCurrency = Number(detail.paymentCurrency ?? 1)
    form.requestRemark = String(detail.requestRemark ?? '')
    form.fee = {
      intermediateBankFee: Number(detail.feeIntermediateBank ?? 0),
      bankCharge: Number(detail.feeBankCharge ?? 0),
      freight: Number(detail.feeFreight ?? 0),
      miscFee: Number(detail.feeMisc ?? 0),
      rounding: Number(detail.feeRounding ?? 0),
      intermediateBankFeePayer:
        detail.feeIntermediateBankPayer === '供应商' ? '供应商' : '我方',
    }

    const items = detail.items ?? []
    const poIds = Array.from(
      new Set(items.map((it) => String(it.purchaseOrderId ?? '').trim()).filter(Boolean))
    )
    const poMap = new Map<string, any>()
    await Promise.all(
      poIds.map(async (poId) => {
        try {
          poMap.set(poId, await purchaseOrderApi.getById(poId))
        } catch {
          poMap.set(poId, null)
        }
      })
    )

    form.lines = items.map((it) => {
      const po = poMap.get(String(it.purchaseOrderId ?? '').trim()) ?? {}
      const poItems: any[] = Array.isArray(po?.items) ? po.items : []
      const matched =
        poItems.find((x) => String(x?.id ?? x?.purchaseOrderItemId ?? '') === String(it.purchaseOrderItemId ?? '')) ??
        {}
      const extend = matched?.extend ?? matched?.Extend ?? {}
      const qty = Number(matched?.qty ?? matched?.Qty ?? 0)
      const cost = Number(matched?.cost ?? matched?.Cost ?? 0)
      const lineTotal = Math.round(qty * cost * 100) / 100
      const requested = Number(
        extend?.paymentAmountRequested ??
          extend?.PaymentAmountRequested ??
          matched?.paymentAmountRequested ??
          0
      )
      const currentAmount = Number(it.paymentAmountToBe ?? 0)
      const pendingRequested = Math.max(0, Math.round((lineTotal - requested + currentAmount) * 100) / 100)
      return {
        id: it.id,
        purchaseOrderCode: po?.purchaseOrderCode ?? po?.PurchaseOrderCode ?? '—',
        pn: it.pn ?? matched?.pn ?? '—',
        brand: it.brand ?? matched?.brand ?? '—',
        qty,
        pendingRequested,
        requestAmount: currentAmount,
        lineRemark: String(it.lineRemark ?? ''),
      }
    })
  } catch (e: any) {
    ElMessage.error(e?.message || t('financePaymentList.messages.loadFailed'))
    emit('update:modelValue', false)
  } finally {
    loading.value = false
  }
}

watch(
  () => [props.modelValue, props.paymentId] as const,
  ([visible, id]) => {
    if (!visible || !id) return
    loadPayment(id)
  }
)

async function save() {
  if (!payment.value || !canEdit.value) return
  if (!form.vendorBankId) {
    ElMessage.warning(t('financePaymentList.editRequest.vendorBankRequired'))
    return
  }
  if (!form.lines.length || form.lines.some((x) => Number(x.requestAmount || 0) <= 0)) {
    ElMessage.warning(t('financePaymentList.editRequest.amountRequired'))
    return
  }

  const payer = form.fee.intermediateBankFeePayer === '供应商' ? '供应商' : '我方'
  const body: UpdateFinancePaymentRequestBody = {
    vendorBankId: form.vendorBankId,
    paymentMode: form.paymentMode,
    paymentCurrency: form.paymentCurrency,
    requestRemark: form.requestRemark?.trim() || null,
    feeIntermediateBank: Number(form.fee.intermediateBankFee || 0),
    feeBankCharge: Number(form.fee.bankCharge || 0),
    feeFreight: Number(form.fee.freight || 0),
    feeMisc: Number(form.fee.miscFee || 0),
    feeRounding: Number(form.fee.rounding || 0),
    feeIntermediateBankPayer: payer,
    items: form.lines.map((line) => ({
      id: line.id,
      paymentAmountToBe: Number(line.requestAmount || 0),
      lineRemark: line.lineRemark?.trim() || null,
    })),
  }

  saving.value = true
  try {
    await financePaymentApi.updateRequest(payment.value.id, body)
    ElMessage.success(t('financePaymentList.messages.saveOk'))
    emit('update:modelValue', false)
    emit('success')
  } catch (e: any) {
    ElMessage.error(e?.message || t('financePaymentList.messages.saveFailed'))
  } finally {
    saving.value = false
  }
}
</script>

<style lang="scss" scoped>
.dialog-loading {
  padding: 8px 0;
}

.whole-payment-alert {
  margin-bottom: 12px;
}

.section-title {
  font-size: 13px;
  font-weight: 600;
  margin: 12px 0 8px;
  color: var(--el-text-color-primary);
}
</style>
