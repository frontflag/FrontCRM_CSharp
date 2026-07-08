<template>
  <el-dialog
    :model-value="modelValue"
    :title="t('financeFfPayableList.payDialogTitle')"
    width="720px"
    class="crm-dialog"
    destroy-on-close
    @update:model-value="emit('update:modelValue', $event)"
  >
    <el-form :model="form" label-width="110px" class="crm-form">
      <el-form-item :label="t('financeFfPayableList.colFfCompany')" required>
        <el-select
          v-model="form.freightForwarderCompanyId"
          filterable
          clearable
          style="width:100%"
          :placeholder="t('financeFfPayableList.selectFfCompany')"
          @change="onCompanyChange"
        >
          <el-option v-for="c in ffCompanies" :key="c.id" :label="c.cname" :value="c.id" />
        </el-select>
      </el-form-item>
      <el-form-item :label="t('financeFfPayableDetail.colFfBank')">
        <el-select
          v-model="form.ffCompanyBankId"
          clearable
          style="width:100%"
          :placeholder="t('financeFfPayableList.selectFfBank')"
          :disabled="!form.freightForwarderCompanyId"
        >
          <el-option
            v-for="b in ffBanks"
            :key="b.id"
            :label="`${b.bankName}${b.accountNo ? ' / ' + b.accountNo : ''}`"
            :value="b.id"
            :disabled="b.isDisabled"
          />
        </el-select>
      </el-form-item>
      <el-form-item :label="t('financePaymentList.formPaymentBank')">
        <company-bank-select v-model="form.companyBankId" style="width:100%" clearable />
      </el-form-item>
      <el-form-item :label="t('financeFfPayableList.colPendingAmount')">
        <el-input :model-value="formatAmount(pendingAmount)" disabled />
      </el-form-item>
      <el-form-item :label="t('financeFfPayableList.payAmount')" required>
        <el-input-number
          v-model="form.paymentAmount"
          :min="0.01"
          :max="pendingAmount"
          :precision="2"
          style="width:100%"
        />
      </el-form-item>
      <el-form-item :label="t('financePaymentList.formMode')">
        <el-select v-model="form.paymentMode" style="width:100%">
          <el-option
            v-for="k in paymentModeKeys"
            :key="k"
            :label="paymentModeLabel(k)"
            :value="k"
          />
        </el-select>
      </el-form-item>
      <el-form-item :label="t('financePaymentList.formDate')">
        <el-date-picker v-model="form.paymentDate" type="date" value-format="YYYY-MM-DD" style="width:100%" />
      </el-form-item>
      <el-form-item :label="t('financePaymentList.formBankSlip')">
        <el-input v-model="form.bankSlipNo" />
      </el-form-item>
      <el-form-item :label="t('financePaymentList.formRemark')">
        <el-input v-model="form.remark" type="textarea" :rows="2" />
      </el-form-item>
      <el-form-item :label="t('financeReceiptList.formSlipAttach')">
        <el-button size="small" type="primary" plain @click="triggerFilePick">{{ t('financeReceiptList.slipSelectFile') }}</el-button>
        <input ref="fileInputRef" type="file" multiple class="slip-file-input-hidden" @change="onFilesSelected" />
        <div v-if="pendingFiles.length" class="slip-doc-tags">
          <el-tag v-for="(f, idx) in pendingFiles" :key="idx" size="small" closable @close="pendingFiles.splice(idx, 1)">{{ f.name }}</el-tag>
        </div>
      </el-form-item>
    </el-form>
    <template #footer>
      <el-button @click="emit('update:modelValue', false)">{{ t('common.cancel') }}</el-button>
      <el-button type="primary" :loading="saving" @click="save">{{ t('common.confirm') }}</el-button>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { ref, reactive, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import { financeFreightForwarderPayableApi } from '@/api/financeFreightForwarderPayable'
import { PAYMENT_MODE_MAP } from '@/api/finance'
import { useFinanceEnumLabels } from '@/composables/useFinanceEnumLabels'
import { fetchFreightForwarderCompanies, fetchFfCompanyBanks, type FreightForwarderCompany, type FreightForwarderCompanyBank } from '@/api/freightForwarderCompany'
import { documentApi } from '@/api/document'

const FF_PAYMENT_DOC_BIZ = 'FINANCE_FF_PAYMENT'

const props = defineProps<{
  modelValue: boolean
  receiptId: string
  pendingAmount: number
  receiptCurrency: number
  freightForwarderCompanyId?: string
}>()

const emit = defineEmits<{
  'update:modelValue': [boolean]
  success: []
}>()

const { t } = useI18n()
const { paymentModeLabel } = useFinanceEnumLabels()
const paymentModeKeys = Object.keys(PAYMENT_MODE_MAP).map((k) => Number(k))
const saving = ref(false)
const ffCompanies = ref<FreightForwarderCompany[]>([])
const ffBanks = ref<FreightForwarderCompanyBank[]>([])
const pendingFiles = ref<File[]>([])
const fileInputRef = ref<HTMLInputElement | null>(null)

const form = reactive({
  freightForwarderCompanyId: '',
  ffCompanyBankId: '',
  companyBankId: '',
  paymentAmount: 0,
  paymentMode: 1,
  paymentDate: '',
  bankSlipNo: '',
  remark: ''
})

function formatAmount(v: number) {
  return Number(v ?? 0).toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

function resetForm() {
  form.freightForwarderCompanyId = props.freightForwarderCompanyId || ''
  form.ffCompanyBankId = ''
  form.companyBankId = ''
  form.paymentAmount = props.pendingAmount > 0 ? props.pendingAmount : 0
  form.paymentMode = 1
  form.paymentDate = ''
  form.bankSlipNo = ''
  form.remark = ''
  pendingFiles.value = []
  ffBanks.value = []
}

async function loadCompanies() {
  ffCompanies.value = await fetchFreightForwarderCompanies(true)
}

async function onCompanyChange(companyId: string) {
  form.ffCompanyBankId = ''
  ffBanks.value = companyId ? await fetchFfCompanyBanks(companyId) : []
  const def = ffBanks.value.find(b => b.isDefault && !b.isDisabled)
  if (def) form.ffCompanyBankId = def.id
}

function triggerFilePick() {
  fileInputRef.value?.click()
}

function onFilesSelected(e: Event) {
  const input = e.target as HTMLInputElement
  const files = Array.from(input.files || [])
  input.value = ''
  for (const f of files) pendingFiles.value.push(f)
}

async function save() {
  if (!props.receiptId) return
  if (!form.freightForwarderCompanyId) {
    ElMessage.warning(t('financeFfPayableList.selectFfCompany'))
    return
  }
  if (!form.paymentAmount || form.paymentAmount <= 0) {
    ElMessage.warning(t('financeFfPayableList.payAmountRequired'))
    return
  }
  saving.value = true
  try {
    const payment = await financeFreightForwarderPayableApi.createPayment(props.receiptId, {
      freightForwarderCompanyId: form.freightForwarderCompanyId,
      paymentAmount: form.paymentAmount,
      paymentCurrency: props.receiptCurrency,
      paymentMode: form.paymentMode,
      companyBankId: form.companyBankId || undefined,
      ffCompanyBankId: form.ffCompanyBankId || undefined,
      bankSlipNo: form.bankSlipNo || undefined,
      paymentDate: form.paymentDate || undefined,
      remark: form.remark || undefined
    })
    if (pendingFiles.value.length && payment?.id) {
      await documentApi.uploadDocuments(FF_PAYMENT_DOC_BIZ, payment.id, pendingFiles.value, t('financeReceiptList.slipUploadCategory'))
    }
    ElMessage.success(t('financeFfPayableList.paySaved'))
    emit('success')
    emit('update:modelValue', false)
  } catch (err: unknown) {
    const msg = err && typeof err === 'object' && 'message' in err ? String((err as { message?: string }).message) : ''
    ElMessage.error(msg || t('financeFfPayableList.payFailed'))
  } finally {
    saving.value = false
  }
}

watch(() => props.modelValue, async (open) => {
  if (!open) return
  resetForm()
  await loadCompanies()
  if (form.freightForwarderCompanyId) await onCompanyChange(form.freightForwarderCompanyId)
})
</script>

<style scoped>
.slip-file-input-hidden { display: none; }
.slip-doc-tags { margin-top: 8px; display: flex; flex-wrap: wrap; gap: 6px; }
</style>
