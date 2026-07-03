<template>
  <el-dialog
    :model-value="modelValue"
    :title="t('financePaymentList.dialogPay')"
    width="920px"
    class="crm-dialog"
    destroy-on-close
    @update:model-value="emit('update:modelValue', $event)"
  >
    <el-form :model="form" label-width="100px" class="crm-form pay-dialog-form">
      <div class="pay-dialog-panels">
        <!-- 收款信息（供应商 / 收款银行） -->
        <section class="pay-dialog-panel pay-dialog-panel--receiving">
          <div class="pay-dialog-panel__head">
            <span class="pay-dialog-panel__bar pay-dialog-panel__bar--receiving" aria-hidden="true" />
            <span class="pay-dialog-panel__title">{{ t('financePaymentList.panelReceiving') }}</span>
          </div>
          <el-row :gutter="16">
            <el-col :span="12">
              <el-form-item :label="t('financePaymentList.formVendorId')">
                <el-input
                  v-if="maskPurchaseSensitiveFields"
                  model-value="—"
                  disabled
                />
                <el-input v-else :model-value="editVendorCodeDisplay" readonly />
              </el-form-item>
            </el-col>
            <el-col :span="12">
              <el-form-item :label="t('financePaymentList.formFreightForwarderOrderNo')">
                <el-input :model-value="freightForwarderOrderNoDisplay" readonly />
              </el-form-item>
            </el-col>
            <el-col :span="24">
              <el-form-item :label="t('financePaymentList.formVendorName')">
                <vendor-name-readonly-field
                  :name-zh="form.vendorName"
                  :name-en="form.vendorEnglishName"
                  :masked="maskPurchaseSensitiveFields"
                  mode="compact"
                />
              </el-form-item>
            </el-col>
            <el-col :span="24">
              <el-form-item :label="t('financePaymentList.formVendorReceivingBank')">
                <el-input
                  :model-value="vendorReceivingBankDisplay"
                  readonly
                />
              </el-form-item>
              <vendor-bank-info-panel
                v-if="vendorReceivingBank"
                class="pay-dialog-panel__bank-detail"
                :bank="vendorReceivingBank"
                :masked="maskPurchaseSensitiveFields"
              />
            </el-col>
          </el-row>
        </section>

        <!-- 费用明细（夹在收款信息与付款信息之间） -->
        <section class="pay-dialog-panel pay-dialog-panel--fee">
          <div class="pay-dialog-panel__head">
            <span class="pay-dialog-panel__bar pay-dialog-panel__bar--fee" aria-hidden="true" />
            <span class="pay-dialog-panel__title">{{ t('financePaymentList.editRequest.feeSection') }}</span>
          </div>
          <PaymentFeeSection
            v-model="feeForm"
            :currency="form.paymentCurrency ?? 1"
            :disabled="!canFinancePaymentWrite"
          />
        </section>

        <!-- 付款信息（付款银行 / 付款明细） -->
        <section class="pay-dialog-panel pay-dialog-panel--payment">
          <div class="pay-dialog-panel__head">
            <span class="pay-dialog-panel__bar pay-dialog-panel__bar--payment" aria-hidden="true" />
            <span class="pay-dialog-panel__title">{{ t('financePaymentList.panelPayment') }}</span>
          </div>
          <el-row :gutter="16">
            <el-col :span="24">
              <el-form-item :label="t('financePaymentList.formPaymentBank')">
                <company-bank-select
                  v-model="formCompanyBankId"
                  :placeholder="t('financePaymentList.formPaymentBankPh')"
                  :disabled="maskPurchaseSensitiveFields || !canFinancePaymentWrite"
                  :masked="maskPurchaseSensitiveFields"
                  clearable
                />
              </el-form-item>
              <company-bank-info-panel
                v-if="selectedCompanyBank"
                class="pay-dialog-panel__bank-detail"
                :bank="selectedCompanyBank"
                :masked="maskPurchaseSensitiveFields"
              />
            </el-col>
            <el-col :span="12">
              <el-form-item :label="t('financePaymentList.formAmount')">
                <el-input-number
                  :model-value="displayPaymentAmount"
                  :precision="2"
                  :min="0"
                  disabled
                  style="width:100%"
                />
              </el-form-item>
            </el-col>
            <el-col :span="12">
              <el-form-item :label="t('financePaymentList.formMode')">
                <el-select :model-value="form.paymentMode" disabled style="width:100%">
                  <el-option
                    v-for="k in paymentModeKeys"
                    :key="k"
                    :label="paymentModeLabel(k)"
                    :value="k"
                  />
                </el-select>
              </el-form-item>
            </el-col>
            <el-col :span="12">
              <el-form-item :label="t('financePaymentList.formCurrency')">
                <el-select :model-value="form.paymentCurrency" disabled style="width:100%">
                  <el-option
                    v-for="opt in SETTLEMENT_CURRENCY_OPTIONS"
                    :key="opt.value"
                    :label="opt.label"
                    :value="opt.value"
                  />
                </el-select>
              </el-form-item>
            </el-col>
            <el-col :span="12">
              <el-form-item :label="t('financePaymentList.formDate')">
                <el-date-picker v-model="form.paymentDate" type="date" value-format="YYYY-MM-DD" style="width:100%" />
              </el-form-item>
            </el-col>
            <el-col :span="12">
              <el-form-item :label="t('financePaymentList.formBankSlip')">
                <el-input v-model="form.bankSlipNo" :placeholder="t('financePaymentList.formBankSlipPh')" />
              </el-form-item>
            </el-col>
            <el-col :span="24">
              <el-form-item :label="t('financePaymentList.formRemark')">
                <el-input v-model="form.remark" type="textarea" :rows="2" :placeholder="t('financePaymentList.formRemarkPh')" />
              </el-form-item>
            </el-col>
          </el-row>
        </section>
      </div>

      <el-row :gutter="16" class="pay-dialog-slip-row">
        <el-col v-if="paymentId && maskPurchaseSensitiveFields" :span="24">
          <el-form-item :label="t('financePaymentList.formSlipAttach')">
            <el-alert type="info" :closable="false" show-icon :title="t('common.crossSideAttachmentsRestricted')" />
          </el-form-item>
        </el-col>
        <el-col v-if="paymentId && !maskPurchaseSensitiveFields" :span="24">
          <el-form-item :label="t('financePaymentList.formSlipAttach')">
            <div class="slip-attach-wrap">
              <div class="slip-upload-row">
                <el-button size="small" type="primary" plain @click="triggerSlipFilePick">
                  {{ t('financePaymentList.slipSelectFile') }}
                </el-button>
                <span v-if="uploadingSlipDocs" class="slip-upload-hint">{{ t('financePaymentList.uploadingSlip') }}</span>
                <span v-else-if="paymentDocs.length" class="slip-upload-hint slip-upload-hint--ok">
                  {{ t('financePaymentList.slipHasUploadsHint') }}
                </span>
                <span v-else class="slip-upload-hint slip-upload-hint--muted">
                  {{ t('financePaymentList.slipPickHint') }}
                </span>
                <input
                  ref="slipFileInputRef"
                  type="file"
                  multiple
                  class="slip-file-input-hidden"
                  @change="onSlipFilesSelected"
                />
              </div>
              <div v-if="paymentDocs.length" class="slip-doc-tags">
                <el-tag
                  v-for="doc in paymentDocs"
                  :key="doc.id"
                  size="small"
                  class="slip-doc-tag"
                  @click="downloadSlipDoc(doc)"
                >
                  {{ doc.originalFileName }}
                </el-tag>
              </div>
              <div v-else-if="!uploadingSlipDocs" class="slip-no-docs">{{ t('financePaymentList.noSlipUploaded') }}</div>
            </div>
          </el-form-item>
        </el-col>
      </el-row>
    </el-form>
    <template #footer>
      <el-button @click="emit('update:modelValue', false)">{{ t('common.cancel') }}</el-button>
      <el-button
        v-if="paymentId && canFinancePaymentWrite && canShowFinishButton"
        type="success"
        @click="completePayment"
        :loading="completingPayment"
      >
        {{ t('financePaymentList.btnPaymentDone') }}
      </el-button>
      <el-button type="primary" @click="saveForm" :loading="saving" :disabled="!canFinancePaymentWrite">
        {{ t('financePaymentList.btnSave') }}
      </el-button>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { computed, ref, reactive, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useFinanceEnumLabels } from '@/composables/useFinanceEnumLabels'
import { ElMessage, ElMessageBox } from 'element-plus'
import { documentApi, type UploadDocumentDto } from '@/api/document'
import {
  financePaymentApi,
  PAYMENT_MODE_MAP,
  type FinancePayment,
} from '@/api/finance'
import { vendorApi, vendorBankApi } from '@/api/vendor'
import type { VendorBankInfo } from '@/types/vendor'
import { SETTLEMENT_CURRENCY_OPTIONS } from '@/constants/currency'
import { usePurchaseSensitiveFieldMask } from '@/composables/usePurchaseSensitiveFieldMask'
import { useFinanceWriteGate } from '@/composables/useDepartmentDataReadOnly'
import { useFinancePaymentBankOptions } from '@/composables/useFinancePaymentBankOptions'
import { useCompanyBankOptions } from '@/composables/useCompanyBankOptions'
import { formatVendorBankOptionLabel } from '@/utils/vendorFinancePaymentBank'
import VendorBankInfoPanel from '@/components/Vendor/VendorBankInfoPanel.vue'
import VendorNameReadonlyField from '@/components/Vendor/VendorNameReadonlyField.vue'
import CompanyBankSelect from '@/components/Company/CompanyBankSelect.vue'
import CompanyBankInfoPanel from '@/components/Company/CompanyBankInfoPanel.vue'
import PaymentFeeSection, { type PaymentFeeForm } from '@/components/Finance/PaymentFeeSection.vue'

const props = defineProps<{
  modelValue: boolean
  payment: FinancePayment | null
}>()

const emit = defineEmits<{
  'update:modelValue': [value: boolean]
  success: []
}>()

const { t } = useI18n()
const { paymentModeLabel } = useFinanceEnumLabels()
const { maskPurchaseSensitiveFields } = usePurchaseSensitiveFieldMask()
const { canWriteFinancePayment: canFinancePaymentWrite } = useFinanceWriteGate()
const { paymentBankOptions, loadPaymentBankOptions } = useFinancePaymentBankOptions()
const { companyBankRows, loadCompanyBankOptions } = useCompanyBankOptions()

const paymentModeKeys = Object.keys(PAYMENT_MODE_MAP).map(k => Number(k))
const vendorReceivingBank = ref<VendorBankInfo | null>(null)

const saving = ref(false)
const completingPayment = ref(false)
const paymentDocs = ref<UploadDocumentDto[]>([])
const uploadingSlipDocs = ref(false)
const slipFileInputRef = ref<HTMLInputElement | null>(null)
const paymentId = ref<string | null>(null)
const linesTotal = ref(0)

const feeForm = reactive<PaymentFeeForm>({
  intermediateBankFee: 0,
  bankCharge: 0,
  freight: 0,
  miscFee: 0,
  rounding: 0,
  intermediateBankFeePayer: '我方',
})

const form = reactive<Partial<FinancePayment>>({
  vendorId: '', vendorCode: '', vendorName: '', vendorEnglishName: '', paymentAmount: 0, paymentMode: 1, paymentCurrency: 1,
  paymentDate: undefined, bankSlipNo: '', remark: '',
  companyBankId: null,
  vendorBankId: null,
})

const formCompanyBankId = computed({
  get: () => String(form.companyBankId || '').trim(),
  set: (v: string) => {
    form.companyBankId = v?.trim() || null
  }
})

const selectedCompanyBank = computed(() => {
  const id = formCompanyBankId.value
  if (!id) return null
  return companyBankRows.value.find((b) => b.id === id) ?? null
})

const vendorReceivingBankDisplay = computed(() => {
  if (maskPurchaseSensitiveFields.value) return '—'
  if (!vendorReceivingBank.value) return '—'
  return formatVendorBankOptionLabel(vendorReceivingBank.value, paymentBankOptions.value)
})

const editVendorCodeDisplay = computed(() => {
  const c = (form.vendorCode || '').trim()
  return c || '—'
})

const freightForwarderOrderNoDisplay = computed(() => {
  if (maskPurchaseSensitiveFields.value) return '—'
  const ext = form as unknown as Record<string, unknown>
  const raw = form.freightForwarderOrderNo ?? ext.FreightForwarderOrderNo ?? ext.freightForwarderOrderNo
  const s = raw != null ? String(raw).trim() : ''
  return s || '—'
})

const canShowFinishButton = computed(() => Number(form.status) === 10)

const feeTotal = computed(() =>
  Number(feeForm.intermediateBankFee ?? 0) +
  Number(feeForm.bankCharge ?? 0) +
  Number(feeForm.freight ?? 0) +
  Number(feeForm.miscFee ?? 0) +
  Number(feeForm.rounding ?? 0)
)

const displayPaymentAmount = computed(() => {
  const total = linesTotal.value + feeTotal.value
  return Math.round(total * 100) / 100
})

function triggerSlipFilePick() {
  slipFileInputRef.value?.click()
}

async function initFromPayment(row: FinancePayment) {
  paymentId.value = row.id
  let detail = row
  try {
    detail = await financePaymentApi.getById(row.id)
  } catch {
    /* 列表行数据兜底 */
  }

  const amountForEdit =
    detail.status === 100
      ? Number(detail.paymentAmount ?? detail.paymentAmountToBe ?? 0)
      : Number(detail.paymentAmountToBe ?? detail.paymentAmount ?? 0)
  Object.assign(form, { ...detail, paymentAmount: amountForEdit, paymentAmountToBe: detail.paymentAmountToBe ?? amountForEdit })

  linesTotal.value = (detail.items ?? []).reduce(
    (sum, item) => sum + Number(item.paymentAmountToBe ?? 0),
    0
  )
  Object.assign(feeForm, {
    intermediateBankFee: Number(detail.feeIntermediateBank ?? 0),
    bankCharge: Number(detail.feeBankCharge ?? 0),
    freight: Number(detail.feeFreight ?? 0),
    miscFee: Number(detail.feeMisc ?? 0),
    rounding: Number(detail.feeRounding ?? 0),
    intermediateBankFeePayer:
      detail.feeIntermediateBankPayer === '供应商' ? '供应商' : '我方',
  })

  const ext = detail as unknown as Record<string, unknown>
  let code = String(detail.vendorCode ?? ext.VendorCode ?? '').trim()
  if (!code && detail.vendorId) {
    try {
      const v = await vendorApi.getVendorById(detail.vendorId)
      code = (v.code || '').trim()
    } catch {
      /* 尽力补全供应商编码 */
    }
  }
  form.vendorCode = code
  const companyBankId = String(detail.companyBankId ?? ext.CompanyBankId ?? '').trim()
  form.companyBankId = companyBankId || null

  await Promise.all([
    loadPaymentDocs(detail.id),
    loadVendorReceivingBank(detail.vendorId, detail.vendorBankId),
    loadPaymentBankOptions(),
    loadCompanyBankOptions()
  ])

  if (!form.companyBankId && !maskPurchaseSensitiveFields.value) {
    const legacyName = String(detail.paymentBankName ?? ext.PaymentBankName ?? '').trim()
    if (legacyName) {
      const matched = companyBankRows.value.find((b) => b.bankName?.trim() === legacyName)
      if (matched) form.companyBankId = matched.id
    }
  }
}

async function loadVendorReceivingBank(vendorId?: string | null, vendorBankId?: string | null) {
  vendorReceivingBank.value = null
  if (maskPurchaseSensitiveFields.value) return
  const vid = String(vendorId || '').trim()
  const bankId = String(vendorBankId || '').trim()
  if (!vid || !bankId) return
  try {
    const banks = await vendorBankApi.getBanksByVendorId(vid)
    vendorReceivingBank.value = banks.find((b) => b.id === bankId) ?? null
  } catch {
    vendorReceivingBank.value = null
  }
}

watch(
  () => [props.modelValue, props.payment] as const,
  async ([visible, payment]) => {
    if (!visible || !payment) return
    await initFromPayment(payment)
  }
)

const saveForm = async () => {
  if (!paymentId.value) return
  saving.value = true
  try {
    const payer = feeForm.intermediateBankFeePayer === '供应商' ? '供应商' : '我方'
    await financePaymentApi.updateExecution(paymentId.value, {
      companyBankId: form.companyBankId ?? null,
      paymentDate: form.paymentDate,
      bankSlipNo: form.bankSlipNo,
      feeIntermediateBank: Number(feeForm.intermediateBankFee || 0),
      feeBankCharge: Number(feeForm.bankCharge || 0),
      feeFreight: Number(feeForm.freight || 0),
      feeMisc: Number(feeForm.miscFee || 0),
      feeRounding: Number(feeForm.rounding || 0),
      feeIntermediateBankPayer: payer,
    })
    ElMessage.success(t('financePaymentList.messages.saveOk'))
    emit('update:modelValue', false)
    emit('success')
  } catch (e: any) {
    ElMessage.error(e?.message || t('financePaymentList.messages.saveFailed'))
  } finally {
    saving.value = false
  }
}

const loadPaymentDocs = async (id: string) => {
  if (maskPurchaseSensitiveFields.value) {
    paymentDocs.value = []
    return
  }
  try {
    paymentDocs.value = await documentApi.getDocuments('FINANCE_PAYMENT', id)
  } catch {
    paymentDocs.value = []
  }
}

const onSlipFilesSelected = async (e: Event) => {
  if (maskPurchaseSensitiveFields.value) {
    ;(e.target as HTMLInputElement).value = ''
    return
  }
  const id = paymentId.value
  if (!id) {
    ElMessage.warning(t('financePaymentList.messages.saveSlipFirst'))
    ;(e.target as HTMLInputElement).value = ''
    return
  }
  const files = Array.from((e.target as HTMLInputElement).files || [])
  if (!files.length) return
  try {
    uploadingSlipDocs.value = true
    await documentApi.uploadDocuments('FINANCE_PAYMENT', id, files, t('financePaymentList.slipUploadCategory'))
    await loadPaymentDocs(id)
    ElMessage.success(t('financePaymentList.messages.slipUploadOk'))
  } catch (err: any) {
    ElMessage.error(err?.message || t('financePaymentList.messages.slipUploadFail'))
  } finally {
    uploadingSlipDocs.value = false
    ;(e.target as HTMLInputElement).value = ''
  }
}

const downloadSlipDoc = async (doc: UploadDocumentDto) => {
  await documentApi.downloadDocument(doc.id, doc.originalFileName)
}

const completePayment = async () => {
  if (!paymentId.value) return
  const code = form.financePaymentCode || paymentId.value
  try {
    await ElMessageBox.confirm(
      t('financePaymentList.messages.completeMsg', { code: String(code) }),
      t('financePaymentList.messages.completeTitle'),
      { type: 'success' }
    )
  } catch {
    return
  }
  completingPayment.value = true
  try {
    await financePaymentApi.complete(paymentId.value)
    ElMessage.success(t('financePaymentList.messages.completed'))
    emit('update:modelValue', false)
    emit('success')
  } catch (e: any) {
    ElMessage.error(e?.message || t('financePaymentList.messages.completeFailed'))
  } finally {
    completingPayment.value = false
  }
}
</script>

<style lang="scss" scoped>
@import '@/assets/styles/variables.scss';

.pay-dialog-form {
  :deep(.el-form-item:last-child) {
    margin-bottom: 18px;
  }
}

.pay-dialog-panels {
  display: flex;
  flex-direction: column;
  gap: 14px;
  margin-bottom: 4px;
}

.pay-dialog-panel {
  border-radius: $border-radius-md;
  border: 1px solid $border-card;
  padding: 12px 14px 2px;
}

.pay-dialog-panel--receiving {
  background: color-mix(in srgb, var(--el-color-warning-light-9) 72%, var(--el-bg-color));
  border-color: color-mix(in srgb, var(--el-color-warning-light-5) 28%, $border-card);
}

.pay-dialog-panel--fee {
  background: color-mix(in srgb, var(--el-color-info-light-9) 72%, var(--el-bg-color));
  border-color: color-mix(in srgb, var(--el-color-info-light-5) 28%, $border-card);
}

.pay-dialog-panel--payment {
  background: color-mix(in srgb, var(--el-color-success-light-9) 72%, var(--el-bg-color));
  border-color: color-mix(in srgb, var(--el-color-success-light-5) 28%, $border-card);
}

.pay-dialog-panel__head {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-bottom: 10px;
}

.pay-dialog-panel__bar {
  width: 4px;
  height: 14px;
  border-radius: 2px;
  flex-shrink: 0;
}

.pay-dialog-panel__bar--receiving {
  background: var(--el-color-warning);
}

.pay-dialog-panel__bar--fee {
  background: var(--el-color-info);
}

.pay-dialog-panel__bar--payment {
  background: var(--el-color-success);
}

.pay-dialog-panel__title {
  font-size: 13px;
  font-weight: 600;
  color: $text-primary;
}

.pay-dialog-panel__bank-detail {
  margin-bottom: 8px;
}

.pay-dialog-slip-row {
  margin-top: 4px;
}

.slip-attach-wrap {
  display: flex;
  flex-direction: column;
  gap: 10px;
  width: 100%;
}

.slip-upload-row {
  position: relative;
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 10px;
}

.slip-upload-hint {
  font-size: 13px;
  line-height: 1.4;
}

.slip-upload-hint--ok {
  color: var(--el-color-success);
}

.slip-upload-hint--muted {
  color: var(--el-text-color-secondary);
}

.slip-file-input-hidden {
  position: absolute;
  left: 0;
  top: 0;
  width: 0;
  height: 0;
  opacity: 0;
  overflow: hidden;
}

.slip-doc-tags {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}

.slip-doc-tag {
  cursor: pointer;
}

.slip-no-docs {
  font-size: 13px;
  color: var(--el-text-color-placeholder);
}
</style>
