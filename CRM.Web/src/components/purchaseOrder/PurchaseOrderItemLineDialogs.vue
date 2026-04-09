<template>
  <div>
    <el-dialog
      v-model="paymentDialogVisible"
      title="申请付款窗口"
      width="min(96vw, 1440px)"
      destroy-on-close
      class="payment-dialog"
    >
      <el-form label-width="120px">
        <el-row :gutter="12">
          <el-col :span="12">
            <el-form-item label="供应商信息">
              <el-input :model-value="paymentForm.vendorName || '--'" disabled />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="采购员">
              <el-input :model-value="paymentForm.purchaseUserName || '--'" disabled />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="12">
          <el-col :span="12">
            <el-form-item label="供应商银行" required>
              <el-select v-model="paymentForm.vendorBankId" placeholder="请选择供应商银行" style="width: 100%">
                <el-option label="中国银行" value="bank-boc" />
                <el-option label="工商银行" value="bank-icbc" />
                <el-option label="建设银行" value="bank-ccb" />
                <el-option label="农业银行" value="bank-abc" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="请款方式" required>
              <el-select v-model="paymentForm.paymentMode" style="width: 100%">
                <el-option label="银行转账" :value="1" />
                <el-option label="现金" :value="2" />
                <el-option label="支票" :value="3" />
                <el-option label="承兑汇票" :value="4" />
              </el-select>
            </el-form-item>
          </el-col>
        </el-row>
        <el-form-item label="请款备注">
          <el-input v-model="paymentForm.remark" type="textarea" :rows="2" />
        </el-form-item>
        <div class="section-title">费用明细</div>
        <el-row :gutter="12">
          <el-col :span="8">
            <el-form-item label="中转行费用">
              <SettlementCurrencyAmountInput
                v-model="paymentForm.fee.intermediateBankFee"
                v-model:currency="paymentForm.currency"
                :min="0"
                :precision="2"
              />
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="银行手续费">
              <SettlementCurrencyAmountInput
                v-model="paymentForm.fee.bankCharge"
                v-model:currency="paymentForm.currency"
                :min="0"
                :precision="2"
              />
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="运费">
              <SettlementCurrencyAmountInput
                v-model="paymentForm.fee.freight"
                v-model:currency="paymentForm.currency"
                :min="0"
                :precision="2"
              />
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="杂费">
              <SettlementCurrencyAmountInput
                v-model="paymentForm.fee.miscFee"
                v-model:currency="paymentForm.currency"
                :min="0"
                :precision="2"
              />
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="尾差">
              <SettlementCurrencyAmountInput
                v-model="paymentForm.fee.rounding"
                v-model:currency="paymentForm.currency"
                :precision="2"
              />
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="费用承担方">
              <el-radio-group v-model="paymentForm.fee.intermediateBankFeePayer">
                <el-radio label="我方">我方</el-radio>
                <el-radio label="供应商">供应商</el-radio>
              </el-radio-group>
            </el-form-item>
          </el-col>
        </el-row>
        <div class="section-title">订单明细列表</div>
        <CrmDataTable :data="paymentForm.lines" size="small">
          <el-table-column prop="purchaseOrderCode" label="采购单号" width="160" min-width="160" show-overflow-tooltip />
          <el-table-column prop="pn" label="型号" min-width="120" />
          <el-table-column prop="brand" label="品牌" width="100" />
          <el-table-column prop="qty" label="数量" width="90" align="right" />
          <el-table-column prop="cost" label="单价" width="160" align="right">
            <template #default="{ row }">{{ formatCurrencyUnitPrice(row.cost, row.currency) }}</template>
          </el-table-column>
          <el-table-column prop="alreadyRequested" label="已请款" width="160" align="right">
            <template #default="{ row }">{{ formatCurrencyTotal(row.alreadyRequested, row.currency) }}</template>
          </el-table-column>
          <el-table-column prop="pendingRequested" label="待请款" width="160" align="right">
            <template #default="{ row }">{{ formatCurrencyTotal(row.pendingRequested, row.currency) }}</template>
          </el-table-column>
          <el-table-column label="本次请款金额*" min-width="220" width="220">
            <template #default="{ row }">
              <SettlementCurrencyAmountInput
                v-model="row.requestAmount"
                v-model:currency="paymentForm.currency"
                :min="0"
                :max="paymentRequestAmountMax(row)"
                :precision="2"
              />
            </template>
          </el-table-column>
          <el-table-column label="备注" min-width="140">
            <template #default="{ row }">
              <el-input v-model="row.remark" />
            </template>
          </el-table-column>
        </CrmDataTable>
        <el-alert :closable="false" type="info" style="margin-top: 8px">
          <template #title>
            合计：请款总额 {{ formatCurrencyTotal(paymentTotalAmount, paymentForm.currency) }}
          </template>
        </el-alert>
      </el-form>
      <template #footer>
        <el-button @click="paymentDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="paymentSubmitting" @click="submitPayment">提交审批</el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="arrivalDialogVisible" title="新建到货通知" width="1180px" align-center destroy-on-close>
      <div class="arrival-form-layout">
        <div class="arrival-section">
          <el-form label-width="120px" class="arrival-notice-form">
            <el-row :gutter="12">
              <el-col :span="8"><el-form-item label="单号"><el-input v-model="arrivalForm.purchaseOrderCode" /></el-form-item></el-col>
              <el-col :span="8">
                <el-form-item label="预计到货日期" required>
                  <el-date-picker
                    v-model="arrivalForm.expectedArrivalDate"
                    type="date"
                    value-format="YYYY-MM-DD"
                    placeholder="选择预计到货日期"
                    style="width: 100%"
                  />
                </el-form-item>
              </el-col>
              <el-col :span="8"><el-form-item label="公司名称"><el-input v-model="arrivalForm.companyName" /></el-form-item></el-col>
            </el-row>
            <el-row :gutter="12">
              <el-col :span="8"><el-form-item label="地址"><el-input v-model="arrivalForm.address" /></el-form-item></el-col>
              <el-col :span="8"><el-form-item label="电话"><el-input v-model="arrivalForm.phone" /></el-form-item></el-col>
              <el-col :span="8"><el-form-item label="联系人"><el-input v-model="arrivalForm.contact" /></el-form-item></el-col>
            </el-row>
            <el-row :gutter="12">
              <el-col :span="8">
                <el-form-item label="来货方式">
                  <el-select
                    v-model="arrivalForm.arrivalMethod"
                    clearable
                    filterable
                    placeholder="请选择"
                    style="width: 100%"
                  >
                    <el-option
                      v-for="o in arrivalMethodDictOptions"
                      :key="o.value"
                      :label="o.label"
                      :value="o.value"
                    />
                  </el-select>
                </el-form-item>
              </el-col>
              <el-col :span="8">
                <el-form-item label="快递方式">
                  <el-select
                    v-model="arrivalForm.expressMethod"
                    clearable
                    filterable
                    placeholder="请选择"
                    style="width: 100%"
                  >
                    <el-option
                      v-for="o in expressMethodDictOptions"
                      :key="o.value"
                      :label="o.label"
                      :value="o.value"
                    />
                  </el-select>
                </el-form-item>
              </el-col>
              <el-col :span="8"><el-form-item label="快递单号"><el-input v-model="arrivalForm.expressNo" /></el-form-item></el-col>
            </el-row>
          </el-form>
        </div>
        <div class="arrival-section">
          <div class="section-title">来货明细</div>
          <CrmDataTable :data="arrivalForm.lines" size="small">
            <el-table-column label="序号" width="70">
              <template #default="{ $index }">{{ $index + 1 }}</template>
            </el-table-column>
            <el-table-column label="原厂型号" min-width="180">
              <template #default="{ row }"><el-input v-model="row.pn" /></template>
            </el-table-column>
            <el-table-column label="品牌" width="120">
              <template #default="{ row }"><el-input v-model="row.brand" /></template>
            </el-table-column>
            <el-table-column label="数量" min-width="168" align="right">
              <template #default="{ row }">
                <el-input-number
                  v-model="row.qty"
                  :min="0"
                  :precision="0"
                  :step="1"
                  class="arrival-qty-input"
                  controls-position="right"
                />
              </template>
            </el-table-column>
            <el-table-column label="规格参数" min-width="130">
              <template #default="{ row }"><el-input v-model="row.spec" /></template>
            </el-table-column>
            <el-table-column label="包装" width="120">
              <template #default="{ row }"><el-input v-model="row.packaging" /></template>
            </el-table-column>
          </CrmDataTable>
        </div>
        <div class="arrival-section">
          <el-form label-width="120px" class="arrival-notice-form">
            <el-form-item label="验货要求"><el-input v-model="arrivalForm.inspectionRequirement" /></el-form-item>
            <el-form-item label="备注"><el-input v-model="arrivalForm.remark" type="textarea" :rows="2" /></el-form-item>
          </el-form>
        </div>
      </div>
      <template #footer>
        <el-button @click="arrivalDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="arrivalSubmitting" @click="submitArrivalNotice">确定</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { computed, reactive, ref } from 'vue'
import { ElMessage } from 'element-plus'
import CrmDataTable from '@/components/CrmDataTable.vue'
import SettlementCurrencyAmountInput from '@/components/SettlementCurrencyAmountInput.vue'
import { formatDisplayDate } from '@/utils/displayDateTime'
import { formatCurrencyTotal, formatCurrencyUnitPrice } from '@/utils/moneyFormat'
import { financePaymentApi } from '@/api/finance'
import { logisticsApi } from '@/api/logistics'
import { useLogisticsFormDict } from '@/composables/useLogisticsFormDict'

const emit = defineEmits<{ success: [] }>()

const { ensureLoaded: ensureLogisticsDict, arrivalOptions: arrivalMethodDictOptions, expressOptions: expressMethodDictOptions } =
  useLogisticsFormDict()

const paymentDialogVisible = ref(false)
const paymentSubmitting = ref(false)
const arrivalDialogVisible = ref(false)
const arrivalSubmitting = ref(false)

const paymentForm = reactive<any>({
  vendorId: '',
  vendorName: '',
  purchaseUserName: '',
  vendorBankId: '',
  paymentMode: 1,
  currency: 1,
  remark: '',
  fee: {
    intermediateBankFee: 0,
    bankCharge: 0,
    freight: 0,
    miscFee: 0,
    rounding: 0,
    intermediateBankFeePayer: '我方'
  },
  lines: [] as any[]
})

const arrivalForm = reactive<any>({
  purchaseOrderItemId: '',
  purchaseOrderId: '',
  purchaseOrderCode: '',
  vendorName: '',
  pn: '',
  expectedArrivalDate: '' as string,
  companyName: '',
  address: '',
  phone: '',
  contact: '',
  arrivalMethod: '',
  expressMethod: '',
  expressNo: '',
  inspectionRequirement: '',
  remark: '',
  lines: [] as any[]
})

const paymentTotalAmount = computed(() => {
  const linesTotal = paymentForm.lines.reduce((sum: number, line: any) => sum + Number(line.requestAmount || 0), 0)
  const fee = paymentForm.fee
  const feeTotal =
    Number(fee.intermediateBankFee || 0) +
    Number(fee.bankCharge || 0) +
    Number(fee.freight || 0) +
    Number(fee.miscFee || 0) +
    Number(fee.rounding || 0)
  return Math.max(0, linesTotal + feeTotal)
})

function paymentRequestAmountMax(row: { pendingRequested?: number }) {
  const p = Number(row?.pendingRequested ?? 0)
  return p > 0 ? p : undefined
}

function buildFinancePaymentCode() {
  const d = new Date()
  const yy = String(d.getFullYear()).slice(-2)
  const MM = String(d.getMonth() + 1).padStart(2, '0')
  const dd = String(d.getDate()).padStart(2, '0')
  const HH = String(d.getHours()).padStart(2, '0')
  const mm = String(d.getMinutes()).padStart(2, '0')
  const ss = String(d.getSeconds()).padStart(2, '0')
  const rand = String(Math.floor(Math.random() * 100)).padStart(2, '0')
  return `FP${yy}${MM}${dd}${HH}${mm}${ss}${rand}`
}

function toDatePickerValue(v: unknown): string {
  if (v == null || v === '') return ''
  const s = String(v)
  const m = s.match(/^(\d{4}-\d{2}-\d{2})/)
  if (m) return m[1]
  const d = formatDisplayDate(s)
  return d === '--' ? '' : d
}

/** 与 PurchaseOrderItemList 行结构一致 */
function openPayment(row: any) {
  paymentForm.vendorId = row.vendorId || ''
  paymentForm.vendorName = row.vendorName || ''
  paymentForm.purchaseUserName = row.purchaseUserName || ''
  paymentForm.vendorBankId = ''
  paymentForm.paymentMode = 1
  paymentForm.currency = row.currency || 1
  paymentForm.remark = ''
  paymentForm.fee = {
    intermediateBankFee: 0,
    bankCharge: 0,
    freight: 0,
    miscFee: 0,
    rounding: 0,
    intermediateBankFeePayer: '我方'
  }
  paymentForm.lines = [
    {
      purchaseOrderId: row.purchaseOrderId,
      purchaseOrderItemId: row.purchaseOrderItemId,
      purchaseOrderCode: row.purchaseOrderCode,
      pn: row.pn,
      brand: row.brand,
      qty: row.qty,
      cost: row.cost,
      currency: row.currency,
      alreadyRequested: 0,
      pendingRequested: Number(row.lineTotal || 0),
      requestAmount: Number(row.lineTotal || 0),
      remark: ''
    }
  ]
  paymentDialogVisible.value = true
}

async function openArrival(row: any) {
  try {
    await ensureLogisticsDict()
  } catch {
    /* 字典失败时仍打开弹窗，下拉为空 */
  }
  arrivalForm.purchaseOrderItemId = row.purchaseOrderItemId || row.id || ''
  arrivalForm.purchaseOrderId = row.purchaseOrderId || ''
  arrivalForm.purchaseOrderCode = row.purchaseOrderCode || ''
  arrivalForm.vendorName = row.vendorName || ''
  arrivalForm.pn = row.pn || ''
  arrivalForm.expectedArrivalDate = toDatePickerValue(row.deliveryDate)
  arrivalForm.companyName = row.vendorName || ''
  arrivalForm.address = ''
  arrivalForm.phone = ''
  arrivalForm.contact = ''
  arrivalForm.arrivalMethod = ''
  arrivalForm.expressMethod = ''
  arrivalForm.expressNo = ''
  arrivalForm.inspectionRequirement = ''
  arrivalForm.remark = ''
  arrivalForm.lines = [
    {
      pn: row.pn || '',
      brand: row.brand || '',
      qty: Math.max(0, Math.round(Number(row.qty ?? 0))),
      spec: '',
      packaging: ''
    }
  ]
  arrivalDialogVisible.value = true
}

async function submitArrivalNotice() {
  if (arrivalSubmitting.value) return
  if (!arrivalForm.purchaseOrderItemId) {
    ElMessage.warning('缺少采购明细ID，无法创建到货通知')
    return
  }
  if (!arrivalForm.purchaseOrderId) {
    ElMessage.warning('缺少采购订单ID，无法创建到货通知')
    return
  }
  if (!arrivalForm.expectedArrivalDate) {
    ElMessage.warning('请填写预计到货日期')
    return
  }
  const expectQty = Number(arrivalForm.lines?.[0]?.qty ?? 0)
  if (!expectQty || expectQty <= 0) {
    ElMessage.warning('请填写大于 0 的本次到货数量')
    return
  }
  arrivalSubmitting.value = true
  try {
    await logisticsApi.createArrivalNotice({
      purchaseOrderItemId: arrivalForm.purchaseOrderItemId,
      expectQty,
      purchaseOrderId: arrivalForm.purchaseOrderId,
      expectedArrivalDate: arrivalForm.expectedArrivalDate
    })
    ElMessage.success('到货通知已创建')
    arrivalDialogVisible.value = false
    emit('success')
  } catch (error: any) {
    ElMessage.error(error?.message || '创建到货通知失败')
  } finally {
    arrivalSubmitting.value = false
  }
}

async function submitPayment() {
  if (paymentSubmitting.value) return
  if (!paymentForm.vendorId) {
    ElMessage.warning('缺少供应商ID，无法创建请款单')
    return
  }
  if (!paymentForm.vendorBankId) {
    ElMessage.warning('请选择供应商银行')
    return
  }
  if (!paymentForm.lines.length || paymentForm.lines.some((x: any) => Number(x.requestAmount || 0) <= 0)) {
    ElMessage.warning('请填写本次请款金额，且必须大于0')
    return
  }
  const lineRemark = paymentForm.lines
    .filter((x: any) => x.remark)
    .map((x: any) => `${x.pn || x.purchaseOrderCode}:${x.remark}`)
    .join('; ')
  const extRemark = [
    paymentForm.remark || '',
    `供应商银行:${paymentForm.vendorBankId}`,
    `费用(中转/手续费/运费/杂费/尾差):${paymentForm.fee.intermediateBankFee}/${paymentForm.fee.bankCharge}/${paymentForm.fee.freight}/${paymentForm.fee.miscFee}/${paymentForm.fee.rounding}`,
    `中转行费用承担方:${paymentForm.fee.intermediateBankFeePayer}`,
    lineRemark ? `明细备注:${lineRemark}` : ''
  ]
    .filter(Boolean)
    .join(' | ')

  paymentSubmitting.value = true
  try {
    const created = await financePaymentApi.create({
      financePaymentCode: buildFinancePaymentCode(),
      vendorId: paymentForm.vendorId,
      vendorName: paymentForm.vendorName,
      paymentMode: paymentForm.paymentMode,
      paymentCurrency: paymentForm.currency,
      paymentAmountToBe: paymentTotalAmount.value,
      remark: extRemark,
      items: paymentForm.lines.map((line: any) => ({
        purchaseOrderId: line.purchaseOrderId,
        purchaseOrderItemId: line.purchaseOrderItemId,
        paymentAmountToBe: Number(line.requestAmount || 0),
        pn: line.pn,
        brand: line.brand
      }))
    })
    const paymentId = (created as any)?.id || (created as any)?.data?.id || (created as any)?.data?.data?.id
    if (!paymentId) {
      throw new Error('创建请款单成功，但未获取到单据ID')
    }
    await financePaymentApi.updateStatus(paymentId, 2)
    ElMessage.success('请款单已提交审批')
    paymentDialogVisible.value = false
    emit('success')
  } catch (error: any) {
    ElMessage.error(error?.message || '提交审批失败，请稍后重试')
  } finally {
    paymentSubmitting.value = false
  }
}

defineExpose({ openPayment, openArrival })
</script>

<style scoped lang="scss">
@import '@/assets/styles/variables.scss';

.section-title {
  font-size: 14px;
  font-weight: 600;
  margin: 12px 0 8px;
  color: $text-primary;
}

.arrival-form-layout {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.arrival-form-layout :deep(.arrival-notice-form .el-form-item__label) {
  white-space: nowrap;
  padding-right: 10px;
  line-height: 1.4;
}
.arrival-section {
  border: 1px solid $border-panel;
  border-radius: 8px;
  padding: 12px;
  background: rgba(255, 255, 255, 0.02);
}
.arrival-section .section-title {
  font-size: 14px;
  margin-bottom: 8px;
}

:deep(.arrival-qty-input) {
  width: 100%;
  box-sizing: border-box;
}
:deep(.arrival-qty-input .el-input__wrapper) {
  width: 100%;
}
</style>
